# Services

All services are registered as singletons in `Program.BuildHost()` unless noted.

## Data / IO

| Service | File | Responsibility |
|---|---|---|
| `OsuDataService` | Services/OsuDataService.cs | Reads `osu!.db`, `collection.db`, `scores.db` via HoLLy.osu.DatabaseReader |
| `BeatmapDataService` | Services/BeatmapDataService.cs | Central reactive state: holds `FilteredBeatmapSets`, `SelectedBeatmap`, runs `Search()` / filtering |
| `FavoriteBeatmapManager` | Services/FavoriteBeatmapManager.cs | Persists favorites list to `favorites.json` |

## Business Logic

| Service | File | Responsibility |
|---|---|---|
| `BeatmapService` | Services/BeatmapService.cs | Beatmap presentation helpers (background images, collection membership) |
| `CollectionService` | Services/CollectionService.cs | Add/remove beatmaps from collections, create/delete collections |
| `RankingService` | Services/RankingService.cs | Aggregates local scores and global API scores for the selected beatmap |
| `AvatarService` | Services/AvatarService.cs | Downloads and caches user avatar bitmaps (priority queue, slot limit, stale-then-fresh). API rate limiting lives in `OsuApiService`, not here |
| `OsuPpsService` | Services/OsuPpsService.cs | Loads osu-pps `diffs.csv` / `mapsets.csv` (24h cache), computes farm scores and a ranked list |
| `BeatmapDownloadService` | Services/BeatmapDownloadService.cs | Beatmapset download queue: mirror fallback, MD5→set resolution, concurrency cap, persistence (see below) |

## External / Infrastructure

| Service | File | Responsibility |
|---|---|---|
| `OsuApiService` | Services/OsuApiService.cs | HTTP client for osu! v2 API (scores, beatmapset search, MD5/id lookup, comments). Uses OsuSharp under the hood. **Owns the single shared rate limiter** (`ThrottleAsync`) for all osu.ppy.sh calls |
| `AudioPlayerService` | Services/AudioPlayerService.cs | NAudio + SoundTouch playback; exposes `IsPlaying`, `Position`, `Duration`, speed/pitch control |
| `ChatService` | Services/ChatService.cs | SmartIrc4net IRC connection to osu! Bancho; exposes channels, messages, users as ObservableCollections |

## App Infrastructure

| Service | File | Responsibility |
|---|---|---|
| `NavigationService` | Services/NavigationService.cs | Named region navigation (`SetContent<T>`, `Subscribe`) |
| `SettingsService` | Services/SettingsService.cs | Thin wrapper around `AppSettings`; fires events on property change |
| `AppInitializationService` | Services/AppInitializationService.cs | `IHostedService` — loads databases on startup, triggers initial search |
| `AuxiliaryService` | Services/AuxiliaryService.cs | Misc utility helpers |

## osu! API rate limiting

All calls to `osu.ppy.sh` go through a **single shared token bucket** in `OsuApiService.ThrottleAsync()` (burst up to 8, refill ~1/sec ≈ 60 req/min). Every public API method awaits it. Consumers (`AvatarService`, `BeatmapDownloadService` lookup loop, comments, beatmap fetches) must **not** add their own throttle — the global budget is shared across the whole app. Image/CDN downloads (e.g. `DownloadAvatarFromUrlAsync`) are intentionally **not** gated.

## BeatmapDownloadService internals

Central download hub for missing-from-collection and osu!pps beatmapsets.

- **Queue** — `ObservableCollection<DownloadEntryViewModel> Downloads` (UI-thread only). Event-driven scheduler (`_kick` semaphore, no idle polling) starts up to `SettingsService.MaxConcurrentDownloads` at once.
- **Mirrors** — tries catboy.best / beatconnect.io / osu.direct in order of `SettingsService.PreferredMirror`. Downloads to `<osu>/Downloads/<setId>.osz` via a `.part` temp file, moved on success and deleted on cancel/fail.
- **Enqueue paths** — `EnqueueByBeatmapSetId` (osu!pps), `EnqueueByMd5` (collections; creates an `AwaitingLookup` entry promoted to `Queued` once the MD5 resolves), `EnqueueDownload` (resolved id).
- **MD5 lookup loop** — resolves `MissingBeatmap.MD5Hash` → beatmapset via `OsuApiService.LookupBeatmapByMd5Async`, with per-item backoff so unresolvable hashes don't stall the queue. Results cached to `md5-lookup-cache.json`.
- **Persistence** — queue → `downloads.json`, lookup cache → `md5-lookup-cache.json` (under `%LocalAppData%/MapManager`). All saves are serialized with a `SemaphoreSlim` (see convention below). `IDisposable` cancels the background loops on shutdown.
- **Thread-safety** — `_lookupCache` / `_md5PendingDownloads` are guarded by `_lookupSync`; collection snapshots for saving are taken on the UI thread.

## Concurrent file writes

Services that persist via fire-and-forget (`_ = SaveAsync()`) **must serialize writes** to avoid colliding on the same file. The pattern is a `SemaphoreSlim(1,1)` around the `File.WriteAllTextAsync` call. Applied in: `AppSettingsManager` (`appsettings.json`), `BeatmapDownloadService` (`downloads.json`, `md5-lookup-cache.json`), `AvatarService` (avatar `url_map.json`).
