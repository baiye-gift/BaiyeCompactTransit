# BaiyeCompactTransit 开发记录

此项目在0.1.5首次独立建仓；以下历史条目迁自原工作区CHANGELOG.md。条目中的CompactTransit/路径前缀表示此前目录，旧验证、安装时间与未验证部分保留为当时记录。

## 0.1.5 独立仓库提交 — 2026-10-04

- 用户实机确认拖建空引用已经修复、当前没有问题，并授权新建公开GitHub仓库baiye-gift/BaiyeCompactTransit及提交现有代码。
- 新增本项目CHANGELOG.md，迁入本站0.1.0—0.1.5与外观候选历史；tools/build-package.ps1改为读取项目内记录，独立克隆不再依赖父项目文件。.gitignore补齐本机审计、缓存、存档、日志及环境文件排除。README.md、docs/playtest.md和docs/development-plan.md更新独立仓库与实机反馈。
- 集成范围为本站源码、真实程序集测试、可编辑SCML、原始图与KAnim资源及制作说明；不上传游戏程序集、存档、构建目录或安装备份，不更改父项目FusionPower的远程地址。
- 提交前36项原生检查、15动画/110精灵资源检查通过；已创建公开仓库 https://github.com/baiye-gift/BaiyeCompactTransit，独立打包Release编译0错误、2个既有程序集引用警告，12个ZIP条目及逐文件哈希匹配。首个源码提交 `2499862` 已推送main，远端SHA与本地一致；共159个源码/资源文件，暂存内容检查未发现本机产物、令牌/私钥模式或尾随空白，git diff --cached --check通过。直接从GitHub重新克隆到独立核验目录后，Release编译0错误、2个既有警告，12个ZIP条目及逐文件哈希检查通过，不依赖父项目文件。全量Unity分项矩阵、存档往返和多模组兼容没有逐项验证，本次用户反馈仅确认报错修复及当前运行。

## BaiyeCompactTransit 0.1.5 — 2026-10-04（建管空引用修复）

- 用户报告U59-744825原版BaseUtilityBuildTool.BuildPath空引用，并确认修复及安装范围。本机Player.log显示同一0x0021e堆栈；调查原版BuildPath确认：TravelTubeTile中的完成建筑必须提供KAnimGraphTileVisualizer，原版无空检查调用IUtilityItem.UpdateConnections。本站此前占用了该层，却缺少该组件。
- `CompactTransit/src/CompactStationConfig.cs` 完成态补原生建管接口；`src/StationTubeConnections.cs` 新增集中配置：Tube来源、物理节点、初始上下连接，skipRefresh和skipCleanup启用。组件只承担原生连接接口，不覆盖本站动画或重复清理两格管网。没有新增管道绘制层，1×2、悬空平台、左右进出、美术和运输规则保留。
- `CompactTransit/tests/Native/Program.cs` 新增完成态接口及配置调用检查、原生组件连接写入/跳过绘制/跳过清理检查。新增检查在修复前失败“completed station occupies tube tiles without the native build-tool interface”；修复后36项检查通过。中间测试误将无外管站体期待为双向物理连接，改用清理前后两格实际快照；未为此改变生产逻辑。
- `CompactTransit/mod_info.yaml` 升为0.1.5；`README.md`、`DESIGN.md`、`UX-CONTRACT.md`、`docs/playtest.md`、`docs/development-plan.md` 更新原生接口职责、实际验证边界及普通/即时建造、拖线穿过上下格、起终点和单击验收。独立复核原版建管、组件加载、动画/清理保护与替换规则后无可行动发现。Release编译0错误、2个既有游戏程序集引用警告；严格UI静态审计0发现，12个ZIP条目及逐文件哈希、34个源码/文档尾随空白、git diff --check通过。三项动画资源哈希与0.1.4一致。游戏退出后安装0.1.5，10个安装文件哈希与发布目录匹配；0.1.4的10个旧文件备份为 `CompactTransit/dist/install-backups/BaiyeCompactTransit-20261004-210456`，备份哈希匹配。最终同步此记录并重建发布包、再次核对安装，报告为 `CompactTransit/dist/verification/installation.json`。未修改其他模组、启用配置或存档。
- 未验证：Unity完整鼠标拖建/替换/即时建造、读档及多模组组合。自动检查读取真实配置方法并执行原生接口，但不构造Unity GameObject或运行完整BuildPath；不将接口检查当作游戏报错已实机消失。

