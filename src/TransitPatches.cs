using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace Baiye.CompactTransit
{
    [HarmonyPatch]
    public static class CompactNavigationTransitions
    {
        static MethodBase TargetMethod() => AccessTools.GetDeclaredConstructors(typeof(NavGrid))
            .Single(c => c.GetParameters().Length == 8 && c.GetParameters()[0].ParameterType == typeof(string));
        static void Prefix(string id, ref NavGrid.Transition[] transitions, ref int max_links_per_cell)
        {
            if (id != "MinionNavGrid") return;
            transitions = AddEntry(transitions);
            max_links_per_cell += 2;
        }
        public static NavGrid.Transition[] AddEntry(NavGrid.Transition[] transitions)
        {
            if (transitions.Any(StationNavigation.IsCompactEntry)) return transitions;
            if (transitions.Length >= 256) throw new InvalidOperationException("Compact Transit: native navigation transition IDs are exhausted.");
            var entry = new NavGrid.Transition(NavType.Floor, NavType.Tube, 0, 1, NavAxis.NA,
                false, false, false, 40, "floor_tube_0_2", Array.Empty<CellOffset>(), Array.Empty<CellOffset>(),
                StationNavigation.EntryMarker, Array.Empty<NavOffset>());
            return transitions.Concat(new[] { entry }).ToArray();
        }
    }

    [HarmonyPatch(typeof(NavGrid.Transition), nameof(NavGrid.Transition.IsValid))]
    public static class CompactNavigationValidation
    {
        static bool Prefix(NavGrid.Transition __instance, int cell, NavTable nav_table, ref int __result)
        {
            if (StationNavigation.TryResolve(__instance, cell, nav_table, out int destination))
            { __result = destination; return false; }
            // This added transition is only valid for our station. All original
            // transitions on ordinary tubes and entrances run the original code.
            if (StationNavigation.IsCompactEntry(__instance)) { __result = Grid.InvalidCell; return false; }
            return true;
        }
    }

    [HarmonyPatch(typeof(TravelTubeEntrance), "TubeConnectionsChanged", new[] { typeof(UtilityConnections) })]
    internal static class CompactConnectionFlag
    {
        static void Prefix(TravelTubeEntrance __instance, ref UtilityConnections connections)
        {
            var station = __instance.GetComponent<CompactStation>();
            if (station != null) connections = station.ReadConnections() ? UtilityConnections.Up : 0;
        }
    }

    [HarmonyPatch(typeof(TravelTubeEntrance.Work), "OnPrefabInit")]
    internal static class CompactWorkerAnchor
    {
        static void Postfix(TravelTubeEntrance.Work __instance)
        {
            if (__instance.GetComponent<CompactStation>() != null) __instance.SetOffsets(new[] { new CellOffset(0, 0) });
        }
    }

    [HarmonyPatch(typeof(TravelTubeEntrance), nameof(TravelTubeEntrance.GetLaunchAnimName))]
    internal static class CompactLaunchAnimation
    {
        static void Postfix(TravelTubeEntrance.SMInstance smi, ref string __result)
        {
            var station = smi.master.GetComponent<CompactStation>();
            if (station == null) return;
            var worker = smi.GetComponent<TravelTubeEntrance.Work>().worker;
            var navigator = worker != null ? worker.GetComponent<Navigator>() : null;
            __result = Select(__result, Grid.PosToCell(station), Grid.WidthInCells, navigator?.path.nodes);
        }

        // Read the already selected native route, never choose a destination.
        // Missing/cancelled routes use the neutral fallback animation.
        internal static string Select(string original, int bottom, int width, IList<PathFinder.Path.Node> nodes)
        {
            if (nodes == null) return original;
            bool inCabin = false;
            foreach (var node in nodes)
            {
                if (node.navType != NavType.Tube) { if (inCabin) break; continue; }
                if (node.cell == bottom || node.cell == bottom + width) { inCabin = true; continue; }
                if (!inCabin) continue;
                if (node.cell == bottom + 2 * width) return original + "_up";
                if (node.cell == bottom - width) return original + "_down";
                break;
            }
            return original;
        }
    }

    [HarmonyPatch(typeof(Game), "OnCleanUp")]
    internal static class CompactRegistryCleanup
    {
        static void Postfix() => StationRegistry.Clear();
    }

    [HarmonyPatch(typeof(UtilityNetworkTubesManager), nameof(UtilityNetworkTubesManager.SetConnections))]
    public static class CompactVerticalSockets
    {
        static void Prefix(int cell, ref UtilityConnections connections)
        {
            connections = StationTubeConnections.Filter(cell, connections);
            connections = StationTubeConnections.IncludeStationSocket(cell, connections);
        }
    }

    [HarmonyPatch(typeof(UtilityNetworkTubesManager), nameof(UtilityNetworkTubesManager.CanAddConnection))]
    public static class CompactVerticalPlacement
    {
        static bool Prefix(UtilityConnections new_connection, int cell, ref string fail_reason, ref bool __result)
        {
            if (StationTubeConnections.Filter(cell, new_connection) == new_connection) return true;
            fail_reason = TransitStrings.Text("本站管口朝上、下；左右开口用于步行。", "Station tube ports face up/down; side openings are for walking.");
            __result = false;
            return false;
        }
    }
}
