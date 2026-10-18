using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text;
using Reforged.Sim;

/// <summary>
/// Browser bridge for the playtest page. Runs the same SimWorld the Unity build uses and packs its
/// state into int arrays (positions are raw Q16.16 fixed-point) for the JavaScript renderer.
/// The only gameplay added here is playtest glue: enemy waves and reviving knocked-out heroes.
/// </summary>
[SupportedOSPlatform("browser")]
public static partial class WebGame
{
    public const int FighterStride = 30;
    public const int ProjectileStride = 6;
    public const int DebrisStride = 6;
    public const int EventStride = 7;
    const int ReviveFrames = 180;

    static ContentDatabase content;
    static SimWorld world;
    static readonly InputFrame[] inputs = new InputFrame[SimConstants.MaxPlayers];
    static readonly int[] deadFor = new int[SimConstants.MaxPlayers];
    static int wave, waveCooldown;
    static uint seed = 1;

    public static void Main() { }

    static ContentDatabase Content => content ??= GrayboxContent.Create();

    /// <summary>Roster, move names, event names and stage geometry, as JSON (read once at startup).</summary>
    [JSExport]
    public static string Describe()
    {
        var stage = GrayboxStages.TestRoom();
        var sb = new StringBuilder("{\"heroes\":[");
        for (int i = 0; i < Content.Heroes.Count; i++)
        {
            var h = Content.Heroes[i];
            bool playable = h.Brain == BrainType.Player && !h.AssistOnly && h.Moves.Count > 0;
            if (i > 0) sb.Append(',');
            sb.Append("{\"name\":").Append(Q(h.Name)).Append(",\"playable\":").Append(playable ? "true" : "false")
              .Append(",\"assist\":").Append(h.AssistMoves.Length > 0 ? "true" : "false")
              .Append(",\"assistOnly\":").Append(h.AssistOnly ? "true" : "false")
              .Append(",\"mechanic\":").Append(Q(h.Mechanic.ToString()))
              .Append(",\"maxHealth\":").Append(h.MaxHealth).Append(",\"moves\":[");
            for (int m = 0; m < h.Moves.Count; m++)
            {
                if (m > 0) sb.Append(',');
                var mv = h.Moves[m];
                sb.Append("{\"name\":").Append(Q(mv.Name)).Append(",\"tier\":").Append((int)mv.Tier)
                  .Append(",\"total\":").Append(mv.TotalFrames).Append('}');
            }
            sb.Append("]}");
        }
        sb.Append("],\"roster\":[");
        for (int i = 0; i < RosterCatalog.All.Length; i++)
        {
            var e = RosterCatalog.All[i];
            if (i > 0) sb.Append(',');
            sb.Append("{\"name\":").Append(Q(e.Name)).Append(",\"role\":").Append(Q(e.Role.ToString()))
              .Append(",\"archetype\":").Append(Q(e.Archetype)).Append(",\"hero\":").Append(Content.IndexOf(e.Name)).Append('}');
        }
        sb.Append("],\"events\":[");
        var names = System.Enum.GetNames(typeof(SimEventType));
        for (int i = 0; i < names.Length; i++) { if (i > 0) sb.Append(','); sb.Append(Q(names[i])); }
        sb.Append("],\"modes\":[");
        var modes = System.Enum.GetNames(typeof(FighterMode));
        for (int i = 0; i < modes.Length; i++) { if (i > 0) sb.Append(','); sb.Append(Q(modes[i])); }
        sb.Append("],\"stage\":{\"minX\":").Append(F(stage.MinX)).Append(",\"maxX\":").Append(F(stage.MaxX))
          .Append(",\"minZ\":").Append(F(stage.MinZ)).Append(",\"maxZ\":").Append(F(stage.MaxZ)).Append(",\"platforms\":[");
        for (int i = 0; i < stage.Platforms.Count; i++)
        {
            var p = stage.Platforms[i];
            if (i > 0) sb.Append(',');
            sb.Append("{\"name\":").Append(Q(p.Name)).Append(",\"minX\":").Append(F(p.MinX)).Append(",\"maxX\":").Append(F(p.MaxX))
              .Append(",\"minZ\":").Append(F(p.MinZ)).Append(",\"maxZ\":").Append(F(p.MaxZ)).Append(",\"top\":").Append(F(p.Top))
              .Append(",\"solid\":").Append(p.Solid ? "true" : "false").Append(",\"destructible\":").Append(p.Destructible).Append('}');
        }
        sb.Append("],\"walls\":[");
        for (int i = 0; i < stage.Walls.Count; i++)
        {
            var w = stage.Walls[i];
            if (i > 0) sb.Append(',');
            sb.Append("{\"name\":").Append(Q(w.Name)).Append(",\"x\":").Append(F(w.X)).Append(",\"minY\":").Append(F(w.MinY))
              .Append(",\"maxY\":").Append(F(w.MaxY)).Append(",\"destructible\":").Append(w.Destructible).Append('}');
        }
        sb.Append("],\"destructibles\":[");
        for (int i = 0; i < stage.Destructibles.Count; i++)
        {
            var d = stage.Destructibles[i];
            if (i > 0) sb.Append(',');
            sb.Append("{\"name\":").Append(Q(d.Name)).Append(",\"tier\":").Append((int)d.Tier)
              .Append(",\"cx\":").Append(F(d.Center.X)).Append(",\"cy\":").Append(F(d.Center.Y)).Append(",\"cz\":").Append(F(d.Center.Z))
              .Append(",\"hx\":").Append(F(d.HalfExtents.X)).Append(",\"hy\":").Append(F(d.HalfExtents.Y)).Append(",\"hz\":").Append(F(d.HalfExtents.Z))
              .Append(",\"health\":").Append(d.Health).Append(",\"stages\":").Append(d.DamageStages).Append('}');
        }
        sb.Append("]},\"assembleMax\":").Append(SimConstants.AssembleMeterMax)
          .Append(",\"mechanicMax\":").Append(SimConstants.MechanicMax)
          .Append(",\"fighterStride\":").Append(FighterStride).Append('}');
        return sb.ToString();
    }

