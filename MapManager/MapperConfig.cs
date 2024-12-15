using AutoMapper;
using MapManager.GUI.Models;
using OsuSharp.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager;
public class MapperConfig
{
    public MapperConfiguration MapperConfiguration { get; set; }
    public MapperConfig()
    {
        MapperConfiguration = new MapperConfiguration(cfg =>
        {
            cfg.CreateMap<IScore, GlobalScore>();
            cfg.CreateMap<IUserCompact, GlobalUserCompact>();
            cfg.CreateMap<IStatistics, GlobalScoreStatistics>();
        });
    }
}
