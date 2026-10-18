using System;

namespace Reforged.Sim
{
    // Destruction, interactive debris and projectiles.
    public sealed partial class SimWorld
    {
        static HitSpec DebrisHit(byte weight) => new HitSpec(
            35 * weight, 22, 6, 3, 10, -10 - weight,
            new FixVec3(Fix.Cents(12 + 4 * weight), Fix.Cents(16), Fix.Zero),
            weight >= 2 ? MoveFlags.GroundBounce : MoveFlags.WallBounce,
            MoveTier.Heavy);

        // ------------------------------------------------------------------ destruction

        /// <summary>Damages a destructible; returns true if this call broke it.</summary>
        bool DamageDestructible(int index, int amount, int slot)
        {
            var s = Destructibles[index];
            if (s.Broken || amount <= 0) return false;
            var def = Stage.Destructibles[index];
            s.Health -= amount;

            if (s.Health <= 0)
            {
                s.Health = 0;
                s.Broken = true;
                s.Stage = (byte)def.DamageStages;
                Emit(SimEventType.DestructibleBroken, index, slot, (int)def.Tier, def.Center);
                if (def.Tier == DestructionTier.Structural) Emit(SimEventType.FloorCollapse, index, slot, 0, def.Center);
                SpawnDebrisFrom(def);
                AddStyle(slot, -400 - index, MoveTier.Special, true);
                return true;
            }

            int stage = Math.Min(def.DamageStages - 1, (def.Health - s.Health) * def.DamageStages / def.Health);
            if (stage > s.Stage)
            {
                s.Stage = (byte)stage;
                Emit(SimEventType.DestructibleDamaged, index, slot, stage, def.Center);
            }
            return false;
        }

        void SpawnDebrisFrom(DestructibleDef def)
        {
            for (int k = 0; k < def.DebrisCount; k++)
            {
                var d = new Debris
                {
                    Id = nextEntityId++,
                    Pos = def.Center + new FixVec3(Fix.Cents(Rng.Range(-30, 31)), Fix.Zero, Fix.Zero),
                    Vel = new FixVec3(Fix.Cents(Rng.Range(-6, 7)), Fix.Cents(Rng.Range(10, 20)), Fix.Zero),
                    Weight = def.DebrisWeight,
                    Mode = DebrisMode.Loose,
                    Active = true,
                };
                Debris.Add(d);
                Emit(SimEventType.DebrisSpawned, d.Id, d.Weight, 0, d.Pos);
            }
        }

        // ------------------------------------------------------------------ debris

        bool TryPickupDebris(Fighter f)
        {
            // An enemy in throw range takes priority over picking things up.
            for (int i = 0; i < Fighters.Count; i++)
            {
                var o = Fighters[i];
                if (!CanBeHit(o, f.Team)) continue;
                var dx = (o.Pos.X - f.Pos.X) * f.Facing;
                if (dx >= 0 && dx <= Fix.Cents(90) && Fix.Abs(o.Pos.Z - f.Pos.Z) <= Fix.Cents(40)) return false;
            }

            Debris best = null;
            Fix bestDx = Fix.Zero;
            for (int i = 0; i < Debris.Count; i++)
            {
                var d = Debris[i];
                if (!d.Active || (d.Mode != DebrisMode.Resting && d.Mode != DebrisMode.Loose)) continue;
                var dx = Fix.Abs(d.Pos.X - f.Pos.X);
                if (dx > Fix.Cents(120) || Fix.Abs(d.Pos.Z - f.Pos.Z) > Fix.Cents(60) || Fix.Abs(d.Pos.Y - f.Pos.Y) > Fix.Cents(50)) continue;
                if (best == null || dx < bestDx) { best = d; bestDx = dx; }
            }
            if (best == null) return false;

            best.Mode = DebrisMode.Held;
            best.HolderId = f.Id;
            best.SupportPlatform = -1;
            f.HeldDebrisId = best.Id;
            Emit(SimEventType.DebrisPickedUp, f.Id, best.Id, best.Weight, best.Pos);
            return true;
        }

