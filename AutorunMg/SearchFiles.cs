using Microsoft.Win32;
using System;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using IWshRuntimeLibrary;
using System.Windows;

namespace AutorunMg
{
    class SearchFiles
    {
        private int ResultCount;
        public void Search()
        {
            string[] keyPaths = { @"SOFTWARE\Microsoft\Windows\CurrentVersion\Run",
                                  @"SOFTWARE\Microsoft\Windows\CurrentVersion\RunOnce",
                                  @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Run",
                                  @"SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\RunOnce"};
            RegistryKey key;
            RegistryKey subKey = null;
            ResultCount = 0;
            foreach (var item in keyPaths)
            {
                foreach (var regHive in new RegistryHive[] {RegistryHive.LocalMachine, RegistryHive.CurrentUser})
                {
                    try
                    {
                        key = RegistryKey.OpenBaseKey(regHive, RegistryView.Default);
                        subKey = key.OpenSubKey(item);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                        subKey?.Close();
                        continue;
                    }
                    if (subKey != null)
                    {
                        SearchInRegistry(subKey, key);
                        subKey.Close();
                    }
                }
            };

            string shellFolders = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders";
            string startupApproved = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\StartupFolder";

            RegistryKey startupApprovedKey = null;
            object startupFolder;

            foreach (var regHive in new RegistryHive[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
            {
                try
                {
                    key = RegistryKey.OpenBaseKey(regHive, RegistryView.Default);
                    subKey = key.OpenSubKey(shellFolders);
                    
                    if (regHive == RegistryHive.CurrentUser)
                        startupFolder = subKey?.GetValue("Startup");
                    else
                        startupFolder = subKey?.GetValue("Common Startup");
                }
                catch (Exception ex)
                { 
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    continue;
                }
                finally
                {
                    subKey?.Close();
                }

                if ((startupFolder == null))
                {
                    MessageBox.Show("Не удалось получить данные о каталоге 'Автозагрузка'", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    startupApprovedKey?.Close();
                    continue;
                }

                try
                {
                    startupApprovedKey = key.OpenSubKey(startupApproved, true);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                SearchInStartMenu(startupFolder.ToString(), startupApprovedKey);
            }
            Completed?.Invoke(ResultCount);
        }

        private void SearchInRegistry(RegistryKey key, RegistryKey baseKey)
        {
            //Thread.Sleep(1000);
            SearchInDirectory?.Invoke(key.Name);
            RegistryKey startupApprovedKey;

            try
            {
                if (key.Name.Contains("Wow6432Node"))
                    startupApprovedKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run32", true);
                else
                    startupApprovedKey = baseKey.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run", true);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                //return;
                startupApprovedKey = null;
            }

            foreach (var item in key.GetValueNames())
            {
                if (item == "")
                    continue;
                StartupFile file = new StartupFile();
                PathParse PathInfo = new PathParse(key.GetValue(item).ToString());
                file.Name = item;
                file.FileName = PathInfo.Name;
                file.FilePath = PathInfo.Path;
                file.Params = PathInfo.Params;
                file.Path = key.Name;
                file.Icon = GetIcon(file.FilePath + file.FileName);
                file.ApprovedKeyPath = startupApprovedKey;
                if (startupApprovedKey != null)
                    file.Enabled = GetEnabled(item, startupApprovedKey);

                SearchedFile?.Invoke(file);
                ResultCount++;
            }
           // FilesParse(paths, names, key.Name);
        }

        private void SearchInStartMenu(string pathStartup, RegistryKey startupApprovedKey)
        {
            //Thread.Sleep(1000);
            SearchInDirectory?.Invoke(pathStartup);
            string[] paths;
            try
            {
                paths = Directory.GetFiles(pathStartup).Where(f => !f.ToLower().EndsWith(".ini")).ToArray();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            IWshShell wsh = new WshShell();

            foreach (var item in paths)
            {
                StartupFile file = new StartupFile();
                PathParse PathInfo = new PathParse(item);

                if (item.EndsWith("lnk"))
                {
                    var lnk = (IWshShortcut)wsh.CreateShortcut(item);
                    var lnkInfo = new PathParse(lnk.TargetPath);
                    file.FileName = lnkInfo.Name;
                    file.Params = lnk.Arguments;
                    file.FilePath = lnkInfo.Path;
                }
                else
                {
                    file.FileName = PathInfo.Name;
                    file.Params = PathInfo.Params;
                    file.FilePath = PathInfo.Path;
                }
                file.Name = PathInfo.Name;
                file.Path = pathStartup;
                file.Icon = GetIcon(file.FilePath+file.FileName);
                file.ApprovedKeyPath = startupApprovedKey;
                if (startupApprovedKey != null)
                    file.Enabled = GetEnabled(file.Name, startupApprovedKey);

                SearchedFile?.Invoke(file);
                ResultCount++;
            }
        }

        private BitmapFrame GetIcon(string filePath)
        {
            BitmapFrame icon;
            if (!System.IO.File.Exists(filePath))
            {
                return null;
            }
            using (System.Drawing.Bitmap bmp = System.Drawing.Icon.ExtractAssociatedIcon(filePath).ToBitmap())
            {
                using (Stream stream = new MemoryStream())
                {
                    bmp.Save(stream, System.Drawing.Imaging.ImageFormat.Png); //Cохраняем bmp в поток
                    icon = BitmapFrame.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad); //Возвращаем BitFrame, созданный из потока
                }
                //Thread.Sleep(1000);
            }
            return icon;
        }

        private bool GetEnabled(string name, RegistryKey startupApprovedKey)
        {
            var enabled = startupApprovedKey.GetValue(name);
            string enabledString;
            if (enabled == null)
                enabledString = "null";
            else
                enabledString = String.Join("", (byte[])enabled);

            //byte[] enableStart = { 0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00 };
            //byte[] enableStart = { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            string[] enableStartup = { "200000000000", "600000000000", "null" };

            if (enableStartup.Any(x => x == enabledString))
                return true;
            else
                return false;
        }


        //найден файл
        public event Action<StartupFile> SearchedFile;
        //смена директории
        public event Action<string> SearchInDirectory;
        //завершение
        public event Action<int> Completed;
    }
}
