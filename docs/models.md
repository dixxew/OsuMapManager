# Data Models

All models live in `MapManager/GUI/Models/`. Most inherit `ReactiveObject` so the UI binds to them directly.

## Beatmap Hierarchy

```
BeatmapSet          — one song folder (multiple difficulties)
  └── Beatmap[]     — one difficulty (.osu file)
```

**`BeatmapSet`** (`Models/BeatmapSet.cs`)
- Groups related `Beatmap` objects (same audio, different diffs)
- Has `IsFavorite` (reactive), artist/title metadata, background image path

**`Beatmap`** (`Models/Betmap.cs`)  
- Individual difficulty; maps 1:1 to a `BeatmapEntry` from HoLLy reader
- ReactiveObject properties: `DifficultyRating`, `CS`, `AR`, `HP`, `OD`, `BPM`, `Tags`, etc.
- Factory: `Beatmap.FromBeatmapEntry(BeatmapEntry entry)`

## Scores

**`Score`** (`Models/Score.cs`) — Local score from `scores.db`  
**`GlobalScore`** (`Models/GlobalScore.cs`) — API score with full stats: accuracy, mods, PP, user with avatar URL  
**`LocalScore`** (`Models/LocalScore.cs`) — Placeholder / legacy

## Collections

**`Collection`** (`Models/Collection.cs`)  
- `Name` (string) + `ObservableCollection<Beatmap>`
- Loaded from `collection.db`, mutations persisted by `CollectionService`

## Chat

Under `Models/Chat/`:
- `ChatChannel` — name + `ObservableCollection<ChatMessage>` + `ObservableCollection<ChatUser>`
- `ChatMessage` — sender, text, timestamp, `ChatMessageType`
- `ChatUser` — nickname, avatar bitmap
- `ChatMessageType` (enum) — Normal, Action, System, etc.

## Enums

**`Mods`** (`Models/Enums/Mods.cs`) — osu! mod flags with bit-mask values and display name mapping  
**`BeatmapsSearchModeEnum`** (`Models/Enums/BeatmapsSearchModeEnum.cs`) — All / Collections / Favorites
