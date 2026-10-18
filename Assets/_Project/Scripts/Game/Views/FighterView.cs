using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// Presentation for one fighter. Mirrors sim state into the transform and animation; never
    /// writes back. Imported heroes are driven by SimAnimationDriver (clip = move, time = sim frame).
    /// Without a model, a placeholder Animator gets parameters: Mode (int, FighterMode), Move (int, move index or -1),
    /// MoveFrame (int), Grounded (bool), Speed (float). Supers and Ultimates also fire a
    /// "Cinematic" trigger for Timeline-driven camera cuts.
    /// </summary>
    public sealed class FighterView : MonoBehaviour
    {
        static readonly int ModeId = Animator.StringToHash("Mode");
        static readonly int MoveId = Animator.StringToHash("Move");
        static readonly int MoveFrameId = Animator.StringToHash("MoveFrame");
        static readonly int GroundedId = Animator.StringToHash("Grounded");
        static readonly int SpeedId = Animator.StringToHash("Speed");
        static readonly int CinematicId = Animator.StringToHash("Cinematic");
        static readonly int RimColorId = Shader.PropertyToID("_RimColor");
        static readonly int RimStrengthId = Shader.PropertyToID("_RimStrength");
        static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Transform visualRoot;
        [SerializeField] Renderer tintRenderer;
        [SerializeField] Color[] playerColors =
        {
            new Color(0.2f, 0.45f, 1f), new Color(1f, 0.25f, 0.2f), new Color(0.2f, 0.9f, 0.35f), new Color(1f, 0.8f, 0.15f),
        };
        [SerializeField] Color enemyColor = new Color(0.35f, 0.35f, 0.38f);
        [SerializeField] Color assistColor = new Color(0.7f, 0.85f, 1f, 0.6f);

        Animator animator;
        SimAnimationDriver simAnimation;
        Vector3 previous, current;
        int lastMove = -1;
        MaterialPropertyBlock block;
        Renderer[] renderers = new Renderer[0];
        Color baseColor, outlineColor = new Color(0.05f, 0.04f, 0.08f);
        bool tintBase;
        Vector3 baseScale = Vector3.one;
        int mechanicLookKey = int.MinValue;

        public int FighterId { get; private set; }
        public int PlayerSlot { get; private set; } = -1;
        public bool IsAssist { get; private set; }

        public void Bind(Fighter f, HeroDef hero, HeroAsset asset)
        {
            FighterId = f.Id;
            PlayerSlot = f.PlayerSlot;
            IsAssist = f.IsAssist;
            name = hero.Name + (f.IsAssist ? " (Assist)" : f.IsPlayer ? " P" + (f.PlayerSlot + 1) : "") + " #" + f.Id;
            if (visualRoot == null) visualRoot = transform;

            var color = f.IsAssist ? assistColor : f.IsPlayer ? playerColors[f.PlayerSlot % playerColors.Length] : enemyColor;
            if (asset != null && asset.modelPrefab != null)
            {
                foreach (Transform child in visualRoot) child.gameObject.SetActive(false);
                var model = Instantiate(asset.modelPrefab, visualRoot);
                model.transform.localRotation = Quaternion.Euler(asset.modelRotation);
                var modelAnimator = model.GetComponentInChildren<Animator>();
                if (modelAnimator == null) modelAnimator = model.AddComponent<Animator>();
                simAnimation = model.AddComponent<SimAnimationDriver>();
                simAnimation.Init(modelAnimator, asset.clips);
                // Players get a colored ink outline; enemies and assists keep the default ink.
                renderers = model.GetComponentsInChildren<Renderer>();
                if (f.IsPlayer) outlineColor = color * 0.6f;
            }
            else
            {
                var size = SimConvert.ToUnity(hero.BodyHalfExtents) * 2f;
                visualRoot.localScale = new Vector3(size.x, size.y * 0.5f, size.z);
                animator = GetComponentInChildren<Animator>();
                if (tintRenderer != null) renderers = new[] { tintRenderer };
                tintBase = true;
                baseColor = color;
            }
            block = new MaterialPropertyBlock();
            baseScale = visualRoot.localScale;
            current = previous = SimConvert.ToUnity(f.Pos);
            ApplyMechanicLook(f, hero);
        }

        public void PushTick(Fighter f, HeroDef hero)
        {
            previous = current;
            current = SimConvert.ToUnity(f.Pos);
            visualRoot.localRotation = Quaternion.Euler(0f, f.Facing > 0 ? 90f : -90f, 0f);
            ApplyMechanicLook(f, hero);

            if (simAnimation != null)
            {
                simAnimation.Show(f, hero);
                return;
            }
            if (animator == null) return;
            animator.SetInteger(ModeId, (int)f.Mode);
            animator.SetInteger(MoveId, f.MoveIndex);
            animator.SetInteger(MoveFrameId, f.MoveFrame);
            animator.SetBool(GroundedId, f.IsGrounded);
            animator.SetFloat(SpeedId, Mathf.Abs(f.Vel.X.ToFloat()) * SimConstants.TicksPerSecond);
            if (f.MoveIndex != lastMove && f.MoveIndex >= 0 && hero.Moves[f.MoveIndex].Tier >= MoveTier.Super)
                animator.SetTrigger(CinematicId);
            lastMove = f.MoveIndex;
        }

        /// <summary>
        /// Makes mechanic state readable at a glance: Ant-Man's size and Hulk's rage tier scale the
        /// model; Phase glows cyan, Diamond glints white, overheating glows orange, World Breaker green.
        /// </summary>
        void ApplyMechanicLook(Fighter f, HeroDef hero)
        {
            float scale = 1f;
            Color rim = Color.white;
            float rimStrength = 0.35f;
            switch (hero.Mechanic)
            {
                case HeroMechanic.Size:
                    scale = f.MechState < 0 ? 0.4f : f.MechState > 0 ? 2f : 1f;
                    break;
                case HeroMechanic.Rage:
                    scale = 1f + 0.06f * f.MechState;
                    if (f.MechState >= 3) { rim = new Color(0.3f, 1f, 0.3f); rimStrength = 1f; }
                    break;
                case HeroMechanic.Density:
                    if (f.MechState == 1) { rim = new Color(0.3f, 0.9f, 1f); rimStrength = 1f; }
                    else if (f.MechState == 2) { rim = Color.white; rimStrength = 0.9f; }
                    break;
                case HeroMechanic.Heat:
                    float heat = f.Meter / (float)SimConstants.MechanicMax;
                    rim = Color.Lerp(Color.white, new Color(1f, 0.45f, 0.1f), heat);
                    rimStrength = f.MechState == 1 ? 1f : 0.35f + 0.5f * heat;
                    break;
            }
            if (f.ShrunkFrames > 0) scale *= 0.5f;

            int key = (int)(scale * 100f) * 1000 + (int)(rimStrength * 100f) * 7 + (int)(rim.r * 10f) + (int)(rim.b * 10f) * 13;
            if (key == mechanicLookKey) return;
            mechanicLookKey = key;
            visualRoot.localScale = baseScale * scale;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(block);
                block.SetColor(RimColorId, rim);
                block.SetFloat(RimStrengthId, rimStrength);
                block.SetColor(OutlineColorId, outlineColor);
                if (tintBase)
                {
                    block.SetColor(BaseColorId, baseColor);
                    block.SetColor("_Color", baseColor);
                }
                r.SetPropertyBlock(block);
            }
        }

        public void Render(float alpha)
        {
            transform.position = Vector3.LerpUnclamped(previous, current, alpha);
        }
    }
}
