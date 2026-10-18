using System;
using System.Collections.Generic;
using Reforged.Sim;
using UnityEngine;

namespace Reforged.Game
{
    /// <summary>
    /// Designer-editable hero data. Values are floats in the Inspector and are rounded to exact
    /// fixed-point when the simulation is built, so tuning never compromises determinism.
    /// Distances are meters; velocities are meters per tick (60 ticks per second).
    /// </summary>
    [CreateAssetMenu(menuName = "Reforged/Hero", fileName = "Hero")]
    public sealed class HeroAsset : ScriptableObject
    {
        public string heroName;
        public BrainType brain = BrainType.Player;
        public int maxHealth = 1000;
        public int armor;
        public float walkSpeed = 0.08f, depthSpeed = 0.06f, jumpVelocity = 0.28f, gravity = 0.016f;
        public int airJumps = 1;
        public Vector3 bodyHalfExtents = new Vector3(0.35f, 0.9f, 0.3f);
        [Tooltip("Assist Callout moves: LT = [0], LT+forward = [1], LT+down = [2].")]
        public string[] assistMoves = new string[0];
        public int toolCycleRange, toolChargeCost;
        [Tooltip("Heat (Iron Man), Rage (Hulk), Density (Vision), Size (Ant-Man).")]
        public HeroMechanic mechanic;
        [Tooltip("Moves fired by D-pad left/right (e.g. Hulk's claps). Leave empty if unused.")]
        public string toolLeftMove, toolRightMove;
        public List<MoveEntry> moves = new List<MoveEntry>();

        [Header("Presentation")]
        [Tooltip("FBX from the Blender pipeline (Assets/_Project/Art/Characters/<hero>/<hero>.fbx).")]
        public GameObject modelPrefab;
        [Tooltip("Correction if the model faces the wrong way after import (the pipeline exports facing +Z).")]
        public Vector3 modelRotation;
        [Tooltip("Clips named after moves and FighterModes. Use 'Collect Clips From Model' in the Inspector.")]
        public List<AnimationClip> clips = new List<AnimationClip>();

        public HeroDef ToDef()
        {
            var h = new HeroDef
            {
                Name = heroName,
                Brain = brain,
                MaxHealth = maxHealth,
                Armor = armor,
                WalkSpeed = SimConvert.ToFix(walkSpeed),
                DepthSpeed = SimConvert.ToFix(depthSpeed),
                JumpVelocity = SimConvert.ToFix(jumpVelocity),
                Gravity = SimConvert.ToFix(gravity),
                AirJumps = airJumps,
                BodyHalfExtents = SimConvert.ToFix(bodyHalfExtents),
                AssistMoves = (string[])assistMoves.Clone(),
                ToolCycleRange = toolCycleRange,
                ToolChargeCost = toolChargeCost,
                Mechanic = mechanic,
                ToolLeftMove = string.IsNullOrEmpty(toolLeftMove) ? null : toolLeftMove,
                ToolRightMove = string.IsNullOrEmpty(toolRightMove) ? null : toolRightMove,
            };
            foreach (var m in moves) h.Add(m.ToDef());
            return h;
        }

        public void CopyFrom(HeroDef h)
        {
            heroName = h.Name;
            brain = h.Brain;
            maxHealth = h.MaxHealth;
            armor = h.Armor;
            walkSpeed = h.WalkSpeed.ToFloat();
            depthSpeed = h.DepthSpeed.ToFloat();
            jumpVelocity = h.JumpVelocity.ToFloat();
            gravity = h.Gravity.ToFloat();
            airJumps = h.AirJumps;
            bodyHalfExtents = SimConvert.ToUnity(h.BodyHalfExtents);
            assistMoves = (string[])h.AssistMoves.Clone();
            toolCycleRange = h.ToolCycleRange;
            toolChargeCost = h.ToolChargeCost;
            mechanic = h.Mechanic;
            toolLeftMove = h.ToolLeftMove;
            toolRightMove = h.ToolRightMove;
            moves.Clear();
            foreach (var m in h.Moves) moves.Add(MoveEntry.From(m));
        }
    }

