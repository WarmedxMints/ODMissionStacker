using ODMissionStacker.Utils;
using System.IO;

namespace ODMissionStacker.Settings
{
    public class AppSettings : PropertyChangeNotify
    {
        private readonly string settingsSaveFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "AppSettings.json");

        private DisplayMode viewDisplayMode;
        private GridSorting mainGridSorting;

        public DisplayMode ViewDisplayMode { get => viewDisplayMode; set { viewDisplayMode = value; OnPropertyChanged(); } }
        public GridSorting MainGridSorting { get => mainGridSorting; set { mainGridSorting = value; OnPropertyChanged(); } }
        public bool ShowClipBoard { get; set; }

        public void LoadSettings()
        {
            AppSettings settings = LoadSaveJson.LoadJson<AppSettings>(settingsSaveFile);

            if(settings is not null)
            {
                ViewDisplayMode = settings.ViewDisplayMode;
                MainGridSorting = settings.MainGridSorting;
                ShowClipBoard = settings.ShowClipBoard;
            }
        }

        public void SaveSettings()
        {
            _ = LoadSaveJson.SaveJson(this, settingsSaveFile);
        }
    }
}
