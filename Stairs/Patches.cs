using HarmonyLib;
using KMod;
using STRINGS;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Stairs
{
	public class MyGrid
	{
		public static void ForceDeconstruction(int cell, bool isStairs = true)
		{
			if (!Grid.IsValidCell(cell)) return;
			if (isStairs) { if (!MyGrid.IsStair(cell)) return; }
			else if (!MyGrid.IsScaffolding(cell)) return;

			int layer = (int)ObjectLayer.Building;
			if (!isStairs) layer = (int)ObjectLayer.AttachableBuilding;
			GameObject gameObject = Grid.Objects[cell, layer];
			if (gameObject == null) return;

			//if (!isStairs && gameObject.GetComponent<Scaffolding>() == null) return;
			Deconstructable deconstructable = gameObject.GetComponent<Deconstructable>();
			if (deconstructable == null) return;
			if (!deconstructable.IsMarkedForDeconstruction()) return;
			deconstructable.CompleteWork(null);
		}
		public static bool IsStair(int cell)
		{
			if ((Masks[cell] & Flags.HasStair) == 0) return false;
			return true;
		}
		public static bool IsScaffolding(int cell)
		{
			if ((Masks[cell] & Flags.HasScaffolding) == 0) return false;
			return true;
		}
		public static bool IsWalkable(int cell)
		{
			if ((Masks[cell] & Flags.Walkable) == 0) return false;
			return true;
		}
		public static bool IsRightSet(int cell)
		{
			if ((Masks[cell] & Flags.RightSet) == 0) return false;
			return true;
		}
		public static bool IsBlocked(int cell)
		{
			if ((Masks[cell] & Flags.Blocked) == 0) return false;
			return true;
		}
		public enum Flags : byte
		{
			HasStair = 1,
			RightSet = 2,
			Walkable = 4,
			Blocked = 8,
			HasScaffolding = 16,
		}
		public static Flags[] Masks;
	}
	public class ScaffoldingValidator : NavTableValidator
	{
		public ScaffoldingValidator()
		{
			World instance = World.Instance;
			instance.OnSolidChanged = (Action<int>)Delegate.Combine(instance.OnSolidChanged, new Action<int>(this.OnSolidChanged));
		}
		private void OnSolidChanged(int cell)
		{
			if (this.onDirty != null)
			{
				this.onDirty.Invoke(cell);
			}
		}
		public override void Clear()
		{
			World instance = World.Instance;
			instance.OnSolidChanged = (Action<int>)Delegate.Remove(instance.OnSolidChanged, new Action<int>(this.OnSolidChanged));
		}
		public override void UpdateCell(int cell, NavTable nav_table, CellOffset[] bounding_offsets)
		{
			bool flag = ScaffoldingValidator.IsWalkableCell(cell);
			if(flag && base.IsClear(cell, bounding_offsets, false))
				nav_table.SetValid(cell, NavType.Floor, true);
		}

		private static bool IsWalkableCell(int cell)
		{
			if (Grid.IsValidCell(cell))
			{
				if (!NavTableValidator.IsCellPassable(cell, false))
				{
					return false;
				}
				else if (MyGrid.IsScaffolding(cell))
				{
					return true;
				}
			}
			return false;
		}
	}
	public class Patches : KMod.UserMod2
	{
		//public static string Name = "Stairs";
		//public static string Version = "1.23";

		public static readonly Tag tag_Stairs = TagManager.Create("Stairs");
		public static bool ChainedDeconstruction = false;

		//public override void OnLoad(Harmony harmony)
		//{
		//}
#if VANILLA
		private static void AddBuildingToTechnology(string tech, string buildingId)
		{
			var techList = new List<string>(Database.Techs.TECH_GROUPING[tech]) { buildingId };
			Database.Techs.TECH_GROUPING[tech] = techList.ToArray();
		}
#elif DLC1
		private static void AddBuildingToTechnology(Db db,string tech, string buildingId)
		{
			Tech t = db.Techs.TryGet(tech);
			if (t == null) return;
			t.unlockedItemIDs.Add(buildingId);
		}
#endif
		public static void AddBuildingToPlanScreen(HashedString category, string buildingId, string addAfterBuildingId = null)
		{
			var index = TUNING.BUILDINGS.PLANORDER.FindIndex(x => x.category == category);

			if (index == -1)
				return;

			if (!(TUNING.BUILDINGS.PLANORDER[index].data is IList<string> planOrderList))
			{
				Debug.Log($"Could not add {buildingId} to the building menu.");
				return;
			}

			var neighborIdx = planOrderList.IndexOf(addAfterBuildingId);

			if (neighborIdx != -1)
				planOrderList.Insert(neighborIdx + 1, buildingId);
			else
				planOrderList.Add(buildingId);
		}

		// ------------- 初始化 -----------------
		[HarmonyPatch(typeof(GridSettings))]
		[HarmonyPatch("Reset")]
		public static class GridSettings_Reset_Patch
		{
			public static void Postfix()
			{
				MyGrid.Masks = new MyGrid.Flags[Grid.CellCount];
				// 查看ChainedDeconstruction是否已启用
				ChainedDeconstruction = false;
				foreach (Mod mod in Global.Instance.modManager.mods)
				{
					if (!mod.enabled) continue;
					if (mod.title == "ChainedDeconstruction")
					{
						ChainedDeconstruction = true;
						Debug.Log("[MOD] Stairs : ChainedDeconstruction Enable");
						break;
					}
				}
			}
		}

		[HarmonyPatch(typeof(GridSettings))]
		[HarmonyPatch("ClearGrid")]
		public static class GridSettings_ClearGrid_Patch
		{
			public static void Postfix()
			{
				MyGrid.Masks = null;
			}
		}

		[HarmonyPatch(typeof(GeneratedBuildings))]
		[HarmonyPatch(nameof(GeneratedBuildings.LoadGeneratedBuildings))]
		public static class GeneratedBuildings_LoadGeneratedBuildings_Patch
		{
			public static void Postfix()
			{
				AddBuildingToPlanScreen("Base", Stairs.StairsAlt1Config.ID, FirePoleConfig.ID);
				AddBuildingToPlanScreen("Base", Stairs.StairsConfig.ID, FirePoleConfig.ID);
				AddBuildingToPlanScreen("Base", Stairs.ScaffoldingConfig.ID, FirePoleConfig.ID);
			}
		}

		[HarmonyPatch(typeof(Db))]
		[HarmonyPatch("Initialize")]
		public static class Db_Initialize_Patch
		{
			public static void Prefix()
			{
				Localization.Locale locale = Localization.GetLocale();
				if (locale != null && locale.Code == "zh")
				{
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS.NAME", $"台阶");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS.DESC", $"（在发明了火箭之后我们甚至发明出了台阶）");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS.EFFECT", $"使复制人可以步行上下，节省力气。\n\n下台阶的速度会很快，向上则会稍慢。\n如果没有建造在可行走的方格上可能会无法正常工作。");
					Strings.Add($"STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.NAME", $"禁止通过");
					Strings.Add($"STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.NAME_OFF", $"允许通过");
					Strings.Add($"STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.TOOLTIP", $"禁止复制人从此步行通过。");
					Strings.Add($"STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.TOOLTIP_OFF", $"允许复制人从此步行通过。");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS_ALT1.NAME", $"豪华台阶");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS_ALT1.DESC", $"");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS_ALT1.EFFECT", $"除了用料比较豪华外，使用上与普通台阶没有什么不同。");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.URFSCAFFOLDING.NAME", $"脚手架");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.URFSCAFFOLDING.DESC", $"（由于不太结实，不建议在上面攀爬）");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.URFSCAFFOLDING.EFFECT", $"一种可以快速建造的薄板，可供复制人在上面行走并能与其他建筑叠加。");
				}
				else
				{
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS.NAME", $"Stairs");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS.DESC", $"(After we made Space Rocket we even invent Stairs)");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS.EFFECT", $"Saving your energy by step up and down.\n\nWalking down strair will be easy and fast ,and tiny slower when walking up.\n" +
						$"It may not working correctly when built on an unwalkble floor.");
					Strings.Add($"STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.NAME", $"Block Path");
					Strings.Add($"STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.NAME_OFF", $"Unblock Path");
					Strings.Add($"STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.TOOLTIP", $"Prevent Duplicants walk through here.");
					Strings.Add($"STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.TOOLTIP_OFF", $"Allow Duplicants walk through here.");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS_ALT1.NAME", $"Deluxe Stairs");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS_ALT1.DESC", $"");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.STAIRS_ALT1.EFFECT", $"In addition to the better material, there is no different from normal stairs.");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.URFSCAFFOLDING.NAME", $"Scaffolding");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.URFSCAFFOLDING.DESC", $"(It's not very stable, any climbing is not recommended.)");
					Strings.Add($"STRINGS.BUILDINGS.PREFABS.URFSCAFFOLDING.EFFECT", $"Thin plate that fast to build, allow Duplicants walk on it and can stack with other building.");
				}
