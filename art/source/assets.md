# 原生建筑资源

## 当前0.1.4资源

用户提供圆润接环参考并确认独立候选后，原样采用 `rounded-station-v5.png`。它从 `art/previews/rounded-station-v5.png` 机械复制，未重新生成或手绘改动；内置image_gen两次生成/定向修正的完整提示词见 `rounded-station-prompts.txt`，两次均采用真实透明背景。原始PNG中央及左右人行区域保持alpha透明，整张body_0统一拥有管壁、接环、短磁极、右侧控制盒和站台。

逻辑1×2；外壳视觉140×224原生单位，x=4.9、y=-24，顶边200，踏板顶面约0，薄前脸向下少量外伸。外壳略宽于主管道，不增加碰撞格。电量表、平台蜡表各51档，位置从station-layout.json读取；屏幕上下箭头仅在对应原版已选路线启动时点亮。两条短流光沿后管壁边缘移动，不越过中央人物区域；空闲光效透明，循环中性、复位渐隐。取消旧foot_glow及中央圆环。110个精灵、15个原生动画、512×512图集。

System.Drawing脚本只机械导入原图及生成独立几何光效/计量部件；write-scml.py仅写动画元数据。源预览不等于游戏实测；实际接缝、脚部、装备遮挡及绘制层级待Unity验收。

## 历史资源

0.1.3 使用 `magnetic-node-v4-final.png`，由内置 imagegen 根据用户已确认的电磁节点概念生成，再定向补齐空蜡表插槽、收薄两侧管壁，透明原图完整保留。2026-10-04 用户明确要求管道与站体一体：整张 body_0 包括管壁、磁极接环与底边薄平台，管壁直接接入磁极座，中心真实透明；没有第二根完整封闭圆管的背景图层。上下仍接外部原版管道。平台的真实站立支撑由代码实现，图像本身不提供碰撞或导航。

本次三个完整提示词保存在 [magnetic-node-prompts.txt](magnetic-node-prompts.txt)。工具为内置 `image_gen`，均 `transparent_background: true`，未使用 CLI。`magnetic-node-v4.png`、`magnetic-node-v4-gauge.png` 保留中间图；只有最终源图进入发布图集。

0.1.2 的两个历史提示词保存在 [floating-station-prompts.txt](floating-station-prompts.txt)。工具为内置 `image_gen`，两次均 `transparent_background: true`，未使用 CLI。`floating-station-v3.png` 是未采用的第一次输出；`floating-station-final.png`、`station-frame-v2.png`、`station-housing.png` 作为历史源图保留，均不进入当前发布图集。用户截图仅用作参考，未打包。

`tools/build-anims.ps1` 只进行机械导入、尺寸适配与图集准备；贴合磁极插槽的亮片、稀疏场线和计量表作为独立几何部件生成，不另造结构层。`art/station-layout.json` 统一源图、磁极/场线尺寸、仪表尺寸和位置；制作、SCML 与预览共同读取。`tools/write-scml.py` 只生成元数据，不编辑图片。所有动画可在 Spriter 中编辑。

固定外壳以 100×200 原生动画单位匹配 1×2 建筑；UI 采用独立 128×128 图标。接环电量表和平台微型蜡表使用原版 MeterController 要求的 `meter_target`/`meter` 与 `wax_meter_target`/`wax_meter`，各 51 档。磁极及场线动画独立于真实储电逻辑；空闲光效透明，中央无全高遮罩。上下行仅在已选路线的启动动画中移动；发射中为中性稳定场，复位渐隐。预览电量是示例。

复制人的身体动作保留原版运载管道互动动画，并把交互位置固定在站内；没有重新绘制复制人皮肤、衣服或装备。精确遮挡与携带物显示仍需 Unity 实机验收。

## v2 完整生成提示词

工具：内置 `image_gen`，`transparent_background: true`，全新生成，未使用 CLI 或参考图。

```text
Use case: stylized-concept. Asset type: production transparent 2D building sprite for an Oxygen Not Included mod, not a concept sheet. Primary request: completely redesigned compact magnetic transit launch station, exactly one tile wide by two tiles tall. Side-scrolling game orthographic FRONT elevation, no perspective, single isolated sprite, slim industrial cartoon machinery with bold dark brown charcoal ink outlines, simple ivory enamel surfaces, desaturated warm yellow fittings, tiny restrained mint status lights, flat 2-tone cel shading, readable at 100x200 pixels. Structure: a small magnetic pipe connector crown centered at the top, an unobtrusive thin horizontal electromagnet arch just below it, a low flat launching pad and centered pipe connector at the bottom. The crown and pad are connected only by a very thin rear guide support, behind where a person would stand. Both LEFT and RIGHT sides at human walking height are completely OPEN and unobstructed: no full-height side wall, no glass, no sealed cylinder, no tank, no broad vertical pillars. Center at least 65 percent genuinely transparent negative space so the game character can be seen. A narrow EMPTY horizontal battery light slot belongs to the top arch at right; a tiny EMPTY wax indicator socket on the bottom left. No charge light fill, no arrows, no doors painted into the fixed sprite; those are separate animation parts. Design should feel like a small open industrial elevator stop built into the colony walkway, friendly and mechanical, with only a handful of bolts and one small cable. Silhouette fits a 1:2 rectangle, top and bottom aligned on the same vertical pipe axis. Actual transparent alpha background including the interior. No person, no lettering, no icons, no scene, no ground shadow, no floor tiles, no extra versions, no border, no photorealism, no chrome, no steampunk ornament.
```
