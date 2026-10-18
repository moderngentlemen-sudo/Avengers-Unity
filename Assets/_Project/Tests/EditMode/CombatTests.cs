using NUnit.Framework;
using Reforged.Sim;

namespace Reforged.Tests
{
    public sealed class CombatTests
    {
        const string Cap = GrayboxContent.CaptainAmerica;
        const string Tony = GrayboxContent.IronMan;

        [Test]
        public void FixedPointMathIsExact()
        {
            Assert.AreEqual(Fix.Ratio(3, 2), Fix.FromInt(3) / 2);
            Assert.AreEqual(Fix.FromInt(6), Fix.FromInt(2) * Fix.FromInt(3));
            Assert.AreEqual(Fix.Cents(25), Fix.One / 4);
            Assert.AreEqual(Fix.Zero, Fix.Damp(Fix.FromRaw(-1), Fix.Ratio(3, 4), Fix.Cents(1)));
        }

        [Test]
        public void LightStringChainsIntoFourHitCombo()
        {
            var rig = new SimTestRig();
            var cap = rig.Player(0, Cap, 0);
            var dummy = rig.Dummy(1);
            int best = 0;
            for (int i = 0; i < 8; i++)
            {
                rig.Tap(0, Buttons.Light);
                for (int k = 0; k < 6; k++) { rig.Run(1); best = System.Math.Max(best, dummy.ComboHits); }
            }
            Assert.GreaterOrEqual(best, 4, "Jab > Cross > Shield Bash > Liberty Kick should all connect");
            Assert.Greater(rig.World.Players[0].StylePoints, 0);
            Assert.Greater(cap.Gauge, 0);
        }

        [Test]
        public void CancelHierarchyOnlyGoesUp()
        {
            var rig = new SimTestRig();
            var cap = rig.Player(0, Cap, 0);
            rig.Dummy(1);
            rig.Tap(0, Buttons.Light);
            rig.Run(6);
            Assert.IsTrue(cap.MoveHasHit, "jab should connect");
            rig.Tap(0, Buttons.Special);
            rig.Run(4);
            Assert.AreEqual("Shield Toss", rig.MoveName(cap), "a connected Light cancels into a Special");

            rig.Tap(0, Buttons.Light);
            rig.Run(1);
            Assert.AreEqual("Shield Toss", rig.MoveName(cap), "a Special cannot cancel down into a Light");
        }

        [Test]
        public void LauncherJugglesAndJuggleDecayEndsInfinites()
        {
            var rig = new SimTestRig();
            rig.Player(0, Cap, 0);
            var dummy = rig.Dummy(1);
            rig.Tap(0, Buttons.Heavy, 0, 127);
            rig.Run(20);
            Assert.AreEqual(FighterMode.Juggle, dummy.Mode);
            Assert.Greater(dummy.Pos.Y.Raw, Fix.One.Raw);

            var g0 = rig.World.EffectiveGravity(dummy);
            dummy.JugglePoints += 20;
            Assert.Greater(rig.World.EffectiveGravity(dummy).Raw, g0.Raw, "gravity rises with juggle points");
        }

        [Test]
        public void ExhaustedJuggleDropsOutInsteadOfTakingMoreHits()
        {
            var rig = new SimTestRig();
            rig.Player(0, Cap, 0);
            var dummy = rig.Dummy(1, Fix.Cents(60));
            dummy.Mode = FighterMode.Juggle;
            dummy.Vel = new FixVec3(Fix.Zero, Fix.Cents(12), Fix.Zero);
            dummy.JugglePoints = SimConstants.MaxJugglePoints - 1;
            int health = dummy.Health;

            rig.Tap(0, Buttons.Light);
            rig.Run(6);
            Assert.IsTrue(dummy.JuggleExhausted || rig.Count(SimEventType.JuggleExhausted) > 0);
            Assert.AreEqual(health, dummy.Health);
        }

        [Test]
        public void TimedBlockParriesAndStaggersAttacker()
        {
            var rig = new SimTestRig();
            var cap = rig.Player(0, Cap, 0);
            var trooper = rig.Player(1, GrayboxContent.HydraTrooper, 1, team: 1);
            trooper.Facing = -1;
            rig.Tap(1, Buttons.Light);   // Trooper Jab: 8 startup
            rig.Run(4);
            rig.Set(0, InputFrame.Of(Buttons.Block));
            rig.Run(8);
            Assert.AreEqual(1, rig.Count(SimEventType.Parry));
            Assert.AreEqual(FighterMode.Hitstun, trooper.Mode);
            Assert.AreEqual(1000, cap.Health);
        }

