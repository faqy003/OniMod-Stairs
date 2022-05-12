using System;
using System.Collections.Generic;
using KSerialization;
using Stairs;
using STRINGS;
using UnityEngine;


namespace Stairs
{
	class Scaffolding : KMonoBehaviour
	{
		protected override void OnPrefabInit()
		{
			base.OnPrefabInit();
			base.Subscribe<Scaffolding>((int)GameHashes.RefreshUserMenu, Scaffolding.OnRefreshUserMenuDelegate);
		}
		public bool IsEnabled
		{
			get
			{
				return buildingEnabled;
			}
			set
			{
				Game.Instance.userMenu.Refresh(base.gameObject);
				this.buildingEnabled = value;
				base.GetComponent<KSelectable>().ToggleStatusItem(Db.Get().BuildingStatusItems.BuildingDisabled, !this.buildingEnabled, null);
				base.Trigger((int)GameHashes.PowerStatusChanged, this.buildingEnabled);
				int cell = Grid.PosToCell(this);
				if (this.buildingEnabled)
				{
					MyGrid.Masks[cell] |= MyGrid.Flags.HasScaffolding;
				}
				else
				{
					MyGrid.Masks[cell] &= ~MyGrid.Flags.HasScaffolding;
				}
				Pathfinding.Instance.AddDirtyNavGridCell(cell);
			}
		}
		private void OnRefreshUserMenu(object data)
		{
			bool isEnabled = this.IsEnabled;
			KIconButtonMenu.ButtonInfo button;
			if (isEnabled)
			{
				button = new KIconButtonMenu.ButtonInfo("action_building_disabled", UI.USERMENUACTIONS.ENABLEBUILDING.NAME, new System.Action(this.OnMenuToggle), Action.ToggleEnabled, null, null, null, UI.USERMENUACTIONS.ENABLEBUILDING.TOOLTIP, true);
			}
			else
			{
				button = new KIconButtonMenu.ButtonInfo("action_building_disabled", UI.USERMENUACTIONS.ENABLEBUILDING.NAME_OFF, new System.Action(this.OnMenuToggle), Action.ToggleEnabled, null, null, null, UI.USERMENUACTIONS.ENABLEBUILDING.TOOLTIP_OFF, true);
			}
			Game.Instance.userMenu.AddButton(base.gameObject, button, 1f);
		}
		private void OnMenuToggle()
		{
			if (this.IsEnabled)
			{
				base.Trigger((int)GameHashes.WorkChoreDisabled, "BuildingDisabled");
			}
			this.IsEnabled = !this.IsEnabled;
			Game.Instance.userMenu.Refresh(base.gameObject);
		}
		protected override void OnSpawn()
		{
			base.OnSpawn();
			base.GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Normal, null);
			int cell = Grid.PosToCell(this);
			this.IsEnabled = this.buildingEnabled;
			//MyGrid.Masks[cell] |= MyGrid.Flags.HasScaffolding;
			//Pathfinding.Instance.AddDirtyNavGridCell(cell);
		}

		protected override void OnCleanUp()
		{
			base.OnCleanUp();
			int cell = Grid.PosToCell(this);
			MyGrid.Masks[cell] &= ~MyGrid.Flags.HasScaffolding;
			Pathfinding.Instance.AddDirtyNavGridCell(cell);
			if (Patches.ChainedDeconstruction)
			{
				Deconstructable deconstructable = base.GetComponent<Deconstructable>();
				if (deconstructable != null && deconstructable.IsMarkedForDeconstruction())
				{
					MyGrid.ForceDeconstruction(Grid.CellLeft(cell), false);
					MyGrid.ForceDeconstruction(Grid.CellRight(cell), false);
				}
			}
		}

		[Serialize]
		private bool buildingEnabled = true;

		private static readonly EventSystem.IntraObjectHandler<Scaffolding> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<Scaffolding>(delegate (Scaffolding component, object data)
		{
			component.OnRefreshUserMenu(data);
		});
	}

}