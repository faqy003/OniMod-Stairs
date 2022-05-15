﻿using HarmonyLib;
using KMod;
using STRINGS;
using System;
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
			//else if (!MyGrid.IsScaffolding(cell)) return;

			int layer = isStairs ? (int)ObjectLayer.Building: (int)ObjectLayer.AttachableBuilding;
			GameObject gameObject = Grid.Objects[cell, layer];
			if (gameObject == null) return;

			if (!isStairs && gameObject.GetComponent<Scaffolding>() == null) return;
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
		//public static bool IsBlocked(int cell)
		//{
		//	if ((Masks[cell] & Flags.Blocked) == 0) return false;
		//	return true;
		//}
		public enum Flags : byte
		{
			HasStair = 1,
			RightSet = 2,
			Walkable = 4,
			//Blocked = 8,
			HasScaffolding = 16,
		}
		public static Flags[] Masks;
	}
	public class Patches : KMod.UserMod2
	{
		public static readonly Tag tag_Stairs = TagManager.Create("Stairs");
		public static readonly Tag tag_Scaffolding = TagManager.Create("Scaffolding");
		public static bool ChainedDeconstruction = false;
		public static string sPath;
		public static void LoadStrings(string file,bool isTemplate=false)
		{
			if (!File.Exists(file)) return;
			var strings = Localization.LoadStringsFile(file, isTemplate);
			foreach (var s in strings)
			{
				Strings.Add(s.Key, s.Value);
			}
			Debug.Log("[MOD][Stairs] Locfile loaded : "+file);
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
						Debug.Log("[MOD][Stairs] ChainedDeconstruction Enable");
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
		[HarmonyPatch(typeof(ScoutRoverConfig))]
		[HarmonyPatch("OnSpawn")]
		public static class ScoutRoverConfig_Patch
		{
			public static void Postfix(GameObject inst)
			{
				Navigator navigator = inst.GetComponent<Navigator>();
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
			public static bool IsScaffolding(GameObject go)
            {
				if (go == null) return false;
				if (go.HasTag(tag_Scaffolding)) return false;
				return true;
            }
			public static void Postfix(BuildingDef __instance, ref bool __result, GameObject source_go, int cell, Orientation orientation, ObjectLayer layer, ObjectLayer tile_layer, bool replace_tile, ref string fail_reason)
			{
				if (!__result) return;
				if (layer == ObjectLayer.Gantry || __instance.BuildLocationRule == BuildLocationRule.Tile || __instance.BuildLocationRule == BuildLocationRule.HighWattBridgeTile)
				{
					for (int i = 0; i < __instance.PlacementOffsets.Length; i++)
					{
						CellOffset offset = __instance.PlacementOffsets[i];
						CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(offset, orientation);
						int offset_cell = Grid.OffsetCell(cell, rotatedCellOffset);
						GameObject go = Grid.Objects[offset_cell, (int)ObjectLayer.AttachableBuilding];
						if (IsScaffolding(go))
						{
							__result = false;
							break;
						}
					}
				}
				else
				{
					if (!IsScaffolding(source_go)) return;
					if (Grid.Objects[cell, (int)ObjectLayer.Gantry] != null) __result = false;
					//else if(Grid.Objects[cell, (int)ObjectLayer.Building] != null)
					//{
					//	GameObject go = Grid.Objects[cell, (int)ObjectLayer.Building];
					//	var makeBaseSolid = go.GetComponent<MakeBaseSolid>();
					//	if (makeBaseSolid != null) __result = false;
					//}
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
				if (!Grid.IsWorldValidCell(cell))return;
				if (!is_dupe) return;
                if (MyGrid.IsScaffolding(cell))
                {
					__result = true;
					return;
                }
				if (Grid.IsWorldValidCell(anchor_cell) && MyGrid.IsWalkable(anchor_cell))
                {
					__result = true;
					return;
				}
			}
		}

		// 寻路限制
		public static bool PathFilter(ref bool __result, ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, Navigator navigator)
		{
			if (path.navType != NavType.Floor || from_nav_type != NavType.Floor) return true;

			int cell = path.cell;
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

  //      // 扫扫寻路
		//[HarmonyPatch(typeof(SweepStates))]
		//[HarmonyPatch("GetNextCell")]
		//public class SweepStates_Patch
		//{
		//	public static void Postfix(SweepStates.Instance smi,ref int __result)
  //          {
		//		if (__result != Grid.InvalidCell) return;
		//		int cell = Grid.PosToCell(smi);
		//		if (!Grid.Solid[Grid.CellBelow(cell)] && !MyGrid.IsScaffolding(cell)) return;
		//		if (Grid.Solid[cell]) return;
		//		int distance = 0;
		//		int next = Grid.InvalidCell;
		//		while (distance < 1)
		//		{
		//			next = (smi.sm.headingRight.Get(smi) ? Grid.CellRight(cell) : Grid.CellLeft(cell));
		//			if (!Grid.IsValidCell(next) && !Grid.Solid[next] && Grid.IsValidCell(Grid.CellBelow(next)) && (Grid.Solid[Grid.CellBelow(next)] || MyGrid.IsScaffolding(next)))
		//			{
		//				break;
		//			}
		//			cell = next;
		//			distance++;
		//		}
		//		if (cell == Grid.PosToCell(smi))
		//		{
		//			return;
		//		}
		//		else
		//		{
		//			__result = cell;
		//		}
		//	}
  //      }

        // 添加陨石伤害
        [HarmonyPatch(typeof(Comet))]
		[HarmonyPatch("DamageThings")]
		public static class Comet_Patch
		{
			public static void Prefix(Vector3 pos, int cell, int damage)
			{
				if (!Grid.IsValidCell(cell))
					return;
				DoDamage2Scaffolding(cell, Mathf.RoundToInt(damage), BUILDINGS.DAMAGESOURCES.COMET, UI.GAMEOBJECTEFFECTS.DAMAGE_POPS.COMET);
			}
		}
		public static void DoDamage2Scaffolding(int cell,int damage,string source,string popString)
        {
			GameObject gameObject = Grid.Objects[cell, (int)ObjectLayer.AttachableBuilding];
			if (gameObject != null)
			{
				if (!gameObject.HasTag(tag_Scaffolding)) return;
				BuildingHP component = gameObject.GetComponent<BuildingHP>();
				if (component != null)
				{
					if (damage < 0) damage = component.MaxHitPoints;
					component.gameObject.Trigger((int)GameHashes.DoBuildingDamage, new BuildingHP.DamageSourceInfo
					{
						damage = damage,
						source = source,
						popString = popString
					});
				}
			}
		}

		//添加火箭伤害
		[HarmonyPatch(typeof(LaunchableRocket.States))]
		[HarmonyPatch("DoWorldDamage")]
		public static class LaunchableRocket_Patch
        {
			public static void Postfix(GameObject part, Vector3 apparentPosition)
			{
				OccupyArea area = part.GetComponent<OccupyArea>();
				foreach (CellOffset cellOffset in area.OccupiedCellsOffsets)
                {
					int cell = Grid.OffsetCell(Grid.PosToCell(apparentPosition), cellOffset);
					if (!Grid.IsValidCell(cell)) continue;
					DoDamage2Scaffolding(cell, -1, BUILDINGS.DAMAGESOURCES.ROCKET, UI.GAMEOBJECTEFFECTS.DAMAGE_POPS.ROCKET);
				}
			}
		}
		[HarmonyPatch(typeof(LaunchableRocketCluster.States))]
		[HarmonyPatch("DoWorldDamage")]
		public class LaunchableRocketCluster_Patch
		{
			public static void Postfix(GameObject part, Vector3 apparentPosition, int actualWorld)
			{
				OccupyArea area = part.GetComponent<OccupyArea>();
				foreach (CellOffset cellOffset in area.OccupiedCellsOffsets)
				{
					int cell = Grid.OffsetCell(Grid.PosToCell(apparentPosition), cellOffset);
					if (!Grid.IsValidCell(cell) || Grid.WorldIdx[cell] != Grid.WorldIdx[actualWorld]) continue;
					DoDamage2Scaffolding(cell, -1, BUILDINGS.DAMAGESOURCES.ROCKET, UI.GAMEOBJECTEFFECTS.DAMAGE_POPS.ROCKET);
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

				GameObject gameObject = Grid.Objects[cell, (int)ObjectLayer.AttachableBuilding];
				if (gameObject != null && gameObject.GetComponent<Scaffolding>() != null)
				{
					gameObject.Trigger((int)GameHashes.MarkForDeconstruct, null);
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
