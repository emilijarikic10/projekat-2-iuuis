using NetworkService.Model;
using NetworkService.ViewModel;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetworkService.View
{
    public partial class MeasurementGraphView : UserControl
    {
        private Dictionary<int, List<double>> history = new Dictionary<int, List<double>>();
        private Dictionary<int, List<string>> times = new Dictionary<int, List<string>>();

        public MeasurementGraphView()
        {
            InitializeComponent();
            Loaded += MeasurementGraphView_Loaded;
        }

        private void MeasurementGraphView_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel vm = DataContext as MainWindowViewModel;

            if (vm == null)
                return;

            cbEntities.ItemsSource = vm.Entities;
            cbEntities.DisplayMemberPath = "Name";

            history.Clear();
            times.Clear();

            foreach (ServerEntity entity in vm.Entities)
            {
                history[entity.Id] = new List<double>() { entity.Value };
                times[entity.Id] = new List<string>() { DateTime.Now.ToString("HH:mm:ss") };

                entity.PropertyChanged += Entity_PropertyChanged;
            }

            if (cbEntities.Items.Count > 0)
                cbEntities.SelectedIndex = 0;
        }

        private void Entity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Value")
                return;

            ServerEntity entity = sender as ServerEntity;

            if (entity == null)
                return;

            if (!history.ContainsKey(entity.Id))
            {
                history[entity.Id] = new List<double>();
                times[entity.Id] = new List<string>();
            }

            history[entity.Id].Add(entity.Value);
            times[entity.Id].Add(DateTime.Now.ToString("HH:mm:ss"));

            while (history[entity.Id].Count > 5)
            {
                history[entity.Id].RemoveAt(0);
                times[entity.Id].RemoveAt(0);
            }

            if (cbEntities.SelectedItem is ServerEntity selectedEntity &&
                selectedEntity.Id == entity.Id)
            {
                DrawGraph(entity);
            }
        }

        private void cbEntities_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ServerEntity entity = cbEntities.SelectedItem as ServerEntity;

            if (entity == null)
                return;

            DrawGraph(entity);
        }

        private void DrawGraph(ServerEntity entity)
        {
            GraphCanvas.Children.Clear();

            if (!history.ContainsKey(entity.Id))
                return;

            List<double> values = history[entity.Id];
            List<string> timeValues = times[entity.Id];

            double startX = 50;
            double stepX = 75;
            double centerY = 170;
            double maxRadius = 45;

            Line xAxis = new Line
            {
                X1 = 30,
                Y1 = centerY + maxRadius + 10,
                X2 = 420,
                Y2 = centerY + maxRadius + 10,
                Stroke = Brushes.Black,
                StrokeThickness = 2
            };

            GraphCanvas.Children.Add(xAxis);

            TextBlock title = new TextBlock
            {
                Text = "G3 - Server load history",
                FontSize = 17,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Color.FromRgb(0x1B, 0x2A, 0x41))
            };

            Canvas.SetLeft(title, 20);
            Canvas.SetTop(title, 10);
            GraphCanvas.Children.Add(title);

            for (int i = 0; i < values.Count; i++)
            {
                double value = values[i];
                double radius = value / 2;

                if (radius > maxRadius)
                    radius = maxRadius;

                if (radius < 10)
                    radius = 10;

                double centerX = startX + i * stepX;

                Ellipse ellipse = new Ellipse
                {
                    Width = radius * 2,
                    Height = radius * 2,
                    Stroke = Brushes.Black,
                    StrokeThickness = 1
                };

                if (value >= 45 && value <= 75)
                    ellipse.Fill = Brushes.LightGreen;
                else
                    ellipse.Fill = Brushes.IndianRed;

                Canvas.SetLeft(ellipse, centerX - radius);
                Canvas.SetTop(ellipse, centerY - radius);
                GraphCanvas.Children.Add(ellipse);

                TextBlock valueText = new TextBlock
                {
                    Text = value.ToString("0"),
                    FontWeight = FontWeights.Bold
                };

                Canvas.SetLeft(valueText, centerX - 10);
                Canvas.SetTop(valueText, centerY - 10);
                GraphCanvas.Children.Add(valueText);

                TextBlock timeText = new TextBlock
                {
                    Text = timeValues[i],
                    FontSize = 10
                };

                Canvas.SetLeft(timeText, centerX - 25);
                Canvas.SetTop(timeText, centerY + maxRadius + 15);
                GraphCanvas.Children.Add(timeText);
            }
        }
    }
}