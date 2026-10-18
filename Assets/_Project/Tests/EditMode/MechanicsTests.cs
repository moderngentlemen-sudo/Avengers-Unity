using NUnit.Framework;
using Reforged.Sim;

namespace Reforged.Tests
{
    public sealed class MechanicsTests
    {
        const string Tony = GrayboxContent.IronMan;
        const string Hulk = GrayboxContent.Hulk;
        const string Vision = GrayboxContent.Vision;
        const string AntMan = GrayboxContent.AntMan;

        static Fighter Trooper(SimTestRig rig, Fix x)
        {
            var t = rig.World.SpawnPlayer(1, rig.Hero(GrayboxContent.HydraTrooper), new FixVec3(x, 0, 0), -1, 1);
            t.Facing = -1;
            return t;
        }

        // ------------------------------------------------------------------ Heat (Iron Man)

        [Test]
        public void HeatOverheatsAndLocksOutHeatMoves()
        {
            var rig = new SimTestRig();
            var tony = rig.Player(0, Tony, 0);
            tony.Meter = 900;
            rig.Tap(0, Buttons.Special);   // Repulsor Blast: +180 heat
            Assert.AreEqual(1, rig.Count(SimEventType.Overheat));
            Assert.IsTrue(rig.World.IsOverheated(tony));
            rig.Run(40);

            rig.Tap(0, Buttons.Special);
            Assert.AreNotEqual(FighterMode.Attack, tony.Mode, "heat moves are locked while overheated");
            rig.Tap(0, Buttons.Light);
            Assert.AreEqual(FighterMode.Attack, tony.Mode, "punches still work");

            rig.Run(SimConstants.OverheatFrames);
            Assert.IsFalse(rig.World.IsOverheated(tony));
            Assert.AreEqual(0, tony.Meter);
        }

        [Test]
        public void HoverSlowsTheFallAndBuildsHeat()
        {
            float FallWithHover(bool hover, out int heat)
            {
                var rig = new SimTestRig();
                var tony = rig.Player(0, Tony, 0);
                rig.Tap(0, Buttons.Jump);
                rig.Run(22);                     // past the apex
                tony.AirJumpsUsed = 2;           // holding Jump must hover, not air-jump
                rig.Set(0, InputFrame.Of(hover ? Buttons.Jump : Buttons.None));
                rig.Run(12);
                heat = tony.Meter;
                return tony.Pos.Y.ToFloat();
            }
            float hovering = FallWithHover(true, out int hoverHeat);
            float falling = FallWithHover(false, out int fallHeat);
            Assert.Greater(hovering, falling);
            Assert.Greater(hoverHeat, fallHeat);
        }

        [Test]
        public void VentTurnsHeatIntoAnAreaBlast()
        {
            var rig = new SimTestRig();
            var tony = rig.Player(0, Tony, 0);
            var dummy = rig.Dummy(Fix.Cents(150));
            tony.Meter = 200;
            rig.Tap(0, Buttons.ToolDown);
            Assert.AreEqual(0, rig.Count(SimEventType.HeatVent), "needs 300+ heat");

            tony.Meter = 600;
            rig.Tap(0, Buttons.ToolDown);
            Assert.AreEqual(1, rig.Count(SimEventType.HeatVent));
            Assert.AreEqual(0, tony.Meter);
            Assert.AreEqual(FighterMode.Juggle, dummy.Mode);
        }

        // ------------------------------------------------------------------ Rage (Hulk)

        [Test]
        public void RageBuildsFromCombatAndUnlocksBiggerThrows()
        {
            var rig = new SimTestRig();
            var hulk = rig.Player(0, Hulk, 0);
            rig.Dummy(Fix.Cents(110));
            rig.Tap(0, Buttons.Light);
            rig.Run(10);
            Assert.Greater(hulk.Meter, 0, "dealing damage builds rage");

            hulk.Meter = 0;
            rig.Run(20);
            rig.Tap(0, Buttons.Grab);
            Assert.AreEqual("Gamma Toss", rig.MoveName(hulk));
            rig.Run(60);

            hulk.Meter = 400;
            rig.Run(1);
            Assert.AreEqual(1, rig.World.MechanicTier(hulk));
            Assert.AreEqual(1, rig.Count(SimEventType.RageTier));
            rig.Tap(0, Buttons.Grab);
            Assert.AreEqual("Savage Slam", rig.MoveName(hulk));
            rig.Run(80);

            hulk.Meter = 700;
            rig.Tap(0, Buttons.Grab);
            Assert.AreEqual("World Breaker Throw", rig.MoveName(hulk));
        }