        bool TryThrowDebris(Fighter f)
        {
            bool heavy = Buffered(f, Buttons.Heavy);
            if (!heavy && !Buffered(f, Buttons.Light) && !Buffered(f, Buttons.Grab)) return false;
            Consume(f, Buttons.Heavy); Consume(f, Buttons.Light); Consume(f, Buttons.Grab);

            var d = FindDebris(f.HeldDebrisId);
            f.HeldDebrisId = -1;
            if (d == null) return false;
            d.Mode = DebrisMode.Flying;
            d.HolderId = -1;
            d.ThrowerId = f.Id;
            d.ThrowerTeam = f.Team;
            d.ThrowerSlot = f.OwnerSlot;
            d.LastHitId = -1;
            d.Pos = f.Pos + new FixVec3(Fix.Cents(50) * f.Facing, Fix.Cents(160), Fix.Zero);
            d.Vel = new FixVec3(Fix.Cents(heavy ? 60 : 45) * f.Facing, Fix.Cents(heavy ? 14 : 6), Fix.Cents(5) * f.Input.MoveZ / 127);
            f.Vel = new FixVec3(Fix.Zero, f.Vel.Y, Fix.Zero);
            SetMode(f, FighterMode.Recover, SimConstants.ThrowRecoveryFrames);
            Emit(SimEventType.DebrisThrown, f.Id, d.Id, d.Weight, d.Pos);
            return true;
        }

        void DropHeldDebris(Fighter f)
        {
            if (f.HeldDebrisId < 0) return;
            var d = FindDebris(f.HeldDebrisId);
            f.HeldDebrisId = -1;
            if (d == null) return;
            d.Mode = DebrisMode.Loose;
            d.HolderId = -1;
        }

        /// <summary>Hitting debris launches it as a projectile owned by the hitter: combo extender.</summary>
        void BatDebris(Fighter a, MoveDef m, Debris d)
        {
            var kx = Fix.Max(Fix.Abs(m.Knockback.X), Fix.Cents(30)) * a.Facing;
            var ky = Fix.Max(m.Knockback.Y, Fix.Cents(8));
            d.Vel = new FixVec3(kx, ky, Fix.Zero);
            d.Mode = DebrisMode.Flying;
            d.SupportPlatform = -1;
            d.ThrowerId = a.Id;
            d.ThrowerTeam = a.Team;
            d.ThrowerSlot = a.OwnerSlot;
            d.LastHitId = -1;
            a.MoveHasHit = true;
            Emit(SimEventType.DebrisBatted, a.Id, d.Id, d.Weight, d.Pos);
        }

        void UpdateDebris()
        {
            for (int i = 0; i < Debris.Count; i++)
            {
                var d = Debris[i];
                if (!d.Active) continue;

                if (d.Mode == DebrisMode.Held)
                {
                    var h = FindFighter(d.HolderId);
                    if (h == null || h.HeldDebrisId != d.Id || !h.IsAlive)
                    {
                        d.Mode = DebrisMode.Loose;
                        d.HolderId = -1;
                    }
                    else
                    {
                        d.Pos = h.Pos + new FixVec3(Fix.Cents(30) * h.Facing, Fix.Cents(170), Fix.Zero);
                        d.Vel = h.Vel;
                        continue;
                    }
                }

                if (d.Mode == DebrisMode.Resting)
                {
                    if (d.SupportPlatform >= 0 && IsPlatformEnabled(d.SupportPlatform)) continue;
                    d.Mode = DebrisMode.Loose;
                    d.SupportPlatform = -1;
                }

                if (d.SupportPlatform >= 0 && !IsPlatformEnabled(d.SupportPlatform)) d.SupportPlatform = -1;

                if (d.SupportPlatform >= 0)
                {
                    var vx = Fix.Damp(d.Vel.X, Fix.Half, Fix.Cents(1));
                    d.Vel = new FixVec3(vx, Fix.Zero, Fix.Zero);
                    if (vx.Raw == 0) d.Mode = DebrisMode.Resting;
                }
                else
                {
                    var vy = Fix.Max(d.Vel.Y - SimConstants.DebrisGravity, -SimConstants.MaxFallSpeed);
                    d.Vel = d.Vel.WithY(vy);
                }

                var prev = d.Pos;
                var next = prev + d.Vel;
                if (next.X < Stage.MinX || next.X > Stage.MaxX)
                {
                    next = next.WithX(Fix.Clamp(next.X, Stage.MinX, Stage.MaxX));
                    d.Vel = d.Vel.WithX(-d.Vel.X / 2);
                }
                for (int w = 0; w < Stage.Walls.Count; w++)
                {
                    if (!IsWallEnabled(w)) continue;
                    var wall = Stage.Walls[w];
                    if (next.Y >= wall.MaxY || next.Y < wall.MinY) continue;
                    if ((prev.X <= wall.X && next.X > wall.X) || (prev.X >= wall.X && next.X < wall.X))
                    {
                        next = next.WithX(prev.X);
                        d.Vel = d.Vel.WithX(-d.Vel.X / 2);
                    }
                }
                next = next.WithZ(Fix.Clamp(next.Z, Stage.MinZ, Stage.MaxZ));

                if (d.SupportPlatform < 0 && d.Vel.Y.Raw <= 0)
                {
                    int land = FindLanding(prev, next, -1);
                    if (land >= 0)
                    {
                        next = next.WithY(Stage.Platforms[land].Top);
                        d.SupportPlatform = (short)land;
                        d.Vel = new FixVec3(d.Vel.X / 2, Fix.Zero, Fix.Zero);
                        if (d.Mode == DebrisMode.Flying) d.Mode = DebrisMode.Loose;
                    }
                }
                d.Pos = next;

                if (d.Mode == DebrisMode.Flying) DebrisCollisions(d);
            }
        }

