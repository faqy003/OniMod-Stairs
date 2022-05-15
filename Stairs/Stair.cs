using System;
using System.Collections.Generic;
using KSerialization;
using STRINGS;
using UnityEngine;

namespace Stairs
{
	[SkipSaveFileSerialization]
	public class AnimStairs : AnimTileable
	{

		protected override void OnSpawn()
		{
			OccupyArea component = base.GetComponent<OccupyArea>();
			if (component != null)
			{
				this.extents = component.GetExtents();
			}
			else
			{
				Building component2 = base.GetComponent<Building>();
				this.extents = component2.GetExtents();
			}
			Extents extents = new Extents(this.extents.x - 1, this.extents.y - 1, this.extents.width + 2, this.extents.height + 2);
			this.partitionerEntry = GameScenePartitioner.Instance.Add("AnimStairs.OnNeighbourCellsUpdated", base.gameObject, extents, GameScenePartitioner.Instance.objectLayers[(int)this.objectLayer], new Action<object>(this.OnNeighbourCellsUpdated));
			this.UpdateEndCaps();
		}


		protected override void OnCleanUp()
		{
			GameScenePartitioner.Instance.Free(ref this.partitionerEntry);
			//GameScenePartitioner.Instance.Free(ref this.partitionerEntry2);
			base.OnCleanUp();
		}

		public void UpdateEndCaps()
		{
			int cell = Grid.PosToCell(base.gameObject);
			bool is_visible = true;
			bool is_blocked_above = true;
			Grid.CellToXY(cell, out int x, out int y);
			CellOffset rotatedCellOffset = new CellOffset(this.extents.x - x - 1, 0);
			Rotatable component = base.GetComponent<Rotatable>();
			if (component)
			{
				rotatedCellOffset = component.GetRotatedCellOffset(rotatedCellOffset);
			}
			int cell_front = Grid.OffsetCell(cell, rotatedCellOffset);
			int cell_above = Grid.CellAbove(cell);
			if (Grid.IsValidCell(cell_above))
			{
				is_blocked_above = this.HasTileableNeighbour(cell_above);
			}
			if (!is_blocked_above)
			{
				if (Grid.IsValidCell(cell_front))
				{
					is_visible = !this.HasTileableNeighbour(cell_front);
				}
			}
			else
			{
				is_visible = false;
			}
			if (MyGrid.IsStair(cell))
			{
				if (is_visible || !is_blocked_above)
				{
					MyGrid.Masks[cell] |= MyGrid.Flags.Walkable;
				}
				else
				{
					MyGrid.Masks[cell] &= ~MyGrid.Flags.Walkable;
				}
			}
			foreach (var kbatchedAnimController in base.GetComponentsInChildren<KBatchedAnimController>())
			{
				foreach (var symbol in leftSymbols)
				{
					kbatchedAnimController.SetSymbolVisiblity(symbol, is_visible);
				}
				foreach (var symbol in leftSymbols2)
				{
					kbatchedAnimController.SetSymbolVisiblity(symbol, !is_visible);
				}
			}
		}

		private bool HasTileableNeighbour(int neighbour_cell)
		{
			GameObject gameObject = Grid.Objects[neighbour_cell, (int)this.objectLayer];
			if (gameObject != null)
			{
				SimCellOccupier simCell = gameObject.GetComponent<SimCellOccupier>();
				if (simCell != null) return true;
				if (gameObject.HasTag(Patches.tag_Stairs)) return true;
			}
			return false;
		}

		private void OnNeighbourCellsUpdated(object data)
		{
			if (this == null )
			{
				return;
			}
			if (this.partitionerEntry.IsValid())
			{
				this.UpdateEndCaps();
			}
		}

		private HandleVector<int>.Handle partitionerEntry;
		private Extents extents;

		private static readonly KAnimHashedString[] leftSymbols = new KAnimHashedString[]
		{
			new KAnimHashedString("cap_left"),
			new KAnimHashedString("cap_left_place")
		};

		private static readonly KAnimHashedString[] leftSymbols2 = new KAnimHashedString[]
		{
			new KAnimHashedString("rcap_left"),
			new KAnimHashedString("rcap_left_place")
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
			base.GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Normal, null);
			base.Subscribe<Stair>((int)GameHashes.RefreshUserMenu, Stair.OnRefreshUserMenuDelegate);
			base.Subscribe<Stair>((int)GameHashes.StatusChange, Stair.OnRefreshUserMenuDelegate);
			Rotatable rotatable = this.GetComponent<Rotatable>();
			int cell = Grid.PosToCell(this);
			MyGrid.Masks[cell] |= MyGrid.Flags.HasStair;
			MyGrid.Masks[cell] |= MyGrid.Flags.Walkable;
			if (rotatable.GetOrientation() == Orientation.FlipH) {
				MyGrid.Masks[cell] |= MyGrid.Flags.RightSet; }
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
			List<Descriptor> list = null;
			if (MyTransitionLayer.upwardsMovementSpeedMultiplier != 1f)
			{
				list = new List<Descriptor>();
				Descriptor descriptor = default(Descriptor);
				descriptor.SetupDescriptor(string.Format(UI.BUILDINGEFFECTS.DUPLICANTMOVEMENTBOOST, 
					GameUtil.GetFormattedPercent(MyTransitionLayer.upwardsMovementSpeedMultiplier * 100f - 100f, GameUtil.TimeSlice.None)),
					string.Format(UI.BUILDINGEFFECTS.TOOLTIPS.DUPLICANTMOVEMENTBOOST, GameUtil.GetFormattedPercent(MyTransitionLayer.upwardsMovementSpeedMultiplier * 100f - 100f, GameUtil.TimeSlice.None)), 
					Descriptor.DescriptorType.Effect);
				list.Add(descriptor);
			}
			return list;
		}

		private void OnMenuToggle()
        {
			Rotatable rotatable = this.GetComponent<Rotatable>();
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
			this.gameObject.GetComponent<AnimStairs>().UpdateEndCaps();
		}
		private void OnRefreshUserMenu(object data)
		{
			Rotatable rotatable = this.GetComponent<Rotatable>();
			KIconButtonMenu.ButtonInfo button = ((rotatable.GetOrientation() == Orientation.FlipH)) ? 
				new KIconButtonMenu.ButtonInfo("action_direction_left", Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBUTTON.NAME"), new System.Action(this.OnMenuToggle), Action.NumActions, null, null, null, Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBUTTON.TOOLTIP"), true) :
				new KIconButtonMenu.ButtonInfo("action_direction_right", Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBUTTON.NAME"), new System.Action(this.OnMenuToggle), Action.NumActions, null, null, null, Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBUTTON.TOOLTIP"), true);
			Game.Instance.userMenu.AddButton(base.gameObject, button, 1f);
		}

        private static readonly EventSystem.IntraObjectHandler<Stair> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<Stair>(delegate (Stair component, object data)
		{
			component.OnRefreshUserMenu(data);
		});

		//[Serialize]
		//private bool blocked;

	}
}