## BaiyeCompactTransit 0.1.4 — 2026-10-04（圆润接环测试包）

- 用户确认独立建筑图并授权开发。`CompactTransit/art/source/rounded-station-v5.png` 原样采用已确认候选，`rounded-station-prompts.txt` 保存完整提示词和当前集成状态，`assets.md` 更新来源、单主体和实际渲染边界。
- `CompactTransit/art/station-layout.json`、`tools/build-anims.ps1`、`tools/write-scml.py`、生成的 `art/source/baiye_compact_transit/*` 与 `anim/assets/baiye_compact_transit/*`：圆润象牙白/蓝灰接环、短磁极、非对称右屏与薄踏板统一绘制。逻辑仍1×2；视觉140×224原生单位，顶边200、踏板表面约0、薄前脸向下24单位。去掉中央圆环及旧foot_glow，改为两侧短流光和对应路线方向箭头；原生电量/蜡表各51档。
- `CompactTransit/src/StationTubeConnections.cs`、`TransitPatches.cs`、`CompactStation.cs`：限定补回本站上下相邻管道晚恢复连接时的朝站管口，首个Sim200ms刷新邻管显示。`tests/Native/Program.cs` 新增真实管网重登记回归，修复前失败“late upper pipe retained an endpoint cap”，修复后34项检查通过；普通远处管段及站体清理后保持原规则。独立复核无可行动发现。
- `CompactTransit/tools/verify-assets.py` 更新单主体、方向屏、侧壁流光、站顶/踏板几何检查，保留65%中央透明门槛；`preview-anims.ps1`更新联系表。15个动画、110个精灵、512×512图集和两套51档仪表检查通过；联系表目视检查通过。
- `CompactTransit/mod_info.yaml`、`README.md`、`DESIGN.md`、`UX-CONTRACT.md`、`docs/playtest.md`、`docs/development-plan.md` 更新版本、外观、行为、先建管/先建站/读档管口验收及运行边界。Release编译0错误、2个既有游戏程序集引用警告；严格UI静态审计0发现，12个ZIP条目及逐文件哈希检查通过；git diff --check、源码/文档尾随空白和已确认源图逐字节复制检查通过。游戏退出后升级安装至 `D:/Documents/Klei/OxygenNotIncluded/mods/local/BaiyeCompactTransit`；10个安装文件SHA-256与发布目录一致。0.1.3的10个旧文件备份到 `CompactTransit/dist/install-backups/BaiyeCompactTransit-20261004-174821`，备份哈希一致；同步最终记录后重建发布包并再次核对安装，报告为 `CompactTransit/dist/verification/installation.json`。未更改其他模组、启用配置或存档。
- 尚未验证：Unity实际上下接缝、略宽接环与相邻建筑的重叠、人物脚部/装备遮挡、动态读档及多模组组合。34项测试使用真实游戏程序集和内存格图，不创建运行世界；源联系表不是游戏截图。

## BaiyeCompactTransit 外观候选 — 2026-10-04（仅供用户审阅）

- 用户先要求 UI 预览，随后提供新的圆润接环/非对称控制盒参考图，确认后要求查看独立建筑图。新增 `CompactTransit/art/previews/rounded-station-v5.png`：内置 imagegen 将已确认的概念转成单张待机外壳透明图，并定向去掉顶部外部管段、下方鼓包；保留短驱动器、方向屏、反滑纹理薄平台和黄黑警示。`rounded-station-v5-prompts.txt` 保存两次完整提示词及候选状态。
- 检查源图 RGBA、透明与不透明 alpha 范围及中央人物区域真实透明通过。此图是美术候选，未引用到 `art/station-layout.json`，未导出 KAnim、编译或安装，代码和已安装的 0.1.3 资源不变。
- 尚未验证：按 1×2 逻辑占格导入后的比例、原版管道接缝、原生仪表与方向动画、人物遮挡和游戏实际呈现。等待用户对独立建筑图的判断后再决定集成。

## BaiyeCompactTransit 0.1.3 — 2026-10-04（一体式电磁节点测试包）

