using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Baiye.CompactTransit;
using HarmonyLib;
using System.Linq;

static class Program
{
    static int passed;
    static int B = 8 * 20 + 8;
    static NavTable table;
    static void Main(string[] args)
    {
        // The shipped emitter references ILGenerator.MarkSequencePoint removed
        // in .NET 8. Use its supported Cecil backend for this test process only.
        AppContext.SetData("MonoMod.DMDType", "cecil");
        string managed = args.Length > 0 ? Path.GetFullPath(args[0]) : @"D:\Steam\steamapps\common\OxygenNotIncluded\OxygenNotIncluded_Data\Managed";
        AppDomain.CurrentDomain.AssemblyResolve += (_, e) => {
            string path = Path.Combine(managed, new AssemblyName(e.Name).Name + ".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        try { Run(); }
        catch (Exception error) {
            Console.Error.WriteLine(error);
            if(error is ReflectionTypeLoadException load) foreach(var cause in load.LoaderExceptions) Console.Error.WriteLine(cause);
            Environment.ExitCode = 1;
        }
    }
    static void Assert(bool ok, string message) { if (!ok) throw new Exception(message); }
    static void Test(string name, System.Action action) { Fixture(); action(); passed++; Console.WriteLine("PASS " + name); }
    static void Fixture(bool top = true, bool bottom = true)
    {
        Grid.WidthInCells = 20; Grid.HeightInCells = 20; Grid.CellCount = 400;
        Grid.BuildMasks = new Grid.BuildFlags[400]; Grid.NavMasks = new Grid.NavFlags[400];
        Grid.NavValidatorMasks = new Grid.NavValidatorFlags[400];
        Grid.WorldIdx = new byte[400];
        Grid.ObjectLayers = new Dictionary<int, UnityEngine.GameObject>[(int)ObjectLayer.NumLayers];
        for(int layer=0;layer<Grid.ObjectLayers.Length;layer++) Grid.ObjectLayers[layer]=new Dictionary<int,UnityEngine.GameObject>();
        // Real derived tube manager invokes native Pathfinding.AddDirtyNavGridCell.
        // An empty native grid list lets that method run without a Unity world.
        Pathfinding.Instance=(Pathfinding)RuntimeHelpers.GetUninitializedObject(typeof(Pathfinding));
        AccessTools.Field(typeof(Pathfinding),"NavGrids").SetValue(Pathfinding.Instance,new List<NavGrid>());
        table = new NavTable(400, "MinionNavGrid");
        StationRegistry.Clear(); StationRegistry.Publish(new StationTopology(B, 20, top, bottom));
        for (int y = -2; y <= 3; y++) table.SetValid(B + y * 20, NavType.Tube, true);
        table.SetValid(B, NavType.Floor, true);
        table.SetValid(B-1, NavType.Floor, true); table.SetValid(B+1, NavType.Floor, true);
    }
    static NavGrid.Transition T(NavType from, NavType to, int x, int y) => new NavGrid.Transition(from, to, x, y,
        NavAxis.NA, false, false, false, 5, "", Array.Empty<CellOffset>(), Array.Empty<CellOffset>(), Array.Empty<NavOffset>(), Array.Empty<NavOffset>());
    static int Resolve(NavType from, NavType to, int cell, int x, int y)
    {
        Assert(StationNavigation.TryResolve(T(from, to, x, y), cell, table, out int result), "compact station transition was not handled");
        return result;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    static void Run()
    {
        Test("completed station exposes the native pipe build-tool interface", () => {
            // Unity GameObjects cannot be constructed in this host. Inspect the
            // actual prefab hook, then exercise its adapter below on the real
            // game component rather than claiming a full mouse/Unity test.
            var hook=AccessTools.Method(typeof(CompactStationConfig),nameof(CompactStationConfig.DoPostConfigureComplete));
            var calls=PatchProcessor.GetOriginalInstructions(hook).Select(i=>i.operand).OfType<MethodInfo>();
            Assert(calls.Any(m=>m.IsGenericMethod && m.GetGenericArguments().Contains(typeof(KAnimGraphTileVisualizer))),
                "completed station occupies tube tiles without the native build-tool interface");
            Assert(calls.Any(m=>m==AccessTools.Method(typeof(StationTubeConnections),"ConfigureVisualizer")),
                "native station adapter is not configured by the completed prefab");
        });
        Test("native station adapter retains connection writes without owning art or cleanup", () => {
            var graph=(KAnimGraphTileVisualizer)RuntimeHelpers.GetUninitializedObject(typeof(KAnimGraphTileVisualizer));
            var configure=AccessTools.Method(typeof(StationTubeConnections),"ConfigureVisualizer");
            Assert(configure!=null,"missing station adapter setup");
            configure.Invoke(null,new object[]{graph});
            Assert(graph.connectionSource==KAnimGraphTileVisualizer.ConnectionSource.Tube && graph.isPhysicalBuilding,
                "station adapter uses the wrong utility system");
            Assert(graph.Connections==(UtilityConnections.Up|UtilityConnections.Down),"station adapter loses a vertical socket");
            Assert(graph.skipRefresh && graph.skipCleanup,"station adapter can replace art or duplicate network cleanup");
            IUtilityItem item=graph;
            item.UpdateConnections(UtilityConnections.Up|UtilityConnections.Down);
            Assert(item.Connections==(UtilityConnections.Up|UtilityConnections.Down),"native build-tool write failed");
            graph.connectionManager=new UtilityNetworkTubesManager(20,20,(int)ObjectLayer.TravelTubeTile);
            StationTubeConnections.Connect((UtilityNetworkManager<TravelTubeNetwork,TravelTube>)graph.connectionManager,B);
            var beforeBottom=graph.connectionManager.GetConnections(B,true);
            var beforeUpper=graph.connectionManager.GetConnections(B+20,true);
            Assert(beforeBottom!=0 && beforeUpper!=0,"adapter cleanup fixture has no station nodes");
            graph.Refresh();
            AccessTools.Method(typeof(KAnimGraphTileVisualizer),"OnCleanUp").Invoke(graph,null);
            Assert(graph.connectionManager.GetConnections(B,true)==beforeBottom && graph.connectionManager.GetConnections(B+20,true)==beforeUpper,
                "adapter cleanup removed station-owned nodes");
            var ordinary=(KAnimGraphTileVisualizer)RuntimeHelpers.GetUninitializedObject(typeof(KAnimGraphTileVisualizer));
            Assert(!ordinary.skipRefresh && !ordinary.skipCleanup,"ordinary pipe behavior changed");
        });
        Test("floating platform supports a duplicant without an external floor", () => {
            Assert(!GameNavGrids.FloorValidator.IsWalkableCell(B,B-20,true),"fixture unexpectedly has a floor");
            var support=new PlatformSupport(); support.Acquire(B);
            Assert(GameNavGrids.FloorValidator.IsWalkableCell(B,B-20,true),"floating pad still needs external ground");
            Assert(!Grid.Solid[B-20],"platform turned the tube cell into a solid tile");
            support.Release();
            Assert(!GameNavGrids.FloorValidator.IsWalkableCell(B,B-20,true),"demolished station left a ghost floor");
        });
        Test("enter the same small station from a normal floor", () => Assert(Resolve(NavType.Floor, NavType.Tube, B, 0, 1) == B + 20, "floor must enter the upper cabin cell"));
        Test("platform acquire and cleanup are idempotent and preserve shared support", () => {
            Grid.FakeFloor.Add(B-20);
            var support=new PlatformSupport(); support.Acquire(B); support.Acquire(B);
            support.Release(); support.Release();
            Assert(Grid.FakeFloor[B-20],"cleanup stole another object's support");
            Grid.FakeFloor.Remove(B-20);
            Assert(!Grid.FakeFloor[B-20],"duplicate acquire leaked support");
        });
        Test("restored platform recreates its native floor after reload", () => {
            var support=new PlatformSupport(); support.Acquire(B); support.Release();
            var restored=new PlatformSupport(); restored.Acquire(B);
            Assert(GameNavGrids.FloorValidator.IsWalkableCell(B,B-20,true),"restored platform has no floor");
            restored.Release();
        });
        Test("native tube sockets join a straight shaft through the station", () => {
            var manager=new UtilityNetworkTubesManager(20,20,(int)ObjectLayer.TravelTubeTile);
            manager.SetConnections(UtilityConnections.Up|UtilityConnections.Down,B-20,true);
            manager.SetConnections(UtilityConnections.Up|UtilityConnections.Down,B+40,true);
            StationTubeConnections.Connect(manager,B);
            Assert((manager.GetConnections(B-20,true)&UtilityConnections.Up)!=0,"lower pipe does not attach to station");
            Assert((manager.GetConnections(B+40,true)&UtilityConnections.Down)!=0,"upper pipe does not attach to station");
            Assert(manager.GetConnections(B,true)==(UtilityConnections.Up|UtilityConnections.Down),"lower cabin is not a straight native socket");
            Assert(manager.GetConnections(B+20,true)==(UtilityConnections.Up|UtilityConnections.Down),"upper cabin is not a straight native socket");
            StationTubeConnections.Disconnect(manager,B);
            Assert((manager.GetConnections(B-20,true)&UtilityConnections.Up)==0,"demolition left lower socket attached");
            Assert((manager.GetConnections(B+40,true)&UtilityConnections.Down)==0,"demolition left upper socket attached");
        });
        Test("late restored pipe sockets remain visually continuous", () => {
            var manager=new UtilityNetworkTubesManager(20,20,(int)ObjectLayer.TravelTubeTile);
            var harmony=new Harmony("baiye.tests.compact.restoredvisual");
            harmony.CreateClassProcessor(typeof(CompactVerticalSockets)).Patch();
            try {
                StationTubeConnections.Connect(manager,B);
                manager.SetConnections(UtilityConnections.Up,B+40,true);
                manager.SetConnections(UtilityConnections.Down,B-20,true);
                Assert(manager.GetVisualizerString(B+40)=="UD","late upper pipe retained an endpoint cap");
                Assert(manager.GetVisualizerString(B-20)=="UD","late lower pipe retained an endpoint cap");
                manager.SetConnections(UtilityConnections.Up,B+80,true);
                Assert(manager.GetVisualizerString(B+80)=="U","ordinary pipe display was altered");
                StationTubeConnections.Disconnect(manager,B); StationRegistry.Clear();
                manager.SetConnections(UtilityConnections.Up,B+40,true);
                Assert(manager.GetVisualizerString(B+40)=="U","removed station left a ghost visual socket");
            } finally { harmony.UnpatchAll(harmony.Id); }
        });
        Test("pipes constructed after the station connect without a bend", () => {
            var manager=new UtilityNetworkTubesManager(20,20,(int)ObjectLayer.TravelTubeTile);
            StationTubeConnections.Connect(manager,B);
            manager.SetConnections(UtilityConnections.Up|UtilityConnections.Down,B+40,true);
            Assert((manager.GetConnections(B+20,true)&UtilityConnections.Up)!=0,"late top pipe not connected");
            manager.SetConnections(UtilityConnections.Up|UtilityConnections.Down,B-20,true);
            Assert((manager.GetConnections(B,true)&UtilityConnections.Down)!=0,"late bottom pipe not connected");
            manager.ClearCell(B+40,true);
            Assert((manager.GetConnections(B+20,true)&UtilityConnections.Up)==0,"removed top pipe remains attached");
            manager.SetConnections(UtilityConnections.Up|UtilityConnections.Down,B+40,true);
            Assert((manager.GetConnections(B+20,true)&UtilityConnections.Up)!=0,"repaired top pipe not connected");
        });
        Test("later side pipes cannot create a false horizontal station socket", () => {
            var manager=new UtilityNetworkTubesManager(20,20,(int)ObjectLayer.TravelTubeTile);
            var harmony=new Harmony("baiye.tests.compact.verticalsockets");
            harmony.CreateClassProcessor(typeof(CompactVerticalSockets)).Patch();
            harmony.CreateClassProcessor(typeof(CompactVerticalPlacement)).Patch();
            try {
                StationTubeConnections.Connect(manager,B);
                manager.SetConnections(UtilityConnections.Left,B+1,true);
                manager.SetConnections(UtilityConnections.Right,B+19,true);
                Assert((manager.GetConnections(B,true)&UtilityConnections.Right)==0,"right pipe polluted cabin");
                Assert((manager.GetConnections(B+20,true)&UtilityConnections.Left)==0,"left pipe polluted upper cabin");
                Assert(manager.GetConnections(B+1,true)==0,"right side falsely connected");
                Assert(!manager.CanAddConnection(UtilityConnections.Left,B+1,true,out var reason)&&!string.IsNullOrWhiteSpace(reason),"side placement was accepted");
                Assert(manager.CanAddConnection(UtilityConnections.Down,B+40,true,out _),"normal vertical placement was rejected");
            } finally { harmony.UnpatchAll(harmony.Id); }
        });
        Test("no reachable side prevents disembarking but retains through travel", () => {
            table.SetValid(B-1,NavType.Floor,false); table.SetValid(B+1,NavType.Floor,false);
            Assert(Resolve(NavType.Tube,NavType.Floor,B+20,0,-1)==-1,"isolated platform accepts disembarking");
            Assert(Resolve(NavType.Tube,NavType.Tube,B+20,0,1)==B+40,"isolated stop blocks through travel");
        });
        Test("a single clear side is sufficient to leave the platform", () => {
            Grid.BuildMasks[B-1]=Grid.BuildFlags.Solid;
            Assert(Resolve(NavType.Tube,NavType.Floor,B+20,0,-1)==B,"clear right side cannot receive");
            Grid.BuildMasks[B+21]=Grid.BuildFlags.DupeImpassable;
            Assert(Resolve(NavType.Tube,NavType.Floor,B+20,0,-1)==-1,"blocked right headroom accepts exit");
        });
        Test("upper terminal supports entry and exit", () => {
            Fixture(false, true);
            Assert(Resolve(NavType.Floor, NavType.Tube, B, 0, 1) == B + 20, "bottom-only station cannot launch");
            Assert(Resolve(NavType.Tube, NavType.Floor, B + 20, 0, -1) == B, "bottom-only station cannot receive");
        });
        Test("pass through a middle station in both directions", () => {
            for(int y=-1;y<2;y++) Assert(Resolve(NavType.Tube, NavType.Tube, B+y*20,0,1)==B+(y+1)*20,"upward through route missing");
            for(int y=2;y> -1;y--) Assert(Resolve(NavType.Tube, NavType.Tube, B+y*20,0,-1)==B+(y-1)*20,"downward through route missing");
        });
        Test("disconnected station cannot launch", () => { Fixture(false,false); Assert(Resolve(NavType.Floor,NavType.Tube,B,0,1)==-1,"disconnected launch accepted"); });
        Test("removed top connection cannot be traversed", () => { Fixture(false,true); Assert(Resolve(NavType.Tube,NavType.Tube,B+20,0,1)==-1,"top gap crossed"); });
        Test("removed bottom connection cannot be traversed", () => { Fixture(true,false); Assert(Resolve(NavType.Tube,NavType.Tube,B,0,-1)==-1,"bottom gap crossed"); });
        Test("blocked landing cannot receive", () => { table.SetValid(B,NavType.Floor,false); Assert(Resolve(NavType.Tube,NavType.Floor,B+20,0,-1)==-1,"blocked exit accepted"); });
        Test("headroom blocker prevents cabin launch", () => { Grid.BuildMasks[B+20]=Grid.BuildFlags.Solid; Assert(Resolve(NavType.Floor,NavType.Tube,B,0,1)==-1,"buried cabin launch accepted"); });
        Test("station is not an unpowered sideways tube exit", () => { table.SetValid(B+1,NavType.Floor,true); Assert(Resolve(NavType.Tube,NavType.Floor,B,1,0)==-1,"uncontrolled ejection accepted"); });
        Test("ordinary floor is not granted compact entry", () => Assert(!StationNavigation.TryResolve(T(NavType.Floor,NavType.Tube,0,1),B+3,table,out _),"normal floor changed"));
        Test("walking through the station still uses native navigation", () => Assert(!StationNavigation.TryResolve(T(NavType.Floor,NavType.Floor,1,0),B,table,out _),"normal walking changed"));
        Test("no horizontal shortcut through cabin", () => Assert(Resolve(NavType.Tube,NavType.Tube,B,1,0)==-1,"sideways shortcut accepted"));
        Test("no two-cell tube jump bypasses the internal cabin", () => Assert(Resolve(NavType.Tube,NavType.Tube,B,0,2)==-1,"unregistered jump accepted"));
        Test("empty charge has no reservable native launch", () => {
            Grid.RegisterTubeEntrance(B,0); Grid.SetTubeEntranceOperational(B,true);
            Assert(!Grid.HasUsableTubeEntrance(B,1),"empty charge can launch"); Grid.UnregisterTubeEntrance(B);
        });
        Test("native launch slots preserve individual reservations", () => {
            Grid.RegisterTubeEntrance(B,2); Grid.SetTubeEntranceOperational(B,true);
            Assert(Grid.ReserveTubeEntrance(B,1,true)&&Grid.ReserveTubeEntrance(B,2,true),"two charges not reserved");
            Assert(!Grid.HasUsableTubeEntrance(B,3)&&Grid.HasUsableTubeEntrance(B,1),"reservation overbooking");
            Grid.ReserveTubeEntrance(B,1,false); Assert(Grid.HasUsableTubeEntrance(B,3),"cancellation did not release slot");
            Grid.UnregisterTubeEntrance(B);
        });
        Test("automation disables entry without cutting the tube route", () => {
            Grid.RegisterTubeEntrance(B,4); Grid.SetTubeEntranceOperational(B,false);
            Assert(!Grid.HasUsableTubeEntrance(B,1),"disabled station accepts launch");
            Assert(Resolve(NavType.Tube,NavType.Tube,B,0,1)==B+20,"disabled station cuts route");
            Assert(Resolve(NavType.Tube,NavType.Floor,B+20,0,-1)==B,"disabled station blocks exit");
            Grid.UnregisterTubeEntrance(B);
        });
        Test("launch indicators follow the selected native route", () => {
            var nodes = new List<PathFinder.Path.Node> {
                new PathFinder.Path.Node { cell=B, navType=NavType.Floor },
                new PathFinder.Path.Node { cell=B+20, navType=NavType.Tube },
                new PathFinder.Path.Node { cell=B+40, navType=NavType.Tube } };
            Assert(CompactLaunchAnimation.Select("working_pre",B,20,nodes)=="working_pre_up","up indicator missing");
            nodes[2]=new PathFinder.Path.Node { cell=B, navType=NavType.Tube };
            nodes.Add(new PathFinder.Path.Node { cell=B-20, navType=NavType.Tube });
            Assert(CompactLaunchAnimation.Select("wax",B,20,nodes)=="wax_down","wax/down indicator missing");
        });
        Test("missing or cancelled routes keep the animation fallback", () => {
            Assert(CompactLaunchAnimation.Select("working_pre",B,20,null)=="working_pre","missing route changed");
            var nodes=new List<PathFinder.Path.Node> { new PathFinder.Path.Node { cell=B+20,navType=NavType.Tube }, new PathFinder.Path.Node { cell=B,navType=NavType.Floor } };
            Assert(CompactLaunchAnimation.Select("wax",B,20,nodes)=="wax","cancelled route guessed a direction");
        });
        Test("all production Harmony targets bind to the installed game", () => {
            var harmony = new Harmony("baiye.tests.compact.binding");
            int targets = 0;
            try {
                foreach(var type in typeof(CompactTransitMod).Assembly.GetTypes().Where(t=>t.GetCustomAttributes(typeof(HarmonyPatch),false).Length>0))
                    targets += harmony.CreateClassProcessor(type).Patch().Count;
                Assert(targets >= 9, "missing production patch targets");
            } finally { harmony.UnpatchAll(harmony.Id); }
        });
        Test("production patch admits the compact entry on native Transition.IsValid", () => {
            var harmony = new Harmony("baiye.tests.compact.navigation");
            harmony.CreateClassProcessor(typeof(CompactNavigationValidation)).Patch();
            try {
                var entries = CompactNavigationTransitions.AddEntry(Array.Empty<NavGrid.Transition>());
                Assert(entries.Length==1 && entries[0].IsValid(B,table)==B+20,"native patch rejected compact entry");
                Assert(entries[0].IsValid(B+3,table)==-1,"native patch enabled plain-floor entry");
                Assert(T(NavType.Tube,NavType.Floor,0,-1).IsValid(B+20,table)==B,"native patch rejected compact exit");
            } finally { harmony.UnpatchAll(harmony.Id); }
        });
        Test("native floor walking reaches the launch pad from both sides", () => {
            table.SetValid(B-1,NavType.Floor,true); table.SetValid(B+1,NavType.Floor,true);
            var harmony=new Harmony("baiye.tests.compact.sideentry");
            harmony.CreateClassProcessor(typeof(CompactNavigationValidation)).Patch();
            try {
                Assert(T(NavType.Floor,NavType.Floor,1,0).IsValid(B-1,table)==B,"left walkway cannot reach pad");
                Assert(T(NavType.Floor,NavType.Floor,-1,0).IsValid(B+1,table)==B,"right walkway cannot reach pad");
                Assert(CompactNavigationTransitions.AddEntry(Array.Empty<NavGrid.Transition>())[0].IsValid(B,table)==B+20,"walkway pad cannot enter tube");
            } finally { harmony.UnpatchAll(harmony.Id); }
        });
        Test("native floor walking leaves the receiving pad to both sides", () => {
            table.SetValid(B-1,NavType.Floor,true); table.SetValid(B+1,NavType.Floor,true);
            var harmony=new Harmony("baiye.tests.compact.sideexit");
            harmony.CreateClassProcessor(typeof(CompactNavigationValidation)).Patch();
            try {
                Assert(T(NavType.Tube,NavType.Floor,0,-1).IsValid(B+20,table)==B,"tube cannot reach receiving pad");
                Assert(T(NavType.Floor,NavType.Floor,-1,0).IsValid(B,table)==B-1,"pad cannot exit left");
                Assert(T(NavType.Floor,NavType.Floor,1,0).IsValid(B,table)==B+1,"pad cannot exit right");
            } finally { harmony.UnpatchAll(harmony.Id); }
        });
        Test("ordinary native walking survives the patch", () => {
            table.SetValid(B+4,NavType.Floor,true);
            var move=T(NavType.Floor,NavType.Floor,1,0);
            int baseline=move.IsValid(B+3,table);
            var harmony = new Harmony("baiye.tests.compact.unrelated");
            harmony.CreateClassProcessor(typeof(CompactNavigationValidation)).Patch();
            try { Assert(move.IsValid(B+3,table)==baseline && baseline==B+4,"native walking changed"); }
            finally { harmony.UnpatchAll(harmony.Id); }
        });
        Test("entry injection is idempotent and preserves another mod's step", () => {
            var foreign=T(NavType.Floor,NavType.Tube,0,1);
            var added=CompactNavigationTransitions.AddEntry(new[]{foreign});
            Assert(added.Length==2,"foreign step incorrectly treated as our marker");
            Assert(CompactNavigationTransitions.AddEntry(added).Length==2,"duplicate compact entry appended");
            Assert(!StationNavigation.IsCompactEntry(foreign),"foreign step captured");
        });
        Test("cleanup and rebuilding remove stale station ownership", () => {
            StationRegistry.Remove(B,20);
            Assert(!StationRegistry.TryGet(B,out _)&&!StationRegistry.TryGet(B+20,out _),"stale cabin cells remain");
            StationRegistry.Publish(new StationTopology(B,20,false,true));
            Assert(Resolve(NavType.Floor,NavType.Tube,B,0,1)==B+20,"rebuilt station unusable");
        });
        Console.WriteLine($"{passed} native grid/navigation scenarios passed. In-memory Grid/NavTable; no Unity world simulation.");
    }
}
