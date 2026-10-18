using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// Graybox HUD: health, Hero Gauge bars, Style Rank, live combo, assist cooldown and the shared
    /// Assemble meter. The shipping HUD replaces this with UI Toolkit; the data sources stay the same.
    /// </summary>
    public sealed class DebugHud : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] bool showFrameData = true;

        GUIStyle label, big;

        void OnGUI()
        {
            if (session == null || session.World == null || session.Phase != SessionPhase.Playing) return;
            label ??= new GUIStyle(GUI.skin.label) { fontSize = 14, richText = true };
            big ??= new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold, richText = true };

            var world = session.World;
            float panelWidth = Screen.width / 4f;
            for (int slot = 0; slot < SimConstants.MaxPlayers; slot++)
            {
                var p = world.Players[slot];
                var x = 10 + slot * panelWidth;
                if (p == null)
                {
                    GUI.Label(new Rect(x, 10, panelWidth, 24), "P" + (slot + 1) + " — press any button to join", label);
                    continue;
                }
                var f = world.FindFighter(p.FighterId);
                if (f == null) continue;
                var hero = session.Content[f.HeroIndex];
                GUI.Label(new Rect(x, 10, panelWidth, 24), "<b>P" + (slot + 1) + " " + hero.Name + "</b>", label);
                Bar(new Rect(x, 34, panelWidth - 30, 10), f.Health / (float)hero.MaxHealth, Color.green);
                for (int bar = 0; bar < 3; bar++)
                {
                    float fill = Mathf.Clamp01((f.Gauge - bar * SimConstants.GaugePerBar) / (float)SimConstants.GaugePerBar);
                    Bar(new Rect(x + bar * ((panelWidth - 30) / 3f), 48, (panelWidth - 36) / 3f, 8), fill, new Color(0.2f, 0.6f, 1f));
                }
                string assist = p.AssistHeroIndex < 0 ? "-" : session.Content[p.AssistHeroIndex].Name +
                    (p.AssistCooldown > 0 ? " (" + (p.AssistCooldown / 60f).ToString("0.0") + "s)" : " READY");
                GUI.Label(new Rect(x, 60, panelWidth, 20), "Assist: " + assist, label);
                GUI.Label(new Rect(x, 78, panelWidth, 32), "<color=#ffcc33>" + RankName(p.Rank) + "</color>", big);
                GUI.Label(new Rect(x, 112, panelWidth, 20), "Score " + p.Score + "   Best " + p.BestCombo + " hits", label);
                if (hero.Mechanic != HeroMechanic.None)
                {
                    var (meterName, state, color) = Mechanic(hero.Mechanic, f, session.World);
                    Bar(new Rect(x, 136, panelWidth - 30, 8), f.Meter / (float)SimConstants.MechanicMax, color);
                    GUI.Label(new Rect(x, 146, panelWidth, 20), meterName + (state.Length > 0 ? "  —  " + state : ""), label);
                }
            }

            int combo = 0;
            foreach (var f in world.Fighters)
                if (f.ComboOwnerSlot >= 0 && f.ComboHits > combo) combo = f.ComboHits;
            if (combo > 1)
                GUI.Label(new Rect(Screen.width / 2f - 80, Screen.height * 0.3f, 300, 40), "<color=#ff4444>" + combo + " HITS!</color>", big);

            var meterRect = new Rect(Screen.width / 2f - 200, Screen.height - 30, 400, 14);
            bool full = world.AssembleMeter >= SimConstants.AssembleMeterMax;
            Bar(meterRect, world.AssembleMeter / (float)SimConstants.AssembleMeterMax, full ? Color.magenta : new Color(0.6f, 0.3f, 0.8f));
            GUI.Label(new Rect(meterRect.x, meterRect.y - 22, 400, 22), full ? "<b>ASSEMBLE READY — everyone press L3+R3!</b>" : "Assemble", label);

            if (showFrameData)
                GUI.Label(new Rect(10, Screen.height - 50, 600, 20),
                    "Frame " + world.Frame + "   Rollbacks " + session.Session.RollbacksPerformed + "   Checksum " + world.Checksum().ToString("X16"), label);
        }

        static (string name, string state, Color color) Mechanic(HeroMechanic mechanic, Fighter f, SimWorld world)
        {
            switch (mechanic)
            {
                case HeroMechanic.Heat:
                    return ("Heat", world.IsOverheated(f) ? "<color=#ff5522><b>OVERHEATED</b></color>" : f.Meter >= SimConstants.VentMinHeat ? "D-pad ↓ to vent" : "",
                        Color.Lerp(new Color(1f, 0.8f, 0.2f), new Color(1f, 0.2f, 0.1f), f.Meter / (float)SimConstants.MechanicMax));
                case HeroMechanic.Rage:
                    string[] tiers = { "Hulk", "Savage", "World Breaker", "WORLD BREAKER (Gamma Rage)" };
                    return ("Rage", tiers[world.MechanicTier(f)], new Color(0.3f, 0.9f, 0.3f));
                case HeroMechanic.Density:
                    return ("Density", world.IsPhased(f) ? "PHASE" : world.IsDiamond(f) ? "DIAMOND" : "solid", new Color(0.4f, 0.9f, 1f));
                case HeroMechanic.Size:
                    return ("Pym Particles", world.IsSmall(f) ? "ANT-SIZE" : world.IsGiant(f) ? "GIANT" : "normal", new Color(0.9f, 0.2f, 0.25f));
                default:
                    return ("", "", Color.white);
            }
        }

        static string RankName(StyleRank r) => r == StyleRank.Assemble ? "ASSEMBLE!" : r.ToString();

        static void Bar(Rect r, float fill, Color color)
        {
            var old = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.6f);
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = color;
            GUI.DrawTexture(new Rect(r.x, r.y, r.width * Mathf.Clamp01(fill), r.height), Texture2D.whiteTexture);
            GUI.color = old;
        }
    }
}
