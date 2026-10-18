using System;

namespace Reforged.Sim
{
    // Fighter state machine, command resolution and movement.
    public sealed partial class SimWorld
    {
        void UpdateFighter(Fighter f)
        {
            if (!f.IsAlive) return;
            if (f.Hitstop > 0) { f.Hitstop--; return; }

            var hero = Content.Heroes[f.HeroIndex];
            f.ModeFrame++;
            if (f.InvulnFrames > 0) f.InvulnFrames--;
            if (f.DropThroughFrames > 0 && --f.DropThroughFrames == 0) f.DropThroughPlatform = -1;
            if (f.BuffFrames > 0)
            {
                f.BuffFrames--;
                if ((Frame & 3) == 0) AddGauge(f, 1);
            }
            if (f.ArmorLeft < hero.Armor && --f.ArmorRegenTimer <= 0) f.ArmorLeft = hero.Armor;
            TickMechanic(f, hero);

            if (f.IsPlayer && !f.IsStunned)
            {
                HandlePlayerCommands(f);
                HandleMechanicInput(f, hero);
            }

            switch (f.Mode)
            {
                case FighterMode.Hitstun:
                case FighterMode.Blockstun:
                case FighterMode.Recover:
                    if (--f.Timer <= 0) ReturnToNeutral(f);
                    break;
                case FighterMode.Evade:
                    if (--f.Timer <= 0)
                    {
                        f.Vel = new FixVec3(Fix.Zero, f.Vel.Y, Fix.Zero);
                        ReturnToNeutral(f);
                    }
                    break;
                case FighterMode.Taunt:
                    if (--f.Timer <= 0)
                    {
                        AddGauge(f, 10);
                        AddStyle(f.OwnerSlot, -100, MoveTier.Special, false);
                        Emit(SimEventType.Taunt, f.Id, f.PlayerSlot, 0, f.Pos);
                        ReturnToNeutral(f);
                    }
                    break;
                case FighterMode.Knockdown:
                    if (--f.Timer <= 0) GetUp(f);
                    break;
                case FighterMode.Attack:
                    UpdateAttack(f, hero);
                    break;
                case FighterMode.Juggle:
                    break;
                default:
                    UpdateNeutral(f, hero);
                    break;
            }

            if (f.IsAlive) Integrate(f, hero);
        }

        void SetMode(Fighter f, FighterMode mode, int timer)
        {
            f.Mode = mode;
            f.Timer = timer;
            f.ModeFrame = 0;
        }

        void ReturnToNeutral(Fighter f)
        {
            f.MoveIndex = -1;
            SetMode(f, f.IsGrounded ? FighterMode.Idle : FighterMode.Airborne, 0);
        }

        void GetUp(Fighter f)
        {
            if (f.Health <= 0)
            {
                SetMode(f, FighterMode.Dead, 0);
                return;
            }
            ReturnToNeutral(f);
            f.InvulnFrames = SimConstants.GetupInvulnFrames;
        }

        // ------------------------------------------------------------------ neutral & commands

