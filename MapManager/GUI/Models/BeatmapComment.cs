using System;

namespace MapManager.GUI.Models;

public class BeatmapComment
{
    public long Id { get; init; }
    public long? ParentId { get; init; }
    public string Message { get; init; } = "";
    public DateTimeOffset CreatedAt { get; init; }
    public int VotesCount { get; init; }
    public int RepliesCount { get; init; }
    public bool Pinned { get; init; }
    public GlobalUserCompact? User { get; init; }

    public bool HasVotes => VotesCount > 0;
    public bool HasReplies => RepliesCount > 0;

    public string TimeAgo
    {
        get
        {
            var diff = DateTimeOffset.UtcNow - CreatedAt;
            if (diff.TotalMinutes < 1) return "just now";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
            if (diff.TotalDays < 30) return $"{(int)diff.TotalDays}d ago";
            if (diff.TotalDays < 365) return $"{(int)(diff.TotalDays / 30)}mo ago";
            return $"{(int)(diff.TotalDays / 365)}y ago";
        }
    }
}

public class BeatmapCommentCacheEntry
{
    public long Id { get; set; }
    public long? ParentId { get; set; }
    public string Message { get; set; } = "";
    public DateTimeOffset CreatedAt { get; set; }
    public int VotesCount { get; set; }
    public int RepliesCount { get; set; }
    public bool Pinned { get; set; }
    public long? UserId { get; set; }
    public string? Username { get; set; }
    public string? AvatarUrl { get; set; }
}
