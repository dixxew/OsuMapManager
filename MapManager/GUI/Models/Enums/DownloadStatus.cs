namespace MapManager.GUI.Models.Enums;

public enum DownloadStatus
{
    AwaitingLookup, // MD5 known, BeatmapSetId not yet resolved
    Queued,
    Downloading,
    Completed,
    Failed,
    Cancelled
}
