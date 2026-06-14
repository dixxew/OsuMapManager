# Architecture Overview

## What is MapManager

MapManager is a Windows desktop application for managing osu! beatmaps. It reads osu!'s local database files (`osu!.db`, `collection.db`, `scores.db`), lets users browse/filter/search beatmaps, manage collections, preview audio, view local and global scores, and chat via osu! IRC.

**Stack:** C# / .NET 8.0 / Avalonia 11 / ReactiveUI / Microsoft.Extensions.DI

## Layer Model

```
Views (axaml + code-behind)
  ↕ data binding
ViewModels (ReactiveObject)
  ↕ service calls
Services (singletons via DI)
  ↕
Models (ReactiveObject data classes)
```

All layers live under `MapManager/GUI/`. Root-level files (`Program.cs`, `AppSettings.cs`, `AppSettingsManager.cs`, `MappingProfile.cs`) handle bootstrapping.

## Application Bootstrap

`Program.cs` does two things:
1. Builds the Avalonia app (`BuildAvaloniaApp`)
2. Builds a `Microsoft.Extensions.Hosting` host (`BuildHost`) that registers all services as singletons and runs `AppInitializationService` as a hosted background service

`AppInitializationService` (IHostedService) loads all databases on startup, then triggers the initial search.

## Navigation

`NavigationService` manages named content regions (`MainContent`, `DialogContent`, `MainBlockContent`). ViewModels call `NavigationService.SetContent<TViewModel>()` to switch the active view. The `ViewLocator` resolves the corresponding View by naming convention (`XyzViewModel` → `XyzControl` or `XyzView`).

## Reactive State

All models and ViewModels inherit `ReactiveObject`. Properties use `this.RaiseAndSetIfChanged()`. `BeatmapDataService` is the central reactive state hub — it holds `FilteredBeatmapSets`, `SelectedBeatmapSet`, `SelectedBeatmap`, `QueryText`, and exposes `Search()`.

## Settings

`AppSettings` is a plain POCO. `AppSettingsManager` serializes it to/from `appsettings.json`. `SettingsService` wraps it for runtime access and fires change events that trigger service reconfigurations (e.g. reconnect IRC, reload osu! directory).

## Audio

`AudioPlayerService` uses **NAudio** for playback and **SoundTouch.Net** for speed/pitch shifting. A 200 ms timer posts progress updates to the UI thread.

## Chat

`ChatService` connects to osu! IRC via **SmartIrc4net**. Channel list, messages, and users are exposed as `ObservableCollection` properties that the `ChatViewModel` binds to directly.

## osu! API access & rate limiting

`OsuApiService` is the single entry point for the osu! v2 API and owns the **one** rate limiter for the whole app — a shared token bucket (`ThrottleAsync`). Every consumer goes through it; no service throttles independently. CDN/image downloads are not gated.

## Beatmap downloads

`BeatmapDownloadService` runs a persistent download queue for beatmapsets that are missing from collections or picked from the osu!pps farm tab. It resolves MD5 hashes to beatmapset ids via the API, downloads `.osz` files from public mirrors with concurrency limited by settings, and persists state under `%LocalAppData%/MapManager`. The UI is a title-bar flyout (`DownloadManagerControl`) with an active-download badge. See `docs/services.md` for internals. Any service doing fire-and-forget persistence serializes its `File.WriteAllTextAsync` with a `SemaphoreSlim(1,1)`.

## Theming

SukiUI (6.0.0-rc) provides the base theme. Custom accent: MediumPurple + DarkBlue, configured in `App.axaml.cs`. Icon packs: FontAwesome + ForkAwesome via `IconPacks.Avalonia`.
