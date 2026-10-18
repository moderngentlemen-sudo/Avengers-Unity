using System;

namespace Reforged.Sim
{
    // Hero mechanics. Each signature hero has one meter (Fighter.Meter) and one state (MechState):
    //
    //   Heat (Iron Man)   Heat moves add heat; it cools after a pause. At max he overheats for 2s:
    //                     no heat moves, no hover, no air jumps. Hold Jump in the air to hover
    //                     (builds heat). D-pad down vents 300+ heat as an area blast.
    //   Rage (Hulk)       Fills from damage dealt and taken; tiers at 333/666 (Savage, World Breaker
    //                     with Gamma Rage). Each tier adds damage, armor and unlocks bigger throws.
    //                     D-pad down spends a tier on a Gamma Roar that staggers everyone nearby.
    //   Density (Vision)  D-pad down = Phase (intangible, through walls and floors, drains energy;
    //                     attacking re-solidifies into an unblockable Phase Strike). D-pad right =
    //                     Diamond (armored, +25% damage, slow, aerial attacks break floors).
    //                     D-pad left = normal density, which regenerates energy.
    //   Size (Ant-Man)    D-pad down = Small (tiny hurtbox, fast, ignores enemy armor with crits),
    //                     right = Giant (double reach, hits every lane, +50% damage, wrecks scenery,
    //                     drains Pym particles), left = normal. Changing size cancels any move.
    public sealed partial class SimWorld
    {
        HeroDef HeroOf(Fighter f) => Content.Heroes[f.HeroIndex];

        public bool IsOverheated(Fighter f) => HeroOf(f).Mechanic == HeroMechanic.Heat && f.MechState == 1;
        public bool IsPhased(Fighter f) => HeroOf(f).Mechanic == HeroMechanic.Density && f.MechState == 1;
        public bool IsDiamond(Fighter f) => HeroOf(f).Mechanic == HeroMechanic.Density && f.MechState == 2;
        public bool IsSmall(Fighter f) => HeroOf(f).Mechanic == HeroMechanic.Size && f.MechState < 0;
        public bool IsGiant(Fighter f) => HeroOf(f).Mechanic == HeroMechanic.Size && f.MechState > 0;

        /// <summary>Rage tier 0..3 for Hulk (3 = World Breaker during Gamma Rage); 0 for everyone else.</summary>
        public int MechanicTier(Fighter f)
        {
            if (HeroOf(f).Mechanic != HeroMechanic.Rage) return 0;
            return f.MechTimer > 0 ? 3 : Math.Min(2, f.Meter / SimConstants.RageTierSize);
        }

        public int DamagePercent(Fighter f)
        {
            if (f == null) return 100;
            switch (HeroOf(f).Mechanic)
            {
                case HeroMechanic.Rage: return 100 + 15 * MechanicTier(f);
                case HeroMechanic.Density: return f.MechState == 2 ? 125 : 100;
                case HeroMechanic.Size: return f.MechState < 0 ? 70 : f.MechState > 0 ? 150 : 100;
                default: return 100;
            }
        }

        public int SpeedPercent(Fighter f)
        {
            if (IsDiamond(f)) return 50;
            if (IsSmall(f)) return 140;
            if (IsGiant(f)) return 60;
            return 100;
        }

        int AirJumpsFor(Fighter f, HeroDef hero) => IsOverheated(f) || IsDiamond(f) || IsGiant(f) ? 0 : hero.AirJumps;

        /// <summary>Hurtbox half extents after size changes and Quantum Tunnel shrinking.</summary>
        public FixVec3 BodyHalf(Fighter f)
        {
            var half = HeroOf(f).BodyHalfExtents;
            if (IsSmall(f)) half = half * Fix.Ratio(2, 5);
            else if (IsGiant(f)) half = half * 2;
            if (f.ShrunkFrames > 0) half = half * Fix.Half;
            return half;
        }

        /// <summary>The move's hitbox in world space. Giants reach twice as far and hit every lane.</summary>
        public void AttackBox(Fighter a, MoveDef m, out FixVec3 center, out FixVec3 half)
        {
            var offset = new FixVec3(m.HitboxOffset.X * a.Facing, m.HitboxOffset.Y, m.HitboxOffset.Z);
            half = m.HitboxHalfExtents;
            if (IsGiant(a))
            {
                offset = new FixVec3(offset.X * 2, offset.Y * 2, offset.Z);
                half = new FixVec3(half.X * 2, half.Y * 2, Stage.MaxZ - Stage.MinZ);
            }
            else if (IsSmall(a))
            {
                offset = offset * Fix.Ratio(3, 5);
                half = half * Fix.Ratio(3, 5);
            }
            center = a.Pos + offset;
        }

