using System;
using System.Collections.Generic;
using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    public enum SessionPhase { CharacterSelect, Playing }

    [Serializable]
    public struct EnemySpawn
    {
        public string hero;
        public Vector3 position;
    }

    /// <summary>
    /// Owns the deterministic simulation and ticks it at a fixed 60 Hz regardless of frame rate.
    /// All play goes through a RollbackSession; local players simply confirm their input on time.
    /// Views read sim state after each tick and interpolate between ticks for smooth rendering.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameSession : MonoBehaviour
    {
        const float TickSeconds = 1f / SimConstants.TicksPerSecond;
        const int MaxTicksPerFrame = 4;

        [SerializeField] StageAuthoring stage;
        [Tooltip("Leave empty to use the code-defined graybox roster.")]
        [SerializeField] ContentLibrary contentLibrary;
        [SerializeField] FighterView fighterViewPrefab;
        [SerializeField] EntityView projectileViewPrefab;
        [SerializeField] EntityView debrisViewPrefab;
        [SerializeField] uint seed = 1;
        [Tooltip("Show character select (hero + Assist Callout partner) before the match. Off = instant join with defaults.")]
        [SerializeField] bool characterSelect = true;
        [SerializeField] float readyCountdownSeconds = 1.5f;
        [SerializeField] Vector3[] playerSpawns = { new Vector3(-4, 0, 0), new Vector3(-5, 0, 1), new Vector3(-5, 0, -1), new Vector3(-6, 0, 0) };
        [SerializeField] List<EnemySpawn> enemySpawns = new List<EnemySpawn>();

        public SimWorld World { get; private set; }
        public RollbackSession Session { get; private set; }
        public ContentDatabase Content { get; private set; }
        public ContentLibrary Library => contentLibrary;
        public float InterpolationAlpha { get; private set; }
        public SessionPhase Phase { get; private set; }
        public Lobby Lobby { get; private set; }
        /// <summary>Seconds until the match starts once everyone is ready; negative when not counting down.</summary>
        public float Countdown { get; private set; } = -1f;

        /// <summary>Raised once per sim event after every tick (VFX, camera, audio, voice).</summary>
        public event Action<SimEvent> SimEventRaised;

        readonly PlayerInputBridge[] localPlayers = new PlayerInputBridge[SimConstants.MaxPlayers];
        readonly Dictionary<int, FighterView> fighterViews = new Dictionary<int, FighterView>();
        readonly Dictionary<int, EntityView> projectileViews = new Dictionary<int, EntityView>();
        readonly Dictionary<int, EntityView> debrisViews = new Dictionary<int, EntityView>();
        readonly HashSet<int> seen = new HashSet<int>();
        readonly List<int> stale = new List<int>();
        float accumulator;

        public IReadOnlyDictionary<int, FighterView> FighterViews => fighterViews;

        void Awake()
        {
            Content = contentLibrary != null ? contentLibrary.Build() : GrayboxContent.Create();
            var stageDef = stage != null ? stage.Build() : GrayboxStages.TestRoom();
            World = new SimWorld(Content, stageDef, seed);
            Session = new RollbackSession(World, SimConstants.MaxPlayers);
            Phase = characterSelect ? SessionPhase.CharacterSelect : SessionPhase.Playing;
            if (characterSelect) Lobby = new Lobby(Content, RosterCatalog.All);
            foreach (var spawn in enemySpawns)
            {
                int index = Content.IndexOf(spawn.hero);
                if (index < 0) { Debug.LogWarning("Unknown enemy " + spawn.hero); continue; }
                World.SpawnEnemy(index, SimConvert.ToFix(spawn.position));
            }
        }

        /// <summary>
        /// Claims the first free slot for a local controller. During character select the player
        /// joins the lobby; mid-match they drop straight in with their controller's default picks.
        /// </summary>
        public int JoinLocal(PlayerInputBridge bridge)
        {
            int slot = Array.IndexOf(localPlayers, null);
            if (slot < 0 || World.Players[slot] != null) return -1;
            localPlayers[slot] = bridge;
            if (Phase == SessionPhase.CharacterSelect) Lobby.Join(slot);
            else SpawnLocal(slot, bridge.DefaultHero, bridge.DefaultAssist);
            return slot;
        }

        void SpawnLocal(int slot, string heroName, string assistName)
        {
            int hero = Content.IndexOf(heroName);
            if (hero < 0) hero = Content.IndexOf(GrayboxContent.CaptainAmerica);
            var spawn = playerSpawns[Mathf.Min(slot, playerSpawns.Length - 1)];
            World.SpawnPlayer(slot, hero, SimConvert.ToFix(spawn), Content.IndexOf(assistName));
        }

        void UpdateLobby()
        {
            bool backedOut = false;
            for (int slot = 0; slot < localPlayers.Length; slot++)
            {
                var bridge = localPlayers[slot];
                if (bridge == null) continue;
                var menu = bridge.ReadMenu();
                if (menu.Horizontal != 0) Lobby.Move(slot, menu.Horizontal);
                if (menu.Confirm) Lobby.Confirm(slot);
                if (menu.Back)
                {
                    Lobby.Back(slot);
                    backedOut = true;
                }
            }

            if (!Lobby.AllReady || backedOut)
            {
                Countdown = -1f;
                return;
            }
            if (Countdown < 0f) Countdown = readyCountdownSeconds;
            Countdown -= Time.deltaTime;
            if (Countdown <= 0f) StartMatch(Lobby.Build());
        }

        void StartMatch(MatchSetup setup)
        {
            for (int slot = 0; slot < localPlayers.Length; slot++)
            {
                if (setup.Heroes[slot] == null) continue;
                SpawnLocal(slot, setup.Heroes[slot], setup.Assists[slot]);
                localPlayers[slot]?.Sample(); // drop menu presses so "confirm" doesn't also jump
            }
            Phase = SessionPhase.Playing;
            Countdown = -1f;
            accumulator = 0f;
        }

        void Update()
        {
            if (Phase == SessionPhase.CharacterSelect)
            {
                UpdateLobby();
                SyncViews();
                foreach (var view in fighterViews.Values) view.Render(1f);
                return;
            }

            accumulator += Time.deltaTime;
            int ticks = 0;
            while (accumulator >= TickSeconds && ticks < MaxTicksPerFrame)
            {
                Tick();
                accumulator -= TickSeconds;
                ticks++;
            }
            if (ticks == MaxTicksPerFrame) accumulator = 0f; // never spiral after a long hitch
            InterpolationAlpha = accumulator / TickSeconds;
            foreach (var view in fighterViews.Values) view.Render(InterpolationAlpha);
            foreach (var view in projectileViews.Values) view.Render(InterpolationAlpha);
            foreach (var view in debrisViews.Values) view.Render(InterpolationAlpha);
        }

        void Tick()
        {
            int frame = World.Frame;
            for (int slot = 0; slot < localPlayers.Length; slot++)
                if (localPlayers[slot] != null) Session.AddInput(slot, frame, localPlayers[slot].Sample());
            Session.Advance();

            SyncViews();
            if (stage != null)
            {
                var views = stage.Destructibles;
                for (int i = 0; i < views.Count && i < World.Destructibles.Length; i++) views[i].ApplyState(World.Destructibles[i]);
            }
            if (SimEventRaised != null)
                foreach (var e in World.Events) SimEventRaised(e);
        }

        void SyncViews()
        {
            seen.Clear();
            foreach (var f in World.Fighters)
            {
                seen.Add(f.Id);
                if (!fighterViews.TryGetValue(f.Id, out var view))
                {
                    view = Instantiate(fighterViewPrefab, SimConvert.ToUnity(f.Pos), Quaternion.identity, transform);
                    view.Bind(f, Content[f.HeroIndex], contentLibrary != null ? contentLibrary.Find(Content[f.HeroIndex].Name) : null);
                    fighterViews.Add(f.Id, view);
                }
                view.PushTick(f, Content[f.HeroIndex]);
            }
            RemoveStale(fighterViews);

            seen.Clear();
            foreach (var p in World.Projectiles)
            {
                seen.Add(p.Id);
                Track(projectileViews, projectileViewPrefab, p.Id, p.Pos);
            }
            RemoveStale(projectileViews);

            seen.Clear();
            foreach (var d in World.Debris)
            {
                seen.Add(d.Id);
                Track(debrisViews, debrisViewPrefab, d.Id, d.Pos).SetScale(SimConvert.ToUnity(d.HalfExtents) * 2f);
            }
            RemoveStale(debrisViews);
        }

        EntityView Track(Dictionary<int, EntityView> views, EntityView prefab, int id, FixVec3 pos)
        {
            if (!views.TryGetValue(id, out var view))
            {
                view = Instantiate(prefab, SimConvert.ToUnity(pos), Quaternion.identity, transform);
                views.Add(id, view);
            }
            view.PushTick(SimConvert.ToUnity(pos));
            return view;
        }

        void RemoveStale<T>(Dictionary<int, T> views) where T : Component
        {
            stale.Clear();
            foreach (var id in views.Keys) if (!seen.Contains(id)) stale.Add(id);
            foreach (var id in stale)
            {
                Destroy(views[id].gameObject);
                views.Remove(id);
            }
        }
    }
}
