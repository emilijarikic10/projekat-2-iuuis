using System;
using System.Linq;
using NetworkService.Model;
using NetworkService.Services;
using NetworkService.Helpers;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Diagnostics;

namespace NetworkService.ViewModel
{
    public class MainWindowViewModel : BaseViewModel
    {
        public ObservableCollection<ServerEntity> Entities { get; set; }
        public ObservableCollection<ServerEntity> FilteredEntities { get; set; }
        public ObservableCollection<ServerType> Types { get; set; }

        public ObservableCollection<string> SavedSearches { get; set; }

        public NewEntityForm CurrentForm { get; set; }

        private readonly SimulatorCommunicationService simulatorCommunication;

        private ServerEntity lastDeletedEntity;
        private ServerEntity lastAddedEntity;
        private string lastAction = "";

        public event Action<int> EntityDeleted;

        private ServerEntity selectedEntity;
        public ServerEntity SelectedEntity
        {
            get { return selectedEntity; }
            set { selectedEntity = value; OnPropertyChanged("SelectedEntity"); }
        }

        private string searchText;
        public string SearchText
        {
            get { return searchText; }
            set { searchText = value; OnPropertyChanged("SearchText"); }
        }

        private string selectedSavedSearch;
        public string SelectedSavedSearch
        {
            get { return selectedSavedSearch; }
            set
            {
                selectedSavedSearch = value;
                OnPropertyChanged("SelectedSavedSearch");

                if (!string.IsNullOrWhiteSpace(selectedSavedSearch))
                    ApplySavedSearch(selectedSavedSearch);
            }
        }

        private bool searchByName = true;
        public bool SearchByName
        {
            get { return searchByName; }
            set { searchByName = value; OnPropertyChanged("SearchByName"); }
        }

        private bool searchByType;
        public bool SearchByType
        {
            get { return searchByType; }
            set { searchByType = value; OnPropertyChanged("SearchByType"); }
        }

        public MyICommand AddCommand { get; set; }
        public MyICommand DeleteCommand { get; set; }
        public MyICommand SearchCommand { get; set; }
        public MyICommand ResetSearchCommand { get; set; }
        public MyICommand UndoCommand { get; set; }
        public MyICommand SaveSearchCommand { get; set; }

        public MainWindowViewModel()
        {
            Types = new ObservableCollection<ServerType>()
            {
                new ServerType("Web Server", "/Images/server.png"),
                new ServerType("File Server", "/Images/file.png"),
                new ServerType("Database Server", "/Images/database.png")
            };

            Entities = new ObservableCollection<ServerEntity>();
            FilteredEntities = new ObservableCollection<ServerEntity>();
            SavedSearches = new ObservableCollection<string>();
            CurrentForm = new NewEntityForm();

            LoadData();
            LoadSavedSearches();
            RefreshTable();

            AddCommand = new MyICommand(AddEntity);
            DeleteCommand = new MyICommand(DeleteEntity);
            SearchCommand = new MyICommand(Search);
            ResetSearchCommand = new MyICommand(ResetSearch);
            UndoCommand = new MyICommand(UndoLastAction);
            SaveSearchCommand = new MyICommand(SaveSearch);

            simulatorCommunication = new SimulatorCommunicationService(() => Entities.Count);
            simulatorCommunication.MeasurementReceived += SimulatorCommunication_MeasurementReceived;
            simulatorCommunication.Start();

            RestartMeteringSimulator();
        }

        private void LoadData()
        {
            Entities.Add(new ServerEntity() { Id = 1, Name = "Web01", IpAddress = "192.168.1.10", Type = Types[0], Value = 60 });
            Entities.Add(new ServerEntity() { Id = 2, Name = "File01", IpAddress = "192.168.1.11", Type = Types[1], Value = 55 });
            Entities.Add(new ServerEntity() { Id = 3, Name = "DB01", IpAddress = "192.168.1.12", Type = Types[2], Value = 70 });
        }

        private void SimulatorCommunication_MeasurementReceived(object sender, MeasurementReceivedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.EntityIndex < 0 || e.EntityIndex >= Entities.Count)
                    return;

                ServerEntity entity = Entities[e.EntityIndex];
                entity.Value = e.Value;

                WriteLog("Measurement for " + entity.Name + " (index " + e.EntityIndex + ") = " + entity.Value + "%");

