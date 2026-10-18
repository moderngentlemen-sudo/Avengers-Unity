using NUnit.Framework;
using Reforged.Sim;

namespace Reforged.Tests
{
    public sealed class LobbyTests
    {
        const string Cap = GrayboxContent.CaptainAmerica;
        const string Tony = GrayboxContent.IronMan;

        static Lobby NewLobby(bool duplicates = true)
        {
            return new Lobby(GrayboxContent.Create(), RosterCatalog.All) { AllowDuplicateHeroes = duplicates };
        }

        static void Pick(Lobby lobby, int slot, string name)
        {
            var s = lobby.Slots[slot];
            var list = s.Phase == LobbyPhase.PickHero ? lobby.HeroChoices : lobby.AssistChoices;
            int index = list.FindIndex(e => e.Name == name);
            Assert.GreaterOrEqual(index, 0, name + " is not on the select screen");
            if (s.Phase == LobbyPhase.PickHero) s.HeroCursor = index; else s.AssistCursor = index;
            Assert.IsTrue(lobby.Confirm(slot), "could not pick " + name);
        }

        [Test]
        public void FullRosterIsShownButOnlyImplementedHeroesArePickable()
        {
            var lobby = NewLobby();
            Assert.AreEqual(14, lobby.HeroChoices.Count, "10 heroes + 4 unlockables");
            Assert.IsTrue(lobby.HeroChoices.Exists(e => e.Name == "Vision"));
            Assert.IsTrue(lobby.HeroChoices.Exists(e => e.Name == "Ant-Man"));
            Assert.IsFalse(lobby.HeroChoices.Exists(e => e.Name == GrayboxContent.NickFury), "cameos are assist-only");

            lobby.Join(0);
            Assert.IsTrue(lobby.CanPlay(0, lobby.HeroChoices.Find(e => e.Name == Cap)));
            Assert.IsFalse(lobby.CanPlay(0, lobby.HeroChoices.Find(e => e.Name == "Thor")), "not built yet");
            Assert.IsFalse(lobby.CanPlay(0, lobby.HeroChoices.Find(e => e.Name == "Spider-Man")), "locked unlockable");
        }

        [Test]
        public void AssistMustBeSomeoneNobodyIsPlaying()
        {
            var lobby = NewLobby();
            lobby.Join(0);
            lobby.Join(1);
            Pick(lobby, 0, Cap);
            Pick(lobby, 1, Tony);

            Assert.IsFalse(lobby.CanAssist(0, lobby.AssistChoices.Find(e => e.Name == Tony)), "Iron Man is being played");
            Assert.IsFalse(lobby.CanAssist(0, lobby.AssistChoices.Find(e => e.Name == Cap)), "can't assist yourself");
            Assert.AreEqual(GrayboxContent.NickFury, lobby.AssistChoices[lobby.Slots[0].AssistCursor].Name, "cursor jumps to the first legal assist");
            Pick(lobby, 0, GrayboxContent.NickFury);
            Pick(lobby, 1, GrayboxContent.WarMachine);

            Assert.IsTrue(lobby.AllReady);
            var setup = lobby.Build();
            Assert.AreEqual(Cap, setup.Heroes[0]);
            Assert.AreEqual(GrayboxContent.WarMachine, setup.Assists[1]);
            Assert.IsNull(setup.Heroes[2]);
        }

        [Test]
        public void LockingInAHeroBumpsAnyoneUsingThemAsAssist()
        {
            var lobby = NewLobby();
            lobby.Join(0);
            Pick(lobby, 0, Cap);
            Pick(lobby, 0, Tony);
            Assert.AreEqual(LobbyPhase.Ready, lobby.Slots[0].Phase);

            lobby.Join(1);
            Pick(lobby, 1, Tony);
            Assert.AreEqual(LobbyPhase.PickAssist, lobby.Slots[0].Phase, "P1 lost Iron Man as assist when P2 picked him");
            Assert.IsNull(lobby.Slots[0].Assist);
            Assert.IsFalse(lobby.AllReady);
        }

        [Test]
        public void DuplicateHeroesCanBeDisabled()
        {
            var lobby = NewLobby(duplicates: false);
            lobby.Join(0);
            lobby.Join(1);
            Pick(lobby, 0, Cap);
            Assert.IsFalse(lobby.CanPlay(1, lobby.HeroChoices.Find(e => e.Name == Cap)));
            lobby.Back(0);
            Assert.IsTrue(lobby.CanPlay(1, lobby.HeroChoices.Find(e => e.Name == Cap)), "backing out frees the hero");
        }

        [Test]
        public void CameoAssistsWorkInTheSimulation()
        {
            var rig = new SimTestRig();
            rig.Player(0, Cap, 0, assist: GrayboxContent.NickFury);
            rig.Dummy(2);
            rig.Tap(0, Buttons.Assist, 127);   // LT + forward: Helicarrier Strike
            var assist = rig.World.Fighters.Find(f => f.IsAssist);
            Assert.AreEqual("Helicarrier Strike", rig.MoveName(assist));
            rig.Run(60);
            Assert.Greater(rig.Count(SimEventType.Hit), 0);
        }
    }
}
