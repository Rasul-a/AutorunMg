using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
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



namespace AutorunMg
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private SearchFiles searchFiles;
        private BindingList<StartupFile> startupFiles = new BindingList<StartupFile>();
        public MainWindow()
        {
            InitializeComponent();
            btnSerch.Click += BtnSerch_Click;
            startupFiles.ListChanged += _startupFiles_ListChanged;
            lvResult.ItemsSource = startupFiles;
            lvResult.MouseDoubleClick += LvResult_MouseDoubleClick;

            searchFiles = new SearchFiles();
            searchFiles.SearchedFile += SearchedFile;
            searchFiles.SearchInDirectory += SearchInDirectory;
            searchFiles.Completed += SearchFiles_Completed;
        }

        private void LvResult_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lvResult.SelectedItem != null)
            {
                string path = ((StartupFile)lvResult.SelectedItem).Path;
                string AppName;
                if (path.StartsWith("HKEY"))
                {
                    foreach (var item in Process.GetProcessesByName("regedit"))
                    {
                        item.Kill();
                    };
                    RegistryKey subKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Applets\Regedit", true);
                    if (subKey == null)
                    {
                        MessageBox.Show("Произошла ошибка", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    try
                    {
                        subKey.SetValue("LastKey", path);
                    }
                    catch (SecurityException ex)
                    {
                        MessageBox.Show(ex.Message, "Нет доступа", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    AppName = "regedit.exe";
                }
                else
                    AppName = path;
                try
                {
                    Process.Start(AppName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

        }

        private void _startupFiles_ListChanged(object sender, ListChangedEventArgs e)
        {
            if (e.ListChangedType == ListChangedType.ItemChanged)
            {
                try
                {
                    SetEnabled(startupFiles[e.NewIndex].Name, startupFiles[e.NewIndex].ApprovedKeyPath, startupFiles[e.NewIndex].Enabled);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                
            }
        }


        private void BtnSerch_Click(object sender, RoutedEventArgs e)
        {
            startupFiles.Clear();
            Thread thread = new Thread(searchFiles.Search);
            thread.Start();
        }

        private void SearchFiles_Completed(int obj)
        {
            Dispatcher.Invoke(()=> label.Content = $"Найдено {obj} файл(ов)");
        }

        private  void SearchedFile(StartupFile _file)
        {
            Dispatcher.Invoke(()=> startupFiles.Add(_file));
        }
        private void SearchInDirectory(string directory)
        {
            Dispatcher.Invoke(()=> label.Content = "Поиск в " + directory);
        }

        private void SetEnabled(string name, RegistryKey startupApprovedKey, bool value)
        {
            byte[] disableStart = { 3, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            byte[] enableStart = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            if (value)
                startupApprovedKey.SetValue(name, enableStart);
            else
                startupApprovedKey.SetValue(name, disableStart);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            startupFiles.Clear();
            Thread thread = new Thread(searchFiles.Search);
            thread.Start();
        }
    }
}
