namespace Reforged.Sim
{
    /// <summary>
    /// Code-defined vertical-slice content: Captain America (full kit), Iron Man (slice kit) and
    /// graybox enemies. Designers can export these to HeroAssets in Unity and tune from there.
    /// Units: distances in hundredths of a meter, velocities in hundredths of a meter per tick.
    /// </summary>
    public static partial class GrayboxContent
    {
        public const string CaptainAmerica = "Captain America";
        public const string IronMan = "Iron Man";
        public const string TrainingDummy = "Training Dummy";
        public const string HydraTrooper = "Hydra Trooper";
        public const string HydraHeavy = "Hydra Heavy";
        public const string NickFury = "Nick Fury";
        public const string WarMachine = "War Machine";

        public static ContentDatabase Create()
        {
            var db = new ContentDatabase();
            db.Add(BuildCaptainAmerica());
            db.Add(BuildIronMan());
            db.Add(BuildTrainingDummy());
            db.Add(BuildHydraTrooper());
            db.Add(BuildHydraHeavy());
            db.Add(BuildNickFury());
            db.Add(BuildWarMachine());
            db.Add(BuildHulk());
            db.Add(BuildVision());
            db.Add(BuildAntMan());
            return db.Resolve();
        }

        static FixVec3 V(int x, int y, int z = 0) => FixVec3.Cents(x, y, z);