        int DestructionPowerOf(Fighter a, MoveDef m) =>
            m.DestructionPower + (IsGiant(a) ? 80 : 0) + (IsDiamond(a) ? 100 : 0) + (MechanicTier(a) == 3 ? m.DestructionPower : 0);

        bool IsHovering(Fighter f) =>
            HeroOf(f).Mechanic == HeroMechanic.Heat && f.MechState == 0 && !f.IsGrounded && f.Vel.Y.Raw <= 0 &&
            f.Input.Has(Buttons.Jump) && (f.Mode == FighterMode.Airborne || f.Mode == FighterMode.Attack);

        /// <summary>Hits that Hulk's rage, Vision's Diamond form or Giant-Man shrug off without flinching.</summary>
        bool MechanicArmor(Fighter victim, in HitSpec hit)
        {
            if ((hit.Flags & MoveFlags.Unblockable) != 0) return false;
            var hero = HeroOf(victim);
            switch (hero.Mechanic)
            {
                case HeroMechanic.Rage:
                    int tier = MechanicTier(victim);
                    if (tier >= 3) return true;
                    if (tier >= 2 && hit.Tier == MoveTier.Light) return true;
                    if (tier >= 1 && victim.Mode == FighterMode.Attack && victim.MoveIndex >= 0)
                    {
                        var m = hero.Moves[victim.MoveIndex];
                        return m.Tier >= MoveTier.Heavy && victim.MoveFrame <= m.ActiveEnd;
                    }
                    return false;
                case HeroMechanic.Density: return victim.MechState == 2;
                case HeroMechanic.Size: return victim.MechState > 0 && hit.Tier == MoveTier.Light;
                default: return false;
            }
        }

        void GainRage(Fighter f, int amount)
        {
            if (f == null || f.IsAssist || HeroOf(f).Mechanic != HeroMechanic.Rage || amount <= 0) return;
            f.Meter = Math.Min(SimConstants.MechanicMax, f.Meter + amount);
            f.MechIdleFrames = 0;
        }

        void AddHeat(Fighter f, int amount)
        {
            f.Meter += amount;
            f.MechIdleFrames = 0;
            if (f.Meter < SimConstants.MechanicMax) return;
            f.Meter = SimConstants.MechanicMax;
            f.MechState = 1;
            f.MechTimer = SimConstants.OverheatFrames;
            Emit(SimEventType.Overheat, f.Id, f.PlayerSlot, 0, f.Pos);
        }

        // ------------------------------------------------------------------ per tick

        void TickMechanic(Fighter f, HeroDef hero)
        {
            if (f.ShrunkFrames > 0) f.ShrunkFrames--;
            if (hero.Mechanic == HeroMechanic.None || f.IsAssist) return;
            f.MechIdleFrames++;
            const int max = SimConstants.MechanicMax;

            switch (hero.Mechanic)
            {
                case HeroMechanic.Heat:
                    if (f.MechState == 1)
                    {
                        f.Meter = Math.Max(0, f.Meter - max / SimConstants.OverheatFrames - 1);
                        if (--f.MechTimer <= 0) { f.MechState = 0; f.Meter = 0; }
                    }
                    else if (IsHovering(f))
                    {
                        AddHeat(f, SimConstants.HoverHeatPerFrame);
                    }
                    else if (f.MechIdleFrames > SimConstants.HeatCoolDelay)
                    {
                        f.Meter = Math.Max(0, f.Meter - (f.IsGrounded ? 4 : 2));
                    }
                    break;

                case HeroMechanic.Rage:
                    if (f.MechTimer > 0) f.MechTimer--;
                    if (f.MechIdleFrames > SimConstants.RageDecayDelay && f.Meter > 0) f.Meter--;
                    int tier = MechanicTier(f);
                    if (tier != f.MechState)
                    {
                        f.MechState = tier;
                        Emit(SimEventType.RageTier, f.Id, tier, f.Meter, f.Pos);
                    }
                    break;

                case HeroMechanic.Density:
                    if (f.MechState == 1)
                    {
                        f.Meter -= SimConstants.PhaseDrainPerFrame;
                        f.InvulnFrames = Math.Max(f.InvulnFrames, 2);
                    }
                    else if (f.MechState == 2) f.Meter -= SimConstants.DiamondDrainPerFrame;
                    else f.Meter = Math.Min(max, f.Meter + SimConstants.DensityRegenPerFrame);
                    if (f.Meter <= 0)
                    {
                        f.Meter = 0;
                        SetMechState(f, 0, SimEventType.DensityChanged);
                        Emit(SimEventType.MechanicDepleted, f.Id, f.PlayerSlot, 0, f.Pos);
                    }
                    break;

                case HeroMechanic.Size:
                    if (f.MechState > 0) f.Meter -= SimConstants.GiantDrainPerFrame;
                    else f.Meter = Math.Min(max, f.Meter + SimConstants.PymRegenPerFrame);
                    if (f.Meter <= 0)
                    {
                        f.Meter = 0;
                        SetMechState(f, 0, SimEventType.SizeChanged);
                        Emit(SimEventType.MechanicDepleted, f.Id, f.PlayerSlot, 0, f.Pos);
                    }
                    break;
            }
        }

