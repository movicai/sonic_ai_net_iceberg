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
    public class DeviceModel : Device , INotifyPropertyChanged
    {

		//public string Name
		//{
		//	get { return name; }
		//	set { name = value; }
		//}

		private bool _isselected;
		public bool isselected
		{
			get { return _isselected; }
			set { _isselected = value; NotifyPropertyChanged("isselected"); }
		}

		//public string ipaddress
		//{
		//	get { return ipAddress; }
		//	set { ipAddress = value; }
		//}
		//      public string Subnet
		//{
		//	get { return subnet; }
		//	set { subnet = value; }
		//}

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
