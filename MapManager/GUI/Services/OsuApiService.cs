using OsuSharp;
using OsuSharp.Domain;
using OsuSharp.Interfaces;
using OsuSharp.Legacy;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class OsuApiService
{
    private readonly IOsuClient _client;
    private readonly LegacyOsuClient _legacyClient;
    private readonly SettingsService _settingsService;

    public OsuApiService(IOsuClient client, LegacyOsuClient legacyClient, SettingsService settingsService)
    {
        _client = client;
        _legacyClient = legacyClient;
        _settingsService = settingsService;
        _settingsService.OnOsuApiSettingsChanged += OnOsuApiSettingsChanged;
        UpdateClientSettings();
    }

    private void OnOsuApiSettingsChanged()
    {
        UpdateClientSettings();
    }

    private void UpdateClientSettings()
    {
        
        _client.Configuration.ClientSecret = _settingsService.OsuClientSecret ?? "";
        _client.Configuration.ClientId = long.TryParse(_settingsService.OsuClientId, out var osuClientId) 
            ? osuClientId 
            : _client.Configuration.ClientId;
    }

    public async IAsyncEnumerable<IBeatmapset> GetLastRankedBeatmapsetsAsync(int count)
    {
        var builder = new BeatmapsetsLookupBuilder()
            .WithGameMode(GameMode.Osu)
            .WithConvertedBeatmaps()
            .WithCategory(BeatmapsetCategory.Ranked);

        await foreach (var beatmap in _client.EnumerateBeatmapsetsAsync(builder, BeatmapSorting.Ranked_Desc))
        {
            yield return beatmap;

            count--;
            if (count == 0)
                break;
        }
    }
    public async Task<IBeatmap> GetBeatmapByIdAsync(long id)
    {
        return await _client.GetBeatmapAsync(id);
    }
    public async Task<IBeatmapScores> GetBeatmapScoresByIdAsync(long id)
    {
        var scores = await _client.GetBeatmapScoresAsync(id, gameMode: GameMode.Osu);
        return scores;
    }


    public async Task<IBeatmapset> GetBeatmapsetByIdAsync(long id)
    {
        return await _client.GetBeatmapsetAsync(id);
    }
    public async Task<string> GetUserAvatarUrlAsync(string username)
    {
        var user = await _client.GetUserAsync(username);
        return user.AvatarUrl.ToString();
    }
}