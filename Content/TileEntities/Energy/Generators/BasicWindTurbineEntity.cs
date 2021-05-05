﻿using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.GameContent.Events;
using Terraria.ModLoader;
using TerraScience.Content.Tiles.Multitiles.EnergyMachines.Basic;
using TerraScience.Systems.Energy;

namespace TerraScience.Content.TileEntities.Energy.Generators{
	public class BasicWindTurbineEntity : GeneratorEntity{
		public override int MachineTile => ModContent.TileType<BasicWindTurbine>();

		public override TerraFlux FluxCap => new TerraFlux(6000);

		//max export is rougly 10TF/t
		public override TerraFlux ExportRate => new TerraFlux(10);

		public float bladeRotation;

		public override void ReactionComplete(){
			TerraFlux flux = GetPowerGeneration(ticks: 1);

			ImportFlux(ref flux);
		}

		public override bool UpdateReaction() => true;

		public override void PreUpdateReaction(){
			//Always adding energy
			ReactionProgress = 100;
			ReactionInProgress = true;

			bladeRotation += MathHelper.ToRadians(Main.windSpeed * 6f);
		}

		public override TerraFlux GetPowerGeneration(int ticks){
			/*  Notes about wind speed:
			 *  
			 *  Main.windSpeed       | The current wind speed.  Always tries to increment/decrement towards Main.windSpeedSet by a
			 *                         factor of `0.001f * Main.dayRate` per tick
			 *  
			 *  Main.windSpeedSet    | The target wind speed.  Set to Main.windSpeedTemp when Main.weatherCounter is <= 0.  Value is
			 *                         set to `genRand.Next(-100, 101) * 0.01f` on world load/generation.
			 *  
			 *  Main.windSpeedSpeed  | The rate at which Main.windSpeedTemp is changed.  Starts at 0, then is incremented by
			 *                         `rand.Next(-10, 11) * 0.0001f` every tick.  Value is clamped to +/- 0.002f
			 *  
			 *  Main.windSpeedTemp   | The next value that Main.windSpeedSet will be set to.  Modified by Main.windSpeedSpeed.
			 *                         If it's currently raining, then this variable is modified by Main.windSpeedSpeed * 2 instead.
			 *                         Value is clamped to +/- `(0.3f + 0.5f * Main.cloudAlpha)`
			 *  
			 *  Main.weatherCounter  | The timer used for modifying Main.windSpeedSet and also sending net messages for syncing
			 *                         world data.  It is decremented by Main.dayRate every tick.  Value is initialized to
			 *                         `rand.Next(3600, 18000)` -- when Main.windSpeedSet is set -- or `genRand.Next(3600, 18000)`
			 *                         -- during worldgen.
			 *  
			 *  Weathe Radio Display | Displayed wind speed is `Math.Abs(Main.windSpeed) * 100`
			 */

			TerraFlux flux = new TerraFlux(0f);

			float realWind = Math.Abs(Main.windSpeed) * 100;

			//Flux = 1TF/t multiplied by a factor of Wind Speed / 28mph
			float tfPerTick = 1f;
			flux += tfPerTick * realWind / 28f;
			flux *= 0.333f;

			//Wind turbine is in a sandstorm/blizzard?
			//Sandstorm: Multiply the generation by 1.15x and add a flat 1.75TF/t increase
			//Blizzard/Raining: Multiply the generation by 1.085x and add a flat 1TF/t increase
			if(Sandstorm.Happening)
				flux *= 1.15f;
			if(Main.raining)
				flux *= 1.085f;

			if(Sandstorm.Happening)
				flux += 1.75f;
			if(Main.raining)
				flux += 1f;

			flux *= ticks;

			return flux;
		}

		// TODO: portable battery slot???
		public override int SlotsCount => 0;
	}
}
