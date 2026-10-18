using UnityEditor;
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Reforged.EditorTools
{
    /// <summary>
    /// Import rules for the Blender pipeline (Tools/Blender):
    /// - Characters: generic rig, no animation compression (keeps "on 2s" holds crisp), clip names
    ///   stripped of the "Rig|" prefix so they match move names, locomotion clips loop.
    /// - Props: no animation; "*_fractured" chunks named *_cell_* get convex colliders and
    ///   rigidbodies so they shatter with physics.
    /// - All Art materials use the Reforged/Toon shader, keeping the Blender base color.
    /// </summary>
    public sealed class ReforgedModelPostprocessor : AssetPostprocessor
    {
        const string ArtRoot = "Assets/_Project/Art/";
        const string CharacterRoot = ArtRoot + "Characters/";
        const string PropRoot = ArtRoot + "Props/";
        static readonly string[] LoopingClips = { "Idle", "Walk", "Guard", "Airborne", "Juggle" };

        public static string CleanClipName(string name)
        {
            int bar = name.LastIndexOf('|');
            return bar >= 0 ? name.Substring(bar + 1) : name;
        }

        void OnPreprocessModel()
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            var importer = (ModelImporter)assetImporter;
            importer.useFileScale = true;
            importer.globalScale = 1f;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportViaMaterialDescription;

            if (assetPath.StartsWith(CharacterRoot))
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.importAnimation = true;
                importer.animationCompression = ModelImporterAnimationCompression.Off;
                importer.resampleCurves = false;
            }
            else if (assetPath.StartsWith(PropRoot))
            {
                importer.animationType = ModelImporterAnimationType.None;
                importer.importAnimation = false;
                importer.addCollider = false;
            }
        }

        void OnPreprocessAnimation()
        {
            if (!assetPath.StartsWith(CharacterRoot)) return;
            var importer = (ModelImporter)assetImporter;
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.name = CleanClipName(clip.takeName);
                clip.loopTime = System.Array.IndexOf(LoopingClips, clip.name) >= 0;
            }
            importer.clipAnimations = clips;
        }

        void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] animations)
        {
            if (!assetPath.StartsWith(ArtRoot)) return;
            if (UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline == null) return; // toon shader needs URP
            var toon = Shader.Find("Reforged/Toon");
            if (toon == null) return;
            material.shader = toon;
            if (description.TryGetProperty("DiffuseColor", out Vector4 color))
                material.SetColor("_BaseColor", new Color(color.x, color.y, color.z, 1f));
            if (description.TryGetProperty("DiffuseColor", out TexturePropertyDescription texture) && texture.texture != null)
                material.SetTexture("_BaseMap", texture.texture);
            if (description.materialName.Contains("Interior")) material.SetFloat("_HalftoneStrength", 0.8f);
        }

        void OnPostprocessModel(GameObject root)
        {
            if (!assetPath.StartsWith(PropRoot) || !assetPath.Contains("_fractured")) return;
            foreach (var filter in root.GetComponentsInChildren<MeshFilter>())
            {
                if (!filter.name.Contains("_cell_")) continue;
                var collider = filter.gameObject.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = true;
                var size = filter.sharedMesh.bounds.size;
                var body = filter.gameObject.AddComponent<Rigidbody>();
                body.mass = Mathf.Max(0.2f, size.x * size.y * size.z * 300f);
                body.interpolation = RigidbodyInterpolation.Interpolate;
            }
        }
    }
}
