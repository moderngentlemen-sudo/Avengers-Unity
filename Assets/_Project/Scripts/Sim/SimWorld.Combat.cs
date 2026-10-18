using System;

namespace Reforged.Sim
{
    // Hit detection, hit reactions, combos, style, assists and team ultimates.
    public sealed partial class SimWorld
    {
        static readonly HitSpec TeamUltimateHit =
            new HitSpec(250, 0, 12, 0, 0, -300, FixVec3.Cents(10, 30, 0), MoveFlags.Launcher | MoveFlags.Unblockable, MoveTier.Ultimate);

        public static int StyleKey(int heroIndex, int moveIndex) => heroIndex * 256 + moveIndex;

        FixVec3 HurtCenter(Fighter f)
        {
            var half = BodyHalf(f);
            return f.Mode == FighterMode.Knockdown ? f.Pos + new FixVec3(Fix.Zero, Fix.Cents(30), Fix.Zero) : f.Pos.WithY(f.Pos.Y + half.Y);
        }

        public FixVec3 HurtHalf(Fighter f)
        {
            var half = BodyHalf(f);
            return f.Mode == FighterMode.Knockdown ? new FixVec3(half.Y, Fix.Cents(30), half.Z) : half;
        }

        static bool Overlaps(FixVec3 ca, FixVec3 ha, FixVec3 cb, FixVec3 hb) =>
            Fix.Abs(ca.X - cb.X) <= ha.X + hb.X &&
            Fix.Abs(ca.Y - cb.Y) <= ha.Y + hb.Y &&
            Fix.Abs(ca.Z - cb.Z) <= ha.Z + hb.Z;

        bool CanBeHit(Fighter t, byte attackerTeam) =>
            t.Team != attackerTeam && t.IsAlive && !t.IsAssist && t.InvulnFrames == 0;

        /// <summary>Hit-box/hurt-box pass for melee moves. Ascending id order keeps it deterministic.</summary>
        void ResolveAttacks()
        {
            for (int ai = 0; ai < Fighters.Count; ai++)
            {
                var a = Fighters[ai];
                if (a.Mode != FighterMode.Attack || a.Hitstop > 0 || a.MoveIndex < 0) continue;
                var hero = Content.Heroes[a.HeroIndex];
                var m = hero.Moves[a.MoveIndex];
                if (!m.HasHitbox || a.MoveFrame < m.ActiveStart || a.MoveFrame > m.ActiveEnd) continue;

                AttackBox(a, m, out var center, out var half);
                var spec = HitSpec.From(m, StyleKey(a.HeroIndex, a.MoveIndex));

                for (int ti = 0; ti < Fighters.Count; ti++)
                {
                    var t = Fighters[ti];
                    if (!CanBeHit(t, a.Team) || a.InHitList(t.Id)) continue;
                    if (!Overlaps(center, half, HurtCenter(t), HurtHalf(t))) continue;
                    a.AddHitList(t.Id);
                    int dir = a.Facing;
                    if (m.Has(MoveFlags.DirectionalThrow) && Math.Abs((int)a.Input.MoveX) >= InputFrame.StickCommandThreshold)
                        dir = Math.Sign(a.Input.MoveX);
                    ApplyHit(a, spec, t, dir, true, a.OwnerSlot, false);
                }

                int destruction = DestructionPowerOf(a, m);
                if (destruction > 0)
                {
                    for (int d = 0; d < Destructibles.Length; d++)
                    {
                        int key = SimConstants.DestructibleKeyBase + d;
                        if (Destructibles[d].Broken || a.InHitList(key)) continue;
                        var def = Stage.Destructibles[d];
                        if (!Overlaps(center, half, def.Center, def.HalfExtents)) continue;
                        a.AddHitList(key);
                        DamageDestructible(d, destruction, a.OwnerSlot);
                    }
                }

                for (int di = 0; di < Debris.Count; di++)
                {
                    var d = Debris[di];
                    int key = SimConstants.DebrisKeyBase + d.Id;
                    if (!d.Active || d.Mode == DebrisMode.Held || a.InHitList(key)) continue;
                    if (!Overlaps(center, half, d.Pos.WithY(d.Pos.Y + d.HalfExtents.Y), d.HalfExtents)) continue;
                    a.AddHitList(key);
                    BatDebris(a, m, d);
                }
            }
        }

