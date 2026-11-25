using HarmonyLib;
using KMod;
using STRINGS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;
using static AmbienceManager;
using static Grid;
using static Grid.Restriction;

namespace Stairs
{
    public class MyGrid
    {
        public static void ForceDeconstruction(int cell, bool isStairs = true)
        {
            if (!Grid.IsValidCell(cell)) return;
            if (isStairs) { if (!MyGrid.IsStair(cell)) return; }
            //else if (!MyGrid.IsScaffolding(cell)) return;

            int layer = isStairs ? (int)ObjectLayer.Building : (int)ObjectLayer.AttachableBuilding;
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
        public static bool IsHypotenuse(int cell)
        {
            if ((Masks[cell] & Flags.Hypotenuse) == 0) return false;
            return true;
        }

        public enum Flags : byte
        {
            HasStair = 1,
            RightSet = 2,
            Walkable = 4,
            Hypotenuse = 8,
            HasScaffolding = 16,
        }
        public static Flags[] Masks;
    }
    public class Patches : KMod.UserMod2
    {
        public static readonly Tag tag_Stairs = TagManager.Create("Stairs");
        public static readonly Tag tag_Scaffolding = TagManager.Create("Scaffolding");
        public static readonly Tag tag_ScaffoldingAlt2 = TagManager.Create(ScaffoldingAlt2Config.ID);
        public static bool ChainedDeconstruction = false;
        public static string sPath;
        public static void LoadStrings(string file, bool isTemplate = false)
        {
            if (!File.Exists(file)) return;
            var strings = Localization.LoadStringsFile(file, isTemplate);
            foreach (var s in strings)
            {
                Strings.Add(s.Key, s.Value);
            }
            Debug.Log("[MOD][Stairs] Locfile loaded : " + file);
        }

        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            sPath = path;
            LoadStrings(Path.Combine(path, "loc/stairs_template.pot"), true);

            MinionConfig_Patch.ApplyPatchas(harmony);
        }

        private static void AddBuildingToTechnology(Db db, string tech, string buildingId)
        {
            Tech t = db.Techs.TryGet(tech);
            if (t == null) return;
            t.unlockedItemIDs.Add(buildingId);
        }

        private static MyTransitionLayer GetTransitionLayer(Navigator navigator)
        {
            if (navigator == null || navigator.transitionDriver == null) return null;
            List<TransitionDriver.OverrideLayer> layers = navigator.transitionDriver.overrideLayers;
            for (int i = 0; i < layers.Count; i++)
            {
                MyTransitionLayer layer = layers[i] as MyTransitionLayer;
                if (layer != null) return layer;
            }
            return null;
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
                    if (!mod.IsActive()) continue;
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
                ModUtil.AddBuildingToPlanScreen("Base", StairsConfig.ID, "ladders", FirePoleConfig.ID);
                ModUtil.AddBuildingToPlanScreen("Base", StairsClassicConfig.ID, "ladders", StairsConfig.ID); 
                ModUtil.AddBuildingToPlanScreen("Base", ScaffoldingAlt1Config.ID, "ladders", StairsClassicConfig.ID);
                ModUtil.AddBuildingToPlanScreen("Base", ScaffoldingConfig.ID, "ladders", ScaffoldingAlt1Config.ID);
                ModUtil.AddBuildingToPlanScreen("Base", ScaffoldingAlt2Config.ID, "ladders", ScaffoldingConfig.ID);
                ModUtil.AddBuildingToPlanScreen("Base", StairsAlt1Config.ID, "ladders", ScaffoldingConfig.ID);
            }
        }

        [HarmonyPatch(typeof(Db))]
        [HarmonyPatch("Initialize")]
        public static class Db_Initialize_Patch
        {
            public static void Prefix()
            {
                if (Localization.GetLocale() != null)
                    LoadStrings(Path.Combine(sPath, "loc", Localization.GetLocale().Code + ".po"));
            }
            public static void Postfix(ref Db __instance)
            {
                AddBuildingToTechnology(__instance, "Luxury", StairsAlt1Config.ID);
                AddBuildingToTechnology(__instance, "RefinedObjects", ScaffoldingConfig.ID);
                AddBuildingToTechnology(__instance, "InteriorDecor", ScaffoldingAlt1Config.ID);
                AddBuildingToTechnology(__instance, "InteriorDecor", StairsClassicConfig.ID);
                AddBuildingToTechnology(__instance, "Smelting", ScaffoldingAlt2Config.ID);
            }
        }