        void UpdateNeutral(Fighter f, HeroDef hero)
        {
            if (f.HeldDebrisId >= 0 && TryThrowDebris(f)) return;
            if (TryStartCommand(f, hero, false)) return;

            if (f.IsGrounded)
            {
                if (f.Input.Has(Buttons.Block))
                {
                    if (f.Mode != FighterMode.Guard) SetMode(f, FighterMode.Guard, 0);
                    f.Vel = new FixVec3(Fix.Zero, f.Vel.Y, Fix.Zero);
                    return;
                }
                if (ConsumeIfBuffered(f, Buttons.Jump))
                {
                    var platform = Stage.Platforms[f.GroundPlatform];
                    bool canDrop = !platform.Solid || (IsPhased(f) && platform.Top > LowestPlatformTop());
                    if (StickDirOf(f) == StickDir.Down && canDrop) DropThrough(f);
                    else Jump(f, hero, false);
                    return;
                }
                if (EvadeFlicked(f)) { StartEvade(f); return; }
                if (ConsumeIfBuffered(f, Buttons.ToolUp))
                {
                    f.Vel = new FixVec3(Fix.Zero, f.Vel.Y, Fix.Zero);
                    SetMode(f, FighterMode.Taunt, SimConstants.TauntFrames);
                    return;
                }
                HandleTools(f, hero);
                Walk(f, hero);
            }
            else
            {
                if (f.AirJumpsUsed < AirJumpsFor(f, hero) && ConsumeIfBuffered(f, Buttons.Jump))
                {
                    f.AirJumpsUsed++;
                    Jump(f, hero, false);
                    return;
                }
                HandleTools(f, hero);
                var target = hero.WalkSpeed * f.Input.MoveX * 4 / (127 * 5);
                var step = Fix.Cents(1);
                var vx = f.Vel.X;
                vx = vx < target ? Fix.Min(vx + step, target) : Fix.Max(vx - step, target);
                f.Vel = f.Vel.WithX(vx);
                if (f.Mode != FighterMode.Airborne) SetMode(f, FighterMode.Airborne, 0);
            }
        }

        void Walk(Fighter f, HeroDef hero)
        {
            int mx = Math.Abs((int)f.Input.MoveX) < InputFrame.StickDeadzone ? 0 : f.Input.MoveX;
            int mz = Math.Abs((int)f.Input.MoveZ) < InputFrame.StickDeadzone ? 0 : f.Input.MoveZ;
            if (mx != 0) f.Facing = (sbyte)Math.Sign(mx);
            int pct = SpeedPercent(f);
            f.Vel = new FixVec3(hero.WalkSpeed * mx / 127 * pct / 100, f.Vel.Y, hero.DepthSpeed * mz / 127 * pct / 100);
            var mode = mx != 0 || mz != 0 ? FighterMode.Walk : FighterMode.Idle;
            if (f.Mode != mode) SetMode(f, mode, 0);
        }

        void Jump(Fighter f, HeroDef hero, bool superJump)
        {
            var vy = superJump ? hero.JumpVelocity * Fix.Ratio(23, 20) : hero.JumpVelocity;
            f.Vel = f.Vel.WithY(vy);
            f.GroundPlatform = -1;
            f.MoveIndex = -1;
            SetMode(f, FighterMode.Airborne, 0);
        }

        void DropThrough(Fighter f)
        {
            f.DropThroughPlatform = f.GroundPlatform;
            f.DropThroughFrames = SimConstants.DropThroughFrames;
            f.GroundPlatform = -1;
            SetMode(f, FighterMode.Airborne, 0);
        }

        void StartEvade(Fighter f)
        {
            f.MoveIndex = -1;
            f.InvulnFrames = Math.Max(f.InvulnFrames, SimConstants.EvadeInvulnFrames);
            var vx = f.EvadeDirZ != 0 ? Fix.Zero : Fix.Cents(16) * f.EvadeDirX;
            var vz = Fix.Cents(12) * f.EvadeDirZ;
            f.Vel = new FixVec3(vx, f.Vel.Y, vz);
            SetMode(f, FighterMode.Evade, SimConstants.EvadeFrames);
        }

        void HandleTools(Fighter f, HeroDef hero)
        {
            if (hero.ToolCycleRange > 0)
            {
                if (ConsumeIfBuffered(f, Buttons.ToolLeft))
                {
                    f.ToolState = Math.Max(-hero.ToolCycleRange, f.ToolState - 1);
                    Emit(SimEventType.ToolUsed, f.Id, 0, f.ToolState, f.Pos);
                }
                if (ConsumeIfBuffered(f, Buttons.ToolRight))
                {
                    f.ToolState = Math.Min(hero.ToolCycleRange, f.ToolState + 1);
                    Emit(SimEventType.ToolUsed, f.Id, 0, f.ToolState, f.Pos);
                }
            }
            if (hero.ToolChargeCost > 0 && f.ToolCharge == 0 && f.Gauge >= hero.ToolChargeCost && ConsumeIfBuffered(f, Buttons.ToolDown))
            {
                f.Gauge -= hero.ToolChargeCost;
                f.ToolCharge = 1;
                Emit(SimEventType.ToolUsed, f.Id, 1, f.ToolCharge, f.Pos);
            }
        }