    /// <summary>Starts a match. heroes/assists are indexed by slot; -1 = empty slot.</summary>
    [JSExport]
    public static void Start(int[] heroes, int[] assists)
    {
        world = new SimWorld(Content, GrayboxStages.TestRoom(), seed++);
        for (int slot = 0; slot < SimConstants.MaxPlayers; slot++)
        {
            deadFor[slot] = 0;
            inputs[slot] = default(InputFrame);
            if (slot >= heroes.Length || heroes[slot] < 0) continue;
            var f = world.SpawnPlayer(slot, heroes[slot], new FixVec3(Fix.FromInt(-4 - slot), Fix.Zero, Fix.Cents(slot % 2 == 0 ? 0 : 120)),
                slot < assists.Length ? assists[slot] : -1);
            f.Gauge = SimConstants.GaugePerBar;
        }
        int trooper = Content.IndexOf(GrayboxContent.HydraTrooper), heavy = Content.IndexOf(GrayboxContent.HydraHeavy);
        world.SpawnEnemy(trooper, new FixVec3(3, 0, 0));
        world.SpawnEnemy(trooper, new FixVec3(5, 0, Fix.Cents(120)));
        world.SpawnEnemy(heavy, new FixVec3(9, 0, 0));
        world.SpawnEnemy(Content.IndexOf(GrayboxContent.TrainingDummy), new FixVec3(Fix.Cents(2250), 0, 0));
        world.SpawnEnemy(Content.IndexOf(GrayboxContent.TrainingDummy), new FixVec3(15, 4, 0));
        wave = 1;
        waveCooldown = 0;
    }

