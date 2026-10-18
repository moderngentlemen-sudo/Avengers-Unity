namespace Reforged.Sim
{
    /// <summary>
    /// The destruction test room used by the vertical slice and the tests:
    ///
    ///   y=4   [ Weak Floor (breakable)  ][ Upper Floor ]
    ///   y=0   [ Ground ......................................... | Drywall (breakable) | ... ]
    ///        x=-20      x=0          x=12          x=20        x=24                     x=40
    ///
    /// plus a crate and a parked car that break into throwable debris.
    /// </summary>
    public static class GrayboxStages
    {
        public const string Ground = "Ground";
        public const string WeakFloor = "Weak Floor";
        public const string UpperFloor = "Upper Floor";
        public const string Drywall = "Drywall";
        public const string Crate = "Crate";
        public const string Car = "Parked Car";

        public static StageDef TestRoom()
        {
            var s = new StageDef
            {
                Name = "Graybox Test Room",
                MinX = -20, MaxX = 40,
                MinZ = Fix.Cents(-300), MaxZ = Fix.Cents(300),
            };

            int weakFloor = s.AddDestructible(new DestructibleDef
            {
                Name = WeakFloor, Tier = DestructionTier.Structural,
                Center = FixVec3.Cents(600, 385, 0), HalfExtents = FixVec3.Cents(600, 15, 300),
                Health = 120, DamageStages = 3, DebrisCount = 2, DebrisWeight = 2,
            });
            int drywall = s.AddDestructible(new DestructibleDef
            {
                Name = Drywall, Tier = DestructionTier.Tactical,
                Center = FixVec3.Cents(2400, 200, 0), HalfExtents = FixVec3.Cents(15, 200, 300),
                Health = 80, DamageStages = 2, DebrisCount = 1, DebrisWeight = 1,
            });
            s.AddDestructible(new DestructibleDef
            {
                Name = Crate, Tier = DestructionTier.Cosmetic,
                Center = FixVec3.Cents(-600, 50, 0), HalfExtents = FixVec3.Cents(50, 50, 50),
                Health = 40, DamageStages = 1, DebrisCount = 1, DebrisWeight = 1,
            });
            s.AddDestructible(new DestructibleDef
            {
                Name = Car, Tier = DestructionTier.Tactical,
                Center = FixVec3.Cents(3200, 75, 100), HalfExtents = FixVec3.Cents(110, 75, 60),
                Health = 150, DamageStages = 3, DebrisCount = 2, DebrisWeight = 2,
            });

            s.Platforms.Add(new PlatformDef { Name = Ground, MinX = -20, MaxX = 40, MinZ = s.MinZ, MaxZ = s.MaxZ, Top = 0, Solid = true });
            s.Platforms.Add(new PlatformDef { Name = WeakFloor, MinX = 0, MaxX = 12, MinZ = s.MinZ, MaxZ = s.MaxZ, Top = 4, Destructible = weakFloor });
            s.Platforms.Add(new PlatformDef { Name = UpperFloor, MinX = 12, MaxX = 20, MinZ = s.MinZ, MaxZ = s.MaxZ, Top = 4 });

            s.Walls.Add(new WallDef { Name = Drywall, X = 24, MinY = 0, MaxY = 4, Destructible = drywall });
            return s;
        }
    }
}
