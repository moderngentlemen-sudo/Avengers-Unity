using System;
using System.Collections.Generic;
using NUnit.Framework;
using Reforged.Sim;

namespace Reforged.Tests
{
    public sealed class ContentTests
    {
        /// <summary>Mirrors SimConvert.ToFix(float) without UnityEngine: round to millimetres.</summary>
        static Fix ViaInspector(Fix v) => Fix.Ratio((int)Math.Round(v.ToFloat() * 1000f, MidpointRounding.AwayFromZero), 1000);

        static IEnumerable<Fix> Values(FixVec3 v) { yield return v.X; yield return v.Y; yield return v.Z; }

        [Test]
        public void GrayboxContentSurvivesInspectorRoundTrip()
        {
            foreach (var hero in GrayboxContent.Create().Heroes)
            {
                var values = new List<Fix> { hero.WalkSpeed, hero.DepthSpeed, hero.JumpVelocity, hero.Gravity };
                values.AddRange(Values(hero.BodyHalfExtents));
                foreach (var m in hero.Moves)
                {
                    values.AddRange(Values(m.HitboxOffset));
                    values.AddRange(Values(m.HitboxHalfExtents));
                    values.AddRange(Values(m.Knockback));
                    values.AddRange(Values(m.SelfVelocity));
                    if (m.Projectile == null) continue;
                    values.Add(m.Projectile.Speed);
                    values.Add(m.Projectile.Rise);
                    values.AddRange(Values(m.Projectile.HalfExtents));
                    values.AddRange(Values(m.Projectile.SpawnOffset));
                }
                foreach (var v in values)
                    Assert.AreEqual(v.Raw, ViaInspector(v).Raw, hero.Name + ": " + v + " does not survive float authoring");
            }
        }

        [Test]
        public void EveryPlayableHeroUsesEveryCommand()
        {
            // "No button goes unused": every playable hero needs a move on each command.
            var content = GrayboxContent.Create();
            foreach (var hero in content.Heroes)
            {
                if (hero.Brain != BrainType.Player || hero.AssistOnly) continue;
                foreach (MoveInput input in Enum.GetValues(typeof(MoveInput)))
                {
                    Assert.GreaterOrEqual(hero.FindMove(input, StickDir.Neutral, false), 0, hero.Name + " has no " + input);
                }
                Assert.AreEqual(3, hero.AssistMoveIndices.Length, hero.Name + " needs three assist callout variants");
            }
        }
    }
}
