using UserSettings.ServerSpecific;

namespace FakeRank;

public static class Utils
{
    /// <summary>
    ///     Registers the SSSS for the plugin based on values in the config and translation files, by combining the existing
    ///     settings with the new ones.
    /// </summary>
    public static void RegisterSSS()
    {
        ServerSpecificSettingBase[] extra =
        [
            new SSGroupHeader(Plugin.Instance.Translation.GroupHeader),
            new SSButton(Plugin.Instance.Config!.RefreshButtonId, "Meinen FakeRank aktualisieren:",
                Plugin.Instance.Translation.RefreshButtonLabel,
                null, Plugin.Instance.Translation.RefreshButtonHint)
        ];

        ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? [];

        ServerSpecificSettingBase[] combined = new ServerSpecificSettingBase[existing.Length + extra.Length];
        existing.CopyTo(combined, 0);
        extra.CopyTo(combined, existing.Length);

        ServerSpecificSettingsSync.DefinedSettings = combined;
        ServerSpecificSettingsSync.UpdateDefinedSettings();
    }
}