        /// <summary>
        /// Resolves buffered buttons into a move, highest priority first. When cancelling, the
        /// target must satisfy the cancel hierarchy (or be the next hit of a light string).
        /// </summary>
        bool TryStartCommand(Fighter f, HeroDef hero, bool cancelling)
        {
            bool aerial = !f.IsGrounded;
            bool mod = f.Input.Has(Buttons.HeroMod);
            var dir = StickDirOf(f);

            if (Buffered(f, Buttons.Ultimate) &&
                TryInput(f, hero, mod ? MoveInput.UltimateII : MoveInput.UltimateI, Buttons.Ultimate, dir, aerial, cancelling))
                return true;

            if (mod)
            {
                if (Buffered(f, Buttons.Assist) && TryInput(f, hero, MoveInput.Super, Buttons.Assist, dir, aerial, cancelling)) return true;
                if (Buffered(f, Buttons.Light) && TryInput(f, hero, MoveInput.HeroLight, Buttons.Light, dir, aerial, cancelling)) return true;
                if (Buffered(f, Buttons.Heavy) && TryInput(f, hero, MoveInput.HeroHeavy, Buttons.Heavy, dir, aerial, cancelling)) return true;
                if (Buffered(f, Buttons.Jump) && TryInput(f, hero, MoveInput.HeroJump, Buttons.Jump, dir, aerial, cancelling)) return true;
                if (Buffered(f, Buttons.Special) && TryInput(f, hero, MoveInput.HeroSpecial, Buttons.Special, dir, aerial, cancelling)) return true;
                return false;
            }

            if (Buffered(f, Buttons.Special) && TryInput(f, hero, MoveInput.Special, Buttons.Special, dir, aerial, cancelling)) return true;
            if (Buffered(f, Buttons.Heavy) && TryInput(f, hero, MoveInput.Heavy, Buttons.Heavy, dir, aerial, cancelling)) return true;
            if (Buffered(f, Buttons.Light) && TryInput(f, hero, MoveInput.Light, Buttons.Light, dir, aerial, cancelling)) return true;
            if (Buffered(f, Buttons.Grab))
            {
                if (!cancelling && !aerial && f.HeldDebrisId < 0 && TryPickupDebris(f))
                {
                    Consume(f, Buttons.Grab);
                    return true;
                }
                if (TryInput(f, hero, MoveInput.Grab, Buttons.Grab, dir, aerial, cancelling)) return true;
            }
            return false;
        }

        bool TryInput(Fighter f, HeroDef hero, MoveInput input, Buttons button, StickDir dir, bool aerial, bool cancelling)
        {
            int idx = -1;
            if (input == MoveInput.Light && f.Mode == FighterMode.Attack && f.MoveIndex >= 0)
            {
                var cur = hero.Moves[f.MoveIndex];
                if (cur.ChainNextIndex >= 0 && hero.Moves[cur.ChainNextIndex].Aerial == aerial) idx = cur.ChainNextIndex;
            }
            if (idx < 0) idx = hero.FindMove(input, dir, aerial, MechanicTier(f));
            if (idx < 0) return false;
            if (cancelling && !CanCancelInto(f, hero, hero.Moves[idx])) return false;
            if (!StartMove(f, hero, idx)) return false;
            Consume(f, button);
            return true;
        }