                RefreshTable();
            });
        }

        private void AddEntity()
        {
            CurrentForm.Validate();

            if (!CurrentForm.IsValid)
            {
                NotificationHelper.ShowWarning("Add failed", "Please select a server type first.");
                return;
            }

            int newId = Entities.Count == 0 ? 1 : Entities.Max(x => x.Id) + 1;
            string serverName = CurrentForm.SelectedType.Name.Replace(" ", "") + newId;

            ServerEntity newEntity = new ServerEntity()
            {
                Id = newId,
                Name = serverName,
                IpAddress = "192.168.1." + (10 + newId),
                Type = CurrentForm.SelectedType,
                Value = 50
            };

            Entities.Add(newEntity);

            lastAddedEntity = newEntity;
            lastDeletedEntity = null;
            lastAction = "Add";

            WriteLog("Added entity " + serverName);
            NotificationHelper.ShowSuccess("Server added", serverName + " was added to the system.");

            RefreshTable();
            RestartMeteringSimulator();
        }

        private void DeleteEntity()
        {
            if (SelectedEntity == null)
            {
                NotificationHelper.ShowWarning("Delete failed", "Select a server in the table before deleting.");
                return;
            }

            lastDeletedEntity = SelectedEntity;
            lastAddedEntity = null;
            lastAction = "Delete";

            string deletedName = SelectedEntity.Name;

            WriteLog("Deleted entity " + deletedName);

            Entities.Remove(SelectedEntity);
            EntityDeleted?.Invoke(lastDeletedEntity.Id);

            RefreshTable();
            RestartMeteringSimulator();

            NotificationHelper.ShowSuccess("Server deleted", deletedName + " was removed from the system.");
        }

        private void UndoLastAction()
        {
            if (lastAction == "Add" && lastAddedEntity != null)
            {
                Entities.Remove(lastAddedEntity);
                NotificationHelper.ShowInfo("Undo", "Add of " + lastAddedEntity.Name + " was undone.");
                WriteLog("Undo add " + lastAddedEntity.Name);
                lastAddedEntity = null;
                lastAction = "";
                RefreshTable();
                RestartMeteringSimulator();
                return;
            }

            if (lastAction == "Delete" && lastDeletedEntity != null)
            {
                Entities.Add(lastDeletedEntity);
                NotificationHelper.ShowInfo("Undo", "Delete of " + lastDeletedEntity.Name + " was undone.");
                WriteLog("Undo delete " + lastDeletedEntity.Name);
                lastDeletedEntity = null;
                lastAction = "";
                RefreshTable();
                RestartMeteringSimulator();
                return;
            }

            NotificationHelper.ShowInfo("Nothing to undo", "There is no action to undo.");
        }

        private void Search()
        {
            FilteredEntities.Clear();

            foreach (var entity in Entities)
            {
                if (string.IsNullOrWhiteSpace(SearchText))
                    FilteredEntities.Add(entity);
                else if (SearchByName && entity.Name.ToLower().Contains(SearchText.ToLower()))
                    FilteredEntities.Add(entity);
                else if (SearchByType && entity.Type.Name.ToLower().Contains(SearchText.ToLower()))
                    FilteredEntities.Add(entity);
            }
        }

        private void ResetSearch()
        {
            SearchText = "";
            RefreshTable();
        }

        private void SaveSearch()
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                NotificationHelper.ShowWarning("Save search failed", "Enter search text before saving.");
                return;
            }

            string mode = SearchByName ? "Name" : "Type";
            string saved = mode + ":" + SearchText;

            if (!SavedSearches.Contains(saved))
            {
                SavedSearches.Add(saved);
                SaveSearchesToFile();
                NotificationHelper.ShowSuccess("Search saved", saved + " is now available in the saved searches list.");
            }
            else
            {
                NotificationHelper.ShowInfo("Already saved", "This search already exists.");
            }
        }

        private void ApplySavedSearch(string savedSearch)
        {
            string[] parts = savedSearch.Split(':');

            if (parts.Length != 2)
                return;

            SearchByName = parts[0] == "Name";
            SearchByType = parts[0] == "Type";
            SearchText = parts[1];

            Search();
        }

        private void LoadSavedSearches()
        {
            string filePath = "SavedSearches\\searches.txt";

            if (!File.Exists(filePath))
                return;

            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    SavedSearches.Add(line);
            }
        }

        private void SaveSearchesToFile()
        {
            Directory.CreateDirectory("SavedSearches");

            File.WriteAllLines("SavedSearches\\searches.txt", SavedSearches);
        }

        private void RefreshTable()
        {
            FilteredEntities.Clear();

            foreach (var entity in Entities)
                FilteredEntities.Add(entity);
        }

        private void WriteLog(string text)
        {
            Directory.CreateDirectory("Logs");

            File.AppendAllText(
                "Logs\\log.txt",
                DateTime.Now + " - " + text + Environment.NewLine
            );
        }

        private void RestartMeteringSimulator()
        {
            try
            {
                foreach (Process process in Process.GetProcessesByName("MeteringSimulator"))
                {
                    process.Kill();
                    process.WaitForExit(1000);
                }

                string exePath = FindMeteringSimulatorExe();

                if (exePath != null)
                {
                    Process.Start(exePath);
                    WriteLog("MeteringSimulator restarted.");
                }
                else
                {
                    WriteLog("MeteringSimulator.exe not found. Build the MeteringSimulator project first.");
                }
            }
            catch (Exception ex)
            {
                WriteLog("MeteringSimulator restart failed: " + ex.Message);
            }
        }

        private string FindMeteringSimulatorExe()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            string[] candidates =
            {
                Path.Combine(baseDir, "MeteringSimulator.exe"),
                Path.Combine(baseDir, @"..\..\..\MeteringSimulator\bin\Debug\MeteringSimulator.exe"),
                Path.Combine(baseDir, @"..\..\..\MeteringSimulator\bin\Release\MeteringSimulator.exe")
            };

            foreach (string candidate in candidates)
            {
                if (File.Exists(candidate))
                    return candidate;
            }

            return null;
        }
    }
}