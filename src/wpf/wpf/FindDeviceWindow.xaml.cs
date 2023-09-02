using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
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
using wpf.Models;

namespace wpf
{

    public delegate void AddDeviceEventHandler(object sender, AddDeviceEventArgs e);
    public class AddDeviceEventArgs : EventArgs
    {
        public List<Models.DeviceModel> Devices { get; set; }
        public bool ResetItems { get; set; }
        public AddDeviceEventArgs(List<DeviceModel> devices)
        {
            Devices = devices;
        }
        public AddDeviceEventArgs(List<DeviceModel> devices, bool resetitems) : this(devices)
        {
            this.ResetItems = resetitems;
        }
        public AddDeviceEventArgs(DeviceModel device)
        {
            this.Devices = new List<DeviceModel>();
            this.Devices.Add(device);
        }
    }

    /// <summary>
    /// FindDeviceWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class FindDeviceWindow : Window, INotifyPropertyChanged
    {
        public event AddDeviceEventHandler AddDeviceEvent;
        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }


        mus.viewer.network.FindDevice _finddevice;
        //public ObservableCollection<Models.DeviceModel> _devices;
        //public ObservableCollection<Models.DeviceModel> Devices { get => _devices; set => _devices = value; }
        public FindDeviceWindow()
        {
            InitializeComponent();
        }
        public FindDeviceWindow(string[] param) : this()
        {
            _finddevice = new mus.viewer.network.FindDevice(param);
            _finddevice.FindDeviceEvent += _finddevice_FindDeviceEvent;
            _finddevice.FindDeviceCompeteEvent += _finddevice_FindDeviceCompeteEvent;
            //Task.Run(async() =>
            //{
            _finddevice.StartFind();
            //});

        }

        private void _finddevice_FindDeviceCompeteEvent(object sender, mus.viewer.network.FindDeviceEventArgs e)
        {
            progress.IsIndeterminate = false;
        }

        private void _finddevice_FindDeviceEvent(object sender, mus.viewer.network.FindDeviceEventArgs e)
        {
            var model = new Models.DeviceModel()
            {
                isselected = true,
                ipaddress = e.DeviceIPAddress.ToString(),
                subnet = e.Subnet,
                name = e.DeviceId,
            };
            datagrid.Items.Add(model);
            //this.Devices.Add(model);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (AddDeviceEvent != null)
            {
                //bool resetitems = false;
                //if (MessageBox.Show("Reset All Items?", "Device Items Attatch", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                //{
                //    resetitems = true;
                //}
                var data = datagrid.Items.OfType<DeviceModel>().Where(m => m.isselected == true).ToList();
                AddDeviceEvent(this, new AddDeviceEventArgs(data, false));
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            foreach (var item in datagrid.Items.OfType<DeviceModel>())
            {
                item.isselected = true;
            }
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (datagrid.SelectedItem != null)
            {
                (datagrid.SelectedItem as DeviceModel).isselected = true;
            }
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            if (datagrid.SelectedItem != null)
            {
                (datagrid.SelectedItem as DeviceModel).isselected = false;
            }
        }
    }

}