        static bool CanCancelInto(Fighter f, HeroDef hero, MoveDef target)
        {
            var cur = hero.Moves[f.MoveIndex];
            if (cur.ChainNextIndex == target.Index && f.MoveFrame >= cur.ActiveStart) return true;
            if (!f.MoveHasHit) return false;
            if (f.MoveFrame < cur.CancelStart || f.MoveFrame > cur.CancelEnd) return false;
            return target.Tier > cur.Tier;
        }

        bool StartMove(Fighter f, HeroDef hero, int idx)
        {
            var m = hero.Moves[idx];
            if (f.Gauge < m.GaugeCost) return false;
            if (m.Projectile != null && HasActiveProjectile(f.Id)) return false;
            if (!MechanicAllowsMove(f, m)) return false;

            f.Gauge -= m.GaugeCost;
            DropHeldDebris(f);
            if (f.IsGrounded && f.Mode != FighterMode.Attack && Math.Abs((int)f.Input.MoveX) >= InputFrame.StickCommandThreshold)
                f.Facing = (sbyte)Math.Sign(f.Input.MoveX);
            if (f.IsGrounded) f.Vel = new FixVec3(Fix.Zero, f.Vel.Y, Fix.Zero);

            f.MoveIndex = (short)idx;
            f.MoveFrame = 0;
            f.MoveHasHit = false;
            f.HitListCount = 0;
            SetMode(f, FighterMode.Attack, 0);
            OnMechanicMoveStarted(f, m);
            if (m.Has(MoveFlags.Invulnerable)) f.InvulnFrames = Math.Max(f.InvulnFrames, m.ActiveEnd);

            Emit(SimEventType.MoveStarted, f.Id, idx, (int)m.Tier, f.Pos);
            if (m.Tier == MoveTier.Super) Emit(SimEventType.SuperStarted, f.Id, idx, f.OwnerSlot, f.Pos);
            if (m.Tier == MoveTier.Ultimate)
            {
                Emit(SimEventType.UltimateStarted, f.Id, idx, m.Input == MoveInput.UltimateII ? 2 : 1, f.Pos);
                // Cinematic freeze: everyone on the other side holds still while the ultimate winds up.
                for (int i = 0; i < Fighters.Count; i++)
                {
                    var o = Fighters[i];
                    if (o.Team != f.Team && o.IsAlive) o.Hitstop = Math.Max(o.Hitstop, m.Startup);
                }
            }
            return true;
        }

        void UpdateAttack(Fighter f, HeroDef hero)
        {
            var m = hero.Moves[f.MoveIndex];
            if (TryStartCommand(f, hero, true)) return;

            if (f.MoveHasHit && f.MoveFrame >= m.CancelStart)
            {
                bool freeJump = m.Has(MoveFlags.Launcher);
                if ((freeJump || f.Gauge >= SimConstants.CancelGaugeCost) && ConsumeIfBuffered(f, Buttons.Jump))
                {
                    if (!freeJump) f.Gauge -= SimConstants.CancelGaugeCost;
                    Jump(f, hero, freeJump);
                    Emit(SimEventType.JumpCancel, f.Id, m.Index, 0, f.Pos);
                    return;
                }
                if (f.IsGrounded && f.Gauge >= SimConstants.CancelGaugeCost && EvadeFlicked(f))
                {
                    f.Gauge -= SimConstants.CancelGaugeCost;
                    StartEvade(f);
                    Emit(SimEventType.LaneCancel, f.Id, m.Index, 0, f.Pos);
                    return;
                }
            }

            f.MoveFrame++;
            if (f.MoveFrame == m.ActiveStart) OnActiveStart(f, m);
            if (m.MaxHits > 1 && m.RehitInterval > 0 && f.MoveFrame > m.ActiveStart && f.MoveFrame <= m.ActiveEnd &&
                (f.MoveFrame - m.ActiveStart) % m.RehitInterval == 0)
                f.HitListCount = 0;
            if (f.MoveFrame >= m.TotalFrames) EndMove(f);
        }

