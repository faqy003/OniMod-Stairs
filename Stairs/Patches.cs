﻿using HarmonyLib;
using KMod;
using STRINGS;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

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
		public static readonly Tag tag_Stairs = TagManager.Create("Stairs");
		public static bool ChainedDeconstruction = false;
		public static string sPath;
		public static void LoadStrings(string file,bool isTemplate=false)
		{
			Debug.Log(file);
			if (!File.Exists(file)) return;
            var strings = Localization.LoadStringsFile(file, isTemplate);
			foreach (var s in strings)
			{
				Strings.Add(s.Key, s.Value);
			}
        }

		public override void OnLoad(Harmony harmony)
        {
			base.OnLoad(harmony);

			sPath = path;
			LoadStrings(Path.Combine(path, "loc/stairs_template.pot"), true);
		}

        private static void AddBuildingToTechnology(Db db,string tech, string buildingId)
		{
			Tech t = db.Techs.TryGet(tech);
			if (t == null) return;
			t.unlockedItemIDs.Add(buildingId);
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
				ModUtil.AddBuildingToPlanScreen("Base", StairsConfig.ID, "ladders", "FirePole");
				ModUtil.AddBuildingToPlanScreen("Base", ScaffoldingConfig.ID, "ladders", StairsConfig.ID);
				ModUtil.AddBuildingToPlanScreen("Base", StairsAlt1Config.ID, "ladders", ScaffoldingConfig.ID);
			}
		}

		[HarmonyPatch(typeof(Db))]
		[HarmonyPatch("Initialize")]
		public static class Db_Initialize_Patch
		{
			public static void Prefix()
			{
				if (Localization.GetLocale()!=null)
					LoadStrings(Path.Combine(sPath, "loc", Localization.GetLocale().Code + ".po"));
			}
			public static void Postfix(ref Db __instance)
			{
				AddBuildingToTechnology(__instance,"Luxury",StairsAlt1Config.ID);
				AddBuildingToTechnology(__instance,"RefinedObjects",ScaffoldingConfig.ID);
			}
		}

		// ------------- 补丁 -----------------
		//添加OverrideLayer
		[HarmonyPatch(typeof(MinionConfig))]
		[HarmonyPatch("OnSpawn")]
		public static class MinionConfig_Patch
        {
			public static void Postfix(GameObject go)
            {
				Navigator navigator = go.GetComponent<Navigator>();
				navigator.transitionDriver.overrideLayers.Add(new MyTransitionLayer(navigator));
			}
		}

		//安全感知修正
		[HarmonyPatch(typeof(SafeCellSensor))]
		[HarmonyPatch("Update")]
		public class SafeCellSensor_Patch
        {
			public static bool Prefix(Navigator ___navigator)
			{
				if(___navigator == null) return true;
				if (!MyGrid.IsStair(Grid.PosToCell(___navigator))) return true;
                MyTransitionLayer layer = (MyTransitionLayer)___navigator.transitionDriver.overrideLayers.Find(x => x.GetType() == typeof(MyTransitionLayer));
                if (layer == null || !layer.isMovingOnStaris) return true;
                return false;
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
				if (!Grid.IsWorldValidCell(cell))
				{
					return;
				}
				if (!is_dupe) return; 
				if (!Grid.IsWorldValidCell(anchor_cell))
				{
					return;
				}
				if (!MyGrid.IsStair(anchor_cell) && !MyGrid.IsScaffolding(cell)) return;
				__result = true;
			}
		}

		// 寻路限制
		public static bool PathFilter(ref bool __result, ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, Navigator navigator)
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
            public static bool Prefix(Navigator ___navigator, ref bool __result, ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, int cost, int transition_id, bool submerged)
            {
                if (___navigator.NavGridName != "RobotNavGrid") return true;
                return PathFilter(ref __result, ref path, from_cell, from_nav_type,___navigator);
            }
        }

        [HarmonyPatch(typeof(MinionPathFinderAbilities))]
        [HarmonyPatch("TraversePath")]
        public static class TraversePath_Patch
        {
            public static bool Prefix(Navigator ___navigator,ref bool __result, ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, int cost, int transition_id, bool submerged)
            {
                return PathFilter(ref __result, ref path, from_cell, from_nav_type,___navigator);
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
