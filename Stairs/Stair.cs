using System;
using System.Collections.Generic;
using KSerialization;
using STRINGS;
using UnityEngine;

namespace Stairs
{
    [SkipSaveFileSerialization]
    public class AnimStairs : KMonoBehaviour
    {
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
        }
        protected override void OnSpawn()
        {
            OccupyArea occupy_area = GetComponent<OccupyArea>();
            if (occupy_area != null)
            {
                extents = occupy_area.GetExtents();
            }
            else
            {
                Building building = GetComponent<Building>();
                extents = building.GetExtents();
            }
            Extents watch_extents = new Extents(extents.x - 1, extents.y - 1, extents.width + 2, extents.height + 2);
            partitionerEntry = GameScenePartitioner.Instance.Add("AnimStairs.OnSpawn", gameObject, watch_extents, GameScenePartitioner.Instance.objectLayers[(int)objectLayer], new Action<object>(OnNeighbourCellsUpdated));
            //underConstruction = this.HasTag(GameTags.UnderConstruction);
            UpdateEndCaps();
        }
        protected override void OnCleanUp()
        {
            GameScenePartitioner.Instance.Free(ref partitionerEntry);
            base.OnCleanUp();
        }
        public static void PrearePreview(GameObject go)
        {
            foreach (var kbatchedAnimController in go.GetComponentsInChildren<KBatchedAnimController>())
            {
                foreach (var symbol in leftSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, true);
                }
                foreach (var symbol in rightSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, false);
                }
                foreach (var symbol in rightHalfSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, false);
                }
                foreach (var symbol in jointSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, false);
                }
                foreach (var symbol in topSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, false);
                }
                foreach (var symbol in topHalfSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, true);
                }
            }
        }
        private void UpdateEndCaps()
        {
            int cell = Grid.PosToCell(gameObject);
            bool showStairs = true;
            bool showBackground = false;
            bool is_blocked_above = true;
            bool is_connected = false;
            Rotatable rotatable = GetComponent<Rotatable>();
            bool is_right_set = rotatable.GetOrientation() == Orientation.FlipH;
            int cell_below = Grid.CellBelow(cell);
            int cell_above = Grid.CellAbove(cell);
            if (Grid.IsValidCell(cell_above))
            {
                is_blocked_above = HasTileableNeighbour(cell_above);
            }
            if (!is_blocked_above)
            {
                int cell_front = is_right_set ? Grid.CellRight(cell) : Grid.CellLeft(cell);
                if (Grid.IsValidCell(cell_front))
                {
                    showStairs = !HasTileableNeighbour(cell_front);
                }
            }
            else
            {
                showStairs = false;
            }
            if (showStairs && !is_blocked_above)
            {
                int cell_connect = is_right_set ? Grid.CellLeft(cell_above) : Grid.CellRight(cell_above);
                if (Grid.IsValidCell(cell_connect))
                {
                    GameObject gameObject = Grid.Objects[cell_connect, (int)objectLayer];
                    if (gameObject != null)
                    {
                        if (gameObject.HasTag(Patches.tag_Stairs))
                        {
                            if ((gameObject.GetComponent<Rotatable>().GetOrientation() == Orientation.FlipH) == is_right_set) is_connected = true;
                        }
                    }
                }
            }
            if (Grid.IsValidCell(cell_below))
            {
                showBackground = is_blocked_above || !showStairs || HasTileableNeighbour(cell_below, false);
                if (!showBackground && showStairs && IsSolid(cell_below))
                {
                    int cell_back = is_right_set ? Grid.CellLeft(cell) : Grid.CellRight(cell);
                    if (Grid.IsValidCell(cell_back)) showBackground = HasTileableNeighbour(cell_back, false);
                }
            }
            if (MyGrid.IsStair(cell))
            {
                if (showStairs || !is_blocked_above)
                {
                    MyGrid.Masks[cell] |= MyGrid.Flags.Walkable;
                }
                else
                {
                    MyGrid.Masks[cell] &= ~MyGrid.Flags.Walkable;
                }
            }
            foreach (var kbatchedAnimController in GetComponentsInChildren<KBatchedAnimController>())
            {
                foreach (var symbol in leftSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, showStairs);
                }
                bool is_visible = showBackground && !showStairs;
                foreach (var symbol in rightSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, is_visible);
                }
                foreach (var symbol in jointSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, is_visible);
                }
                is_visible = showBackground && showStairs;
                foreach (var symbol in rightHalfSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, is_visible);
                }
                is_visible = !is_blocked_above && !is_connected && !showStairs;
                foreach (var symbol in topSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, is_visible);
                }
                is_visible = showStairs && !is_blocked_above && !is_connected;
                foreach (var symbol in topHalfSymbols)
                {
                    kbatchedAnimController.SetSymbolVisiblity(symbol, is_visible);
                }
            }
        }
        private bool IsSolid(int cell)
        {
            GameObject gameObject = Grid.Objects[cell, (int)ObjectLayer.Building];
            if (gameObject != null)
            {
                SimCellOccupier simCell = gameObject.GetComponent<SimCellOccupier>();
                if (simCell != null) return true;
            }
            return false;
        }
        private bool HasTileableNeighbour(int neighbour_cell, bool check_tile = true)
        {
            GameObject gameObject = Grid.Objects[neighbour_cell, (int)objectLayer];
            if (gameObject != null)
            {
                if (check_tile)
                {
                    SimCellOccupier simCell = gameObject.GetComponent<SimCellOccupier>();
                    if (simCell != null) return true;
                }
                //if (gameObject.HasTag(Patches.tag_Stairs) && (gameObject.HasTag(GameTags.UnderConstruction) == underConstruction)) return true;
                if (gameObject.HasTag(Patches.tag_Stairs)) return true;
            }
            return false;
        }

        private void OnNeighbourCellsUpdated(object data)
        {
            if (this == null || gameObject == null)
                return;
            if (partitionerEntry.IsValid())
                UpdateEndCaps();
        }

        private HandleVector<int>.Handle partitionerEntry;
        private Extents extents;
        //private bool underConstruction = true;
        public ObjectLayer objectLayer = ObjectLayer.Building;

        private static readonly KAnimHashedString[] leftSymbols = new KAnimHashedString[]
        {
            new KAnimHashedString("cap_left"),
            new KAnimHashedString("cap_left_place")
        };
        private static readonly KAnimHashedString[] rightSymbols = new KAnimHashedString[]
        {
            new KAnimHashedString("cap_right"),
            new KAnimHashedString("cap_right_place")
        };
        private static readonly KAnimHashedString[] rightHalfSymbols = new KAnimHashedString[]
        {
            new KAnimHashedString("cap_right_half"),
            new KAnimHashedString("cap_right_half_place")
        };
        private static readonly KAnimHashedString[] topSymbols = new KAnimHashedString[]
        {
            new KAnimHashedString("cap_top"),
            new KAnimHashedString("cap_top_place")
        };
        private static readonly KAnimHashedString[] topHalfSymbols = new KAnimHashedString[]
        {
            new KAnimHashedString("cap_top_half"),
            new KAnimHashedString("cap_top_half_place")
        };
        private static readonly KAnimHashedString[] jointSymbols = new KAnimHashedString[]
        {
            new KAnimHashedString("joint"),
            new KAnimHashedString("joint_place")
        };
    }

    public class Stair : KMonoBehaviour, IGameObjectEffectDescriptor
    {
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            //base.GetComponent<KPrefabID>().AddTag(Patches.tag_Stairs, false);
        }

        protected override void OnSpawn()
        {
            base.OnSpawn();
            GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Normal, null);
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
            Subscribe((int)GameHashes.StatusChange, OnRefreshUserMenuDelegate);
            Rotatable rotatable = GetComponent<Rotatable>();
            int cell = Grid.PosToCell(this);
            MyGrid.Masks[cell] |= MyGrid.Flags.HasStair;
            MyGrid.Masks[cell] |= MyGrid.Flags.Walkable;
            if (rotatable.GetOrientation() == Orientation.FlipH)
            {
                MyGrid.Masks[cell] |= MyGrid.Flags.RightSet;
            }
            Pathfinding.Instance.AddDirtyNavGridCell(Grid.CellAbove(cell));
        }

        protected override void OnCleanUp()
        {
            base.OnCleanUp();
            int cell = Grid.PosToCell(this);
            bool isscaff = MyGrid.IsScaffolding(cell);
            MyGrid.Masks[cell] = 0;
            if (isscaff) MyGrid.Masks[cell] |= MyGrid.Flags.HasScaffolding;
            Pathfinding.Instance.AddDirtyNavGridCell(Grid.CellAbove(cell));
        }

        public List<Descriptor> GetDescriptors(GameObject go)
        {
            var list = new List<Descriptor>();
            Descriptor descriptor = default(Descriptor);
            descriptor.SetupDescriptor(string.Format(UI.BUILDINGEFFECTS.DUPLICANTMOVEMENTBOOST,
                GameUtil.GetFormattedPercent(MyTransitionLayer.upwardsMovementSpeedMultiplier * 100f - 100f, GameUtil.TimeSlice.None)),
                string.Format(UI.BUILDINGEFFECTS.TOOLTIPS.DUPLICANTMOVEMENTBOOST, GameUtil.GetFormattedPercent(MyTransitionLayer.upwardsMovementSpeedMultiplier * 100f - 100f, GameUtil.TimeSlice.None)),
                Descriptor.DescriptorType.Effect);
            list.Add(descriptor);
            return list;
        }

        private void OnMenuToggle()
        {
            Rotatable rotatable = GetComponent<Rotatable>();
            int cell = Grid.PosToCell(this);
            if (rotatable.GetOrientation() == Orientation.FlipH)
            {
                rotatable.SetOrientation(Orientation.Neutral);
                MyGrid.Masks[cell] &= ~MyGrid.Flags.RightSet;
            }
            else
            {
                rotatable.SetOrientation(Orientation.FlipH);
                MyGrid.Masks[cell] |= MyGrid.Flags.RightSet;
            }
            GameScenePartitioner.Instance.TriggerEvent(cell, GameScenePartitioner.Instance.objectLayers[(int)ObjectLayer.Building], null);
        }
        private void OnRefreshUserMenu(object data)
        {
            Rotatable rotatable = GetComponent<Rotatable>();
            KIconButtonMenu.ButtonInfo button = ((rotatable.GetOrientation() == Orientation.FlipH)) ?
                new KIconButtonMenu.ButtonInfo("action_direction_left", Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBUTTON.NAME"), new System.Action(OnMenuToggle), Action.NumActions, null, null, null, Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBUTTON.TOOLTIP"), true) :
                new KIconButtonMenu.ButtonInfo("action_direction_right", Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBUTTON.NAME"), new System.Action(OnMenuToggle), Action.NumActions, null, null, null, Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBUTTON.TOOLTIP"), true);
            Game.Instance.userMenu.AddButton(gameObject, button, 1f);
        }

        private static readonly EventSystem.IntraObjectHandler<Stair> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<Stair>(delegate (Stair component, object data)
        {
            component.OnRefreshUserMenu(data);
        });

        //[Serialize]
        //private bool blocked;

    }
}