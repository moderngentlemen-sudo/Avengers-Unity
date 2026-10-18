using System.IO;

namespace Reforged.Sim
{
    public enum StyleRank : byte { D, C, B, A, S, SS, Assemble }

    /// <summary>Per-player state that outlives any one fighter: assist partner, style, score.</summary>
    public sealed class PlayerState
    {
        public const int RecentMoveCount = 4;
        public const int MaxStyle = 1000;
        static readonly int[] RankThresholds = { 0, 100, 200, 350, 500, 700, 900 };

        public int Slot;
        public int FighterId;
        public int HeroIndex;
        public int AssistHeroIndex = -1;
        public int AssistCooldown;
        public int StylePoints;
        public int StyleIdleFrames;
        public readonly int[] RecentMoves = new int[RecentMoveCount];
        public int RecentCursor;
        public int Score;
        public int BestCombo;
        public int AssembleVoteFrame = Fighter.NeverPressed;

        public PlayerState()
        {
            for (int i = 0; i < RecentMoves.Length; i++) RecentMoves[i] = int.MinValue;
        }

        public StyleRank Rank => RankFor(StylePoints);

        public static StyleRank RankFor(int points)
        {
            for (int i = RankThresholds.Length - 1; i > 0; i--)
                if (points >= RankThresholds[i]) return (StyleRank)i;
            return StyleRank.D;
        }

        public void Write(BinaryWriter w)
        {
            w.Write(Slot); w.Write(FighterId); w.Write(HeroIndex); w.Write(AssistHeroIndex); w.Write(AssistCooldown);
            w.Write(StylePoints); w.Write(StyleIdleFrames);
            for (int i = 0; i < RecentMoves.Length; i++) w.Write(RecentMoves[i]);
            w.Write(RecentCursor); w.Write(Score); w.Write(BestCombo); w.Write(AssembleVoteFrame);
        }

        public static PlayerState Read(BinaryReader r)
        {
            var p = new PlayerState
            {
                Slot = r.ReadInt32(), FighterId = r.ReadInt32(), HeroIndex = r.ReadInt32(), AssistHeroIndex = r.ReadInt32(),
                AssistCooldown = r.ReadInt32(), StylePoints = r.ReadInt32(), StyleIdleFrames = r.ReadInt32(),
            };
            for (int i = 0; i < p.RecentMoves.Length; i++) p.RecentMoves[i] = r.ReadInt32();
            p.RecentCursor = r.ReadInt32(); p.Score = r.ReadInt32(); p.BestCombo = r.ReadInt32(); p.AssembleVoteFrame = r.ReadInt32();
            return p;
        }
    }
}
