using ODMissionStacker.Utils;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Runtime.Serialization;
using System.Windows;

namespace ODMissionStacker.Settings
{
    public class AppSettings : PropertyChangeNotify
    {
        public event EventHandler CommanderChanged;

        private readonly string settingsSaveFile = Path.Combine(Directory.GetCurrentDirectory(), "Data", "AppSettings.json");

        private DisplayMode viewDisplayMode;
        private GridSorting mainGridSorting;
        private WindowPos lastWindowPos = new();

        public DisplayMode ViewDisplayMode { get => viewDisplayMode; set { viewDisplayMode = value; OnPropertyChanged(); } }
        public GridSorting MainGridSorting { get => mainGridSorting; set { mainGridSorting = value; OnPropertyChanged(); } }
        public bool ShowClipBoard { get; set; }
        public bool ShowBountyBoard { get; set; }
        public WindowPos LastWindowPos { get => lastWindowPos; set { lastWindowPos = value; OnPropertyChanged(); } }

        private Commander currentCommander;
        [IgnoreDataMember]
        public Commander CurrentCommander
        {
            get => currentCommander;
            set
            {
                if (currentCommander is null || currentCommander.FID != value.FID)
                {
                    currentCommander = value;
                    CommanderChanged?.Invoke(this, EventArgs.Empty);
                    OnPropertyChanged();
                }

            }
        }

        public ObservableCollection<Commander> Commanders { get; set; } = new();

        public void LoadSettings()
        {
            AppSettings settings = LoadSaveJson.LoadJson<AppSettings>(settingsSaveFile);

            if (settings is not null)
            {
                ViewDisplayMode = settings.ViewDisplayMode;
                MainGridSorting = settings.MainGridSorting;
                ShowClipBoard = settings.ShowClipBoard;
                ShowBountyBoard = settings.ShowBountyBoard;

                if (settings.Commanders.Count > 0)
                {
                    foreach (Commander cmdr in settings.Commanders)
                    {
                        Commanders.Add(cmdr);
                    }
                }

                if (settings.lastWindowPos != null)
                {
                    LastWindowPos = settings.LastWindowPos;
                    return;
                }
            }

            SetWindowPos();
        }

        public void SetWindowPos()
        {
            LastWindowPos.Width = 1320;
            LastWindowPos.Height = 850;

            double screenWidth = SystemParameters.PrimaryScreenWidth;
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            double windowWidth = LastWindowPos.Width;
            double windowHeight = LastWindowPos.Height;
            LastWindowPos.Left = (screenWidth / 2) - (windowWidth / 2);
            LastWindowPos.Top = (screenHeight / 2) - (windowHeight / 2);

            if (LastWindowPos.Height > SystemParameters.VirtualScreenHeight)
            {
                LastWindowPos.Height = SystemParameters.VirtualScreenHeight;
            }

            if (LastWindowPos.Width > SystemParameters.VirtualScreenWidth)
            {
                LastWindowPos.Width = SystemParameters.VirtualScreenWidth;
            }


        }
        public void SaveSettings()
        {
            _ = LoadSaveJson.SaveJson(this, settingsSaveFile);
        }
    }
}
