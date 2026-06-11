# MapManager — Claude Code Reference

## Project

Windows desktop app for managing osu! beatmaps. Built with C# / .NET 8 / Avalonia 11 / ReactiveUI.

Full docs in `docs/`:
- `docs/architecture.md` — app structure, layers, navigation, bootstrap
- `docs/services.md` — all services and their responsibilities
- `docs/models.md` — data models
- `docs/viewmodels.md` — ViewModels and paired Views
- `docs/dependencies.md` — NuGet packages

## Solution Layout

```
MapManager.sln
├── MapManager/          ← the app (work here)
│   ├── Program.cs
│   ├── AppSettings.cs
│   ├── AppSettingsManager.cs
│   ├── MappingProfile.cs
│   └── GUI/
│       ├── Models/
│       ├── Services/
│       ├── ViewModels/
│       ├── Views/
│       ├── Dialogs/
│       └── Converters/
├── OsuSharp*/           ← external lib, do not modify
└── MapManagerService/   ← separate service project, do not modify
```

## Build & Run

```powershell
dotnet build MapManager/MapManager.csproj
dotnet run --project MapManager/MapManager.csproj
```

No test project exists yet.

## Key Conventions

### MVVM + ReactiveUI
- All models and ViewModels inherit `ReactiveObject`
- Properties use `this.RaiseAndSetIfChanged(ref _field, value)`
- Commands use `ReactiveCommand.Create` / `ReactiveCommand.CreateFromTask`
- UI updates from background threads: `Dispatcher.UIThread.Post(() => ...)`

### Dependency Injection
- All services are singletons registered in `Program.BuildHost()`
- New services go in `GUI/Services/` and are registered there
- ViewModels are resolved via `ViewModelLocator` (also registered in DI)

### Navigation
- Use `NavigationService.SetContent<TViewModel>()` to switch regions
- Region names: `MainContent`, `DialogContent`, `MainBlockContent`
- `ViewLocator` maps `XyzViewModel` → `XyzControl` / `XyzView` by naming convention

### Views
- Views are Avalonia `UserControl` (`.axaml` + `.axaml.cs`)
- Minimal code-behind — logic belongs in the ViewModel
- Converters in `GUI/Converters/`

### Settings
- Read/write settings through `SettingsService`, not `AppSettingsManager` directly
- `AppSettings` is the POCO; `appsettings.json` is the persisted file

## Architecture in One Sentence

`BeatmapDataService` is the central reactive state hub — nearly everything subscribes to or mutates it; services are singletons wired via DI; navigation is region-based via `NavigationService`.