        void OnActiveStart(Fighter f, MoveDef m)
        {
            if (m.SelfVelocity != FixVec3.Zero)
            {
                var vy = m.SelfVelocity.Y.Raw != 0 ? m.SelfVelocity.Y : f.Vel.Y;
                f.Vel = new FixVec3(m.SelfVelocity.X * f.Facing, vy, m.SelfVelocity.Z);
                if (vy.Raw > 0) f.GroundPlatform = -1;
            }
            if (m.Projectile != null) SpawnProjectile(f, m);
            if (m.Has(MoveFlags.LiftDebris)) LiftDebrisAround(f);
            if (m.TeamBuffFrames > 0)
            {
                for (int i = 0; i < Fighters.Count; i++)
                {
                    var o = Fighters[i];
                    if (o.Team == f.Team && o.IsAlive && !o.IsAssist) o.BuffFrames = Math.Max(o.BuffFrames, m.TeamBuffFrames);
                }
                Emit(SimEventType.TeamBuff, f.Id, f.Team, m.TeamBuffFrames, f.Pos);
            }
        }

        void EndMove(Fighter f)
        {
            f.PhaseStrike = false;
            if (f.IsAssist)
            {
                f.Mode = FighterMode.Despawned;
                return;
            }
            ReturnToNeutral(f);
        }

        // ------------------------------------------------------------------ movement

        public Fix EffectiveGravity(Fighter f)
        {
            var hero = Content.Heroes[f.HeroIndex];
            var g = hero.Gravity;
            if (IsHovering(f)) return g / 5;
            if (f.Mode == FighterMode.Juggle)
                return g * (100 + f.JugglePoints * SimConstants.JuggleGravityPercentPerPoint) / 100;
            if (f.Mode == FighterMode.Attack && f.MoveIndex >= 0)
            {
                var m = hero.Moves[f.MoveIndex];
                if (m.Aerial && f.MoveFrame >= m.ActiveStart && f.MoveFrame <= m.ActiveEnd) return g / 2;
            }
            return g;
        }

        void Integrate(Fighter f, HeroDef hero)
        {
            if (f.IsGrounded)
            {
                if (f.Vel.Y.Raw > 0)
                {
                    f.GroundPlatform = -1;
                }
                else if (f.Mode != FighterMode.Walk && f.Mode != FighterMode.Evade)
                {
                    var fr = SimConstants.GroundFriction;
                    var snap = SimConstants.VelocitySnap;
                    f.Vel = new FixVec3(Fix.Damp(f.Vel.X, fr, snap), Fix.Zero, Fix.Damp(f.Vel.Z, fr, snap));
                }
            }
            if (!f.IsGrounded)
            {
                var vy = f.Vel.Y - EffectiveGravity(f);
                if (vy < -SimConstants.MaxFallSpeed) vy = -SimConstants.MaxFallSpeed;
                f.Vel = f.Vel.WithY(vy);
            }

            var prev = f.Pos;
            var next = prev + f.Vel;
            next = ResolveWalls(f, hero, prev, next);
            next = next.WithZ(Fix.Clamp(next.Z, Stage.MinZ, Stage.MaxZ));

            if (f.IsGrounded)
            {
                var p = Stage.Platforms[f.GroundPlatform];
                if (!IsPlatformEnabled(f.GroundPlatform) || !p.Contains(next.X, next.Z))
                {
                    f.GroundPlatform = -1;
                    OnLeftGround(f);
                }
                else
                {
                    next = next.WithY(p.Top);
                }
            }
            else if (f.Vel.Y.Raw <= 0)
            {
                int land = FindLanding(prev, next, f.DropThroughPlatform);
                if (land >= 0)
                {
                    f.Pos = next.WithY(Stage.Platforms[land].Top);
                    Land(f, hero, land);
                    return;
                }
            }
            else if (f.Mode == FighterMode.Juggle)
            {
                next = ResolveCeiling(f, hero, prev, next);
            }
            f.Pos = next;
        }