        [Test]
        public void RagingHulkArmorsThroughHitsDuringHeavies()
        {
            Fighter HitDuringHaymaker(int rage, out SimTestRig rig)
            {
                rig = new SimTestRig();
                var hulk = rig.Player(0, Hulk, 0);
                Trooper(rig, Fix.Cents(120));
                hulk.Meter = rage;
                rig.Run(1);
                rig.Tap(0, Buttons.Heavy);     // Gamma Haymaker, 14f startup
                rig.Tap(1, Buttons.Light);     // Trooper Jab lands during the startup
                rig.Run(10);
                return hulk;
            }
            var calm = HitDuringHaymaker(0, out var calmRig);
            Assert.AreEqual(FighterMode.Hitstun, calm.Mode, "control: without rage the jab interrupts him");

            var savage = HitDuringHaymaker(400, out var rig2);
            Assert.Less(savage.Health, rig2.Content[savage.HeroIndex].MaxHealth, "he still takes the damage");
            Assert.AreEqual("Gamma Haymaker", rig2.MoveName(savage), "...but keeps swinging");
        }

        [Test]
        public void GammaRageIsWorldBreakerAndShrugsOffEverything()
        {
            var rig = new SimTestRig();
            var hulk = rig.Player(0, Hulk, 0);
            hulk.Gauge = SimConstants.MaxGauge;
            rig.Tap(0, Buttons.HeroMod | Buttons.Ultimate);
            Assert.AreEqual(3, rig.World.MechanicTier(hulk));
            rig.Run(60);

            Trooper(rig, Fix.Cents(120));
            rig.Tap(1, Buttons.Light);
            rig.Run(12);
            Assert.AreNotEqual(FighterMode.Hitstun, hulk.Mode);
            Assert.Greater(rig.Count(SimEventType.ArmorHit), 0);
        }

        [Test]
        public void ThunderclapLiftsNearbyDebris()
        {
            var rig = new SimTestRig();
            rig.Player(0, Hulk, 0);
            var debris = new Debris { Id = 999, Pos = new FixVec3(1, 0, 0), Weight = 1, Mode = DebrisMode.Resting, SupportPlatform = 0, Active = true };
            rig.World.Debris.Add(debris);
            rig.Tap(0, Buttons.Special, 0, -127);
            rig.Run(14);
            Assert.AreEqual(1, rig.Count(SimEventType.DebrisLifted));
            Assert.Greater(debris.Pos.Y.Raw, 0, "popped into the air, ready to be batted");
        }

        // ------------------------------------------------------------------ Density (Vision)

        [Test]
        public void PhaseIsIntangibleAndDrainsEnergy()
        {
            var rig = new SimTestRig();
            var vision = rig.Player(0, Vision, 0);
            var trooper = Trooper(rig, Fix.Cents(110));
            rig.Tap(1, Buttons.Light);
            rig.Run(12);
            Assert.Less(vision.Health, rig.Content[vision.HeroIndex].MaxHealth, "control: solid Vision gets hit");
            vision.Health = rig.Content[vision.HeroIndex].MaxHealth;
            rig.Run(30);

            rig.Tap(0, Buttons.ToolDown);
            Assert.IsTrue(rig.World.IsPhased(vision));
            rig.Tap(1, Buttons.Light);
            rig.Run(15);
            Assert.AreEqual(rig.Content[vision.HeroIndex].MaxHealth, vision.Health, "attacks pass through");
            Assert.Less(vision.Meter, SimConstants.MechanicMax);

            rig.Run(200);
            Assert.IsFalse(rig.World.IsPhased(vision), "energy ran out");
            Assert.AreEqual(1, rig.Count(SimEventType.MechanicDepleted));
        }

        [Test]
        public void PhasedVisionWalksThroughWalls()
        {
            float WalkRight(bool phase)
            {
                var rig = new SimTestRig();
                var vision = rig.Player(0, Vision, 23);
                if (phase) rig.Tap(0, Buttons.ToolDown);
                rig.Set(0, InputFrame.Of(Buttons.None, 127));
                rig.Run(40);
                return vision.Pos.X.ToFloat();
            }
            Assert.Less(WalkRight(false), 24f, "the drywall stops normal Vision");
            Assert.Greater(WalkRight(true), 24.5f, "phased Vision walks straight through");
        }

        [Test]
        public void PhaseStrikeCannotBeBlocked()
        {
            var rig = new SimTestRig();
            rig.Player(0, Vision, 0);
            Trooper(rig, Fix.Cents(100));
            rig.Set(1, InputFrame.Of(Buttons.Block));
            rig.Run(10);
            rig.Tap(0, Buttons.ToolDown);
            rig.Tap(0, Buttons.Light);
            rig.Run(10);
            Assert.AreEqual(0, rig.Count(SimEventType.Blocked));
            Assert.Greater(rig.Count(SimEventType.Hit), 0);
            Assert.GreaterOrEqual(rig.Count(SimEventType.PhaseStrike), 2, "re-solidify + connect");
        }

