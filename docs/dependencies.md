# Dependencies

## UI Framework

| Package | Version | Purpose |
|---|---|---|
| Avalonia | 11.2.3 | Cross-platform desktop UI (XAML-based) |
| Avalonia.ReactiveUI | 11.2.3 | ReactiveUI integration for Avalonia |
| SukiUI | 6.0.0-rc | Component library + theming (dialogs, notifications, theme) |
| IconPacks.Avalonia.FontAwesome | — | FA icon font |
| IconPacks.Avalonia.ForkAwesome | — | ForkAwesome icon font |
| Avalonia.Fonts.Inter | — | Inter typeface |

## Reactive / MVVM

| Package | Purpose |
|---|---|
| ReactiveUI | Base MVVM framework (`ReactiveObject`, `ReactiveCommand`) |
| DynamicData | Reactive collection extensions used internally by ReactiveUI |

## Application Infrastructure

| Package | Purpose |
|---|---|
| Microsoft.Extensions.DependencyInjection | DI container |
| Microsoft.Extensions.Hosting 9.0.1 | `IHostedService` lifecycle, generic host |
| AutoMapper 13.0.1 | DTO ↔ domain model mapping (profile in `MappingProfile.cs`) |
| Newtonsoft.Json 13.0.3 | Settings JSON serialization |

## osu! Data

| Package | Purpose |
|---|---|
| HoLLy.osu.DatabaseReader 3.3.0 | Reads binary `osu!.db`, `collection.db`, `scores.db` |
| OsuSharp (solution project) | osu! v2 API client (included as source, not NuGet) |

## Audio

| Package | Purpose |
|---|---|
| NAudio 2.2.1 | Audio decoding and playback (MP3, WAV) |
| NAudio.Vorbis 1.5.0 | OGG/Vorbis support for NAudio |
| SoundTouch.Net 2.3.2 | Real-time speed and pitch shifting |

## Chat

| Package | Purpose |
|---|---|
| SmartIrc4net 1.1.0 | IRC client library for osu! Bancho chat |

## OsuSharp (Solution Projects — source reference, not NuGet)

The solution includes OsuSharp as source projects:
- `OsuSharp` — main API client
- `OsuSharp.Domain` — domain models
- `OsuSharp.JsonModels` — JSON deserialization models
- `OsuSharp.Legacy` — legacy osu! API support
- `OsuSharp.Legacy.Oppai` — star rating / PP calculations
