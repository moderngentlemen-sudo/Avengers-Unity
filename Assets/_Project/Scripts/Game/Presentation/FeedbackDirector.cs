using Reforged.Sim;
using Unity.Cinemachine;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// Turns simulation events into game feel: camera impulses scaled by hit tier, hit sparks, and
    /// 3D comic onomatopoeia ("THOOM!"). Pure presentation; nothing here touches the sim.
    /// </summary>
    public sealed class FeedbackDirector : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] CinemachineImpulseSource impulse;
        [SerializeField] GameObject hitSparkPrefab;
        [SerializeField] GameObject shatterBurstPrefab;
        [SerializeField] float wordLifetime = 0.6f;
        [SerializeField] Font wordFont;

        [Tooltip("How fast the ink-outline impact pulse fades, per second.")]
        [SerializeField] float impactDecay = 6f;

        static readonly string[] HitWords = { "POW!", "WHAM!", "KRAK!", "THOOM!", "BLAM!", "SMASH!" };
        static readonly int ImpactId = Shader.PropertyToID("_ReforgedImpact");
        float impact;

        void Update()
        {
            impact = Mathf.MoveTowards(impact, 0f, impactDecay * Time.deltaTime);
            Shader.SetGlobalFloat(ImpactId, impact);
        }

        void OnEnable() { if (session != null) session.SimEventRaised += OnSimEvent; }
        void OnDisable() { if (session != null) session.SimEventRaised -= OnSimEvent; }

        void OnSimEvent(SimEvent e)
        {
            var pos = SimConvert.ToUnity(e.Pos) + Vector3.up * 1.2f;
            switch (e.Type)
            {
                case SimEventType.Hit:
                    Shake(Mathf.Clamp(e.Value / 60f, 0.15f, 1.5f));
                    Spawn(hitSparkPrefab, pos);
                    if (e.Value >= 70) Word(HitWords[(e.B + e.Value) % HitWords.Length], pos, Color.yellow);
                    break;
                case SimEventType.Parry: Shake(0.6f); Word("PARRY!", pos, Color.cyan); break;
                case SimEventType.ArmorBreak: Shake(1f); Word("BREAK!", pos, new Color(1f, 0.5f, 0f)); break;
                case SimEventType.WallBreak: Shake(1.6f); Word("KRAKOOM!", pos, Color.red); break;
                case SimEventType.CeilingBreakthrough: Shake(1.4f); Word("THROUGH THE ROOF!", pos, Color.red); break;
                case SimEventType.FloorCollapse: Shake(2f); Word("CRUNCH!", pos, Color.red); break;
                case SimEventType.DestructibleBroken: Shake(0.8f); Spawn(shatterBurstPrefab, SimConvert.ToUnity(e.Pos)); break;
                case SimEventType.EnvironmentalHit: Word("CLANG!", pos, Color.white); break;
                case SimEventType.UltimateStarted: Shake(1f); Word(e.Value == 2 ? "ULTIMATE II!" : "ULTIMATE!", pos, Color.magenta); break;
                case SimEventType.TeamUltimate: Shake(3f); Word("AVENGERS ASSEMBLE!", pos + Vector3.up * 2f, Color.magenta); break;
                case SimEventType.AssistCalled: Word("ASSIST!", pos, new Color(0.6f, 0.85f, 1f)); break;
                case SimEventType.ComboRelay: Word("RELAY!", pos, Color.green); break;
                case SimEventType.Ping: Word("!", pos + Vector3.up, Color.white); break;
            }
        }

        void Shake(float force)
        {
            // Big hits also thicken every toon outline for a beat (Reforged/Toon reads _ReforgedImpact).
            impact = Mathf.Max(impact, Mathf.Clamp01(force / 1.5f));
            if (impulse != null) impulse.GenerateImpulseWithForce(force);
        }

        static void Spawn(GameObject prefab, Vector3 pos)
        {
            if (prefab != null) Destroy(Instantiate(prefab, pos, Quaternion.identity), 3f);
        }

        void Word(string text, Vector3 pos, Color color)
        {
            var go = new GameObject("FX " + text);
            go.transform.position = pos;
            var mesh = go.AddComponent<TextMesh>();
            mesh.text = text;
            mesh.color = color;
            mesh.anchor = TextAnchor.MiddleCenter;
            mesh.characterSize = 0.12f;
            mesh.fontSize = 64;
            mesh.fontStyle = FontStyle.Bold;
            if (wordFont != null)
            {
                mesh.font = wordFont;
                go.GetComponent<MeshRenderer>().sharedMaterial = wordFont.material;
            }
            go.AddComponent<ComicWord>().Play(wordLifetime);
        }
    }

    /// <summary>Punches in, drifts up, and fades out.</summary>
    public sealed class ComicWord : MonoBehaviour
    {
        float lifetime, age;
        TextMesh mesh;

        public void Play(float seconds)
        {
            lifetime = seconds;
            mesh = GetComponent<TextMesh>();
            transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(-12f, 12f));
        }

        void Update()
        {
            age += Time.deltaTime;
            float t = age / lifetime;
            transform.localScale = Vector3.one * (t < 0.15f ? Mathf.Lerp(0.3f, 1.3f, t / 0.15f) : Mathf.Lerp(1.3f, 1f, t));
            transform.position += Vector3.up * Time.deltaTime * 0.8f;
            var c = mesh.color;
            c.a = 1f - Mathf.Clamp01((t - 0.6f) / 0.4f);
            mesh.color = c;
            if (age >= lifetime) Destroy(gameObject);
        }
    }
}