        // ------------- 补丁 -----------------
        //添加OverrideLayer
        public static class MinionConfig_Patch
        {
            public static void Postfix(GameObject go)
            {
                Navigator navigator = go.GetComponent<Navigator>();
                navigator.transitionDriver.overrideLayers.Add(new MyTransitionLayer(navigator));
            }
            public static void ApplyPatchas(Harmony harmony)
            {
                harmony.Patch(typeof(MinionConfig).GetMethod("OnSpawn"), new HarmonyMethod(typeof(MinionConfig_Patch).GetMethod("Postfix")));
                harmony.Patch(typeof(BionicMinionConfig).GetMethod("OnSpawn"), new HarmonyMethod(typeof(MinionConfig_Patch).GetMethod("Postfix")));
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
                if (___navigator == null) return true;
                int cell = Grid.PosToCell(___navigator);
                if (!Grid.IsValidCell(cell)) return true;
                if (!MyGrid.IsHypotenuse(cell)) return true;
                MyTransitionLayer layer = GetTransitionLayer(___navigator);
                if (layer == null || !layer.isMovingOnStaris) return true;
                return false;
            }
        }

        [HarmonyPatch(typeof(AsyncPathProber.Manager))]
        [HarmonyPatch("makeWorkOrder")]
        public class PathProbeWorkOrder_Patch
        {
            public static void Postfix(Navigator nav, ref AsyncPathProber.WorkOrder __result)
            {
                MyTransitionLayer layer = GetTransitionLayer(nav);
                if (layer == null || !layer.isMovingOnStaris) return;
                if (!Grid.IsValidCell(layer.transitionOriginCell)) return;
                if (MyGrid.Masks == null || MyGrid.IsHypotenuse(layer.transitionOriginCell)) return;

                __result.originCell = layer.transitionOriginCell;
            }
        }

        [HarmonyPatch(typeof(Navigator))]
        [HarmonyPatch("Stop", new Type[] { typeof(bool), typeof(bool) })]
        public static class Navigator_Stop_StairTransition_Patch
        {
            public static bool Prefix(Navigator __instance, bool arrived_at_destination, bool play_idle, ref bool __result)
            {
                MyTransitionLayer layer = GetTransitionLayer(__instance);
                if (layer == null || !layer.DeferStop(__instance, arrived_at_destination, play_idle)) return true;
                __result = true;
                return false;
            }
        }

        [HarmonyPatch(typeof(Navigator))]
        [HarmonyPatch("SimEveryTick")]
        public static class Navigator_SimEveryTick_StairTransition_Patch
        {
            public static void Postfix(Navigator __instance)
            {
                MyTransitionLayer layer = GetTransitionLayer(__instance);
                if (layer == null || !layer.TryTakePendingStop(out bool arrived, out bool playIdle)) return;

                __instance.Stop(arrived, playIdle);
            }
        }

        [HarmonyPatch(typeof(Navigator))]
        [HarmonyPatch("OnDefeated")]
        public static class Navigator_OnDefeated_StairTransition_Patch
        {
            public static void Prefix(Navigator __instance)
            {
                SetForcedStop(__instance, true);
            }

            public static void Postfix(Navigator __instance)
            {
                SetForcedStop(__instance, false);
            }
        }

        private static void SetForcedStop(Navigator navigator, bool value)
        {
            MyTransitionLayer layer = GetTransitionLayer(navigator);
            if (layer != null) layer.allowForcedStop = value;
        }

