using System.Collections.Generic;
using Reforged.Sim;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace Reforged.Game
{
    /// <summary>
    /// Plays a hero's clips as a pure function of simulation state: the clip is the current move
    /// (or mode) and the time is the sim's frame counter. No Animator state machine, so animation
    /// can't drift from gameplay, hitstop freezes it for free, and rollback re-renders correctly.
    /// Clip names come from the Blender pipeline: move names, plus FighterMode names for
    /// everything else (Idle, Walk, Hitstun...).
    /// </summary>
    public sealed class SimAnimationDriver : MonoBehaviour
    {
        static readonly HashSet<string> Looping = new HashSet<string> { "Idle", "Walk", "Guard", "Airborne", "Juggle" };

        readonly Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
        PlayableGraph graph;
        AnimationPlayableOutput output;
        AnimationClipPlayable current;
        string currentName;

        public void Init(Animator animator, IEnumerable<AnimationClip> source)
        {
            foreach (var clip in source)
                if (clip != null) clips[CleanName(clip.name)] = clip;
            animator.runtimeAnimatorController = null;
            animator.applyRootMotion = false;
            graph = PlayableGraph.Create(name + " Sim Animation");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            output = AnimationPlayableOutput.Create(graph, "Sim", animator);
        }

        public static string CleanName(string clipName)
        {
            int bar = clipName.LastIndexOf('|');
            return bar >= 0 ? clipName.Substring(bar + 1) : clipName;
        }

        /// <summary>Called once per sim tick after the fighter has been updated.</summary>
        public void Show(Fighter f, HeroDef hero)
        {
            if (!graph.IsValid()) return;
            string clipName;
            int frame;
            if (f.Mode == FighterMode.Attack && f.MoveIndex >= 0)
            {
                clipName = hero.Moves[f.MoveIndex].Name;
                frame = f.MoveFrame;
            }
            else
            {
                clipName = f.Mode.ToString();
                frame = f.ModeFrame;
            }
            if (!clips.TryGetValue(clipName, out var clip) && !clips.TryGetValue("Idle", out clip)) return;

            if (clipName != currentName)
            {
                if (current.IsValid()) current.Destroy();
                current = AnimationClipPlayable.Create(graph, clip);
                current.SetApplyFootIK(false);
                output.SetSourcePlayable(current);
                currentName = clipName;
            }

            double t = frame / (double)SimConstants.TicksPerSecond;
            double length = clip.length;
            t = Looping.Contains(clipName) && length > 0 ? t % length : System.Math.Min(t, length);
            current.SetTime(t);
            graph.Evaluate();
        }

        void OnDestroy()
        {
            if (graph.IsValid()) graph.Destroy();
        }
    }
}