    [Serializable]
    public sealed class MoveEntry
    {
        public string name;
        public MoveTier tier;
        public MoveInput input;
        public StickDir direction;
        public bool aerial;
        [Header("Frames")]
        public int startup = 5;
        public int active = 3;
        public int recovery = 12;
        public int cancelStart, cancelEnd, landingLag = 8;
        [Header("Hit")]
        public int damage = 40;
        public int hitstun = 18;
        public int hitstop = 5;
        public Vector3 hitboxOffset = new Vector3(0.75f, 1.1f, 0f);
        public Vector3 hitboxHalfExtents = new Vector3(0.45f, 0.35f, 0.45f);
        public Vector3 knockback = new Vector3(0.04f, 0f, 0f);
        public Vector3 selfVelocity;
        public int juggleCost = 2, maxHits = 1, rehitInterval;
        [Header("Economy")]
        public int gaugeCost;
        public int gaugeGain = 8;
        public int destructionPower;
        public int teamBuffFrames;
        public string chainNext;
        public MoveFlags flags;
        [Header("Mechanic")]
        public int heatCost;
        public int minTier;
        public MechanicEffect effect;
        public int effectFrames;
        [Header("Projectile")]
        public bool hasProjectile;
        public float projectileSpeed = 0.4f, projectileRise;
        public int projectileLifetime = 40, projectileRicochets;
        public bool projectileReturns;
        public Vector3 projectileHalfExtents = new Vector3(0.4f, 0.4f, 0.5f);
        public Vector3 projectileSpawnOffset = new Vector3(0.8f, 1.1f, 0f);

        public MoveDef ToDef() => new MoveDef
        {
            Name = name, Tier = tier, Input = input, Direction = direction, Aerial = aerial,
            Startup = startup, Active = active, Recovery = recovery,
            CancelStart = cancelStart, CancelEnd = cancelEnd, LandingLag = landingLag,
            Damage = damage, Hitstun = hitstun, Hitstop = hitstop,
            HitboxOffset = SimConvert.ToFix(hitboxOffset), HitboxHalfExtents = SimConvert.ToFix(hitboxHalfExtents),
            Knockback = SimConvert.ToFix(knockback), SelfVelocity = SimConvert.ToFix(selfVelocity),
            JuggleCost = juggleCost, MaxHits = maxHits, RehitInterval = rehitInterval,
            GaugeCost = gaugeCost, GaugeGain = gaugeGain, DestructionPower = destructionPower, TeamBuffFrames = teamBuffFrames,
            ChainNext = string.IsNullOrEmpty(chainNext) ? null : chainNext,
            Flags = flags,
            HeatCost = heatCost, MinTier = minTier, Effect = effect, EffectFrames = effectFrames,
            Projectile = !hasProjectile ? null : new ProjectileSpec
            {
                Speed = SimConvert.ToFix(projectileSpeed), Rise = SimConvert.ToFix(projectileRise),
                Lifetime = projectileLifetime, Ricochets = projectileRicochets, ReturnsToOwner = projectileReturns,
                HalfExtents = SimConvert.ToFix(projectileHalfExtents), SpawnOffset = SimConvert.ToFix(projectileSpawnOffset),
            },
        };

        public static MoveEntry From(MoveDef m)
        {
            var e = new MoveEntry
            {
                name = m.Name, tier = m.Tier, input = m.Input, direction = m.Direction, aerial = m.Aerial,
                startup = m.Startup, active = m.Active, recovery = m.Recovery,
                cancelStart = m.CancelStart == m.ActiveStart ? 0 : m.CancelStart,
                cancelEnd = m.CancelEnd == m.TotalFrames ? 0 : m.CancelEnd,
                landingLag = m.LandingLag, damage = m.Damage, hitstun = m.Hitstun, hitstop = m.Hitstop,
                hitboxOffset = SimConvert.ToUnity(m.HitboxOffset), hitboxHalfExtents = SimConvert.ToUnity(m.HitboxHalfExtents),
                knockback = SimConvert.ToUnity(m.Knockback), selfVelocity = SimConvert.ToUnity(m.SelfVelocity),
                juggleCost = m.JuggleCost, maxHits = m.MaxHits, rehitInterval = m.RehitInterval,
                gaugeCost = m.GaugeCost, gaugeGain = m.GaugeGain, destructionPower = m.DestructionPower,
                teamBuffFrames = m.TeamBuffFrames, chainNext = m.ChainNext, flags = m.Flags,
                heatCost = m.HeatCost, minTier = m.MinTier, effect = m.Effect, effectFrames = m.EffectFrames,
                hasProjectile = m.Projectile != null,
            };
            if (m.Projectile != null)
            {
                e.projectileSpeed = m.Projectile.Speed.ToFloat();
                e.projectileRise = m.Projectile.Rise.ToFloat();
                e.projectileLifetime = m.Projectile.Lifetime;
                e.projectileRicochets = m.Projectile.Ricochets;
                e.projectileReturns = m.Projectile.ReturnsToOwner;
                e.projectileHalfExtents = SimConvert.ToUnity(m.Projectile.HalfExtents);
                e.projectileSpawnOffset = SimConvert.ToUnity(m.Projectile.SpawnOffset);
            }
            return e;
        }
    }
}
