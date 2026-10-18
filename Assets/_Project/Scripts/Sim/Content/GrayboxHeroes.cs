namespace Reforged.Sim
{
    /// <summary>
    /// Graybox kits for the mechanic heroes: Hulk (Rage), Vision (Density), Ant-Man (Size).
    /// Every command is bound (see ContentTests). Names match the design doc's move sketches.
    /// </summary>
    public static partial class GrayboxContent
    {
        public const string Hulk = "Hulk";
        public const string Vision = "Vision";
        public const string AntMan = "Ant-Man";

        public static HeroDef BuildHulk()
        {
            var h = new HeroDef
            {
                Name = Hulk,
                MaxHealth = 1300,
                WalkSpeed = Fix.Cents(7),
                JumpVelocity = Fix.Cents(32),
                Gravity = Fix.Ratio(17, 1000),
                AirJumps = 0,
                BodyHalfExtents = V(50, 120, 40),
                AssistMoves = new[] { "Gamma Charge", "Boulder Toss", "Thunderclap" },
                Mechanic = HeroMechanic.Rage,      // D-pad down: Gamma Roar (spends a rage tier)
                ToolLeftMove = "Ground Clap",      // D-pad left: low OTG shockwave
                ToolRightMove = "Thunderclap",     // D-pad right: launches and lifts debris
            };

            h.Add(new MoveDef { Name = "Hammer Fist", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 6, Active = 3, Recovery = 12,
                Damage = 45, Hitstun = 18, Hitstop = 6, HitboxOffset = V(90, 140), HitboxHalfExtents = V(60, 45, 50),
                Knockback = V(5, 0), SelfVelocity = V(5, 0), DestructionPower = 20, ChainNext = "Backhand" });
            h.Add(new MoveDef { Name = "Backhand", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 7, Active = 3, Recovery = 14,
                Damage = 50, Hitstun = 20, Hitstop = 6, HitboxOffset = V(95, 140), HitboxHalfExtents = V(65, 45, 50),
                Knockback = V(6, 0), SelfVelocity = V(5, 0), DestructionPower = 30, ChainNext = "Double Axe Handle" });
            h.Add(new MoveDef { Name = "Double Axe Handle", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 10, Active = 4, Recovery = 22,
                Damage = 85, Hitstun = 26, Hitstop = 9, HitboxOffset = V(95, 110), HitboxHalfExtents = V(70, 60, 55),
                Knockback = V(6, -10), JuggleCost = 4, DestructionPower = 60, Flags = MoveFlags.GroundBounce });

            h.Add(new MoveDef { Name = "Gamma Haymaker", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Startup = 14, Active = 4, Recovery = 22,
                Damage = 120, Hitstun = 30, Hitstop = 10, HitboxOffset = V(100, 130), HitboxHalfExtents = V(70, 60, 55),
                Knockback = V(24, 8), JuggleCost = 5, GaugeGain = 14, DestructionPower = 80, Flags = MoveFlags.WallBounce });
            h.Add(new MoveDef { Name = "Hulk Uppercut", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Up,
                Startup = 12, Active = 4, Recovery = 24, Damage = 100, Hitstun = 30, Hitstop = 9, HitboxOffset = V(80, 170),
                HitboxHalfExtents = V(60, 90, 55), Knockback = V(2, 34), JuggleCost = 6, DestructionPower = 40,
                Flags = MoveFlags.Launcher | MoveFlags.CeilingBounce });
            h.Add(new MoveDef { Name = "Gamma Charge", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Forward,
                Startup = 14, Active = 10, Recovery = 20, Damage = 110, Hitstun = 30, Hitstop = 10, HitboxOffset = V(90, 120),
                HitboxHalfExtents = V(60, 80, 55), Knockback = V(30, 10), SelfVelocity = V(34, 0), JuggleCost = 5,
                DestructionPower = 100, Flags = MoveFlags.WallBounce | MoveFlags.SuperArmor });
            h.Add(new MoveDef { Name = "Stomp", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Down,
                Startup = 12, Active = 4, Recovery = 22, Damage = 90, Hitstun = 26, Hitstop = 9, HitboxOffset = V(70, 20),
                HitboxHalfExtents = V(80, 40, 60), Knockback = V(4, 20), JuggleCost = 4, DestructionPower = 50,
                Flags = MoveFlags.OTG | MoveFlags.GroundBounce });

            h.Add(new MoveDef { Name = "Air Smash", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 6, Active = 4, Recovery = 14,
                Damage = 55, Hitstun = 22, HitboxOffset = V(80, 110), HitboxHalfExtents = V(65, 60, 55), Knockback = V(5, 8) });
            h.Add(new MoveDef { Name = "Meteor Drop", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Aerial = true, Startup = 10, Active = 26, Recovery = 12,
                Damage = 110, Hitstun = 28, Hitstop = 10, HitboxOffset = V(30, 30), HitboxHalfExtents = V(110, 60, 70), Knockback = V(6, 18),
                SelfVelocity = V(6, -42), JuggleCost = 4, DestructionPower = 150, LandingLag = 16, Flags = MoveFlags.FloorBreak | MoveFlags.OTG });

            h.Add(new MoveDef { Name = "Boulder Toss", Tier = MoveTier.Special, Input = MoveInput.Special, Startup = 16, Active = 1, Recovery = 20,
                Damage = 90, Hitstun = 28, Hitstop = 9, HitboxHalfExtents = FixVec3.Zero, Knockback = V(16, 14), JuggleCost = 4, DestructionPower = 90,
                Flags = MoveFlags.GroundBounce,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(35), Rise = Fix.Cents(8), Lifetime = 40, HalfExtents = V(60, 60, 60), SpawnOffset = V(60, 260, 0) } });
            h.Add(new MoveDef { Name = "Gamma Leap", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Up,
                Startup = 8, Active = 10, Recovery = 22, Damage = 90, Hitstun = 30, Hitstop = 8, HitboxOffset = V(50, 200), HitboxHalfExtents = V(70, 90, 60),
                Knockback = V(4, 32), SelfVelocity = V(10, 40), JuggleCost = 5, DestructionPower = 60,
                Flags = MoveFlags.Launcher | MoveFlags.CeilingBounce | MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Thunderclap", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Down,
                Startup = 12, Active = 4, Recovery = 20, Damage = 60, Hitstun = 28, Hitstop = 8, HitboxOffset = V(0, 130), HitboxHalfExtents = V(250, 100, 250),
                Knockback = V(10, 22), JuggleCost = 4, DestructionPower = 40, Flags = MoveFlags.Launcher | MoveFlags.LiftDebris });
            h.Add(new MoveDef { Name = "Shoulder Barge", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Forward,
                Startup = 10, Active = 12, Recovery = 18, Damage = 80, Hitstun = 28, HitboxOffset = V(80, 130), HitboxHalfExtents = V(60, 80, 55),
                Knockback = V(22, 6), SelfVelocity = V(40, 0), JuggleCost = 4, DestructionPower = 120, Flags = MoveFlags.WallBounce });
            h.Add(new MoveDef { Name = "Ground Clap", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Back,
                Startup = 10, Active = 4, Recovery = 18, Damage = 50, Hitstun = 24, HitboxOffset = V(0, 20), HitboxHalfExtents = V(300, 40, 250),
                Knockback = V(6, 14), JuggleCost = 3, DestructionPower = 30, Flags = MoveFlags.OTG });

            h.Add(new MoveDef { Name = "Gamma Wave", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroLight, Startup = 14, Active = 1, Recovery = 18,
                Damage = 90, Hitstun = 28, HitboxHalfExtents = FixVec3.Zero, Knockback = V(12, 18), JuggleCost = 4, GaugeCost = 100, DestructionPower = 150,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(45), Lifetime = 40, HalfExtents = V(60, 50, 120), SpawnOffset = V(80, 50, 0) } });
            h.Add(new MoveDef { Name = "Smash Flurry", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroHeavy, Startup = 8, Active = 32, Recovery = 20,
                Damage = 24, Hitstun = 16, Hitstop = 4, HitboxOffset = V(95, 130), HitboxHalfExtents = V(70, 70, 55), Knockback = V(3, 0),
                MaxHits = 8, RehitInterval = 4, JuggleCost = 1, GaugeCost = 100, DestructionPower = 40 });
            h.Add(new MoveDef { Name = "Earthshaker Leap", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroJump, Startup = 12, Active = 24, Recovery = 18,
                Damage = 140, Hitstun = 32, Hitstop = 11, HitboxOffset = V(40, 40), HitboxHalfExtents = V(150, 70, 120), Knockback = V(8, -20),
                SelfVelocity = V(16, 34), JuggleCost = 5, GaugeCost = 100, DestructionPower = 200, Flags = MoveFlags.GroundBounce | MoveFlags.FloorBreak });
            h.Add(new MoveDef { Name = "Car Fists", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroSpecial, Startup = 12, Active = 6, Recovery = 22,
                Damage = 150, Hitstun = 32, Hitstop = 12, HitboxOffset = V(120, 130), HitboxHalfExtents = V(90, 70, 70), Knockback = V(28, 16),
                JuggleCost = 5, GaugeCost = 100, DestructionPower = 200, Flags = MoveFlags.WallBounce });

            h.Add(new MoveDef { Name = "Gamma Crush", Tier = MoveTier.Super, Input = MoveInput.Super, Startup = 20, Active = 8, Recovery = 24,
                Damage = 220, Hitstun = 40, Hitstop = 14, HitboxOffset = V(150, 120), HitboxHalfExtents = V(200, 150, 200), Knockback = V(18, 28),
                JuggleCost = 2, GaugeCost = 200, DestructionPower = 250, Flags = MoveFlags.Invulnerable | MoveFlags.Launcher });
            h.Add(new MoveDef { Name = "World Breaker Smash", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateI, Startup = 30, Active = 10, Recovery = 30,
                Damage = 380, Hitstun = 50, Hitstop = 16, HitboxOffset = V(0, 50), HitboxHalfExtents = V(700, 150, 300), Knockback = V(10, 34),
                JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, DestructionPower = 400, Flags = MoveFlags.Invulnerable | MoveFlags.Launcher });
            h.Add(new MoveDef { Name = "Gamma Rage", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateII, Startup = 24, Active = 4, Recovery = 20,
                Damage = 60, Hitstun = 40, Hitstop = 10, HitboxOffset = V(0, 130), HitboxHalfExtents = V(300, 150, 250), Knockback = V(14, 20),
                JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, DestructionPower = 100, Flags = MoveFlags.Invulnerable,
                Effect = MechanicEffect.GammaRage, EffectFrames = 720 });

            // Throws: bigger variants unlock with rage tiers (listed first so they win when available).
            h.Add(new MoveDef { Name = "World Breaker Throw", Tier = MoveTier.Heavy, Input = MoveInput.Grab, MinTier = 2, Startup = 6, Active = 2, Recovery = 26,
                Damage = 200, Hitstun = 40, Hitstop = 14, HitboxOffset = V(70, 130), HitboxHalfExtents = V(45, 80, 45), Knockback = V(8, 40),
                JuggleCost = 5, DestructionPower = 100, Flags = MoveFlags.Unblockable | MoveFlags.CeilingBounce | MoveFlags.GroundBounce });
            h.Add(new MoveDef { Name = "Savage Slam", Tier = MoveTier.Heavy, Input = MoveInput.Grab, MinTier = 1, Startup = 6, Active = 2, Recovery = 24,
                Damage = 150, Hitstun = 36, Hitstop = 12, HitboxOffset = V(70, 130), HitboxHalfExtents = V(45, 80, 45), Knockback = V(4, -24),
                JuggleCost = 5, Flags = MoveFlags.Unblockable | MoveFlags.GroundBounce });
            h.Add(new MoveDef { Name = "Gamma Toss", Tier = MoveTier.Heavy, Input = MoveInput.Grab, Startup = 6, Active = 2, Recovery = 22,
                Damage = 100, Hitstun = 32, Hitstop = 10, HitboxOffset = V(70, 130), HitboxHalfExtents = V(45, 80, 45), Knockback = V(22, 20),
                JuggleCost = 4, Flags = MoveFlags.Unblockable | MoveFlags.WallBounce | MoveFlags.DirectionalThrow });
            return h;
        }

        public static HeroDef BuildVision()
        {
            var h = new HeroDef
            {
                Name = Vision,
                MaxHealth = 950,
                WalkSpeed = Fix.Cents(8),
                JumpVelocity = Fix.Cents(29),
                Gravity = Fix.Ratio(13, 1000),
                AirJumps = 2,
                AssistMoves = new[] { "Mind Stone Beam", "Intangible Rush", "Ascension" },
                Mechanic = HeroMechanic.Density,   // D-pad down Phase, right Diamond, left normal
            };

            h.Add(new MoveDef { Name = "Synthezoid Jab", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 4, Active = 2, Recovery = 10,
                Damage = 30, Hitstun = 16, HitboxOffset = V(75, 115), HitboxHalfExtents = V(45, 30, 45), Knockback = V(4, 0), SelfVelocity = V(5, 0),
                ChainNext = "Synthezoid Cross" });
            h.Add(new MoveDef { Name = "Synthezoid Cross", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 5, Active = 2, Recovery = 12,
                Damage = 35, Hitstun = 18, HitboxOffset = V(85, 115), HitboxHalfExtents = V(50, 30, 45), Knockback = V(5, 0), SelfVelocity = V(5, 0),
                ChainNext = "Phase Backfist" });
            // Lunges through the enemy and hits from the other side: a built-in cross-up.
            h.Add(new MoveDef { Name = "Phase Backfist", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 9, Active = 5, Recovery = 18,
                Damage = 60, Hitstun = 26, Hitstop = 7, HitboxOffset = V(0, 110), HitboxHalfExtents = V(70, 45, 50), Knockback = V(-14, 10),
                SelfVelocity = V(30, 0), JuggleCost = 3, Flags = MoveFlags.Invulnerable });

            h.Add(new MoveDef { Name = "Solid Palm", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Startup = 11, Active = 3, Recovery = 18,
                Damage = 85, Hitstun = 28, Hitstop = 8, HitboxOffset = V(85, 115), HitboxHalfExtents = V(55, 45, 50), Knockback = V(22, 6),
                JuggleCost = 4, DestructionPower = 50, Flags = MoveFlags.WallBounce });
            h.Add(new MoveDef { Name = "Density Uppercut", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Up,
                Startup = 9, Active = 4, Recovery = 22, Damage = 80, Hitstun = 30, HitboxOffset = V(65, 140), HitboxHalfExtents = V(50, 80, 50),
                Knockback = V(2, 32), JuggleCost = 6, Flags = MoveFlags.Launcher });
            h.Add(new MoveDef { Name = "Solid Lance", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Forward,
                Startup = 12, Active = 5, Recovery = 20, Damage = 90, Hitstun = 30, Hitstop = 8, HitboxOffset = V(130, 115), HitboxHalfExtents = V(90, 30, 45),
                Knockback = V(26, 8), SelfVelocity = V(18, 0), JuggleCost = 5, DestructionPower = 70, Flags = MoveFlags.WallBounce });
            h.Add(new MoveDef { Name = "Phase Sweep", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Down,
                Startup = 9, Active = 4, Recovery = 20, Damage = 65, Hitstun = 24, HitboxOffset = V(80, 30), HitboxHalfExtents = V(70, 30, 50),
                Knockback = V(4, 12), JuggleCost = 4, Flags = MoveFlags.OTG });

            h.Add(new MoveDef { Name = "Air Phase Strike", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 4, Active = 3, Recovery = 10,
                Damage = 30, Hitstun = 18, HitboxOffset = V(70, 90), HitboxHalfExtents = V(50, 45, 45), Knockback = V(3, 8), ChainNext = "Air Solar Kick" });
            h.Add(new MoveDef { Name = "Air Solar Kick", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 7, Active = 3, Recovery = 16,
                Damage = 50, Hitstun = 22, HitboxOffset = V(75, 70), HitboxHalfExtents = V(55, 50, 45), Knockback = V(6, -28), JuggleCost = 3,
                Flags = MoveFlags.GroundBounce });
            h.Add(new MoveDef { Name = "Diamond Dive", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Aerial = true, Startup = 9, Active = 22, Recovery = 10,
                Damage = 80, Hitstun = 26, Hitstop = 8, HitboxOffset = V(20, 30), HitboxHalfExtents = V(70, 50, 60), Knockback = V(6, 16),
                SelfVelocity = V(8, -40), JuggleCost = 4, DestructionPower = 60, LandingLag = 14, Flags = MoveFlags.OTG });

            h.Add(new MoveDef { Name = "Mind Stone Beam", Tier = MoveTier.Special, Input = MoveInput.Special, Startup = 12, Active = 16, Recovery = 18,
                Damage = 18, Hitstun = 16, Hitstop = 2, HitboxOffset = V(350, 150), HitboxHalfExtents = V(300, 25, 40), Knockback = V(3, 2),
                MaxHits = 4, RehitInterval = 4, JuggleCost = 1, DestructionPower = 40 });
            h.Add(new MoveDef { Name = "Ascension", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Up,
                Startup = 7, Active = 8, Recovery = 22, Damage = 80, Hitstun = 30, Hitstop = 7, HitboxOffset = V(50, 150), HitboxHalfExtents = V(55, 90, 50),
                Knockback = V(2, 32), SelfVelocity = V(2, 30), JuggleCost = 5, Flags = MoveFlags.Launcher | MoveFlags.CeilingBounce | MoveFlags.Invulnerable });
            // Sinks out of sight, then erupts under the target.
            h.Add(new MoveDef { Name = "Phase Drop", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Down,
                Startup = 16, Active = 4, Recovery = 20, Damage = 70, Hitstun = 30, HitboxOffset = V(180, 60), HitboxHalfExtents = V(70, 60, 60),
                Knockback = V(2, 30), JuggleCost = 5, Flags = MoveFlags.Launcher | MoveFlags.Invulnerable | MoveFlags.Unblockable });
            h.Add(new MoveDef { Name = "Intangible Rush", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Forward,
                Startup = 8, Active = 10, Recovery = 16, Damage = 65, Hitstun = 26, HitboxOffset = V(60, 110), HitboxHalfExtents = V(60, 60, 50),
                Knockback = V(12, 6), SelfVelocity = V(36, 0), JuggleCost = 3, Flags = MoveFlags.Invulnerable });

            h.Add(new MoveDef { Name = "Solar Lances", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroLight, Startup = 10, Active = 1, Recovery = 16,
                Damage = 50, Hitstun = 24, HitboxHalfExtents = FixVec3.Zero, Knockback = V(8, 14), JuggleCost = 3, GaugeCost = 100, DestructionPower = 50,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(55), Lifetime = 35, Ricochets = 2, HalfExtents = V(35, 20, 45) } });
            h.Add(new MoveDef { Name = "Diamond Hammer", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroHeavy, Startup = 14, Active = 4, Recovery = 22,
                Damage = 140, Hitstun = 32, Hitstop = 12, HitboxOffset = V(90, 100), HitboxHalfExtents = V(70, 80, 60), Knockback = V(8, -24),
                JuggleCost = 5, GaugeCost = 100, DestructionPower = 150, Flags = MoveFlags.GroundBounce | MoveFlags.SuperArmor });
            h.Add(new MoveDef { Name = "Phantom Flight", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroJump, Startup = 6, Active = 18, Recovery = 14,
                Damage = 70, Hitstun = 26, HitboxOffset = V(60, 110), HitboxHalfExtents = V(70, 60, 60), Knockback = V(12, 16),
                SelfVelocity = V(22, 24), JuggleCost = 4, GaugeCost = 100, Flags = MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Mind Stone Pulse", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroSpecial, Startup = 14, Active = 6, Recovery = 18,
                Damage = 30, Hitstun = 70, HitboxOffset = V(0, 110), HitboxHalfExtents = V(250, 120, 200), Knockback = V(2, 0),
                JuggleCost = 3, GaugeCost = 100 });

            h.Add(new MoveDef { Name = "Synthezoid Overdrive", Tier = MoveTier.Super, Input = MoveInput.Super, Startup = 18, Active = 30, Recovery = 20,
                Damage = 30, Hitstun = 22, Hitstop = 3, HitboxOffset = V(550, 150), HitboxHalfExtents = V(500, 300, 60), Knockback = V(5, 3),
                MaxHits = 8, RehitInterval = 4, JuggleCost = 1, GaugeCost = 200, DestructionPower = 150, Flags = MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Mind Stone Solar Beam", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateI, Startup = 30, Active = 45, Recovery = 30,
                Damage = 42, Hitstun = 30, Hitstop = 4, HitboxOffset = V(900, 150), HitboxHalfExtents = V(850, 600, 120), Knockback = V(8, 4),
                MaxHits = 10, RehitInterval = 4, JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, DestructionPower = 300, Flags = MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Phase Field", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateII, Startup = 24, Active = 6, Recovery = 20,
                Damage = 40, Hitstun = 120, Hitstop = 8, HitboxOffset = V(0, 150), HitboxHalfExtents = V(800, 400, 400), Knockback = V(0, 0),
                JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, Flags = MoveFlags.Invulnerable | MoveFlags.Unblockable,
                Effect = MechanicEffect.PhaseField, EffectFrames = 360 });

            h.Add(new MoveDef { Name = "Density Throw", Tier = MoveTier.Heavy, Input = MoveInput.Grab, Startup = 5, Active = 2, Recovery = 20,
                Damage = 90, Hitstun = 30, HitboxOffset = V(60, 110), HitboxHalfExtents = V(35, 60, 35), Knockback = V(4, -26),
                JuggleCost = 4, Flags = MoveFlags.Unblockable | MoveFlags.GroundBounce | MoveFlags.DirectionalThrow });
            return h;
        }

        public static HeroDef BuildAntMan()
        {
            var h = new HeroDef
            {
                Name = AntMan,
                MaxHealth = 900,
                WalkSpeed = Fix.Cents(9),
                JumpVelocity = Fix.Cents(29),
                AirJumps = 1,
                BodyHalfExtents = V(32, 88, 28),
                AssistMoves = new[] { "Ant Swarm", "Red Disc", "Growth Spurt" },
                Mechanic = HeroMechanic.Size,   // D-pad down shrink, right grow, left normal; cancels any move
            };

            h.Add(new MoveDef { Name = "Quick Jab", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 3, Active = 2, Recovery = 9,
                Damage = 26, Hitstun = 15, HitboxOffset = V(70, 110), HitboxHalfExtents = V(45, 30, 45), Knockback = V(3, 0), SelfVelocity = V(5, 0),
                ChainNext = "Quick Hook" });
            h.Add(new MoveDef { Name = "Quick Hook", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 4, Active = 2, Recovery = 11,
                Damage = 30, Hitstun = 17, HitboxOffset = V(80, 110), HitboxHalfExtents = V(50, 30, 45), Knockback = V(4, 0), SelfVelocity = V(5, 0),
                ChainNext = "Shrink Dash Punch" });
            h.Add(new MoveDef { Name = "Shrink Dash Punch", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 7, Active = 5, Recovery = 16,
                Damage = 55, Hitstun = 24, Hitstop = 6, HitboxOffset = V(60, 90), HitboxHalfExtents = V(60, 40, 45), Knockback = V(14, 10),
                SelfVelocity = V(30, 0), JuggleCost = 3 });

            h.Add(new MoveDef { Name = "Pym Haymaker", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Startup = 10, Active = 3, Recovery = 18,
                Damage = 80, Hitstun = 28, Hitstop = 8, HitboxOffset = V(80, 105), HitboxHalfExtents = V(55, 45, 50), Knockback = V(16, 6),
                JuggleCost = 4, DestructionPower = 40 });
            // Grows mid-uppercut: the launcher everyone remembers.
            h.Add(new MoveDef { Name = "Growth Spurt", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Up,
                Startup = 8, Active = 5, Recovery = 22, Damage = 85, Hitstun = 30, Hitstop = 8, HitboxOffset = V(60, 150), HitboxHalfExtents = V(60, 100, 55),
                Knockback = V(2, 34), JuggleCost = 6, DestructionPower = 30, Flags = MoveFlags.Launcher | MoveFlags.CeilingBounce });
            h.Add(new MoveDef { Name = "Giant Shoulder", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Forward,
                Startup = 12, Active = 6, Recovery = 20, Damage = 90, Hitstun = 30, Hitstop = 9, HitboxOffset = V(80, 110), HitboxHalfExtents = V(60, 70, 55),
                Knockback = V(28, 8), SelfVelocity = V(24, 0), JuggleCost = 5, DestructionPower = 90, Flags = MoveFlags.WallBounce });
            h.Add(new MoveDef { Name = "Ant Sweep", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Down,
                Startup = 8, Active = 4, Recovery = 20, Damage = 60, Hitstun = 24, HitboxOffset = V(80, 25), HitboxHalfExtents = V(70, 30, 50),
                Knockback = V(4, 12), JuggleCost = 4, Flags = MoveFlags.OTG });

            h.Add(new MoveDef { Name = "Ant Kick", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 4, Active = 3, Recovery = 10,
                Damage = 28, Hitstun = 18, HitboxOffset = V(70, 80), HitboxHalfExtents = V(50, 45, 45), Knockback = V(3, 8), ChainNext = "Pinball Kick" });
            h.Add(new MoveDef { Name = "Pinball Kick", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 6, Active = 4, Recovery = 14,
                Damage = 45, Hitstun = 22, HitboxOffset = V(70, 70), HitboxHalfExtents = V(55, 50, 45), Knockback = V(6, -26), JuggleCost = 3,
                Flags = MoveFlags.GroundBounce });
            h.Add(new MoveDef { Name = "Size Drop", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Aerial = true, Startup = 9, Active = 22, Recovery = 12,
                Damage = 75, Hitstun = 26, Hitstop = 8, HitboxOffset = V(20, 30), HitboxHalfExtents = V(70, 50, 60), Knockback = V(6, 16),
                SelfVelocity = V(4, -40), JuggleCost = 4, DestructionPower = 120, LandingLag = 14, Flags = MoveFlags.FloorBreak | MoveFlags.OTG });

            h.Add(new MoveDef { Name = "Red Disc", Tier = MoveTier.Special, Input = MoveInput.Special, Startup = 9, Active = 1, Recovery = 16,
                Damage = 40, Hitstun = 40, HitboxHalfExtents = FixVec3.Zero, Knockback = V(4, 6), JuggleCost = 3, DestructionPower = 30,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(50), Lifetime = 35, HalfExtents = V(25, 25, 45), SpawnOffset = V(60, 120, 0) } });
            h.Add(new MoveDef { Name = "Blue Disc", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Down, Startup = 11, Active = 1, Recovery = 18,
                Damage = 55, Hitstun = 26, HitboxHalfExtents = FixVec3.Zero, Knockback = V(22, 12), JuggleCost = 4, DestructionPower = 80,
                Flags = MoveFlags.WallBounce,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(40), Rise = Fix.Cents(4), Lifetime = 40, HalfExtents = V(35, 35, 50), SpawnOffset = V(60, 120, 0) } });
            h.Add(new MoveDef { Name = "Flying Ant Mount", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Up,
                Startup = 6, Active = 14, Recovery = 18, Damage = 60, Hitstun = 26, HitboxOffset = V(50, 140), HitboxHalfExtents = V(55, 70, 50),
                Knockback = V(4, 28), SelfVelocity = V(12, 34), JuggleCost = 4, Flags = MoveFlags.Launcher | MoveFlags.CeilingBounce | MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Ant Swarm", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Forward,
                Startup = 10, Active = 24, Recovery = 16, Damage = 14, Hitstun = 18, Hitstop = 2, HitboxOffset = V(200, 100), HitboxHalfExtents = V(130, 70, 60),
                Knockback = V(4, 4), MaxHits = 6, RehitInterval = 4, JuggleCost = 1 });

            h.Add(new MoveDef { Name = "Pinball", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroLight, Startup = 6, Active = 24, Recovery = 16,
                Damage = 22, Hitstun = 18, Hitstop = 3, HitboxOffset = V(50, 60), HitboxHalfExtents = V(60, 50, 50), Knockback = V(4, 6),
                SelfVelocity = V(26, 0), MaxHits = 6, RehitInterval = 4, JuggleCost = 1, GaugeCost = 100 });
            h.Add(new MoveDef { Name = "Giant Stomp", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroHeavy, Startup = 16, Active = 6, Recovery = 24,
                Damage = 130, Hitstun = 32, Hitstop = 12, HitboxOffset = V(120, 30), HitboxHalfExtents = V(160, 60, 200), Knockback = V(6, 24),
                JuggleCost = 5, GaugeCost = 100, DestructionPower = 180, Flags = MoveFlags.GroundBounce | MoveFlags.OTG });
            h.Add(new MoveDef { Name = "Quantum Dive", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroJump, Startup = 18, Active = 6, Recovery = 18,
                Damage = 110, Hitstun = 32, Hitstop = 10, HitboxOffset = V(220, 60), HitboxHalfExtents = V(80, 70, 60), Knockback = V(4, 32),
                JuggleCost = 5, GaugeCost = 100, Flags = MoveFlags.Launcher | MoveFlags.Invulnerable | MoveFlags.Unblockable });
            h.Add(new MoveDef { Name = "Colony Call", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroSpecial, Startup = 14, Active = 30, Recovery = 16,
                Damage = 16, Hitstun = 20, Hitstop = 2, HitboxOffset = V(250, 100), HitboxHalfExtents = V(250, 100, 200), Knockback = V(3, 5),
                MaxHits = 8, RehitInterval = 4, JuggleCost = 1, GaugeCost = 100 });

            h.Add(new MoveDef { Name = "Giant-Man Rampage", Tier = MoveTier.Super, Input = MoveInput.Super, Startup = 20, Active = 24, Recovery = 24,
                Damage = 50, Hitstun = 30, Hitstop = 6, HitboxOffset = V(250, 150), HitboxHalfExtents = V(300, 200, 300), Knockback = V(10, 14),
                MaxHits = 4, RehitInterval = 6, JuggleCost = 1, GaugeCost = 200, DestructionPower = 250, Flags = MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Giant-Man", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateI, Startup = 30, Active = 10, Recovery = 30,
                Damage = 360, Hitstun = 50, Hitstop = 16, HitboxOffset = V(300, 100), HitboxHalfExtents = V(500, 300, 300), Knockback = V(12, 30),
                JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, DestructionPower = 400, Flags = MoveFlags.Invulnerable | MoveFlags.Launcher });
            h.Add(new MoveDef { Name = "Quantum Tunnel", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateII, Startup = 24, Active = 4, Recovery = 20,
                Damage = 30, Hitstun = 40, Hitstop = 8, HitboxOffset = V(0, 150), HitboxHalfExtents = V(800, 400, 400), Knockback = V(4, 12),
                JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, Flags = MoveFlags.Invulnerable,
                Effect = MechanicEffect.QuantumTunnel, EffectFrames = 600 });

            h.Add(new MoveDef { Name = "Pym Toss", Tier = MoveTier.Heavy, Input = MoveInput.Grab, Startup = 5, Active = 2, Recovery = 20,
                Damage = 80, Hitstun = 30, HitboxOffset = V(60, 100), HitboxHalfExtents = V(35, 60, 35), Knockback = V(18, 22),
                JuggleCost = 4, Flags = MoveFlags.Unblockable | MoveFlags.WallBounce | MoveFlags.DirectionalThrow });
            return h;
        }
    }
}