        // 建筑摆放判定
        [HarmonyPatch(typeof(BuildingDef))]
        [HarmonyPatch("IsAreaClear", new Type[] { typeof(GameObject), typeof(int), typeof(Orientation), typeof(ObjectLayer), typeof(ObjectLayer), typeof(bool), typeof(bool), typeof(string) ,typeof(bool)},
            new ArgumentType[] { ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Normal, ArgumentType.Out ,ArgumentType.Normal})]
        public static class BuildingDef_IsAreaClear_Patch
        {
            private static bool IsScaffolding(GameObject go)
            {
                if (go == null) return false;
                if (!go.HasTag(tag_Scaffolding)) return false;
                return true;
            }
            private static bool IsSolid(int cell)
            {
                GameObject go = Grid.Objects[cell, (int)ObjectLayer.Building];
                if (go == null) return false;

                Building building = go.GetComponent<Building>();
                if (building == null)
                {
                    building = go.GetComponent<BuildingUnderConstruction>();
                    if(building == null) return false;
                }
                GameObject go_c = building.Def.BuildingComplete;
                if(go_c.GetComponent<Door>() != null) return true;
                MakeBaseSolid.Def def = go_c.GetDef<MakeBaseSolid.Def>();
                if (def == null) return false;
                int cell_go = Grid.PosToCell(go);
                for (int j = 0; j < def.solidOffsets.Length; j++)
				{
					CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(def.solidOffsets[j], building.Orientation);
                    if (Grid.OffsetCell(cell_go,rotatedCellOffset) == cell) return true;
				}
                return false;
            }
            private static bool CheckAll(int cell, CellOffset[] offsets,Orientation orientation)
            {
                for (int i = 0; i < offsets.Length; i++)
                {
                    CellOffset offset = offsets[i];
                    CellOffset rotatedCellOffset = Rotatable.GetRotatedCellOffset(offset, orientation);
                    int offset_cell = Grid.OffsetCell(cell, rotatedCellOffset);
                    GameObject go = Grid.Objects[offset_cell, (int)ObjectLayer.AttachableBuilding];
                    if (IsScaffolding(go)) return true;
                }
                return false;
            }
            public static void Postfix(BuildingDef __instance, ref bool __result, GameObject source_go, int cell, Orientation orientation, ObjectLayer layer, ObjectLayer tile_layer, bool replace_tile, bool restrictToActiveWorld, ref string fail_reason, bool permitUproots)
            {
                if (!__result) return;
                //GameObject go_c = source_go.GetComponent<BuildingPreview>().Def.BuildingComplete;
                if (layer == ObjectLayer.Gantry || __instance.BuildLocationRule == BuildLocationRule.Tile || __instance.BuildLocationRule == BuildLocationRule.HighWattBridgeTile)
                {
                    if(CheckAll(cell,__instance.PlacementOffsets,orientation)) __result = false;
                }
                //else if (go_c.GetDef<MakeBaseSolid.Def>() != null)
                //{
                //    MakeBaseSolid.Def def = go_c.GetDef<MakeBaseSolid.Def>();
                //    if (CheckAll(cell, def.solidOffsets, orientation)) __result = false;
                //}
                else
                {
                    if (!IsScaffolding(source_go)) return;
                    if (!Grid.IsWorldValidCell(cell)) return;
                    if (Grid.Objects[cell, (int)ObjectLayer.Gantry] != null) __result = false;
                    else if (Grid.Objects[cell, (int)ObjectLayer.Building] != null)
                    {
                        GameObject go = Grid.Objects[cell, (int)ObjectLayer.Building];

                        Building building = go.GetComponent<Building>();
                        if (building != null)
                        {
                            if(building.Def.BuildingComplete.GetComponent<Door>() != null) __result = false;
                        }
                    }
                }
                if (!__result) fail_reason = UI.TOOLTIPS.HELP_BUILDLOCATION_OCCUPIED;
            }

