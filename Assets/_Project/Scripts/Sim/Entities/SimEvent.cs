namespace Reforged.Sim
{
    /// <summary>
    /// Things that happened this tick, for the presentation layer (VFX, camera, audio, voice barks).
    /// Events are not part of the snapshot: they are rebuilt every tick and never drive gameplay.
    /// </summary>
    public enum SimEventType : byte
    {
        MoveStarted, SuperStarted, UltimateStarted, TeamUltimate, AssembleVote,
        Hit, Blocked, Parry, ArmorHit, ArmorBreak, Launch, JuggleExhausted, KO,
        WallBounce, WallBreak, GroundBounce, CeilingBreakthrough, Knockdown, FloorCollapse,
        DestructibleDamaged, DestructibleBroken, EnvironmentalHit,
        DebrisSpawned, DebrisPickedUp, DebrisThrown, DebrisBatted,
        ProjectileSpawned, Ricochet, ProjectileCaught,
        AssistCalled, AssistNotReady, ComboRelay, ComboEnded, StyleRankUp,
        JumpCancel, LaneCancel, Taunt, ToolUsed, TeamBuff, Ping,
    }

    public struct SimEvent
    {
        public SimEventType Type;
        /// <summary>Usually the acting entity id.</summary>
        public int A;
        /// <summary>Usually the target entity id, slot or index; see the emitting code.</summary>
        public int B;
        public int Value;
        public FixVec3 Pos;

        public override string ToString() => Type + "(" + A + ", " + B + ", " + Value + ")";
    }
}
