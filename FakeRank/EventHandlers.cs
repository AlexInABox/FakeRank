using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;
using UserSettings.ServerSpecific;

namespace FakeRank;

public static class EventHandlers
{
    private static readonly Dictionary<string, (string, string)> FakeRanks = new();
    private static CoroutineHandle _coroutine;

    private static readonly Dictionary<string, (DateTime LastFetch, (string Name, string Color) Rank)> FakeRankCache =
        new();

    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(10);

    public static void RegisterEvents()
    {
        Utils.RegisterSSS();

        ServerSpecificSettingsSync.ServerOnSettingValueReceived += OnSSSReceived;

        // Feel free to add more event registrations here
        PlayerEvents.Joined += OnJoined;

        //Apply fakerank every five seconds
        _coroutine = Timing.RunCoroutine(FakeRankLoop());
    }

    private static void OnSSSReceived(ReferenceHub hub, ServerSpecificSettingBase ev)
    {
        if (!Player.TryGet(hub.networkIdentity, out Player player))
            return;

        if (ev is SSButton button && button.SettingId == Plugin.Instance.Config!.RefreshButtonId)
            GetFakeRankFromBackend(player.UserId);
    }

    public static void UnregisterEvents()
    {
        ServerSpecificSettingsSync.ServerOnSettingValueReceived -= OnSSSReceived;
        PlayerEvents.Joined -= OnJoined;
        Timing.KillCoroutines(_coroutine);
    }

    private static IEnumerator<float> FakeRankLoop()
    {
        while (true)
        {
            foreach (KeyValuePair<string, (string, string)> rank in FakeRanks)
            {
                if (!Player.TryGet(rank.Key, out Player player))
                    continue;

                string fakeRank = rank.Value.Item1;
                string fakeColor = rank.Value.Item2;

                // Case 1: Player has no rank (null or empty)
                if (string.IsNullOrEmpty(player.GroupName))
                {
                    if (!string.IsNullOrEmpty(fakeRank))
                    {
                        player.GroupName = fakeRank + " (Stammspieler)";
                        player.GroupColor = fakeColor;
                    }

                    continue;
                }

                // Case 2: Already in format "FakeRank (Original)"
                if (player.GroupName.Contains("("))
                {
                    string currentFake = player.GroupName.Split('(')[0].Trim();
                    string originalRank = player.GroupName.Split('(')[1].Split(')')[0].Trim();

                    if (currentFake == fakeRank && player.GroupColor == fakeColor)
                        continue; // already correct

                    if (!string.IsNullOrEmpty(fakeRank))
                    {
                        // Replace old fake with new fake or update color
                        player.GroupName = fakeRank + " (" + originalRank + ")";
                        player.GroupColor = fakeColor;
                    }
                    else
                    {
                        // Remove fake, restore original
                        player.GroupName = originalRank;
                        player.GroupColor = "default";
                    }

                    continue;
                }

                // Case 3: Has a rank but no parentheses -> add fake rank
                if (!string.IsNullOrEmpty(fakeRank))
                {
                    player.GroupName = fakeRank + " (" + player.GroupName + ")";
                    player.GroupColor = fakeColor;
                }
            }

            yield return Timing.WaitForSeconds(1);
        }
    }

    private static void OnJoined(PlayerJoinedEventArgs ev)
    {
        if (ev.Player.UserId == string.Empty || ev.Player.IsDummy || ev.Player.IsHost) return;
        //if (!ev.Player.HasPermissions("fakerank")) return;

        GetFakeRankFromBackend(ev.Player.UserId);
    }

    private static async void GetFakeRankFromBackend(string userId)
    {
        if (FakeRankCache.TryGetValue(userId, out (DateTime LastFetch, (string Name, string Color) Rank) cache) &&
            DateTime.UtcNow - cache.LastFetch < CacheDuration)
        {
            FakeRanks[userId] = cache.Rank;
            return;
        }

        (string Name, string Color) rank = (string.Empty, string.Empty);

        try
        {
            Config cfg = Plugin.Instance.Config!;
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", cfg.BackendAPIToken);

            HttpResponseMessage res =
                await client.GetAsync($"{cfg.BackendURL}/fakerank?userid={Uri.EscapeDataString(userId)}");
            if (res.IsSuccessStatusCode)
            {
                string[] parts = (await res.Content.ReadAsStringAsync()).Split(',');
                if (parts.Length == 2)
                    rank = (parts[0].Trim(), parts[1].Trim());
            }
        }
        catch
        {
        }

        FakeRanks[userId] = rank;
        FakeRankCache[userId] = (DateTime.UtcNow, rank);
    }
}