        [Test]
        public void EarlyBlockGuardsWithoutParry()
        {
            var rig = new SimTestRig();
            var cap = rig.Player(0, Cap, 0);
            var trooper = rig.Player(1, GrayboxContent.HydraTrooper, 1, team: 1);
            trooper.Facing = -1;
            rig.Set(0, InputFrame.Of(Buttons.Block));
            rig.Run(10);
            rig.Tap(1, Buttons.Light);
            rig.Run(10);
            Assert.AreEqual(0, rig.Count(SimEventType.Parry));
            Assert.AreEqual(1, rig.Count(SimEventType.Blocked));
            Assert.AreEqual(1000, cap.Health);
        }

        [Test]
        public void ArmoredEnemiesNeedHeavyHitsToBreak()
        {
            var rig = new SimTestRig();
            rig.Player(0, Cap, 0);
            var brute = rig.World.SpawnEnemy(rig.Hero(GrayboxContent.HydraHeavy), new FixVec3(Fix.Cents(120), 0, 0));
            brute.AiCooldown = 9999;
            rig.Tap(0, Buttons.Light);
            rig.Run(20);
            Assert.AreEqual(3, brute.ArmorLeft, "lights don't chip armor");
            for (int i = 0; i < 3; i++)
            {
                rig.Tap(0, Buttons.Heavy);
                rig.Run(40);
            }
            Assert.AreEqual(1, rig.Count(SimEventType.ArmorBreak));
        }

        [Test]
        public void EachHeroHasTwoUltimates()
        {
            var content = GrayboxContent.Create();
            foreach (var name in new[] { Cap, Tony })
            {
                var hero = content[content.IndexOf(name)];
                Assert.GreaterOrEqual(hero.FindMove(MoveInput.UltimateI, StickDir.Neutral, false), 0, name + " Ultimate I");
                Assert.GreaterOrEqual(hero.FindMove(MoveInput.UltimateII, StickDir.Neutral, false), 0, name + " Ultimate II");
            }
        }

        [Test]
        public void UltimateOneAndTwoUseSeparateInputs()
        {
            var rig = new SimTestRig();
            var cap = rig.Player(0, Cap, 0);
            var tony = rig.Player(1, Tony, -2);
            rig.Dummy(3);

            cap.Gauge = 200;
            rig.Tap(0, Buttons.Ultimate);
            Assert.AreNotEqual(FighterMode.Attack, cap.Mode, "ultimates cost a full 3 bars");

            cap.Gauge = 300;
            rig.Tap(0, Buttons.Ultimate);
            Assert.AreEqual("Avengers Assemble", rig.MoveName(cap));
            Assert.AreEqual(0, cap.Gauge);
            rig.Run(240);   // 100 frames of move plus hitstop from eight hits
            Assert.AreNotEqual(FighterMode.Attack, cap.Mode);

            cap.Gauge = 300;
            rig.Tap(0, Buttons.HeroMod | Buttons.Ultimate);
            Assert.AreEqual("Star-Spangled Stand", rig.MoveName(cap));
            rig.Run(40);
            Assert.Greater(tony.BuffFrames, 0, "Ultimate II buffs the whole team");
        }

        [Test]
        public void TeamUltimateNeedsEveryPlayer()
        {
            var rig = new SimTestRig();
            rig.Player(0, Cap, 0);
            rig.Player(1, Tony, -2);
            var dummy = rig.Dummy(3);
            rig.World.AssembleMeter = SimConstants.AssembleMeterMax;

            rig.Tap(0, Buttons.Ultimate);
            rig.Run(2);
            Assert.AreEqual(0, rig.Count(SimEventType.TeamUltimate), "one vote is not enough with two players");

            rig.Tap(1, Buttons.Ultimate);
            Assert.AreEqual(1, rig.Count(SimEventType.TeamUltimate));
            Assert.AreEqual(0, rig.World.AssembleMeter);
            Assert.Less(dummy.Health, rig.Content[dummy.HeroIndex].MaxHealth);
            Assert.AreEqual(FighterMode.Juggle, dummy.Mode);
        }
    }
}
