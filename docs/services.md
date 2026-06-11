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
| `AvatarService` | Services/AvatarService.cs | Downloads and caches user avatar bitmaps by user ID |

## External / Infrastructure

| Service | File | Responsibility |
|---|---|---|
| `OsuApiService` | Services/OsuApiService.cs | HTTP client for osu! v2 API (scores, beatmapset search). Uses OsuSharp under the hood |
| `AudioPlayerService` | Services/AudioPlayerService.cs | NAudio + SoundTouch playback; exposes `IsPlaying`, `Position`, `Duration`, speed/pitch control |
| `ChatService` | Services/ChatService.cs | SmartIrc4net IRC connection to osu! Bancho; exposes channels, messages, users as ObservableCollections |

## App Infrastructure

| Service | File | Responsibility |
|---|---|---|
| `NavigationService` | Services/NavigationService.cs | Named region navigation (`SetContent<T>`, `Subscribe`) |
| `SettingsService` | Services/SettingsService.cs | Thin wrapper around `AppSettings`; fires events on property change |
| `AppInitializationService` | Services/AppInitializationService.cs | `IHostedService` — loads databases on startup, triggers initial search |
| `AuxiliaryService` | Services/AuxiliaryService.cs | Misc utility helpers |
