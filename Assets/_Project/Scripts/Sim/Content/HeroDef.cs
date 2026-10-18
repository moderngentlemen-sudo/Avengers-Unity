using System;
using System.Collections.Generic;

namespace Reforged.Sim
{
    public enum BrainType : byte { Player, None, Grunt, Brute }

    /// <summary>Immutable character data: stats plus the full move list. Shared by heroes, assists and enemies.</summary>
    public sealed class HeroDef
    {
        public string Name;
        public BrainType Brain = BrainType.Player;
        public int MaxHealth = 1000;
        /// <summary>Hits absorbed before staggering. Only Heavy-tier or higher hits wear it down.</summary>
        public int Armor;
        public Fix WalkSpeed = Fix.Cents(8);
        public Fix DepthSpeed = Fix.Cents(6);
        public Fix JumpVelocity = Fix.Cents(28);
        public Fix Gravity = Fix.Ratio(16, 1000);
        public int AirJumps = 1;
        public FixVec3 BodyHalfExtents = FixVec3.Cents(35, 90, 30);

        /// <summary>Moves used when this hero is called as an assist: neutral, forward, down.</summary>
        public string[] AssistMoves = new string[0];
        /// <summary>Cameo that can only be called as an assist (Nick Fury, War Machine...). Never playable.</summary>
        public bool AssistOnly;
        /// <summary>D-pad left/right cycles a tool state in [-range, range] (Cap: shield angle).</summary>
        public int ToolCycleRange;
        /// <summary>Gauge cost of the D-pad down tool charge (Cap: extra shield ricochet). 0 = none.</summary>
        public int ToolChargeCost;

        public readonly List<MoveDef> Moves = new List<MoveDef>();

        public int Index { get; internal set; }
        public int[] AssistMoveIndices { get; private set; } = new int[0];

        public Fix Height => BodyHalfExtents.Y * 2;

        public MoveDef Add(MoveDef move)
        {
            Moves.Add(move);
            return move;
        }

        /// <summary>Exact direction match first, then the neutral version of the same command.</summary>
        public int FindMove(MoveInput input, StickDir dir, bool aerial)
        {
            for (int i = 0; i < Moves.Count; i++)
            {
                var m = Moves[i];
                if (m.Input == input && m.Aerial == aerial && m.Direction == dir) return i;
            }
            if (dir == StickDir.Neutral) return -1;
            for (int i = 0; i < Moves.Count; i++)
            {
                var m = Moves[i];
                if (m.Input == input && m.Aerial == aerial && m.Direction == StickDir.Neutral) return i;
            }
            return -1;
        }

        public int FindMoveByName(string name)
        {
            for (int i = 0; i < Moves.Count; i++)
                if (Moves[i].Name == name) return i;
            return -1;
        }

        internal void Resolve(int index)
        {
            Index = index;
            for (int i = 0; i < Moves.Count; i++)
            {
                var m = Moves[i];
                m.Index = i;
                if (m.CancelStart == 0) m.CancelStart = m.ActiveStart;
                if (m.CancelEnd == 0) m.CancelEnd = m.TotalFrames;
                m.ChainNextIndex = -1;
                if (!string.IsNullOrEmpty(m.ChainNext))
                {
                    m.ChainNextIndex = FindMoveByName(m.ChainNext);
                    if (m.ChainNextIndex < 0) throw new InvalidOperationException(Name + ": unknown chain target " + m.ChainNext);
                }
            }
            var assists = new int[AssistMoves.Length];
            for (int i = 0; i < assists.Length; i++)
            {
                assists[i] = FindMoveByName(AssistMoves[i]);
                if (assists[i] < 0) throw new InvalidOperationException(Name + ": unknown assist move " + AssistMoves[i]);
            }
            AssistMoveIndices = assists;
        }
    }

    public sealed class ContentDatabase
    {
        public readonly List<HeroDef> Heroes = new List<HeroDef>();

        public HeroDef this[int index] => Heroes[index];

        public int Add(HeroDef hero)
        {
            Heroes.Add(hero);
            return Heroes.Count - 1;
        }

        public int IndexOf(string name)
        {
            for (int i = 0; i < Heroes.Count; i++)
                if (Heroes[i].Name == name) return i;
            return -1;
        }

        /// <summary>Resolves names to indices. Call once after all heroes are added.</summary>
        public ContentDatabase Resolve()
        {
            for (int i = 0; i < Heroes.Count; i++) Heroes[i].Resolve(i);
            return this;
        }
    }
}
