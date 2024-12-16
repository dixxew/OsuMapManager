using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MapManager.GUI.ViewModels;
public class ViewModelLocator
{
    public MainWindowViewModel MainWindowViewModel =>
        App.AppHost.Services.GetRequiredService<MainWindowViewModel>();

    public AudioPlayerViewModel AudioPlayerViewModel =>
        App.AppHost.Services.GetRequiredService<AudioPlayerViewModel>();

    public SettingsViewModel SettingsViewModel =>
        App.AppHost.Services.GetRequiredService<SettingsViewModel>();
}
