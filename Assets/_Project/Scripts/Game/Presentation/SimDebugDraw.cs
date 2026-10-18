using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// Training-lab overlay: draws the sim's actual hurtboxes (blue), active hitboxes (red),
    /// projectiles, debris and destructibles in the Scene view while playing.
    /// </summary>
    public sealed class SimDebugDraw : MonoBehaviour
    {
        [SerializeField] GameSession session;

        void OnDrawGizmos()
        {
            if (session == null || session.World == null) return;
            var world = session.World;

            foreach (var f in world.Fighters)
            {
                var hero = world.Content[f.HeroIndex];
                var half = SimConvert.ToUnity(hero.BodyHalfExtents);
                Gizmos.color = f.InvulnFrames > 0 ? new Color(1f, 1f, 1f, 0.5f) : new Color(0.2f, 0.5f, 1f, 0.8f);
                Gizmos.DrawWireCube(SimConvert.ToUnity(f.Pos) + Vector3.up * half.y, half * 2f);

                if (f.Mode != FighterMode.Attack || f.MoveIndex < 0) continue;
                var m = hero.Moves[f.MoveIndex];
                if (!m.HasHitbox || f.MoveFrame < m.ActiveStart || f.MoveFrame > m.ActiveEnd) continue;
                var off = SimConvert.ToUnity(m.HitboxOffset);
                off.x *= f.Facing;
                Gizmos.color = new Color(1f, 0.15f, 0.1f, 0.9f);
                Gizmos.DrawWireCube(SimConvert.ToUnity(f.Pos) + off, SimConvert.ToUnity(m.HitboxHalfExtents) * 2f);
            }

            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.9f);
            foreach (var p in world.Projectiles)
            {
                var spec = world.Content[p.HeroIndex].Moves[p.MoveIndex].Projectile;
                Gizmos.DrawWireCube(SimConvert.ToUnity(p.Pos), SimConvert.ToUnity(spec.HalfExtents) * 2f);
            }
            foreach (var d in world.Debris)
            {
                var half = SimConvert.ToUnity(d.HalfExtents);
                Gizmos.color = d.Mode == DebrisMode.Flying ? Color.red : Color.yellow;
                Gizmos.DrawWireCube(SimConvert.ToUnity(d.Pos) + Vector3.up * half.y, half * 2f);
            }
            for (int i = 0; i < world.Destructibles.Length; i++)
            {
                var def = world.Stage.Destructibles[i];
                var s = world.Destructibles[i];
                Gizmos.color = s.Broken ? new Color(0.4f, 0.4f, 0.4f, 0.3f) : Color.Lerp(Color.red, Color.green, s.Health / (float)def.Health);
                Gizmos.DrawWireCube(SimConvert.ToUnity(def.Center), SimConvert.ToUnity(def.HalfExtents) * 2f);
            }
        }
    }
}
