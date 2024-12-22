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
            List<Tag> list = new List<Tag>
            {
                Patches.tag_Stairs
            };
            buildingDef.ReplacementTags = list;
            List<ObjectLayer> list2 = new List<ObjectLayer>
            {
                ObjectLayer.ReplacementTile
            };
            buildingDef.EquivalentReplacementLayers = list2;

            return buildingDef;
        }
        public static BuildingDef CreateScaffoldingDef(string id, string anim, float[] construction_mass, string[] construction_materials)
        {
            int width = 1;
            int height = 1;
            int hitpoints = 5;
            float construction_time = 12f;

            float melting_point = BUILDINGS.MELTING_POINT_KELVIN.TIER2;
            BuildLocationRule build_location_rule = BuildLocationRule.NotInTiles;
            EffectorValues none = NOISE_POLLUTION.NONE;
            BuildingDef buildingDef = BuildingTemplates.CreateBuildingDef(id, width, height, anim, hitpoints, construction_time, construction_mass, construction_materials, melting_point, build_location_rule, BUILDINGS.DECOR.BONUS.TIER0, none, 1f);
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

            buildingDef.ReplacementLayer = ObjectLayer.ReplacementTile;
            buildingDef.ReplacementCandidateLayers = new List<ObjectLayer> { ObjectLayer.AttachableBuilding };
            List<Tag> list = new List<Tag> { Patches.tag_Scaffolding };
            buildingDef.ReplacementTags = list;

            return buildingDef;
        }
    }
    public class ScaffoldingConfig : IBuildingConfig
    {
        public const string ID = "urfScaffolding";
        public override BuildingDef CreateBuildingDef()
        {
            //耗费材料
            float[] construction_mass = new float[]
            {
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0],
            };
            string[] construction_materials = new string[]
            {
                MATERIALS.METAL,
            };

            var buildingDef = BuidingTemplates.CreateScaffoldingDef(ID, "scaffolding_kanim", construction_mass, construction_materials);
            buildingDef.ConstructionTime = 2f;

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
            go.AddOrGet<AnimTileable>().objectLayer = ObjectLayer.AttachableBuilding;
            go.AddTag(Patches.tag_Scaffolding);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<Scaffolding>();
            //GeneratedBuildings.RemoveLoopingSounds(go);
        }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            go.AddTag(Patches.tag_Scaffolding);
        }
        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            go.AddOrGet<AnimTileable>().objectLayer = ObjectLayer.AttachableBuilding;
            base.DoPostConfigureUnderConstruction(go);
            go.AddTag(Patches.tag_Scaffolding);
        }
    }
    public class ScaffoldingAlt1Config : IBuildingConfig
    {
        public const string ID = "urfScaffolding_Alt1";
        public override BuildingDef CreateBuildingDef()
        {
            //耗费材料
            float[] construction_mass = new float[]
            {
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0],
            };
            string[] construction_materials = MATERIALS.RAW_MINERALS_OR_WOOD;

            var buildingDef = BuidingTemplates.CreateScaffoldingDef(ID, "scaffolding_alt1_kanim", construction_mass, construction_materials);
            buildingDef.BaseDecor = 0f;

            return buildingDef;
        }
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
            go.AddOrGet<AnimTileable>().objectLayer = ObjectLayer.AttachableBuilding;
            go.AddTag(Patches.tag_Scaffolding);
        }
        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<Scaffolding>();
            //GeneratedBuildings.RemoveLoopingSounds(go);
        }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            go.AddTag(Patches.tag_Scaffolding);
        }
        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            go.AddOrGet<AnimTileable>().objectLayer = ObjectLayer.AttachableBuilding;
            base.DoPostConfigureUnderConstruction(go);
            go.AddTag(Patches.tag_Scaffolding);
        }
    }
    public class ScaffoldingAlt2Config : IBuildingConfig
    {
        public const string ID = "urfScaffolding_Alt2";
        public override BuildingDef CreateBuildingDef()
        {
            //耗费材料
            float[] construction_mass = new float[]
            {
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER2[0],
            };
            string[] construction_materials = new string[]
            {
                MATERIALS.REFINED_METAL,
            };

            var buildingDef = BuidingTemplates.CreateScaffoldingDef(ID, "scaffolding_alt2_kanim", construction_mass, construction_materials);
            buildingDef.HitPoints = 50;
            buildingDef.ConstructionTime = 25f;

            return buildingDef;
        }
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
            go.AddOrGet<AnimTileable>().objectLayer = ObjectLayer.AttachableBuilding;
            go.AddTag(Patches.tag_Scaffolding);
        }
        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<Scaffolding>();
            //GeneratedBuildings.RemoveLoopingSounds(go);
        }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            go.AddTag(Patches.tag_Scaffolding);
        }
        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            go.AddOrGet<AnimTileable>().objectLayer = ObjectLayer.AttachableBuilding;
            base.DoPostConfigureUnderConstruction(go);
            go.AddTag(Patches.tag_Scaffolding);
        }
    }
    public class StairsConfig : IBuildingConfig
    {
        public const string ID = "Stairs";
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
                MATERIALS.RAW_MINERALS_OR_WOOD[0],
                MATERIALS.METAL
            };

            var buildingDef = BuidingTemplates.CreateStairsDef(ID, "stairs_kanim", construction_mass, construction_materials);

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            go.AddOrGet<Stair>();
            go.AddOrGet<AnimStairs>();
            go.AddTag(Patches.tag_Stairs);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            AnimStairs.PrearePreview(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            go.AddOrGet<AnimStairs>();
            go.AddTag(Patches.tag_Stairs);
        }
    }
    public class StairsAlt1Config : IBuildingConfig
    {
        public const string ID = "Stairs_Alt1";
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
            buildingDef.AudioCategory = "Glass";

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            go.AddOrGet<Stair>();
            go.AddOrGet<AnimStairs>();
            go.AddTag(Patches.tag_Stairs);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            AnimStairs.PrearePreview(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            go.AddOrGet<AnimStairs>();
            go.AddTag(Patches.tag_Stairs);
        }
    }
    public class StairsClassicConfig : IBuildingConfig
    {
        public const string ID = "Stairs_Classic";
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
                MATERIALS.BUILDABLERAW,
                MATERIALS.METAL
            };
            var buildingDef = BuidingTemplates.CreateStairsDef(ID, "stairs_classic_kanim", construction_mass, construction_materials);

            return buildingDef;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            GeneratedBuildings.MakeBuildingAlwaysOperational(go);
            go.AddOrGet<Stair>();
            go.AddOrGet<AnimStairs>();
            go.AddTag(Patches.tag_Stairs);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            base.DoPostConfigurePreview(def, go);
            AnimStairs.PrearePreview(go);
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            base.DoPostConfigureUnderConstruction(go);
            go.AddOrGet<AnimStairs>();
            go.AddTag(Patches.tag_Stairs);
        }
    }
}
