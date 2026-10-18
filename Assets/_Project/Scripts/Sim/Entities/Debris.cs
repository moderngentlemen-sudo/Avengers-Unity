using System.IO;

namespace Reforged.Sim
{
    public enum DebrisMode : byte { Loose, Resting, Held, Flying }

    /// <summary>
    /// Interactive debris: pick it up, throw it, or bat it mid-combo. Only gameplay debris lives in
    /// the sim; the hundreds of cosmetic chunks are view-side physics and are never rolled back.
    /// </summary>
    public sealed class Debris
    {
        public int Id;
        public FixVec3 Pos, Vel;
        public byte Weight;
        public DebrisMode Mode;
        public short SupportPlatform = -1;
        public int HolderId = -1;
        public int ThrowerId = -1;
        public byte ThrowerTeam;
        public sbyte ThrowerSlot = -1;
        public int LastHitId = -1;
        public bool Active;

        public FixVec3 HalfExtents => Weight >= 2 ? FixVec3.Cents(60, 45, 45) : FixVec3.Cents(35, 30, 35);

        public void Write(BinaryWriter w)
        {
            w.Write(Id); w.Write(Pos); w.Write(Vel); w.Write(Weight); w.Write((byte)Mode); w.Write(SupportPlatform);
            w.Write(HolderId); w.Write(ThrowerId); w.Write(ThrowerTeam); w.Write(ThrowerSlot); w.Write(LastHitId); w.Write(Active);
        }

        public static Debris Read(BinaryReader r) => new Debris
        {
            Id = r.ReadInt32(), Pos = r.ReadVec(), Vel = r.ReadVec(), Weight = r.ReadByte(), Mode = (DebrisMode)r.ReadByte(),
            SupportPlatform = r.ReadInt16(), HolderId = r.ReadInt32(), ThrowerId = r.ReadInt32(), ThrowerTeam = r.ReadByte(),
            ThrowerSlot = r.ReadSByte(), LastHitId = r.ReadInt32(), Active = r.ReadBoolean(),
        };
    }
}
