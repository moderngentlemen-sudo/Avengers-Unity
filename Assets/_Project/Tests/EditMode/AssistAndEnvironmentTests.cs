using NUnit.Framework;
using Reforged.Sim;

namespace Reforged.Tests
{
    public sealed class AssistAndEnvironmentTests
    {
        const string Cap = GrayboxContent.CaptainAmerica;
        const string Tony = GrayboxContent.IronMan;

        [Test]
        public void AssistCalloutSpawnsPartnerThenCoolsDown()
        {
            var rig = new SimTestRig();
            rig.Player(0, Cap, 0, assist: Tony);
            var dummy = rig.Dummy(3);

            rig.Tap(0, Buttons.Assist);
            var assist = rig.World.Fighters.Find(f => f.IsAssist);
            Assert.IsNotNull(assist, "LT calls the assist partner in");
            Assert.AreEqual(rig.Hero(Tony), assist.HeroIndex);
            Assert.AreEqual("Repulsor Blast", rig.MoveName(assist), "neutral LT = assist variant 1");
            Assert.AreEqual(SimConstants.AssistCooldownFrames - 1, rig.World.Players[0].AssistCooldown, "cooldown starts ticking the same frame");
            Assert.AreEqual(1, rig.Count(SimEventType.AssistCalled));

            rig.Run(10);
            rig.Tap(0, Buttons.Assist);
            Assert.AreEqual(1, rig.Count(SimEventType.AssistCalled), "cooldown blocks a second call");
            Assert.AreEqual(1, rig.Count(SimEventType.AssistNotReady));

            rig.Run(120);
            Assert.IsFalse(rig.World.Fighters.Exists(f => f.IsAssist), "assists leave after their move");
            Assert.Less(dummy.Health, rig.Content[dummy.HeroIndex].MaxHealth);
            Assert.Greater(rig.Count(SimEventType.Hit), 0, "the assist's repulsor hit the dummy");
        }

        [Test]
        public void AssistDirectionPicksVariant()
        {
            var rig = new SimTestRig();
            var cap = rig.Player(0, Cap, 0, assist: Tony);
            rig.Tap(0, Buttons.Assist, 0, -127);
            var assist = rig.World.Fighters.Find(f => f.IsAssist);
            Assert.AreEqual("Boot-Jet Launcher", rig.MoveName(assist), "down + LT = assist variant 3");
            Assert.AreEqual(cap.Facing, assist.Facing);
        }

        [Test]
        public void ComboRelayHandsJuggleToTeammate()
        {
            var rig = new SimTestRig();
            rig.Player(0, Cap, 0, assist: Tony);
            rig.Player(1, Tony, 3);
            var dummy = rig.Dummy(1);
            rig.Tap(0, Buttons.Heavy, 0, 127);   // Star-Spangled Uppercut
            rig.Run(16);
            Assert.AreEqual(FighterMode.Juggle, dummy.Mode);
            int before = dummy.JugglePoints;

            rig.Tap(0, Buttons.Assist);
            rig.Run(1);
            Assert.AreEqual(1, rig.Count(SimEventType.ComboRelay));
            Assert.AreEqual(before * 6 / 10, dummy.JugglePoints, "relay refunds juggle points");
            Assert.IsFalse(rig.World.Fighters.Exists(f => f.IsAssist), "a relay replaces the assist call");
        }

        [Test]
        public void ShieldDropCollapsesWeakFloorToLevelBelow()
        {
            var rig = new SimTestRig();
            var cap = rig.Player(0, Cap, 6, 4);
            Assert.IsTrue(cap.IsGrounded, "spawned standing on the weak floor");
            rig.Tap(0, Buttons.Jump);
            rig.Run(8);
            rig.Tap(0, Buttons.Heavy, 0, -127);
            Assert.AreEqual("Shield Drop", rig.MoveName(cap));
            rig.Run(120);

            int floor = rig.World.Stage.FindDestructible(GrayboxStages.WeakFloor);
            Assert.IsTrue(rig.World.Destructibles[floor].Broken);
            Assert.AreEqual(1, rig.Count(SimEventType.FloorCollapse));
            Assert.AreEqual(Fix.Zero, cap.Pos.Y, "fell through to the ground level");
            Assert.AreEqual(rig.World.Stage.FindPlatform(GrayboxStages.Ground), cap.GroundPlatform);
        }

