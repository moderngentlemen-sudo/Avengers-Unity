using Reforged.Game;
using Reforged.Sim;
using UnityEditor;
using UnityEngine;

namespace Reforged.EditorTools
{
    /// <summary>
    /// Exports the code-defined graybox roster to editable HeroAssets plus a ContentLibrary, so
    /// designers can tune frame data in the Inspector. Assign the library on the Game Session.
    /// </summary>
    public static class ContentExporter
    {
        const string Folder = "Assets/_Project/Content";

        [MenuItem("Reforged/Export Graybox Roster To Hero Assets")]
        public static void Export()
        {
            if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/_Project", "Content");
            var library = ScriptableObject.CreateInstance<ContentLibrary>();
            foreach (var hero in GrayboxContent.Create().Heroes)
            {
                var asset = ScriptableObject.CreateInstance<HeroAsset>();
                asset.name = hero.Name;
                asset.CopyFrom(hero);
                HeroAssetEditor.AssignPipelineModel(asset);   // picks up Art/Characters/<hero>/<hero>.fbx if built
                AssetDatabase.CreateAsset(asset, AssetDatabase.GenerateUniqueAssetPath(Folder + "/" + hero.Name + ".asset"));
                library.heroes.Add(asset);
            }
            AssetDatabase.CreateAsset(library, AssetDatabase.GenerateUniqueAssetPath(Folder + "/ContentLibrary.asset"));
            AssetDatabase.SaveAssets();

            // Round-trip check: the assets must rebuild content that plays out bit-identically.
            bool identical = PlayScripted(GrayboxContent.Create()) == PlayScripted(library.Build());
            Debug.Log(identical
                ? "Exported " + library.heroes.Count + " heroes to " + Folder + " (round-trip verified)"
                : "Exported, but the rebuilt content plays differently from the source. Check rounding in HeroAsset.");
            Selection.activeObject = library;
        }

        static ulong PlayScripted(ContentDatabase content)
        {
            var world = new SimWorld(content, GrayboxStages.TestRoom(), 1);
            world.SpawnPlayer(0, content.IndexOf(GrayboxContent.CaptainAmerica), FixVec3.Zero, content.IndexOf(GrayboxContent.IronMan));
            world.SpawnEnemy(content.IndexOf(GrayboxContent.HydraTrooper), new FixVec3(2, 0, 0));
            world.SpawnEnemy(content.IndexOf(GrayboxContent.HydraHeavy), new FixVec3(4, 0, 0));
            Buttons[] script = { Buttons.Light, Buttons.Light, Buttons.Heavy, Buttons.Special, Buttons.Assist, Buttons.Jump, Buttons.Light };
            var inputs = new InputFrame[SimConstants.MaxPlayers];
            for (int f = 0; f < 600; f++)
            {
                inputs[0] = InputFrame.Of(f % 12 == 0 ? script[f / 12 % script.Length] : Buttons.None, f % 120 < 60 ? 100 : 0);
                world.Step(inputs);
            }
            return world.Checksum();
        }
    }
}
