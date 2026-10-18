using Reforged.Game;
using Reforged.Sim;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reforged.EditorTools
{
    /// <summary>
    /// Generates the playable graybox test room (scene, prefabs, materials) straight from the
    /// simulation's own GrayboxStages.TestRoom definition, so the scene and the tests can't drift.
    /// Menu: Reforged > Build Graybox Test Room.
    /// </summary>
    public static class GrayboxSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string ControlsPath = Root + "/Input/ReforgedControls.inputactions";
        const string ScenePath = Root + "/Scenes/GrayboxTestRoom.unity";
        const string PropRoot = Root + "/Art/Props/";
        const string LibraryPath = Root + "/Content/ContentLibrary.asset";

        [MenuItem("Reforged/Build Graybox Test Room")]
        public static void Build()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "Materials");

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var light = new GameObject("Key Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.2f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            var stage = BuildStage(GrayboxStages.TestRoom());
            var fighterPrefab = BuildFighterPrefab();
            var shieldPrefab = BuildEntityPrefab("Projectile", PrimitiveType.Cylinder, new Vector3(0.7f, 0.06f, 0.7f), Mat("Projectile", new Color(0.15f, 0.35f, 0.9f)), 720f);
            var debrisPrefab = BuildEntityPrefab("Debris", PrimitiveType.Cube, Vector3.one, Mat("Debris", new Color(0.55f, 0.4f, 0.25f)), 240f);
            var playerPrefab = BuildPlayerPrefab();

            var sessionGo = new GameObject("Game Session");
            var session = sessionGo.AddComponent<GameSession>();
            var so = new SerializedObject(session);
            so.FindProperty("stage").objectReferenceValue = stage;
            so.FindProperty("fighterViewPrefab").objectReferenceValue = fighterPrefab;
            so.FindProperty("projectileViewPrefab").objectReferenceValue = shieldPrefab;
            so.FindProperty("debrisViewPrefab").objectReferenceValue = debrisPrefab;
            so.FindProperty("contentLibrary").objectReferenceValue = EnsureContentLibrary();
            var spawns = so.FindProperty("enemySpawns");
            AddSpawn(spawns, GrayboxContent.HydraTrooper, new Vector3(3f, 0f, 0f));
            AddSpawn(spawns, GrayboxContent.HydraTrooper, new Vector3(5f, 0f, 1.2f));
            AddSpawn(spawns, GrayboxContent.HydraHeavy, new Vector3(9f, 0f, 0f));
            AddSpawn(spawns, GrayboxContent.TrainingDummy, new Vector3(22.5f, 0f, 0f));
            AddSpawn(spawns, GrayboxContent.TrainingDummy, new Vector3(15f, 0f, 0f));
            so.ApplyModifiedPropertiesWithoutUndo();

            var joinGo = new GameObject("Player Join (press any button)");
            var manager = joinGo.AddComponent<PlayerInputManager>();
            manager.joinBehavior = PlayerJoinBehavior.JoinPlayersWhenButtonIsPressed;
            manager.playerPrefab = playerPrefab;
            var mso = new SerializedObject(manager);
            var maxPlayers = mso.FindProperty("m_MaxPlayerCount");
            if (maxPlayers != null) maxPlayers.intValue = SimConstants.MaxPlayers;
            mso.ApplyModifiedPropertiesWithoutUndo();

            BuildCameraAndPresentation(session);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("Graybox test room built: " + ScenePath + ". Press Play, press any button on a pad (or K on keyboard) to join, then pick a hero and assist.");
        }

        /// <summary>Exports the roster to HeroAssets once (picking up any Blender models) and reuses it after.</summary>
        static ContentLibrary EnsureContentLibrary()
        {
            var library = AssetDatabase.LoadAssetAtPath<ContentLibrary>(LibraryPath);
            if (library != null) return library;
            ContentExporter.Export();
            return AssetDatabase.LoadAssetAtPath<ContentLibrary>(LibraryPath);
        }

        static StageAuthoring BuildStage(StageDef def)
        {
            var root = new GameObject("Stage");
            var stage = root.AddComponent<StageAuthoring>();
            stage.SetBounds(def.MinX.ToFloat(), def.MaxX.ToFloat(), def.MaxZ.ToFloat());
            var linked = new bool[def.Destructibles.Count];
            float thickness = SimConstants.PlatformThickness.ToFloat();

            foreach (var p in def.Platforms)
            {
                var center = new Vector3((p.MinX + p.MaxX).ToFloat() * 0.5f, p.Top.ToFloat() - thickness * 0.5f, (p.MinZ + p.MaxZ).ToFloat() * 0.5f);
                var size = new Vector3((p.MaxX - p.MinX).ToFloat(), thickness, (p.MaxZ - p.MinZ).ToFloat());
                var color = p.Destructible >= 0 ? new Color(0.85f, 0.55f, 0.25f) : p.Solid ? new Color(0.35f, 0.36f, 0.4f) : new Color(0.45f, 0.5f, 0.6f);
                DestructibleAuthoring breakable = null;
                GameObject go;
                if (p.Destructible >= 0)
                {
                    breakable = DestructibleBox(def.Destructibles[p.Destructible], center, size, color, root.transform);
                    go = breakable.gameObject;
                    linked[p.Destructible] = true;
                }
                else go = Box(p.Name, center, size, Mat(p.Name, color), root.transform);
                go.AddComponent<PlatformAuthoring>().Configure(p.Solid, breakable);
            }

            foreach (var w in def.Walls)
            {
                var center = new Vector3(w.X.ToFloat(), (w.MinY + w.MaxY).ToFloat() * 0.5f, 0f);
                var size = new Vector3(0.3f, (w.MaxY - w.MinY).ToFloat(), (def.MaxZ - def.MinZ).ToFloat());
                var color = new Color(0.9f, 0.85f, 0.7f);
                DestructibleAuthoring breakable = null;
                GameObject go;
                if (w.Destructible >= 0)
                {
                    breakable = DestructibleBox(def.Destructibles[w.Destructible], center, size, color, root.transform);
                    go = breakable.gameObject;
                    linked[w.Destructible] = true;
                }
                else go = Box(w.Name, center, size, Mat(w.Name, color), root.transform);
                go.AddComponent<WallAuthoring>().Configure(breakable);
            }

            for (int i = 0; i < def.Destructibles.Count; i++)
            {
                if (linked[i]) continue;
                var d = def.Destructibles[i];
                var color = d.Tier == DestructionTier.Cosmetic ? new Color(0.6f, 0.42f, 0.2f) : new Color(0.8f, 0.15f, 0.15f);
                DestructibleBox(d, SimConvert.ToUnity(d.Center), SimConvert.ToUnity(d.HalfExtents) * 2f, color, root.transform);
            }
            return stage;
        }

        /// <summary>
        /// A destructible root: BoxCollider for authoring bounds plus a separate visual child, so
        /// the Blender props (Art/Props/&lt;name&gt;_intact / _fractured) drop in without inheriting scale.
        /// Falls back to a graybox cube when the prop hasn't been built.
        /// </summary>
        static DestructibleAuthoring DestructibleBox(DestructibleDef d, Vector3 center, Vector3 size, Color color, Transform parent)
        {
            var go = new GameObject(d.Name);
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.AddComponent<BoxCollider>().size = size;

            var slug = d.Name.ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
            var intactModel = AssetDatabase.LoadAssetAtPath<GameObject>(PropRoot + slug + "_intact.fbx");
            var fracturedModel = AssetDatabase.LoadAssetAtPath<GameObject>(PropRoot + slug + "_fractured.fbx");
            GameObject visual;
            if (intactModel != null)
            {
                visual = (GameObject)PrefabUtility.InstantiatePrefab(intactModel, go.transform);
                visual.transform.localPosition = Vector3.zero;
            }
            else
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
                visual.transform.SetParent(go.transform, false);
                visual.transform.localScale = size;
                visual.GetComponent<Renderer>().sharedMaterial = Mat(d.Name, color);
            }
            visual.name = "Intact";

            var a = go.AddComponent<DestructibleAuthoring>();
            a.Configure(d.Tier, d.Health, d.DamageStages, d.DebrisCount, d.DebrisWeight);
            Wire(a, ("intact", visual), ("fracturedPrefab", fracturedModel));
            return a;
        }

        static FighterView BuildFighterPrefab()
        {
            var root = new GameObject("Fighter");
            var view = root.AddComponent<FighterView>();
            var visual = new GameObject("Visual").transform;
            visual.SetParent(root.transform, false);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            Object.DestroyImmediate(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(visual, false);
            body.transform.localPosition = Vector3.up;
            body.GetComponent<Renderer>().sharedMaterial = Mat("Fighter", Color.white);

            var nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.DestroyImmediate(nose.GetComponent<Collider>());
            nose.name = "Facing";
            nose.transform.SetParent(visual, false);
            nose.transform.localPosition = new Vector3(0f, 1.5f, 0.55f);
            nose.transform.localScale = new Vector3(0.3f, 0.2f, 0.4f);
            nose.GetComponent<Renderer>().sharedMaterial = Mat("Facing", Color.black);

            var so = new SerializedObject(view);
            so.FindProperty("visualRoot").objectReferenceValue = visual;
            so.FindProperty("tintRenderer").objectReferenceValue = body.GetComponent<Renderer>();
            so.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, "Fighter").GetComponent<FighterView>();
        }

        static EntityView BuildEntityPrefab(string prefabName, PrimitiveType shape, Vector3 scale, Material mat, float spin)
        {
            var root = new GameObject(prefabName);
            var view = root.AddComponent<EntityView>();
            var mesh = GameObject.CreatePrimitive(shape);
            Object.DestroyImmediate(mesh.GetComponent<Collider>());
            mesh.transform.SetParent(root.transform, false);
            mesh.transform.localScale = scale;
            if (shape == PrimitiveType.Cube) mesh.transform.localPosition = new Vector3(0f, 0.5f, 0f);
            else mesh.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            mesh.GetComponent<Renderer>().sharedMaterial = mat;
            var so = new SerializedObject(view);
            so.FindProperty("spinDegreesPerSecond").floatValue = spin;
            so.ApplyModifiedPropertiesWithoutUndo();
            return SavePrefab(root, prefabName).GetComponent<EntityView>();
        }

        static GameObject BuildPlayerPrefab()
        {
            var root = new GameObject("Player");
            var input = root.AddComponent<PlayerInput>();
            input.actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(ControlsPath);
            input.defaultActionMap = "Gameplay";
            input.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
            root.AddComponent<PlayerInputBridge>();
            return SavePrefab(root, "Player");
        }

        static void BuildCameraAndPresentation(GameSession session)
        {
            var groupGo = new GameObject("Player Target Group");
            var group = groupGo.AddComponent<CinemachineTargetGroup>();

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 35f;
            camGo.AddComponent<AudioListener>();
            camGo.AddComponent<CinemachineBrain>();

            var vcamGo = new GameObject("Brawler Camera");
            vcamGo.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            vcam.Follow = group.transform;
            var lens = vcam.Lens;
            lens.FieldOfView = 35f;
            vcam.Lens = lens;
            var composer = vcamGo.AddComponent<CinemachinePositionComposer>();
            composer.CameraDistance = 16f;
            vcamGo.AddComponent<CinemachineGroupFraming>();
            vcamGo.AddComponent<CinemachineImpulseListener>();

            var fx = new GameObject("Presentation");
            var impulse = fx.AddComponent<CinemachineImpulseSource>();
            Wire(fx.AddComponent<CameraDirector>(), ("session", session), ("targetGroup", group));
            Wire(fx.AddComponent<FeedbackDirector>(), ("session", session), ("impulse", impulse));
            Wire(fx.AddComponent<DebugHud>(), ("session", session));
            Wire(fx.AddComponent<SimDebugDraw>(), ("session", session));
            Wire(fx.AddComponent<CharacterSelectScreen>(), ("session", session));
        }

        static void Wire(Object target, params (string field, Object value)[] refs)
        {
            var so = new SerializedObject(target);
            foreach (var (field, value) in refs) so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void AddSpawn(SerializedProperty list, string hero, Vector3 pos)
        {
            int i = list.arraySize;
            list.InsertArrayElementAtIndex(i);
            var e = list.GetArrayElementAtIndex(i);
            e.FindPropertyRelative("hero").stringValue = hero;
            e.FindPropertyRelative("position").vector3Value = pos;
        }

        static GameObject Box(string objectName, Vector3 center, Vector3 size, Material mat, Transform parent)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = objectName;
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = mat;
            return go;
        }

        static Material Mat(string matName, Color color)
        {
            var path = Root + "/Materials/" + matName + ".mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;
            // The toon shader is URP-only; without a URP asset assigned, fall back to Standard so nothing renders pink.
            bool urp = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline != null;
            var shader = (urp ? Shader.Find("Reforged/Toon") : null) ?? Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            var mat = new Material(shader) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static GameObject SavePrefab(GameObject go, string prefabName)
        {
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, Root + "/Prefabs/" + prefabName + ".prefab");
            Object.DestroyImmediate(go);
            return prefab;
        }

        static void EnsureFolder(string parent, string child)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + child)) AssetDatabase.CreateFolder(parent, child);
        }
    }
}
