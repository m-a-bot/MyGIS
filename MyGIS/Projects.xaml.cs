using Microsoft.Win32;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using static System.Net.Mime.MediaTypeNames;

namespace MyGIS
{
    /// <summary>
    /// Логика взаимодействия для Projects.xaml
    /// </summary>

    class InfoProject
    { 
    }


    public partial class Projects : Page
    {
        List<InfoProject> infoProjects = new List<InfoProject>();
        public Projects()
        {
            InitializeComponent();
            
        }

        private async void Page_Initialized(object sender, EventArgs e)
        {
            string command = "SELECT info.idProject, info.Name, info.DateOfCreation, info.DateOfLastEdit, image.dataOffSet, image.ProjectImage FROM ProjectInformations as info join ProjectImages as image on info.idProject = image.idProject;";
            ProjectPanel.Children.Clear();

            await DbManager.OpenConnection();

            var reader = await DbManager.ExecuteCommand(command);

            while(await reader.ReadAsync())
            {
                int id = reader.GetInt32(0);

                string name = reader.GetString(1);

                DateTime dateOfCreation = reader.GetDateTime(2);

                DateTime dateOfLastEdit = reader.GetDateTime(3);

                int dataOffSet = reader.GetInt32(4);

                byte[] buffer = new byte[dataOffSet];
                reader.GetBytes(5, 0, buffer, 0, dataOffSet);


                var imageSource = new BitmapImage { CacheOption = BitmapCacheOption.OnLoad };
                using (MemoryStream memoryStream = new MemoryStream(buffer, 0, dataOffSet))
                {
                    imageSource.BeginInit();
                    imageSource.StreamSource = memoryStream;
                    imageSource.EndInit();
                }

                ProjectPanel.Children.Add(CreateItemOfProjects(name, dateOfCreation, dateOfLastEdit, imageSource));
            }
            

            await reader.CloseAsync();

            await DbManager.CloseConnection();

        }

        private Grid CreateItemOfProjects(string name, DateTime dateOfCreation, DateTime dateOfLastEdit, BitmapImage image)
        {
            Grid mainGrid = new Grid();
            mainGrid.Width = 140;
            mainGrid.Height = 210;
            mainGrid.Margin = new Thickness(5);
            mainGrid.MouseDown += Grid_MouseDown;

            System.Windows.Controls.Image map = new System.Windows.Controls.Image();
            map.Width = 140;
            map.Height = 140;
            map.VerticalAlignment = VerticalAlignment.Top;
            map.Source = image;

            Random random = new Random();

            Grid grid = new Grid();
            grid.Height = 90;

            Frame MFrame = new Frame();
            MFrame.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(
                (byte)random.Next(0, 255), (byte)random.Next(0, 255), (byte)random.Next(0, 255)));

            grid.VerticalAlignment = VerticalAlignment.Bottom;

            Frame frame = new Frame();
            frame.Background = new SolidColorBrush(System.Windows.Media.Color.FromRgb(170, 170, 170));
            Label label = new Label();
            label.Content = "Название "+name;
            label.FontSize = 9;

            Label label1 = new Label();
            label1.Content = "Дата создания\n " + dateOfCreation;
            label1.FontSize = 9;

            Label label2 = new Label();
            label2.Content = "Дата последнего\n изменения " + dateOfLastEdit;
            label2.FontSize = 9;

            StackPanel stackPanel = new StackPanel();


            grid.Children.Add(frame);
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(label1);
            stackPanel.Children.Add(label2);
            grid.Children.Add(stackPanel);

            mainGrid.Children.Add(MFrame);

            mainGrid.Children.Add(grid);
            mainGrid.Children.Add(map);

            return mainGrid;
        }

        private void BtnOpenCreationWindow_Click(object sender, RoutedEventArgs e)
        {
            var window = new CreateProject();

            window.Show();
        }

        private void Grid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Page_Initialized(sender, e);
        }
    }
}
