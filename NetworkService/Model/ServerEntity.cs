using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.ComponentModel;

namespace NetworkService.Model
{
    public class ServerEntity : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int id;
        private string name;
        private string ipAddress;
        private ServerType type;
        private double value;

        public int Id
        {
            get { return id; }
            set { id = value; OnPropertyChanged("Id"); }
        }

        public string Name
        {
            get { return name; }
            set { name = value; OnPropertyChanged("Name"); }
        }

        public string IpAddress
        {
            get { return ipAddress; }
            set { ipAddress = value; OnPropertyChanged("IpAddress"); }
        }

        public ServerType Type
        {
            get { return type; }
            set { type = value; OnPropertyChanged("Type"); }
        }

        public double Value
        {
            get { return value; }
            set { this.value = value; OnPropertyChanged("Value"); OnPropertyChanged("IsValid"); OnPropertyChanged("StatusColor");}
            
        }

        public bool IsValid
        {
            get { return Value >= 45 && Value <= 75; }
        }

        public string StatusColor
        {
            get { return IsValid ? "DarkGreen" : "DarkRed"; }
        }

        public string ImagePath
        {
            get { return Type != null ? Type.ImagePath : ""; }
        }

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
