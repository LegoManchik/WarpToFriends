using StardewModdingAPI.Utilities;

namespace WarpToFriends;

public class ModConfig
{
    [OptionDisplay("Open Menu Key")]
    public KeybindList OpenMenuKey { get; set; } = KeybindList.Parse("J");

    [OptionDisplay("Energy Cost (%)", 100, 0, true)]
    public int EnergyCostPercent { get; set; } = 10;

    [OptionDisplay("Allow Exhaustion", true)]
    public bool AllowExhaustion { get; set; } = false;

    [OptionDisplay("Allow Console Warp")]
    public bool canConsoleWarp { get; set; } = true;
}
