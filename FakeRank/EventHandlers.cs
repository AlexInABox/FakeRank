using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Wrappers;
using MEC;

namespace FakeRank;

public static class EventHandlers
{
    private static readonly Dictionary<string, (string, string)> FakeRanks = new();
    private static CoroutineHandle _coroutineFakeRankLoop;
    private static CoroutineHandle _coroutineFetchLoop;

    public static void RegisterEvents()
    {
        //Apply fakerank every ten seconds
        _coroutineFakeRankLoop = Timing.RunCoroutine(FakeRankLoop());

        //Refetch from backend every ten seconds
        _coroutineFetchLoop = Timing.RunCoroutine(FetchLoop());

        // Fetch fake rank on player join
        PlayerEvents.Joined += OnJoined;
    }

    public static void UnregisterEvents()
    {
        Timing.KillCoroutines(_coroutineFakeRankLoop);
        Timing.KillCoroutines(_coroutineFetchLoop);

        PlayerEvents.Joined -= OnJoined;
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

                if (player.ReferenceHub.serverRoles.HasBadgeHidden) continue;

                if (string.IsNullOrEmpty(fakeRank))
                {
                    player.GroupName = player.UserGroup?.BadgeText ?? string.Empty;
                    player.GroupColor = player.UserGroup?.BadgeColor ?? "default";

                    continue;
                }

                player.GroupName = fakeRank + " (" + (player.UserGroup?.BadgeText ?? "Stammspieler") + ")";
                player.GroupColor = fakeColor;
            }

            yield return Timing.WaitForSeconds(10f);
        }
    }

    private static IEnumerator<float> FetchLoop()
    {
        while (true)
        {
            GetAllFakeRanksFromBackend();
            yield return Timing.WaitForSeconds(10f);
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
    }

    private static async void GetAllFakeRanksFromBackend()
    {
        Dictionary<string, (string Name, string Color)> ranks = new();

        try
        {
            Config cfg = Plugin.Instance.Config!;
            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", cfg.BackendAPIToken);

            // collect all userIds from Player.ReadyList
            List<string> userIds = Player.ReadyList.Select(p => p.UserId).ToList();

            // build query string: userid=...&userid=...
            string query = string.Join("&", userIds.Select(id => $"userid={Uri.EscapeDataString(id)}"));

            HttpResponseMessage res =
                await client.GetAsync($"{cfg.BackendURL}/fakerank?{query}");

            if (res.IsSuccessStatusCode)
            {
                string[] entries = (await res.Content.ReadAsStringAsync()).Split(';');
                foreach (string entry in entries)
                {
                    string[] parts = entry.Split(',');
                    if (parts.Length == 3)
                        ranks[parts[0].Trim()] = (parts[1].Trim(), parts[2].Trim());
                }
            }
        }
        catch
        {
        }

        foreach (KeyValuePair<string, (string, string)> rank in ranks) FakeRanks[rank.Key] = rank.Value;
        // Remove any userIds that are no longer present
        foreach (string userId in FakeRanks.Keys.ToList().Where(userId => !ranks.ContainsKey(userId)))
        {
            FakeRanks[userId] = (string.Empty, string.Empty);
        }
    }
}