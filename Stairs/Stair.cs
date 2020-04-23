using System;
using System.Collections.Generic;
using KSerialization;
using Stairs;
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
			//int cell = Grid.PosToCell(base.gameObject);
			//this.partitionerEntry2 = GameScenePartitioner.Instance.Add("AnimStairs.OnNavChanged", base.gameObject, cell, GameScenePartitioner.Instance.validNavCellChangedLayer, new Action<object>(this.OnNavChanged));
			this.UpdateEndCaps();
			//this.UpadateRotation();
		}


		protected override void OnCleanUp()
		{
			GameScenePartitioner.Instance.Free(ref this.partitionerEntry);
			//GameScenePartitioner.Instance.Free(ref this.partitionerEntry2);
			base.OnCleanUp();
		}
		private void OnNavChanged(object obj)
		{
			if (base.gameObject == null) return;
			if (!this.partitionerEntry2.IsValid()) return;
			this.UpadateRotation();
		}
		private void UpadateRotation()
		{
			int cell = Grid.PosToCell(base.gameObject);
			int cell_b = Grid.CellBelow(cell);
			float angle;
			if (MyGrid.IsWalkable(cell) && !GameNavGrids.FloorValidator.IsWalkableCell(cell, cell_b, true))
			{
				angle = 10f;
			}
			else
			{
				angle = 0;
			}
			if(angle != this.lastAngle)
			{
				foreach (KBatchedAnimController kbatchedAnimController in base.GetComponentsInChildren<KBatchedAnimController>())
				{
					kbatchedAnimController.Rotation = angle;
				}
			}
			this.lastAngle = angle;
		}
		private void UpdateEndCaps()
		{
			int cell = Grid.PosToCell(base.gameObject);
			bool is_visible = true;
			//bool is_visible2 = true;
			bool is_visible3 = true;
			//bool is_visible4 = true;
			Grid.CellToXY(cell, out int num, out int num2);
			CellOffset rotatedCellOffset = new CellOffset(this.extents.x - num - 1, 0);
			//CellOffset rotatedCellOffset2 = new CellOffset(this.extents.x - num + this.extents.width, 0);
			CellOffset rotatedCellOffset3 = new CellOffset(0, this.extents.y - num2 + this.extents.height);
			//CellOffset rotatedCellOffset4 = new CellOffset(0, this.extents.y - num2 - 1);
			Rotatable component = base.GetComponent<Rotatable>();
			if (component)
			{
				rotatedCellOffset = component.GetRotatedCellOffset(rotatedCellOffset);
				//rotatedCellOffset2 = component.GetRotatedCellOffset(rotatedCellOffset2);
				rotatedCellOffset3 = component.GetRotatedCellOffset(rotatedCellOffset3);
				//rotatedCellOffset4 = component.GetRotatedCellOffset(rotatedCellOffset4);
			}
			int num3 = Grid.OffsetCell(cell, rotatedCellOffset);
			//int num4 = Grid.OffsetCell(cell, rotatedCellOffset2);
			int num5 = Grid.OffsetCell(cell, rotatedCellOffset3);
			//int num6 = Grid.OffsetCell(cell, rotatedCellOffset4);
			if (Grid.IsValidCell(num5))
			{
				is_visible3 = this.HasTileableNeighbour(num5);
			}
			if (!is_visible3)
			{
				if (Grid.IsValidCell(num3))
				{
					is_visible = !this.HasTileableNeighbour(num3);
				}
				//if (Grid.IsValidCell(num4))
				//{
				//	is_visible2 = !this.HasTileableNeighbour(num4);
				//}
			}
			else
			{
				is_visible = false;
				//is_visible2 = false;
			}
			if (MyGrid.IsStair(cell))
			{
				if (is_visible)
				{
					MyGrid.Masks[cell] |= MyGrid.Flags.Walkable;
				}
				else
				{
					MyGrid.Masks[cell] &= ~MyGrid.Flags.Walkable;
				}
			}
			foreach (KBatchedAnimController kbatchedAnimController in base.GetComponentsInChildren<KBatchedAnimController>())
			{
				foreach (KAnimHashedString symbol in leftSymbols)
				{
					kbatchedAnimController.SetSymbolVisiblity(symbol, is_visible);
				}
				foreach (KAnimHashedString symbol3 in leftSymbols2)
				{
					kbatchedAnimController.SetSymbolVisiblity(symbol3, !is_visible);
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
				KPrefabID component = gameObject.GetComponent<KPrefabID>();
				if (component != null && component.HasTag(Patches.tag_Stairs)) return true;
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

		private float lastAngle = 0f;
		private HandleVector<int>.Handle partitionerEntry;
		private HandleVector<int>.Handle partitionerEntry2;
		private Extents extents;

		private static readonly KAnimHashedString[] leftSymbols = new KAnimHashedString[]
		{
			new KAnimHashedString("cap_left"),
			new KAnimHashedString("cap_left_place")
		};

		//private static readonly KAnimHashedString[] rightSymbols = new KAnimHashedString[]
		//{
		//	new KAnimHashedString("cap_right"),
		//	new KAnimHashedString("cap_right_place")
		//};

		private static readonly KAnimHashedString[] leftSymbols2 = new KAnimHashedString[]
		{
			new KAnimHashedString("rcap_left"),
			new KAnimHashedString("rcap_left_place")
		};

		//private static readonly KAnimHashedString[] rightSymbols2 = new KAnimHashedString[]
		//{
		//	new KAnimHashedString("rcap_right"),
		//	new KAnimHashedString("rcap_right_place")
		//};
	}

	public class Stair : KMonoBehaviour, IEffectDescriptor
	{
		protected override void OnPrefabInit()
		{
			base.OnPrefabInit();
			base.GetComponent<KPrefabID>().AddTag(Patches.tag_Stairs, false);
		}

		protected override void OnSpawn()
		{
			base.OnSpawn();
			base.GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Normal, null);
			base.Subscribe<Stair>(493375141, Stair.OnRefreshUserMenuDelegate);
			base.Subscribe<Stair>(-111137758, Stair.OnRefreshUserMenuDelegate);
			Rotatable rotatable = this.GetComponent<Rotatable>();
			int cell = Grid.PosToCell(this);
			MyGrid.Masks[cell] |= MyGrid.Flags.HasStair;
			MyGrid.Masks[cell] |= MyGrid.Flags.Walkable;
			if (rotatable.GetOrientation() == Orientation.FlipH) {
				MyGrid.Masks[cell] |= MyGrid.Flags.RightSet; }
			Pathfinding.Instance.AddDirtyNavGridCell(cell);
			if (this.blocked)
			{
				OnBlock();
			}
		}

		protected override void OnCleanUp()
		{
			base.OnCleanUp();
			int cell = Grid.PosToCell(this);
			bool isscaff = MyGrid.IsScaffolding(cell);
			MyGrid.Masks[cell] = 0;
			if (isscaff) MyGrid.Masks[cell] |= MyGrid.Flags.HasScaffolding;
			Pathfinding.Instance.AddDirtyNavGridCell(cell);
			//if (Patches.ChainedDeconstruction)
			//{
			//	Deconstructable deconstructable = base.GetComponent<Deconstructable>();
			//	if (deconstructable != null && deconstructable.IsMarkedForDeconstruction())
			//	{
			//		MyGrid.ForceDeconstruction(Grid.CellAbove(cell));
			//		MyGrid.ForceDeconstruction(Grid.CellBelow(cell));
			//		MyGrid.ForceDeconstruction(Grid.CellLeft(cell));
			//		MyGrid.ForceDeconstruction(Grid.CellRight(cell));
			//	}
			//}
		}

		public List<Descriptor> GetDescriptors(BuildingDef def)
		{
			List<Descriptor> list = null;
			if (this.upwardsMovementSpeedMultiplier != 1f)
			{
				list = new List<Descriptor>();
				Descriptor descriptor = default(Descriptor);
				descriptor.SetupDescriptor(string.Format(UI.BUILDINGEFFECTS.DUPLICANTMOVEMENTBOOST, 
					GameUtil.GetFormattedPercent(this.upwardsMovementSpeedMultiplier * 100f - 100f, GameUtil.TimeSlice.None)),
					string.Format(UI.BUILDINGEFFECTS.TOOLTIPS.DUPLICANTMOVEMENTBOOST, GameUtil.GetFormattedPercent(this.upwardsMovementSpeedMultiplier * 100f - 100f, GameUtil.TimeSlice.None)), 
					Descriptor.DescriptorType.Effect);
				list.Add(descriptor);
			}
			return list;
		}

		private void OnBlock()
		{
			this.blocked = true;
			int cell = Grid.PosToCell(this);
			MyGrid.Masks[cell] |= MyGrid.Flags.Blocked;
		}
		private void OnResume()
		{
			this.blocked = false;
			int cell = Grid.PosToCell(this);
			MyGrid.Masks[cell] &= ~MyGrid.Flags.Blocked;
		}
		private void OnRefreshUserMenu(object data)
		{
			KIconButtonMenu.ButtonInfo button = (!this.blocked) ? 
				new KIconButtonMenu.ButtonInfo("action_building_disabled", Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.NAME"), new System.Action(this.OnBlock), Action.NumActions, null, null, null, Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.TOOLTIP"), true) :
				new KIconButtonMenu.ButtonInfo("action_direction_both", Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.NAME_OFF"), new System.Action(this.OnResume), Action.NumActions, null, null, null, Strings.Get("STRINGS.UI.USERMENUACTIONS.STAIRSBLOCK.TOOLTIP_OFF"), true);
			Game.Instance.userMenu.AddButton(base.gameObject, button, 1f);
		}
		private static readonly EventSystem.IntraObjectHandler<Stair> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<Stair>(delegate (Stair component, object data)
		{
			component.OnRefreshUserMenu(data);
		});

		[Serialize]
		private bool blocked;

		public float upwardsMovementSpeedMultiplier = 0.9f;

		public float downwardsMovementSpeedMultiplier = 1.5f;
	}
}