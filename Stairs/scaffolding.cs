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
				this.buildingEnabled = value;
				Game.Instance.userMenu.Refresh(base.gameObject);
				base.Trigger((int)GameHashes.PowerStatusChanged, this.buildingEnabled);
				UpdateState();
			}
		}
		private void UpdateState()
		{
			int cell = Grid.PosToCell(this);
			base.GetComponent<KSelectable>().ToggleStatusItem(Db.Get().BuildingStatusItems.BuildingDisabled, !this.IsEnabled, null); //图标
			if (this.IsEnabled && !this.GetComponent<BuildingHP>().IsBroken)
			{
				MyGrid.Masks[cell] |= MyGrid.Flags.HasScaffolding;
			}
			else
			{
				MyGrid.Masks[cell] &= ~MyGrid.Flags.HasScaffolding;
			}
			Pathfinding.Instance.AddDirtyNavGridCell(cell);
		}
		private void OnRefreshUserMenu(object data)
		{
			KIconButtonMenu.ButtonInfo button;
			if (this.IsEnabled)
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
            this.IsEnabled = !this.IsEnabled;
		}
		protected override void OnSpawn()
		{
			base.OnSpawn();
			base.GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Normal, null);
			this.IsEnabled = this.buildingEnabled;
			base.Subscribe<Scaffolding>((int)GameHashes.BuildingBroken, Scaffolding.OnBuildingBrokenDelegate);
			base.Subscribe<Scaffolding>((int)GameHashes.BuildingFullyRepaired, Scaffolding.OnBuildingFullyRepairedDelegate);
		}
		protected override void OnCleanUp()
		{
			base.Unsubscribe<Scaffolding>((int)GameHashes.BuildingBroken, Scaffolding.OnBuildingBrokenDelegate, false);
			base.Unsubscribe<Scaffolding>((int)GameHashes.BuildingFullyRepaired, Scaffolding.OnBuildingFullyRepairedDelegate, false);
			base.OnCleanUp();
			int cell = Grid.PosToCell(this);
			MyGrid.Masks[cell] &= ~MyGrid.Flags.HasScaffolding;
			Pathfinding.Instance.AddDirtyNavGridCell(cell);
			//连环拆除
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
		private void OnBuildingBroken(object data)
		{
			UpdateState();
		}
		private void OnBuildingFullyRepaired(object data)
		{
			UpdateState();
		}

		private static readonly EventSystem.IntraObjectHandler<Scaffolding> OnBuildingBrokenDelegate = new EventSystem.IntraObjectHandler<Scaffolding>(delegate (Scaffolding component, object data)
		{
			component.OnBuildingBroken(data);
		});

		private static readonly EventSystem.IntraObjectHandler<Scaffolding> OnBuildingFullyRepairedDelegate = new EventSystem.IntraObjectHandler<Scaffolding>(delegate (Scaffolding component, object data)
		{
			component.OnBuildingFullyRepaired(data);
		});

		private static readonly EventSystem.IntraObjectHandler<Scaffolding> OnRefreshUserMenuDelegate = new EventSystem.IntraObjectHandler<Scaffolding>(delegate (Scaffolding component, object data)
		{
			component.OnRefreshUserMenu(data);
		});

		[Serialize]
		private bool buildingEnabled = true;
	}

}