using HarmonyLib;
using KMod;

namespace Baiye.CompactTransit
{
    public sealed class CompactTransitMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            TransitStrings.Register();
        }
    }

    [HarmonyPatch(typeof(Db), nameof(Db.Initialize))]
    internal static class TransitRegistration
    {
        private static void Postfix()
        {
            ModUtil.AddBuildingToPlanScreen((HashedString)"Base", CompactStationConfig.ID);
            var tech = Db.Get().Techs.Get("TravelTubes");
            if (tech != null && !tech.unlockedItemIDs.Contains(CompactStationConfig.ID)) tech.unlockedItemIDs.Add(CompactStationConfig.ID);
        }
    }
}
