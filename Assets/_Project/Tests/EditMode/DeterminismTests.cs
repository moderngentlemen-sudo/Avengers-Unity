using System;
using NUnit.Framework;
using Reforged.Sim;

namespace Reforged.Tests
{
    public sealed class DeterminismTests
    {
        static readonly Buttons[] Palette =
        {
            Buttons.None, Buttons.Light, Buttons.Heavy, Buttons.Special, Buttons.Jump, Buttons.Grab, Buttons.Block,
            Buttons.Assist, Buttons.HeroMod | Buttons.Light, Buttons.HeroMod | Buttons.Assist, Buttons.Ultimate, Buttons.ToolDown,
        };

        /// <summary>Pseudo-random but "held for a while" inputs, like a real player mashing.</summary>
        static InputFrame[][] ScriptInputs(int frames, int players, int seed)
        {
            var rng = new Random(seed);
            var script = new InputFrame[frames][];
            var current = new InputFrame[players];
            for (int f = 0; f < frames; f++)
            {
                script[f] = new InputFrame[SimConstants.MaxPlayers];
                for (int p = 0; p < players; p++)
                {
                    if (rng.Next(6) == 0)
                        current[p] = InputFrame.Of(Palette[rng.Next(Palette.Length)], rng.Next(-127, 128), rng.Next(-127, 128),
                            rng.Next(8) == 0 ? rng.Next(-127, 128) : 0, 0);
                    script[f][p] = current[p];
                }
            }
            return script;
        }

        static SimWorld NewBattle()
        {
            var content = GrayboxContent.Create();
            var world = new SimWorld(content, GrayboxStages.TestRoom(), 1234);
            world.SpawnPlayer(0, content.IndexOf(GrayboxContent.CaptainAmerica), new FixVec3(0, 0, 0), content.IndexOf(GrayboxContent.IronMan));
            world.SpawnPlayer(1, content.IndexOf(GrayboxContent.IronMan), new FixVec3(2, 0, 0), content.IndexOf(GrayboxContent.CaptainAmerica));
            world.SpawnEnemy(content.IndexOf(GrayboxContent.HydraTrooper), new FixVec3(4, 0, 0));
            world.SpawnEnemy(content.IndexOf(GrayboxContent.HydraTrooper), new FixVec3(-3, 0, 1));
            world.SpawnEnemy(content.IndexOf(GrayboxContent.HydraHeavy), new FixVec3(7, 0, 0));
            world.Fighters[0].Gauge = SimConstants.MaxGauge;
            return world;
        }

        [Test]
        public void SameInputsProduceIdenticalState()
        {
            var script = ScriptInputs(1200, 2, 99);
            var a = NewBattle();
            var b = NewBattle();
            for (int f = 0; f < script.Length; f++)
            {
                a.Step(script[f]);
                b.Step(script[f]);
            }
            Assert.AreEqual(a.Checksum(), b.Checksum());
            Assert.Greater(a.Frame, 0);
        }

        [Test]
        public void SnapshotRestoreReplaysIdentically()
        {
            var script = ScriptInputs(600, 2, 5);
            var world = NewBattle();
            for (int f = 0; f < 300; f++) world.Step(script[f]);
            var snapshot = world.SaveState();
            for (int f = 300; f < 600; f++) world.Step(script[f]);
            var expected = world.Checksum();

            world.LoadState(snapshot);
            Assert.AreEqual(300, world.Frame);
            for (int f = 300; f < 600; f++) world.Step(script[f]);
            Assert.AreEqual(expected, world.Checksum());
        }

        [Test]
        public void RollbackWithLateRemoteInputsMatchesPerfectInformation()
        {
            const int frames = 900, delay = 4;
            var script = ScriptInputs(frames, 2, 42);

            var reference = NewBattle();
            for (int f = 0; f < frames; f++) reference.Step(script[f]);

            var session = new RollbackSession(NewBattle(), 2);
            for (int f = 0; f < frames; f++)
            {
                session.AddInput(0, f, script[f][0]);
                if (f - delay >= 0) session.AddInput(1, f - delay, script[f - delay][1]);
                session.Advance();
            }
            for (int f = frames - delay; f < frames; f++) session.AddInput(1, f, script[f][1]);
            session.Resimulate();

            Assert.AreEqual(frames, session.Frame);
            Assert.Greater(session.RollbacksPerformed, 0, "the remote player's input changes should have forced rollbacks");
            Assert.AreEqual(reference.Checksum(), session.World.Checksum());
        }
    }
}
