using AutoMapper;
using MapManager.GUI.Models;
using MapManager.OSU;
using OsuSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class RankingService
{
    private readonly OsuService _osuService;
    private readonly OsuDataReader OsuDataReader;
    private readonly Mapper _mapper;

    public RankingService(OsuService osuService, OsuDataReader osuDataReader, Mapper mapper)
    {
        _osuService = osuService;
        OsuDataReader = osuDataReader;
        _mapper = mapper;
    }

    internal List<Tuple<string, List<OsuParsers.Database.Objects.Score>>> GetAllLocalScores()
    {

        return OsuDataReader.GetScoresList();
    }

    internal async Task<List<GlobalScore>> GetGlobalRanksByBeatmapIdAsync(int beatmapId)
    {
        return (await _osuService.GetBeatmapScoresByIdAsync(beatmapId))
            .Scores.Select(s => _mapper.Map<GlobalScore>(s)).ToList();
    }

    internal async Task<List<GlobalScore>> GetLocalRanksByBeatmapIdAsync(int beatmapId)
    {
        return (await _osuService.GetBeatmapScoresByIdAsync(beatmapId))
            .Scores.Select(s => _mapper.Map<GlobalScore>(s)).ToList();
    }
}
