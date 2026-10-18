using System;

namespace Reforged.Sim
{
    /// <summary>
    /// GGPO-style rollback over SimWorld. Missing remote inputs are predicted (repeat the last
    /// confirmed input). When a late input disagrees with the prediction, the world is restored to
    /// the snapshot from that frame and re-simulated up to the present.
    ///
    /// Local play goes through this class too (every input is confirmed on time, so it never rolls
    /// back), which keeps one code path for offline and online play.
    /// </summary>
    public sealed class RollbackSession
    {
        public const int MaxRollbackFrames = 8;
        const int HistorySize = 64;

        readonly SimWorld world;
        readonly int playerCount;
        readonly InputFrame[,] inputs;
        readonly int[,] confirmedFrame;
        readonly InputFrame[,] used;
        readonly byte[][] snapshots = new byte[HistorySize][];
        readonly InputFrame[] lastConfirmed;
        readonly int[] lastConfirmedFrame;
        readonly InputFrame[] stepInputs;
        int rollbackTo = -1;

        public RollbackSession(SimWorld world, int playerCount)
        {
            this.world = world;
            this.playerCount = playerCount;
            inputs = new InputFrame[HistorySize, playerCount];
            confirmedFrame = new int[HistorySize, playerCount];
            used = new InputFrame[HistorySize, playerCount];
            lastConfirmed = new InputFrame[playerCount];
            lastConfirmedFrame = new int[playerCount];
            stepInputs = new InputFrame[playerCount];
            for (int i = 0; i < HistorySize; i++)
                for (int p = 0; p < playerCount; p++) confirmedFrame[i, p] = -1;
            for (int p = 0; p < playerCount; p++) lastConfirmedFrame[p] = -1;
        }

        public SimWorld World => world;
        public int Frame => world.Frame;
        public int RollbacksPerformed { get; private set; }
        public int FramesResimulated { get; private set; }

        /// <summary>Records a confirmed input for <paramref name="player"/> at <paramref name="frame"/>.</summary>
        public void AddInput(int player, int frame, InputFrame input)
        {
            if (frame < world.Frame - MaxRollbackFrames)
                throw new InvalidOperationException("Input for frame " + frame + " arrived too late to roll back (now " + world.Frame + ")");
            if (frame >= world.Frame + HistorySize - MaxRollbackFrames)
                throw new InvalidOperationException("Input for frame " + frame + " is too far ahead");

            int i = frame % HistorySize;
            inputs[i, player] = input;
            confirmedFrame[i, player] = frame;
            if (frame > lastConfirmedFrame[player])
            {
                lastConfirmedFrame[player] = frame;
                lastConfirmed[player] = input;
            }
            if (frame < world.Frame && !used[i, player].Equals(input))
                rollbackTo = rollbackTo < 0 ? frame : Math.Min(rollbackTo, frame);
        }

        /// <summary>Applies any pending rollback, then simulates one new frame.</summary>
        public void Advance()
        {
            Resimulate();
            StepFrame(world.Frame);
        }

        /// <summary>Applies a pending rollback without advancing past the current frame.</summary>
        public void Resimulate()
        {
            if (rollbackTo < 0) return;
            int target = world.Frame;
            world.LoadState(snapshots[rollbackTo % HistorySize]);
            for (int f = rollbackTo; f < target; f++)
            {
                StepFrame(f);
                FramesResimulated++;
            }
            rollbackTo = -1;
            RollbacksPerformed++;
        }

        void StepFrame(int frame)
        {
            int i = frame % HistorySize;
            snapshots[i] = world.SaveState();
            for (int p = 0; p < playerCount; p++)
            {
                stepInputs[p] = confirmedFrame[i, p] == frame ? inputs[i, p] : Predict(p, frame);
                used[i, p] = stepInputs[p];
            }
            world.Step(stepInputs);
        }

        InputFrame Predict(int player, int frame)
        {
            // Repeat the newest confirmed input at or before this frame.
            for (int f = frame; f >= 0 && f > frame - HistorySize + 1; f--)
                if (confirmedFrame[f % HistorySize, player] == f) return inputs[f % HistorySize, player];
            return lastConfirmedFrame[player] < frame ? lastConfirmed[player] : default(InputFrame);
        }
    }
}
