using NetworkService.Model;
using NetworkService.Helpers;
using NetworkService.ViewModel;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NetworkService.View
{
    public partial class NetworkDisplayView : UserControl
    {
        private Point startPoint;
        private ObservableCollection<ServerEntity> availableEntities;
        private ObservableCollection<EntityTypeGroup> entityGroups;

        private bool connectMode = false;
        private Border firstSelectedBorder = null;

        private ObservableCollection<string> connections = new ObservableCollection<string>();

        public NetworkDisplayView()
        {
            InitializeComponent();
            availableEntities = new ObservableCollection<ServerEntity>();
            entityGroups = new ObservableCollection<EntityTypeGroup>();
            availableEntities.CollectionChanged += (s, e) => RebuildGroups();
        }

        private void RebuildGroups()
        {
            entityGroups.Clear();

            foreach (ServerEntity entity in availableEntities)
            {
                EntityTypeGroup group = entityGroups.FirstOrDefault(g => g.TypeName == entity.Type.Name);

                if (group == null)
                {
                    group = new EntityTypeGroup(entity.Type.Name);
                    entityGroups.Add(group);
                }

                group.Entities.Add(entity);
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel viewModel = DataContext as MainWindowViewModel;

            if (viewModel == null)
                return;

            availableEntities.Clear();

            foreach (ServerEntity entity in viewModel.Entities)
            {
                if (!IsEntityAlreadyPlaced(entity))
                    availableEntities.Add(entity);
            }

            EntitiesTreeView.ItemsSource = entityGroups;
            viewModel.EntityDeleted += ViewModel_EntityDeleted;
            foreach (ServerEntity entity in viewModel.Entities)
            {
                entity.PropertyChanged += Entity_PropertyChanged;
            }
        }

        private void ViewModel_EntityDeleted(int deletedId)
        {
            foreach (object child in NetworkGrid.Children)
            {
                Border border = child as Border;

                if (border != null && border.Tag is ServerEntity entity)
                {
                    if (entity.Id == deletedId)
                    {
                        RemoveLinesForEntity(entity);
                        ClearField(border);
                        break;
                    }
                }
            }

            for (int i = availableEntities.Count - 1; i >= 0; i--)
            {
                if (availableEntities[i].Id == deletedId)
                    availableEntities.RemoveAt(i);
            }
        }

        private void Entity_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName != "Value" && e.PropertyName != "IsValid")
                return;

            ServerEntity changedEntity = sender as ServerEntity;

            if (changedEntity == null)
                return;

            foreach (object child in NetworkGrid.Children)
            {
                Border border = child as Border;

                if (border != null && border.Tag is ServerEntity entity)
                {
                    if (entity.Id == changedEntity.Id)
                    {
                        PutEntityInField(border, changedEntity);
                        break;
                    }
                }
            }
        }

        private void EntitiesTreeView_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            startPoint = e.GetPosition(null);
        }

        private void EntitiesTreeView_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(null);
            Vector difference = startPoint - mousePosition;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(difference.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(difference.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                TreeViewItem item = FindParent<TreeViewItem>((DependencyObject)e.OriginalSource);

                if (item == null)
                    return;

                ServerEntity entity = item.DataContext as ServerEntity;

                if (entity == null)
                    return;

                DragDrop.DoDragDrop(item, entity, DragDropEffects.Move);
            }
        }

        private void Field_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (connectMode)
            {
                Border clickedBorder = sender as Border;

                if (clickedBorder == null)
                    return;

                if (clickedBorder.Tag == null)
                    return;

                if (firstSelectedBorder == null)
                {
                    firstSelectedBorder = clickedBorder;
                    clickedBorder.BorderBrush = Brushes.Red;
                    clickedBorder.BorderThickness = new Thickness(3);
                }
                else
                {
                    if (firstSelectedBorder != clickedBorder)
                    {
                        DrawLine(firstSelectedBorder, clickedBorder);
                    }

                    firstSelectedBorder.BorderBrush = Brushes.Black;
                    firstSelectedBorder.BorderThickness = new Thickness(1);

                    firstSelectedBorder = null;
                }

                return;
            }
            startPoint = e.GetPosition(null);
        }

        private void Field_MouseMove(object sender, MouseEventArgs e)
        {
            Border sourceBorder = sender as Border;

            if (sourceBorder == null)
                return;

            ServerEntity entity = sourceBorder.Tag as ServerEntity;

            if (entity == null)
                return;

            Point mousePosition = e.GetPosition(null);
            Vector difference = startPoint - mousePosition;

            if (e.LeftButton == MouseButtonState.Pressed &&
                (Math.Abs(difference.X) > SystemParameters.MinimumHorizontalDragDistance ||
                 Math.Abs(difference.Y) > SystemParameters.MinimumVerticalDragDistance))
            {
                DragDrop.DoDragDrop(sourceBorder, entity, DragDropEffects.Move);
            }
        }

        private void Field_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ServerEntity)))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void Field_Drop(object sender, DragEventArgs e)
        {
            Border targetBorder = sender as Border;

            if (targetBorder == null)
                return;

            if (targetBorder.Child != null)
                return;

            ServerEntity entity = e.Data.GetData(typeof(ServerEntity)) as ServerEntity;

            if (entity == null)
                return;

            RemoveVisualLinesOnly();

            foreach (object child in NetworkGrid.Children)
            {
                Border existingBorder = child as Border;

                if (existingBorder != null &&
                    existingBorder.Tag is ServerEntity existingEntity &&
                    existingEntity.Id == entity.Id)
                {
                    ClearField(existingBorder);
                    break;
                }
            }

            PutEntityInField(targetBorder, entity);

            availableEntities.Remove(entity);

            RedrawAllLines();

            e.Handled = true;
        }

        private void EntitiesTreeView_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(ServerEntity)))
                e.Effects = DragDropEffects.Move;
            else
                e.Effects = DragDropEffects.None;

            e.Handled = true;
        }

        private void EntitiesTreeView_Drop(object sender, DragEventArgs e)
        {
            ServerEntity entity = e.Data.GetData(typeof(ServerEntity)) as ServerEntity;

            if (entity == null)
                return;

            Border border = FindBorderByEntityId(entity.Id);

            if (border == null)
                return;

            RemoveLinesForEntity(entity);
            ClearField(border);

            if (!availableEntities.Contains(entity))
                availableEntities.Add(entity);

            e.Handled = true;
        }

        private void Field_RightClick(object sender, MouseButtonEventArgs e)
        {
            Border border = sender as Border;

            if (border == null)
                return;

            ServerEntity entity = border.Tag as ServerEntity;

            if (entity == null)
                return;

            RemoveLinesForEntity(entity);

            ClearField(border);

            if (!availableEntities.Contains(entity))
                availableEntities.Add(entity);
        }

        private void AutoPopulate_Click(object sender, RoutedEventArgs e)
        {
            MainWindowViewModel viewModel = DataContext as MainWindowViewModel;

            if (viewModel == null)
                return;

            foreach (ServerEntity entity in viewModel.Entities)
            {
                if (IsEntityAlreadyPlaced(entity))
                    continue;

                Border emptyField = FindFirstEmptyField();

                if (emptyField == null)
                    return;

                PutEntityInField(emptyField, entity);
                availableEntities.Remove(entity);
            }
        }

        private void SaveLayout_Click(object sender, RoutedEventArgs e)
        {
            Directory.CreateDirectory("Layouts");

            string filePath = "Layouts\\network_layout.txt";

            File.WriteAllText(filePath, "");

            int index = 0;

            foreach (object child in NetworkGrid.Children)
            {
                Border border = child as Border;

                if (border != null && border.Tag is ServerEntity entity)
                {
                    File.AppendAllText(filePath, index + ";" + entity.Id + Environment.NewLine);
                }

                index++;
            }

            NotificationHelper.ShowSuccess("Layout saved", "The current grid layout was saved to disk.");
        }

        private void LoadLayout_Click(object sender, RoutedEventArgs e)
        {
            string filePath = "Layouts\\network_layout.txt";

            if (!File.Exists(filePath))
                return;

            foreach (object child in NetworkGrid.Children)
            {
                Border border = child as Border;

                if (border != null)
                    ClearField(border);
            }

            MainWindowViewModel viewModel = DataContext as MainWindowViewModel;

            if (viewModel == null)
                return;

            availableEntities.Clear();

            foreach (ServerEntity entity in viewModel.Entities)
                availableEntities.Add(entity);

            string[] lines = File.ReadAllLines(filePath);

            foreach (string line in lines)
            {
                string[] parts = line.Split(';');

                int fieldIndex = int.Parse(parts[0]);
                int entityId = int.Parse(parts[1]);

                ServerEntity entity = viewModel.Entities.FirstOrDefault(x => x.Id == entityId);

                if (entity == null)
                    continue;

                Border border = NetworkGrid.Children[fieldIndex] as Border;

                if (border == null)
                    continue;

                PutEntityInField(border, entity);
                availableEntities.Remove(entity);
            }

            RedrawAllLines();

            NotificationHelper.ShowSuccess("Layout loaded", "The saved grid layout was restored.");
        }

        private void ConnectMode_Click(object sender, RoutedEventArgs e)
        {
            connectMode = !connectMode;

            firstSelectedBorder = null;

            NotificationHelper.ShowInfo(
                "Connect mode",
                connectMode ? "Click two servers on the grid to draw a connection between them." : "Connect mode is now off.");
        }

        private bool IsEntityAlreadyPlaced(ServerEntity entity)
        {
            foreach (object child in NetworkGrid.Children)
            {
                Border border = child as Border;

                if (border != null && border.Tag is ServerEntity placedEntity)
                {
                    if (placedEntity.Id == entity.Id)
                        return true;
                }
            }

            return false;
        }

        private Border FindFirstEmptyField()
        {
            foreach (object child in NetworkGrid.Children)
            {
                Border border = child as Border;

                if (border != null && border.Child == null)
                    return border;
            }

            return null;
        }

        private void PutEntityInField(Border border, ServerEntity entity)
        {
            border.Child = null;
            border.Tag = entity;

            Border card = new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8),
                Margin = new Thickness(4),
                Background = Brushes.White,
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(1)
            };

            StackPanel panel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            Image image = new Image
            {
                Width = 50,
                Height = 50,
                Margin = new Thickness(0, 0, 0, 4),
                Source = new BitmapImage(new Uri(entity.Type.ImagePath, UriKind.Relative))
            };

            panel.Children.Add(new TextBlock
            {
                Text = entity.IsValid ? "NORMAL" : "ALARM",
                FontWeight = FontWeights.Bold,
                FontSize = 13,
                Foreground = entity.IsValid ? Brushes.DarkGreen : Brushes.DarkRed,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(image);

            panel.Children.Add(new TextBlock
            {
                Text = entity.Name,
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = "ID: " + entity.Id,
                FontSize = 12,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            panel.Children.Add(new TextBlock
            {
                Text = entity.Value + "%",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                HorizontalAlignment = HorizontalAlignment.Center
            });

            card.Child = panel;

            if (entity.IsValid)
                border.Background = new SolidColorBrush(Color.FromRgb(214, 245, 225));
            else
                border.Background = new SolidColorBrush(Color.FromRgb(255, 220, 220));

            border.ToolTip =
                "Name: " + entity.Name + "\n" +
                "ID: " + entity.Id + "\n" +
                "IP Address: " + entity.IpAddress + "\n" +
                "Type: " + entity.Type.Name + "\n" +
                "Load: " + entity.Value + "%\n" +
                "Status: " + (entity.IsValid ? "Valid" : "Invalid");

            border.Child = card;
        }

        private void ClearField(Border border)
        {
            border.Child = null;
            border.Tag = null;
            border.ToolTip = null;
            border.Background = Brushes.Transparent;
        }

        private void DrawLine(Border border1, Border border2)
        {
            ServerEntity entity1 = border1.Tag as ServerEntity;
            ServerEntity entity2 = border2.Tag as ServerEntity;

            if (entity1 == null || entity2 == null)
                return;

            string connectionKey1 = entity1.Id + "-" + entity2.Id;
            string connectionKey2 = entity2.Id + "-" + entity1.Id;

            if (connections.Contains(connectionKey1) || connections.Contains(connectionKey2))
                return;

            connections.Add(connectionKey1);

            Point p1 = border1.TranslatePoint(
                new Point(border1.ActualWidth / 2, border1.ActualHeight / 2),
                LinesCanvas);

            Point p2 = border2.TranslatePoint(
                new Point(border2.ActualWidth / 2, border2.ActualHeight / 2),
                LinesCanvas);

            Line line = new Line();

            line.X1 = p1.X;
            line.Y1 = p1.Y;

            line.X2 = p2.X;
            line.Y2 = p2.Y;

            line.Stroke = Brushes.Blue;
            line.StrokeThickness = 3;

            line.Tag = connectionKey1;

            LinesCanvas.Children.Add(line);
        }

        private void RemoveLinesForEntity(ServerEntity entity)
        {
            for (int i = LinesCanvas.Children.Count - 1; i >= 0; i--)
            {
                Line line = LinesCanvas.Children[i] as Line;

                if (line == null)
                    continue;

                string key = line.Tag as string;

                if (key == null)
                    continue;

                string[] parts = key.Split('-');

                if (parts[0] == entity.Id.ToString() || parts[1] == entity.Id.ToString())
                {
                    LinesCanvas.Children.RemoveAt(i);
                    connections.Remove(key);
                }
            }
        }

        private void RemoveVisualLinesOnly()
        {
            LinesCanvas.Children.Clear();
        }

        private void RedrawAllLines()
        {
            ObservableCollection<string> savedConnections = new ObservableCollection<string>();

            foreach (string connection in connections)
            {
                savedConnections.Add(connection);
            }

            LinesCanvas.Children.Clear();
            connections.Clear();

            foreach (string connection in savedConnections)
            {
                string[] parts = connection.Split('-');

                int id1 = int.Parse(parts[0]);
                int id2 = int.Parse(parts[1]);

                Border border1 = FindBorderByEntityId(id1);
                Border border2 = FindBorderByEntityId(id2);

                if (border1 != null && border2 != null)
                {
                    DrawLine(border1, border2);
                }
            }
        }

        private Border FindBorderByEntityId(int id)
        {
            foreach (object child in NetworkGrid.Children)
            {
                Border border = child as Border;

                if (border != null && border.Tag is ServerEntity entity)
                {
                    if (entity.Id == id)
                        return border;
                }
            }

            return null;
        }

        private T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parent = VisualTreeHelper.GetParent(child);

            while (parent != null && !(parent is T))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }

            return parent as T;
        }

        private void NetworkGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RedrawAllLines();
        }
    }
}