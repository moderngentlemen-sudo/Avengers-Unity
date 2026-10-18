using Reforged.Sim;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Reforged.Game
{
    /// <summary>Edge-triggered menu input for character select.</summary>
    public struct MenuInput
    {
        public int Horizontal;
        public bool Confirm, Back, Start;
    }

    /// <summary>
    /// One per local controller (spawned by PlayerInputManager on join). Reads the Input System
    /// every rendered frame and hands the simulation one InputFrame per tick. Presses are latched
    /// between ticks so a tap shorter than a tick is never lost.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public sealed class PlayerInputBridge : MonoBehaviour
    {
        [Tooltip("Hero used for each join order when dropping into a match already in progress.")]
        [SerializeField] string[] heroRotation = { GrayboxContent.CaptainAmerica, GrayboxContent.IronMan };
        [Tooltip("Assist Callout partner for each join order.")]
        [SerializeField] string[] assistRotation = { GrayboxContent.IronMan, GrayboxContent.CaptainAmerica };

        InputAction move, aim, ultimateL, ultimateR, jump, special, pause;
        (InputAction action, Buttons button)[] buttons;
        Buttons latched;
        MenuInput menu;
        int stickDirection;
        int joinOrder;
        int menuEnabledFrame;

        public int Slot { get; private set; } = -1;
        public string DefaultHero => heroRotation[joinOrder % heroRotation.Length];
        public string DefaultAssist => assistRotation[joinOrder % assistRotation.Length];

        void Awake()
        {
            var actions = GetComponent<PlayerInput>().actions;
            move = actions["Move"];
            aim = actions["Aim"];
            ultimateL = actions["UltimateL"];
            ultimateR = actions["UltimateR"];
            jump = actions["Jump"];
            special = actions["Special"];
            pause = actions["Pause"];
            buttons = new[]
            {
                (actions["Light"], Buttons.Light), (actions["Heavy"], Buttons.Heavy),
                (actions["Jump"], Buttons.Jump), (actions["Special"], Buttons.Special),
                (actions["Grab"], Buttons.Grab), (actions["Block"], Buttons.Block),
                (actions["HeroMod"], Buttons.HeroMod), (actions["Assist"], Buttons.Assist),
                (actions["ToolUp"], Buttons.ToolUp), (actions["ToolDown"], Buttons.ToolDown),
                (actions["ToolLeft"], Buttons.ToolLeft), (actions["ToolRight"], Buttons.ToolRight),
                (actions["Ping"], Buttons.Ping),
            };
        }

        void Start()
        {
            joinOrder = GetComponent<PlayerInput>().playerIndex;
            menuEnabledFrame = Time.frameCount + 1; // the button that joined shouldn't also confirm a pick
            Slot = FindFirstObjectByType<GameSession>().JoinLocal(this);
            if (Slot < 0) Destroy(gameObject);
        }

        void Update()
        {
            foreach (var (action, button) in buttons)
                if (action.WasPressedThisFrame()) latched |= button;
            if (UltimateHeld() && (ultimateL.WasPressedThisFrame() || ultimateR.WasPressedThisFrame()))
                latched |= Buttons.Ultimate;

            if (Time.frameCount <= menuEnabledFrame) return;
            // Menu edges, with hysteresis so a held stick moves the cursor once.
            float x = move.ReadValue<Vector2>().x;
            int dir = x > 0.6f ? 1 : x < -0.6f ? -1 : Mathf.Abs(x) < 0.3f ? 0 : stickDirection;
            if (dir != 0 && dir != stickDirection) menu.Horizontal = dir;
            stickDirection = dir;
            if (jump.WasPressedThisFrame()) menu.Confirm = true;
            if (special.WasPressedThisFrame()) menu.Back = true;
            if (pause.WasPressedThisFrame()) menu.Start = true;
        }

        /// <summary>Menu presses since the last call.</summary>
        public MenuInput ReadMenu()
        {
            var result = menu;
            menu = default(MenuInput);
            return result;
        }

        bool UltimateHeld() => ultimateL.IsPressed() && ultimateR.IsPressed();

        /// <summary>Called once per simulation tick.</summary>
        public InputFrame Sample()
        {
            var held = Buttons.None;
            foreach (var (action, button) in buttons)
                if (action.IsPressed()) held |= button;
            if (UltimateHeld()) held |= Buttons.Ultimate;

            var m = move.ReadValue<Vector2>();
            var a = aim.ReadValue<Vector2>();
            var frame = InputFrame.Of(held | latched, Quantize(m.x), Quantize(m.y), Quantize(a.x), Quantize(a.y));
            latched = Buttons.None;
            return frame;
        }

        static int Quantize(float v) => Mathf.RoundToInt(Mathf.Clamp(v, -1f, 1f) * 127f);
    }
}
