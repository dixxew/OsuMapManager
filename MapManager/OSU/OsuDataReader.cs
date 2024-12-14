// OsuDataReader.cs
using OsuParsers.Beatmaps;
using OsuParsers.Decoders;
using OsuParsers.Database;
using OsuParsers.Replays;
using OsuParsers.Storyboards;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using OsuParsers.Database.Objects;
using MapManager;

public class OsuDataReader
{
    private readonly string osuDirectory;
    private readonly string osuDbPath;

    public OsuDataReader()
    {
        this.osuDirectory = AppStore.OsuDirectory;
        osuDbPath = Path.Combine(osuDirectory, "osu!.db");
    }

    public List<DbBeatmap> GetBeatmapList()
    {
        var osuDb = DatabaseDecoder.DecodeOsu(osuDbPath);
        var beatmapPaths = new List<string>();

        return osuDb.Beatmaps;
    }

    public Beatmap ReadBeatmap(string beatmapPath)
    {
        return BeatmapDecoder.Decode(beatmapPath);
    }
    public static string? GetBeatmapImage(string beatmapFolder)
    {
        // Список допустимых расширений изображений
        var imageExtensions = new[] { ".jpg", ".png", ".jpeg" };

        // Получаем все файлы изображений из папки
        var imageFiles = Directory.GetFiles(Path.Combine(AppStore.OsuDirectory, "Songs", beatmapFolder))
                                  .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()));

        // Находим файл с наибольшим размером
        var largestImage = imageFiles
            .Select(file => new FileInfo(file))
            .OrderByDescending(fileInfo => fileInfo.Length)
            .FirstOrDefault();

        return largestImage?.FullName;
    }

    public void ModifyBeatmapSettings(string beatmapPath, float cs, float ar, float od, string[] tags)
    {
        var beatmap = BeatmapDecoder.Decode(beatmapPath);
        beatmap.DifficultySection.CircleSize = cs;
        beatmap.DifficultySection.ApproachRate = ar;
        beatmap.DifficultySection.OverallDifficulty = od;
        beatmap.MetadataSection.Tags = tags;

        beatmap.Save(beatmapPath);
    }

    public void SaveModifiedBeatmap(Beatmap beatmap, string newPath)
    {
        beatmap.Save(newPath);
    }

    public Replay ReadReplay(string replayPath)
    {
        return ReplayDecoder.Decode(replayPath);
    }

    public string GetReplayPlayer(string replayPath)
    {
        var replay = ReplayDecoder.Decode(replayPath);
        return replay.PlayerName;
    }

    public Storyboard ReadStoryboard(string storyboardPath)
    {
        return StoryboardDecoder.Decode(storyboardPath);
    }

    public void ModifyReplayPlayer(string replayPath, string newPlayerName)
    {
        var replay = ReplayDecoder.Decode(replayPath);
        replay.PlayerName = newPlayerName;
        replay.Save(replayPath);
    }
}
