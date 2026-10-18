using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// Graybox character select: four player columns, hero carousel then Assist Callout partner.
    /// Left/right moves, A (K) confirms, B (L) goes back. Unbuilt heroes are shown as "coming soon"
    /// so the full roster is visible. Rules live in Reforged.Sim.Lobby; this only draws them.
    /// </summary>
    public sealed class CharacterSelectScreen : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] int visibleRows = 7;

        static readonly Color[] SlotColors =
        {
            new Color(0.2f, 0.45f, 1f), new Color(1f, 0.25f, 0.2f), new Color(0.2f, 0.9f, 0.35f), new Color(1f, 0.8f, 0.15f),
        };

        GUIStyle title, header, row, small;

        void OnGUI()
        {
            if (session == null || session.Phase != SessionPhase.CharacterSelect || session.Lobby == null) return;
            EnsureStyles();
            var lobby = session.Lobby;

            GUI.color = new Color(0f, 0f, 0f, 0.75f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(0, 20, Screen.width, 60), "ASSEMBLE YOUR TEAM", title);

            float colWidth = (Screen.width - 50f) / SimConstants.MaxPlayers;
            for (int slot = 0; slot < SimConstants.MaxPlayers; slot++)
                DrawSlot(lobby, slot, new Rect(10 + slot * (colWidth + 10), 100, colWidth, Screen.height - 170));

            string footer = session.Countdown >= 0f
                ? "Starting in " + Mathf.CeilToInt(session.Countdown) + "…"
                : "Press any button to join  ·  ←/→ choose  ·  A confirm  ·  B back";
            GUI.Label(new Rect(0, Screen.height - 50, Screen.width, 30), footer, header);
        }

        void DrawSlot(Lobby lobby, int slot, Rect area)
        {
            var s = lobby.Slots[slot];
            GUI.color = s.Phase == LobbyPhase.Empty ? new Color(1f, 1f, 1f, 0.08f) : SlotColors[slot] * new Color(1f, 1f, 1f, 0.25f);
            GUI.DrawTexture(area, Texture2D.whiteTexture);
            GUI.color = Color.white;
            GUI.Label(new Rect(area.x, area.y + 8, area.width, 30), "P" + (slot + 1), header);

            float y = area.y + 50;
            switch (s.Phase)
            {
                case LobbyPhase.Empty:
                    GUI.Label(new Rect(area.x, y, area.width, 30), "Press any button", header);
                    return;
                case LobbyPhase.PickHero:
                    GUI.Label(new Rect(area.x + 12, y, area.width, 24), "CHOOSE YOUR HERO", small);
                    DrawList(lobby, slot, true, s.HeroCursor, new Rect(area.x, y + 28, area.width, area.height - 90));
                    return;
                case LobbyPhase.PickAssist:
                    GUI.Label(new Rect(area.x + 12, y, area.width, 24), s.Hero.ToUpperInvariant() + " — CHOOSE AN ASSIST", small);
                    DrawList(lobby, slot, false, s.AssistCursor, new Rect(area.x, y + 28, area.width, area.height - 90));
                    return;
                case LobbyPhase.Ready:
                    GUI.Label(new Rect(area.x, y + 40, area.width, 40), s.Hero, header);
                    GUI.Label(new Rect(area.x, y + 80, area.width, 30), "Assist: " + s.Assist, row);
                    GUI.Label(new Rect(area.x, y + 140, area.width, 40), "READY!", title);
                    return;
            }
        }

        void DrawList(Lobby lobby, int slot, bool heroes, int cursor, Rect area)
        {
            var list = heroes ? lobby.HeroChoices : lobby.AssistChoices;
            int half = visibleRows / 2;
            for (int i = -half; i <= half; i++)
            {
                int index = ((cursor + i) % list.Count + list.Count) % list.Count;
                var entry = list[index];
                string reason = heroes ? HeroReason(lobby, slot, entry) : AssistReason(lobby, slot, entry);
                var rect = new Rect(area.x + 12, area.y + (i + half) * 30, area.width - 24, 28);
                if (i == 0)
                {
                    GUI.color = new Color(1f, 1f, 1f, 0.2f);
                    GUI.DrawTexture(rect, Texture2D.whiteTexture);
                }
                GUI.color = reason == null ? Color.white : new Color(1f, 1f, 1f, 0.4f);
                GUI.Label(rect, (i == 0 ? "▶ " : "   ") + entry.Name + (reason == null ? "" : "  (" + reason + ")"), row);
                GUI.color = Color.white;
            }
            var selected = list[cursor];
            GUI.Label(new Rect(area.x + 12, area.y + visibleRows * 30 + 8, area.width - 24, 24), selected.Archetype, small);
        }

        static string HeroReason(Lobby lobby, int slot, RosterEntry e)
        {
            if (!lobby.IsImplemented(e.Name)) return "coming soon";
            if (e.Role == RosterRole.Unlockable && !lobby.CanPlay(slot, e)) return "locked";
            return lobby.CanPlay(slot, e) ? null : "taken";
        }

        static string AssistReason(Lobby lobby, int slot, RosterEntry e)
        {
            if (!lobby.IsImplemented(e.Name)) return "coming soon";
            return lobby.CanAssist(slot, e) ? null : "in play";
        }

        void EnsureStyles()
        {
            if (title != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            header = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            row = new GUIStyle(GUI.skin.label) { fontSize = 17 };
            small = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
        }
    }
}
