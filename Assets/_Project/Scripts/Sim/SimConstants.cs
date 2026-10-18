namespace Reforged.Sim
{
    /// <summary>Global tuning. Distances are meters, velocities are meters per tick (60 ticks/s).</summary>
    public static class SimConstants
    {
        public const int TicksPerSecond = 60;
        public const int MaxPlayers = 4;

        public const int InputBufferFrames = 8;
        public const int ParryWindowFrames = 6;

        public const int MaxGauge = 300;
        public const int GaugePerBar = 100;
        public const int CancelGaugeCost = 10;

        public const int MaxJugglePoints = 40;
        public const int JuggleGravityPercentPerPoint = 4;
        public const int KnockdownFrames = 36;
        public const int GetupInvulnFrames = 20;
        public const int ParryStaggerFrames = 30;
        public const int ArmorBreakStunFrames = 40;
        public const int ArmorRegenFrames = 180;

        public const int EvadeFrames = 18;
        public const int EvadeInvulnFrames = 12;
        public const int TauntFrames = 40;
        public const int ThrowRecoveryFrames = 12;
        public const int DropThroughFrames = 12;

        public const int AssistCooldownFrames = 360;
        public const int RelayCooldownFrames = 180;
        public const int AssembleMeterMax = 1000;
        public const int AssembleVoteWindow = 60;

        public const int DestructibleKeyBase = 100000;
        public const int DebrisKeyBase = 200000;

        public static readonly Fix PlatformThickness = Fix.Cents(30);
        public static readonly Fix MaxFallSpeed = Fix.Cents(45);
        public static readonly Fix AirHitMinLift = Fix.Cents(8);
        public static readonly Fix GroundFriction = Fix.Ratio(3, 4);
        public static readonly Fix VelocitySnap = Fix.Ratio(2, 1000);
        public static readonly Fix DebrisGravity = Fix.Ratio(16, 1000);
        public static readonly Fix RelayRange = Fix.FromInt(5);
        public static readonly Fix RicochetRange = Fix.FromInt(9);
    }
}