        public static HeroDef BuildCaptainAmerica()
        {
            var h = new HeroDef
            {
                Name = CaptainAmerica,
                MaxHealth = 1000,
                WalkSpeed = Fix.Cents(9),
                JumpVelocity = Fix.Cents(28),
                AirJumps = 1,
                AssistMoves = new[] { "Charging Star", "Shield Toss", "Rising Shield Slash" },
                ToolCycleRange = 1,   // D-pad left/right: shield throw angle low / level / high
                ToolChargeCost = 25,  // D-pad down: next shield throw ricochets once more
            };

            // Light string: X, X, X, X
            h.Add(new MoveDef { Name = "Jab", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 4, Active = 2, Recovery = 10,
                Damage = 30, Hitstun = 16, Hitstop = 4, HitboxOffset = V(75, 110), HitboxHalfExtents = V(45, 30, 45),
                Knockback = V(4, 0), SelfVelocity = V(5, 0), JuggleCost = 2, GaugeGain = 6, ChainNext = "Cross" });
            h.Add(new MoveDef { Name = "Cross", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 5, Active = 2, Recovery = 12,
                Damage = 35, Hitstun = 17, Hitstop = 4, HitboxOffset = V(85, 110), HitboxHalfExtents = V(50, 30, 45),
                Knockback = V(5, 0), SelfVelocity = V(5, 0), JuggleCost = 2, GaugeGain = 6, ChainNext = "Shield Bash" });
            h.Add(new MoveDef { Name = "Shield Bash", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 6, Active = 3, Recovery = 14,
                Damage = 45, Hitstun = 20, Hitstop = 5, HitboxOffset = V(90, 105), HitboxHalfExtents = V(55, 40, 50),
                Knockback = V(6, 0), SelfVelocity = V(6, 0), JuggleCost = 3, GaugeGain = 8, DestructionPower = 20, ChainNext = "Liberty Kick" });
            h.Add(new MoveDef { Name = "Liberty Kick", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 8, Active = 3, Recovery = 20,
                Damage = 70, Hitstun = 26, Hitstop = 7, HitboxOffset = V(95, 100), HitboxHalfExtents = V(60, 40, 50),
                Knockback = V(18, 10), SelfVelocity = V(6, 0), JuggleCost = 4, GaugeGain = 10, DestructionPower = 30, Flags = MoveFlags.WallBounce });
            h.Add(new MoveDef { Name = "Step-In Elbow", Tier = MoveTier.Light, Input = MoveInput.Light, Direction = StickDir.Forward,
                Startup = 6, Active = 3, Recovery = 14, Damage = 40, Hitstun = 18, HitboxOffset = V(80, 115), HitboxHalfExtents = V(50, 30, 45),
                Knockback = V(5, 0), SelfVelocity = V(14, 0), ChainNext = "Shield Bash" });
            h.Add(new MoveDef { Name = "Low Shield Sweep", Tier = MoveTier.Light, Input = MoveInput.Light, Direction = StickDir.Down,
                Startup = 6, Active = 3, Recovery = 14, Damage = 35, Hitstun = 18, HitboxOffset = V(80, 35), HitboxHalfExtents = V(55, 30, 45),
                Knockback = V(4, 6), Flags = MoveFlags.OTG, ChainNext = "Shield Bash" });

            // Heavies
            h.Add(new MoveDef { Name = "Haymaker", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Startup = 10, Active = 3, Recovery = 18,
                Damage = 85, Hitstun = 28, Hitstop = 7, HitboxOffset = V(80, 100), HitboxHalfExtents = V(55, 50, 50),
                Knockback = V(14, 4), JuggleCost = 4, GaugeGain = 12, DestructionPower = 40 });
            h.Add(new MoveDef { Name = "Star-Spangled Uppercut", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Up,
                Startup = 9, Active = 4, Recovery = 22, Damage = 80, Hitstun = 30, Hitstop = 6, HitboxOffset = V(65, 140),
                HitboxHalfExtents = V(50, 80, 50), Knockback = V(2, 32), JuggleCost = 6, GaugeGain = 12, DestructionPower = 20,
                Flags = MoveFlags.Launcher });
            h.Add(new MoveDef { Name = "Shield Charge", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Forward,
                Startup = 12, Active = 6, Recovery = 20, Damage = 90, Hitstun = 30, Hitstop = 8, HitboxOffset = V(80, 100),
                HitboxHalfExtents = V(50, 60, 50), Knockback = V(28, 8), SelfVelocity = V(25, 0), JuggleCost = 5, GaugeGain = 12,
                DestructionPower = 60, Flags = MoveFlags.WallBounce });
            h.Add(new MoveDef { Name = "Shield Sweep", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Down,
                Startup = 10, Active = 4, Recovery = 22, Damage = 70, Hitstun = 24, HitboxOffset = V(85, 30), HitboxHalfExtents = V(70, 30, 50),
                Knockback = V(6, 12), JuggleCost = 4, Flags = MoveFlags.OTG });

            // Air normals
            h.Add(new MoveDef { Name = "Air Jab", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 4, Active = 3, Recovery = 10,
                Damage = 30, Hitstun = 18, HitboxOffset = V(70, 90), HitboxHalfExtents = V(45, 40, 45), Knockback = V(3, 8), ChainNext = "Air Shield Swipe" });
            h.Add(new MoveDef { Name = "Air Shield Swipe", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 5, Active = 3, Recovery = 12,
                Damage = 35, Hitstun = 18, HitboxOffset = V(75, 90), HitboxHalfExtents = V(55, 50, 45), Knockback = V(4, 9), ChainNext = "Air Hammer Kick" });
            h.Add(new MoveDef { Name = "Air Hammer Kick", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 7, Active = 3, Recovery = 16,
                Damage = 50, Hitstun = 22, Hitstop = 6, HitboxOffset = V(70, 70), HitboxHalfExtents = V(55, 55, 45), Knockback = V(6, -30),
                JuggleCost = 3, Flags = MoveFlags.GroundBounce });
            h.Add(new MoveDef { Name = "Aerial Shield Spike", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Aerial = true, Startup = 9, Active = 4, Recovery = 18,
                Damage = 80, Hitstun = 26, Hitstop = 7, HitboxOffset = V(70, 60), HitboxHalfExtents = V(60, 60, 50), Knockback = V(8, -35),
                JuggleCost = 4, DestructionPower = 30, Flags = MoveFlags.GroundBounce });
            h.Add(new MoveDef { Name = "Shield Drop", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Down, Aerial = true,
                Startup = 8, Active = 24, Recovery = 10, Damage = 70, Hitstun = 24, Hitstop = 6, HitboxOffset = V(0, 20), HitboxHalfExtents = V(70, 40, 60),
                Knockback = V(4, 14), SelfVelocity = V(0, -40), JuggleCost = 3, DestructionPower = 120, LandingLag = 14,
                Flags = MoveFlags.FloorBreak | MoveFlags.OTG });

            // Specials (B + direction)
            h.Add(new MoveDef { Name = "Shield Toss", Tier = MoveTier.Special, Input = MoveInput.Special, Startup = 10, Active = 1, Recovery = 18,
                Damage = 55, Hitstun = 22, Hitstop = 5, HitboxHalfExtents = FixVec3.Zero, Knockback = V(10, 6), JuggleCost = 3, GaugeGain = 10,
                DestructionPower = 40, Projectile = new ProjectileSpec { Speed = Fix.Cents(38), Lifetime = 40, Ricochets = 2, ReturnsToOwner = true } });
            h.Add(new MoveDef { Name = "Air Shield Toss", Tier = MoveTier.Special, Input = MoveInput.Special, Aerial = true, Startup = 8, Active = 1, Recovery = 16,
                Damage = 50, Hitstun = 22, HitboxHalfExtents = FixVec3.Zero, Knockback = V(8, 10), JuggleCost = 3, DestructionPower = 40,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(38), Rise = Fix.Cents(-12), Lifetime = 40, Ricochets = 2, ReturnsToOwner = true } });
            h.Add(new MoveDef { Name = "Rising Shield Slash", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Up,
                Startup = 6, Active = 6, Recovery = 24, Damage = 90, Hitstun = 30, Hitstop = 7, HitboxOffset = V(60, 140), HitboxHalfExtents = V(50, 80, 50),
                Knockback = V(4, 30), SelfVelocity = V(4, 24), JuggleCost = 5, GaugeGain = 12, DestructionPower = 40,
                Flags = MoveFlags.Launcher | MoveFlags.CeilingBounce | MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Shield Plant", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Down,
                Startup = 4, Active = 14, Recovery = 14, Damage = 60, Hitstun = 26, HitboxOffset = V(70, 100), HitboxHalfExtents = V(50, 70, 50),
                Knockback = V(14, 16), JuggleCost = 4, Flags = MoveFlags.SuperArmor });
            h.Add(new MoveDef { Name = "Charging Star", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Forward,
                Startup = 8, Active = 8, Recovery = 14, Damage = 60, Hitstun = 24, Hitstop = 6, HitboxOffset = V(70, 100), HitboxHalfExtents = V(55, 60, 50),
                Knockback = V(10, 4), SelfVelocity = V(32, 0), JuggleCost = 3, GaugeGain = 10, DestructionPower = 50 });