        void SetMechState(Fighter f, int state, SimEventType evt)
        {
            if (f.MechState == state) return;
            f.MechState = state;
            Emit(evt, f.Id, state, f.Meter, f.Pos);
        }

        // ------------------------------------------------------------------ D-pad input

        bool InNeutral(Fighter f) =>
            f.Mode == FighterMode.Idle || f.Mode == FighterMode.Walk || f.Mode == FighterMode.Guard || f.Mode == FighterMode.Airborne;

        /// <summary>Mechanic D-pad commands. Density and size changes work mid-move and cancel it.</summary>
        void HandleMechanicInput(Fighter f, HeroDef hero)
        {
            const int enter = SimConstants.MechanicEnterCost;
            switch (hero.Mechanic)
            {
                case HeroMechanic.Heat:
                    if (!IsOverheated(f) && f.Meter >= SimConstants.VentMinHeat && f.Mode != FighterMode.Attack &&
                        ConsumeIfBuffered(f, Buttons.ToolDown))
                        Vent(f);
                    break;

                case HeroMechanic.Rage:
                    if (f.Meter >= SimConstants.RoarCost && InNeutral(f) && ConsumeIfBuffered(f, Buttons.ToolDown))
                        GammaRoar(f);
                    break;

                case HeroMechanic.Density:
                case HeroMechanic.Size:
                    var evt = hero.Mechanic == HeroMechanic.Density ? SimEventType.DensityChanged : SimEventType.SizeChanged;
                    int target = int.MinValue;
                    if (ConsumeIfBuffered(f, Buttons.ToolLeft)) target = 0;
                    else if (Buffered(f, Buttons.ToolDown) && (hero.Mechanic == HeroMechanic.Size || f.Meter >= enter))
                    {
                        Consume(f, Buttons.ToolDown);
                        target = hero.Mechanic == HeroMechanic.Density ? 1 : -1;
                    }
                    else if (Buffered(f, Buttons.ToolRight) && f.Meter >= enter)
                    {
                        Consume(f, Buttons.ToolRight);
                        target = hero.Mechanic == HeroMechanic.Density ? 2 : 1;
                    }
                    if (target == int.MinValue || target == f.MechState) break;
                    if (f.Mode == FighterMode.Attack) ReturnToNeutral(f); // shifting cancels the current move
                    SetMechState(f, target, evt);
                    break;
            }

            TryToolMove(f, hero, Buttons.ToolLeft, hero.ToolLeftIndex);
            TryToolMove(f, hero, Buttons.ToolRight, hero.ToolRightIndex);
        }

        void TryToolMove(Fighter f, HeroDef hero, Buttons button, int moveIndex)
        {
            if (moveIndex < 0 || !Buffered(f, button)) return;
            bool can = InNeutral(f) || (f.Mode == FighterMode.Attack && f.MoveIndex >= 0 && CanCancelInto(f, hero, hero.Moves[moveIndex]));
            if (can && hero.Moves[moveIndex].Aerial == !f.IsGrounded && StartMove(f, hero, moveIndex)) Consume(f, button);
        }

