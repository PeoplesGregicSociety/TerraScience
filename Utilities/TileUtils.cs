﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ObjectData;
using Terraria.UI;
using TerraScience.Content.Items.Placeable.Machines;
using TerraScience.Content.TileEntities;
using TerraScience.Content.TileEntities.Energy;
using TerraScience.Content.Tiles.Multitiles;
using TerraScience.Content.UI;
using TerraScience.Systems.Energy;

namespace TerraScience.Utilities{
	public static class TileUtils{
		public static ushort SupportType => (ushort)ModContent.TileType<MachineSupport>();
		public static ushort BlastFurnaceType => (ushort)ModContent.TileType<BlastBrickTile>();

		public static Dictionary<int, MachineEntity> tileToEntity;

		public static Dictionary<int, string> tileToStructureName;

		public static void Register<TTile, TEntity>() where TTile : Machine where TEntity : MachineEntity{
			var tType = ModContent.TileType<TTile>();
			var eInst = ModContent.GetInstance<TEntity>();

			tileToEntity.Add(tType, eInst);
			tileToStructureName.Add(tType, typeof(TTile).Name);

			TerraScience.Instance.Logger.Debug($"Registered tile type \"{typeof(TTile).FullName}\" (ID: {tType}) with entity \"{typeof(TEntity).FullName}\"");
		}

		public static Vector2 TileEntityCenter(TileEntity entity, int tileType) {
			Machine tile = ModContent.GetModTile(tileType) as Machine;
			tile.GetDefaultParams(out _, out uint width, out uint height, out _);

			Point16 topLeft = entity.Position;
			Point16 size = new Point16((int)width, (int)height);
			Vector2 worldTopLeft = topLeft.ToVector2() * 16;
			return worldTopLeft + size.ToVector2() * 8; // * 16 / 2
		}

		public static Point16 Frame(this Tile tile)
			=> new Point16(tile.frameX, tile.frameY);

		public static Point16 TileCoord(this Tile tile)
			=> new Point16(tile.frameX / 18, tile.frameY / 18);

		public static Texture2D GetEffectTexture(this ModTile multitile, string effect){
			try{
				return ModContent.GetTexture($"TerraScience/Content/Tiles/Multitiles/Overlays/Effect_{multitile.Name}_{effect}");
			}catch{
				throw new ContentLoadException($"Could not find overlay texture \"{effect}\" for machine \"{multitile.Name}\"");
			}
		}

		public static void KillMachine(int i, int j, int type){
			Tile tile = Main.tile[i, j];
			Machine mTile = ModContent.GetModTile(type) as Machine;
			mTile.GetDefaultParams(out _, out _, out _, out int itemType);

			int itemIndex = Item.NewItem(i * 16, j * 16, 16, 16, itemType);
			MachineItem item = Main.item[itemIndex].modItem as MachineItem;

		//	Main.NewText($"Spawned item \"{item.Name}\" at tile position ({i}, {j})");

			Point16 tePos = new Point16(i, j) - tile.TileCoord();
			if(TileEntity.ByPosition.ContainsKey(tePos)){
				MachineEntity tileEntity = TileEntity.ByPosition[tePos] as MachineEntity;
				//Drop any items the entity contains
				if(tileEntity.SlotsCount > 0){
					for(int slot = 0; slot < tileEntity.SlotsCount; slot++){
						Item drop = tileEntity.RetrieveItem(slot);

						//Drop the item and copy over any important data
						if(drop.type > ItemID.None && drop.stack > 0){
							int dropIndex = Item.NewItem(i * 16, j * 16, 16, 16, drop.type, drop.stack);
							if(drop.modItem != null)
								Main.item[dropIndex].modItem.Load(drop.modItem.Save());
						}

						tileEntity.ClearItem(slot);
					}
				}

				item.entityData = tileEntity.Save();

				//Remove this machine from the wire networks if it's a powered machine
				if(tileEntity is PoweredMachineEntity pme){
					NetworkCollection.RemoveMachine(pme);

				//	Main.NewText($"Removed machine \"{tileEntity.MachineName}\" at position ({i}, {j}) from all networks");
				}

				tileEntity.Kill(i, j);

				if(Main.netMode == NetmodeID.MultiplayerClient)
					NetMessage.SendData(MessageID.TileEntitySharing, remoteClient: -1, ignoreClient: Main.myPlayer, number: tileEntity.ID);
			}
		}

		public static void MultitileDefaults(ModTile tile, string mapName, int type, uint width, uint height){
			Main.tileNoAttach[type] = true;
			Main.tileFrameImportant[type] = true;

			TileObjectData.newTile.CoordinateHeights = MiscUtils.Create1DArray(16, height);
			TileObjectData.newTile.CoordinateWidth = 16;
			TileObjectData.newTile.Height = (int)height;
			TileObjectData.newTile.Width = (int)width;
			TileObjectData.newTile.CoordinatePadding = 2;
			TileObjectData.newTile.LavaDeath = false;
			TileObjectData.newTile.WaterDeath = false;
			TileObjectData.newTile.LavaPlacement = LiquidPlacement.NotAllowed;
			TileObjectData.newTile.WaterPlacement = LiquidPlacement.NotAllowed;
			TileObjectData.newTile.Origin = new Point16((int)width / 2, (int)height - 1);

			TileObjectData.addTile(type);

			ModTranslation name = tile.CreateMapEntryName();
			name.SetDefault(mapName);
			tile.AddMapEntry(new Color(0xd1, 0x89, 0x32), name);

			tile.mineResist = 3f;
			//Metal sound
			tile.soundType = SoundID.Tink;
			tile.soundStyle = 1;
		}

		public static bool HandleMouse<TEntity>(Machine machine, Point16 tilePos, Func<bool> additionalCondition) where TEntity : MachineEntity{
			if(MiscUtils.TryGetTileEntity(tilePos, out TEntity entity) && additionalCondition()){
				TerraScience instance = TerraScience.Instance;
				string name = tileToStructureName[instance.TileType(machine.Name)];

				UserInterface ui = instance.machineLoader.GetInterface(name);

				//Force the current one to close if another one of the same type is going to be opened
				if(ui.CurrentState is MachineUI mui && mui.UIEntity.Position != tilePos)
					instance.machineLoader.HideUI(mui.MachineName);

				if(ui.CurrentState == null)
					instance.machineLoader.ShowUI(name, entity);
				else
					instance.machineLoader.HideUI(name);

				return true;
			}

			return false;
		}
	}

	public enum TileSlopeVariant{
		Solid,
		DownLeft,
		DownRight,
		UpLeft,
		UpRight,
		HalfBrick
	}
}