    /// <summary>Advances one 60 Hz tick. Five ints per slot: buttons, moveX, moveZ, aimX, aimY.</summary>
    [JSExport]
    public static void Step(int[] packed)
    {
        if (world == null) return;
        for (int slot = 0; slot < SimConstants.MaxPlayers; slot++)
        {
            int o = slot * 5;
            if (o + 4 >= packed.Length) break;
            inputs[slot] = InputFrame.Of((Buttons)packed[o], packed[o + 1], packed[o + 2], packed[o + 3], packed[o + 4]);
        }
        world.Step(inputs);
        Revive();
        Waves();
    }

    /// <summary>Knocked-out heroes get back up after 3 seconds with half health.</summary>
    static void Revive()
    {
        for (int slot = 0; slot < SimConstants.MaxPlayers; slot++)
        {
            var f = world.PlayerFighter(slot);
            if (f == null) continue;
            if (f.Mode != FighterMode.Dead) { deadFor[slot] = 0; continue; }
            if (++deadFor[slot] < ReviveFrames) continue;
            f.Health = Content[f.HeroIndex].MaxHealth / 2;
            f.Mode = FighterMode.Idle;
            f.InvulnFrames = 90;
            f.ResetCombo();
            deadFor[slot] = 0;
        }
    }

    /// <summary>When every Hydra agent is down, the next, bigger wave drops in near the players.</summary>
    static void Waves()
    {
        foreach (var f in world.Fighters)
        {
            var hero = Content[f.HeroIndex];
            if (f.Team == 1 && f.IsAlive && hero.Brain != BrainType.None) return;
        }
        if (++waveCooldown < 120) return;
        waveCooldown = 0;
        wave++;
        Fix center = Fix.Zero;
        int n = 0;
        for (int slot = 0; slot < SimConstants.MaxPlayers; slot++)
        {
            var p = world.PlayerFighter(slot);
            if (p == null) continue;
            center += p.Pos.X;
            n++;
        }
        if (n > 0) center = center / n;
        int trooper = Content.IndexOf(GrayboxContent.HydraTrooper), heavy = Content.IndexOf(GrayboxContent.HydraHeavy);
        int troopers = 2 + wave, heavies = wave / 2;
        for (int i = 0; i < troopers; i++)
        {
            int side = i % 2 == 0 ? 1 : -1;
            var x = Fix.Clamp(center + Fix.FromInt(side * (6 + i)), world.Stage.MinX + 1, world.Stage.MaxX - 1);
            world.SpawnEnemy(trooper, new FixVec3(x, 0, Fix.Cents((i % 3 - 1) * 150)), (sbyte)-side);
        }
        for (int i = 0; i < heavies; i++)
        {
            var x = Fix.Clamp(center + Fix.FromInt(9 + 2 * i), world.Stage.MinX + 1, world.Stage.MaxX - 1);
            world.SpawnEnemy(heavy, new FixVec3(x, 0, 0));
        }
    }