        /// <summary>
        /// Applies one hit. Returns true when the hit "connected" for cancel purposes (including
        /// blocked and armored hits). <paramref name="dirSign"/> is the push direction.
        /// </summary>
        bool ApplyHit(Fighter source, in HitSpec hit, Fighter victim, int dirSign, bool sourceHitstop, int ownerSlot, bool environmental)
        {
            var victimHero = Content.Heroes[victim.HeroIndex];
            var gaugeTarget = source != null && source.IsAssist ? PlayerFighter(source.OwnerSlot) : source;

            if (victim.Mode == FighterMode.Knockdown)
            {
                if ((hit.Flags & MoveFlags.OTG) == 0 || (victim.BouncesUsed & Fighter.BounceOTG) != 0) return false;
                victim.BouncesUsed |= Fighter.BounceOTG;
            }
            if (victim.JuggleExhausted) return false;

            int juggleCost = source != null && source.IsAssist ? hit.JuggleCost / 2 : hit.JuggleCost;
            if (victim.Mode == FighterMode.Juggle && victim.JugglePoints + juggleCost > SimConstants.MaxJugglePoints)
            {
                // Juggle decay: the victim drops out and can't be hit again until it lands.
                victim.JuggleExhausted = true;
                Emit(SimEventType.JuggleExhausted, victim.Id, ownerSlot, victim.ComboHits, victim.Pos);
                return false;
            }

            bool phaseStrike = source != null && sourceHitstop && source.PhaseStrike;
            if (phaseStrike)
            {
                source.PhaseStrike = false;
                Emit(SimEventType.PhaseStrike, source.Id, victim.Id, 1, victim.Pos);
            }
            if (!phaseStrike && CanBlock(victim, dirSign, hit))
            {
                if (Frame - victim.LastPress[InputFrame.IndexOf(Buttons.Block)] <= SimConstants.ParryWindowFrames)
                {
                    Parry(source, victim);
                    return false;
                }
                SetMode(victim, FighterMode.Blockstun, Math.Max(6, hit.Hitstun / 2));
                victim.Vel = new FixVec3(hit.Knockback.X / 2 * dirSign, victim.Vel.Y, Fix.Zero);
                victim.Hitstop = hit.Hitstop;
                if (sourceHitstop && source != null) source.Hitstop = hit.Hitstop;
                AddGauge(victim, hit.GaugeGain / 2);
                if (source != null && sourceHitstop) source.MoveHasHit = true;
                Emit(SimEventType.Blocked, source != null ? source.Id : -1, victim.Id, 0, victim.Pos);
                return true;
            }

            int scaling = Math.Max(30, 100 - 6 * Math.Max(0, victim.ComboHits - 2));
            int damage = hit.Damage * scaling / 100 * DamagePercent(source) / 100;
            if (victim.BuffFrames > 0) damage /= 2;
            if (IsDiamond(victim)) damage /= 2;
            if (victim.ShrunkFrames > 0) damage = damage * 3 / 2;
            bool unblockable = (hit.Flags & MoveFlags.Unblockable) != 0 || phaseStrike;
            // Small Ant-Man slips inside armor: armored enemies take a critical hit and flinch anyway.
            bool armorCrit = victim.ArmorLeft > 0 && source != null && IsSmall(source);
            if (armorCrit) damage = damage * 3 / 2;

            if (victim.ArmorLeft > 0 && !unblockable && !armorCrit)
            {
                victim.ArmorRegenTimer = SimConstants.ArmorRegenFrames;
                bool heavy = hit.Tier >= MoveTier.Heavy;
                if (heavy) victim.ArmorLeft--;
                victim.Health = Math.Max(1, victim.Health - (heavy ? damage / 2 : damage / 4));
                victim.Hitstop = hit.Hitstop;
                if (sourceHitstop && source != null) { source.Hitstop = hit.Hitstop; source.MoveHasHit = true; }
                AddGauge(gaugeTarget, hit.GaugeGain / 2);
                if (victim.ArmorLeft == 0)
                {
                    victim.MoveIndex = -1;
                    SetMode(victim, FighterMode.Hitstun, SimConstants.ArmorBreakStunFrames);
                    Emit(SimEventType.ArmorBreak, source != null ? source.Id : -1, victim.Id, 0, victim.Pos);
                    AddStyle(ownerSlot, hit.StyleKey, hit.Tier, false);
                }
                else
                {
                    Emit(SimEventType.ArmorHit, source != null ? source.Id : -1, victim.Id, victim.ArmorLeft, victim.Pos);
                }
                return true;
            }
            if (unblockable && victim.ArmorLeft > 0 && !armorCrit)
            {
                victim.ArmorLeft = 0;
                Emit(SimEventType.ArmorBreak, source != null ? source.Id : -1, victim.Id, 0, victim.Pos);
            }

            bool superArmor = false;
            if (victim.Mode == FighterMode.Attack && victim.MoveIndex >= 0)
            {
                var vm = victimHero.Moves[victim.MoveIndex];
                superArmor = vm.Has(MoveFlags.SuperArmor) && victim.MoveFrame <= vm.ActiveEnd;
            }
            superArmor |= MechanicArmor(victim, hit);

            bool wasAlive = victim.Health > 0;
            victim.Health -= damage;
            if (victim.ComboHits == 0) victim.ComboOwnerSlot = (sbyte)ownerSlot;
            victim.ComboHits++;
            victim.ComboDamage += damage;
            victim.JugglePoints += juggleCost;
            GainRage(source != null && source.IsAssist ? null : source, damage / 2);
            GainRage(victim, damage);

            if (source != null)
            {
                source.LastVictimId = victim.Id;
                if (sourceHitstop)
                {
                    source.MoveHasHit = true;
                    source.Hitstop = hit.Hitstop;
                }
            }
            AddGauge(gaugeTarget, hit.GaugeGain);
            AddGauge(victim, hit.GaugeGain / 2);
            // Ultimates never feed the Assemble meter, so a Team Ultimate can't refill itself.
            if (ownerSlot >= 0 && hit.Tier != MoveTier.Ultimate)
                AssembleMeter = Math.Min(SimConstants.AssembleMeterMax, AssembleMeter + Math.Max(1, damage / 4));
            if (victim.IsPlayer) Players[victim.PlayerSlot].StylePoints = Math.Max(0, Players[victim.PlayerSlot].StylePoints - 60);
            victim.Hitstop = hit.Hitstop;

            AddStyle(ownerSlot, hit.StyleKey, hit.Tier, environmental);
            Emit(SimEventType.Hit, source != null ? source.Id : -1, victim.Id, damage, victim.Pos);
            if (environmental) Emit(SimEventType.EnvironmentalHit, source != null ? source.Id : -1, victim.Id, damage, victim.Pos);

            if (superArmor && victim.Health > 0)
            {
                Emit(SimEventType.ArmorHit, source != null ? source.Id : -1, victim.Id, 0, victim.Pos);
                return true;
            }

            victim.MoveIndex = -1;
            DropHeldDebris(victim);
            byte bounces = (byte)(BounceBits(hit.Flags) & ~victim.BouncesUsed);
            victim.PendingBounce |= bounces;

            bool airborne = !victim.IsGrounded || victim.Mode == FighterMode.Juggle;
            var ky = hit.Knockback.Y;
            if (airborne && ky.Raw >= 0 && ky < SimConstants.AirHitMinLift) ky = SimConstants.AirHitMinLift;
            if (victim.Health <= 0)
            {
                victim.Health = 0;
                if (ky < Fix.Cents(20)) ky = Fix.Cents(20);
                if (wasAlive) Emit(SimEventType.KO, source != null ? source.Id : -1, victim.Id, ownerSlot, victim.Pos);
            }
            victim.Vel = new FixVec3(hit.Knockback.X * dirSign, ky, hit.Knockback.Z);

            if (airborne || ky.Raw > 0)
            {
                victim.GroundPlatform = -1;
                SetMode(victim, FighterMode.Juggle, 0);
                if ((hit.Flags & MoveFlags.Launcher) != 0) Emit(SimEventType.Launch, source != null ? source.Id : -1, victim.Id, 0, victim.Pos);
            }
            else
            {
                SetMode(victim, FighterMode.Hitstun, phaseStrike ? hit.Hitstun * 3 / 2 : hit.Hitstun);
            }

            // Air-combo hang: an airborne attacker that connects stops falling for a moment.
            if (source != null && sourceHitstop && !source.IsGrounded && source.Vel.Y < Fix.Cents(4))
                source.Vel = source.Vel.WithY(Fix.Cents(4));
            return true;
        }