        void DebrisCollisions(Debris d)
        {
            var center = d.Pos.WithY(d.Pos.Y + d.HalfExtents.Y);
            for (int i = 0; i < Fighters.Count; i++)
            {
                var t = Fighters[i];
                if (t.Id == d.LastHitId || !CanBeHit(t, d.ThrowerTeam)) continue;
                if (!Overlaps(center, d.HalfExtents, HurtCenter(t), HurtHalf(t))) continue;
                var thrower = FindFighter(d.ThrowerId);
                int dir = d.Vel.X.Raw >= 0 ? 1 : -1;
                ApplyHit(thrower, DebrisHit(d.Weight), t, dir, false, d.ThrowerSlot, true);
                d.LastHitId = t.Id;
                d.Vel = new FixVec3(-d.Vel.X / 3, Fix.Cents(12), Fix.Zero);
                return;
            }
            for (int i = 0; i < Destructibles.Length; i++)
            {
                int key = SimConstants.DestructibleKeyBase + i;
                if (Destructibles[i].Broken || d.LastHitId == key) continue;
                var def = Stage.Destructibles[i];
                if (!Overlaps(center, d.HalfExtents, def.Center, def.HalfExtents)) continue;
                DamageDestructible(i, 30 * d.Weight, d.ThrowerSlot);
                d.LastHitId = key;
                d.Vel = d.Vel.WithX(-d.Vel.X / 3);
                return;
            }
        }

        // ------------------------------------------------------------------ projectiles

        void SpawnProjectile(Fighter f, MoveDef m)
        {
            var s = m.Projectile;
            var p = new Projectile
            {
                Id = nextEntityId++,
                OwnerId = f.Id,
                OwnerSlot = f.OwnerSlot,
                Team = f.Team,
                HeroIndex = f.HeroIndex,
                MoveIndex = (short)m.Index,
                Pos = f.Pos + new FixVec3(s.SpawnOffset.X * f.Facing, s.SpawnOffset.Y, s.SpawnOffset.Z),
                Vel = new FixVec3(s.Speed * f.Facing, s.Rise + Fix.Cents(6) * f.ToolState, Fix.Zero),
                Speed = s.Speed,
                Life = s.Lifetime,
                RicochetsLeft = s.Ricochets + f.ToolCharge,
                Active = true,
            };
            f.ToolCharge = 0;
            Projectiles.Add(p);
            Emit(SimEventType.ProjectileSpawned, f.Id, p.Id, m.Index, p.Pos);
        }