        void OnLeftGround(Fighter f)
        {
            switch (f.Mode)
            {
                case FighterMode.Idle:
                case FighterMode.Walk:
                case FighterMode.Guard:
                case FighterMode.Taunt:
                case FighterMode.Recover:
                    SetMode(f, FighterMode.Airborne, 0);
                    break;
                case FighterMode.Hitstun:
                case FighterMode.Blockstun:
                case FighterMode.Knockdown:
                    SetMode(f, FighterMode.Juggle, 0);
                    break;
            }
        }

        bool IsPlatformEnabled(int index)
        {
            int d = Stage.Platforms[index].Destructible;
            return d < 0 || !Destructibles[d].Broken;
        }

        bool IsWallEnabled(int index)
        {
            int d = Stage.Walls[index].Destructible;
            return d < 0 || !Destructibles[d].Broken;
        }

        int FindSupportPlatform(FixVec3 pos)
        {
            for (int i = 0; i < Stage.Platforms.Count; i++)
            {
                var p = Stage.Platforms[i];
                if (p.Top == pos.Y && p.Contains(pos.X, pos.Z) && IsPlatformEnabled(i)) return i;
            }
            return -1;
        }

        /// <summary>Highest enabled platform crossed from above between prev and next.</summary>
        int FindLanding(FixVec3 prev, FixVec3 next, int ignore)
        {
            int best = -1;
            for (int i = 0; i < Stage.Platforms.Count; i++)
            {
                if (i == ignore || !IsPlatformEnabled(i)) continue;
                var p = Stage.Platforms[i];
                if (!p.Contains(next.X, next.Z)) continue;
                if (prev.Y >= p.Top && next.Y <= p.Top && (best < 0 || p.Top > Stage.Platforms[best].Top)) best = i;
            }
            return best;
        }

        void Land(Fighter f, HeroDef hero, int platform)
        {
            f.GroundPlatform = (short)platform;
            f.AirJumpsUsed = 0;
            f.Vel = f.Vel.WithY(Fix.Zero);

            switch (f.Mode)
            {
                case FighterMode.Juggle:
                    if ((f.PendingBounce & Fighter.BounceGround) != 0)
                    {
                        f.PendingBounce &= unchecked((byte)~Fighter.BounceGround);
                        f.BouncesUsed |= Fighter.BounceGround;
                        f.GroundPlatform = -1;
                        f.Vel = new FixVec3(f.Vel.X / 2, Fix.Cents(22), f.Vel.Z);
                        Emit(SimEventType.GroundBounce, f.Id, f.ComboOwnerSlot, f.ComboHits, f.Pos);
                        return;
                    }
                    f.JuggleExhausted = false;
                    f.Vel = new FixVec3(f.Vel.X / 2, Fix.Zero, Fix.Zero);
                    SetMode(f, FighterMode.Knockdown, SimConstants.KnockdownFrames);
                    Emit(SimEventType.Knockdown, f.Id, f.ComboOwnerSlot, f.ComboHits, f.Pos);
                    break;

                case FighterMode.Attack:
                    var m = hero.Moves[f.MoveIndex];
                    // Diamond-dense Vision breaks floors with any aerial attack.
                    bool floorBreak = m.Has(MoveFlags.FloorBreak) || (IsDiamond(f) && m.Aerial);
                    if (floorBreak && f.MoveFrame >= m.ActiveStart)
                    {
                        int d = Stage.Platforms[platform].Destructible;
                        if (d >= 0 && DamageDestructible(d, DestructionPowerOf(f, m), f.OwnerSlot))
                        {
                            // The floor gave way: keep the dive going down to the level below.
                            f.GroundPlatform = -1;
                            return;
                        }
                    }
                    if (m.Aerial || m.SelfVelocity.Y.Raw > 0)
                    {
                        f.MoveIndex = -1;
                        SetMode(f, FighterMode.Recover, m.LandingLag);
                    }
                    break;

                case FighterMode.Airborne:
                    SetMode(f, FighterMode.Idle, 0);
                    break;
            }
        }

