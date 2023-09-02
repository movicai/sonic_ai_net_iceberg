using mus.viewer.db.sqlite;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace wpf.Models
{
    public class DeviceModel : INotifyPropertyChanged
    {
        protected string _name;
        protected string _ipaddress;
        private string _subnet;
        private string _historyurl;
        private string _edgeurl;
        private string _desc;
        private string _guid;
        //private bool _collect;
        public string name { get => _name; set => _name = value; }
        public string ipaddress { get => _ipaddress; set => _ipaddress = value; }
        public string subnet { get => _subnet; set => _subnet = value; }
        public string historyurl { get => _historyurl; set => _historyurl = value; }
        public string edgeurl { get => _edgeurl; set => _edgeurl = value; }
        //public bool collect { get => _collect; set => _collect = value; }
        public string desc { get => _desc; set => _desc = value; }


        private bool _isselected;
        public bool isselected
        {
            get { return _isselected; }
            set { _isselected = value; NotifyPropertyChanged("isselected"); }
        }

        public string guid { get => _guid; set => _guid = value; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

    }
}
