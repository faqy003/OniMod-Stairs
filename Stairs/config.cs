using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace Stairs
{
	public class BuidingTemplates
    {
		public static BuildingDef CreateStairsDef(string id, string anim, float[] construction_mass, string[] construction_materials)
		{
			int width = 1;
			int height = 1;
			int hitpoints = 10;
			float construction_time = 10f;

			float melting_point = BUILDINGS.MELTING_POINT_KELVIN.TIER2;
			BuildLocationRule build_location_rule = BuildLocationRule.Anywhere;
			EffectorValues none = NOISE_POLLUTION.NONE;
			BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(id, width, height, anim, hitpoints, construction_time, construction_mass, construction_materials, melting_point, build_location_rule, BUILDINGS.DECOR.BONUS.TIER0, none, 0.2f);
			buildingDef.Floodable = false;
			buildingDef.Entombable = false;
			buildingDef.Overheatable = false;
			buildingDef.AudioCategory = "Metal";
			buildingDef.AudioSize = "small";
			buildingDef.BaseTimeUntilRepair = -1f;
			buildingDef.DragBuild = true;
			buildingDef.IsFoundation = false;
			buildingDef.BaseDecor = -2f;
			buildingDef.TileLayer = ObjectLayer.LadderTile;
			buildingDef.PermittedRotations = PermittedRotations.FlipH;

			buildingDef.ReplacementLayer = ObjectLayer.ReplacementLadder;
			List<Tag> list = new List<Tag>();
			list.Add(Patches.tag_Stairs);
			buildingDef.ReplacementTags = list;
			List<ObjectLayer> list2 = new List<ObjectLayer>();
			list2.Add(ObjectLayer.ReplacementTile);
			buildingDef.EquivalentReplacementLayers = list2;

			return buildingDef;
		}
	}
	public class ScaffoldingConfig : IBuildingConfig
	{
		public const string ID = "urfScaffolding";
		public override BuildingDef CreateBuildingDef()
		{
			int width = 1;
			int height = 1;
			string anim = "scaffolding_kanim";
			int hitpoints = 5;
			float construction_time = 2f;

			//耗费材料
			float[] construction_mass = new float[]
			{
				BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0],
			};
			string[] construction_materials = new string[]
			{
				"Metal",
			};

			float melting_point = BUILDINGS.MELTING_POINT_KELVIN.TIER2;
			BuildLocationRule build_location_rule = BuildLocationRule.NotInTiles;
			EffectorValues none = NOISE_POLLUTION.NONE;
			BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(ID, width, height, anim, hitpoints, construction_time, construction_mass, construction_materials, melting_point, build_location_rule, BUILDINGS.DECOR.BONUS.TIER0, none, 1f);
			buildingDef.Floodable = false;
			buildingDef.Entombable = false;
			buildingDef.Overheatable = false;
			buildingDef.AudioCategory = "Metal";
			buildingDef.AudioSize = "small";
			buildingDef.BaseTimeUntilRepair = -1f;
			buildingDef.DragBuild = true;
			buildingDef.IsFoundation = false;
			buildingDef.BaseDecor = -4f;
			buildingDef.ObjectLayer = ObjectLayer.AttachableBuilding;
			buildingDef.SceneLayer = Grid.SceneLayer.TileMain;

			return buildingDef;
		}

		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
			GeneratedBuildings.MakeBuildingAlwaysOperational(go);
			BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
			go.AddOrGet<AnimTileable>().objectLayer = ObjectLayer.AttachableBuilding;
			//go.AddOrGet<BuildingHP>().destroyOnDamaged = true;
		}

		public override void DoPostConfigureComplete(GameObject go)
		{
			go.AddOrGet<Scaffolding>();
			GeneratedBuildings.RemoveLoopingSounds(go);
		}
		public override void DoPostConfigureUnderConstruction(GameObject go)
		{
			go.AddOrGet<AnimTileable>().objectLayer = ObjectLayer.AttachableBuilding;
			base.DoPostConfigureUnderConstruction(go);
		}
	}
	public class StairsConfig : IBuildingConfig
    {
		public static string ID = "Stairs";
		public override BuildingDef CreateBuildingDef()
		{
			//耗费材料
			float[] construction_mass = new float[]
			{
				BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0],
				BUILDINGS.CONSTRUCTION_MASS_KG.TIER0[0]
			};
			string[] construction_materials = new string[]
			{
				"BuildableRaw",
				"Metal"
			};

			var buildingDef = BuidingTemplates.CreateStairsDef(ID, "stairs_kanim",construction_mass,construction_materials);

			return buildingDef;
		}

		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
			GeneratedBuildings.MakeBuildingAlwaysOperational(go);
			go.AddOrGet<Stair>();
			go.AddOrGet<AnimStairs>();
		}

		public override void DoPostConfigureComplete(GameObject go)
		{
		}

		public override void DoPostConfigureUnderConstruction(GameObject go)
		{
			base.DoPostConfigureUnderConstruction(go);
		}
	}
	public class StairsAlt1Config : IBuildingConfig
	{
		public static string ID = "Stairs_Alt1";
		public override BuildingDef CreateBuildingDef()
		{
			//耗费材料
			float[] construction_mass = new float[]
			{
				BUILDINGS.CONSTRUCTION_MASS_KG.TIER0[0],
				BUILDINGS.CONSTRUCTION_MASS_KG.TIER0[0]
			};
			string[] construction_materials = new string[]
			{
				MATERIALS.REFINED_METAL,
				MATERIALS.GLASS
			};
			var buildingDef = BuidingTemplates.CreateStairsDef(ID, "stairs_alt1_kanim", construction_mass, construction_materials);
			buildingDef.BaseDecor = 8f;
			buildingDef.BaseDecorRadius = 2f;

			return buildingDef;
		}

		public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
		{
			GeneratedBuildings.MakeBuildingAlwaysOperational(go);
			go.AddOrGet<Stair>();
			go.AddOrGet<AnimStairs>();
		}

		public override void DoPostConfigureComplete(GameObject go)
		{
		}

		public override void DoPostConfigureUnderConstruction(GameObject go)
		{
			base.DoPostConfigureUnderConstruction(go);
		}
	}
}
