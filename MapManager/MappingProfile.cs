using AutoMapper;
using MapManager.GUI.Models;
using OsuSharp.Interfaces;

namespace MapManager;
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<IScore, GlobalScore>();
        CreateMap<IUserCompact, GlobalUserCompact>();
        CreateMap<IStatistics, GlobalScoreStatistics>();
    }
}