        [Test]
        public void DiamondArmorsAndBreaksFloorsWithAerials()
        {
            var rig = new SimTestRig();
            var vision = rig.Player(0, Vision, 0);
            Trooper(rig, Fix.Cents(110));
            rig.Tap(0, Buttons.ToolRight);
            Assert.IsTrue(rig.World.IsDiamond(vision));
            rig.Tap(1, Buttons.Light);
            rig.Run(12);
            Assert.AreNotEqual(FighterMode.Hitstun, vision.Mode);
            Assert.AreEqual(rig.Content[vision.HeroIndex].MaxHealth - 20, vision.Health, "Diamond halves damage (40 -> 20)");

            bool DiveBreaksFloor(bool diamond)
            {
                var r = new SimTestRig();
                var v = r.Player(0, Vision, 6, 4);
                if (diamond) r.Tap(0, Buttons.ToolRight);
                r.Tap(0, Buttons.Jump);
                r.Run(8);
                r.Tap(0, Buttons.Heavy);   // Diamond Dive
                r.Run(60);
                return r.World.Destructibles[r.World.Stage.FindDestructible(GrayboxStages.WeakFloor)].Broken;
            }
            Assert.IsFalse(DiveBreaksFloor(false));
            Assert.IsTrue(DiveBreaksFloor(true));
        }

        // ------------------------------------------------------------------ Size (Ant-Man)

        [Test]
        public void SizeChangesTheHurtboxAndCancelsMoves()
        {
            var rig = new SimTestRig();
            var ant = rig.Player(0, AntMan, 0);
            var normal = rig.World.HurtHalf(ant).Y;
            rig.Tap(0, Buttons.ToolDown);
            Assert.IsTrue(rig.World.IsSmall(ant));
            Assert.Less(rig.World.HurtHalf(ant).Y.Raw, normal.Raw);

            rig.Tap(0, Buttons.ToolRight);
            Assert.IsTrue(rig.World.IsGiant(ant));
            Assert.Greater(rig.World.HurtHalf(ant).Y.Raw, normal.Raw);

            rig.Tap(0, Buttons.ToolLeft);
            rig.Tap(0, Buttons.Heavy);
            Assert.AreEqual(FighterMode.Attack, ant.Mode);
            rig.Tap(0, Buttons.ToolDown);
            Assert.AreNotEqual(FighterMode.Attack, ant.Mode, "shrinking cancels the haymaker");
            Assert.IsTrue(rig.World.IsSmall(ant));
        }

        [Test]
        public void GiantHitsEveryLaneAndDrainsPymParticles()
        {
            int HitsAcrossLanes(bool giant)
            {
                var rig = new SimTestRig();
                var ant = rig.Player(0, AntMan, 0);
                rig.World.SpawnEnemy(rig.Hero(GrayboxContent.TrainingDummy), new FixVec3(Fix.Cents(150), 0, 2));
                if (giant) rig.Tap(0, Buttons.ToolRight);
                rig.Tap(0, Buttons.Light);
                rig.Run(10);
                return rig.Count(SimEventType.Hit);
            }
            Assert.AreEqual(0, HitsAcrossLanes(false), "a normal jab doesn't reach two lanes back");
            Assert.AreEqual(1, HitsAcrossLanes(true), "Giant-Man's does");

            var r = new SimTestRig();
            var a = r.Player(0, AntMan, 0);
            r.Tap(0, Buttons.ToolRight);
            r.Run(SimConstants.MechanicMax / SimConstants.GiantDrainPerFrame + 5);
            Assert.IsFalse(r.World.IsGiant(a), "out of Pym particles");
            Assert.AreEqual(1, r.Count(SimEventType.MechanicDepleted));
        }

        [Test]
        public void SmallAntManCritsThroughArmor()
        {
            var rig = new SimTestRig();
            var ant = rig.Player(0, AntMan, 0);
            var brute = rig.World.SpawnEnemy(rig.Hero(GrayboxContent.HydraHeavy), new FixVec3(1, 0, 0));
            brute.AiCooldown = 9999;
            rig.Tap(0, Buttons.ToolDown);
            rig.Tap(0, Buttons.Light);
            rig.Run(8);
            Assert.AreEqual(3, brute.ArmorLeft, "armor is bypassed, not broken");
            Assert.AreEqual(FighterMode.Hitstun, brute.Mode, "the brute flinches anyway");
            Assert.AreEqual(0, rig.Count(SimEventType.ArmorHit));
        }

        [Test]
        public void QuantumTunnelShrinksEnemies()
        {
            var rig = new SimTestRig();
            var ant = rig.Player(0, AntMan, 0);
            var dummy = rig.Dummy(3);
            var before = rig.World.HurtHalf(dummy).Y;
            ant.Gauge = SimConstants.MaxGauge;
            rig.Tap(0, Buttons.HeroMod | Buttons.Ultimate);
            Assert.Greater(dummy.ShrunkFrames, 0);
            Assert.Less(rig.World.HurtHalf(dummy).Y.Raw, before.Raw);
        }
    }
}