        static byte BounceBits(MoveFlags flags)
        {
            byte b = 0;
            if ((flags & MoveFlags.WallBounce) != 0) b |= Fighter.BounceWall;
            if ((flags & MoveFlags.GroundBounce) != 0) b |= Fighter.BounceGround;
            if ((flags & MoveFlags.CeilingBounce) != 0) b |= Fighter.BounceCeiling;
            return b;
        }

        static bool CanBlock(Fighter victim, int dirSign, in HitSpec hit)
        {
            if ((hit.Flags & MoveFlags.Unblockable) != 0) return false;
            if (!victim.IsGrounded || !victim.Input.Has(Buttons.Block)) return false;
            var m = victim.Mode;
            if (m != FighterMode.Idle && m != FighterMode.Walk && m != FighterMode.Guard && m != FighterMode.Blockstun) return false;
            return victim.Facing == -dirSign;
        }

        void Parry(Fighter source, Fighter victim)
        {
            victim.InvulnFrames = Math.Max(victim.InvulnFrames, 10);
            victim.Hitstop = 8;
            AddGauge(victim, 30);
            if (source != null && !source.IsAssist && source.Mode == FighterMode.Attack)
            {
                source.MoveIndex = -1;
                source.ArmorLeft = 0;
                source.Hitstop = 8;
                source.Vel = FixVec3.Zero;
                SetMode(source, FighterMode.Hitstun, SimConstants.ParryStaggerFrames);
            }
            AddStyle(victim.OwnerSlot, -2, MoveTier.Super, false);
            Emit(SimEventType.Parry, victim.Id, source != null ? source.Id : -1, 0, victim.Pos);
        }

