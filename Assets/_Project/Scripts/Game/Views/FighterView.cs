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
                block = new MaterialPropertyBlock();
                foreach (var r in model.GetComponentsInChildren<Renderer>())
                {
                    r.GetPropertyBlock(block);
                    if (f.IsPlayer) block.SetColor("_OutlineColor", color * 0.6f);
                    r.SetPropertyBlock(block);
                }
            }
            else
            {
                var size = SimConvert.ToUnity(hero.BodyHalfExtents) * 2f;
                visualRoot.localScale = new Vector3(size.x, size.y * 0.5f, size.z);
                animator = GetComponentInChildren<Animator>();
                if (tintRenderer != null)
                {
                    block = new MaterialPropertyBlock();
                    block.SetColor("_BaseColor", color);
                    block.SetColor("_Color", color);
                    tintRenderer.SetPropertyBlock(block);
                }
            }
            current = previous = SimConvert.ToUnity(f.Pos);
        }

        public void PushTick(Fighter f, HeroDef hero)
        {
            previous = current;
            current = SimConvert.ToUnity(f.Pos);
            visualRoot.localRotation = Quaternion.Euler(0f, f.Facing > 0 ? 90f : -90f, 0f);

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

        public void Render(float alpha)
        {
            transform.position = Vector3.LerpUnclamped(previous, current, alpha);
        }
    }
}
