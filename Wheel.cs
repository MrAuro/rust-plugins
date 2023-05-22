using Oxide.Core;
using System;
using System.Collections.Generic;
using Oxide.Core.Plugins;
using Oxide.Game.Rust.Cui;
using Rust;
using System.Linq;

namespace Oxide.Plugins
{
	[Info("Wheel", "Auro", "1.0.0")]
	[Description("testing")]
	public class Wheel : RustPlugin
	{
		object OnBigWheelWin(BigWheelGame bigWheel, Item scrap, int multiplier)
		{
			return null;
		}
	}
}
