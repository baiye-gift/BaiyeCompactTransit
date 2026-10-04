using System;

namespace Baiye.CompactTransit
{
    public sealed class StationStatus : KMonoBehaviour
    {
        private KSelectable selectable;
        private CompactStation station;
        private TravelTubeEntrance entrance;
        private Guid connectionHandle, chargeHandle;
        protected override void OnSpawn()
        {
            base.OnSpawn();
            selectable = GetComponent<KSelectable>(); station = GetComponent<CompactStation>(); entrance = GetComponent<TravelTubeEntrance>();
            var connections = new StatusItem("BAIYE_COMPACT_CONNECTIONS", TransitStrings.Text("管道：{PORTS}", "Tubes: {PORTS}"),
                TransitStrings.Text("上方管口：{UP}\n下方管口：{DOWN}\n可悬空建造，自带一格宽站台，无需在脚下另铺地板或渡口。上下管道直接接入。\n复制人站在平台上，从可达的左/右通道进出。两侧均不可达时不在本站下车，仍可贯通。平台不形成密封地砖。", "Upper tube port: {UP}\nLower tube port: {DOWN}\nCan be suspended with its own one-tile-wide platform; no extra floor or crossing is needed below the station. Tubes connect directly above and below.\nDuplicants use an accessible side walkway. Without a side landing, through travel remains available but disembarking is blocked. The platform is not a sealed tile."),
                "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.Power.ID, showWorldIcon:false);
            connections.resolveStringCallback = (text, data) => text.Replace("{PORTS}", Ports()).Replace("{UP}", Connected(station.TopConnected)).Replace("{DOWN}", Connected(station.BottomConnected));
            var charge = new StatusItem("BAIYE_COMPACT_CHARGE", TransitStrings.Text("储电：{CHARGE}/40 kJ · {STATE}", "Charge: {CHARGE}/40 kJ · {STATE}"),
                TransitStrings.Text("每次发射：10 kJ\n充电功率：960 W；充满后停止耗电。\n禁用发射和自动化红信号不阻止离开或贯通。\n加速蜡沿用原版按钮和配送规则。", "Per launch: 10 kJ\nCharging power: 960 W; stops drawing power when full.\nDisabling launching or a red automation signal does not block exit or through travel.\nSmooth Ride uses the native button and delivery rules."),
                "", StatusItem.IconType.Info, NotificationType.Neutral, false, OverlayModes.Power.ID, showWorldIcon:false);
            charge.resolveStringCallback = (text, data) => text.Replace("{CHARGE}", (entrance.AvailableJoules / 1000f).ToString("0.0")).Replace("{STATE}", State());
            connectionHandle = selectable.AddStatusItem(connections, this); chargeHandle = selectable.AddStatusItem(charge, this);
        }
        private static string Connected(bool value) => value ? TransitStrings.Text("已连接", "connected") : TransitStrings.Text("未连接", "not connected");
        private string Ports() => station.TopConnected && station.BottomConnected ? TransitStrings.Text("上下贯通", "above and below")
            : station.TopConnected ? TransitStrings.Text("仅上接", "above only") : station.BottomConnected ? TransitStrings.Text("仅下接", "below only") : TransitStrings.Text("未连接", "not connected");
        private string State()
        {
            if (!station.TopConnected && !station.BottomConnected) return TransitStrings.Text("未连接管道", "no tube connected");
            if (!GetComponent<Operational>().IsOperational) return TransitStrings.Text("发射暂停", "launching paused");
            if (GetComponent<TravelTubeEntrance.Work>().worker != null) return TransitStrings.Text("正在发射", "launching");
            return entrance.HasLaunchPower ? TransitStrings.Text("就绪", "ready") : TransitStrings.Text("等待电量", "waiting for charge");
        }
        protected override void OnCleanUp()
        {
            if (selectable != null) { selectable.RemoveStatusItem(connectionHandle); selectable.RemoveStatusItem(chargeHandle); }
            base.OnCleanUp();
        }
    }
}
