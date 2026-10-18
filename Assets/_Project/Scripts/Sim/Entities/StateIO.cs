using System.IO;

namespace Reforged.Sim
{
    /// <summary>Binary helpers for snapshotting fixed-point and input types.</summary>
    static class StateIO
    {
        public static void Write(this BinaryWriter w, Fix v) => w.Write(v.Raw);
        public static Fix ReadFix(this BinaryReader r) => Fix.FromRaw(r.ReadInt32());

        public static void Write(this BinaryWriter w, FixVec3 v) { w.Write(v.X.Raw); w.Write(v.Y.Raw); w.Write(v.Z.Raw); }
        public static FixVec3 ReadVec(this BinaryReader r) =>
            new FixVec3(Fix.FromRaw(r.ReadInt32()), Fix.FromRaw(r.ReadInt32()), Fix.FromRaw(r.ReadInt32()));

        public static void Write(this BinaryWriter w, InputFrame f)
        {
            w.Write((uint)f.Held); w.Write(f.MoveX); w.Write(f.MoveZ); w.Write(f.AimX); w.Write(f.AimY);
        }

        public static InputFrame ReadInput(this BinaryReader r) => new InputFrame
        {
            Held = (Buttons)r.ReadUInt32(),
            MoveX = r.ReadSByte(),
            MoveZ = r.ReadSByte(),
            AimX = r.ReadSByte(),
            AimY = r.ReadSByte(),
        };
    }
}