    /// <summary>
    /// Header: frame, assemble, wave, fighters, projectiles, debris, destructibles.
    /// Then 4 player blocks (8 ints), fighters, projectiles, debris, destructibles (3 ints).
    /// </summary>
    [JSExport]
    public static int[] State()
    {
        if (world == null) return new int[0];
        int nf = world.Fighters.Count, np = world.Projectiles.Count, nd = world.Debris.Count, nx = world.Destructibles.Length;
        var s = new int[7 + 4 * 8 + nf * FighterStride + np * ProjectileStride + nd * DebrisStride + nx * 3];
        int o = 0;
        s[o++] = world.Frame; s[o++] = world.AssembleMeter; s[o++] = wave; s[o++] = nf; s[o++] = np; s[o++] = nd; s[o++] = nx;
        for (int slot = 0; slot < 4; slot++)
        {
            var p = world.Players[slot];
            if (p == null) { o += 8; continue; }
            s[o++] = 1; s[o++] = p.FighterId; s[o++] = p.StylePoints; s[o++] = (int)p.Rank; s[o++] = p.Score;
            s[o++] = p.BestCombo; s[o++] = p.AssistCooldown; s[o++] = p.AssistHeroIndex;
        }
        foreach (var f in world.Fighters)
        {
            var half = world.HurtHalf(f);
            int start = o;
            s[o++] = f.Id; s[o++] = f.HeroIndex; s[o++] = f.Team; s[o++] = f.PlayerSlot; s[o++] = f.IsAssist ? 1 : 0;
            s[o++] = f.Pos.X.Raw; s[o++] = f.Pos.Y.Raw; s[o++] = f.Pos.Z.Raw; s[o++] = f.Facing; s[o++] = (int)f.Mode;
            s[o++] = f.MoveIndex; s[o++] = f.MoveFrame; s[o++] = f.Health; s[o++] = f.Gauge; s[o++] = f.ComboHits;
            s[o++] = f.MechState; s[o++] = f.Meter; s[o++] = f.InvulnFrames > 0 ? 1 : 0; s[o++] = half.X.Raw; s[o++] = half.Y.Raw;
            s[o++] = f.ShrunkFrames; s[o++] = f.BuffFrames; s[o++] = world.MechanicTier(f); s[o++] = f.ArmorLeft;
            // Active hitbox, when the current move can hit this frame.
            if (f.Mode == FighterMode.Attack && f.MoveIndex >= 0)
            {
                var m = Content[f.HeroIndex].Moves[f.MoveIndex];
                if (m.HasHitbox && f.MoveFrame >= m.ActiveStart && f.MoveFrame <= m.ActiveEnd)
                {
                    world.AttackBox(f, m, out var c, out var h);
                    s[o++] = 1; s[o++] = c.X.Raw; s[o++] = c.Y.Raw; s[o++] = c.Z.Raw; s[o++] = h.X.Raw; s[o++] = h.Y.Raw;
                }
            }
            o = start + FighterStride;
        }
        foreach (var p in world.Projectiles)
        {
            s[o++] = p.Id; s[o++] = p.HeroIndex; s[o++] = p.MoveIndex; s[o++] = p.Pos.X.Raw; s[o++] = p.Pos.Y.Raw; s[o++] = p.Pos.Z.Raw;
        }
        foreach (var d in world.Debris)
        {
            s[o++] = d.Id; s[o++] = d.Pos.X.Raw; s[o++] = d.Pos.Y.Raw; s[o++] = d.Pos.Z.Raw; s[o++] = d.Weight; s[o++] = (int)d.Mode;
        }
        foreach (var d in world.Destructibles)
        {
            s[o++] = d.Broken ? 1 : 0; s[o++] = d.Stage; s[o++] = d.Health;
        }
        return s;
    }

    /// <summary>This tick's events: type, a, b, value, x, y, z (positions raw fixed-point).</summary>
    [JSExport]
    public static int[] Events()
    {
        if (world == null) return new int[0];
        var e = new int[world.Events.Count * EventStride];
        int o = 0;
        foreach (var ev in world.Events)
        {
            e[o++] = (int)ev.Type; e[o++] = ev.A; e[o++] = ev.B; e[o++] = ev.Value;
            e[o++] = ev.Pos.X.Raw; e[o++] = ev.Pos.Y.Raw; e[o++] = ev.Pos.Z.Raw;
        }
        return e;
    }

    /// <summary>Fills a player's Hero Gauge (playtest cheat so everyone can try Supers and Ultimates).</summary>
    [JSExport]
    public static void FillGauge(int slot)
    {
        var f = world?.PlayerFighter(slot);
        if (f != null) f.Gauge = SimConstants.MaxGauge;
    }

    static string F(Fix v) => v.ToString();
    static string Q(string s) => "\"" + s.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
}
