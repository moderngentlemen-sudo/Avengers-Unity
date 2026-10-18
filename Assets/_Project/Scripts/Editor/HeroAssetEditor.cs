using System.Collections.Generic;
using System.Linq;
using Reforged.Game;
using Reforged.Sim;
using UnityEditor;
using UnityEngine;

namespace Reforged.EditorTools
{
    /// <summary>
    /// HeroAsset inspector: collects the model's clips and flags any move without an animation
    /// (or any animation whose length disagrees with the move's frame data).
    /// </summary>
    [CustomEditor(typeof(HeroAsset))]
    public sealed class HeroAssetEditor : Editor
    {
        const string CharacterRoot = "Assets/_Project/Art/Characters/";

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var hero = (HeroAsset)target;
            EditorGUILayout.Space();
            if (GUILayout.Button("Find Model From Blender Pipeline")) AssignPipelineModel(hero);
            using (new EditorGUI.DisabledScope(hero.modelPrefab == null))
                if (GUILayout.Button("Collect Clips From Model")) CollectClips(hero);
            foreach (var problem in Audit(hero))
                EditorGUILayout.HelpBox(problem, MessageType.Warning);
        }

        public static string ModelPath(string heroName)
        {
            var slug = heroName.ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
            return CharacterRoot + slug + "/" + slug + ".fbx";
        }

        public static bool AssignPipelineModel(HeroAsset hero)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath(hero.heroName));
            if (model == null) return false;
            Undo.RecordObject(hero, "Assign model");
            hero.modelPrefab = model;
            CollectClips(hero);
            return true;
        }

        public static void CollectClips(HeroAsset hero)
        {
            var path = AssetDatabase.GetAssetPath(hero.modelPrefab);
            Undo.RecordObject(hero, "Collect clips");
            hero.clips = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<AnimationClip>()
                .Where(c => !c.name.StartsWith("__preview__"))
                .OrderBy(c => c.name)
                .ToList();
            EditorUtility.SetDirty(hero);
        }

        public static IEnumerable<string> Audit(HeroAsset hero)
        {
            if (hero.modelPrefab == null) yield break;
            var byName = new Dictionary<string, AnimationClip>();
            foreach (var c in hero.clips)
                if (c != null) byName[SimAnimationDriver.CleanName(c.name)] = c;
            foreach (var move in hero.moves)
            {
                if (!byName.TryGetValue(move.name, out var clip))
                {
                    yield return "No animation for move '" + move.name + "'";
                    continue;
                }
                int frames = Mathf.RoundToInt(clip.length * SimConstants.TicksPerSecond);
                int total = move.startup + move.active + move.recovery;
                if (frames != total)
                    yield return "'" + move.name + "' animation is " + frames + " frames; the move is " + total;
            }
            foreach (var mode in new[] { "Idle", "Walk", "Hitstun", "Juggle", "Knockdown" })
                if (!byName.ContainsKey(mode)) yield return "No '" + mode + "' animation";
        }
    }
}