        void UpdateProjectiles()
        {
            for (int i = 0; i < Projectiles.Count; i++)
            {
                var p = Projectiles[i];
                if (!p.Active) continue;
                var move = Content.Heroes[p.HeroIndex].Moves[p.MoveIndex];
                var spec = move.Projectile;
                var owner = FindFighter(p.OwnerId);

                if (p.Returning)
                {
                    UpdateReturningProjectile(p, owner);
                    continue;
                }

                p.Pos += p.Vel;
                p.Life--;
                if (p.Pos.X < Stage.MinX || p.Pos.X > Stage.MaxX)
                {
                    p.Pos = p.Pos.WithX(Fix.Clamp(p.Pos.X, Stage.MinX, Stage.MaxX));
                    p.Vel = p.Vel.WithX(-p.Vel.X);
                    Emit(SimEventType.Ricochet, p.Id, -1, 0, p.Pos);
                }
                if (p.Vel.Y.Raw < 0 && p.Pos.Y < Fix.Cents(20)) p.Vel = p.Vel.WithY(-p.Vel.Y);

                bool hit = false;
                for (int t = 0; t < Fighters.Count && !hit; t++)
                {
                    var target = Fighters[t];
                    if (target.Id == p.LastHitId || !CanBeHit(target, p.Team)) continue;
                    if (!Overlaps(p.Pos, spec.HalfExtents, HurtCenter(target), HurtHalf(target))) continue;
                    ApplyHit(owner, HitSpec.From(move, StyleKey(p.HeroIndex, p.MoveIndex)), target, p.Vel.X.Raw >= 0 ? 1 : -1, false, p.OwnerSlot, false);
                    p.LastHitId = target.Id;
                    hit = true;
                }
                for (int d = 0; d < Destructibles.Length && !hit && move.DestructionPower > 0; d++)
                {
                    int key = SimConstants.DestructibleKeyBase + d;
                    if (Destructibles[d].Broken || p.LastHitId == key) continue;
                    var def = Stage.Destructibles[d];
                    if (!Overlaps(p.Pos, spec.HalfExtents, def.Center, def.HalfExtents)) continue;
                    DamageDestructible(d, move.DestructionPower, p.OwnerSlot);
                    p.LastHitId = key;
                    hit = true;
                }

                if (hit)
                {
                    if (p.RicochetsLeft > 0)
                    {
                        var next = FindRicochetTarget(p);
                        if (next != null)
                        {
                            p.RicochetsLeft--;
                            p.Vel = (HurtCenter(next) - p.Pos).WithMaxNorm(p.Speed);
                            p.Life = spec.Lifetime;
                            Emit(SimEventType.Ricochet, p.Id, next.Id, p.RicochetsLeft, p.Pos);
                            continue;
                        }
                    }
                    if (spec.ReturnsToOwner) p.Returning = true;
                    else p.Active = false;
                    continue;
                }

                if (p.Life <= 0)
                {
                    if (spec.ReturnsToOwner) p.Returning = true;
                    else p.Active = false;
                }
            }
        }

        void UpdateReturningProjectile(Projectile p, Fighter owner)
        {
            if (owner == null || !owner.IsAlive || --p.Life < -180)
            {
                p.Active = false;
                return;
            }
            var delta = owner.Pos.WithY(owner.Pos.Y + Fix.Cents(110)) - p.Pos;
            if (delta.MaxAbs <= Fix.Cents(60))
            {
                // Clean catch: refund gauge and let the owner act immediately (combo extension).
                p.Active = false;
                AddGauge(owner, 15);
                if (owner.Mode == FighterMode.Attack && owner.MoveIndex >= 0)
                {
                    var m = Content.Heroes[owner.HeroIndex].Moves[owner.MoveIndex];
                    if (owner.MoveFrame > m.ActiveEnd) ReturnToNeutral(owner);
                }
                Emit(SimEventType.ProjectileCaught, owner.Id, p.Id, 0, p.Pos);
                return;
            }
            p.Vel = delta.WithMaxNorm(p.Speed);
            p.Pos += p.Vel;
        }

        Fighter FindRicochetTarget(Projectile p)
        {
            Fighter best = null;
            Fix bestDist = Fix.Zero;
            for (int i = 0; i < Fighters.Count; i++)
            {
                var t = Fighters[i];
                if (t.Id == p.LastHitId || !CanBeHit(t, p.Team)) continue;
                var dist = Fix.Abs(t.Pos.X - p.Pos.X) + Fix.Abs(t.Pos.Z - p.Pos.Z);
                if (dist > SimConstants.RicochetRange) continue;
                if (best == null || dist < bestDist) { best = t; bestDist = dist; }
            }
            return best;
        }
    }
}
