using NetworkService.View;
using NetworkService.ViewModel;
using Notification.Wpf;
using System.Windows;
using System.Windows.Input;


namespace NetworkService
{
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            DataContext = new MainWindowViewModel();

            MainContent.Content = new MeasurementGraphView()
            {
                DataContext = DataContext
            };
        }

        private void Entities_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new EntitiesView()
            {
                DataContext = DataContext
            };
        }

        private void Graph_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new MeasurementGraphView()
            {
                DataContext = DataContext
            };
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.E)
            {
                Entities_Click(null, null);
            }

            if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.G)
            {
                Graph_Click(null, null);
            }
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MainContent.Content = new HelpView()
            {
                DataContext = DataContext
            };
        }
    }
}