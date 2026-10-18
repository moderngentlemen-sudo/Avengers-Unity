using System;

namespace Reforged.Sim
{
    /// <summary>Cancel hierarchy: a move that has connected can cancel into any move of a higher tier.</summary>
    public enum MoveTier : byte { Light, Heavy, Special, HeroSpecial, Super, Ultimate }

    /// <summary>The command that performs a move. Hero* inputs are face buttons with RT held.</summary>
    public enum MoveInput : byte
    {
        Light, Heavy, Special, Grab,
        HeroLight, HeroHeavy, HeroJump, HeroSpecial,
        Super,        // RT + LT
        UltimateI,    // L3 + R3
        UltimateII,   // RT + L3 + R3
    }

    /// <summary>Left-stick direction at the moment of input, relative to facing. Up/Down are depth.</summary>
    public enum StickDir : byte { Neutral, Forward, Back, Up, Down }

    [Flags]
    public enum MoveFlags : uint
    {
        None = 0,
        Launcher = 1 << 0,
        WallBounce = 1 << 1,       // first wall contact bounces (or smashes through a breakable wall)
        GroundBounce = 1 << 2,     // first landing bounces back up
        CeilingBounce = 1 << 3,    // victim smashes up through the floor above to the next stage level
        OTG = 1 << 4,              // can hit knocked-down enemies (once per combo)
        SuperArmor = 1 << 5,       // absorbs hits during startup/active
        Invulnerable = 1 << 6,     // invulnerable during startup/active
        Unblockable = 1 << 7,      // throws; also strips enemy armor
        FloorBreak = 1 << 8,       // landing during this move damages a breakable floor
        DirectionalThrow = 1 << 9, // throw direction follows the stick
    }

    public sealed class ProjectileSpec
    {
        public Fix Speed = Fix.Cents(40);
        public Fix Rise;
        public int Lifetime = 40;
        public int Ricochets;
        public bool ReturnsToOwner;
        public FixVec3 HalfExtents = FixVec3.Cents(40, 40, 50);
        public FixVec3 SpawnOffset = FixVec3.Cents(80, 110, 0);
    }

    /// <summary>
    /// Frame data for one move. Frames are 1-indexed: frames 1..Startup are startup, the next
    /// Active frames can hit, and the rest is recovery.
    /// </summary>
    public sealed class MoveDef
    {
        public string Name;
        public MoveTier Tier;
        public MoveInput Input;
        public StickDir Direction;
        public bool Aerial;

        public int Startup = 5, Active = 3, Recovery = 12;
        public int Damage = 40, Hitstun = 18, Hitstop = 5;
        public FixVec3 HitboxOffset = FixVec3.Cents(75, 110, 0);
        public FixVec3 HitboxHalfExtents = FixVec3.Cents(45, 35, 45);
        /// <summary>Velocity given to the victim. X is relative to the attacker's facing.</summary>
        public FixVec3 Knockback = FixVec3.Cents(4, 0, 0);
        /// <summary>Velocity given to the attacker on the first active frame (lunges, leaps, dives).</summary>
        public FixVec3 SelfVelocity;
        public int JuggleCost = 2;
        public int GaugeCost;
        public int GaugeGain = 8;
        public int DestructionPower;
        public int MaxHits = 1, RehitInterval;
        /// <summary>0 means the defaults: first active frame through the last frame.</summary>
        public int CancelStart, CancelEnd;
        public int LandingLag = 8;
        public string ChainNext;
        public MoveFlags Flags;
        public ProjectileSpec Projectile;
        /// <summary>Frames of team buff granted on the first active frame (support ultimates).</summary>
        public int TeamBuffFrames;

        public int Index { get; internal set; }
        public int ChainNextIndex { get; internal set; } = -1;

        public int TotalFrames => Startup + Active + Recovery;
        public int ActiveStart => Startup + 1;
        public int ActiveEnd => Startup + Active;
        public bool HasHitbox => HitboxHalfExtents.X.Raw > 0;
        public bool Has(MoveFlags f) => (Flags & f) != 0;
    }

    /// <summary>The part of a move (or projectile, debris, team ultimate) that matters when it connects.</summary>
    public readonly struct HitSpec
    {
        public readonly int Damage, Hitstun, Hitstop, JuggleCost, GaugeGain, StyleKey;
        public readonly FixVec3 Knockback;
        public readonly MoveFlags Flags;
        public readonly MoveTier Tier;

        public HitSpec(int damage, int hitstun, int hitstop, int juggleCost, int gaugeGain, int styleKey,
            FixVec3 knockback, MoveFlags flags, MoveTier tier)
        {
            Damage = damage; Hitstun = hitstun; Hitstop = hitstop; JuggleCost = juggleCost;
            GaugeGain = gaugeGain; StyleKey = styleKey; Knockback = knockback; Flags = flags; Tier = tier;
        }

        public static HitSpec From(MoveDef m, int styleKey) =>
            new HitSpec(m.Damage, m.Hitstun, m.Hitstop, m.JuggleCost, m.GaugeGain, styleKey, m.Knockback, m.Flags, m.Tier);
    }
}
