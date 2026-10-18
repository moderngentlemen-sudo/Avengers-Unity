using System;
using System.Globalization;

namespace Reforged.Sim
{
    /// <summary>
    /// Q16.16 fixed-point number. Every gameplay value in the simulation uses this type so the sim is
    /// bit-identical on every machine, which rollback netcode depends on. Floats exist only in views.
    /// </summary>
    [Serializable]
    public readonly struct Fix : IEquatable<Fix>, IComparable<Fix>
    {
        public const int FractionalBits = 16;
        public const int OneRaw = 1 << FractionalBits;

        public readonly int Raw;

        Fix(int raw) { Raw = raw; }

        public static readonly Fix Zero = new Fix(0);
        public static readonly Fix One = new Fix(OneRaw);
        public static readonly Fix Half = new Fix(OneRaw >> 1);

        public static Fix FromRaw(int raw) => new Fix(raw);
        public static Fix FromInt(int value) => new Fix(value << FractionalBits);

        /// <summary>Exact rational constant: Ratio(3, 2) == 1.5. Use this instead of float literals.</summary>
        public static Fix Ratio(int numerator, int denominator) =>
            new Fix((int)(((long)numerator << FractionalBits) / denominator));

        /// <summary>Hundredths shorthand used by content tables: Cents(25) == 0.25.</summary>
        public static Fix Cents(int hundredths) => Ratio(hundredths, 100);

        public static implicit operator Fix(int value) => FromInt(value);

        public static Fix operator +(Fix a, Fix b) => new Fix(a.Raw + b.Raw);
        public static Fix operator -(Fix a, Fix b) => new Fix(a.Raw - b.Raw);
        public static Fix operator -(Fix a) => new Fix(-a.Raw);
        public static Fix operator *(Fix a, Fix b) => new Fix((int)(((long)a.Raw * b.Raw) >> FractionalBits));
        public static Fix operator /(Fix a, Fix b) => new Fix((int)(((long)a.Raw << FractionalBits) / b.Raw));
        public static Fix operator *(Fix a, int b) => new Fix(a.Raw * b);
        public static Fix operator /(Fix a, int b) => new Fix(a.Raw / b);

        public static bool operator ==(Fix a, Fix b) => a.Raw == b.Raw;
        public static bool operator !=(Fix a, Fix b) => a.Raw != b.Raw;
        public static bool operator <(Fix a, Fix b) => a.Raw < b.Raw;
        public static bool operator >(Fix a, Fix b) => a.Raw > b.Raw;
        public static bool operator <=(Fix a, Fix b) => a.Raw <= b.Raw;
        public static bool operator >=(Fix a, Fix b) => a.Raw >= b.Raw;

        public static Fix Abs(Fix v) => v.Raw < 0 ? new Fix(-v.Raw) : v;
        public static Fix Min(Fix a, Fix b) => a.Raw < b.Raw ? a : b;
        public static Fix Max(Fix a, Fix b) => a.Raw > b.Raw ? a : b;
        public static Fix Clamp(Fix v, Fix min, Fix max) => Min(Max(v, min), max);

        /// <summary>Moves toward zero and snaps to zero below the threshold. Plain multiplication
        /// floors negative values and would never reach zero.</summary>
        public static Fix Damp(Fix v, Fix factor, Fix snap)
        {
            var d = v * factor;
            return Abs(d) < snap ? Zero : d;
        }

        public int Sign => Raw > 0 ? 1 : Raw < 0 ? -1 : 0;
        public int FloorToInt => Raw >> FractionalBits;

        /// <summary>View-only conversion. Never feed the result back into the simulation.</summary>
        public float ToFloat() => Raw / (float)OneRaw;

        public bool Equals(Fix other) => Raw == other.Raw;
        public override bool Equals(object obj) => obj is Fix other && Raw == other.Raw;
        public override int GetHashCode() => Raw;
        public int CompareTo(Fix other) => Raw.CompareTo(other.Raw);
        public override string ToString() => ToFloat().ToString("0.###", CultureInfo.InvariantCulture);
    }
}
