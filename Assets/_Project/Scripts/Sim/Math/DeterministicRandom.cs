namespace Reforged.Sim
{
    /// <summary>Xorshift32 RNG whose entire state is one uint, so it snapshots and rolls back trivially.</summary>
    public sealed class DeterministicRandom
    {
        public uint State;

        public DeterministicRandom(uint seed) { State = seed == 0 ? 0x9E3779B9u : seed; }

        public uint NextUInt()
        {
            uint x = State;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            State = x;
            return x;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            uint span = (uint)(maxExclusive - minInclusive);
            return span == 0 ? minInclusive : minInclusive + (int)(NextUInt() % span);
        }

        public bool Chance(int percent) => Range(0, 100) < percent;
    }
}
