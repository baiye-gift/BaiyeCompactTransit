# Native game UI contract

| Capability | Canonical owner | Source of truth | Allowed variants | Verification |
| --- | --- | --- | --- | --- |
| Station status | KSelectable / StatusItem | StationStatus.cs | connection and stored charge | native assembly binding; real game pending |
| Launch enable | BuildingEnabledButton / LogicOperationalController | native TravelTubeEntrance | enable launch only | native reservation/navigation regression |
| Smooth Ride | TravelTubeEntrance user menu | native game implementation | native enable/disable wax | retained component; real game pending |
| Charge indicator | MeterController | native component plus art/station-layout.json | horizontal 51-frame charge strip / wax | verify-assets.py; real game pending |
| Integrated station body | one body_0 KAnim symbol | rounded-station-v5.png / station-layout.json | tube-wall edges terminate into magnets and bottom deck; no separate complete tube background | single-body/transparent-center asset checks and source preview; game appearance pending |
| Magnetic launch field | station KAnim / selected native route | write-scml.py / station-layout.json | short side-wall up/down streaks and matching screen arrow, neutral loop, fade on reset | field direction/range/alpha checks; character occlusion pending |
| Side access | native floor walking plus station navigation | StationNavigation.cs | accessible left/right walkway, then pad-to-tube | native left/right ingress and egress tests; character motion in game pending |
| Suspended support | Grid.FakeFloor / native FloorValidator | PlatformSupport.cs | one thin-floor reference below pad, no solid tile; release on cleanup | native no-ground/shared-support/recreation tests; real world/load pending |
| Native tube build-tool adapter | completed prefab KAnimGraphTileVisualizer / IUtilityItem | CompactStationConfig.cs / StationTubeConnections.cs | connection read/write only; skipRefresh and skipCleanup preserve station art and ownership | native completed-hook inspection, adapter write/refresh/cleanup checks; Unity mouse/build/save flow pending |
| Vertical sockets | UtilityNetworkTubesManager | StationTubeConnections.cs / TransitPatches.cs | two vertical native nodes, reject side connections, restore adjacent vertical socket when a neighboring tube re-registers; retain other native placement rules | actual native manager and 9 Harmony binding tests; rendering pending |

不存在网页表单、日期、分页、账户权限或数据删除流程。没有添加等价的自定义 UI 控件。连接缺失、等待电量、发射暂停和正在发射必须使用文本状态；已有标准按钮与悬浮提示保留。
