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
using System;

public class OsuDataReader
{
    private readonly string osuDirectory;
    private readonly string osuDbPath;
    private readonly string scoresDbPath;

    public OsuDataReader()
    {
        this.osuDirectory = AppStore.OsuDirectory;
        osuDbPath = Path.Combine(osuDirectory, "osu!.db");
        scoresDbPath = Path.Combine(osuDirectory, "scores.db");
    }

    public List<DbBeatmap> GetBeatmapList()
    {
        var osuDb = DatabaseDecoder.DecodeOsu(osuDbPath);

        return osuDb.Beatmaps;
    }

    public List<Tuple<string, List<Score>>> GetScoresList()
    {
        var scoresDb = DatabaseDecoder.DecodeScores(scoresDbPath);

        return scoresDb.Scores;
    }

    public Beatmap ReadBeatmap(string beatmapPath)
    {
        return BeatmapDecoder.Decode(beatmapPath);
    }
    public static string GetBeatmapImage(string beatmapFolder, string beatmapFileName)
    {
        // Путь к файлу карты
        var beatmapPath = Path.Combine(AppStore.OsuDirectory, "Songs", beatmapFolder, beatmapFileName);

        // Список допустимых расширений изображений
        var imageExtensions = new[] { ".jpg", ".png", ".jpeg" };

        try
        {
            if (File.Exists(beatmapPath))
            {
                // Читаем строки из файла .osu
                var lines = File.ReadAllLines(beatmapPath);

                // Ищем блок [Events] и путь к картинке
                var eventIndex = Array.FindIndex(lines, line => line.Trim() == "[Events]");
                if (eventIndex >= 0)
                {
                    for (int i = eventIndex + 1; i < lines.Length; i++)
                    {
                        var line = lines[i].Trim();

                        // Ищем строку с 0,0,"path/to/image.jpg"
                        if (line.StartsWith("0,0,\""))
                        {
                            // Извлекаем путь к картинке
                            var startIndex = line.IndexOf("\"") + 1;
                            var endIndex = line.LastIndexOf("\"");
                            if (startIndex > 0 && endIndex > startIndex)
                            {
                                var relativeImagePath = line.Substring(startIndex, endIndex - startIndex);

                                // Полный путь к картинке
                                var fullImagePath = Path.Combine(AppStore.OsuDirectory, "Songs", beatmapFolder, relativeImagePath);

                                if (File.Exists(fullImagePath))
                                {
                                    return fullImagePath;
                                }
                            }
                        }
                    }
                }
            }

            // Если путь из файла .osu не найден, ищем картинку по названию в папке
            var imageFiles = Directory.GetFiles(Path.Combine(AppStore.OsuDirectory, "Songs", beatmapFolder), "*.*", SearchOption.AllDirectories)
                                      .Where(file => imageExtensions.Contains(Path.GetExtension(file).ToLower()));

            // Находим файл с наибольшим размером
            var largestImage = imageFiles
                .Select(file => new FileInfo(file))
                .OrderByDescending(fileInfo => fileInfo.Length)
                .FirstOrDefault();

            return largestImage?.FullName ?? "avares://MapManager/GUI/Assets/defaultBg.jpg";
        }
        catch (Exception ex)
        {
            // Логируем ошибки
            Console.WriteLine($"Ошибка при поиске изображения для карты {beatmapFileName}: {ex.Message}");

            // Возвращаем путь к изображению по умолчанию
            return "avares://MapManager/GUI/Assets/defaultBg.jpg";
        }
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