#if VANILLA
				AddBuildingToTechnology("Luxury", Stairs.StairsAlt1Config.ID);
				AddBuildingToTechnology("RefinedObjects", Stairs.ScaffoldingConfig.ID);
#endif
			}
#if DLC1
			public static void Postfix(ref Db __instance)
			{
				AddBuildingToTechnology(__instance,"Luxury",Stairs.StairsAlt1Config.ID);
				AddBuildingToTechnology(__instance,"RefinedObjects",Stairs.ScaffoldingConfig.ID);
			}
#endif
		}

		// ------------- 补丁 -----------------
		// 动画与移动速度
		[HarmonyPatch(typeof(TransitionDriver))]
		[HarmonyPatch("BeginTransition")]
		public static class Navigator_BeginTransition_Patch
		{
			public static void Prefix(Navigator navigator, ref Navigator.ActiveTransition transition, ref bool __state)
			{
				__state = false;
				if (transition.navGridTransition.isCritter) return;
				if (transition.y != 1 && transition.y != -1) return;
				if (transition.x != 1 && transition.x != -1) return;
				if (transition.start != NavType.Floor || transition.end != NavType.Floor) return;
				int num = Grid.PosToCell(navigator);
				//if (MyGrid.IsStair(num)) return;
				if (transition.y > 0)
				{
					num = Grid.OffsetCell(num, transition.x, 0);
					if (transition.x > 0) {
						if (MyGrid.IsRightSet(num)) return;
					}
					else
					{
						if (!MyGrid.IsRightSet(num)) return;
					}
				}
				else
				{
					num = Grid.OffsetCell(num, 0, transition.y);
					if (transition.x > 0)
					{
						if (!MyGrid.IsRightSet(num)) return;
					}
					else
					{
						if (MyGrid.IsRightSet(num)) return;
					}
				}
				if (!MyGrid.IsWalkable(num)) return;

				transition.isLooping = true;
				transition.anim = "floor_floor_1_0_loop";
				__state = true;
			}
			public static void Postfix(Navigator navigator, ref Navigator.ActiveTransition transition, ref bool __state, ref TransitionDriver __instance)
			{
				if (!__state) return;
				if (transition.y > 0)
				{
					transition.speed *= 0.9f;
					transition.animSpeed *= 0.9f;
				}
				else
				{
					transition.speed *= 1.5f;
					transition.animSpeed *= 1.5f;
				}
			}
		}

		// 建筑摆放判定
		[HarmonyPatch(typeof(BuildingDef))]
		[HarmonyPatch("IsAreaClear")]
		public static class BuildingDef_IsAreaClear_Patch
		{
			public static void Prefix(BuildingDef __instance, ref bool __state, GameObject source_go, int cell, Orientation orientation,ref ObjectLayer layer, ObjectLayer tile_layer, bool replace_tile, ref string fail_reason)
			{
				__state = __instance.name == "urfScaffolding";
				//if (__state)
				//{
				//	layer = ObjectLayer.AttachableBuilding;
				//}
			}
			public static void Postfix(BuildingDef __instance, ref bool __state, ref bool __result, GameObject source_go, int cell, Orientation orientation, ObjectLayer layer, ObjectLayer tile_layer, bool replace_tile, ref string fail_reason)
			{
				if ((layer == ObjectLayer.Gantry || __instance.BuildLocationRule == BuildLocationRule.Tile || __instance.BuildLocationRule == BuildLocationRule.HighWattBridgeTile) 
					&& __result)
				{
					for (int i = 0; i < __instance.PlacementOffsets.Length; i++)
					{
						CellOffset offset = __instance.PlacementOffsets[i];
						CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(offset, orientation);
						int num = Grid.OffsetCell(cell, rotatedCellOffset);
						if (MyGrid.IsScaffolding(num))
						{
							__result = false;
							break;
						}
					}
				}
				else
				{
					if (!__state) return;
					if (__result)
					{
						if (Grid.Objects[cell, (int)ObjectLayer.Gantry] != null) __result = false;
					}
				}
				if(!__result) fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
			}
		}

		// 允许行走的方格
		[HarmonyPatch(typeof(GameNavGrids.FloorValidator))]
		[HarmonyPatch("IsWalkableCell")]
		public static class FloorValidator_IsWalkableCell_Patch
		{
			public static void Postfix(ref bool __result, int cell, int anchor_cell, bool is_dupe)
			{
				if (__result) return;
				if (!Grid.IsValidCell(cell))
				{
					return;
				}
				if (!is_dupe) return; 
				if (!Grid.IsValidCell(anchor_cell))
				{
					return;
				}
				if (!MyGrid.IsStair(anchor_cell) && !MyGrid.IsScaffolding(cell)) return;
				__result = true;
			}
		}

		// 寻路限制
		public static bool PathFilter(ref bool __result, ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, int cost, int transition_id, int underwater_cost)
		{
			int cell = path.cell;
			if (MyGrid.IsBlocked(cell))
			{
				__result = false;
				return false;
			}
			if (path.navType != NavType.Floor || from_nav_type != NavType.Floor) return true;

			int f_b = Grid.CellBelow(from_cell);
			bool cellNotStair = !MyGrid.IsStair(cell);
			bool fromStair = MyGrid.IsStair(from_cell);
			bool fromNotScaff = !MyGrid.IsScaffolding(from_cell);
			//if (!MyGrid.IsStair(f_b) && !fromStair && cellNotStair && !MyGrid.IsScaffolding(cell) && fromNotScaff) return true;

			CellOffset offset = Grid.GetOffset(from_cell, cell);

			bool goUpAndStr = offset.y >= 0;
			if (goUpAndStr)
			{
				bool goStraight = offset.y == 0;
				if (fromStair && goStraight)
					return true;
				if (!fromNotScaff && goStraight)
					return true;
				bool flag = true;

				int f_a = Grid.CellAbove(from_cell);
				if (offset.y > 1)
				{
					int f_a_a = Grid.CellAbove(f_a);
					if (MyGrid.IsScaffolding(f_a) || MyGrid.IsScaffolding(f_a_a)) flag = false;
				}
				else if (offset.y == 1)
				{
					if (MyGrid.IsScaffolding(cell))
					{
						if (MyGrid.IsWalkable(f_b))
						{
							int c_b = Grid.CellBelow(cell);
							if (!MyGrid.IsWalkable(c_b)) flag = false;
						}
						else if (MyGrid.IsScaffolding(f_a))
							flag = false;
					}
					else if (MyGrid.IsScaffolding(f_a))
						flag = false;
				}

				if (MyGrid.IsRightSet(f_b))
				{
					if (!fromStair)
					{
						if (goUpAndStr && flag) return true;
						else if (offset.x > 0 && flag) return true;
					}
				}
				else
				{
					if (!fromStair)
					{
						if (goUpAndStr && flag) return true;
						else if (offset.x < 0 && flag) return true;
					}
				}
			}
			else if (cellNotStair)
			{
				if (offset.x > 0)
				{
					int f_r = Grid.CellRight(from_cell);
					if (offset.y == -1)
					{
						if (MyGrid.IsRightSet(f_b) && MyGrid.IsStair(f_b))
						{
							if (!fromStair) return true;
						}
						else if (!MyGrid.IsScaffolding(f_r)) return true;
					}
					else
					{
						int f_b_r = Grid.CellRight(f_b);
						if (!MyGrid.IsStair(f_b_r) && !MyGrid.IsScaffolding(f_r) && !MyGrid.IsScaffolding(f_b_r)) return true;
					}
				}
				else if (offset.x < 0)
				{
					int f_l = Grid.CellLeft(from_cell);
					if (offset.y == -1)
					{
						if (!MyGrid.IsRightSet(f_b) && MyGrid.IsStair(f_b))
						{
							if (!fromStair) return true;
						}
						else if (!MyGrid.IsScaffolding(f_l)) return true;
					}
					else
					{
						int f_b_l = Grid.CellLeft(f_b);
						if (!MyGrid.IsStair(f_b_l) && !MyGrid.IsScaffolding(f_l) && !MyGrid.IsScaffolding(f_b_l)) return true;
					}
				}
				else
				{
					if (fromNotScaff) return true;
				}

			}

			__result = false;
			return false;
		}

		[HarmonyPatch(typeof(CreaturePathFinderAbilities))]
		[HarmonyPatch("TraversePath")]
		public static class CreatureTraversePath_Patch
		{
			public static bool Prefix(Navigator ___navigator, ref bool __result, ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, int cost, int transition_id, int underwater_cost)
			{
				if (___navigator.NavGridName != "RobotNavGrid") return true;
				return PathFilter(ref __result, ref path, from_cell, from_nav_type, cost, transition_id, underwater_cost);
			}
		}

		[HarmonyPatch(typeof(MinionPathFinderAbilities))]
		[HarmonyPatch("TraversePath")]
		public static class TraversePath_Patch
		{
			public static bool Prefix(ref bool __result, ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, int cost, int transition_id, int underwater_cost)
			{
				return PathFilter(ref __result, ref path, from_cell, from_nav_type, cost, transition_id, underwater_cost);
			}
		}

		//// 扫扫寻路
		//[HarmonyPatch(typeof(NavGrid))]
		//[HarmonyPatch(MethodType.Constructor)]
		//[HarmonyPatch(new Type[] { typeof(string), typeof(NavGrid.Transition[]), typeof(NavGrid.NavTypeData[]), typeof(CellOffset[]), typeof(NavTableValidator[]), typeof(int), typeof(int), typeof(int) })]
		//public static class NavGrid_Patch
		//{
		//	public static void Prefix(string id,ref NavTableValidator[] validators)
		//	{
		//		if (id != "WalkerBabyNavGrid") return;
		//		validators = validators.Append(new ScaffoldingValidator());
		//	}
		//}

		// 添加陨石伤害
		[HarmonyPatch(typeof(Comet))]
		[HarmonyPatch("DamageThings")]
		public static class Comet_Patch
		{
			public static void Prefix(Comet __instance,Vector3 pos, int cell, int damage)
			{
				if (!Grid.IsValidCell(cell))
					return;
				if (!MyGrid.IsScaffolding(cell)) 
					return;

				GameObject gameObject = Grid.Objects[cell, (int)ObjectLayer.AttachableBuilding];
				if (gameObject != null)
				{
					BuildingHP component = gameObject.GetComponent<BuildingHP>();
					if (component != null )
					{
						float f = gameObject.GetComponent<KPrefabID>().HasTag(GameTags.Bunker) ? ((float)damage * __instance.bunkerDamageMultiplier) : ((float)damage);
						component.gameObject.Trigger((int)GameHashes.DoBuildingDamage, new BuildingHP.DamageSourceInfo
						{
							damage = Mathf.RoundToInt(f),
							source = BUILDINGS.DAMAGESOURCES.COMET,
							popString = UI.GAMEOBJECTEFFECTS.DAMAGE_POPS.COMET
						});
					}
				}
			}
		}

		// 批量拆除
		[HarmonyPatch(typeof(DeconstructTool))]
		[HarmonyPatch("DeconstructCell")]
		public static class DeconstructTool_Patch
		{
			public static void Postfix(DeconstructTool __instance, int cell)
			{
				if (!((FilteredDragTool)__instance).IsActiveLayer(ToolParameterMenu.FILTERLAYERS.BACKWALL)) return;
				if (!MyGrid.IsScaffolding(cell)) return;

				GameObject gameObject = Grid.Objects[cell, (int)ObjectLayer.AttachableBuilding];
				if (gameObject != null)
				{
					gameObject.Trigger(-790448070, null);
					Prioritizable component = gameObject.GetComponent<Prioritizable>();
					if (component != null)
					{
						component.SetMasterPriority(ToolMenu.Instance.PriorityScreen.GetLastSelectedPriority());
					}
				}
			}
		}
	}
}