            // netstandard2.1 版：等价的 List 手写扫描实现，避免 CodeMatcher 构造函数依赖 ILGenerator。
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var instrList = new List<CodeInstruction>(instructions);
                int idx = -1;
                for (int i = 0; i < instrList.Count - 3; i++)
                {
                    if (instrList[i].opcode == OpCodes.Ldsflda &&
                        instrList[i + 1].opcode == OpCodes.Ldloc_3 &&
                        instrList[i + 2].opcode == OpCodes.Ldc_I4_S && (sbyte)instrList[i + 2].operand == (sbyte)ObjectLayer.AttachableBuilding &&
                        instrList[i + 3].opcode == OpCodes.Call)
                    {
                        idx = i + 1;
                        break;
                    }
                }
                if (idx == -1)
                {
                    Debug.Log("no match @BuildingDef_IsAreaClear_Patch");
                    return instructions;
                }
                // 删除3条指令
                instrList.RemoveRange(idx, 3);
                // 插入 pop, ldloc_3, call ICall
                instrList.Insert(idx, new CodeInstruction(OpCodes.Pop));
                instrList.Insert(idx + 1, new CodeInstruction(OpCodes.Ldloc_3));
                instrList.Insert(idx + 2, new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(BuildingDef_IsAreaClear_Patch), nameof(ICall))));
                return instrList;
            }
            public static GameObject ICall(int cell)
            {
                var go = Grid.Objects[cell, (int)ObjectLayer.AttachableBuilding];
                if (IsScaffolding(go)) return null;
                return go;
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
                if (!is_dupe) return;
                if (!Grid.IsWorldValidCell(cell)) return;
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

        //
        //[HarmonyPatch(typeof(NavTable))]
        //[HarmonyPatch("SetValid")]
        //public static class NavTable_SetValid_Patch
        //{
        //    public static void Prefix(int cell, NavType nav_type, bool is_valid)
        //    {
        //        if (is_valid) return;
        //        if (nav_type != NavType.Floor) return;
        //    }
        //}

        //[HarmonyPatch(typeof(PathFinder.PotentialList))]
        //[HarmonyPatch("Add")]
        //public static class PathFinder_Patch
        //{
        //    public static void Prefix(ref int cost, PathFinder.PotentialPath path)
        //    {
        //        if (MyGrid.IsWalkable(path.cell))
        //        {
        //            cost -= 1;
        //        }
        //    }
        //}

        // 寻路限制
        public static bool PathFilter(ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, Navigator navigator)
        {
            if (!(from_nav_type == NavType.Floor || (path.navType == NavType.Floor && from_nav_type == NavType.Swim)) ||
                !(path.navType == NavType.Floor || (path.navType == NavType.Swim && from_nav_type == NavType.Floor))) return true;

            int targetCell = path.cell;
            CellOffset offset = Grid.GetOffset(from_cell, targetCell);

            //快速过滤
            if(offset.y == 0) return true;
            int c_b = Grid.CellBelow(targetCell);
            int f_b = Grid.CellBelow(from_cell);

            if (offset.y == 1)
            {
                if(from_nav_type == NavType.Swim) return true;
                if (MyGrid.IsHypotenuse(c_b))
                {
                    if (offset.x == 1)
                    {
                        if (!MyGrid.IsRightSet(c_b)) return true;
                    }
                    else if (offset.x == -1)
                    {
                        if (MyGrid.IsRightSet(c_b)) return true;
                    }
                }
            }
            else if (offset.y == -1) {
                if (path.navType == NavType.Swim) return true;
                if (MyGrid.IsHypotenuse(f_b))
                {
                    //if (!MyGrid.IsHypotenuse(targetCell) && MyGrid.IsWalkable(targetCell)) return false;
                    if (offset.x == 1)
                    {
                        if (MyGrid.IsRightSet(f_b)) return true;
                    }
                    else if (offset.x == -1)
                    {
                        if (!MyGrid.IsRightSet(f_b)) return true;
                    }
                }
            }

            //完整过滤
            if(offset.y > 0)
            {
                int offsetCell = Grid.OffsetCell(from_cell, 0, 1);
                if (MyGrid.IsScaffolding(offsetCell)) return false;
                if (offset.x != 0)
                {
                    if (MyGrid.IsWalkable(from_cell)) return false; 
                    if (offset.x > 0)
                    {
                        if (MyGrid.IsHypotenuse(offsetCell) && MyGrid.IsRightSet(offsetCell)) return false;
                        if (offset.x > 1)
                        {
                            int cellRight = Grid.CellRight(offsetCell);
                            if (MyGrid.IsHypotenuse(cellRight) && MyGrid.IsRightSet(cellRight)) return false;
                        }
                    }else if (offset.x < 0)
                    {
                        if (MyGrid.IsHypotenuse(offsetCell) && !MyGrid.IsRightSet(offsetCell)) return false;
                        if (offset.x < -1)
                        {
                            int cellLeft = Grid.CellLeft(offsetCell);
                            if (MyGrid.IsHypotenuse(cellLeft) && !MyGrid.IsRightSet(cellLeft)) return false;
                        }
                    }
                }
                else {
                    if (MyGrid.IsHypotenuse(from_cell)) return false;
                }
                if (offset.y > 1)
                {
                    if (MyGrid.IsWalkable(offsetCell)) return false;
                    offsetCell = Grid.OffsetCell(from_cell, 0, 2);
                    if (MyGrid.IsScaffolding(offsetCell)) return false;
                    if (MyGrid.IsHypotenuse(c_b) && MyGrid.IsHypotenuse(offsetCell) && (MyGrid.IsRightSet(c_b) == MyGrid.IsRightSet(offsetCell))) return false;
                }
            }
            else
            {
                if (offset.x > 0)
                {
                    if (MyGrid.IsWalkable(targetCell)) return false;
                    int offsetCell = Grid.CellRight(from_cell);
                    if (MyGrid.IsScaffolding(offsetCell)) return false;
                    if (MyGrid.IsHypotenuse(f_b) && MyGrid.IsHypotenuse(offsetCell) && !MyGrid.IsRightSet(offsetCell)) return false;
                    if(offset.x > 1)
                    {
                        offsetCell = Grid.CellRight(offsetCell);
                        if (MyGrid.IsScaffolding(offsetCell)) return false;
                        if (MyGrid.IsWalkable(offsetCell) && !MyGrid.IsRightSet(offsetCell)) return false;
                    }
                    else if (offset.y < -1)
                    {
                        offsetCell = Grid.CellDownRight(from_cell);
                        if (MyGrid.IsScaffolding(offsetCell)) return false;
                        if (MyGrid.IsWalkable(offsetCell)) return false;
                        offsetCell = Grid.CellBelow(offsetCell);
                        if (MyGrid.IsWalkable(offsetCell)) return false;
                    }
                }
                else if(offset.x < 0)
                {
                    if (MyGrid.IsWalkable(targetCell)) return false;
                    int offsetCell = Grid.CellLeft(from_cell);
                    if (MyGrid.IsScaffolding(offsetCell)) return false;
                    if (MyGrid.IsHypotenuse(f_b) && MyGrid.IsHypotenuse(offsetCell) && MyGrid.IsRightSet(offsetCell)) return false;
                    if (offset.x < -1)
                    {
                        offsetCell = Grid.CellLeft(offsetCell);
                        if (MyGrid.IsScaffolding(offsetCell)) return false;
                        if (MyGrid.IsWalkable(offsetCell) && MyGrid.IsRightSet(offsetCell)) return false;
                    }
                    else if (offset.y < -1)
                    {
                        offsetCell = Grid.CellDownLeft(from_cell);
                        if (MyGrid.IsScaffolding(offsetCell)) return false;
                        if (MyGrid.IsWalkable(offsetCell)) return false;
                        offsetCell = Grid.CellBelow(offsetCell);
                        if (MyGrid.IsWalkable(offsetCell)) return false;
                    }
                }
                else
                {
                    if (MyGrid.IsScaffolding(from_cell)) return false;
                    int offsetCell = Grid.CellBelow(from_cell);
                    if (MyGrid.IsHypotenuse(offsetCell)) return false;
                    if (offset.y < -1)
                    {
                        if (MyGrid.IsScaffolding(offsetCell)) return false;
                        offsetCell = Grid.OffsetCell(from_cell, 0, -2);
                        if (MyGrid.IsWalkable(offsetCell)) return false;
                    }
                }
            }

            return true;
        }

        [HarmonyPatch(typeof(RobotPathFinderAbilities))]
        [HarmonyPatch("TraversePath")]
        public static class RobotPathFinderAbilities_Patch
        {
            public static bool Prefix(Navigator ___navigator, ref bool __result, ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, int cost, int transition_id, bool submerged)
            {
                if (___navigator.NavGridName != "RobotNavGrid") return true;
                __result = false;
                return PathFilter( ref path, from_cell, from_nav_type, ___navigator);
            }
        }

        [HarmonyPatch(typeof(MinionPathFinderAbilities))]
        [HarmonyPatch("TraversePath")]
        public static class TraversePath_Patch
        {
            public static bool Prefix(Navigator ___navigator, ref bool __result, ref PathFinder.PotentialPath path, int from_cell, NavType from_nav_type, int cost, int transition_id, bool submerged)
            {
                __result = false;
                return PathFilter( ref path, from_cell, from_nav_type, ___navigator);
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
        public static void DoDamage2Scaffolding(int cell, int damage, string source, string popString)
        {
            GameObject gameObject = Grid.Objects[cell, (int)ObjectLayer.AttachableBuilding];
            if (gameObject != null)
            {
                if (!gameObject.HasTag(tag_Scaffolding)) return;
                BuildingHP component = gameObject.GetComponent<BuildingHP>();
                if (component != null)
                {
                    if (damage < 0) damage = component.MaxHitPoints;
                    component.gameObject.BoxingTrigger((int)GameHashes.DoBuildingDamage, new BuildingHP.DamageSourceInfo
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
