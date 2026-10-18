using System.Collections.Generic;
using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// The roster the simulation is built from. Order matters: it defines hero indices, which
    /// are part of the snapshot, so every peer in an online match must use the same library.
    /// </summary>
    [CreateAssetMenu(menuName = "Reforged/Content Library", fileName = "ContentLibrary")]
    public sealed class ContentLibrary : ScriptableObject
    {
        public List<HeroAsset> heroes = new List<HeroAsset>();

        public ContentDatabase Build()
        {
            if (heroes.Count == 0) return GrayboxContent.Create();
            var db = new ContentDatabase();
            foreach (var h in heroes) db.Add(h.ToDef());
            return db.Resolve();
        }

        public HeroAsset Find(string heroName) => heroes.Find(h => h != null && h.heroName == heroName);
    }
}