            // Hero Specials (RT + face button), one bar each
            h.Add(new MoveDef { Name = "Shield Slingshot", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroLight, Startup = 10, Active = 1, Recovery = 16,
                Damage = 60, Hitstun = 26, HitboxHalfExtents = FixVec3.Zero, Knockback = V(10, 12), JuggleCost = 3, GaugeCost = 100, DestructionPower = 60,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(50), Lifetime = 50, Ricochets = 4, ReturnsToOwner = true } });
            h.Add(new MoveDef { Name = "Brooklyn Brawl", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroHeavy, Startup = 6, Active = 30, Recovery = 18,
                Damage = 18, Hitstun = 14, Hitstop = 3, HitboxOffset = V(75, 105), HitboxHalfExtents = V(55, 50, 50), Knockback = V(2, 0),
                MaxHits = 10, RehitInterval = 3, JuggleCost = 1, GaugeCost = 100, GaugeGain = 2, DestructionPower = 15 });
            h.Add(new MoveDef { Name = "Leap of Liberty", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroJump, Startup = 10, Active = 20, Recovery = 16,
                Damage = 120, Hitstun = 30, Hitstop = 9, HitboxOffset = V(50, 40), HitboxHalfExtents = V(80, 60, 60), Knockback = V(6, -20),
                SelfVelocity = V(18, 26), JuggleCost = 5, GaugeCost = 100, DestructionPower = 150,
                Flags = MoveFlags.GroundBounce | MoveFlags.FloorBreak });
            h.Add(new MoveDef { Name = "Rally Cry", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroSpecial, Startup = 20, Active = 1, Recovery = 20,
                HitboxHalfExtents = FixVec3.Zero, GaugeCost = 100, TeamBuffFrames = 300 });

