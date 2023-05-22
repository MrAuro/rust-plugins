using Oxide.Core;
using System;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using System.Linq;

namespace Oxide.Plugins
{
	[Info("LimitTemp", "Auro", "1.0.0")]
	[Description("Limits how cold a player can be")]
	public class LimitTemp : RustPlugin
	{
		object OnRunPlayerMetabolism(PlayerMetabolism metabolism, BasePlayer player, float delta)
		{
			if (metabolism.temperature.min != -7.0f || metabolism.temperature.max != 42.0f)
			{
				metabolism.temperature.min = -7.0f;
				metabolism.temperature.max = 42.0f;
			}
			return null;
		}
	}
}
