using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class BuildRoadsideGarageScene
{
    private const string ScenePath = "Assets/Scenes/RoadsideRepairGarage.unity";
    private const string Kenney = "Assets/Kenney/";

    private static readonly Dictionary<string, Material> Materials = new Dictionary<string, Material>();

    [MenuItem("Automotive Idle/Build Roadside Garage Scene")]
    public static void Build()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "RoadsideRepairGarage";

        EnsureFolder("Assets/Scenes");
        CreateMaterials();
        CreateRootGroups();

        BuildGround();
        BuildRoads();
        BuildGarageShell();
        BuildLittleCity();
        BuildCoreGameplayObjects();
        BuildUnlockStations();
        BuildCharactersAndVehicles();
        BuildDecorations();
        BuildWaypoints();
        BuildCameraAndLighting();
        BuildSceneNotes();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Built scene: " + ScenePath);
    }

    private static void CreateRootGroups()
    {
        foreach (var group in new[]
                 {
                     "_Scene",
                     "_Scene/Roads",
                     "_Scene/Buildings",
                     "_Scene/City",
                     "_Scene/City/RoadGrid",
                     "_Scene/City/CommercialBlocks",
                     "_Scene/City/SuburbanBlocks",
                     "_Scene/City/IndustrialBlocks",
                     "_Scene/City/TrafficAndParking",
                     "_Scene/City/StreetFurniture",
                     "_Scene/Gameplay",
                     "_Scene/Gameplay/Zones",
                     "_Scene/Gameplay/Waypoints",
                     "_Scene/Characters",
                     "_Scene/Vehicles",
                     "_Scene/Decorations",
                     "_Scene/Labels"
                 })
        {
            var parts = group.Split('/');
            Transform parent = null;
            string current = "";
            foreach (var part in parts)
            {
                current = string.IsNullOrEmpty(current) ? part : current + "/" + part;
                var existing = GameObject.Find(current);
                if (existing == null)
                {
                    existing = new GameObject(part);
                    if (parent != null) existing.transform.SetParent(parent, false);
                }
                parent = existing.transform;
            }
        }
    }

    private static void BuildGround()
    {
        Primitive("City terrain base", PrimitiveType.Cube, new Vector3(0, -0.16f, 0), new Vector3(88, 0.08f, 76), Mat("Grass"), "_Scene");
        Primitive("City central asphalt", PrimitiveType.Cube, new Vector3(0, -0.13f, -12), new Vector3(74, 0.08f, 11), Mat("Asphalt"), "_Scene");
        Primitive("City north asphalt", PrimitiveType.Cube, new Vector3(0, -0.14f, 24), new Vector3(78, 0.08f, 8), Mat("Asphalt"), "_Scene");
        Primitive("City west asphalt", PrimitiveType.Cube, new Vector3(-34, -0.14f, 2), new Vector3(8, 0.08f, 64), Mat("Asphalt"), "_Scene");
        Primitive("City east asphalt", PrimitiveType.Cube, new Vector3(34, -0.14f, 2), new Vector3(8, 0.08f, 64), Mat("Asphalt"), "_Scene");
        Primitive("Commercial sidewalk north", PrimitiveType.Cube, new Vector3(0, -0.11f, 18), new Vector3(72, 0.08f, 5), Mat("Sidewalk"), "_Scene");
        Primitive("Garage sidewalk south", PrimitiveType.Cube, new Vector3(0, -0.11f, -6.5f), new Vector3(42, 0.08f, 3), Mat("Sidewalk"), "_Scene");
        Primitive("Garage floor", PrimitiveType.Cube, new Vector3(0, -0.08f, 2), new Vector3(30, 0.12f, 22), Mat("Floor"), "_Scene");
        Primitive("Front road base", PrimitiveType.Cube, new Vector3(0, -0.09f, -12), new Vector3(50, 0.1f, 8), Mat("Asphalt"), "_Scene");
        Primitive("Back grass base", PrimitiveType.Cube, new Vector3(0, -0.12f, 12), new Vector3(54, 0.08f, 18), Mat("Grass"), "_Scene");
        Primitive("Garage playable bounds", PrimitiveType.Cube, new Vector3(0, 0.01f, 2), new Vector3(28, 0.02f, 20), Mat("TransparentBounds"), "_Scene/Gameplay/Zones");
    }

    private static void BuildRoads()
    {
        Add("road_front_01", Road("road-straight.glb"), new Vector3(-16, 0, -12), new Vector3(0, 90, 0), Vector3.one, "_Scene/Roads");
        Add("road_front_02", Road("road-straight.glb"), new Vector3(-8, 0, -12), new Vector3(0, 90, 0), Vector3.one, "_Scene/Roads");
        Add("road_front_03", Road("road-straight.glb"), new Vector3(0, 0, -12), new Vector3(0, 90, 0), Vector3.one, "_Scene/Roads");
        Add("road_front_04", Road("road-straight.glb"), new Vector3(8, 0, -12), new Vector3(0, 90, 0), Vector3.one, "_Scene/Roads");
        Add("road_front_05", Road("road-straight.glb"), new Vector3(16, 0, -12), new Vector3(0, 90, 0), Vector3.one, "_Scene/Roads");
        Add("road_entry_driveway", Road("road-driveway-single.glb"), new Vector3(-8, 0, -8), Vector3.zero, Vector3.one, "_Scene/Roads");
        Add("road_exit_driveway", Road("road-driveway-single.glb"), new Vector3(8, 0, -8), new Vector3(0, 180, 0), Vector3.one, "_Scene/Roads");
        Add("road_service_pad", Racing("roadPitGarage.glb"), new Vector3(0, 0, 1), Vector3.zero, Vector3.one, "_Scene/Roads");
    }

    private static void BuildGarageShell()
    {
        Add("garage_bay_shell", Racing("pitsGarage.glb"), new Vector3(0, 0, 4), new Vector3(0, 180, 0), Vector3.one, "_Scene/Buildings");
        Add("garage_left_corner", Racing("pitsGarageCorner.glb"), new Vector3(-8, 0, 4), new Vector3(0, 180, 0), Vector3.one, "_Scene/Buildings");
        Add("reception_office", Racing("pitsOffice.glb"), new Vector3(10, 0, 7), new Vector3(0, 180, 0), Vector3.one, "_Scene/Buildings");
        Add("reception_office_corner", Racing("pitsOfficeCorner.glb"), new Vector3(14, 0, 7), new Vector3(0, 180, 0), Vector3.one, "_Scene/Buildings");
        Add("garage_sign_low", Racing("billboardLow.glb"), new Vector3(0, 0, 11.5f), new Vector3(0, 180, 0), Vector3.one, "_Scene/Buildings");
        Add("roadside_sign", Road("sign-highway-wide.glb"), new Vector3(-18, 0, -7), new Vector3(0, 90, 0), Vector3.one, "_Scene/Buildings");
        Add("lot_light_left", Road("light-square-double.glb"), new Vector3(-15, 0, -7), new Vector3(0, 45, 0), Vector3.one, "_Scene/Buildings");
        Add("lot_light_right", Road("light-square-double.glb"), new Vector3(15, 0, -7), new Vector3(0, -45, 0), Vector3.one, "_Scene/Buildings");
    }

    private static void BuildLittleCity()
    {
        BuildCityRoadGrid();
        BuildCommercialBlock();
        BuildSuburbanBlocks();
        BuildIndustrialBlock();
        BuildTrafficAndParking();
        BuildStreetFurniture();
    }

    private static void BuildCityRoadGrid()
    {
        for (int i = 0; i < 9; i++)
        {
            float x = -32 + i * 8;
            Add("city_main_road_south_" + i, Road("road-straight.glb"), new Vector3(x, 0, -28), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/RoadGrid");
            Add("city_main_road_north_" + i, Road("road-straight.glb"), new Vector3(x, 0, 24), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/RoadGrid");
        }

        for (int i = 0; i < 7; i++)
        {
            float z = -24 + i * 8;
            Add("city_west_road_" + i, Road("road-straight.glb"), new Vector3(-34, 0, z), Vector3.zero, Vector3.one, "_Scene/City/RoadGrid");
            Add("city_east_road_" + i, Road("road-straight.glb"), new Vector3(34, 0, z), Vector3.zero, Vector3.one, "_Scene/City/RoadGrid");
        }

        Add("city_crossroad_nw", Road("road-crossroad.glb"), new Vector3(-34, 0, 24), Vector3.zero, Vector3.one, "_Scene/City/RoadGrid");
        Add("city_crossroad_ne", Road("road-crossroad.glb"), new Vector3(34, 0, 24), Vector3.zero, Vector3.one, "_Scene/City/RoadGrid");
        Add("city_crossroad_sw", Road("road-crossroad.glb"), new Vector3(-34, 0, -28), Vector3.zero, Vector3.one, "_Scene/City/RoadGrid");
        Add("city_crossroad_se", Road("road-crossroad.glb"), new Vector3(34, 0, -28), Vector3.zero, Vector3.one, "_Scene/City/RoadGrid");
        Add("city_roundabout", Road("road-roundabout.glb"), new Vector3(34, 0, -12), Vector3.zero, Vector3.one, "_Scene/City/RoadGrid");
        Add("city_west_driveway_to_garage", Road("road-side-entry.glb"), new Vector3(-24, 0, -12), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/RoadGrid");
        Add("city_east_driveway_from_garage", Road("road-side-exit.glb"), new Vector3(24, 0, -12), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/RoadGrid");
    }

    private static void BuildCommercialBlock()
    {
        Add("commercial_shop_auto_parts", Commercial("building-a.glb"), new Vector3(-28, 0, 31), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/CommercialBlocks");
        Add("commercial_shop_cafe", Commercial("building-c.glb"), new Vector3(-18, 0, 31), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/CommercialBlocks");
        Add("commercial_shop_office", Commercial("building-e.glb"), new Vector3(-8, 0, 31), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/CommercialBlocks");
        Add("commercial_shop_market", Commercial("building-h.glb"), new Vector3(4, 0, 31), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/CommercialBlocks");
        Add("commercial_shop_bank", Commercial("building-k.glb"), new Vector3(16, 0, 31), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/CommercialBlocks");
        Add("commercial_shop_showroom", Commercial("low-detail-building-wide-b.glb"), new Vector3(29, 0, 31), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/CommercialBlocks");

        Add("city_skyline_01", Commercial("building-skyscraper-a.glb"), new Vector3(-26, 0, 39), new Vector3(0, 180, 0), new Vector3(0.9f, 0.9f, 0.9f), "_Scene/City/CommercialBlocks");
        Add("city_skyline_02", Commercial("building-skyscraper-c.glb"), new Vector3(-12, 0, 40), new Vector3(0, 180, 0), new Vector3(0.85f, 0.85f, 0.85f), "_Scene/City/CommercialBlocks");
        Add("city_skyline_03", Commercial("building-skyscraper-e.glb"), new Vector3(8, 0, 39), new Vector3(0, 180, 0), new Vector3(0.9f, 0.9f, 0.9f), "_Scene/City/CommercialBlocks");
        Add("city_skyline_04", Commercial("building-skyscraper-b.glb"), new Vector3(24, 0, 40), new Vector3(0, 180, 0), new Vector3(0.8f, 0.8f, 0.8f), "_Scene/City/CommercialBlocks");

        Add("commercial_awning_01", Commercial("detail-awning-wide.glb"), new Vector3(-28, 0, 24.5f), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/CommercialBlocks");
        Add("commercial_awning_02", Commercial("detail-awning.glb"), new Vector3(-18, 0, 24.5f), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/CommercialBlocks");
        Add("commercial_parasol_01", Commercial("detail-parasol-a.glb"), new Vector3(-12, 0, 19), Vector3.zero, Vector3.one, "_Scene/City/CommercialBlocks");
        Add("commercial_parasol_02", Commercial("detail-parasol-b.glb"), new Vector3(18, 0, 19), Vector3.zero, Vector3.one, "_Scene/City/CommercialBlocks");
    }

    private static void BuildSuburbanBlocks()
    {
        Add("suburban_house_west_01", Suburban("building-type-a.glb"), new Vector3(-48, 0, 12), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
        Add("suburban_house_west_02", Suburban("building-type-d.glb"), new Vector3(-48, 0, 0), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
        Add("suburban_house_west_03", Suburban("building-type-h.glb"), new Vector3(-48, 0, -14), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
        Add("suburban_house_east_01", Suburban("building-type-b.glb"), new Vector3(48, 0, 12), new Vector3(0, -90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
        Add("suburban_house_east_02", Suburban("building-type-f.glb"), new Vector3(48, 0, 0), new Vector3(0, -90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
        Add("suburban_house_east_03", Suburban("building-type-j.glb"), new Vector3(48, 0, -14), new Vector3(0, -90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");

        Add("suburban_driveway_west_01", Suburban("driveway-short.glb"), new Vector3(-40, 0, 12), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
        Add("suburban_driveway_west_02", Suburban("driveway-short.glb"), new Vector3(-40, 0, 0), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
        Add("suburban_driveway_east_01", Suburban("driveway-short.glb"), new Vector3(40, 0, 12), new Vector3(0, -90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
        Add("suburban_driveway_east_02", Suburban("driveway-short.glb"), new Vector3(40, 0, 0), new Vector3(0, -90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");

        for (int i = 0; i < 5; i++)
        {
            Add("suburban_west_fence_" + i, Suburban("fence-low.glb"), new Vector3(-43, 0, -22 + i * 8), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
            Add("suburban_east_fence_" + i, Suburban("fence-low.glb"), new Vector3(43, 0, -22 + i * 8), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/SuburbanBlocks");
        }
    }

    private static void BuildIndustrialBlock()
    {
        Add("industrial_warehouse_01", Industrial("building-a.glb"), new Vector3(-26, 0, -40), Vector3.zero, Vector3.one, "_Scene/City/IndustrialBlocks");
        Add("industrial_warehouse_02", Industrial("building-d.glb"), new Vector3(-13, 0, -40), Vector3.zero, Vector3.one, "_Scene/City/IndustrialBlocks");
        Add("industrial_factory_01", Industrial("building-h.glb"), new Vector3(4, 0, -40), Vector3.zero, Vector3.one, "_Scene/City/IndustrialBlocks");
        Add("industrial_factory_02", Industrial("building-m.glb"), new Vector3(19, 0, -40), Vector3.zero, Vector3.one, "_Scene/City/IndustrialBlocks");
        Add("industrial_tank_01", Industrial("detail-tank.glb"), new Vector3(30, 0, -39), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/IndustrialBlocks");
        Add("industrial_chimney_01", Industrial("chimney-medium.glb"), new Vector3(13, 0, -34), Vector3.zero, Vector3.one, "_Scene/City/IndustrialBlocks");
        Add("industrial_chimney_02", Industrial("chimney-small.glb"), new Vector3(23, 0, -34), Vector3.zero, Vector3.one, "_Scene/City/IndustrialBlocks");
        Add("industrial_delivery_truck", Car("truck-flat.glb"), new Vector3(-20, 0, -31), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/IndustrialBlocks");
    }

    private static void BuildTrafficAndParking()
    {
        Add("parked_taxi_storefront", Car("taxi.glb"), new Vector3(-24, 0, 19), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/TrafficAndParking");
        Add("parked_van_storefront", Car("van.glb"), new Vector3(-2, 0, 19), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/TrafficAndParking");
        Add("parked_suv_showroom", Car("suv-luxury.glb"), new Vector3(23, 0, 19), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/TrafficAndParking");
        Add("traffic_sedan_southbound", Car("sedan-sports.glb"), new Vector3(-34, 0, -4), Vector3.zero, Vector3.one, "_Scene/City/TrafficAndParking");
        Add("traffic_delivery_east", Car("delivery.glb"), new Vector3(5, 0, -28), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/TrafficAndParking");
        Add("traffic_truck_northbound", Car("truck.glb"), new Vector3(34, 0, 6), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/TrafficAndParking");
        Add("parked_home_car_01", Car("sedan.glb"), new Vector3(-40, 0, 12), new Vector3(0, -90, 0), Vector3.one, "_Scene/City/TrafficAndParking");
        Add("parked_home_car_02", Car("hatchback-sports.glb"), new Vector3(40, 0, 0), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/TrafficAndParking");

        Primitive("parking_stripe_01", PrimitiveType.Cube, new Vector3(-24, 0.02f, 16), new Vector3(0.15f, 0.03f, 5), Mat("ParkingPaint"), "_Scene/City/TrafficAndParking");
        Primitive("parking_stripe_02", PrimitiveType.Cube, new Vector3(-18, 0.02f, 16), new Vector3(0.15f, 0.03f, 5), Mat("ParkingPaint"), "_Scene/City/TrafficAndParking");
        Primitive("parking_stripe_03", PrimitiveType.Cube, new Vector3(-2, 0.02f, 16), new Vector3(0.15f, 0.03f, 5), Mat("ParkingPaint"), "_Scene/City/TrafficAndParking");
        Primitive("parking_stripe_04", PrimitiveType.Cube, new Vector3(4, 0.02f, 16), new Vector3(0.15f, 0.03f, 5), Mat("ParkingPaint"), "_Scene/City/TrafficAndParking");
        Primitive("parking_stripe_05", PrimitiveType.Cube, new Vector3(22, 0.02f, 16), new Vector3(0.15f, 0.03f, 5), Mat("ParkingPaint"), "_Scene/City/TrafficAndParking");
        Primitive("parking_stripe_06", PrimitiveType.Cube, new Vector3(28, 0.02f, 16), new Vector3(0.15f, 0.03f, 5), Mat("ParkingPaint"), "_Scene/City/TrafficAndParking");
    }

    private static void BuildStreetFurniture()
    {
        for (int i = 0; i < 6; i++)
        {
            float x = -30 + i * 12;
            Add("city_light_north_" + i, Road("light-square.glb"), new Vector3(x, 0, 17), Vector3.zero, Vector3.one, "_Scene/City/StreetFurniture");
            Add("city_tree_north_" + i, Suburban(i % 2 == 0 ? "tree-small.glb" : "tree-large.glb"), new Vector3(x + 4, 0, 18), Vector3.zero, Vector3.one, "_Scene/City/StreetFurniture");
        }

        for (int i = 0; i < 5; i++)
        {
            float z = -20 + i * 9;
            Add("city_tree_west_" + i, Suburban("tree-small.glb"), new Vector3(-42, 0, z), Vector3.zero, Vector3.one, "_Scene/City/StreetFurniture");
            Add("city_tree_east_" + i, Suburban("tree-small.glb"), new Vector3(42, 0, z), Vector3.zero, Vector3.one, "_Scene/City/StreetFurniture");
        }

        Add("city_billboard_auto_empire", Racing("billboard.glb"), new Vector3(31, 0, 17), new Vector3(0, -90, 0), Vector3.one, "_Scene/City/StreetFurniture");
        Add("city_highway_sign_service", Road("sign-highway-detailed.glb"), new Vector3(-30, 0, -21), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/StreetFurniture");

        Add("city_pedestrian_01", Character("character-d.glb"), new Vector3(-16, 0, 18), new Vector3(0, 90, 0), Vector3.one, "_Scene/City/StreetFurniture");
        Add("city_pedestrian_02", Character("character-e.glb"), new Vector3(12, 0, 18), new Vector3(0, -90, 0), Vector3.one, "_Scene/City/StreetFurniture");
        Add("city_pedestrian_03", Character("character-f.glb"), new Vector3(39, 0, 10), new Vector3(0, 180, 0), Vector3.one, "_Scene/City/StreetFurniture");
    }

    private static void BuildCoreGameplayObjects()
    {
        Add("parts_source_visual_box_01", Car("box.glb"), new Vector3(-11, 0, 7), new Vector3(0, 20, 0), Vector3.one, "_Scene/Gameplay");
        Add("parts_source_visual_crate_01", Platformer("crate.glb"), new Vector3(-13, 0, 7), new Vector3(0, -10, 0), Vector3.one, "_Scene/Gameplay");
        Zone("parts_source_zone__ResourceSource__BasicParts", new Vector3(-11.5f, 0.04f, 6.5f), 2.0f, Mat("ZoneBlue"), "Pickup Basic Parts");

        Add("repair_bay_01_floor", Racing("roadPitGarage.glb"), new Vector3(0, 0.03f, 1), Vector3.zero, Vector3.one, "_Scene/Gameplay");
        Add("repair_bay_01_barrier_l", Racing("barrierWhite.glb"), new Vector3(-4, 0, 1), Vector3.zero, Vector3.one, "_Scene/Gameplay");
        Add("repair_bay_01_barrier_r", Racing("barrierWhite.glb"), new Vector3(4, 0, 1), Vector3.zero, Vector3.one, "_Scene/Gameplay");
        Add("repair_bay_01_tool_bolt", Car("debris-bolt.glb"), new Vector3(-2, 0, 4), new Vector3(0, 35, 0), Vector3.one, "_Scene/Gameplay");
        Add("repair_bay_01_tool_nut", Car("debris-nut.glb"), new Vector3(2, 0, 4), new Vector3(0, -20, 0), Vector3.one, "_Scene/Gameplay");
        Zone("repair_bay_01_zone__Workstation__BasicRepairBay", new Vector3(0, 0.05f, 1), 2.25f, Mat("ZoneOrange"), "Basic Repair Bay");

        Add("payment_point_visual", Platformer("coin-gold.glb"), new Vector3(9, 1.0f, 2), Vector3.zero, new Vector3(1.4f, 1.4f, 1.4f), "_Scene/Gameplay");
        Zone("payment_point_zone__PaymentPoint__Cash", new Vector3(9, 0.05f, 2), 2.0f, Mat("ZoneGreen"), "Collect Payment");
    }

    private static void BuildUnlockStations()
    {
        Add("construction_queue_02_pad", Platformer("button-round.glb"), new Vector3(-2, 0, -8), Vector3.zero, Vector3.one, "_Scene/Gameplay");
        Zone("construction_queue_02__ConstructionZone__Cost60", new Vector3(-2, 0.04f, -8), 2.0f, Mat("ZoneYellow"), "Unlock Queue Slot");

        Add("construction_tire_station_pad", Platformer("button-round.glb"), new Vector3(-9, 0, -2), Vector3.zero, Vector3.one, "_Scene/Gameplay");
        Zone("construction_tire_station__ConstructionZone__Cost120", new Vector3(-9, 0.04f, -2), 2.2f, Mat("ZoneYellow"), "Unlock Tire Station");
        Add("tire_locked_barrier_01", Road("construction-barrier.glb"), new Vector3(-9, 0, -5), new Vector3(0, 90, 0), Vector3.one, "_Scene/Gameplay");
        Add("tire_locked_light_01", Road("construction-light.glb"), new Vector3(-12, 0, -5), Vector3.zero, Vector3.one, "_Scene/Gameplay");
        AddWheelStack("tire_preview_stack", new Vector3(-11, 0, 0), "wheel-default.glb", 3);
        Zone("tire_source_zone__ResourceSource__Tire", new Vector3(-11, 0.04f, 0), 1.8f, Mat("ZoneBlue"), "Tire Source");
        Zone("tire_station_zone__Workstation__TireStation", new Vector3(-7, 0.04f, -1), 2.0f, Mat("ZoneOrange"), "Tire Station");
        Add("tire_station_wheel_01", Car("wheel-racing.glb"), new Vector3(-7, 0.5f, -1), new Vector3(90, 0, 0), Vector3.one, "_Scene/Gameplay");

        Add("construction_oil_station_pad", Platformer("button-round.glb"), new Vector3(9, 0, -2), Vector3.zero, Vector3.one, "_Scene/Gameplay");
        Zone("construction_oil_station__ConstructionZone__Cost180", new Vector3(9, 0.04f, -2), 2.2f, Mat("ZoneYellow"), "Unlock Oil Station");
        Add("oil_source_barrel_01", Platformer("barrel.glb"), new Vector3(11, 0, 0), Vector3.zero, Vector3.one, "_Scene/Gameplay");
        Add("oil_source_barrel_02", Platformer("barrel.glb"), new Vector3(12.2f, 0, 0.2f), new Vector3(0, 20, 0), Vector3.one, "_Scene/Gameplay");
        Add("oil_source_tank", Industrial("detail-tank.glb"), new Vector3(12, 0, 2), new Vector3(0, 90, 0), Vector3.one, "_Scene/Gameplay");
        Zone("oil_source_zone__ResourceSource__OilCan", new Vector3(11.5f, 0.04f, 0), 1.8f, Mat("ZoneBlue"), "Oil Source");
        Zone("oil_station_zone__Workstation__OilStation", new Vector3(7, 0.04f, -1), 2.0f, Mat("ZoneOrange"), "Oil Station");

        Add("employee_hire_pad", Platformer("button-square.glb"), new Vector3(12, 0, 2), Vector3.zero, Vector3.one, "_Scene/Gameplay");
        Zone("employee_hire_pad__HireZone__TransporterCost250", new Vector3(12, 0.04f, 2), 1.6f, Mat("ZonePurple"), "Hire Transporter");
    }

    private static void BuildCharactersAndVehicles()
    {
        Add("player__PlayerCharacter", Character("character-a.glb"), new Vector3(-5, 0, -4), new Vector3(0, 45, 0), Vector3.one, "_Scene/Characters");
        Add("customer_waiting_01__CustomerAgent", Character("character-c.glb"), new Vector3(-10, 0, -10), new Vector3(0, 90, 0), Vector3.one, "_Scene/Characters");
        Add("employee_transporter_preview__Locked", Character("character-k.glb"), new Vector3(-12, 0, 4), Vector3.zero, Vector3.one, "_Scene/Characters");
        Add("cashier_preview__Locked", Character("character-m.glb"), new Vector3(11, 0, 5), new Vector3(0, 180, 0), Vector3.one, "_Scene/Characters");

        Add("vehicle_sedan_queue_01__CustomerVehicle", Car("sedan.glb"), new Vector3(-8, 0, -12), new Vector3(0, 90, 0), Vector3.one, "_Scene/Vehicles");
        Add("vehicle_suv_preview__FutureCustomerVehicle", Car("suv.glb"), new Vector3(16, 0, -12), new Vector3(0, 90, 0), Vector3.one, "_Scene/Vehicles");
        Add("vehicle_repair_demo__InService", Car("hatchback-sports.glb"), new Vector3(0, 0, 1), Vector3.zero, Vector3.one, "_Scene/Vehicles");
    }

    private static void BuildDecorations()
    {
        Add("front_cone_01", Car("cone.glb"), new Vector3(-5, 0, -8), Vector3.zero, Vector3.one, "_Scene/Decorations");
        Add("front_cone_02", Car("cone.glb"), new Vector3(5, 0, -8), Vector3.zero, Vector3.one, "_Scene/Decorations");
        Add("parts_delivery_van", Car("delivery-flat.glb"), new Vector3(-16, 0, 5), new Vector3(0, 90, 0), Vector3.one, "_Scene/Decorations");
        Add("fence_back_01", Suburban("fence.glb"), new Vector3(-12, 0, 13), new Vector3(0, 90, 0), Vector3.one, "_Scene/Decorations");
        Add("fence_back_02", Suburban("fence.glb"), new Vector3(-6, 0, 13), new Vector3(0, 90, 0), Vector3.one, "_Scene/Decorations");
        Add("tree_left_01", Suburban("tree-small.glb"), new Vector3(-20, 0, 6), Vector3.zero, Vector3.one, "_Scene/Decorations");
        Add("tree_right_01", Suburban("tree-large.glb"), new Vector3(19, 0, 7), Vector3.zero, Vector3.one, "_Scene/Decorations");
        Add("commercial_backdrop_01", Commercial("low-detail-building-wide-a.glb"), new Vector3(20, 0, 16), new Vector3(0, 225, 0), Vector3.one, "_Scene/Decorations");

        Label("PARTS", new Vector3(-11.5f, 2.6f, 6.5f), Color.white, 0.7f);
        Label("REPAIR BAY", new Vector3(0, 2.6f, 3.8f), Color.white, 0.65f);
        Label("PAY", new Vector3(9, 2.6f, 2), Color.white, 0.7f);
        Label("LOCKED: TIRES", new Vector3(-9, 2.6f, -2), Color.white, 0.55f);
        Label("LOCKED: OIL", new Vector3(9, 2.6f, -2), Color.white, 0.55f);
    }

    private static void BuildWaypoints()
    {
        Waypoint("veh_spawn", new Vector3(-24, 0.1f, -12));
        Waypoint("veh_queue_01", new Vector3(-8, 0.1f, -12));
        Waypoint("veh_queue_02_locked", new Vector3(-2, 0.1f, -12));
        Waypoint("veh_to_repair_01", new Vector3(-4, 0.1f, -6));
        Waypoint("veh_repair_01", new Vector3(0, 0.1f, 1));
        Waypoint("veh_to_payment", new Vector3(7, 0.1f, -5));
        Waypoint("veh_exit", new Vector3(24, 0.1f, -12));
    }

    private static void BuildCameraAndLighting()
    {
        var cameraObject = new GameObject("Main Camera");
        var camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(34, 42, -44);
        cameraObject.transform.rotation = Quaternion.Euler(55, -35, 0);
        camera.orthographic = true;
        camera.orthographicSize = 34;
        camera.clearFlags = CameraClearFlags.Skybox;

        var sunObject = new GameObject("Directional Light");
        var sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.intensity = 1.25f;
        sunObject.transform.rotation = Quaternion.Euler(50, -30, 0);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.65f, 0.68f, 0.72f);
    }

    private static void BuildSceneNotes()
    {
        var notes = new GameObject("README_SCENE_BLUEPRINT");
        notes.transform.SetParent(GameObject.Find("_Scene").transform, false);
        notes.AddComponent<SceneBlueprintNotes>().notes =
            "Roadside Repair Garage and little surrounding city generated from automotive_idle_scene_asset_catalog.md. " +
            "Colored discs are gameplay trigger zones: blue pickup, orange workstation, green payment, yellow construction, purple hiring. " +
            "Waypoint spheres show vehicle navigation. The _Scene/City hierarchy contains commercial, suburban, industrial, traffic, parking, and street-furniture dressing. " +
            "Replace prototype labels/zones with production UI and scripts.";
    }

    private static void AddWheelStack(string baseName, Vector3 basePosition, string file, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Add(baseName + "_" + (i + 1), Car(file), basePosition + new Vector3(0, i * 0.6f, 0), new Vector3(90, 0, 0), Vector3.one, "_Scene/Gameplay");
        }
    }

    private static void Zone(string name, Vector3 position, float radius, Material material, string label)
    {
        var zone = Primitive(name, PrimitiveType.Cylinder, position, new Vector3(radius * 2, 0.04f, radius * 2), material, "_Scene/Gameplay/Zones");
        var collider = zone.GetComponent<Collider>();
        if (collider != null) collider.isTrigger = true;
        Label(label, position + new Vector3(0, 0.8f, 0), Color.white, 0.35f);
    }

    private static void Waypoint(string name, Vector3 position)
    {
        Primitive(name, PrimitiveType.Sphere, position, new Vector3(0.45f, 0.45f, 0.45f), Mat("Waypoint"), "_Scene/Gameplay/Waypoints");
        Label(name, position + new Vector3(0, 0.6f, 0), Color.white, 0.25f);
    }

    private static GameObject Add(string name, string assetPath, Vector3 position, Vector3 rotation, Vector3 scale, string parentPath)
    {
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        GameObject instance;
        if (asset == null)
        {
            instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
            instance.name = name + "__MISSING_ASSET";
            instance.GetComponent<Renderer>().sharedMaterial = Mat("Missing");
            Debug.LogWarning("Missing asset: " + assetPath);
        }
        else
        {
            instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            instance.name = name;
        }

        instance.transform.position = position;
        instance.transform.rotation = Quaternion.Euler(rotation);
        instance.transform.localScale = scale;
        SetParent(instance, parentPath);
        AddBoxColliderIfMissing(instance);
        return instance;
    }

    private static GameObject Primitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material, string parentPath)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.GetComponent<Renderer>().sharedMaterial = material;
        SetParent(obj, parentPath);
        return obj;
    }

    private static void Label(string text, Vector3 position, Color color, float size)
    {
        var obj = new GameObject("label_" + text.Replace(" ", "_").Replace(":", ""));
        obj.transform.position = position;
        obj.transform.rotation = Quaternion.Euler(60, -35, 0);
        SetParent(obj, "_Scene/Labels");
        var mesh = obj.AddComponent<TextMesh>();
        mesh.text = text;
        mesh.anchor = TextAnchor.MiddleCenter;
        mesh.alignment = TextAlignment.Center;
        mesh.characterSize = size;
        mesh.fontSize = 64;
        mesh.color = color;
    }

    private static void AddBoxColliderIfMissing(GameObject obj)
    {
        if (obj.GetComponentInChildren<Collider>() != null) return;
        obj.AddComponent<BoxCollider>();
    }

    private static void SetParent(GameObject obj, string parentPath)
    {
        var parent = GameObject.Find(parentPath);
        if (parent != null) obj.transform.SetParent(parent.transform, true);
    }

    private static void CreateMaterials()
    {
        EnsureFolder("Assets/Generated");
        MakeMat("Floor", new Color(0.48f, 0.50f, 0.48f, 1));
        MakeMat("Asphalt", new Color(0.12f, 0.13f, 0.14f, 1));
        MakeMat("Grass", new Color(0.34f, 0.55f, 0.28f, 1));
        MakeMat("Sidewalk", new Color(0.56f, 0.58f, 0.56f, 1));
        MakeMat("ParkingPaint", new Color(0.96f, 0.94f, 0.82f, 1));
        MakeMat("ZoneBlue", new Color(0.12f, 0.5f, 1f, 0.35f), true);
        MakeMat("ZoneOrange", new Color(1f, 0.47f, 0.12f, 0.35f), true);
        MakeMat("ZoneGreen", new Color(0.15f, 0.85f, 0.35f, 0.35f), true);
        MakeMat("ZoneYellow", new Color(1f, 0.82f, 0.1f, 0.35f), true);
        MakeMat("ZonePurple", new Color(0.55f, 0.25f, 1f, 0.35f), true);
        MakeMat("TransparentBounds", new Color(1f, 1f, 1f, 0.04f), true);
        MakeMat("Waypoint", new Color(0.1f, 0.9f, 1f, 0.8f), true);
        MakeMat("Missing", new Color(1f, 0f, 0.7f, 1));
    }

    private static void MakeMat(string name, Color color, bool transparent = false)
    {
        var path = "Assets/Generated/" + name + ".mat";
        var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(mat, path);
        }

        mat.color = color;
        if (transparent)
        {
            mat.SetFloat("_Mode", 3);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }

        Materials[name] = mat;
    }

    private static Material Mat(string name) => Materials[name];

    private static string Fbx(string file) => file.Replace(".glb", ".fbx");
    private static string Character(string file) => Kenney + "kenney_blocky-characters_20/Models/FBX format/" + Fbx(file);
    private static string Car(string file) => Kenney + "kenney_car-kit/Models/FBX format/" + Fbx(file);
    private static string Road(string file) => Kenney + "kenney_city-kit-roads/Models/FBX format/" + Fbx(file);
    private static string Racing(string file) => Kenney + "kenney_racing-kit/Models/FBX format/" + Fbx(file);
    private static string Platformer(string file) => Kenney + "kenney_platformer-kit/Models/FBX format/" + Fbx(file);
    private static string Suburban(string file) => Kenney + "kenney_city-kit-suburban_20/Models/FBX format/" + Fbx(file);
    private static string Industrial(string file) => Kenney + "kenney_city-kit-industrial_1/Models/FBX format/" + Fbx(file);
    private static string Commercial(string file) => Kenney + "kenney_city-kit-commercial_2/Models/FBX format/" + Fbx(file);

    private static void EnsureFolder(string path)
    {
        var parts = path.Split('/');
        var current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            var next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}

public sealed class SceneBlueprintNotes : MonoBehaviour
{
    [TextArea(4, 12)]
    public string notes;
}
