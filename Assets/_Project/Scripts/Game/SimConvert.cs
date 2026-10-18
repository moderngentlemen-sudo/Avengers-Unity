using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>The only bridge between Unity floats and simulation fixed-point.</summary>
    public static class SimConvert
    {
        public static Vector3 ToUnity(FixVec3 v) => new Vector3(v.X.ToFloat(), v.Y.ToFloat(), v.Z.ToFloat());

        /// <summary>Authoring-time only. Rounds to millimetres so every machine builds identical content.</summary>
        public static Fix ToFix(float v) => Fix.Ratio(Mathf.RoundToInt(v * 1000f), 1000);

        public static FixVec3 ToFix(Vector3 v) => new FixVec3(ToFix(v.x), ToFix(v.y), ToFix(v.z));

        /// <summary>World-space bounds of an axis-aligned BoxCollider without needing physics to be synced.</summary>
        public static Bounds WorldBounds(BoxCollider box)
        {
            var t = box.transform;
            var size = Vector3.Scale(box.size, t.lossyScale);
            return new Bounds(t.TransformPoint(box.center), new Vector3(Mathf.Abs(size.x), Mathf.Abs(size.y), Mathf.Abs(size.z)));
        }
    }
}
