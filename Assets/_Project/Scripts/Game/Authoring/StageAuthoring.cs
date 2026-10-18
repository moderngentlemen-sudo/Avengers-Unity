using System.Collections.Generic;
using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// Root of a stage. Converts the platforms, walls and destructibles beneath it into a StageDef
    /// for the simulation. Hierarchy order defines indices, so the result is the same on every machine.
    /// </summary>
    public sealed class StageAuthoring : MonoBehaviour
    {
        [SerializeField] string stageName = "Stage";
        [SerializeField] float minX = -20f, maxX = 40f;
        [SerializeField] float halfDepth = 3f;

        readonly List<DestructibleAuthoring> destructibles = new List<DestructibleAuthoring>();

        /// <summary>Destructible views indexed exactly like SimWorld.Destructibles.</summary>
        public IReadOnlyList<DestructibleAuthoring> Destructibles => destructibles;

        public void SetBounds(float min, float max, float depth)
        {
            minX = min;
            maxX = max;
            halfDepth = depth;
        }

        public StageDef Build()
        {
            var stage = new StageDef
            {
                Name = stageName,
                MinX = SimConvert.ToFix(minX),
                MaxX = SimConvert.ToFix(maxX),
                MinZ = SimConvert.ToFix(-halfDepth),
                MaxZ = SimConvert.ToFix(halfDepth),
            };

            destructibles.Clear();
            destructibles.AddRange(GetComponentsInChildren<DestructibleAuthoring>());
            foreach (var d in destructibles) stage.AddDestructible(d.ToDef());

            foreach (var p in GetComponentsInChildren<PlatformAuthoring>())
                stage.Platforms.Add(p.ToDef(destructibles.IndexOf(p.Destructible)));
            foreach (var w in GetComponentsInChildren<WallAuthoring>())
                stage.Walls.Add(w.ToDef(destructibles.IndexOf(w.Destructible)));
            return stage;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.6f);
            var center = new Vector3((minX + maxX) * 0.5f, 0f, 0f);
            Gizmos.DrawWireCube(center, new Vector3(maxX - minX, 0.05f, halfDepth * 2f));
        }
    }
}
