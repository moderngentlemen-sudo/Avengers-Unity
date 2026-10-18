using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>A wall across the full stage depth at the BoxCollider's center X.</summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class WallAuthoring : MonoBehaviour
    {
        [Tooltip("Optional. A wall-bounce into a breakable wall smashes through it.")]
        [SerializeField] DestructibleAuthoring destructible;

        public DestructibleAuthoring Destructible => destructible;

        public void Configure(DestructibleAuthoring linked) => destructible = linked;

        public WallDef ToDef(int destructibleIndex)
        {
            var b = SimConvert.WorldBounds(GetComponent<BoxCollider>());
            return new WallDef
            {
                Name = name,
                X = SimConvert.ToFix(b.center.x),
                MinY = SimConvert.ToFix(b.min.y),
                MaxY = SimConvert.ToFix(b.max.y),
                Destructible = destructibleIndex,
            };
        }
    }
}