- 用户实机确认 0.1.2 整体运行正常，授权采用电磁换乘节点外观，并明确站内管道与建筑必须一体，不能是封闭圆管背景加设备贴片。`CompactTransit/art/source/magnetic-node-v4-final.png` 是内置 imagegen 生成并经插槽、管壁定向修正的透明源图；`magnetic-node-v4.png`、`magnetic-node-v4-gauge.png` 保留中间图，`magnetic-node-prompts.txt` 保存三个完整提示词，`assets.md` 更新来源和结构说明。
- `CompactTransit/art/station-layout.json`、`tools/build-anims.ps1`、`tools/write-scml.py` 及生成的 `art/source/baiye_compact_transit/*`、`anim/assets/baiye_compact_transit/*`：一张 body_0 统一管壁、磁极接环及底边薄平台，去掉独立挡片和箭头，新增嵌入磁极插槽的亮片与三条稀疏场线；上下行启动按已选原版路线扫动，循环中性稳定、复位渐隐；电量表移至接环中央，微型蜡表移至平台。未增加站内完整管道子绘制器，未修改 C# 运输、平台支撑、预约或存储逻辑。
- `CompactTransit/tools/verify-assets.py` 新增单结构主体/允许符号、场线方向/范围/透明度、中央无封闭管面或立柱、底边平台检查；保留 65% 开放区域门槛。初次导出透明率 64.9846%，检查失败；定向收薄管壁再导出后通过，没有降低门槛。15 个动画、109 个精灵、两套 51 档仪表和 512×512 图集检查通过。
- `CompactTransit/tools/preview-anims.ps1` 更新磁场状态联系表；`README.md`、`docs/playtest.md`、`docs/development-plan.md`、`DESIGN.md`、`UX-CONTRACT.md` 更新一体化绘制所有者、用户运行反馈、动画行为及实机验收边界；`mod_info.yaml` 升为 0.1.3。
- 验证：33 项原生程序集检查复测通过；严格 UI 静态审计 0 发现；资源检查及源动画联系表目视检查通过。Release 编译 0 错误、2 个既有游戏程序集引用警告；12 个 ZIP 条目的内容及逐文件哈希、git diff --check 和 32 个源码/文档尾随空白检查通过。
- 游戏退出后升级安装至 `D:/Documents/Klei/OxygenNotIncluded/mods/local/BaiyeCompactTransit`，10 个安装文件 SHA-256 与发布目录一致。0.1.2 的 10 个旧文件备份到 `CompactTransit/dist/install-backups/BaiyeCompactTransit-20261004-165057`，备份哈希匹配；同步最终开发记录后重建发布包、核对安装和 SHA-256 配套文件。报告位于 `CompactTransit/dist/verification/installation.json`。未改其他模组、启用配置或存档。
- 尚未验证：0.1.3 游戏实际接缝、一体感、磁场与人物/装备遮挡、旧存档外观更新；完整乘坐/断修/拆除/存档/兼容矩阵也未逐项验收。源预览和静态单主体检查不等同于游戏实际绘制层级验证。

## BaiyeCompactTransit 0.1.2 — 2026-10-04（悬空站台测试包）

