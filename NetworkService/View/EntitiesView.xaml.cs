using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using NetworkService.ViewModel;


namespace NetworkService.View
{
    public partial class EntitiesView : UserControl
    {
        public EntitiesView()
        {
            InitializeComponent();
        }

        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmationPanel.Visibility = Visibility.Visible;
        }

        private void ConfirmDelete_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel vm = DataContext as MainWindowViewModel;

            if (vm != null)
                vm.DeleteCommand.Execute(null);

            DeleteConfirmationPanel.Visibility = Visibility.Collapsed;
        }

        private void CancelDelete_Click(object sender, RoutedEventArgs e)
        {
            DeleteConfirmationPanel.Visibility = Visibility.Collapsed;
        }
    }
}