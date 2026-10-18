using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// Keeps every player hero in the shared Cinemachine target group so the camera frames all
    /// four players, even when they split across stage levels. Assists get a small weight so
    /// the camera leans toward them as they swoop in.
    /// </summary>
    public sealed class CameraDirector : MonoBehaviour
    {
        [SerializeField] GameSession session;
        [SerializeField] CinemachineTargetGroup targetGroup;
        [SerializeField] float playerRadius = 2f;
        [SerializeField] float assistWeight = 0.35f;

        readonly HashSet<Transform> tracked = new HashSet<Transform>();
        readonly List<Transform> scratch = new List<Transform>();

        void LateUpdate()
        {
            if (session == null || targetGroup == null) return;
            scratch.Clear();
            foreach (var view in session.FighterViews.Values)
            {
                if (view.PlayerSlot < 0 && !view.IsAssist) continue;
                scratch.Add(view.transform);
                if (tracked.Add(view.transform))
                    targetGroup.AddMember(view.transform, view.IsAssist ? assistWeight : 1f, playerRadius);
            }
            tracked.RemoveWhere(t =>
            {
                if (t != null && scratch.Contains(t)) return false;
                if (t != null) targetGroup.RemoveMember(t);
                return true;
            });
        }
    }
}
