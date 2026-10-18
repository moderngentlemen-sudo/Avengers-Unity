using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// Authoring and presentation for one destructible. The simulation owns health and broken
    /// state; this component only mirrors it: swapping damage meshes and spawning the cosmetic
    /// fracture (pre-fractured in Blender with Cell Fracture). Cosmetic chunks are local physics
    /// and are never rolled back.
    /// </summary>
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DestructibleAuthoring : MonoBehaviour
    {
        [SerializeField] DestructionTier tier;
        [SerializeField] int health = 50;
        [SerializeField] int damageStages = 2;
        [SerializeField] int debrisCount;
        [SerializeField, Range(1, 2)] int debrisWeight = 1;

        [Header("Presentation")]
        [SerializeField] GameObject intact;
        [Tooltip("One visual per damage stage, swapped in as the prop takes damage.")]
        [SerializeField] GameObject[] damageStateVisuals = new GameObject[0];
        [Tooltip("Prefab of rigidbody chunks exported from Blender (name chunks *_cell_###).")]
        [SerializeField] GameObject fracturedPrefab;
        [SerializeField] float shatterForce = 6f;
        [SerializeField] float chunkLifetime = 8f;

        int shownStage;
        GameObject fractured;

        public void Configure(DestructionTier t, int hp, int stages, int debris, int weight)
        {
            tier = t;
            health = hp;
            damageStages = stages;
            debrisCount = debris;
            debrisWeight = weight;
        }

        public DestructibleDef ToDef()
        {
            var b = SimConvert.WorldBounds(GetComponent<BoxCollider>());
            return new DestructibleDef
            {
                Name = name,
                Tier = tier,
                Center = SimConvert.ToFix(b.center),
                HalfExtents = SimConvert.ToFix(b.extents),
                Health = health,
                DamageStages = damageStages,
                DebrisCount = debrisCount,
                DebrisWeight = (byte)debrisWeight,
            };
        }

        public void ApplyState(DestructibleState state)
        {
            if (state.Broken)
            {
                if (fractured == null) Shatter();
                return;
            }
            if (fractured != null) Restore();
            if (state.Stage != shownStage) ShowStage(state.Stage);
        }

        void ShowStage(int stage)
        {
            shownStage = stage;
            for (int i = 0; i < damageStateVisuals.Length; i++)
                if (damageStateVisuals[i] != null) damageStateVisuals[i].SetActive(i == stage - 1);
            if (intact != null) intact.SetActive(stage == 0 || damageStateVisuals.Length == 0);
        }

        void Shatter()
        {
            if (intact != null) intact.SetActive(false);
            foreach (var v in damageStateVisuals) if (v != null) v.SetActive(false);
            SetSelfVisible(false);

            fractured = fracturedPrefab != null
                ? Instantiate(fracturedPrefab, transform.position, transform.rotation)
                : BuildGrayboxChunks();
            var origin = transform.position;
            foreach (var rb in fractured.GetComponentsInChildren<Rigidbody>())
            {
                rb.AddExplosionForce(shatterForce, origin, 4f, 0.5f, ForceMode.Impulse);
                Destroy(rb.gameObject, chunkLifetime + Random.value * 2f);
            }
        }

        /// <summary>Stand-in until the Blender fracture exists: split the box into 2x2x2 cubes.</summary>
        GameObject BuildGrayboxChunks()
        {
            var root = new GameObject(name + " (broken)");
            var b = SimConvert.WorldBounds(GetComponent<BoxCollider>());
            var size = b.size * 0.5f;
            var material = GetComponent<Renderer>() != null ? GetComponent<Renderer>().sharedMaterial : null;
            for (int i = 0; i < 8; i++)
            {
                var chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
                chunk.transform.SetParent(root.transform, false);
                var offset = new Vector3((i & 1) - 0.5f, ((i >> 1) & 1) - 0.5f, ((i >> 2) & 1) - 0.5f);
                chunk.transform.position = b.center + Vector3.Scale(offset, size);
                chunk.transform.localScale = size * 0.95f;
                if (material != null) chunk.GetComponent<Renderer>().sharedMaterial = material;
                chunk.AddComponent<Rigidbody>().mass = Mathf.Max(0.2f, size.x * size.y * size.z);
            }
            return root;
        }

        /// <summary>A rollback undid the break: rare, and the visual simply snaps back.</summary>
        void Restore()
        {
            Destroy(fractured);
            fractured = null;
            SetSelfVisible(true);
            shownStage = -1;
        }

        /// <summary>Graybox props have no separate intact child: hide this object's own renderer and collider.</summary>
        void SetSelfVisible(bool visible)
        {
            GetComponent<BoxCollider>().enabled = visible;
            foreach (var r in GetComponents<Renderer>()) r.enabled = visible;
        }
    }
}
