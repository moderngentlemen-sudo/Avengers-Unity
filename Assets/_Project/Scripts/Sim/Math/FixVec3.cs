using System;

namespace Reforged.Sim
{
    /// <summary>
    /// Fixed-point vector. X runs along the scroll direction, Y is up, Z is belt-scroller depth
    /// (positive Z is into the screen).
    /// </summary>
    [Serializable]
    public readonly struct FixVec3 : IEquatable<FixVec3>
    {
        public readonly Fix X, Y, Z;

        public FixVec3(Fix x, Fix y, Fix z) { X = x; Y = y; Z = z; }

        public static readonly FixVec3 Zero = new FixVec3(Fix.Zero, Fix.Zero, Fix.Zero);

        /// <summary>Hundredths shorthand used by content tables.</summary>
        public static FixVec3 Cents(int x, int y, int z) => new FixVec3(Fix.Cents(x), Fix.Cents(y), Fix.Cents(z));

        public static FixVec3 operator +(FixVec3 a, FixVec3 b) => new FixVec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static FixVec3 operator -(FixVec3 a, FixVec3 b) => new FixVec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static FixVec3 operator *(FixVec3 a, Fix s) => new FixVec3(a.X * s, a.Y * s, a.Z * s);
        public static FixVec3 operator *(FixVec3 a, int s) => new FixVec3(a.X * s, a.Y * s, a.Z * s);
        public static bool operator ==(FixVec3 a, FixVec3 b) => a.Equals(b);
        public static bool operator !=(FixVec3 a, FixVec3 b) => !a.Equals(b);

        public FixVec3 WithX(Fix x) => new FixVec3(x, Y, Z);
        public FixVec3 WithY(Fix y) => new FixVec3(X, y, Z);
        public FixVec3 WithZ(Fix z) => new FixVec3(X, Y, z);

        public Fix MaxAbs => Fix.Max(Fix.Abs(X), Fix.Max(Fix.Abs(Y), Fix.Abs(Z)));

        /// <summary>
        /// Scales the vector so its largest component equals <paramref name="length"/>. A cheap,
        /// deterministic stand-in for normalize (no square roots needed).
        /// </summary>
        public FixVec3 WithMaxNorm(Fix length)
        {
            var m = MaxAbs;
            if (m.Raw == 0) return Zero;
            return new FixVec3(X * length / m, Y * length / m, Z * length / m);
        }

        public bool Equals(FixVec3 other) => X == other.X && Y == other.Y && Z == other.Z;
        public override bool Equals(object obj) => obj is FixVec3 other && Equals(other);
        public override int GetHashCode() => X.Raw * 73856093 ^ Y.Raw * 19349663 ^ Z.Raw * 83492791;
        public override string ToString() => "(" + X + ", " + Y + ", " + Z + ")";
    }
}