        FixVec3 ResolveWalls(Fighter f, HeroDef hero, FixVec3 prev, FixVec3 next)
        {
            var half = hero.BodyHalfExtents.X;
            if (next.X - half < Stage.MinX) return OnWall(f, -1, Stage.MinX + half, next);
            if (next.X + half > Stage.MaxX) return OnWall(f, -1, Stage.MaxX - half, next);
            var height = hero.Height;
            for (int i = 0; i < Stage.Walls.Count && !IsPhased(f); i++)
            {
                if (!IsWallEnabled(i)) continue;
                var w = Stage.Walls[i];
                if (next.Y >= w.MaxY || next.Y + height <= w.MinY) continue;
                if (prev.X + half <= w.X && next.X + half > w.X) return OnWall(f, i, w.X - half, next);
                if (prev.X - half >= w.X && next.X - half < w.X) return OnWall(f, i, w.X + half, next);
            }
            return next;
        }

        FixVec3 OnWall(Fighter f, int wallIndex, Fix clampX, FixVec3 next)
        {
            bool launched = (f.Mode == FighterMode.Juggle || f.Mode == FighterMode.Hitstun) && (f.PendingBounce & Fighter.BounceWall) != 0;
            if (launched)
            {
                f.PendingBounce &= unchecked((byte)~Fighter.BounceWall);
                f.BouncesUsed |= Fighter.BounceWall;
                int d = wallIndex >= 0 ? Stage.Walls[wallIndex].Destructible : -1;
                if (d >= 0)
                {
                    // Smashed straight through: the combo continues in the next room.
                    DamageDestructible(d, Destructibles[d].Health, f.ComboOwnerSlot);
                    f.Vel = f.Vel.WithX(f.Vel.X * Fix.Ratio(3, 4));
                    Emit(SimEventType.WallBreak, f.Id, wallIndex, f.ComboHits, next);
                    AddStyle(f.ComboOwnerSlot, -200, MoveTier.Super, true);
                    return next;
                }
                f.Vel = new FixVec3(-f.Vel.X / 2, Fix.Cents(18), f.Vel.Z);
                f.GroundPlatform = -1;
                if (f.Mode != FighterMode.Juggle) SetMode(f, FighterMode.Juggle, 0);
                Emit(SimEventType.WallBounce, f.Id, wallIndex, f.ComboHits, next);
                return next.WithX(clampX);
            }
            f.Vel = f.Vel.WithX(Fix.Zero);
            return next.WithX(clampX);
        }

        /// <summary>
        /// Juggled fighters bonk on the underside of floors, unless the hit carried CeilingBounce:
        /// then they smash up through it and pop out on the level above.
        /// </summary>
        FixVec3 ResolveCeiling(Fighter f, HeroDef hero, FixVec3 prev, FixVec3 next)
        {
            var height = hero.Height;
            for (int i = 0; i < Stage.Platforms.Count; i++)
            {
                if (!IsPlatformEnabled(i)) continue;
                var p = Stage.Platforms[i];
                if (!p.Contains(next.X, next.Z)) continue;
                var bottom = p.Top - SimConstants.PlatformThickness;
                if (prev.Y + height > bottom || next.Y + height <= bottom) continue;

                if ((f.PendingBounce & Fighter.BounceCeiling) != 0)
                {
                    f.PendingBounce &= unchecked((byte)~Fighter.BounceCeiling);
                    f.BouncesUsed |= Fighter.BounceCeiling;
                    f.Vel = f.Vel.WithY(Fix.Cents(14));
                    Emit(SimEventType.CeilingBreakthrough, f.Id, i, f.ComboHits, next);
                    AddStyle(f.ComboOwnerSlot, -201, MoveTier.Super, true);
                    return next.WithY(p.Top + Fix.Cents(5));
                }
                f.Vel = f.Vel.WithY(Fix.Zero);
                return next.WithY(bottom - height);
            }
            return next;
        }
    }
}
