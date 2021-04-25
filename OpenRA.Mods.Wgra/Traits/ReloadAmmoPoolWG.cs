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
using OpenRA.Mods.Common;
using OpenRA.Mods.Common.Traits;
using OpenRA.Primitives;
using OpenRA.Traits;

namespace OpenRA.Mods.Wgra.Traits
{
	[Desc("Reloads an ammo pool.")]
	public class ReloadAmmoPoolWGInfo : PausableConditionalTraitInfo
	{
		[Desc("Reload ammo pool with this name.")]
		public readonly string AmmoPool = "primary";

		[Desc("Reload time in ticks per Count.")]
		public readonly int Delay = 50;

		[Desc("How much ammo is reloaded after Delay.")]
		public readonly int Count = 1;

		public override object Create(ActorInitializer init) { return new ReloadAmmoPoolWG(this); }

		public override void RulesetLoaded(Ruleset rules, ActorInfo ai)
		{
			if (ai.TraitInfos<AmmoPoolInfo>().Count(ap => ap.Name == AmmoPool) != 1)
				throw new YamlException("ReloadsAmmoPool.AmmoPool requires exactly one AmmoPool with matching Name!");

			base.RulesetLoaded(rules, ai);
		}
	}

	public class ReloadAmmoPoolWG : PausableConditionalTrait<ReloadAmmoPoolWGInfo>, ITick, ISync
	{
		AmmoPool ammoPool;
		IReloadAmmoModifier[] modifiers;

		[Sync]
		int remainingTicks;

		public ReloadAmmoPoolWG(ReloadAmmoPoolWGInfo info)
			: base(info) { }

		protected override void Created(Actor self)
		{
			ammoPool = self.TraitsImplementing<AmmoPool>().Single(ap => ap.Info.Name == Info.AmmoPool);
			modifiers = self.TraitsImplementing<IReloadAmmoModifier>().ToArray();
			base.Created(self);

			self.World.AddFrameEndTask(w =>
			{
				remainingTicks = Util.ApplyPercentageModifiers(Info.Delay, modifiers.Select(m => m.GetReloadAmmoModifier()));
			});
		}

		void ITick.Tick(Actor self)
		{
			if (IsTraitPaused || IsTraitDisabled)
				return;

			Reload(self, Info.Delay, Info.Count);
		}

		protected virtual void Reload(Actor self, int reloadDelay, int reloadCount)
		{
			if ((!ammoPool.HasFullAmmo || reloadCount < 0) && --remainingTicks == 0)
			{
				remainingTicks = Util.ApplyPercentageModifiers(reloadDelay, modifiers.Select(m => m.GetReloadAmmoModifier()));

				if (reloadCount < 0)
 					ammoPool.TakeAmmo(self, -reloadCount);
 				else
 					ammoPool.GiveAmmo(self, reloadCount);
			}
		}
	}
}