- 根据用户 0.1.1 游戏截图反馈，经确认改为 1×2 悬空站台。`CompactTransit/src/CompactStationConfig.cs` 改为 Anywhere 放置并排除默认 RequiresFoundation；新增 `PlatformSupport.cs`，用原生 FakeFloor 引用让自带一格薄平台可以站人，不生成实心地板、不阻断气液；拆除只释放自身引用，饱和时重试。
- 新增 `CompactTransit/src/StationTubeConnections.cs`，`CompactStation.cs` 在自身两格登记真实原生上下管网节点，并刷新相邻管道接头显示；下方直接接管，不再依赖站下外部地板或渡口。拓扑先发布再连接，清理时先禁止回调重登记，再释放支撑及自身管网节点。
- `CompactTransit/src/StationNavigation.cs` 要求至少一侧有可达、无头部阻挡的落脚通道才能下车，无出口时保留贯通。`TransitPatches.cs` 新增管网 SetConnections 竖直接口过滤与 CanAddConnection 侧接拒绝，修复独立复核发现的后建侧管污染；保留其他原生建管限制。`StationStatus.cs`、`TransitStrings.cs` 更新悬空平台与出口说明。
- `CompactTransit/tests/Native/Program.cs` 新增支撑引用、重建、真实派生管网连接/断修/拆除、后建侧管过滤、放置规则及左右出口场景。新增平台测试先失败，报“floating pad still needs external ground”，实现后 33 项检查通过；9 个生产 Harmony 目标实际绑定。复核修复通过，无新增具体代码缺陷。
- `CompactTransit/art/source/floating-station-final.png` 为内置 imagegen 定向编辑后选用的透明源图；`floating-station-v3.png` 保留未采用的中间图，`floating-station-prompts.txt` 保存两次完整提示词，`assets.md` 更新来源。`art/station-layout.json`、生成的 `art/source/baiye_compact_transit/*` 与 `anim/assets/baiye_compact_transit/*` 改为平口接头和底边薄平台，去掉尖圆柱管嘴、中央支柱和深底座；15 个动画、108 个精灵及仪表检查通过，联系表已目视检查。
- `CompactTransit/tools/preview-layout.ps1`、`docs/connection-layout.png` 更新悬空直管连接图；`README.md`、`docs/playtest.md`、`docs/development-plan.md`、`DESIGN.md`、`UX-CONTRACT.md` 更新施工、出口、升级及验证说明；`mod_info.yaml` 升为 0.1.2。
- 验证：33 项托管原生程序集检查、动画资源检查、严格 UI 静态审计（0 发现）和独立代码复核通过；Release 编译 0 错误、2 个既有游戏程序集引用警告。资源联系表及连接逻辑图已目视检查；12 个 ZIP 条目的内容/逐文件哈希、git diff --check 和源码/文档尾随空白检查通过。
- 游戏进程退出后升级安装至 `D:/Documents/Klei/OxygenNotIncluded/mods/local/BaiyeCompactTransit`，10 个文件 SHA-256 与发布目录一致。旧版 10 个文件完整备份到 `CompactTransit/dist/install-backups/BaiyeCompactTransit-20261004-153822`，备份哈希核对通过。同步最终记录后再次打包、核对本地安装；报告位于 `CompactTransit/dist/verification/installation.json`。未改其他模组、启用配置或存档。
- 尚未验证：Unity 悬空施工、运行世界导航脏区传播、动态管口显示、真实乘坐与人物脚部对齐、在用站体拆除、存档往返和多模组组合。原生管网测试的 Pathfinding 持有空 NavGrid 列表；支撑重建检查不等同于真实读档；UI 静态审计不检查游戏屏幕。

## BaiyeCompactTransit 0.1.1 — 2026-10-04（开放式外观测试包）

- 用户确认“上下接管、左右走人”的开放式磁力站门方向后改版。`CompactTransit/art/source/station-frame-v2.png` 是内置 imagegen 生成的新透明原图；旧 `station-housing.png` 保留。`art/source/assets.md` 保存工具、完整提示词与导入说明。
- `CompactTransit/art/station-layout.json` 统一外壳选择、挡片及仪表几何；`tools/build-anims.ps1`、`write-scml.py`、`preview-anims.ps1` 和生成的 `art/source/baiye_compact_transit/*`、`anim/assets/baiye_compact_transit/*` 替换玻璃罐壁为顶部小挡片，改用横梁电量灯带及较小方向光。固定框架中央透明，空闲挡片不遮通道；图集从 1024×512 改为 512×512。
- `CompactTransit/src/StationStatus.cs`、`TransitStrings.cs` 明确上下管口和左右步行，要求至少一侧保留两格高可达通道；连接状态简化为仅上接、仅下接、上下贯通或未连接。建筑 ID、导航、充电、预约、蜡和启停规则不变。
- `CompactTransit/tests/Native/Program.cs` 增加真实原生 Transition.IsValid 的左右走入踏板及下车后左右离开两项检查，25 项托管检查全部通过。`tools/verify-assets.py` 增加空闲遮罩、中央透明区域、横向仪表尺寸及位置检查，15 个动画、108 个精灵检查通过；动画联系表已目视检查。
- `CompactTransit/tools/preview-layout.ps1`、`docs/connection-layout.png` 新增格子连接示意。`README.md`、`docs/playtest.md`、`docs/development-plan.md`、`DESIGN.md`、`UX-CONTRACT.md` 更新连接、改版、验证及左右动作验收说明。`mod_info.yaml` 升为 0.1.1；`tools/build-package.ps1` 从清单读取版本并打包 `CONNECTIONS.png`，生成新版 ZIP 和 SHA-256 文件。
- 验证：Release 编译 0 错误、2 个既有游戏程序集引用警告；25 项托管检查、动画资源检查、严格 UI 静态审计（0 发现）、12 个 ZIP 条目的内容/逐文件哈希检查、git diff --check 及 28 个源码/文档的尾随空白检查通过。新图和连接图均已目视检查。
- 用户随后确认安装：游戏进程已退出，首次安装至 `D:/Documents/Klei/OxygenNotIncluded/mods/local/BaiyeCompactTransit`，没有同名旧版需要备份。安装 `BaiyeCompactTransit.dll`、`mod.yaml`、`mod_info.yaml`、`README.md`、`PLAYTEST.md`、`CONNECTIONS.png`、`CHANGELOG.md` 和 `anim/assets/baiye_compact_transit/` 的 PNG、ANIM、BILD 共 10 个文件；逐文件与发布 ZIP 的 SHA-256 核对通过。更新安装记录后同步发布包及本地记录，验证报告保存在 `CompactTransit/dist/verification/installation.json`。未改其他模组、启用配置或存档。
- 游戏内启用和 Unity 实测尚未执行。真实左右进出、脚部与踏板对齐、人物与导轨的遮挡、侧栏呈现、在用站体拆除、0.1.0 升级存档及多模组组合仍需实机验收；不把逻辑图或托管测试当作游戏截图或实测结果。

