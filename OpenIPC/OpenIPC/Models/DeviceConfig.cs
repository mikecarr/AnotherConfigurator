using System.ComponentModel;
using System.Runtime.CompilerServices;
using OpenIPC.Events;
using Prism.Events;

namespace OpenIPC.Models
{
    public class DeviceConfig : INotifyPropertyChanged
    {
        public DeviceConfig()
        {
            Username = "root";
        }
        
        public IEventAggregator EventAggregator { get; set; }

        private static DeviceConfig _instance;
        public static DeviceConfig Instance => _instance ??= new DeviceConfig();

        private string _username;
        private string _password;
        private string _ipAddress;
        private string _hostname;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged();  }
        }

        public string IpAddress
        {
            get => _ipAddress;
            set { _ipAddress = value; OnPropertyChanged(); }
        }

        public string Hostname
        {
            get => _hostname;
            set { _hostname = value; OnPropertyChanged();  }
        }

        public DeviceType DeviceType { get; set; }

        // CanConnect property to determine connection eligibility
        public bool CanConnect => 
            !string.IsNullOrEmpty(Hostname) && 
            !string.IsNullOrEmpty(IpAddress) && 
            !string.IsNullOrEmpty(Password);

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        
        
    }
}
