using System.Collections.Generic;
using Reforged.Sim;

namespace Reforged.Tests
{
    /// <summary>Drives a SimWorld frame by frame with scripted input and records every event.</summary>
    public sealed class SimTestRig
    {
        public readonly ContentDatabase Content = GrayboxContent.Create();
        public readonly SimWorld World;
        public readonly List<SimEvent> Events = new List<SimEvent>();
        readonly InputFrame[] inputs = new InputFrame[SimConstants.MaxPlayers];

        public SimTestRig(uint seed = 7)
        {
            World = new SimWorld(Content, GrayboxStages.TestRoom(), seed);
        }

        public int Hero(string name) => Content.IndexOf(name);

        public Fighter Player(int slot, string hero, int x, int y = 0, string assist = null, byte team = 0)
        {
            var assistIndex = assist == null ? -1 : Hero(assist);
            return World.SpawnPlayer(slot, Hero(hero), new FixVec3(x, y, 0), assistIndex, team);
        }

        public Fighter PlayerAt(int slot, string hero, Fix x, string assist = null, byte team = 0) =>
            World.SpawnPlayer(slot, Hero(hero), new FixVec3(x, 0, 0), assist == null ? -1 : Hero(assist), team);

        public Fighter Dummy(Fix x, Fix y = default(Fix))
        {
            return World.SpawnEnemy(Hero(GrayboxContent.TrainingDummy), new FixVec3(x, y, Fix.Zero));
        }

        public void Set(int slot, InputFrame input) => inputs[slot] = input;

        /// <summary>Holds the input for one frame, then releases it.</summary>
        public void Tap(int slot, Buttons buttons, int moveX = 0, int moveZ = 0)
        {
            inputs[slot] = InputFrame.Of(buttons, moveX, moveZ);
            Run(1);
            inputs[slot] = default(InputFrame);
        }

        public void Run(int frames)
        {
            for (int i = 0; i < frames; i++)
            {
                World.Step(inputs);
                Events.AddRange(World.Events);
            }
        }

        public int Count(SimEventType type)
        {
            int n = 0;
            foreach (var e in Events) if (e.Type == type) n++;
            return n;
        }

        public string MoveName(Fighter f) =>
            f.MoveIndex < 0 ? null : Content.Heroes[f.HeroIndex].Moves[f.MoveIndex].Name;
    }
}
