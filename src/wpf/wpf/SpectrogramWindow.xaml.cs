using InteractiveExamples;
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
using wpf.Rest;

namespace wpf
{
    /// <summary>
    /// SpectrogramWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SpectrogramWindow : Window
    {
        DeviceModel _curdevice;
        public DeviceModel CurDevice { get => _curdevice; }

        public SpectrogramWindow(string title)
        {
            InitializeComponent();
            this.Title = title;
        }
        public SpectrogramWindow(DeviceModel curdevice)
        {
            InitializeComponent();
            _curdevice = curdevice;
            this.Title = $"{_curdevice.name} <> {_curdevice.ipaddress}";
        }

        public async void spec_chart_FileOpenEvent(object sender, lc.spec_chart.SpectrumOpenFIleEventArgs e)
        {
            if (_curdevice != null)
            {
                var filepath = e.FilePath;
                var re = Task<DetectResultJsonData>.Run(() =>
                {
                    using (DetectResultApiCaller d = new DetectResultApiCaller(_curdevice.ipaddress))
                    {
                        return d.Call();
                    }
                });
                if (re.Result != null)
                {
                    dtresult_control.DataContext = re.Result;
                }

            }
        }

    }
}
