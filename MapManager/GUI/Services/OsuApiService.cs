﻿using Avalonia.Media.Imaging;
using OsuSharp;
using OsuSharp.Domain;
using OsuSharp.Interfaces;
using OsuSharp.Legacy;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class OsuApiService
{
    private readonly IOsuClient _client;
    private readonly SettingsService _settingsService;

    public OsuApiService(IOsuClient client,  SettingsService settingsService)
    {
        _client = client;
        _settingsService = settingsService;
        _settingsService.OnOsuApiSettingsChanged += OnOsuApiSettingsChanged;
        UpdateClientSettings();
    }

    private readonly HttpClient _httpClient = new HttpClient();




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

    public async Task<Bitmap?> GetAvatarAsync(string username)
    {
        try
        {
            var url = await GetUserAvatarUrlAsync(username);
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                var bitmap = new Bitmap(stream);
                return bitmap;
            }
        }
        catch { }
        return null;
    }
}