        void Vent(Fighter f)
        {
            int heat = f.Meter;
            f.Meter = 0;
            var spec = new HitSpec(heat / 8, 30, 8, 3, 0, StyleKey(f.HeroIndex, 250),
                FixVec3.Cents(14, 22, 0), MoveFlags.Launcher, MoveTier.Special);
            AreaHit(f, f.Pos.WithY(f.Pos.Y + Fix.One), new FixVec3(Fix.Cents(250), Fix.Cents(150), Fix.Cents(150)), spec);
            if (f.IsGrounded)
            {
                f.MoveIndex = -1;
                SetMode(f, FighterMode.Recover, 16);
            }
            Emit(SimEventType.HeatVent, f.Id, f.PlayerSlot, heat, f.Pos);
        }

        void GammaRoar(Fighter f)
        {
            f.Meter -= SimConstants.RoarCost;
            var spec = new HitSpec(0, 45, 10, 1, 0, StyleKey(f.HeroIndex, 251),
                FixVec3.Cents(6, 0, 0), MoveFlags.Unblockable, MoveTier.Heavy);
            AreaHit(f, f.Pos.WithY(f.Pos.Y + Fix.One), new FixVec3(Fix.FromInt(4), Fix.FromInt(2), Fix.FromInt(3)), spec);
            f.Hitstop = Math.Max(f.Hitstop, 10);
            Emit(SimEventType.GammaRoar, f.Id, f.PlayerSlot, 0, f.Pos);
        }

        int AreaHit(Fighter source, FixVec3 center, FixVec3 half, in HitSpec spec)
        {
            int hits = 0;
            for (int i = 0; i < Fighters.Count; i++)
            {
                var t = Fighters[i];
                if (!CanBeHit(t, source.Team) || !Overlaps(center, half, HurtCenter(t), HurtHalf(t))) continue;
                ApplyHit(source, spec, t, t.Pos.X >= source.Pos.X ? 1 : -1, false, source.OwnerSlot, false);
                hits++;
            }
            return hits;
        }

        // ------------------------------------------------------------------ move hooks

        bool MechanicAllowsMove(Fighter f, MoveDef m) => !(m.HeatCost > 0 && IsOverheated(f));

        void OnMechanicMoveStarted(Fighter f, MoveDef m)
        {
            if (m.HeatCost > 0) AddHeat(f, m.HeatCost);

            f.PhaseStrike = false;
            if (IsPhased(f))
            {
                // Re-solidifying inside the enemy: the next hit can't be blocked and stuns longer.
                SetMechState(f, 0, SimEventType.DensityChanged);
                f.PhaseStrike = true;
                Emit(SimEventType.PhaseStrike, f.Id, m.Index, 0, f.Pos);
            }

            switch (m.Effect)
            {
                case MechanicEffect.GammaRage:
                    f.MechTimer = m.EffectFrames;
                    break;
                case MechanicEffect.PhaseField:
                    for (int i = 0; i < Fighters.Count; i++)
                    {
                        var o = Fighters[i];
                        if (o.Team == f.Team && o.IsAlive && !o.IsAssist) o.InvulnFrames = Math.Max(o.InvulnFrames, m.EffectFrames);
                    }
                    Emit(SimEventType.TeamBuff, f.Id, f.Team, m.EffectFrames, f.Pos);
                    break;
                case MechanicEffect.QuantumTunnel:
                    for (int i = 0; i < Fighters.Count; i++)
                    {
                        var o = Fighters[i];
                        if (o.Team != f.Team && o.IsAlive && !o.IsAssist) o.ShrunkFrames = Math.Max(o.ShrunkFrames, m.EffectFrames);
                    }
                    break;
            }
        }

        void LiftDebrisAround(Fighter f)
        {
            for (int i = 0; i < Debris.Count; i++)
            {
                var d = Debris[i];
                if (!d.Active || d.Mode == DebrisMode.Held) continue;
                if (Fix.Abs(d.Pos.X - f.Pos.X) > 4 || Fix.Abs(d.Pos.Y - f.Pos.Y) > 1) continue;
                d.Mode = DebrisMode.Loose;
                d.SupportPlatform = -1;
                d.Vel = new FixVec3(Fix.Zero, Fix.Cents(32), Fix.Zero);
                Emit(SimEventType.DebrisLifted, f.Id, d.Id, d.Weight, d.Pos);
            }
        }

        Fix LowestPlatformTop()
        {
            var low = Stage.Platforms[0].Top;
            for (int i = 1; i < Stage.Platforms.Count; i++) low = Fix.Min(low, Stage.Platforms[i].Top);
            return low;
        }
    }
}
