using System;
using System.Collections.Generic;
using System.IO;

namespace Reforged.Sim
{
    /// <summary>
    /// The deterministic game simulation. Given the same content, stage, seed and inputs it produces
    /// bit-identical state on every machine, so it can be snapshotted and re-simulated for rollback.
    /// Rules for code in this assembly: no floats, no UnityEngine, no Dictionary iteration, no
    /// wall-clock time, and every mutable field must be written by SaveState.
    /// </summary>
    public sealed partial class SimWorld
    {
        public readonly ContentDatabase Content;
        public readonly StageDef Stage;
        public readonly DeterministicRandom Rng;

        public readonly List<Fighter> Fighters = new List<Fighter>();
        public readonly List<Projectile> Projectiles = new List<Projectile>();
        public readonly List<Debris> Debris = new List<Debris>();
        public readonly PlayerState[] Players = new PlayerState[SimConstants.MaxPlayers];
        public readonly DestructibleState[] Destructibles;

        /// <summary>Shared team meter; when full, every player pressing L3+R3 fires a Team Ultimate.</summary>
        public int AssembleMeter;

        /// <summary>Presentation events from the most recent Step.</summary>
        public readonly List<SimEvent> Events = new List<SimEvent>();

        public int Frame { get; private set; }

        int nextEntityId = 1;

        public SimWorld(ContentDatabase content, StageDef stage, uint seed)
        {
            Content = content;
            Stage = stage;
            Rng = new DeterministicRandom(seed);
            Destructibles = new DestructibleState[stage.Destructibles.Count];
            for (int i = 0; i < Destructibles.Length; i++)
                Destructibles[i] = new DestructibleState { Health = stage.Destructibles[i].Health };
        }

        // ------------------------------------------------------------------ spawning

        public Fighter SpawnPlayer(int slot, int heroIndex, FixVec3 pos, int assistHeroIndex = -1, byte team = 0)
        {
            if (slot < 0 || slot >= SimConstants.MaxPlayers) throw new ArgumentOutOfRangeException(nameof(slot));
            if (Players[slot] != null) throw new InvalidOperationException("Slot " + slot + " is already taken");
            var f = CreateFighter(heroIndex, pos, team);
            f.PlayerSlot = (sbyte)slot;
            f.OwnerSlot = (sbyte)slot;
            Players[slot] = new PlayerState { Slot = slot, FighterId = f.Id, HeroIndex = heroIndex, AssistHeroIndex = assistHeroIndex };
            return f;
        }

        public Fighter SpawnEnemy(int heroIndex, FixVec3 pos, sbyte facing = -1)
        {
            var f = CreateFighter(heroIndex, pos, 1);
            f.Facing = facing;
            return f;
        }

        Fighter CreateFighter(int heroIndex, FixVec3 pos, byte team)
        {
            var hero = Content.Heroes[heroIndex];
            var f = new Fighter
            {
                Id = nextEntityId++,
                Team = team,
                HeroIndex = (short)heroIndex,
                Pos = pos,
                Health = hero.MaxHealth,
                ArmorLeft = hero.Armor,
            };
            f.GroundPlatform = (short)FindSupportPlatform(pos);
            f.Mode = f.IsGrounded ? FighterMode.Idle : FighterMode.Airborne;
            Fighters.Add(f);
            return f;
        }

        public Fighter FindFighter(int id)
        {
            for (int i = 0; i < Fighters.Count; i++)
                if (Fighters[i].Id == id) return Fighters[i];
            return null;
        }

        public Fighter PlayerFighter(int slot)
        {
            var p = slot >= 0 && slot < Players.Length ? Players[slot] : null;
            return p == null ? null : FindFighter(p.FighterId);
        }

        Debris FindDebris(int id)
        {
            for (int i = 0; i < Debris.Count; i++)
                if (Debris[i].Id == id) return Debris[i];
            return null;
        }

        bool HasActiveProjectile(int ownerId)
        {
            for (int i = 0; i < Projectiles.Count; i++)
                if (Projectiles[i].Active && Projectiles[i].OwnerId == ownerId) return true;
            return false;
        }

        // ------------------------------------------------------------------ tick

        /// <summary>Advances one tick. <paramref name="inputs"/> is indexed by player slot.</summary>
        public void Step(InputFrame[] inputs)
        {
            Events.Clear();

            // Inputs (including AI decisions) are gathered for everyone before anyone moves, so
            // every brain reads the same end-of-last-frame state.
            int count = Fighters.Count;
            for (int i = 0; i < count; i++) GatherInput(Fighters[i], inputs);
            for (int i = 0; i < count; i++) UpdateFighter(Fighters[i]);

            UpdateProjectiles();
            UpdateDebris();
            ResolveAttacks();
            UpdateCombos();
            UpdatePlayers();

            Fighters.RemoveAll(f => f.Mode == FighterMode.Despawned);
            Projectiles.RemoveAll(p => !p.Active);
            Debris.RemoveAll(d => !d.Active);
            Frame++;
        }

