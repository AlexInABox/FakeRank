using System.ComponentModel;

namespace FakeRank;

public class Translation
{
    [Description("The label for the keybind setting")]
    public string KeybindSettingLabel { get; set; } = "FakeRank";

    [Description("The hint description for the keybind setting")]
    public string KeybindSettingHintDescription { get; set; } =
        "Press this key to FakeRank!!";

    [Description("Header text for the spray settings group")]
    public string FakeRankGroupHeader { get; set; } = "FakeRank Plugin Settings";
}