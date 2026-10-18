using System.Collections.Generic;
using System.IO;

namespace Reforged.Sim
{
    /// <summary>
    /// Cosmetic props only make debris and style. Tactical pieces (walls, cover) change routes.
    /// Structural pieces (floors) move the fight to another level of the stage.
    /// </summary>
    public enum DestructionTier : byte { Cosmetic, Tactical, Structural }

    public sealed class DestructibleDef
    {
        public string Name;
        public DestructionTier Tier;
        public FixVec3 Center, HalfExtents;
        public int Health = 50;
        /// <summary>Visual damage states before breaking; the view swaps meshes on each.</summary>
        public int DamageStages = 2;
        /// <summary>Interactive (gameplay) debris pieces spawned on break. Cosmetic chunks are view-only.</summary>
        public int DebrisCount;
        public byte DebrisWeight = 1;
    }

    /// <summary>A walkable floor slab. Non-solid platforms are one-way: you can jump up through them.</summary>
    public sealed class PlatformDef
    {
        public string Name;
        public Fix MinX, MaxX, MinZ, MaxZ, Top;
        public bool Solid;
        public int Destructible = -1;

        public bool Contains(Fix x, Fix z) => x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;
    }

    /// <summary>A vertical plane at X spanning the full stage depth between MinY and MaxY.</summary>
    public sealed class WallDef
    {
        public string Name;
        public Fix X, MinY, MaxY;
        public int Destructible = -1;
    }

    public sealed class StageDef
    {
        public string Name;
        public Fix MinX, MaxX, MinZ, MaxZ;
        public readonly List<PlatformDef> Platforms = new List<PlatformDef>();
        public readonly List<WallDef> Walls = new List<WallDef>();
        public readonly List<DestructibleDef> Destructibles = new List<DestructibleDef>();

        public int AddDestructible(DestructibleDef d)
        {
            Destructibles.Add(d);
            return Destructibles.Count - 1;
        }

        public int FindDestructible(string name)
        {
            for (int i = 0; i < Destructibles.Count; i++)
                if (Destructibles[i].Name == name) return i;
            return -1;
        }

        public int FindPlatform(string name)
        {
            for (int i = 0; i < Platforms.Count; i++)
                if (Platforms[i].Name == name) return i;
            return -1;
        }
    }

    /// <summary>The mutable half of a destructible; lives in the snapshot.</summary>
    public sealed class DestructibleState
    {
        public int Health;
        public byte Stage;
        public bool Broken;

        public void Write(BinaryWriter w) { w.Write(Health); w.Write(Stage); w.Write(Broken); }
        public void Read(BinaryReader r) { Health = r.ReadInt32(); Stage = r.ReadByte(); Broken = r.ReadBoolean(); }
    }
}