        void GatherInput(Fighter f, InputFrame[] inputs)
        {
            f.PrevInput = f.Input;
            if (f.PlayerSlot >= 0)
                f.Input = inputs != null && f.PlayerSlot < inputs.Length ? inputs[f.PlayerSlot] : default(InputFrame);
            else if (f.IsAssist)
                f.Input = default(InputFrame);
            else
                f.Input = EnemyBrain.Think(this, f);

            uint pressed = (uint)(f.Input.Held & ~f.PrevInput.Held);
            for (int b = 0; b < InputFrame.ButtonCount; b++)
                if ((pressed & (1u << b)) != 0) f.LastPress[b] = Frame;

            int mag = Math.Max(Math.Abs((int)f.Input.AimX), Math.Abs((int)f.Input.AimY));
            int prevMag = Math.Max(Math.Abs((int)f.PrevInput.AimX), Math.Abs((int)f.PrevInput.AimY));
            if (mag >= InputFrame.FlickThreshold && prevMag < InputFrame.FlickThreshold)
            {
                f.LastEvadeFlick = Frame;
                bool horizontal = Math.Abs((int)f.Input.AimX) >= Math.Abs((int)f.Input.AimY);
                f.EvadeDirX = (sbyte)(horizontal ? Math.Sign(f.Input.AimX) : 0);
                f.EvadeDirZ = (sbyte)(horizontal ? 0 : Math.Sign(f.Input.AimY));
            }
        }

        bool Buffered(Fighter f, Buttons b) => Frame - f.LastPress[InputFrame.IndexOf(b)] <= SimConstants.InputBufferFrames;

        void Consume(Fighter f, Buttons b) => f.LastPress[InputFrame.IndexOf(b)] = Fighter.NeverPressed;

        bool ConsumeIfBuffered(Fighter f, Buttons b)
        {
            if (!Buffered(f, b)) return false;
            Consume(f, b);
            return true;
        }

        bool EvadeFlicked(Fighter f)
        {
            if (Frame - f.LastEvadeFlick > SimConstants.InputBufferFrames) return false;
            f.LastEvadeFlick = Fighter.NeverPressed;
            return true;
        }

        static StickDir StickDirOf(Fighter f)
        {
            int mx = f.Input.MoveX, mz = f.Input.MoveZ;
            if (Math.Abs(mz) > Math.Abs(mx) && Math.Abs(mz) >= InputFrame.StickCommandThreshold)
                return mz > 0 ? StickDir.Up : StickDir.Down;
            if (Math.Abs(mx) >= InputFrame.StickCommandThreshold)
                return (mx > 0) == (f.Facing > 0) ? StickDir.Forward : StickDir.Back;
            return StickDir.Neutral;
        }

        void Emit(SimEventType type, int a, int b, int value, FixVec3 pos)
        {
            Events.Add(new SimEvent { Type = type, A = a, B = b, Value = value, Pos = pos });
        }

        void UpdatePlayers()
        {
            for (int i = 0; i < Players.Length; i++)
            {
                var p = Players[i];
                if (p == null) continue;
                if (p.AssistCooldown > 0) p.AssistCooldown--;
                p.StyleIdleFrames++;
                if (p.StyleIdleFrames > 90 && p.StylePoints > 0) p.StylePoints = Math.Max(0, p.StylePoints - 2);
            }
        }

        // ------------------------------------------------------------------ snapshots

        public byte[] SaveState()
        {
            using (var ms = new MemoryStream(4096))
            using (var w = new BinaryWriter(ms))
            {
                w.Write(Frame); w.Write(Rng.State); w.Write(nextEntityId); w.Write(AssembleMeter);
                w.Write(Fighters.Count);
                for (int i = 0; i < Fighters.Count; i++) Fighters[i].Write(w);
                w.Write(Projectiles.Count);
                for (int i = 0; i < Projectiles.Count; i++) Projectiles[i].Write(w);
                w.Write(Debris.Count);
                for (int i = 0; i < Debris.Count; i++) Debris[i].Write(w);
                for (int i = 0; i < Players.Length; i++)
                {
                    w.Write(Players[i] != null);
                    if (Players[i] != null) Players[i].Write(w);
                }
                for (int i = 0; i < Destructibles.Length; i++) Destructibles[i].Write(w);
                w.Flush();
                return ms.ToArray();
            }
        }

        public void LoadState(byte[] state)
        {
            using (var ms = new MemoryStream(state))
            using (var r = new BinaryReader(ms))
            {
                Frame = r.ReadInt32(); Rng.State = r.ReadUInt32(); nextEntityId = r.ReadInt32(); AssembleMeter = r.ReadInt32();
                Fighters.Clear();
                int n = r.ReadInt32();
                for (int i = 0; i < n; i++) Fighters.Add(Fighter.Read(r));
                Projectiles.Clear();
                n = r.ReadInt32();
                for (int i = 0; i < n; i++) Projectiles.Add(Projectile.Read(r));
                Debris.Clear();
                n = r.ReadInt32();
                for (int i = 0; i < n; i++) Debris.Add(Sim.Debris.Read(r));
                for (int i = 0; i < Players.Length; i++)
                    Players[i] = r.ReadBoolean() ? PlayerState.Read(r) : null;
                for (int i = 0; i < Destructibles.Length; i++) Destructibles[i].Read(r);
            }
            Events.Clear();
        }

        /// <summary>FNV-1a over the snapshot. Peers exchange this to detect desyncs.</summary>
        public ulong Checksum()
        {
            var bytes = SaveState();
            ulong hash = 14695981039346656037UL;
            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= 1099511628211UL;
            }
            return hash;
        }
    }
}