        void AddGauge(Fighter f, int amount)
        {
            if (f == null || f.IsAssist || amount <= 0) return;
            f.Gauge = Math.Min(SimConstants.MaxGauge, f.Gauge + amount);
        }

        /// <summary>Style rewards variety: repeating a move from the last four earns a third.</summary>
        void AddStyle(int slot, int key, MoveTier tier, bool environmental)
        {
            if (slot < 0 || slot >= Players.Length || Players[slot] == null) return;
            var p = Players[slot];
            int gain = 8 + (int)tier * 6;
            for (int i = 0; i < p.RecentMoves.Length; i++)
            {
                if (p.RecentMoves[i] != key) continue;
                gain /= 3;
                break;
            }
            p.RecentMoves[p.RecentCursor] = key;
            p.RecentCursor = (p.RecentCursor + 1) % p.RecentMoves.Length;
            if (environmental) gain += 25;

            var before = p.Rank;
            p.StylePoints = Math.Min(PlayerState.MaxStyle, p.StylePoints + gain);
            p.StyleIdleFrames = 0;
            if (p.Rank > before) Emit(SimEventType.StyleRankUp, slot, (int)p.Rank, p.StylePoints, FixVec3.Zero);
        }

        void UpdateCombos()
        {
            for (int i = 0; i < Fighters.Count; i++)
            {
                var f = Fighters[i];
                if (f.ComboHits == 0 || f.Hitstop > 0) continue;
                if (f.Mode == FighterMode.Hitstun || f.Mode == FighterMode.Juggle || f.Mode == FighterMode.Knockdown) continue;

                int slot = f.ComboOwnerSlot;
                if (slot >= 0 && Players[slot] != null)
                {
                    var p = Players[slot];
                    p.BestCombo = Math.Max(p.BestCombo, f.ComboHits);
                    p.Score += f.ComboDamage * (1 + (int)p.Rank) + f.ComboHits * 10;
                }
                Emit(SimEventType.ComboEnded, f.Id, slot, f.ComboHits, f.Pos);
                f.ResetCombo();
            }
        }

        // ------------------------------------------------------------------ player-level commands

        void HandlePlayerCommands(Fighter f)
        {
            var p = Players[f.PlayerSlot];
            if (p == null) return;
            bool mod = f.Input.Has(Buttons.HeroMod);

            if (ConsumeIfBuffered(f, Buttons.Ping))
                Emit(SimEventType.Ping, f.Id, f.PlayerSlot, (int)StickDirOf(f), f.Pos);

            if (!mod && AssembleMeter >= SimConstants.AssembleMeterMax && ConsumeIfBuffered(f, Buttons.Ultimate))
            {
                p.AssembleVoteFrame = Frame;
                Emit(SimEventType.AssembleVote, f.Id, f.PlayerSlot, 0, f.Pos);
                TryTeamUltimate();
            }

            if (!mod && ConsumeIfBuffered(f, Buttons.Assist)) CallAssistOrRelay(f, p);
        }

