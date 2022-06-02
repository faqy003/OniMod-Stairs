using System;
using System.Collections.Generic;
using KSerialization;
using Stairs;
using STRINGS;
using UnityEngine;


namespace Stairs
{
    class Scaffolding : KMonoBehaviour, IGameObjectEffectDescriptor
    {
        protected override void OnPrefabInit()
        {
            base.OnPrefabInit();
            Subscribe((int)GameHashes.RefreshUserMenu, OnRefreshUserMenuDelegate);
        }
        public bool IsEnabled
        {
            get
            {
                return buildingEnabled;
            }
            set
            {
                buildingEnabled = value;
                Game.Instance.userMenu.Refresh(gameObject);
                Trigger((int)GameHashes.PowerStatusChanged, buildingEnabled);
                UpdateState();
            }
        }
        private void UpdateState()
        {
            int cell = Grid.PosToCell(this);
            GetComponent<KSelectable>().ToggleStatusItem(Db.Get().BuildingStatusItems.BuildingDisabled, !IsEnabled, null); //图标
            if (IsEnabled && !GetComponent<BuildingHP>().IsBroken)
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
            if (IsEnabled)
            {
                button = new KIconButtonMenu.ButtonInfo("action_building_disabled", UI.USERMENUACTIONS.ENABLEBUILDING.NAME, new System.Action(OnMenuToggle), Action.ToggleEnabled, null, null, null, UI.USERMENUACTIONS.ENABLEBUILDING.TOOLTIP, true);
            }
            else
            {
                button = new KIconButtonMenu.ButtonInfo("action_building_disabled", UI.USERMENUACTIONS.ENABLEBUILDING.NAME_OFF, new System.Action(OnMenuToggle), Action.ToggleEnabled, null, null, null, UI.USERMENUACTIONS.ENABLEBUILDING.TOOLTIP_OFF, true);
            }
            Game.Instance.userMenu.AddButton(gameObject, button, 1f);
        }
        private void OnMenuToggle()
        {
            IsEnabled = !IsEnabled;
        }
        protected override void OnSpawn()
        {
            base.OnSpawn();
            GetComponent<KSelectable>().SetStatusItem(Db.Get().StatusItemCategories.Main, Db.Get().BuildingStatusItems.Normal, null);
            IsEnabled = buildingEnabled;
            Subscribe((int)GameHashes.BuildingBroken, OnBuildingBrokenDelegate);
            Subscribe((int)GameHashes.BuildingFullyRepaired, OnBuildingFullyRepairedDelegate);
            PrimaryElement primary_element = GetComponent<PrimaryElement>();
            if (primary_element.ElementID == SimHashes.Steel && this.PrefabID() == Patches.tag_ScaffoldingAlt2)
            {
                GetComponent<BuildingHP>().invincible = true;
            }
        }
        protected override void OnCleanUp()
        {
            Unsubscribe((int)GameHashes.BuildingBroken, OnBuildingBrokenDelegate, false);
            Unsubscribe((int)GameHashes.BuildingFullyRepaired, OnBuildingFullyRepairedDelegate, false);
            base.OnCleanUp();
            int cell = Grid.PosToCell(this);
            MyGrid.Masks[cell] &= ~MyGrid.Flags.HasScaffolding;
            Pathfinding.Instance.AddDirtyNavGridCell(cell);
            //连环拆除
            if (Patches.ChainedDeconstruction)
            {
                Deconstructable deconstructable = GetComponent<Deconstructable>();
                if (deconstructable != null && deconstructable.IsMarkedForDeconstruction())
                {
                    MyGrid.ForceDeconstruction(Grid.CellLeft(cell), false);
                    MyGrid.ForceDeconstruction(Grid.CellRight(cell), false);
                }
            }
        }
        public List<Descriptor> GetDescriptors(GameObject go)
        {
            List<Descriptor> list = null;
            if (go.PrefabID() == Patches.tag_ScaffoldingAlt2)
            {
                list = new List<Descriptor>();
                Descriptor descriptor = default;
                descriptor.SetupDescriptor(string.Format(UI.BUILDINGEFFECTS.DUPLICANTMOVEMENTBOOST,
                    GameUtil.AddPositiveSign(GameUtil.GetFormattedPercent(MyTransitionLayer.scaffoldingSpeedMultiplier * 100f - 100f),
                    MyTransitionLayer.scaffoldingSpeedMultiplier - 1f >= 0f)), string.Format(UI.BUILDINGEFFECTS.TOOLTIPS.DUPLICANTMOVEMENTBOOST,
                    GameUtil.GetFormattedPercent(MyTransitionLayer.scaffoldingSpeedMultiplier * 100f - 100f)), Descriptor.DescriptorType.Effect);
                list.Add(descriptor);
            }
            return list;
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