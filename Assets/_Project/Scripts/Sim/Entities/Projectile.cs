using System.IO;

namespace Reforged.Sim
{
    /// <summary>A thrown shield, repulsor blast, arrow... Its hit data comes from the move that spawned it.</summary>
    public sealed class Projectile
    {
        public int Id;
        public int OwnerId;
        public sbyte OwnerSlot;
        public byte Team;
        public short HeroIndex, MoveIndex;
        public FixVec3 Pos, Vel;
        public Fix Speed;
        public int Life;
        public int RicochetsLeft;
        public bool Returning;
        public int LastHitId = -1;
        public bool Active;

        public void Write(BinaryWriter w)
        {
            w.Write(Id); w.Write(OwnerId); w.Write(OwnerSlot); w.Write(Team); w.Write(HeroIndex); w.Write(MoveIndex);
            w.Write(Pos); w.Write(Vel); w.Write(Speed); w.Write(Life); w.Write(RicochetsLeft); w.Write(Returning);
            w.Write(LastHitId); w.Write(Active);
        }

        public static Projectile Read(BinaryReader r) => new Projectile
        {
            Id = r.ReadInt32(), OwnerId = r.ReadInt32(), OwnerSlot = r.ReadSByte(), Team = r.ReadByte(),
            HeroIndex = r.ReadInt16(), MoveIndex = r.ReadInt16(),
            Pos = r.ReadVec(), Vel = r.ReadVec(), Speed = r.ReadFix(), Life = r.ReadInt32(),
            RicochetsLeft = r.ReadInt32(), Returning = r.ReadBoolean(), LastHitId = r.ReadInt32(), Active = r.ReadBoolean(),
        };
    }
}