        /// <summary>
        /// LT: if this player's juggled victim is near a teammate, hand the combo off (Combo Relay).
        /// Otherwise call the assist partner. Stick direction picks one of three assist moves.
        /// </summary>
        void CallAssistOrRelay(Fighter f, PlayerState p)
        {
            if (p.AssistCooldown > 0)
            {
                Emit(SimEventType.AssistNotReady, f.Id, f.PlayerSlot, p.AssistCooldown, f.Pos);
                return;
            }

            var victim = f.LastVictimId >= 0 ? FindFighter(f.LastVictimId) : null;
            if (victim != null && victim.Mode == FighterMode.Juggle && victim.ComboOwnerSlot == f.PlayerSlot)
            {
                Fighter partner = null;
                for (int i = 0; i < Fighters.Count; i++)
                {
                    var o = Fighters[i];
                    if (o == f || !o.IsPlayer || o.Team != f.Team || !o.IsAlive || o.IsStunned) continue;
                    if (Fix.Abs(o.Pos.X - victim.Pos.X) > SimConstants.RelayRange || Fix.Abs(o.Pos.Z - victim.Pos.Z) > 2) continue;
                    partner = o;
                    break;
                }
                if (partner != null)
                {
                    victim.JugglePoints = victim.JugglePoints * 6 / 10;
                    int toward = partner.Pos.X >= victim.Pos.X ? 1 : -1;
                    victim.Vel = new FixVec3(Fix.Cents(6) * toward, Fix.Max(victim.Vel.Y, Fix.Cents(18)), victim.Vel.Z);
                    p.AssistCooldown = SimConstants.RelayCooldownFrames;
                    AddStyle(f.PlayerSlot, -3, MoveTier.HeroSpecial, false);
                    Emit(SimEventType.ComboRelay, f.Id, partner.Id, victim.ComboHits, victim.Pos);
                    return;
                }
            }

            if (p.AssistHeroIndex < 0) return;
            var assistHero = Content.Heroes[p.AssistHeroIndex];
            if (assistHero.AssistMoveIndices.Length == 0) return;
            var dir = StickDirOf(f);
            int variant = dir == StickDir.Down ? 2 : dir == StickDir.Forward || dir == StickDir.Back ? 1 : 0;
            variant = Math.Min(variant, assistHero.AssistMoveIndices.Length - 1);

            var spawnX = Fix.Clamp(f.Pos.X - Fix.FromInt(2) * f.Facing, Stage.MinX + 1, Stage.MaxX - 1);
            var a = CreateFighter(p.AssistHeroIndex, new FixVec3(spawnX, f.Pos.Y, f.Pos.Z), f.Team);
            a.IsAssist = true;
            a.OwnerSlot = f.OwnerSlot;
            a.Facing = f.Facing;
            a.InvulnFrames = int.MaxValue / 2;
            a.Gauge = SimConstants.MaxGauge;
            StartMove(a, assistHero, assistHero.AssistMoveIndices[variant]);
            p.AssistCooldown = SimConstants.AssistCooldownFrames;
            Emit(SimEventType.AssistCalled, f.Id, a.Id, variant, a.Pos);
        }

        void TryTeamUltimate()
        {
            if (AssembleMeter < SimConstants.AssembleMeterMax) return;
            int heroMask = 0, firstSlot = -1;
            for (int i = 0; i < Players.Length; i++)
            {
                var p = Players[i];
                if (p == null) continue;
                var pf = FindFighter(p.FighterId);
                if (pf == null || !pf.IsAlive) continue;
                if (Frame - p.AssembleVoteFrame > SimConstants.AssembleVoteWindow) return;
                heroMask |= 1 << p.HeroIndex;
                if (firstSlot < 0) firstSlot = i;
            }
            if (firstSlot < 0) return;

            AssembleMeter = 0;
            Fix sumX = Fix.Zero;
            int heroes = 0;
            for (int i = 0; i < Players.Length; i++)
            {
                var p = Players[i];
                var pf = p == null ? null : FindFighter(p.FighterId);
                if (pf == null || !pf.IsAlive) continue;
                p.AssembleVoteFrame = Fighter.NeverPressed;
                pf.InvulnFrames = Math.Max(pf.InvulnFrames, 120);
                sumX += pf.Pos.X;
                heroes++;
            }
            var centerX = sumX / heroes;
            Emit(SimEventType.TeamUltimate, firstSlot, heroMask, heroes, FixVec3.Zero);
            for (int i = 0; i < Fighters.Count; i++)
            {
                var e = Fighters[i];
                if (e.Team == 0 || !e.IsAlive || e.IsAssist) continue;
                ApplyHit(null, TeamUltimateHit, e, e.Pos.X >= centerX ? 1 : -1, false, firstSlot, false);
            }
        }
    }
}
