using Avalonia.Media.Imaging;
using MapManager.GUI.Models;
using OsuSharp;
using OsuSharp.Domain;
using OsuSharp.Interfaces;
using OsuSharp.Legacy;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class OsuApiService
{
    private readonly IOsuClient _client;
    private readonly SettingsService _settingsService;
    private readonly ILogger<OsuApiService> _logger;

    public OsuApiService(IOsuClient client,  SettingsService settingsService, ILogger<OsuApiService> logger)
    {
        _client = client;
        _settingsService = settingsService;
        _logger = logger;
        _settingsService.OnOsuApiSettingsChanged += OnOsuApiSettingsChanged;
        UpdateClientSettings();
        _logger.LogInformation("OsuApiService initialized");
    }

    private readonly HttpClient _httpClient = new HttpClient();
    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    // ── Shared rate limiter for all osu! API (osu.ppy.sh) calls ─────────────────
    // Single global budget so every consumer (avatars, md5 lookups, comments, beatmap
    // fetches) shares one token bucket instead of each throttling independently.
    // Token bucket: burst up to MaxTokens, then refill RefillPerSecond (~60 req/min sustained,
    // matching osu!'s guidance). Image/CDN downloads are NOT gated — only API calls.
    private const double MaxTokens = 8;
    private const double RefillPerSecond = 1.0;
    private readonly SemaphoreSlim _rateLock = new(1, 1);
    private double _tokens = MaxTokens;
    private DateTime _lastRefill = DateTime.UtcNow;

    public async Task ThrottleAsync(CancellationToken ct = default)
    {
        while (true)
        {
            TimeSpan wait;
            await _rateLock.WaitAsync(ct);
            try
            {
                var now = DateTime.UtcNow;
                var elapsed = (now - _lastRefill).TotalSeconds;
                if (elapsed > 0)
                {
                    _tokens = Math.Min(MaxTokens, _tokens + elapsed * RefillPerSecond);
                    _lastRefill = now;
                }
                if (_tokens >= 1.0)
                {
                    _tokens -= 1.0;
                    return;
                }
                wait = TimeSpan.FromSeconds((1.0 - _tokens) / RefillPerSecond);
            }
            finally { _rateLock.Release(); }

            _logger.LogTrace("Rate limiter: out of tokens, waiting {WaitMs:F0}ms", wait.TotalMilliseconds);
            await Task.Delay(wait, ct);
        }
    }




    private void OnOsuApiSettingsChanged()
    {
        _logger.LogInformation("osu! API settings changed — updating client");
        UpdateClientSettings();
    }

    private void UpdateClientSettings()
    {

        _client.Configuration.ClientSecret = _settingsService.OsuClientSecret ?? "";
        _client.Configuration.ClientId = long.TryParse(_settingsService.OsuClientId, out var osuClientId)
            ? osuClientId
            : _client.Configuration.ClientId;
        _logger.LogDebug("osu! API client settings updated (clientId={ClientId}, secret={HasSecret})",
            _client.Configuration.ClientId, string.IsNullOrEmpty(_client.Configuration.ClientSecret) ? "no" : "yes");
    }

    public async IAsyncEnumerable<IBeatmapset> GetLastRankedBeatmapsetsAsync(int count)
    {
        _logger.LogDebug("GetLastRankedBeatmapsetsAsync(count={Count})", count);
        await ThrottleAsync();
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
        _logger.LogDebug("GetBeatmapByIdAsync(id={Id})", id);
        await ThrottleAsync();
        try
        {
            return await _client.GetBeatmapAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBeatmapByIdAsync failed for id={Id}", id);
            throw;
        }
    }
    public async Task<IBeatmapScores> GetBeatmapScoresByIdAsync(long id)
    {
        _logger.LogDebug("GetBeatmapScoresByIdAsync(id={Id})", id);
        await ThrottleAsync();
        try
        {
            var scores = await _client.GetBeatmapScoresAsync(id, gameMode: GameMode.Osu);
            _logger.LogTrace("GetBeatmapScoresByIdAsync(id={Id}) → {Count} scores", id, scores?.Scores?.Count ?? 0);
            return scores;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBeatmapScoresByIdAsync failed for id={Id}", id);
            throw;
        }
    }


    public async Task<IBeatmapset> GetBeatmapsetByIdAsync(long id)
    {
        _logger.LogDebug("GetBeatmapsetByIdAsync(id={Id})", id);
        await ThrottleAsync();
        try
        {
            return await _client.GetBeatmapsetAsync(id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GetBeatmapsetByIdAsync failed for id={Id}", id);
            throw;
        }
    }
    public async Task<string?> GetUserAvatarUrlAsync(string username)
    {
        try
        {
            await ThrottleAsync();
            var user = await _client.GetUserAsync(username);
            var url = user?.AvatarUrl?.ToString();
            _logger.LogDebug("Avatar URL for '{Username}': {Url}", username, url ?? "(none)");
            return url;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetUserAsync failed for '{Username}'", username);
            return null;
        }
    }

    public async Task<Bitmap?> DownloadAvatarFromUrlAsync(string url)
    {
        try
        {
            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                using var stream = await response.Content.ReadAsStreamAsync();
                _logger.LogTrace("Avatar downloaded from {Url}", url);
                return new Bitmap(stream);
            }
            _logger.LogWarning("Avatar download HTTP {Status} for URL: {Url}", (int)response.StatusCode, url);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "DownloadAvatarFromUrlAsync failed for {Url}", url);
        }
        return null;
    }

    public Task<(int beatmapId, int beatmapSetId, string title, string artist)?> LookupBeatmapByMd5Async(string md5) =>
        LookupBeatmapAsync($"checksum={md5}");

    public Task<(int beatmapId, int beatmapSetId, string title, string artist)?> LookupBeatmapByIdAsync(int beatmapId) =>
        LookupBeatmapAsync($"id={beatmapId}");

    private async Task<(int beatmapId, int beatmapSetId, string title, string artist)?> LookupBeatmapAsync(string queryParam)
    {
        try
        {
            await ThrottleAsync();
            var token = await _client.GetOrUpdateAccessTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://osu.ppy.sh/api/v2/beatmaps/lookup?{queryParam}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Beatmap lookup ({Query}) → HTTP {Status}", queryParam, (int)response.StatusCode);
                return null;
            }
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var id = root.GetProperty("id").GetInt32();
            var setId = root.GetProperty("beatmapset_id").GetInt32();
            var set = root.GetProperty("beatmapset");
            var title = set.GetProperty("title").GetString() ?? "";
            var artist = set.GetProperty("artist").GetString() ?? "";
            _logger.LogDebug("Beatmap lookup ({Query}) → set {SetId} '{Artist} - {Title}'", queryParam, setId, artist, title);
            return (id, setId, title, artist);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Beatmap lookup ({Query}) failed", queryParam);
            return null;
        }
    }

    public async Task<List<BeatmapCommentCacheEntry>> GetBeatmapsetCommentsAsync(long beatmapSetId)
    {
        try
        {
            _logger.LogDebug("GetBeatmapsetCommentsAsync(setId={SetId})", beatmapSetId);
            await ThrottleAsync();
            var token = await _client.GetOrUpdateAccessTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://osu.ppy.sh/api/v2/comments?commentable_type=beatmapset&commentable_id={beatmapSetId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Comments for set {SetId} → HTTP {Status}", beatmapSetId, (int)response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync();
            var bundle = JsonSerializer.Deserialize<CommentBundleJson>(json, _jsonOptions);
            if (bundle == null) return [];

            var userMap = bundle.Users.ToDictionary(u => u.Id);

            return bundle.Comments
                .Where(c => c.DeletedAt == null)
                .Select(c =>
                {
                    userMap.TryGetValue(c.UserId ?? 0, out var u);
                    return new BeatmapCommentCacheEntry
                    {
                        Id = c.Id,
                        ParentId = c.ParentId,
                        Message = c.Message ?? "",
                        CreatedAt = c.CreatedAt,
                        VotesCount = c.VotesCount,
                        RepliesCount = c.RepliesCount,
                        Pinned = c.Pinned,
                        UserId = c.UserId,
                        Username = u?.Username,
                        AvatarUrl = u?.AvatarUrl,
                    };
                })
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GetBeatmapsetCommentsAsync failed for set {SetId}", beatmapSetId);
            return [];
        }
    }

    private class CommentBundleJson
    {
        [JsonPropertyName("comments")]
        public List<CommentJson> Comments { get; set; } = [];
        [JsonPropertyName("users")]
        public List<CommentUserJson> Users { get; set; } = [];
    }

    private class CommentJson
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("parent_id")]
        public long? ParentId { get; set; }
        [JsonPropertyName("user_id")]
        public long? UserId { get; set; }
        [JsonPropertyName("message")]
        public string? Message { get; set; }
        [JsonPropertyName("created_at")]
        public DateTimeOffset CreatedAt { get; set; }
        [JsonPropertyName("votes_count")]
        public int VotesCount { get; set; }
        [JsonPropertyName("replies_count")]
        public int RepliesCount { get; set; }
        [JsonPropertyName("pinned")]
        public bool Pinned { get; set; }
        [JsonPropertyName("deleted_at")]
        public DateTimeOffset? DeletedAt { get; set; }
    }

    private class CommentUserJson
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }
        [JsonPropertyName("username")]
        public string Username { get; set; } = "";
        [JsonPropertyName("avatar_url")]
        public string AvatarUrl { get; set; } = "";
    }
}