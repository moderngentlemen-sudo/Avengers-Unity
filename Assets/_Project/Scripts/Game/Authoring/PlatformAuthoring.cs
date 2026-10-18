using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>A walkable floor. Its BoxCollider's top face is the floor height.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class PlatformAuthoring : MonoBehaviour
    {
        [Tooltip("Solid floors can't be dropped or jumped through. Leave off for one-way upper floors.")]
        [SerializeField] bool solid;
        [Tooltip("Optional. When this destructible breaks, the floor disappears and everyone on it falls.")]
        [SerializeField] DestructibleAuthoring destructible;

        public DestructibleAuthoring Destructible => destructible;

        public void Configure(bool isSolid, DestructibleAuthoring linked)
        {
            solid = isSolid;
            destructible = linked;
        }

        public PlatformDef ToDef(int destructibleIndex)
        {
            var b = SimConvert.WorldBounds(GetComponent<BoxCollider>());
            return new PlatformDef
            {
                Name = name,
                MinX = SimConvert.ToFix(b.min.x),
                MaxX = SimConvert.ToFix(b.max.x),
                MinZ = SimConvert.ToFix(b.min.z),
                MaxZ = SimConvert.ToFix(b.max.z),
                Top = SimConvert.ToFix(b.max.y),
                Solid = solid,
                Destructible = destructibleIndex,
            };
        }
    }
}
