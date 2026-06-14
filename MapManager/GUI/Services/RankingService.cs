using MapManager.GUI.Models;
using Microsoft.Extensions.Logging;
using osu_database_reader.Components.Player;
using OsuSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;

public class RankingService
{
    private readonly OsuApiService _osuApiService;
    private readonly OsuDataService _osuDataReader;
    private readonly CacheService _cacheService;
    private readonly ILogger<RankingService> _logger;

    public RankingService(OsuApiService osuService, OsuDataService osuDataReader, CacheService cacheService,
        ILogger<RankingService> logger)
    {
        _osuApiService = osuService;
        _osuDataReader = osuDataReader;
        _cacheService = cacheService;
        _logger = logger;
    }

    public List<Replay> GetAllLocalScores() => _osuDataReader.GetScoresList();

    public async Task<List<GlobalScore>> GetGlobalRanksByBeatmapIdAsync(int beatmapId)
    {
        _logger.LogDebug("GetGlobalRanksByBeatmapIdAsync(beatmapId={BeatmapId})", beatmapId);
        try
        {
            var entries = await _cacheService.GetJsonAsync<List<GlobalScoreCacheEntry>>(
                $"scores_{beatmapId}",
                async () =>
                {
                    var result = await _osuApiService.GetBeatmapScoresByIdAsync(beatmapId);
                    return result.Scores.Select(ToDto).ToList();
                });

            var scores = entries?.Select(FromDto).ToList() ?? [];
            _logger.LogDebug("GetGlobalRanksByBeatmapIdAsync(beatmapId={BeatmapId}) → {Count} scores", beatmapId, scores.Count);
            return scores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetGlobalRanksByBeatmapIdAsync failed for beatmapId={BeatmapId}", beatmapId);
            return [];
        }
    }

    private static GlobalScoreCacheEntry ToDto(IScore s) => new()
    {
        Id = s.Id,
        BestId = s.BestId,
        UserId = s.UserId,
        Accuracy = s.Accuracy,
        Mods = s.Mods.ToList(),
        TotalScore = s.TotalScore,
        MaxCombo = s.MaxCombo,
        Statistics = new GlobalScoreStatsCacheEntry
        {
            Count50 = s.Statistics?.Count50 ?? 0,
            Count100 = s.Statistics?.Count100 ?? 0,
            Count300 = s.Statistics?.Count300 ?? 0,
            CountGeki = s.Statistics?.CountGeki ?? 0,
            CountKatu = s.Statistics?.CountKatu ?? 0,
            CountMiss = s.Statistics?.CountMiss ?? 0,
        },
        PerformancePoints = s.PerformancePoints,
        Rank = s.Rank ?? "",
        GlobalRank = s.GlobalRank,
        User = s.User == null ? new() : new GlobalUserCacheEntry
        {
            Id = s.User.Id,
            Username = s.User.Username ?? "",
            CountryCode = s.User.CountryCode ?? "",
            AvatarUrl = s.User.AvatarUrl?.ToString() ?? "",
            IsActive = s.User.IsActive,
            IsBot = s.User.IsBot,
            IsOnline = s.User.IsOnline,
            IsSupporter = s.User.IsSupporter,
            LastVisit = s.User.LastVisit,
            PmFriendsOnly = s.User.PmFriendsOnly,
            ProfileColour = s.User.ProfileColour,
            DefaultGroup = s.User.DefaultGroup,
        },
    };

    private static GlobalScore FromDto(GlobalScoreCacheEntry d) => new()
    {
        Id = d.Id,
        BestId = d.BestId,
        UserId = d.UserId,
        Accuracy = d.Accuracy,
        Mods = d.Mods,
        TotalScore = d.TotalScore,
        MaxCombo = d.MaxCombo,
        Statistics = new GlobalScoreStatistics
        {
            Count50 = d.Statistics.Count50,
            Count100 = d.Statistics.Count100,
            Count300 = d.Statistics.Count300,
            CountGeki = d.Statistics.CountGeki,
            CountKatu = d.Statistics.CountKatu,
            CountMiss = d.Statistics.CountMiss,
        },
        PerformancePoints = d.PerformancePoints,
        Rank = d.Rank,
        GlobalRank = d.GlobalRank,
        User = new GlobalUserCompact
        {
            Id = d.User.Id,
            Username = d.User.Username,
            CountryCode = d.User.CountryCode,
            IsActive = d.User.IsActive,
            IsBot = d.User.IsBot,
            IsOnline = d.User.IsOnline,
            IsSupporter = d.User.IsSupporter,
            LastVisit = d.User.LastVisit,
            PmFriendsOnly = d.User.PmFriendsOnly,
            ProfileColour = d.User.ProfileColour ?? "",
            DefaultGroup = d.User.DefaultGroup ?? "",
            AvatarUrl = string.IsNullOrEmpty(d.User.AvatarUrl) ? null! : new Uri(d.User.AvatarUrl),
        },
    };
}
