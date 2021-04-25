#region Copyright & License Information
/*
 * Copyright 2007-2020 The OpenRA Developers (see AUTHORS)
 * This file is part of OpenRA, which is free software. It is made
 * available to you under the terms of the GNU General Public License
 * as published by the Free Software Foundation, either version 3 of
 * the License, or (at your option) any later version. For more
 * information, see COPYING.
 */
#endregion

using System.Linq;
using OpenRA.GameRules;
using OpenRA.Mods.Common.Warheads;
using OpenRA.Mods.Common.Traits;
using OpenRA.Traits;

namespace OpenRA.Mods.Wgra.Warheads
{
	public class GrantExternalConditionWGWarhead : Warhead
	{
		[FieldLoader.Require]
		[Desc("The conditions to apply. Must be included in the target actor's ExternalConditions list.")]
		public readonly string[] Conditions = new string[0];

		[Desc("Duration of the condition (in ticks). Set to 0 for a permanent condition.")]
		public readonly int Duration = 0;

		public readonly WDist Range = WDist.FromCells(1);

		[Desc("Percentual chance the condition is granted.")]
		public readonly int Chance = 100;

		public override void DoImpact(in Target target, WarheadArgs args)
		{
			var firedBy = args.SourceActor;
			var world = firedBy.World;

			if (target.Type == TargetType.Invalid)
				return;

			var actors = target.Type == TargetType.Actor ? new[] { target.Actor } :
				firedBy.World.FindActorsInCircle(target.CenterPosition, Range);

			foreach (var a in actors)
			{
				if (Chance < world.SharedRandom.Next(100) || !IsValidAgainst(a, firedBy))
					continue;

				var condition = Conditions.Random(firedBy.World.SharedRandom);

				a.TraitsImplementing<ExternalCondition>()
					.FirstOrDefault(t => t.Info.Condition == condition && t.CanGrantCondition(a, firedBy))
					?.GrantCondition(a, firedBy, Duration);
			}
		}
	}
}