        [Test]
        public void WallBounceSmashesThroughBreakableWall()
        {
            var rig = new SimTestRig();
            rig.PlayerAt(0, Cap, Fix.Cents(2190));
            var dummy = rig.Dummy(23);
            rig.Tap(0, Buttons.Heavy, 127);   // Shield Charge
            rig.Run(60);
            int wall = rig.World.Stage.FindDestructible(GrayboxStages.Drywall);
            Assert.IsTrue(rig.World.Destructibles[wall].Broken);
            Assert.AreEqual(1, rig.Count(SimEventType.WallBreak));
            Assert.Greater(dummy.Pos.X.Raw, Fix.FromInt(24).Raw, "the victim ends up in the next room");
        }

        [Test]
        public void CeilingBounceLaunchesVictimToUpperLevel()
        {
            var rig = new SimTestRig();
            rig.PlayerAt(0, Cap, Fix.Cents(1320));
            var dummy = rig.Dummy(14);
            rig.Tap(0, Buttons.Special, 0, 127);   // Rising Shield Slash
            rig.Run(120);
            Assert.AreEqual(1, rig.Count(SimEventType.CeilingBreakthrough));
            Assert.AreEqual(Fix.FromInt(4), dummy.Pos.Y, "landed on the floor above");
            Assert.AreEqual(rig.World.Stage.FindPlatform(GrayboxStages.UpperFloor), dummy.GroundPlatform);
        }

        [Test]
        public void BrokenPropDropsDebrisThatCanBeThrownForEnvironmentalHits()
        {
            var rig = new SimTestRig();
            var cap = rig.PlayerAt(0, Cap, Fix.Cents(-730));
            rig.Tap(0, Buttons.Heavy);   // Haymaker into the crate
            rig.Run(60);
            int crate = rig.World.Stage.FindDestructible(GrayboxStages.Crate);
            Assert.IsTrue(rig.World.Destructibles[crate].Broken);
            Assert.AreEqual(1, rig.World.Debris.Count);
            var debris = rig.World.Debris[0];
            Assert.AreEqual(DebrisMode.Resting, debris.Mode);

            cap.Pos = new FixVec3(debris.Pos.X - Fix.Cents(80), Fix.Zero, Fix.Zero);
            cap.Facing = 1;
            var dummy = rig.Dummy(debris.Pos.X + 3);
            rig.Tap(0, Buttons.Grab);
            Assert.AreEqual(DebrisMode.Held, debris.Mode);
            rig.Run(2);
            rig.Tap(0, Buttons.Light);
            Assert.AreEqual(DebrisMode.Flying, debris.Mode);
            rig.Run(40);
            Assert.AreEqual(1, rig.Count(SimEventType.EnvironmentalHit));
            Assert.Less(dummy.Health, rig.Content[dummy.HeroIndex].MaxHealth);
        }

        [Test]
        public void ShieldTossRicochetsAndReturnsForCatch()
        {
            var rig = new SimTestRig();
            var cap = rig.Player(0, Cap, 0);
            rig.Dummy(3);
            rig.Dummy(5);
            rig.Tap(0, Buttons.Special);
            rig.Run(150);
            Assert.GreaterOrEqual(rig.Count(SimEventType.Ricochet), 1);
            Assert.AreEqual(1, rig.Count(SimEventType.ProjectileCaught));
            Assert.AreEqual(0, rig.World.Projectiles.Count);
            Assert.Greater(cap.Gauge, 0);
        }
    }
}
