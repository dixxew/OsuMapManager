using OsuSharp.Domain;
using OsuSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsuSharp.Interfaces;
using System.IO;
using System.Threading;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OsuSharp.Exceptions;
using OsuSharp.JsonModels;
using System.Collections.Concurrent;
using System.Collections;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Net;
using System.Reflection;
using OsuSharp.Legacy;

namespace MapManager.OSU;
public class OsuService
{
    private readonly IOsuClient _client;
    private readonly LegacyOsuClient _legacyClient;
    private readonly AppSettings _appSettings;

    public OsuService(IOsuClient client, LegacyOsuClient legacyClient, AppSettings appSettings)
    {
        _client = client;
        _legacyClient = legacyClient;
        _appSettings = appSettings;
        ApplySettings();
    }

    private void ApplySettings()
    {
        _client.Configuration.ClientId = _appSettings.OsuClientId;
        _client.Configuration.ClientSecret = _appSettings.OsuClientSecret;
    }
    public void UpdateSettings(AppSettings updatedSettings)
    {
        // Set new settings
        _appSettings.OsuClientId = updatedSettings.OsuClientId;
        _appSettings.OsuClientSecret = updatedSettings.OsuClientSecret;
        ApplySettings();
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
            {
                break;
            }
        }
    }
    public async Task<IBeatmap> GetBeatmapByIdAsync(long id)
    {
        return await _client.GetBeatmapAsync(id);
    }
    public async Task<IBeatmapScores> GetBeatmapScoresByIdAsync(long id)
    {
            return await _client.GetBeatmapScoresAsync(id, gameMode: GameMode.Osu);        
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