            // Super (RT + LT), two bars
            h.Add(new MoveDef { Name = "Shield of Freedom", Tier = MoveTier.Super, Input = MoveInput.Super, Startup = 20, Active = 1, Recovery = 24,
                Damage = 80, Hitstun = 30, Hitstop = 8, HitboxHalfExtents = FixVec3.Zero, Knockback = V(10, 14), JuggleCost = 2, GaugeCost = 200,
                DestructionPower = 100, Flags = MoveFlags.Invulnerable,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(60), Lifetime = 60, Ricochets = 8, ReturnsToOwner = true } });

            // Two Ultimates, three bars each
            h.Add(new MoveDef { Name = "Avengers Assemble", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateI, Startup = 30, Active = 40, Recovery = 30,
                Damage = 60, Hitstun = 40, Hitstop = 6, HitboxOffset = V(300, 150), HitboxHalfExtents = V(600, 300, 300), Knockback = V(6, 12),
                MaxHits = 8, RehitInterval = 5, JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, DestructionPower = 80, Flags = MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Star-Spangled Stand", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateII, Startup = 25, Active = 10, Recovery = 25,
                Damage = 120, Hitstun = 40, Hitstop = 10, HitboxOffset = V(0, 150), HitboxHalfExtents = V(800, 400, 400), Knockback = V(10, 20),
                JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, DestructionPower = 60, TeamBuffFrames = 600,
                Flags = MoveFlags.Invulnerable | MoveFlags.Launcher });

            // Throw (RB)
            h.Add(new MoveDef { Name = "Shoulder Toss", Tier = MoveTier.Heavy, Input = MoveInput.Grab, Startup = 5, Active = 2, Recovery = 20,
                Damage = 90, Hitstun = 30, Hitstop = 8, HitboxOffset = V(60, 100), HitboxHalfExtents = V(35, 60, 35), Knockback = V(16, 22),
                JuggleCost = 4, Flags = MoveFlags.Unblockable | MoveFlags.WallBounce | MoveFlags.DirectionalThrow });
            return h;
        }

        public static HeroDef BuildIronMan()
        {
            var h = new HeroDef
            {
                Name = IronMan,
                MaxHealth = 950,
                WalkSpeed = Fix.Cents(8),
                JumpVelocity = Fix.Cents(30),
                Gravity = Fix.Ratio(15, 1000),
                AirJumps = 2,
                AssistMoves = new[] { "Repulsor Blast", "Unibeam Burst", "Boot-Jet Launcher" },
                Mechanic = HeroMechanic.Heat,   // hold Jump in the air to hover; D-pad down vents
                ToolCycleRange = 1,             // D-pad left/right: repulsor aim low / level / high
            };
            h.Add(new MoveDef { Name = "Gauntlet Jab", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 4, Active = 2, Recovery = 10,
                Damage = 28, Hitstun = 16, HitboxOffset = V(75, 110), HitboxHalfExtents = V(45, 30, 45), Knockback = V(4, 0), SelfVelocity = V(5, 0),
                ChainNext = "Repulsor Palm" });
            h.Add(new MoveDef { Name = "Repulsor Palm", HeatCost = 60, Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 6, Active = 3, Recovery = 12,
                Damage = 40, Hitstun = 18, HitboxOffset = V(90, 110), HitboxHalfExtents = V(60, 35, 45), Knockback = V(6, 0), SelfVelocity = V(5, 0),
                DestructionPower = 20, ChainNext = "Thruster Kick" });
            h.Add(new MoveDef { Name = "Thruster Kick", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 8, Active = 3, Recovery = 18,
                Damage = 65, Hitstun = 24, Hitstop = 6, HitboxOffset = V(95, 100), HitboxHalfExtents = V(60, 40, 50), Knockback = V(16, 12),
                JuggleCost = 4, DestructionPower = 30 });
            h.Add(new MoveDef { Name = "Unibeam Burst", HeatCost = 220, Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Startup = 12, Active = 4, Recovery = 18,
                Damage = 85, Hitstun = 28, Hitstop = 7, HitboxOffset = V(150, 120), HitboxHalfExtents = V(110, 30, 45), Knockback = V(20, 6),
                JuggleCost = 4, DestructionPower = 60, Flags = MoveFlags.WallBounce });
            h.Add(new MoveDef { Name = "Boot-Jet Launcher", HeatCost = 120, Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Direction = StickDir.Up,
                Startup = 9, Active = 4, Recovery = 20, Damage = 75, Hitstun = 30, HitboxOffset = V(60, 120), HitboxHalfExtents = V(50, 80, 50),
                Knockback = V(2, 33), SelfVelocity = V(0, 20), JuggleCost = 6, Flags = MoveFlags.Launcher });
            h.Add(new MoveDef { Name = "Air Palm", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 4, Active = 3, Recovery = 10,
                Damage = 30, Hitstun = 18, HitboxOffset = V(75, 90), HitboxHalfExtents = V(50, 40, 45), Knockback = V(3, 8), ChainNext = "Air Thruster Spin" });
            h.Add(new MoveDef { Name = "Air Thruster Spin", Tier = MoveTier.Light, Input = MoveInput.Light, Aerial = true, Startup = 6, Active = 6, Recovery = 14,
                Damage = 45, Hitstun = 22, HitboxOffset = V(40, 80), HitboxHalfExtents = V(80, 60, 50), Knockback = V(5, -28), JuggleCost = 3,
                Flags = MoveFlags.GroundBounce });
            h.Add(new MoveDef { Name = "Repulsor Blast", HeatCost = 180, Tier = MoveTier.Special, Input = MoveInput.Special, Startup = 9, Active = 1, Recovery = 16,
                Damage = 50, Hitstun = 20, HitboxHalfExtents = FixVec3.Zero, Knockback = V(12, 4), JuggleCost = 3, DestructionPower = 35,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(55), Lifetime = 30, HalfExtents = V(35, 30, 45) } });
            h.Add(new MoveDef { Name = "Smart Missiles", HeatCost = 200, Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Down,
                Startup = 12, Active = 1, Recovery = 20, Damage = 40, Hitstun = 20, HitboxHalfExtents = FixVec3.Zero, Knockback = V(6, 14),
                JuggleCost = 3, DestructionPower = 50,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(35), Rise = Fix.Cents(10), Lifetime = 45, Ricochets = 3 } });
            h.Add(new MoveDef { Name = "Repulsor Barrage", HeatCost = 300, Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroHeavy, Startup = 8, Active = 24, Recovery = 16,
                Damage = 20, Hitstun = 14, Hitstop = 3, HitboxOffset = V(160, 110), HitboxHalfExtents = V(120, 40, 50), Knockback = V(3, 0),
                MaxHits = 8, RehitInterval = 3, JuggleCost = 1, GaugeCost = 100, DestructionPower = 20 });
            h.Add(new MoveDef { Name = "Pulse Bolts", HeatCost = 250, Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroLight, Startup = 10, Active = 1, Recovery = 16,
                Damage = 55, Hitstun = 24, HitboxHalfExtents = FixVec3.Zero, Knockback = V(8, 16), JuggleCost = 3, GaugeCost = 100,
                DestructionPower = 60, Projectile = new ProjectileSpec { Speed = Fix.Cents(45), Lifetime = 40, Ricochets = 3 } });
            h.Add(new MoveDef { Name = "Afterburner Dive", HeatCost = 150, Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroJump, Startup = 12, Active = 22, Recovery = 16,
                Damage = 110, Hitstun = 30, Hitstop = 9, HitboxOffset = V(60, 50), HitboxHalfExtents = V(80, 60, 60), Knockback = V(10, -24),
                SelfVelocity = V(24, 22), JuggleCost = 5, GaugeCost = 100, DestructionPower = 150,
                Flags = MoveFlags.GroundBounce | MoveFlags.FloorBreak });
            h.Add(new MoveDef { Name = "Overclock Protocol", Tier = MoveTier.HeroSpecial, Input = MoveInput.HeroSpecial, Startup = 16, Active = 6, Recovery = 18,
                Damage = 40, Hitstun = 30, HitboxOffset = V(0, 100), HitboxHalfExtents = V(200, 120, 150), Knockback = V(14, 10),
                JuggleCost = 3, GaugeCost = 100, DestructionPower = 40, TeamBuffFrames = 180 });
            h.Add(new MoveDef { Name = "Unibeam", Tier = MoveTier.Super, Input = MoveInput.Super, Startup = 18, Active = 30, Recovery = 20,
                Damage = 30, Hitstun = 20, Hitstop = 3, HitboxOffset = V(550, 120), HitboxHalfExtents = V(500, 50, 50), Knockback = V(6, 2),
                MaxHits = 8, RehitInterval = 4, JuggleCost = 1, GaugeCost = 200, DestructionPower = 200, Flags = MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Proton Cannon", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateI, Startup = 30, Active = 45, Recovery = 30,
                Damage = 40, Hitstun = 30, Hitstop = 4, HitboxOffset = V(900, 130), HitboxHalfExtents = V(850, 90, 90), Knockback = V(8, 4),
                MaxHits = 10, RehitInterval = 4, JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, DestructionPower = 300, Flags = MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Iron Legion", Tier = MoveTier.Ultimate, Input = MoveInput.UltimateII, Startup = 25, Active = 60, Recovery = 25,
                Damage = 45, Hitstun = 30, Hitstop = 4, HitboxOffset = V(0, 200), HitboxHalfExtents = V(900, 400, 400), Knockback = V(6, 14),
                MaxHits = 6, RehitInterval = 10, JuggleCost = 0, GaugeCost = 300, GaugeGain = 0, DestructionPower = 50, TeamBuffFrames = 300,
                Flags = MoveFlags.Invulnerable });
            h.Add(new MoveDef { Name = "Jet Toss", Tier = MoveTier.Heavy, Input = MoveInput.Grab, Startup = 5, Active = 2, Recovery = 20,
                Damage = 85, Hitstun = 30, HitboxOffset = V(60, 100), HitboxHalfExtents = V(35, 60, 35), Knockback = V(10, 30),
                JuggleCost = 4, Flags = MoveFlags.Unblockable | MoveFlags.DirectionalThrow });
            return h;
        }

        // ------------------------------------------------------------------ assist-only cameos

        public static HeroDef BuildNickFury()
        {
            var h = new HeroDef
            {
                Name = NickFury, AssistOnly = true, MaxHealth = 1,
                AssistMoves = new[] { "Gun Kata Burst", "Helicarrier Strike", "Flashbang" },
            };
            h.Add(new MoveDef { Name = "Gun Kata Burst", Tier = MoveTier.Special, Input = MoveInput.Special, Startup = 8, Active = 1, Recovery = 14,
                Damage = 35, Hitstun = 18, HitboxHalfExtents = FixVec3.Zero, Knockback = V(6, 4), JuggleCost = 2, DestructionPower = 20,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(60), Lifetime = 30, HalfExtents = V(25, 20, 45) } });
            // Called-in orbital strike: long startup, huge hitbox ahead, spikes and breaks floors.
            h.Add(new MoveDef { Name = "Helicarrier Strike", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Forward,
                Startup = 30, Active = 6, Recovery = 10, Damage = 90, Hitstun = 30, Hitstop = 8, HitboxOffset = V(350, 150),
                HitboxHalfExtents = V(150, 200, 150), Knockback = V(4, -20), JuggleCost = 3, DestructionPower = 120, Flags = MoveFlags.GroundBounce });
            // Utility: long stun, unblockable, strips armor.
            h.Add(new MoveDef { Name = "Flashbang", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Down,
                Startup = 10, Active = 4, Recovery = 12, Damage = 10, Hitstun = 60, HitboxOffset = V(150, 100), HitboxHalfExtents = V(150, 100, 150),
                Knockback = V(0, 0), JuggleCost = 2, Flags = MoveFlags.Unblockable });
            return h;
        }

        public static HeroDef BuildWarMachine()
        {
            var h = new HeroDef
            {
                Name = WarMachine, AssistOnly = true, MaxHealth = 1,
                AssistMoves = new[] { "Shoulder Cannon", "Missile Barrage", "Gatling Sweep" },
            };
            h.Add(new MoveDef { Name = "Shoulder Cannon", Tier = MoveTier.Special, Input = MoveInput.Special, Startup = 12, Active = 1, Recovery = 16,
                Damage = 60, Hitstun = 26, HitboxHalfExtents = FixVec3.Zero, Knockback = V(14, 8), JuggleCost = 3, DestructionPower = 60,
                Flags = MoveFlags.WallBounce,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(50), Lifetime = 40, HalfExtents = V(40, 35, 50), SpawnOffset = V(60, 170, 0) } });
            h.Add(new MoveDef { Name = "Missile Barrage", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Forward,
                Startup = 14, Active = 1, Recovery = 18, Damage = 30, Hitstun = 20, HitboxHalfExtents = FixVec3.Zero, Knockback = V(6, 12),
                JuggleCost = 2, DestructionPower = 40,
                Projectile = new ProjectileSpec { Speed = Fix.Cents(40), Rise = Fix.Cents(10), Lifetime = 40, Ricochets = 4 } });
            h.Add(new MoveDef { Name = "Gatling Sweep", Tier = MoveTier.Special, Input = MoveInput.Special, Direction = StickDir.Down,
                Startup = 10, Active = 24, Recovery = 12, Damage = 12, Hitstun = 16, Hitstop = 2, HitboxOffset = V(300, 110),
                HitboxHalfExtents = V(250, 40, 50), Knockback = V(2, 6), MaxHits = 6, RehitInterval = 4, JuggleCost = 1, DestructionPower = 15 });
            return h;
        }

        public static HeroDef BuildTrainingDummy() => new HeroDef
        {
            Name = TrainingDummy,
            Brain = BrainType.None,
            MaxHealth = 99999,
            AirJumps = 0,
        };

        public static HeroDef BuildHydraTrooper()
        {
            var h = new HeroDef { Name = HydraTrooper, Brain = BrainType.Grunt, MaxHealth = 300, WalkSpeed = Fix.Cents(5), AirJumps = 0 };
            h.Add(new MoveDef { Name = "Trooper Jab", Tier = MoveTier.Light, Input = MoveInput.Light, Startup = 8, Active = 3, Recovery = 20,
                Damage = 40, Hitstun = 18, HitboxOffset = V(75, 110), HitboxHalfExtents = V(50, 35, 45), Knockback = V(6, 0) });
            h.Add(new MoveDef { Name = "Rifle Butt", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Startup = 14, Active = 3, Recovery = 24,
                Damage = 70, Hitstun = 24, HitboxOffset = V(80, 110), HitboxHalfExtents = V(55, 40, 45), Knockback = V(14, 6) });
            return h;
        }

        public static HeroDef BuildHydraHeavy()
        {
            var h = new HeroDef
            {
                Name = HydraHeavy, Brain = BrainType.Brute, MaxHealth = 800, Armor = 3, WalkSpeed = Fix.Cents(4), AirJumps = 0,
                BodyHalfExtents = V(55, 110, 45),
            };
            h.Add(new MoveDef { Name = "Heavy Swing", Tier = MoveTier.Heavy, Input = MoveInput.Heavy, Startup = 16, Active = 4, Recovery = 28,
                Damage = 90, Hitstun = 28, Hitstop = 8, HitboxOffset = V(110, 120), HitboxHalfExtents = V(70, 50, 55), Knockback = V(18, 10),
                DestructionPower = 40, Flags = MoveFlags.SuperArmor });
            return h;
        }
    }
}