## BaiyeCompactTransit 0.1.0 — 2026-10-04（开发测试包）

- 新增独立 `CompactTransit/CompactTransit.csproj`、`src/StationTopology.cs`、`StationNavigation.cs`、`CompactTransitMod.cs`、`CompactStationConfig.cs`、`CompactStation.cs`、`TransitPatches.cs`、`TransitStrings.cs`、`StationStatus.cs`：1×2 双向运载站，复用原版入口储电/预约/扣电，加入上下贯通和局部导航步骤；导航工作线程使用拓扑快照。
- `TransitPatches.cs` 还读取原版已选路线控制上下行启动动画；不新增选层窗口或路径搜索。未知/取消路线使用中性动画，贯通及离开不受发射开关控制。
- `CompactTransit/tests/Native/Native.csproj`、`Program.cs`：23 项真实游戏程序集检查通过，覆盖进出、贯通、断管、堵塞、储电预约、自动化、普通步行保留、方向动画选择、重复注入、清理重建，及全部 7 个生产 Harmony 目标实际绑定。首测在未实现的导航适配上失败，报“compact station transition was not handled”；实现后复测通过。
- `CompactTransit/art/source/station-housing.png`、`assets.md`、`art/source/baiye_compact_transit/*`、`anim/assets/baiye_compact_transit/*`：保留生成的原图和可编辑 SCML；新增 108 个精灵、15 个动画、128 像素菜单图与两个 51 档原生仪表。`tools/build-anims.ps1`、`write-scml.py`、`preview-anims.ps1`、`verify-assets.py` 完成机械图集导入、导出与验证；资源检查及源动画预览通过。
- `CompactTransit/mod.yaml`、`mod_info.yaml`、`README.md`、`docs/development-plan.md`、`docs/playtest.md`、`.gitignore`：独立模组清单、安装和楼层连接说明、执行记录及实机验收清单；隔离构建与只读反编译输出。`DESIGN.md`、`UX-CONTRACT.md`、`premium-ui.json` 记录原生 UI 所有者、状态行为和语言策略。
- `CompactTransit/tools/build-package.ps1`：生成 `dist/BaiyeCompactTransit-0.1.0.zip` 和 SHA-256 文件；检查 ZIP 必要文件、单一模组 DLL，以及每个 ZIP 条目与打包目录的 SHA-256 一致，不附带游戏程序集。不安装到游戏目录。
- 验证：23 项托管检查、动画资源检查、严格 UI 静态审计（0 发现）、Release 构建（0 错误、2 个游戏程序集引用警告）、ZIP 逐文件哈希及配套 SHA-256 校验、git diff --check 和 26 个新增源码/文档的尾随空白检查通过。独立代码复核无可行动发现；Unity 加载、侧栏屏幕呈现、复制人动作与装备遮挡、在用站体拆除、存档往返及多模组组合尚未验证。
- 原生测试在 .NET 8 进程中为游戏自带 Harmony 使用 Cecil 动态方法后端及可信内部元数据序列化兼容设置；这些设置只存在于测试工程，不进入发布 DLL。UI 静态工具不验证 Unity 屏幕，托管 Grid/NavTable 检查也不等同于游戏实测。
