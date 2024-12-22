using OsuParsers.Enums.Database;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MapManager.GUI.ViewModels;
public class SearchFiltersViewModel : ViewModelBase
{
    public List<RankedStatus> TargetRankedStatusesList =>
        Enum.GetValues(typeof(RankedStatus)).Cast<RankedStatus>().ToList();

    
    private RankedStatus _targetRankedStatus;
    public RankedStatus TargetRankedStatus
    {
        get => _targetRankedStatus;
        set
        {
            this.RaiseAndSetIfChanged(ref _targetRankedStatus, value);
            OnFiltersChanged();
        }
    }

    private float _minStarRating = 0;
    public float MinStarRating
    {
        get => _minStarRating;
        set
        {
            this.RaiseAndSetIfChanged(ref _minStarRating, value);
            OnFiltersChanged();
        }
    }

    private float _maxStarRating = 10;
    public float MaxStarRating
    {
        get => _maxStarRating;
        set
        {
            this.RaiseAndSetIfChanged(ref _maxStarRating, value);
            OnFiltersChanged();
        }
    }

    private TimeSpan? _minDuration;
    public TimeSpan? MinDuration
    {
        get => _minDuration;
        set
        {
            this.RaiseAndSetIfChanged(ref _minDuration, value);
            OnFiltersChanged();
        }
    }

    private TimeSpan? _maxDuration;
    public TimeSpan? MaxDuration
    {
        get => _maxDuration;
        set
        {
            this.RaiseAndSetIfChanged(ref _maxDuration, value);
            OnFiltersChanged();
        }
    }

    private float _minAR = 0;
    public float MinAR
    {
        get => _minAR;
        set
        {
            this.RaiseAndSetIfChanged(ref _minAR, value);
            OnFiltersChanged();
        }
    }

    private float _maxAR = 10;
    public float MaxAR
    {
        get => _maxAR;
        set
        {
            this.RaiseAndSetIfChanged(ref _maxAR, value);
            OnFiltersChanged();
        }
    }

    private float _minCS = 0;
    public float MinCS
    {
        get => _minCS;
        set
        {
            this.RaiseAndSetIfChanged(ref _minCS, value);
            OnFiltersChanged();
        }
    }

    private float _maxCS = 10;
    public float MaxCS
    {
        get => _maxCS;
        set
        {
            this.RaiseAndSetIfChanged(ref _maxCS, value);
            OnFiltersChanged();
        }
    }

    private float _minOD = 0;
    public float MinOD
    {
        get => _minOD;
        set
        {
            this.RaiseAndSetIfChanged(ref _minOD, value);
            OnFiltersChanged();
        }
    }

    private float _maxOD = 10;
    public float MaxOD
    {
        get => _maxOD;
        set
        {
            this.RaiseAndSetIfChanged(ref _maxOD, value);
            OnFiltersChanged();
        }
    }

    private float _minHP = 0;
    public float MinHP
    {
        get => _minHP;
        set
        {
            this.RaiseAndSetIfChanged(ref _minHP, value);
            OnFiltersChanged();
        }
    }

    private float _maxHP = 10;
    public float MaxHP
    {
        get => _maxHP;
        set
        {
            this.RaiseAndSetIfChanged(ref _maxHP, value);
            OnFiltersChanged();
        }
    }

    private string _artist;
    public string Artist
    {
        get => _artist;
        set
        {
            this.RaiseAndSetIfChanged(ref _artist, value);
            OnFiltersChanged();
        }
    }

    private string _title;
    public string Title
    {
        get => _title;
        set
        {
            this.RaiseAndSetIfChanged(ref _title, value);
            OnFiltersChanged();
        }
    }

    private string _mapper;
    public string Mapper
    {
        get => _mapper;
        set
        {
            this.RaiseAndSetIfChanged(ref _mapper, value);
            OnFiltersChanged();
        }
    }

    private string _tags;
    public string Tags
    {
        get => _tags;
        set
        {
            this.RaiseAndSetIfChanged(ref _tags, value);
            TagList = value.Split(", ").ToList();
        }
    }

    private List<string> _tagList;
    public List<string> TagList
    {
        get => _tagList;
        set
        {
            this.RaiseAndSetIfChanged(ref _tagList, value);
            OnFiltersChanged();
        }
    }

    public SearchFiltersViewModel()
    {

    }

    public event Action FiltersChanged = null;
    private void OnFiltersChanged() => FiltersChanged?.Invoke();
}
