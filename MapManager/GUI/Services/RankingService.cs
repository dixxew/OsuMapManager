using AutoMapper;
using MapManager.GUI.Models;
using osu_database_reader.Components.Player;
using OsuSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.Services;
public class RankingService
{
    private readonly OsuApiService _osuApiService;
    private readonly OsuDataService OsuDataReader;
    private readonly IMapper _mapper;

    public RankingService(OsuApiService osuService, OsuDataService osuDataReader, IMapper mapper)
    {
        _osuApiService = osuService;
        OsuDataReader = osuDataReader;
        _mapper = mapper;
    }

    public List<Replay> GetAllLocalScores()
    {

        return OsuDataReader.GetScoresList();
    }

    public async Task<List<GlobalScore>> GetGlobalRanksByBeatmapIdAsync(int beatmapId)
    {
        return (await _osuApiService.GetBeatmapScoresByIdAsync(beatmapId))
            .Scores.Select(s => _mapper.Map<GlobalScore>(s)).ToList();
    }
}
