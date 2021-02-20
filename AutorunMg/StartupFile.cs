using System.ComponentModel;
using System.Windows.Media.Imaging;

namespace AutorunMg
{
    class StartupFile: INotifyPropertyChanged
    {
        private bool enabled; 
        public string Name { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string Params { get; set; }
        public string Path { get; set; }
        public bool Enabled
        {
            get { return enabled; }
            set
            {
                if (value != enabled)
                {
                    enabled = value;
                    OnPropertyChanged("Enabled");
                }
            }
        }
        public Microsoft.Win32.RegistryKey ApprovedKeyPath { get; set; }
        public bool CheckBoxEnabled
        {
            get
            {
                if (ApprovedKeyPath == null)
                    return false;
                else
                    return true;
            }
        }
        public BitmapFrame Icon { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
