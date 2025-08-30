using System.ComponentModel;

namespace FakeRank;

public class Config
{
    public bool Debug { get; set; } = false;
    public string BackendURL { get; set; } = "https://example.com/";
    public string BackendAPIToken { get; set; } = "your_api_token_here";

    [Description("The ID of the keybind setting. This should be unique for each plugin.")]
    public int RefreshButtonId { get; set; } = 600;

    [Description("The ID of the keybind setting. This should be unique for each plugin.")]
    public int AdminRefreshButtonId { get; set; } = 601;
}