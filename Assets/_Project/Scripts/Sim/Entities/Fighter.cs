using System.IO;

namespace Reforged.Sim
{
    public enum FighterMode : byte
    {
        Idle, Walk, Guard, Airborne, Attack, Recover, Evade, Taunt,
        Hitstun, Blockstun, Juggle, Knockdown, Dead, Despawned,
    }

    /// <summary>
    /// A hero, assist or enemy. All fields are plain values so the whole thing serializes into a
    /// rollback snapshot. Content (moves, stats) is referenced by index, never by object.
    /// </summary>
    public sealed class Fighter
    {
        public const int HitListCapacity = 16;
        public const int NeverPressed = int.MinValue / 2;

        public const byte BounceWall = 1, BounceGround = 2, BounceCeiling = 4, BounceOTG = 8;

        public int Id;
        public byte Team;
        public sbyte PlayerSlot = -1;
        /// <summary>Slot credited for this fighter's hits (a player's own slot, or the caller of an assist).</summary>
        public sbyte OwnerSlot = -1;
        public short HeroIndex;
        public bool IsAssist;

        public FixVec3 Pos, Vel;
        public sbyte Facing = 1;

        public FighterMode Mode;
        public int ModeFrame;
        public int Timer;

        public short MoveIndex = -1;
        public int MoveFrame;
        public bool MoveHasHit;
        public readonly int[] HitList = new int[HitListCapacity];
        public int HitListCount;

        public int Health, Gauge, Hitstop, InvulnFrames;

        public short GroundPlatform = -1;
        public short DropThroughPlatform = -1;
        public int DropThroughFrames;
        public byte AirJumpsUsed;

        // Combo state, tracked on the victim so a combo's count is shared by everyone hitting it.
        public int ComboHits, ComboDamage, JugglePoints;
        public sbyte ComboOwnerSlot = -1;
        public byte BouncesUsed, PendingBounce;
        public bool JuggleExhausted;

        public int ArmorLeft, ArmorRegenTimer, BuffFrames;
        public int HeldDebrisId = -1;
        public int ToolState, ToolCharge, AiCooldown;
        public int LastVictimId = -1;

        // Hero mechanic state (see SimWorld.Mechanics): Meter is heat / rage / density energy / Pym
        // particles; MechState is overheated flag, rage tier, density (0 normal, 1 phase, 2 diamond)
        // or size (-1 small, 0 normal, 1 giant).
        public int Meter, MechState, MechTimer, MechIdleFrames;
        public bool PhaseStrike;
        /// <summary>Shrunk by Ant-Man's Quantum Tunnel: tiny hurtbox, takes extra damage.</summary>
        public int ShrunkFrames;

        public InputFrame Input, PrevInput;
        public readonly int[] LastPress = new int[InputFrame.ButtonCount];
        public int LastEvadeFlick = NeverPressed;
        public sbyte EvadeDirX, EvadeDirZ;

        public Fighter()
        {
            for (int i = 0; i < LastPress.Length; i++) LastPress[i] = NeverPressed;
        }

        public bool IsGrounded => GroundPlatform >= 0;
        public bool IsAlive => Mode != FighterMode.Dead && Mode != FighterMode.Despawned;
        public bool IsStunned =>
            Mode == FighterMode.Hitstun || Mode == FighterMode.Blockstun ||
            Mode == FighterMode.Juggle || Mode == FighterMode.Knockdown;
        public bool IsPlayer => PlayerSlot >= 0;

        public bool InHitList(int key)
        {
            for (int i = 0; i < HitListCount; i++)
                if (HitList[i] == key) return true;
            return false;
        }

        public void AddHitList(int key)
        {
            if (HitListCount < HitListCapacity) HitList[HitListCount++] = key;
        }

        public void ResetCombo()
        {
            ComboHits = 0; ComboDamage = 0; JugglePoints = 0; ComboOwnerSlot = -1;
            BouncesUsed = 0; PendingBounce = 0; JuggleExhausted = false;
        }

