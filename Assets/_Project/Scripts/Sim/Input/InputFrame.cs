using System;

namespace Reforged.Sim
{
    /// <summary>Logical buttons. Every physical button on the pad maps to exactly one of these.</summary>
    [Flags]
    public enum Buttons : uint
    {
        None = 0,
        Light = 1 << 0,      // X / Square
        Heavy = 1 << 1,      // Y / Triangle
        Jump = 1 << 2,       // A / Cross
        Special = 1 << 3,    // B / Circle
        Grab = 1 << 4,       // RB / R1   (throws, debris pickup)
        Block = 1 << 5,      // LB / L1   (tap on impact = parry)
        HeroMod = 1 << 6,    // RT / R2   (hold: Hero Specials, Super, Ultimate II)
        Assist = 1 << 7,     // LT / L2   (Assist Callout / Combo Relay; RT+LT = Super)
        Ultimate = 1 << 8,   // L3 + R3   (Ultimate I; with RT = Ultimate II; full Assemble meter = Team vote)
        ToolUp = 1 << 9,     // D-pad up: taunt
        ToolDown = 1 << 10,  // D-pad down: hero tool (charge, size, density...)
        ToolLeft = 1 << 11,  // D-pad left: hero tool cycle
        ToolRight = 1 << 12, // D-pad right: hero tool cycle
        Ping = 1 << 13,      // View / Select: co-op callout ping
    }

    /// <summary>One tick of input for one player. This is the only thing sent over the network.</summary>
    [Serializable]
    public struct InputFrame : IEquatable<InputFrame>
    {
        public const int ButtonCount = 14;
        public const int StickDeadzone = 24;
        public const int StickCommandThreshold = 64;
        public const int FlickThreshold = 80;

        public Buttons Held;
        /// <summary>Left stick, -127..127. MoveZ is depth: positive is into the screen ("up").</summary>
        public sbyte MoveX, MoveZ;
        /// <summary>Right stick, -127..127. Flicks trigger evades; holding aims ranged moves.</summary>
        public sbyte AimX, AimY;

        public bool Has(Buttons b) => (Held & b) != 0;

        public static InputFrame Of(Buttons held, int moveX = 0, int moveZ = 0, int aimX = 0, int aimY = 0)
        {
            return new InputFrame
            {
                Held = held,
                MoveX = ClampAxis(moveX),
                MoveZ = ClampAxis(moveZ),
                AimX = ClampAxis(aimX),
                AimY = ClampAxis(aimY),
            };
        }

        public static sbyte ClampAxis(int v) => (sbyte)(v < -127 ? -127 : v > 127 ? 127 : v);

        /// <summary>Bit index of a single-flag button, used to index per-button press history.</summary>
        public static int IndexOf(Buttons single)
        {
            uint v = (uint)single;
            for (int i = 0; i < ButtonCount; i++)
                if (v == 1u << i) return i;
            throw new ArgumentException("Expected exactly one button flag", nameof(single));
        }

        public bool Equals(InputFrame o) => Held == o.Held && MoveX == o.MoveX && MoveZ == o.MoveZ && AimX == o.AimX && AimY == o.AimY;
        public override bool Equals(object obj) => obj is InputFrame o && Equals(o);
        public override int GetHashCode() => (int)Held ^ (MoveX << 14) ^ (MoveZ << 20) ^ (AimX << 8) ^ (AimY << 26);
    }
}
