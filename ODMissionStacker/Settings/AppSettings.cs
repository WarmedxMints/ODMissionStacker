using ODMissionStacker.Utils;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ODMissionStacker.Settings
{
    public class AppSettings : PropertyChangeNotify
    {
        public event EventHandler CommanderChanged;

        private readonly string settingsSaveFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "AppSettings.json");

        private DisplayMode viewDisplayMode;
        private GridSorting mainGridSorting;

        public DisplayMode ViewDisplayMode { get => viewDisplayMode; set { viewDisplayMode = value; OnPropertyChanged(); } }
        public GridSorting MainGridSorting { get => mainGridSorting; set { mainGridSorting = value; OnPropertyChanged(); } }
        public bool ShowClipBoard { get; set; }

        private Commander currentCommander; 
        public Commander CurrentCommander { get => currentCommander; set { currentCommander = value; OnPropertyChanged(); CommanderChanged?.Invoke(this, EventArgs.Empty); } }
        public ObservableCollection<Commander> Commanders { get; set; } = new();
        public void LoadSettings()
        {
            AppSettings settings = LoadSaveJson.LoadJson<AppSettings>(settingsSaveFile);

            if(settings is not null)
            {
                ViewDisplayMode = settings.ViewDisplayMode;
                MainGridSorting = settings.MainGridSorting;
                ShowClipBoard = settings.ShowClipBoard;

                if (settings.Commanders.Count > 0)
                {
                    foreach (Commander cmdr in settings.Commanders)
                    {
                        Commanders.Add(cmdr);
                    }

                    CurrentCommander = Commanders.FirstOrDefault(x => x.FID == settings.CurrentCommander.FID);
                }
            }
        }
     
        public void SaveSettings()
        {
            _ = LoadSaveJson.SaveJson(this, settingsSaveFile);
        }
    }
}
