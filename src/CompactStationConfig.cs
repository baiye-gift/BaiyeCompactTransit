using System.Collections.Generic;
using TUNING;
using UnityEngine;

namespace Baiye.CompactTransit
{
    public sealed class CompactStationConfig : IBuildingConfig
    {
        public const string ID = "BaiyeCompactTransitStation";
        public const string Anim = "baiye_compact_transit_kanim";
        public override BuildingDef CreateBuildingDef()
        {
            var def = BuildingTemplates.CreateBuildingDef(ID, 1, 2, Anim, 100, 120f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER5, MATERIALS.REFINED_METALS, 1600f,
                BuildLocationRule.Anywhere, BUILDINGS.DECOR.PENALTY.TIER1, NOISE_POLLUTION.NONE);
            def.Overheatable = false;
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 960f;
            def.Entombable = true;
            def.AudioCategory = "Metal";
            def.PowerInputOffset = new CellOffset(0, 0);
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 1));
            def.UtilityInputOffset = new CellOffset(0, -1);
            def.UtilityOutputOffset = new CellOffset(0, 2);
            def.AddSearchTerms(STRINGS.SEARCH_TERMS.TRANSPORT);
            return def;
        }
        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            BuildingConfigManager.Instance.IgnoreDefaultKComponent(typeof(RequiresFoundation), prefab_tag);
            var entrance = go.AddOrGet<TravelTubeEntrance>();
            entrance.joulesPerLaunch = 10000f; entrance.jouleCapacity = 40000f; entrance.waxPerLaunch = 0.05f;
            var storage = go.AddOrGet<Storage>(); storage.capacityKg = 10f;
            storage.SetDefaultStoredItemModifiers(new List<Storage.StoredItemModifier> {
                Storage.StoredItemModifier.Hide, Storage.StoredItemModifier.Seal,
                Storage.StoredItemModifier.Insulate, Storage.StoredItemModifier.Preserve });
            var delivery = go.AddOrGet<ManualDeliveryKG>();
            delivery.RequestedItemTag = SimHashes.MilkFat.CreateTag();
            delivery.choreTypeIDHash = Db.Get().ChoreTypes.Fetch.IdHash;
            delivery.capacity = storage.capacityKg; delivery.refillMass = entrance.waxPerLaunch; delivery.SetStorage(storage);
            go.AddOrGet<TravelTubeEntrance.Work>();
            go.AddOrGet<LogicOperationalController>();
            go.AddOrGet<EnergyConsumerSelfSustaining>();
            go.AddOrGet<CompactStation>();
            go.AddOrGet<StationStatus>();
        }
        public override void DoPostConfigureComplete(GameObject go)
        {
            go.GetComponent<RequireInputs>().visualizeRequirements = RequireInputs.Requirements.NoWire;
            StationTubeConnections.ConfigureVisualizer(go.AddOrGet<KAnimGraphTileVisualizer>());
        }
    }
}
