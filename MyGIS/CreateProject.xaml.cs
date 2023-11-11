using Microsoft.Win32;
using MySqlConnector;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
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
using System.Windows.Shapes;
using Windows.Graphics.Imaging;

namespace MyGIS
{
    /// <summary>
    /// Логика взаимодействия для CreateProject.xaml
    /// </summary>
    public partial class CreateProject : Window
    {
        public string ImageFileName { get; set; }

        public byte[] ImageBytes { get; set; }

        public CreateProject()
        {
            InitializeComponent();
        }

        private async void SendInfoProjectToDb(object sender, EventArgs e)
        {
            var connection = DbManager.Connection;

            await DbManager.OpenConnection();

            var transaction = await connection.BeginTransactionAsync();

            Log.Information("Start transaction");

            var added = false;
            
            try
            {
                string command = "insert into ProjectInformations (`Name`, `DateOfCreation`, `DateOfLastEdit`) values (@nameProject, @DateOfCreation, @DateOfLastCreation); SELECT idProject FROM ProjectInformations WHERE Name=@nameProject;";

                string command1 = "insert into ProjectImages values (@id, @Image, @fileName, @offset);";

                MySqlParameter parameter1 = new MySqlParameter("@nameProject", MySqlDbType.VarChar);
                parameter1.Value = NameProject.Text;

                MySqlParameter parameter2 = new MySqlParameter("@DateOfCreation", MySqlDbType.Date);
                parameter2.Value = DateTime.Now;

                MySqlParameter parameter3 = new MySqlParameter("@DateOfLastCreation", MySqlDbType.Date);
                parameter3.Value = DateTime.Now;

                List<MySqlParameter> parameters = new List<MySqlParameter>()
                {
                    parameter1,
                    parameter2, 
                    parameter3
                };

                int id = -1;
                try
                {
                    var result = await DbManager.ExecuteCommand(command, parameters, transaction);
                    Log.Information("Ok");

                    if (result.HasRows)
                    {
                        result.Read();
                        id = (int)result.GetValue(0);
                    }

                    await result.CloseAsync();
                } catch (Exception ex)
                {
                    Log.Error(ex.Message);
                    await transaction.RollbackAsync();
                }

                if (id != -1)
                {
                    parameter1 = new MySqlParameter("@id", MySqlDbType.Int32);
                    parameter1.Value = id;

                    parameter2 = new MySqlParameter("@Image", MySqlDbType.Blob);
                    parameter2.Value = ImageBytes;

                    parameter3 = new MySqlParameter("@fileName", MySqlDbType.VarChar);
                    parameter3.Value = ImageFileName; 

                    MySqlParameter parameter4 = new MySqlParameter("@offset", MySqlDbType.Int32);
                    parameter4.Value = ImageBytes.Length;

                    parameters = new List<MySqlParameter>()
                    {
                        parameter1,
                        parameter2,
                        parameter3,
                        parameter4
                    };
                    MySqlDataReader res = null;
                    try
                    {
                        res = await DbManager.ExecuteCommand(command1, parameters, transaction);
                        await res.CloseAsync();
                    } catch (Exception ex) 
                    {
                        Log.Error(ex.Message);
                        await transaction.RollbackAsync();
                    }
                    
                }

                await transaction.CommitAsync();

                Log.Information("Transaction commited");
                added = true;
            } catch
            {
                await transaction.RollbackAsync();
                Log.Information("Transaction rollbacked");
            }

            await transaction.DisposeAsync();

            await DbManager.CloseConnection();

            if (added)
            {
                MessageBox.Show("Проект добавлен");

            }

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "jpg|*.jpg; .png|*.png; jpeg|*.jpeg";

            if (!openFileDialog.ShowDialog().GetValueOrDefault())
                return;

            ImageFileName = openFileDialog.FileName;

            ImageBytes = File.ReadAllBytes(ImageFileName);
        }
    }
}
