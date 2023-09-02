using System;
using System.Collections.Generic;
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
using wpf.Models;

namespace wpf
{
    public delegate void ManualDeviceSaveEventHandler(object sender, AddDeviceEventArgs e);
    /// <summary>
    /// FindDeviceWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ManualDeviceWindow : Window
    {
        public event ManualDeviceSaveEventHandler ManualDeviceSaveEvent;
        private void OnManualDeviceSave()
        {
            if (ManualDeviceSaveEvent != null)
            {
                var model = new DeviceModel()
                {
                    name = txt_name.Text,
                    ipaddress = txt_ipaddress.Text,
                    subnet = txt_subnet.Text,
                    historyurl = txt_historyurl.Text,
                    edgeurl = txt_edgeurl.Text,
                    desc = txt_desc.Text,
                };

                ManualDeviceSaveEvent(this, new AddDeviceEventArgs(model));
            }
        }
        public ManualDeviceWindow()
        {
            InitializeComponent();
        }
        public ManualDeviceWindow(DeviceModel device) : this()
        {
            if (device != null)
            {

                txt_name.IsEnabled = false;
                txt_ipaddress.IsEnabled = false;
                txt_subnet.IsEnabled = false;
                this.DataContext = device;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            OnManualDeviceSave();

        }
    }
}
