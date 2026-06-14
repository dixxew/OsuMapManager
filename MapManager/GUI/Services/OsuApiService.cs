using Avalonia.Media.Imaging;
using MapManager.GUI.Models;
using OsuSharp;
using OsuSharp.Domain;
using OsuSharp.Interfaces;
using OsuSharp.Legacy;
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

    public OsuApiService(IOsuClient client,  SettingsService settingsService)
    {
        _client = client;
        _settingsService = settingsService;
        _settingsService.OnOsuApiSettingsChanged += OnOsuApiSettingsChanged;
        UpdateClientSettings();
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

            await Task.Delay(wait, ct);
        }
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
        await ThrottleAsync();
        return await _client.GetBeatmapAsync(id);
    }
    public async Task<IBeatmapScores> GetBeatmapScoresByIdAsync(long id)
    {
        await ThrottleAsync();
        var scores = await _client.GetBeatmapScoresAsync(id, gameMode: GameMode.Osu);
        return scores;
    }


    public async Task<IBeatmapset> GetBeatmapsetByIdAsync(long id)
    {
        await ThrottleAsync();
        return await _client.GetBeatmapsetAsync(id);
    }
    public async Task<string?> GetUserAvatarUrlAsync(string username)
    {
        try
        {
            await ThrottleAsync();
            var user = await _client.GetUserAsync(username);
            var url = user?.AvatarUrl?.ToString();
            Debug.WriteLine($"[OsuApi] avatar URL for '{username}': {url}");
            return url;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OsuApi] GetUserAsync failed for '{username}': {ex.GetType().Name}: {ex.Message}");
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
                return new Bitmap(stream);
            }
            Debug.WriteLine($"[OsuApi] avatar HTTP {(int)response.StatusCode} for URL: {url}");
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[OsuApi] DownloadAvatarFromUrlAsync failed: {ex.GetType().Name}: {ex.Message}");
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
            if (!response.IsSuccessStatusCode) return null;
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = doc.RootElement;
            var id = root.GetProperty("id").GetInt32();
            var setId = root.GetProperty("beatmapset_id").GetInt32();
            var set = root.GetProperty("beatmapset");
            var title = set.GetProperty("title").GetString() ?? "";
            var artist = set.GetProperty("artist").GetString() ?? "";
            return (id, setId, title, artist);
        }
        catch { return null; }
    }

    public async Task<List<BeatmapCommentCacheEntry>> GetBeatmapsetCommentsAsync(long beatmapSetId)
    {
        try
        {
            await ThrottleAsync();
            var token = await _client.GetOrUpdateAccessTokenAsync();
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://osu.ppy.sh/api/v2/comments?commentable_type=beatmapset&commentable_id={beatmapSetId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);

            using var response = await _httpClient.SendAsync(request);
            if (!response.IsSuccessStatusCode) return [];

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
        catch
        {
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