        public void Write(BinaryWriter w)
        {
            w.Write(Id); w.Write(Team); w.Write(PlayerSlot); w.Write(OwnerSlot); w.Write(HeroIndex); w.Write(IsAssist);
            w.Write(Pos); w.Write(Vel); w.Write(Facing);
            w.Write((byte)Mode); w.Write(ModeFrame); w.Write(Timer);
            w.Write(MoveIndex); w.Write(MoveFrame); w.Write(MoveHasHit);
            w.Write(HitListCount);
            for (int i = 0; i < HitListCount; i++) w.Write(HitList[i]);
            w.Write(Health); w.Write(Gauge); w.Write(Hitstop); w.Write(InvulnFrames);
            w.Write(GroundPlatform); w.Write(DropThroughPlatform); w.Write(DropThroughFrames); w.Write(AirJumpsUsed);
            w.Write(ComboHits); w.Write(ComboDamage); w.Write(JugglePoints); w.Write(ComboOwnerSlot);
            w.Write(BouncesUsed); w.Write(PendingBounce); w.Write(JuggleExhausted);
            w.Write(ArmorLeft); w.Write(ArmorRegenTimer); w.Write(BuffFrames);
            w.Write(HeldDebrisId); w.Write(ToolState); w.Write(ToolCharge); w.Write(AiCooldown); w.Write(LastVictimId);
            w.Write(Meter); w.Write(MechState); w.Write(MechTimer); w.Write(MechIdleFrames); w.Write(PhaseStrike); w.Write(ShrunkFrames);
            w.Write(Input); w.Write(PrevInput);
            for (int i = 0; i < LastPress.Length; i++) w.Write(LastPress[i]);
            w.Write(LastEvadeFlick); w.Write(EvadeDirX); w.Write(EvadeDirZ);
        }

        public static Fighter Read(BinaryReader r)
        {
            var f = new Fighter();
            f.Id = r.ReadInt32(); f.Team = r.ReadByte(); f.PlayerSlot = r.ReadSByte(); f.OwnerSlot = r.ReadSByte();
            f.HeroIndex = r.ReadInt16(); f.IsAssist = r.ReadBoolean();
            f.Pos = r.ReadVec(); f.Vel = r.ReadVec(); f.Facing = r.ReadSByte();
            f.Mode = (FighterMode)r.ReadByte(); f.ModeFrame = r.ReadInt32(); f.Timer = r.ReadInt32();
            f.MoveIndex = r.ReadInt16(); f.MoveFrame = r.ReadInt32(); f.MoveHasHit = r.ReadBoolean();
            f.HitListCount = r.ReadInt32();
            for (int i = 0; i < f.HitListCount; i++) f.HitList[i] = r.ReadInt32();
            f.Health = r.ReadInt32(); f.Gauge = r.ReadInt32(); f.Hitstop = r.ReadInt32(); f.InvulnFrames = r.ReadInt32();
            f.GroundPlatform = r.ReadInt16(); f.DropThroughPlatform = r.ReadInt16(); f.DropThroughFrames = r.ReadInt32();
            f.AirJumpsUsed = r.ReadByte();
            f.ComboHits = r.ReadInt32(); f.ComboDamage = r.ReadInt32(); f.JugglePoints = r.ReadInt32(); f.ComboOwnerSlot = r.ReadSByte();
            f.BouncesUsed = r.ReadByte(); f.PendingBounce = r.ReadByte(); f.JuggleExhausted = r.ReadBoolean();
            f.ArmorLeft = r.ReadInt32(); f.ArmorRegenTimer = r.ReadInt32(); f.BuffFrames = r.ReadInt32();
            f.HeldDebrisId = r.ReadInt32(); f.ToolState = r.ReadInt32(); f.ToolCharge = r.ReadInt32();
            f.AiCooldown = r.ReadInt32(); f.LastVictimId = r.ReadInt32();
            f.Meter = r.ReadInt32(); f.MechState = r.ReadInt32(); f.MechTimer = r.ReadInt32(); f.MechIdleFrames = r.ReadInt32();
            f.PhaseStrike = r.ReadBoolean(); f.ShrunkFrames = r.ReadInt32();
            f.Input = r.ReadInput(); f.PrevInput = r.ReadInput();
            for (int i = 0; i < f.LastPress.Length; i++) f.LastPress[i] = r.ReadInt32();
            f.LastEvadeFlick = r.ReadInt32(); f.EvadeDirX = r.ReadSByte(); f.EvadeDirZ = r.ReadSByte();
            return f;
        }
    }
}
