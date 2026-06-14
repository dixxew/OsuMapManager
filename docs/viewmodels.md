# ViewModels

All ViewModels live in `MapManager/GUI/ViewModels/` and inherit `ViewModelBase` → `ReactiveObject`.

`ViewModelLocator` acts as a factory/resolver — it holds references to all VM instances and is itself registered in DI so Views can get their VM via constructor injection or the locator.

## Root / Shell

| ViewModel | Paired View | Notes |
|---|---|---|
| `MainWindowViewModel` | `MainWindow.axaml` | Root VM; holds `MainContent` and `DialogContent` regions |
| `MainViewModel` | `MainControl.axaml` | Main layout; left panel (beatmaps + search) + right panel (details) |
| `GreetingsViewModel` | `GreetingsControl.axaml` | First-run / settings-not-configured screen |

## Beatmap Area

| ViewModel | Notes |
|---|---|
| `BeatmapsViewModel` | Beatmap list; binds to `BeatmapDataService.FilteredBeatmapSets`; handles selection |
| `BeatmapsSearchViewModel` | Search text input + mode selector; writes to `BeatmapDataService.QueryText` |
| `SearchFiltersViewModel` | Filter panel (star range, mods, status, etc.) |
| `SearchOptionsViewModel` | Additional search config (sort order, etc.) |
| `MainBlockBeatmapViewModel` | Large central beatmap display (background, metadata, action buttons) |
| `BeatmapInfoViewModel` | Compact stats panel (AR, CS, OD, HP, BPM, length) |
| `BeatmapBlockCollectionsViewModel` | Shows which collections the selected beatmap belongs to |

## Collections

| ViewModel | Notes |
|---|---|
| `CollectionsViewModel` | Collection tree; delegates mutations to `CollectionService`; enqueues missing beatmaps via `BeatmapDownloadService.EnqueueByMd5` |
| `CollectionsSearchViewModel` | Filter collections by name |

## osu!pps (farm)

| ViewModel | Notes |
|---|---|
| `OsuPpsViewModel` | Ranked farm list from `OsuPpsService`; filtering/sorting |
| `OsuPpsFiltersViewModel` | Star/length/mods filters for the farm list |
| `OsuPpsEntryViewModel` | One farm row; `DownloadCommand` → `BeatmapDownloadService.EnqueueByBeatmapSetId` |

## Downloads

| ViewModel | Notes |
|---|---|
| `DownloadManagerViewModel` | Backs the downloads flyout. Exposes a DynamicData-sorted view of `BeatmapDownloadService.Downloads` (active on top), a progress summary (`SummaryText`, `OverallProgress`, error count), mirror/concurrency settings, and `ClearFinishedCommand` |
| `DownloadEntryViewModel` | One download row. Holds `Status`/`Progress`/`Error`, `StatusLabel`, and `CancelCommand` / `RetryCommand` / `RemoveCommand` gated by `CanCancel` / `CanRetry` / `CanRemove` |

## Scores

| ViewModel | Notes |
|---|---|
| `LocalScoresViewModel` | Scores from `scores.db` for the selected beatmap |
| `GlobalScoresViewModel` | Scores from osu! API; lazy-loaded on beatmap selection if API is configured |

## Audio / Utility

| ViewModel | Notes |
|---|---|
| `AudioPlayerViewModel` | Playback controls bound to `AudioPlayerService` |
| `SettingsViewModel` | API credentials, osu! directory, IRC settings; writes via `SettingsService` |
| `ChatViewModel` | IRC chat UI; bound to `ChatService` channels/messages |

## Dialogs

`TextBoxDialogViewModel` / `TextBoxDialogView.axaml` — generic single-input dialog pushed to `DialogContent` region.
