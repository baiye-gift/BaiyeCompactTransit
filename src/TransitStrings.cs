using System;

namespace Baiye.CompactTransit
{
    public static class TransitStrings
    {
        public static bool Chinese
        {
            get {
                string language = Localization.GetCurrentLanguageCode();
                return language != null && (language.StartsWith("zh", StringComparison.OrdinalIgnoreCase) || language.EndsWith("chinese", StringComparison.OrdinalIgnoreCase));
            }
        }
        public static string Text(string chinese, string english) => Chinese ? chinese : english;
        public static void Register()
        {
            string prefix = "STRINGS.BUILDINGS.PREFABS." + CompactStationConfig.ID.ToUpperInvariant();
            Strings.Add(prefix + ".NAME", Text("紧凑型双向运载站", "Compact Transit Station"));
            Strings.Add(prefix + ".DESC", Text("沿着一条直管道，在需要停靠的楼层挂上站台。", "Hang a platform at each stop along a straight tube shaft."));
            Strings.Add(prefix + ".EFFECT", Text("允许复制人进入和离开原版运载管道。\n占地 1×2，可悬空建造；自带一格宽薄平台，无需脚下地板或额外渡口。上下直接接管，左右步行进出。\n至少一侧需要两格高的可达落脚通道；无侧出口时禁止下车，保留直通。平台不形成密封地砖。\n每次发射消耗 10 kJ，储电 40 kJ，以 960 W 充电。充满后停止耗电。\n禁用与自动化红信号只停止发射，保留离开与贯通。",
                "Allows duplicants to enter and leave the native Transit Tube network.\nOccupies 1×2 tiles and can be suspended. Includes a one-tile-wide thin platform; no extra floor or crossing is needed below it. Tubes connect directly above and below; duplicants walk in and out on either side.\nNeeds an accessible two-tile-high side landing to disembark. Through travel remains available without a landing. The platform is not a sealed tile.\nEach launch costs 10 kJ; stores 40 kJ and charges at 960 W. Stops drawing power when full.\nDisabling and a red automation signal stop launching, while exit and through travel remain available."));
        }
    }
}
