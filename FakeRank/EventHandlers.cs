using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Features.Wrappers;
using MEC;

namespace FakeRank;

public static class EventHandlers
{
    private static readonly Dictionary<string, (string, string)> FakeRanks = new();
    private static CoroutineHandle _coroutine;

    public static void RegisterEvents()
    {
        // Feel free to add more event registrations here
        PlayerEvents.Joined += OnJoined;

        //Apply fakerank every five seconds
        _coroutine = Timing.RunCoroutine(FakeRankLoop());
    }

    public static void UnregisterEvents()
    {
        PlayerEvents.Joined -= OnJoined;
        Timing.KillCoroutines(_coroutine);
    }

    private static IEnumerator<float> FakeRankLoop()
    {
        while (true)
        {
            foreach (KeyValuePair<string, (string, string)> rank in FakeRanks)
                if (Player.TryGet(rank.Key, out Player player))
                {
                    if (string.IsNullOrEmpty(player.GroupName))
                    {
                        player.GroupName = rank.Value.Item1 + " (Stammspieler)";
                        player.GroupColor = rank.Value.Item2;
                    }
                    else
                    {
                        if (player.GroupName.Contains("(")) continue;
                        player.GroupName = rank.Value.Item1 + " (" + player.GroupName + ")";
                        player.GroupColor = rank.Value.Item2;
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
        try
        {
            Config config = Plugin.Instance.Config!;
            string endpoint = $"{config.BackendURL}/fakerank?userid={Uri.EscapeDataString(userId)}";

            Logger.Debug($"Fetching FakeRank from endpoint: {endpoint}");

            using HttpClient client = new();
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", config.BackendAPIToken);

            HttpResponseMessage response = await client.GetAsync(endpoint);

            if (response.IsSuccessStatusCode)
            {
                string responseContent = await response.Content.ReadAsStringAsync();
                Logger.Debug($"Successfully fetched FakeRank for User ID: {userId}");

                // Parse the tuple response (fakerank,fakerank_color)
                string[] parts = responseContent.Split(',');
                if (parts.Length == 2)
                {
                    string fakeRankName = parts[0].Trim();
                    string fakeRankColor = parts[1].Trim();
                    FakeRanks[userId] = (fakeRankName, fakeRankColor);
                }
                else
                {
                    Logger.Debug(
                        $"Invalid response format for User ID: {userId}. Expected tuple, got: {responseContent}");
                    FakeRanks[userId] = (string.Empty, string.Empty);
                }
            }
            else
            {
                Logger.Debug($"Failed to fetch FakeRank for User ID: {userId}. Status: {response.StatusCode}");
                FakeRanks[userId] = (string.Empty, string.Empty);
            }
        }
        catch (Exception ex)
        {
            Logger.Debug($"Exception while fetching FakeRank for User ID {userId}: {ex}");
            FakeRanks[userId] = (string.Empty, string.Empty);
        }
    }
}