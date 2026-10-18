using System;
using System.Collections.Generic;

namespace Reforged.Sim
{
    public enum LobbyPhase : byte { Empty, PickHero, PickAssist, Ready }

    public sealed class LobbySlot
    {
        public LobbyPhase Phase;
        public int HeroCursor, AssistCursor;
        public string Hero, Assist;
    }

    /// <summary>The picks a match starts with, indexed by player slot (null = empty slot).</summary>
    public sealed class MatchSetup
    {
        public readonly string[] Heroes = new string[SimConstants.MaxPlayers];
        public readonly string[] Assists = new string[SimConstants.MaxPlayers];
    }

    /// <summary>
    /// Character select rules, kept engine-free so they are unit tested:
    /// - Pick a hero (implemented, unlocked, and unique unless duplicates are allowed).
    /// - Then pick an Assist Callout partner: any implemented character with assist moves that
    ///   nobody is playing. Assist-only cameos are always eligible.
    /// - When someone locks in a hero, anyone who had that hero as their assist goes back to
    ///   picking an assist.
    /// </summary>
    public sealed class Lobby
    {
        readonly ContentDatabase content;
        readonly Func<string, bool> isUnlocked;
        public readonly LobbySlot[] Slots = new LobbySlot[SimConstants.MaxPlayers];
        public readonly List<RosterEntry> HeroChoices = new List<RosterEntry>();
        public readonly List<RosterEntry> AssistChoices = new List<RosterEntry>();

        /// <summary>The vertical slice only has two heroes, so mirror matches are on by default.</summary>
        public bool AllowDuplicateHeroes = true;

        public Lobby(ContentDatabase content, IEnumerable<RosterEntry> roster, Func<string, bool> isUnlocked = null)
        {
            this.content = content;
            this.isUnlocked = isUnlocked ?? (name => false);
            for (int i = 0; i < Slots.Length; i++) Slots[i] = new LobbySlot();
            foreach (var e in roster)
            {
                if (e.Role != RosterRole.AssistOnly) HeroChoices.Add(e);
                AssistChoices.Add(e);
            }
        }

        public bool IsImplemented(string name) => content.IndexOf(name) >= 0;

        public bool CanPlay(int slot, RosterEntry e)
        {
            if (e.Role == RosterRole.AssistOnly || !IsImplemented(e.Name)) return false;
            if (content[content.IndexOf(e.Name)].AssistOnly) return false;
            if (e.Role == RosterRole.Unlockable && !isUnlocked(e.Name)) return false;
            if (AllowDuplicateHeroes) return true;
            for (int i = 0; i < Slots.Length; i++)
                if (i != slot && HasLockedHero(i) && Slots[i].Hero == e.Name) return false;
            return true;
        }

        public bool CanAssist(int slot, RosterEntry e)
        {
            int index = content.IndexOf(e.Name);
            if (index < 0 || content[index].AssistMoves.Length == 0) return false;
            if (e.Role == RosterRole.Unlockable && !isUnlocked(e.Name)) return false;
            for (int i = 0; i < Slots.Length; i++)
                if (HasLockedHero(i) && Slots[i].Hero == e.Name) return false;
            return true;
        }

        bool HasLockedHero(int slot) => Slots[slot].Phase >= LobbyPhase.PickAssist && Slots[slot].Hero != null;

        public void Join(int slot)
        {
            var s = Slots[slot];
            if (s.Phase != LobbyPhase.Empty) return;
            s.Phase = LobbyPhase.PickHero;
            s.HeroCursor = FirstSelectable(HeroChoices, e => CanPlay(slot, e));
        }

        public void Leave(int slot)
        {
            Slots[slot] = new LobbySlot();
        }

        public void Move(int slot, int delta)
        {
            var s = Slots[slot];
            if (s.Phase == LobbyPhase.PickHero) s.HeroCursor = Wrap(s.HeroCursor + delta, HeroChoices.Count);
            else if (s.Phase == LobbyPhase.PickAssist) s.AssistCursor = Wrap(s.AssistCursor + delta, AssistChoices.Count);
        }

        public bool Confirm(int slot)
        {
            var s = Slots[slot];
            switch (s.Phase)
            {
                case LobbyPhase.PickHero:
                    var hero = HeroChoices[s.HeroCursor];
                    if (!CanPlay(slot, hero)) return false;
                    s.Hero = hero.Name;
                    s.Phase = LobbyPhase.PickAssist;
                    s.AssistCursor = FirstSelectable(AssistChoices, e => CanAssist(slot, e));
                    BumpAssistsUsing(hero.Name);
                    return true;
                case LobbyPhase.PickAssist:
                    var assist = AssistChoices[s.AssistCursor];
                    if (!CanAssist(slot, assist)) return false;
                    s.Assist = assist.Name;
                    s.Phase = LobbyPhase.Ready;
                    return true;
            }
            return false;
        }

        public void Back(int slot)
        {
            var s = Slots[slot];
            if (s.Phase == LobbyPhase.Ready)
            {
                s.Assist = null;
                s.Phase = LobbyPhase.PickAssist;
            }
            else if (s.Phase == LobbyPhase.PickAssist)
            {
                s.Hero = null;
                s.Phase = LobbyPhase.PickHero;
            }
        }

        public bool AllReady
        {
            get
            {
                bool any = false;
                foreach (var s in Slots)
                {
                    if (s.Phase == LobbyPhase.Empty) continue;
                    if (s.Phase != LobbyPhase.Ready) return false;
                    any = true;
                }
                return any;
            }
        }

        public MatchSetup Build()
        {
            if (!AllReady) throw new InvalidOperationException("Not everyone is ready");
            var setup = new MatchSetup();
            for (int i = 0; i < Slots.Length; i++)
            {
                if (Slots[i].Phase != LobbyPhase.Ready) continue;
                setup.Heroes[i] = Slots[i].Hero;
                setup.Assists[i] = Slots[i].Assist;
            }
            return setup;
        }

        void BumpAssistsUsing(string hero)
        {
            for (int i = 0; i < Slots.Length; i++)
            {
                var s = Slots[i];
                int slot = i;
                if (s.Phase == LobbyPhase.Ready && s.Assist == hero)
                {
                    s.Assist = null;
                    s.Phase = LobbyPhase.PickAssist;
                }
                // Anyone still choosing whose cursor sits on a now-illegal partner gets moved off it.
                if (s.Phase == LobbyPhase.PickAssist && !CanAssist(slot, AssistChoices[s.AssistCursor]))
                    s.AssistCursor = FirstSelectable(AssistChoices, e => CanAssist(slot, e));
            }
        }

        static int FirstSelectable(List<RosterEntry> list, Func<RosterEntry, bool> ok)
        {
            for (int i = 0; i < list.Count; i++)
                if (ok(list[i])) return i;
            return 0;
        }

        static int Wrap(int v, int n) => ((v % n) + n) % n;
    }
}
