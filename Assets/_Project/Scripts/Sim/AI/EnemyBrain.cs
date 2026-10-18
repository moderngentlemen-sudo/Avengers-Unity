namespace Reforged.Sim
{
    /// <summary>
    /// Enemy decision-making. Brains emit InputFrames exactly like players, so enemies use the same
    /// move system. Randomness comes only from the world RNG to stay deterministic.
    /// </summary>
    static class EnemyBrain
    {
        public static InputFrame Think(SimWorld world, Fighter f)
        {
            var hero = world.Content.Heroes[f.HeroIndex];
            if (hero.Brain == BrainType.None || hero.Brain == BrainType.Player) return default(InputFrame);
            if (f.AiCooldown > 0) f.AiCooldown--;
            if (f.IsStunned || f.Mode == FighterMode.Attack || !f.IsAlive) return default(InputFrame);

            Fighter target = null;
            Fix bestDist = Fix.Zero;
            for (int i = 0; i < world.Fighters.Count; i++)
            {
                var o = world.Fighters[i];
                if (o.Team == f.Team || !o.IsPlayer || !o.IsAlive) continue;
                var dist = Fix.Abs(o.Pos.X - f.Pos.X) + Fix.Abs(o.Pos.Z - f.Pos.Z);
                if (target == null || dist < bestDist) { target = o; bestDist = dist; }
            }
            if (target == null) return default(InputFrame);

            var dx = target.Pos.X - f.Pos.X;
            var dz = target.Pos.Z - f.Pos.Z;
            var range = hero.Brain == BrainType.Brute ? Fix.Cents(160) : Fix.Cents(110);
            bool aligned = Fix.Abs(dz) <= Fix.Cents(25);
            bool inRange = Fix.Abs(dx) <= range;

            var input = default(InputFrame);
            if (aligned && inRange)
            {
                if (f.AiCooldown == 0)
                {
                    f.Facing = (sbyte)(dx.Raw >= 0 ? 1 : -1);
                    input.Held = hero.Brain == BrainType.Brute ? Buttons.Heavy : Buttons.Light;
                    f.AiCooldown = world.Rng.Range(45, 100);
                }
                return input;
            }
            if (Fix.Abs(dx) > range - Fix.Cents(20)) input.MoveX = (sbyte)(dx.Raw > 0 ? 90 : -90);
            if (!aligned) input.MoveZ = (sbyte)(dz.Raw > 0 ? 90 : -90);
            return input;
        }
    }
}
