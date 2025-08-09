using UnityEngine;
using UserSettings.ServerSpecific;

namespace FakeRank;

public static class Utils
{
    /// <summary>
    ///     Registers the SSSS for the plugin based on values in the config and translation files, by combining the existing
    ///     settings with the new ones.
    /// </summary>
    public static void RegisterKeybinds()
    {
        ServerSpecificSettingBase[] extra =
        [
            new SSGroupHeader(Plugin.Instance.Translation.FakeRankGroupHeader),
            new SSKeybindSetting(
                Plugin.Instance.Config!.KeybindId,
                Plugin.Instance.Translation.KeybindSettingLabel,
                KeyCode.None, true, false,
                Plugin.Instance.Translation.KeybindSettingHintDescription)
        ];

        ServerSpecificSettingBase[] existing = ServerSpecificSettingsSync.DefinedSettings ?? [];

        ServerSpecificSettingBase[] combined = new ServerSpecificSettingBase[existing.Length + extra.Length];
        existing.CopyTo(combined, 0);
        extra.CopyTo(combined, existing.Length);

        ServerSpecificSettingsSync.DefinedSettings = combined;
        ServerSpecificSettingsSync.UpdateDefinedSettings();
    }
}