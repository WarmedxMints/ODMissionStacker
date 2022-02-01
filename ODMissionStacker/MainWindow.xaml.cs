using ODMissionStacker.CustomMessageBox;
using ODMissionStacker.Missions;
using ODMissionStacker.Settings;
using ODMissionStacker.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ODMissionStacker
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        #region Property Changed
        // Declare the event
        public event PropertyChangedEventHandler PropertyChanged;

        // Create the OnPropertyChanged method to raise the event
        // The calling member's name will be used as the parameter.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            if (Dispatcher.CheckAccess())
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
                return;
            }

            Dispatcher.Invoke(() =>
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
            });
        }
        #endregion

        #region Custom Title Bar
        // Can execute
        private void CommandBinding_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        // Minimize
        private void CommandBinding_Executed_Minimize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        // Maximize
        private void CommandBinding_Executed_Maximize(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        // Restore
        private void CommandBinding_Executed_Restore(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        // Close
        private void CommandBinding_Executed_Close(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        // State change
        private void MainWindowStateChangeRaised(object sender, EventArgs e)
        {
            if (Settings is not null)
            {
                Settings.LastWindowPos.State = WindowState;
            }

            if (WindowState == WindowState.Maximized)
            {
                MainWindowBorder.BorderThickness = new Thickness(8);
                RestoreButton.Visibility = Visibility.Visible;
                MaximizeButton.Visibility = Visibility.Collapsed;
            }
            else
            {
                MainWindowBorder.BorderThickness = new Thickness(0);
                RestoreButton.Visibility = Visibility.Collapsed;
                MaximizeButton.Visibility = Visibility.Visible;
            }
        }
        #endregion

        public AppSettings Settings { get; set; } = new();

        private MissionsContainer missionsContainer;
        public MissionsContainer MissionsContainer { get => missionsContainer; set { missionsContainer = value; OnPropertyChanged(); } }

        public MainWindow()
        {
            missionsContainer = new(Settings);
            Settings.LoadSettings();
            InitializeComponent();
        }

        #region Window Methods
        private void SimpleCommand_OnExecuted(object sender, object e)
        {
            Settings.SetWindowPos();
            WindowState = WindowState.Normal;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ShowSiderBar(Settings.ShowClipBoard, "\xE974", "\xE973", ClipboardColumn, ClipboardExpander);
            ShowSiderBar(Settings.ShowBountyBoard, "\xE973", "\xE974", BountyColumn, BountyBoardExapander);
            MissionsContainer.Init();
            WindowState = Settings.LastWindowPos.State;
        }

        private void Root_Closing(object sender, CancelEventArgs e)
        {
            MissionsContainer.SaveData();
            Settings.SaveSettings();
        }
        #endregion

        #region Datagrid Load and Sort Methods
        private static void ItemContainerGenerator_StatusChanged(object sender, DataGrid dataGrid)
        {
            ItemContainerGenerator icg = (ItemContainerGenerator)sender;

            if (icg.Status == System.Windows.Controls.Primitives.GeneratorStatus.ContainersGenerated)
            {
                foreach (DataGridColumn col in dataGrid.Columns)
                {
                    DataGridLength width = col.Width;
                    col.Width = 0;
                    col.Width = width;
                }
            }
        }

        private void MissionDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            grid.Items.IsLiveSorting = true;
            SortMissionDataGrid(true);

            grid.ItemContainerGenerator.StatusChanged += (sender, e) => ItemContainerGenerator_StatusChanged(sender, grid);
        }

        private void SortMissionDataGrid(bool resize)
        {
            List<SortDescription> sortDescriptions = new();

            switch (Settings.MainGridSorting)
            {
                case GridSorting.System:
                    sortDescriptions.Add(new SortDescription("SourceSystem", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("IssuingFaction", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("ExpireTime", ListSortDirection.Ascending));
                    break;
                case GridSorting.Station:
                    sortDescriptions.Add(new SortDescription("SourceStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceSystem", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("IssuingFaction", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("ExpireTime", ListSortDirection.Ascending));
                    break;
                case GridSorting.Faction:
                    sortDescriptions.Add(new SortDescription("IssuingFaction", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceSystem", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("ExpireTime", ListSortDirection.Ascending));
                    break;
                case GridSorting.Target:
                    sortDescriptions.Add(new SortDescription("TargetFaction", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("IssuingFaction", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceSystem", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("ExpireTime", ListSortDirection.Ascending));
                    break;
                case GridSorting.Kills:
                    sortDescriptions.Add(new SortDescription("KillCount", ListSortDirection.Ascending));
                    break;
                case GridSorting.Reward:
                    sortDescriptions.Add(new SortDescription("Reward", ListSortDirection.Ascending));
                    break;
                case GridSorting.Expiry:
                    sortDescriptions.Add(new SortDescription("ExpireTime", ListSortDirection.Ascending));
                    break;
                case GridSorting.Wing:
                    sortDescriptions.Add(new SortDescription("Wing", ListSortDirection.Ascending));
                    break;
                default:
                    sortDescriptions.Add(new SortDescription("SourceSystem", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("SourceStation", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("IssuingFaction", ListSortDirection.Ascending));
                    sortDescriptions.Add(new SortDescription("ExpireTime", ListSortDirection.Ascending));
                    break;
            }

            SortDataGrid(MissionDataGrid, sortDescriptions, 2, resize);
        }

        private void TargetFactionGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            grid.Items.IsLiveSorting = true;
            SortTargetFactionGrid();

            grid.ItemContainerGenerator.StatusChanged += (sender, e) => ItemContainerGenerator_StatusChanged(sender, grid);
        }

        private void SortTargetFactionGrid()
        {
            List<SortDescription> sortDescriptions = new();

            sortDescriptions.Add(new SortDescription("TargetFaction", ListSortDirection.Ascending));

            SortDataGrid(TargetFactionGrid, sortDescriptions);
        }

        private void StackInfoGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            grid.Items.IsLiveSorting = true;
            SortStackInfoGrid();

            grid.ItemContainerGenerator.StatusChanged += (sender, e) => ItemContainerGenerator_StatusChanged(sender, grid);
        }

        private void SortStackInfoGrid()
        {
            List<SortDescription> sortDescriptions = new();

            sortDescriptions.Add(new SortDescription("TargetFaction", ListSortDirection.Ascending));
            sortDescriptions.Add(new SortDescription("IssuingFaction", ListSortDirection.Ascending));

            SortDataGrid(StackInfoGrid, sortDescriptions, 0);
        }

        private static void SortDataGrid(DataGrid dataGrid, List<SortDescription> sortDescriptions, int ignoreColoumn = -1, bool resize = true)
        {
            if (dataGrid == null)
            {
                return;
            }

            dataGrid.Items.SortDescriptions.Clear();
            // Add the new sort descriptions
            foreach (SortDescription sort in sortDescriptions)
            {
                dataGrid.Items.SortDescriptions.Add(sort);
            }

            // Refresh items to display sort
            dataGrid.Items.Refresh();

            if (resize == false)
            {
                return;
            }

            for (int i = 0; i < dataGrid.Columns.Count; i++)
            {
                if (i == ignoreColoumn)
                {
                    continue;
                }

                DataGridColumn column = dataGrid.Columns[i];

                column.Width = new DataGridLength(1.0, DataGridLengthUnitType.SizeToCells);

            }
        }

        private void BountyFeedGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = (DataGrid)sender;
            grid.Items.IsLiveSorting = true;

            List<SortDescription> sortDescriptions = new();

            sortDescriptions.Add(new SortDescription("TimeStamp", ListSortDirection.Descending));

            SortDataGrid(grid, sortDescriptions, -1, false);
        }

        private void ClipBoardDataGrid_Loaded(object sender, RoutedEventArgs e)
        {
            DataGrid grid = sender as DataGrid;

            grid.Items.IsLiveSorting = true;

            List<SortDescription> sortDescriptions = new();

            sortDescriptions.Add(new SortDescription("DestinationName", ListSortDirection.Ascending));
            sortDescriptions.Add(new SortDescription("SystemName", ListSortDirection.Ascending));
            sortDescriptions.Add(new SortDescription("StationName", ListSortDirection.Ascending));

            SortDataGrid(grid, sortDescriptions);
        }
        #endregion

        #region Button Methods
        private void KillCountBox_Click(object sender, RoutedEventArgs e)
        {
            MissionsContainer.CurrentManager.UpdateFactionInfo();

            MissionDataGrid.UnselectAllCells();
        }

        private void BountyBoardExapander_Click(object sender, RoutedEventArgs e)
        {
            Settings.ShowBountyBoard = !Settings.ShowBountyBoard;

            ShowSiderBar(Settings.ShowBountyBoard, "\xE973", "\xE974", BountyColumn, BountyBoardExapander);
        }

        private void AddClipboardSource_Click(object sender, RoutedEventArgs e)
        {
            AddClipboardSourceView clipboardSourceView = new()
            {
                Owner = this
            };

            _ = clipboardSourceView.ShowDialog();

            if ((bool)clipboardSourceView.DialogResult)
            {
                MissionData data = new()
                {
                    DestinationSystem = clipboardSourceView.TargetSystem,
                    SourceSystem = clipboardSourceView.SourceSystem,
                    SourceStation = clipboardSourceView.SourceStation
                };

                MissionsContainer.AddToMissionSourceClipBoard(data);
            }
        }

        private void PurgeCompleted_Click(object sender, RoutedEventArgs e)
        {
            var del = ODMessageBox.Show(this, "Purge All Completed Missions?", MessageBoxButton.YesNo);

            if (del == MessageBoxResult.Yes)
            {
                MissionsContainer.PurgeMissions(MissionState.Complete);
            }
        }

        private void PurgeAbandonded_Click(object sender, RoutedEventArgs e)
        {
            var del = ODMessageBox.Show(this, "Purge All Abandonded Missions?", MessageBoxButton.YesNo);

            if (del == MessageBoxResult.Yes)
            {
                MissionsContainer.PurgeMissions(MissionState.Abandonded);
            }
        }

        private void ReadHistory_Click(object sender, RoutedEventArgs e)
        {
            MissionHistoryReaderView ret = new(MissionsContainer)
            {
                Owner = this
            };

            if ((bool)ret.ShowDialog())
            {
                MissionsContainer.SaveData();
            }
        }

        private void KillsDownButton_Click(object sender, RoutedEventArgs e)
        {
            if (missionsContainer.CurrentManager == null)
            {
                return;
            }

            missionsContainer.CurrentManager.UpdateKills(-1);
        }

        private void KillsUpButton_Click(object sender, RoutedEventArgs e)
        {
            if (missionsContainer.CurrentManager == null)
            {
                return;
            }

            missionsContainer.CurrentManager.UpdateKills(1);
        }

        private void CopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            MissionData mission = (sender as Button).DataContext as MissionData;

            Helpers.SetClipboard(mission.SourceSystem);

            MissionDataGrid.UnselectAllCells();
        }

        private void ClipboardExpander_Click(object sender, RoutedEventArgs e)
        {
            Settings.ShowClipBoard = !Settings.ShowClipBoard;

            ShowSiderBar(Settings.ShowClipBoard, "\xE974", "\xE973", ClipboardColumn, ClipboardExpander);
        }

        private static void ShowSiderBar(bool show, string showText, string hideText, ColumnDefinition columnDefinition, Button button)
        {
            columnDefinition.Width = show ? new GridLength(1.0, GridUnitType.Auto) : new GridLength(0);

            button.Content = show ? showText : hideText;
        }
        #endregion

        #region DropDown Methods
        private void DisplayModeSelecter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            MissionsContainer?.SetCurrentManager();

            SortMissionDataGrid(true);
            SortStackInfoGrid();
            SortTargetFactionGrid();
        }

        private void SortingModeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SortMissionDataGrid(false);
        }
        #endregion

        #region Datagrid Mouse Enter/Leave Methods
        private void MissionsDateGridRow_MouseEnter(object sender, MouseEventArgs e)
        {
            if (StackInfoGrid == null || Settings.ViewDisplayMode == DisplayMode.Completed)
            {
                return;
            }

            string faction = ((MissionData)((DataGridRow)sender).DataContext).IssuingFaction;
            SolidColorBrush foreGround = Application.Current.FindResource("Foreground") as SolidColorBrush;

            for (int i = 0; i < StackInfoGrid.Items.Count; i++)
            {
                DataGridRow dgRow = (DataGridRow)StackInfoGrid.ItemContainerGenerator
                                               .ContainerFromIndex(i);
                if (dgRow == null)
                {
                    continue;
                }

                StackInfo stackinfo = (StackInfo)StackInfoGrid.Items[i];

                if (stackinfo.IssuingFaction == faction)
                {
                    dgRow.Foreground = Application.Current.FindResource("Highlighted") as SolidColorBrush;
                    continue;
                }
                dgRow.Foreground = foreGround;
            }
        }

        private void MissionsDateGridRow_MouseLeave(object sender, MouseEventArgs e)
        {
            if (StackInfoGrid == null || Settings.ViewDisplayMode == DisplayMode.Completed)
            {
                return;
            }

            SolidColorBrush foreGround = Application.Current.FindResource("Foreground") as SolidColorBrush;

            for (int i = 0; i < StackInfoGrid.Items.Count; i++)
            {
                DataGridRow dgRow = (DataGridRow)StackInfoGrid.ItemContainerGenerator
                                               .ContainerFromIndex(i);

                if (dgRow is null)
                {
                    continue;
                }

                dgRow.Foreground = foreGround;
            }
        }

        private void StackInfoGrid_MouseOver(object sender, MouseEventArgs e)
        {
            if (MissionDataGrid == null || Settings.ViewDisplayMode == DisplayMode.Completed)
            {
                return;
            }

            string faction = ((StackInfo)((DataGridRow)sender).DataContext).IssuingFaction;

            for (int i = 0; i < MissionDataGrid.Items.Count; i++)
            {
                MissionData missionData = (MissionData)MissionDataGrid.Items[i];

                missionData.Highlight = missionData.IssuingFaction == faction;
            }
        }

        private void StackInfoGrid_MouseLeave(object sender, MouseEventArgs e)
        {
            if (MissionDataGrid == null || Settings.ViewDisplayMode == DisplayMode.Completed)
            {
                return;
            }

            for (int i = 0; i < MissionDataGrid.Items.Count; i++)
            {
                MissionData missionData = (MissionData)MissionDataGrid.Items[i];

                missionData.Highlight = false;
            }
        }
        #endregion
    }
}