using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using wpf.Models;
//using System.IO;
//using System.Windows;
//using System.Windows.Media;
//using System.Windows.Media.Imaging;
using NLog;
using mus.viewer.db.sqlite;
using System.Data.Entity;
using wpf.Rest;
using System.Configuration;
using InteractiveExamples;
using wpf.Cl;
using RestSharp;
using wpf.Controls;
using NAudio.Wave;
using SoftCircuits.IniFileParser;
using System.Net.Configuration;
using Microsoft.Web.WebView2.Core;
using System.Data.Entity.Migrations.History;

namespace wpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        //private ICommand _removeItem;

        //public ICommand RemoveItem
        //{
        //    get { return _removeItem; }
        //    set { _removeItem = value; }
        //}

        //ObservableCollection<DeviceModel> _devices;

        public event PropertyChangedEventHandler PropertyChanged;
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        MusDbContext _db = new MusDbContext();
        int _currenttab = 0;
        DeviceModel _curdevice;
        private static Logger _logger = LogManager.GetCurrentClassLogger();
        public static Logger Logger { get => _logger; }
        LastWavResultApiCaller _detectresultapicaller;
        LastWavResultApiCaller _restapi;
        public string _deviceinifile = ConfigurationManager.AppSettings["DEVICE_INI_FILEPATH"].ToString();

        //public ObservableCollection<DeviceModel> Devices { get => _devices; set { _devices = value; NotifyPropertyChanged("Devices"); }}
        public MainWindow()
        {
            InitializeComponent();
            //_devices = new ObservableCollection<DeviceModel>();
            this.Loaded += async (s, e) =>
            {

                //ini 디바이스 정보 추가
                var devices = GetIniFile();
                foreach (var item in devices)
                {
                    list_device.Items.Add(item);
                }

                //local db 정보 추가
                //foreach (var item in _db.Devices.ToList())
                //{
                //    var itemexists = list_device.Items.OfType<DeviceModel>().Where(m => m.name == item.name && m.ipaddress == item.ipaddress).FirstOrDefault();
                //    if (itemexists != null)
                //    {
                //        list_device.Items.Add(new DeviceModel()
                //        {
                //            name = item.name,
                //            ipaddress = item.ipaddress,
                //            subnet = item.subnet,
                //            desc = item.desc
                //            //collect = item.iscollect.HasValue ? item.iscollect.Value : false,
                //        });
                //    }
                //}
                //_restapi = new LastWavResultApiCaller(null, spec_chart.QueueFiles);
                DownloadFileRemover.Instance.Start();
                CollectDeviceApiCaller.Instance.Start();



                try
                {
                    ///--disable-web-security option enabled all webcontrols
                    CoreWebView2EnvironmentOptions options = new CoreWebView2EnvironmentOptions("--disable-web-security");
                    CoreWebView2Environment environment = await CoreWebView2Environment.CreateAsync(null, null, options);
                    await historywebview.EnsureCoreWebView2Async(environment);
                    await analysiswebview.EnsureCoreWebView2Async(environment);
                    await edgewebview.EnsureCoreWebView2Async(environment);

                }
                catch (Exception ex)
                {
                    Logger.Error(ex);
                }

                //alysiswebview.Source = new Uri(ConfigurationManager.AppSettings["COLLECTING_SERVER_IP"].ToString());
                //alysiswebview.CoreWebView2.Navigate(ConfigurationManager.AppSettings["COLLECTING_SERVER_IP"].ToString());
                //analysiswebview.CoreWebView2InitializationCompleted += (es, ee) =>
                //{
                //    if (ee.IsSuccess)
                //    {
                //        //browser accelate disabled
                //        analysiswebview.CoreWebView2.Settings.AreBrowserAcceleratorKeysEnabled = false;
                //        //contextmenu disabled
                //        analysiswebview.CoreWebView2.Settings.AreDefaultContextMenusEnabled = false;
                //    }
                //};

                //analysiswebview.Source = new Uri(ConfigurationManager.AppSettings["COLLECTING_SERVER_IP"].ToString());
                //analysiswebview.CoreWebView2.Navigate(ConfigurationManager.AppSettings["COLLECTING_SERVER_IP"].ToString());
            };

            this.Closing += (s, e) =>
            {
                var listitem = list_device.Items.Count;
                if (listitem > 0)
                {
                    //foreach (var d in list_device.Items.OfType<DeviceModel>())
                    //{
                    //    var m = new Device()
                    //    {
                    //        name = d.name,
                    //        ipaddress = d.ipaddress,
                    //        subnet = d.subnet,
                    //        historyurl = d.historyurl,
                    //        edgeurl = d.edgeurl,
                    //        desc = d.desc
                    //    };
                    //    var dbm = _db.Devices.Where(c => c.ipaddress.Equals(m.ipaddress)).FirstOrDefault();
                    //    if (dbm == null)
                    //    {
                    //        _db.Devices.Add(m);
                    //    }
                    //    else
                    //    {
                    //        dbm.desc = m.desc;
                    //    }
                    //    _db.SaveChanges();
                    //};

                    DownloadFileRemover.Instance.Stop();
                    CollectDeviceApiCaller.Instance.Stop();

                    foreach (var item in _specwndqueue.ToList())
                    {
                        item.Close();
                    }

                    UpdateIniFile();
                }
            };

        }

        private void LaunchGitHubSite(object sender, RoutedEventArgs e)
        {
            // Launch the GitHub site...
        }

        private void DeployCupCakes(object sender, RoutedEventArgs e)
        {
            // deploy some CupCakes...
        }

        private void btnFindDevice_Click(object sender, RoutedEventArgs e)
        {
            OpenFindDeviceWindow(new[] { this.sip.Text, this.eip.Text, this.subnet.Text, this.hnuri.Text });
        }

        private void OpenFindDeviceWindow(string[] param)
        {
            FindDeviceWindow wnd = new FindDeviceWindow(param);
            wnd.Owner = this;
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            wnd.AddDeviceEvent += (s, e) =>
            {
                if (e.ResetItems)
                {
                    list_device.Items.Clear();
                }
                var ld = list_device.Items.OfType<DeviceModel>();
                foreach (var item in e.Devices)
                {
                    if (ld.Where(c => c.ipaddress == item.ipaddress && c.subnet == item.subnet).FirstOrDefault() == null)
                    {
                        list_device.Items.Add(item);
                        CollectDeviceApiCaller.Instance.DeviceQueue.Enqueue(new CollectDeviceModel(item));
                    }
                }
                wnd.Close();
                UpdateIniFile();
            };
            wnd.ShowDialog();
        }

        private void btn_DeviceManual_Click(object sender, RoutedEventArgs e)
        {
            OpenManualDeviceWindow();
        }

        private void OpenManualDeviceWindow(DeviceModel editdevice = null)
        {
            ManualDeviceWindow wnd = new ManualDeviceWindow(editdevice);
            wnd.Owner = this;
            wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            wnd.ManualDeviceSaveEvent += (s, e) =>
            {
                if (editdevice == null)
                {

                    //var name = wnd.txt_name.Text;
                    //var ip = wnd.txt_ipaddress.Text;
                    //var subnet = wnd.txt_subnet.Text;
                    var device = e.Devices.First();
                    list_device.Items.Add(device);
                    CollectDeviceApiCaller.Instance.DeviceQueue.Enqueue(new CollectDeviceModel(device));
                }
                else
                {
                    var device = e.Devices.First();
                    //var dbitem = _db.Devices.Where(m=>m.name == device.name && m.ipaddress == device.ipaddress).FirstOrDefault();
                    //dbitem.desc = device.desc;
                    //_db.SaveChanges();
                    var item = list_device.Items.OfType<DeviceModel>().Where(m => m.name == device.name && m.ipaddress == device.ipaddress).FirstOrDefault();
                    item.desc = device.desc;
                    list_device.Items.Refresh();

                }
                wnd.Close();
                UpdateIniFile();
            };
            wnd.ShowDialog();
        }

        private bool _tile_viewer = false;

        public bool Tile_viewer
        {
            get { return _tile_viewer; }
            set { _tile_viewer = value; NotifyPropertyChanged("Tile_viewer"); }
        }
        private bool _tile_diagnostics = true;

        public bool Tile_diagnostics
        {
            get { return _tile_diagnostics; }
            set { _tile_diagnostics = value; NotifyPropertyChanged("Tile_diagnostics"); }
        }
        private bool _tile_settings = true;

        public bool Tile_settings
        {
            get { return _tile_settings; }
            set { _tile_settings = value; NotifyPropertyChanged("Tile_settings"); }
        }
        private bool _tile_predict = true;

        public bool Tile_predict
        {
            get { return _tile_predict; }
            set { _tile_predict = value; NotifyPropertyChanged("Tile_predict"); }
        }

        private bool _settings_auth = false;

        private void Tile_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var tile = sender as MahApps.Metro.Controls.Tile;
            //foreach (var item in stack_tile.Children.Cast<MahApps.Metro.Controls.Tile>())
            //{
            //    if (item.Tag != tile.Tag)
            //    {
            //    }
            //}
            var curtab = Convert.ToInt16(tile.Tag);
            _currenttab = curtab;
            tab.SelectedIndex = tab.Items.Count < curtab ? tab.Items.Count - 1 : curtab;
            switch (_currenttab)
            {
                case 0:
                    {
                        this.Tile_viewer = false;
                        this.Tile_diagnostics = true;
                        this.Tile_settings = true;
                        this.Tile_predict = true;
                    }
                    break;
                case 1:
                    {
                        //trendwebview.CoreWebView2.Navigate($"http://{CollectDeviceApiCaller.Instance.RequestUrl}/device/{_curdevice.name}");
                        this.Tile_viewer = true;
                        this.Tile_diagnostics = false;
                        this.Tile_settings = true;
                        this.Tile_predict = true;
                    }
                    break;
                case 2:
                    {
                        this.Tile_viewer = true;
                        this.Tile_diagnostics = true;
                        this.Tile_settings = true;
                        this.Tile_predict = false;
                    }
                    break;
                case 99:
                    {
                        //settingwebview.CoreWebView2 = ($"http://{_curdevice.ipaddress}:3000");
                        this.Tile_viewer = true;
                        this.Tile_diagnostics = true;
                        this.Tile_settings = false;
                        this.Tile_predict = true;
                        _settings_auth = false;

                        var wnd = new SettingsAuthWindow();
                        wnd.Owner = this;
                        wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        wnd.AuthResultEvent += (s, re) =>
                        {
                            settingwebview.IsEnabled = _settings_auth = re.Result;
                            if (_settings_auth == false)
                            {
                                if (MessageBox.Show("Password incorrect, try again.", "", MessageBoxButton.OK) == MessageBoxResult.OK)
                                {
                                    wnd.Close();
                                }
                            }
                            else
                            {
                                wnd.Close();
                            }
                        };
                        wnd.ShowDialog();
                    }
                    break;
                default:
                    break;
            }
        }

        private void DeleteMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Cast the sender to a MenuItem
            MenuItem menuItem = sender as MenuItem;
            if (menuItem != null)
            {
                // Get the context menu that the menu item belongs to
                ContextMenu contextMenu = menuItem.Parent as ContextMenu;
                if (contextMenu != null)
                {
                    // Get the item that the context menu belongs to
                    ListViewItem item = contextMenu.PlacementTarget as ListViewItem;
                    if (item != null)
                    {
                        // Remove the item from the list
                        list_device.Items.Remove(item.DataContext);
                    }
                }
            }
        }

        private void MenuItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

        }

        DeviceModel[] _compDevice = new DeviceModel[2];
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            //(list_device.SelectedItem as DeviceModel).isselected = true;
            //var item = sender;

        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            //(list_device.SelectedItem as DeviceModel).isselected = false;
        }


        private void list_device_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                _curdevice = list_device.SelectedItem as DeviceModel;
                if (_restapi != null)
                {
                    _restapi.Dispose();
                }
                _currenttab = Convert.ToInt16((tab.SelectedItem as TabItem).Tag);
                switch (_currenttab)
                {
                    case 0:
                        {
                            var maxcount = Convert.ToInt16(ConfigurationManager.AppSettings["CHART_MAX_OPEN_WINDOW"]);
                            //if (_specwndqueue.Count >= maxcount)
                            //{
                            //    MessageBox.Show($"sorry. You can only open up to {maxcount} charts.");
                            //    return;
                            //}
                            if (_specwndqueue.Count >= 1) return;
                            ContentPresenter spec_content = new ContentPresenter();
                            if (_restapi != null)
                            {
                                _restapi.Stop();
                            }
                            //spectrum viewer
                            if (spec_content.Content != null)
                            {
                                var specchart = spec_content.Content as SpectrumChartControl;
                                specchart.Stop();
                                specchart.Dispose();
                            }
                            SpectrumChartControl spec_chart = new SpectrumChartControl();
                            //spec_chart.FileOpenEvent -= spec_chart_FileOpenEvent;
                            //spec_chart.FileOpenEvent += spec_chart_FileOpenEvent;
                            spec_content.Content = spec_chart;
                            //_restapi = new LastWavResultApiCaller(null, ref spec_chart.QueueFiles);
                            if (_curdevice != null)
                            {
                                //_restapi = new LastWavResultApiCaller(_curdevice.ipaddress, ref spec_chart.QueueFiles);
                                _restapi = new LastWavResultApiCaller(_curdevice.ipaddress, spec_chart.ChartWavFileQueue);
                                _restapi.Start();

                            }
                            var hbind = new Binding("ActualHeight");
                            hbind.ElementName = "spec_grid";
                            spec_content.SetBinding(HeightProperty, hbind);
                            //spec_content.Height = 400;
                            spec_stack.Children.Clear();
                            spec_stack.Children.Add(spec_content);

                        }
                        break;
                    case 1:
                        {
                            if (_curdevice != null)
                            {
                                var navurl = _curdevice.historyurl;// ConfigurationManager.AppSettings["HISTORY_SERVER_URL"].ToString();
                                if (historywebview.Source == null)
                                {
                                    historywebview.Source = new Uri(navurl);
                                }
                                else
                                {
                                    historywebview.CoreWebView2.Navigate(navurl);
                                }
                                historywebview.NavigationCompleted += async (ns, ne) =>
                                {
                                    //string scripts = $"setInterval(() => document.getElementsByClassName('tb-powered-by-footer ng-star-inserted')[0].innerHTML = '', 2000);";

                                    string scripts = @"let nIntervId;

                                                            function removeBrand() {
                                                              if (!nIntervId) {
                                                                nIntervId = setInterval(removeHTML, 500);
                                                              }
                                                            }

                                                            function removeHTML() {
                                                                if (document.getElementsByClassName('tb-powered-by-footer ng-star-inserted').length >= 1) {
	                                                                document.getElementsByClassName('tb-powered-by-footer ng-star-inserted')[0].innerHTML = '';
	                                                                clearInterval(nIntervId);
	                                                                nInterId = null;
	                                                            }
                                                            } removeBrand();";
                                    await historywebview.CoreWebView2.ExecuteScriptAsync(scripts);
                                };

                                //await historywebview.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('tb-powered-by-footer ng-star-inserted')[0].innerHTML = ''");
                                //await historywebview.CoreWebView2.ExecuteScriptAsync("alert(document.title)");
                            }
                        }
                        break;
                    case 2:
                        {
                            //if (predict_panel.Content != null)
                            //{
                            //    (predict_panel.Content as Controls.PredictControl).Dispose();
                            //}
                            //predict_panel.Content = new PredictControl(_curdevice);
                            if (_curdevice != null)
                            {
                                var navurl = _curdevice.edgeurl; //ConfigurationManager.AppSettings["EDGE_SERVER_URL"].ToString();
                                //edgewebview.CoreWebView2.Navigate(string.Format(navurl, _curdevice.ipaddress));

                                if (edgewebview.Source == null)
                                {
                                    edgewebview.Source = new Uri(navurl);
                                }
                                else
                                {
                                    edgewebview.CoreWebView2.Navigate(navurl);
                                }

                                //edgewebview.NavigationCompleted += async (ns, ne) => {
                                //    await edgewebview.CoreWebView2.ExecuteScriptAsync("document.getElementsByClassName('tb-powered-by-footer ng-star-inserted')[0].innerHTML = ''");
                                //};

                            }
                        }
                        break;
                    case 3:
                        {
                            //if (predict_panel.Content != null)
                            //{
                            //    (predict_panel.Content as Controls.PredictControl).Dispose();
                            //}
                            //predict_panel.Content = new PredictControl(_curdevice);
                            //if (_curdevice != null)
                            //{
                            //    var navurl = ConfigurationManager.AppSettings["COLLECTING_SERVER_IP"].ToString();
                            //    //edgewebview.CoreWebView2.Navigate(string.Format(navurl, _curdevice.ipaddress));
                            //
                            //    if (analysiswebview.Source == null)
                            //    {
                            //        analysiswebview.Source = new Uri(navurl);
                            //    }
                            //    else
                            //    {
                            //        analysiswebview.CoreWebView2.Navigate(navurl);
                            //    }
                            //}
                        }
                        break;
                    case 99:
                        {

                            //setting viewer
                            //settingwebview.Visibility = Visibility.Visible;
                            //settingwebview.IsEnabled = true;
                            //if (_curdevice != null)
                            //{
                            //    if (_settings_auth == false)
                            //    {
                            //        MessageBox.Show("Click the Settings button at the top of the main window, verify, and try again!", "Settings Authenticate", MessageBoxButton.OK);
                            //    }
                            //    else
                            //    {
                            //        settingwebview.CoreWebView2.Navigate($"http://{_curdevice.ipaddress}:3000");
                            //    }
                            //}
                        }
                        break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
            }
        }

        /// <summary>
        /// spectrum chart capture 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
                CaptureControl(spec_chart_grid, System.IO.Path.Combine(path, string.Format("spectrumchart_{0}_{1}.png", _curdevice.ipaddress, DateTime.Now.Ticks)));

            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.Message}");
            }
        }

        /// <summary>
        /// detect trend chart capture
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            try
            {
                var path = Environment.GetFolderPath(System.Environment.SpecialFolder.MyPictures);
                CaptureControl(trendwebview, System.IO.Path.Combine(path, string.Format("trendchart_{0}_{1}.png", _curdevice.ipaddress, DateTime.Now.Ticks)));
            }
            catch (Exception ex)
            {
                _logger.Error($"{ex.Message}");
            }
        }


        public void CaptureControl(UIElement source, string destination)
        {
            try
            {
                Rect bounds = VisualTreeHelper.GetDescendantBounds(source);
                RenderTargetBitmap rtb = new RenderTargetBitmap((int)bounds.Width, (int)bounds.Height, 96, 96, PixelFormats.Default);

                DrawingVisual dv = new DrawingVisual();
                using (DrawingContext ctx = dv.RenderOpen())
                {
                    VisualBrush vb = new VisualBrush(source);
                    ctx.DrawRectangle(vb, null, new Rect(new Point(), bounds.Size));
                }

                rtb.Render(dv);

                PngBitmapEncoder png = new PngBitmapEncoder();
                png.Frames.Add(BitmapFrame.Create(rtb));

                using (Stream stm = File.Create(destination))
                {
                    png.Save(stm);
                    MessageBox.Show($"The captured image file was saved to the path below. \n {destination}", "capture image");
                }

            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error($"CaptureControl Error\n{ex}");
            }
        }

        private void list_device_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var d = e.OriginalSource;
            if (d.GetType() != typeof(System.Windows.Controls.ScrollViewer))
            {
                list_device_SelectionChanged(sender, null);
            }
        }

        private async void spec_chart_FileOpenEvent(object sender, lc.spec_chart.SpectrumOpenFIleEventArgs e)
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

        /// <summary>
        /// ContextMenu Event - Add Collection Device 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <summary>
        private async void MenuItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            //var item = list_device.SelectedItem as DeviceModel;
            //CollectDeviceModel m = new CollectDeviceModel(item.name, item.ipaddress);
            //var re = await CollectDeviceApiCaller.Instance.AddDeviceAsync(m);

            //var selecteditems = list_device.SelectedItems.OfType<DeviceModel>();
            var checkeditems = list_device.Items.OfType<DeviceModel>().Where(m => m.isselected == true);

            StringBuilder sb = new StringBuilder();
            foreach (var item in checkeditems.ToList())
            {
                CollectDeviceModel m = new CollectDeviceModel(item.name, item.ipaddress);
                var re = await CollectDeviceApiCaller.Instance.AddDeviceAsync(m);
                if (re == false)
                {
                    sb.AppendLine($"{item.name} Add Error.");
                }
                if (sb.Length > 0)
                {
                    ToolTip tooltip = new ToolTip();
                    tooltip.Content = new TextBlock() { Text = sb.ToString() };
                    list_device.ToolTip = tooltip;
                }
                else
                {
                    list_device.ToolTip = null;
                }
            }

        }

        /// ContextMenu Event - Delete Device 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void MenuItem_PreviewMouseLeftButtonUp_1(object sender, MouseButtonEventArgs e)
        {
            //var selecteditems = list_device.SelectedItems.OfType<DeviceModel>();
            var checkeditems = list_device.Items.OfType<DeviceModel>().Where(m => m.isselected == true);
            StringBuilder sb = new StringBuilder();
            foreach (var item in checkeditems.ToList())
            {
                CollectDeviceModel m = new CollectDeviceModel(item.name, item.ipaddress);
                var re = await CollectDeviceApiCaller.Instance.SubDeviceAsync(m);
                if (re)
                {
                    list_device.Items.Remove(item);

                    var db_device = _db.Devices.Where(c => c.name == item.name && c.ipaddress == item.ipaddress).FirstOrDefault();
                    _db.Devices.Remove(db_device);
                }
                else
                {
                    sb.AppendLine($"{item.name} Delete Error.");
                }
                if (sb.Length > 0)
                {
                    ToolTip tooltip = new ToolTip();
                    tooltip.Content = new TextBlock() { Text = sb.ToString() };
                    list_device.ToolTip = tooltip;
                    list_device.Focus();
                }
                else
                {
                    list_device.ToolTip = null;
                }
                await _db.SaveChangesAsync();
            }

        }

        /// <summary>
        /// ContextMenu Event - Reboot Device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_PreviewMouseLeftButtonUp_2(object sender, MouseButtonEventArgs e)
        {
            var selecteditems = list_device.SelectedItems.OfType<DeviceModel>().First();
            //var checkeditems = list_device.Items.OfType<DeviceModel>().Where(m => m.isselected == true);

            Task.Run(async () =>
            {
                try
                {
                    var client = new RestClient($"http://{selecteditems.ipaddress}:8082/healthcheck/rebootdaq");
                    var request = new RestRequest();
                    request.Method = Method.Get;

                    var response = await client.ExecuteAsync(request);

                    if (response.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        //result = true;
                    }
                    else
                    {
                        MainWindow.Logger.Error($"Error while reboot device : {response.ErrorMessage} - {selecteditems.name}:{selecteditems.ipaddress}");
                    }
                }
                catch (Exception ex)
                {
                    MainWindow.Logger.Error(ex);
                }
            });


        }

        /// <summary>
        /// ContextMenu Event - Device Force Delete
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_PreviewMouseLeftButtonUp_3(object sender, MouseButtonEventArgs e)
        {
            try
            {
                ContentPresenter spec_panel = spec_stack.Children.OfType<ContentPresenter>().FirstOrDefault();

                if (spec_panel.Content != null)
                {
                    (spec_panel.Content as SpectrumChartControl).Dispose();
                }

                //var selecteditems = list_device.SelectedItems.OfType<DeviceModel>();
                var checkeditems = list_device.Items.OfType<DeviceModel>().Where(m => m.isselected == true);


                foreach (var item in checkeditems.ToList())
                {
                    list_device.Items.Remove(item);
                    var db_device = _db.Devices.Where(c => c.name == item.name && c.ipaddress == item.ipaddress).FirstOrDefault();

                    if (db_device != null) { _db.Devices.Remove(db_device); }
                }
                _db.SaveChangesAsync();

                UpdateIniFile();
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error($" Device Force Delete Exception : {ex}");
            }
        }

        Queue<SpectrogramWindow> _specwndqueue = new Queue<SpectrogramWindow>();
        private void MenuItem_PreviewMouseLeftButtonUp_4(object sender, MouseButtonEventArgs e)
        {
            try
            {
                _curdevice = list_device.SelectedItem as DeviceModel;

                var contentpresenter = spec_stack.Children.OfType<ContentPresenter>().FirstOrDefault();
                if (contentpresenter != null)
                {
                    var specchart = contentpresenter.Content as SpectrumChartControl;
                    _restapi.Dispose();
                    specchart.Dispose();
                    spec_stack.Children.Clear();
                }

                SpectrogramWindow wnd = new SpectrogramWindow(_curdevice);
                if (_specwndqueue.Where(c => c.CurDevice.Equals(_curdevice)).FirstOrDefault() != null)
                {
                    MessageBox.Show("this window is already opened.");
                    return;
                }
                var maxcount = Convert.ToInt16(ConfigurationManager.AppSettings["CHART_MAX_OPEN_WINDOW"]);
                if (_specwndqueue.Count >= maxcount)
                {
                    MessageBox.Show($"sorry. You can only open up to {maxcount} charts.");
                    return;
                }
                SpectrumChartControl spec_chart = new SpectrumChartControl();
                spec_chart.FileOpenEvent -= wnd.spec_chart_FileOpenEvent;
                spec_chart.FileOpenEvent += wnd.spec_chart_FileOpenEvent;
                //spec_panel.Content = spec_chart;
                wnd.spec_content.Content = spec_chart;
                //var restapi;// = new LastWavResultApiCaller(null, spec_chart.QueueFiles);
                if (_curdevice != null)
                {
                    var restapi = new LastWavResultApiCaller(_curdevice.ipaddress, spec_chart.ChartWavFileQueue);
                    restapi.Start();

                    //var c = Task<object>.Run(async delegate
                    //{
                    //    await spec_chart.Start();
                    //});
                    wnd.Closing += (ws, we) =>
                    {
                        spec_chart.Dispose();
                        restapi.Dispose();
                        _specwndqueue.Dequeue();
                    };
                    wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    wnd.Show();
                    _specwndqueue.Enqueue(wnd);
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        /// <summary>
        /// edit item description
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_PreviewMouseLeftButtonUp_5(object sender, MouseButtonEventArgs e)
        {
            var device = list_device.SelectedItem as DeviceModel;
            if (device == null)
            {
                MessageBox.Show("No device selected.");
                return;
            }
            else
            {
                OpenManualDeviceWindow(device);
            }
        }

        /// <summary>
        /// Compare Device
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_PreviewMouseLeftButtonUp_6(object sender, MouseButtonEventArgs e)
        {

            try
            {
                var maxcount = Convert.ToInt16(ConfigurationManager.AppSettings["CHART_MAX_OPEN_WINDOW"]);
                if (_specwndqueue.Count >= maxcount)
                {
                    MessageBox.Show($"sorry. You can only open up to {maxcount} charts.");
                    return;
                }

                var checkeditems = list_device.Items.OfType<DeviceModel>().Where(m => m.isselected == true);
                if (checkeditems == null || checkeditems.Count() != 2)
                {
                    MessageBox.Show("Check the two device to be compared.");
                    return;
                }

                var contentpresenter = spec_stack.Children.OfType<ContentPresenter>().FirstOrDefault();
                if (contentpresenter != null)
                {
                    var specchart = contentpresenter.Content as SpectrumChartControl;
                    if (_restapi != null)
                    {
                        _restapi.Dispose();
                    }
                    specchart.Dispose();
                    spec_stack.Children.Clear();
                }
                switch (checkeditems.Count())
                {
                    case 2: WavCompare_Device2(checkeditems); break;
                    //case 3: WavCompare_Device3(checkeditems); break;
                    //case 4: WavCompare_Device4(checkeditems); break;
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void WavCompare_Device2(IEnumerable<DeviceModel> checkeditems)
        {
            try
            {
                //var maxcount = Convert.ToInt16(ConfigurationManager.AppSettings["CHART_MAX_OPEN_WINDOW"]);
                //if (_specwndqueue.Count >= maxcount)
                //{
                //    MessageBox.Show($"sorry. You can only open up to {maxcount} charts.");
                //    return;
                //}

                //var checkeditems = list_device.Items.OfType<DeviceModel>().Where(m => m.isselected == true);
                //if (checkeditems == null || checkeditems.Count() < 2)
                //{
                //    MessageBox.Show("Check the device to be compared.");
                //    return;
                //}

                //var contentpresenter = spec_stack.Children.OfType<ContentPresenter>().FirstOrDefault();
                //if (contentpresenter != null)
                //{
                //    var specchart = contentpresenter.Content as SpectrumChartControl;
                //    if (_restapi != null)
                //    {
                //        _restapi.Dispose();
                //    }
                //    specchart.Dispose();
                //    spec_stack.Children.Clear();
                //}
                var compareitems = checkeditems.ToList().GetRange(0, 2);
                Wav2MergeMonitor wavcollector = new Wav2MergeMonitor(compareitems.Count);
                int cnt = 0;
                string[] lastwavfiles = new string[compareitems.Count];
                //foreach (var item in checkeditems)
                //{
                var restapi_1 = new LastWavResultApiCaller(compareitems[0].ipaddress);//, spec_chart.QueueFiles);
                restapi_1.CallerResultEvent -= (ws, we) => { };
                restapi_1.CallerResultEvent += (ws, we) =>
                {
                    //wavcollector.WavFiles[cnt++] = we.WavFileName;

                    wavcollector.Wav1 = we.WavFileName;
                };
                restapi_1.Start();

                var restapi_2 = new LastWavResultApiCaller(compareitems[1].ipaddress);//, spec_chart.QueueFiles);
                restapi_2.CallerResultEvent -= (ws, we) => { };
                restapi_2.CallerResultEvent += (ws, we) =>
                {
                    //wavcollector.WavFiles[cnt++] = we.WavFileName;
                    wavcollector.Wav2 = we.WavFileName;
                };
                restapi_2.Start();

                //var restapi_3 = new LastWavResultApiCaller(compareitems[2].ipaddress);//, spec_chart.QueueFiles);
                //restapi_3.CallerResultEvent -= (ws, we) => { };
                //restapi_3.CallerResultEvent += (ws, we) =>
                //{
                //    //wavcollector.WavFiles[cnt++] = we.WavFileName;
                //    wavcollector.Wav3 = we.WavFileName;
                //};
                //restapi_3.Start();

                //var restapi_4 = new LastWavResultApiCaller(compareitems[3].ipaddress);//, spec_chart.QueueFiles);
                //restapi_4.CallerResultEvent -= (ws, we) => { };
                //restapi_4.CallerResultEvent += (ws, we) =>
                //{
                //    //wavcollector.WavFiles[cnt++] = we.WavFileName;
                //    wavcollector.Wav4 = we.WavFileName;
                //};
                //restapi_4.Start();

                //}
                SpectrogramWindow wnd = new SpectrogramWindow($"{compareitems[0].name} - {compareitems[1].name}");
                wnd.dtresult_control.Visibility = Visibility.Collapsed;
                //wnd.gridChart.Visibility = Visibility.Visible;
                SpectrumChartControl spec_chart = new SpectrumChartControl();
                spec_chart.FileOpenEvent -= wnd.spec_chart_FileOpenEvent;
                spec_chart.FileOpenEvent += wnd.spec_chart_FileOpenEvent;
                spec_chart.gridChart.Visibility = Visibility.Visible;
                spec_chart.comboBoxFFTWindowLength.SelectedIndex = 1;
                //spec_chart.FitY();
                spec_chart.IsFitY = false;
                wnd.spec_content.Content = spec_chart;
                _specwndqueue.Enqueue(wnd);
                wnd.Closing += (ws, we) =>
                {
                    spec_chart.Dispose();
                    restapi_1.Dispose();
                    restapi_2.Dispose();
                    //restapi_3.Dispose();
                    //restapi_4.Dispose();
                    _specwndqueue.Dequeue();
                };
                wnd.Closed += (ws, we) =>
                {
                    //foreach(var item in list_device.Items.OfType<DeviceModel>().ToList()
                    foreach (var item in checkeditems)
                    {
                        item.isselected = false;
                    }
                };
                //wnd.Owner = this;
                //wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                ////wnd.Top = 78;
                ////wnd.Left = 219;
                //wnd.ShowDialog();

                bool wav1change = false;
                bool wav2change = false;
                //bool wav3change = false;
                //bool wav4change = false;
                object lockFlags = new object();

                bool _isfit = false;
                wavcollector.PropertyChanged += (ps, pe) =>
                {
                    lock (lockFlags)
                    {
                        if (pe.PropertyName == nameof(wavcollector.Wav1))
                        {
                            wav1change = true;
                        }
                        if (pe.PropertyName == nameof(wavcollector.Wav2))
                        {
                            wav2change = true;
                        }
                        //if (pe.PropertyName == nameof(wavcollector.Wav3))
                        //{
                        //    wav3change = true;
                        //}
                        //if (pe.PropertyName == nameof(wavcollector.Wav4))
                        //{
                        //    wav4change = true;
                        //}

                        if (wav1change && wav2change /*&& wav3change && wav4change*/)
                        {
                            //spec_chart.QueueFiles.Enqueue(wavcollector.MergeFilePath);
                            spec_chart.ChartWavFileQueue.Enqueue(wavcollector.MergeFilePath);

                            wav1change = false;
                            wav2change = false;
                            //wav3change = false;
                            //wav4change = false;
                            //if (!_isfit)
                            //{
                            //    spec_chart.FitY();
                            //    _isfit = true;
                            //}
                        }
                    }
                };

                //_curdevice = null;
                wnd.Owner = this;
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                //wnd.Top = 78;
                //wnd.Left = 219;
                wnd.ShowDialog();

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void WavCompare_Device3(IEnumerable<DeviceModel> checkeditems)
        {
            try
            {
                //var maxcount = Convert.ToInt16(ConfigurationManager.AppSettings["CHART_MAX_OPEN_WINDOW"]);
                //if (_specwndqueue.Count >= maxcount)
                //{
                //    MessageBox.Show($"sorry. You can only open up to {maxcount} charts.");
                //    return;
                //}

                //var checkeditems = list_device.Items.OfType<DeviceModel>().Where(m => m.isselected == true);
                //if (checkeditems == null || checkeditems.Count() < 2)
                //{
                //    MessageBox.Show("Check the device to be compared.");
                //    return;
                //}

                //var contentpresenter = spec_stack.Children.OfType<ContentPresenter>().FirstOrDefault();
                //if (contentpresenter != null)
                //{
                //    var specchart = contentpresenter.Content as SpectrumChartControl;
                //    if (_restapi != null)
                //    {
                //        _restapi.Dispose();
                //    }
                //    specchart.Dispose();
                //    spec_stack.Children.Clear();
                //}
                var compareitems = checkeditems.ToList().GetRange(0, 3);
                Wav2MergeMonitor wavcollector = new Wav2MergeMonitor(compareitems.Count);
                int cnt = 0;
                string[] lastwavfiles = new string[compareitems.Count];
                //foreach (var item in checkeditems)
                //{
                var restapi_1 = new LastWavResultApiCaller(compareitems[0].ipaddress);//, spec_chart.QueueFiles);
                restapi_1.CallerResultEvent -= (ws, we) => { };
                restapi_1.CallerResultEvent += (ws, we) =>
                {
                    //wavcollector.WavFiles[cnt++] = we.WavFileName;

                    wavcollector.Wav1 = we.WavFileName;
                };
                restapi_1.Start();

                var restapi_2 = new LastWavResultApiCaller(compareitems[1].ipaddress);//, spec_chart.QueueFiles);
                restapi_2.CallerResultEvent -= (ws, we) => { };
                restapi_2.CallerResultEvent += (ws, we) =>
                {
                    //wavcollector.WavFiles[cnt++] = we.WavFileName;
                    wavcollector.Wav2 = we.WavFileName;
                };
                restapi_2.Start();

                var restapi_3 = new LastWavResultApiCaller(compareitems[2].ipaddress);//, spec_chart.QueueFiles);
                restapi_3.CallerResultEvent -= (ws, we) => { };
                restapi_3.CallerResultEvent += (ws, we) =>
                {
                    //wavcollector.WavFiles[cnt++] = we.WavFileName;
                    wavcollector.Wav3 = we.WavFileName;
                };
                restapi_3.Start();

                //var restapi_4 = new LastWavResultApiCaller(compareitems[3].ipaddress);//, spec_chart.QueueFiles);
                //restapi_4.CallerResultEvent -= (ws, we) => { };
                //restapi_4.CallerResultEvent += (ws, we) =>
                //{
                //    //wavcollector.WavFiles[cnt++] = we.WavFileName;
                //    wavcollector.Wav4 = we.WavFileName;
                //};
                //restapi_4.Start();

                //}
                SpectrogramWindow wnd = new SpectrogramWindow($"{compareitems[0].name} - {compareitems[1].name} - {compareitems[2].name}");
                wnd.dtresult_control.Visibility = Visibility.Collapsed;
                //wnd.gridChart.Visibility = Visibility.Visible;
                SpectrumChartControl spec_chart = new SpectrumChartControl();
                spec_chart.FileOpenEvent -= wnd.spec_chart_FileOpenEvent;
                spec_chart.FileOpenEvent += wnd.spec_chart_FileOpenEvent;
                spec_chart.gridChart.Visibility = Visibility.Visible;
                spec_chart.comboBoxFFTWindowLength.SelectedIndex = 1;
                //spec_chart.FitY();
                spec_chart.IsFitY = false;
                wnd.spec_content.Content = spec_chart;
                _specwndqueue.Enqueue(wnd);
                wnd.Closing += (ws, we) =>
                {
                    spec_chart.Dispose();
                    restapi_1.Dispose();
                    restapi_2.Dispose();
                    restapi_3.Dispose();
                    //restapi_4.Dispose();
                    _specwndqueue.Dequeue();
                };
                //wnd.Owner = this;
                //wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                //wnd.ShowDialog();

                bool wav1change = false;
                bool wav2change = false;
                bool wav3change = false;
                //bool wav4change = false;
                object lockFlags = new object();

                bool _isfit = false;
                wavcollector.PropertyChanged += (ps, pe) =>
                {
                    lock (lockFlags)
                    {
                        if (pe.PropertyName == nameof(wavcollector.Wav1))
                        {
                            wav1change = true;
                        }
                        if (pe.PropertyName == nameof(wavcollector.Wav2))
                        {
                            wav2change = true;
                        }
                        if (pe.PropertyName == nameof(wavcollector.Wav3))
                        {
                            wav3change = true;
                        }
                        //if (pe.PropertyName == nameof(wavcollector.Wav4))
                        //{
                        //    wav4change = true;
                        //}

                        if (wav1change && wav2change && wav3change /*&& wav4change*/)
                        {
                            //spec_chart.QueueFiles.Enqueue(wavcollector.MergeFilePath);
                            spec_chart.ChartWavFileQueue.Enqueue(wavcollector.MergeFilePath);

                            wav1change = false;
                            wav2change = false;
                            wav3change = false;
                            //wav4change = false;
                            //if (!_isfit)
                            //{
                            //    spec_chart.FitY();
                            //    _isfit = true;
                            //}
                        }
                    }
                };
                //_curdevice = null;
                wnd.Owner = this;
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                wnd.ShowDialog();

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        private void WavCompare_Device4(IEnumerable<DeviceModel> checkeditems)
        {
            try
            {
                //var maxcount = Convert.ToInt16(ConfigurationManager.AppSettings["CHART_MAX_OPEN_WINDOW"]);
                //if (_specwndqueue.Count >= maxcount)
                //{
                //    MessageBox.Show($"sorry. You can only open up to {maxcount} charts.");
                //    return;
                //}

                //var checkeditems = list_device.Items.OfType<DeviceModel>().Where(m => m.isselected == true);
                //if (checkeditems == null || checkeditems.Count() < 2)
                //{
                //    MessageBox.Show("Check the device to be compared.");
                //    return;
                //}

                //var contentpresenter = spec_stack.Children.OfType<ContentPresenter>().FirstOrDefault();
                //if (contentpresenter != null)
                //{
                //    var specchart = contentpresenter.Content as SpectrumChartControl;
                //    if (_restapi != null)
                //    {
                //        _restapi.Dispose();
                //    }
                //    specchart.Dispose();
                //    spec_stack.Children.Clear();
                //}
                var compareitems = checkeditems.ToList().GetRange(0, 4);
                Wav2MergeMonitor wavcollector = new Wav2MergeMonitor(compareitems.Count);
                int cnt = 0;
                string[] lastwavfiles = new string[compareitems.Count];
                //foreach (var item in checkeditems)
                //{
                var restapi_1 = new LastWavResultApiCaller(compareitems[0].ipaddress);//, spec_chart.QueueFiles);
                restapi_1.CallerResultEvent -= (ws, we) => { };
                restapi_1.CallerResultEvent += (ws, we) =>
                {
                    //wavcollector.WavFiles[cnt++] = we.WavFileName;

                    wavcollector.Wav1 = we.WavFileName;
                };
                restapi_1.Start();

                var restapi_2 = new LastWavResultApiCaller(compareitems[1].ipaddress);//, spec_chart.QueueFiles);
                restapi_2.CallerResultEvent -= (ws, we) => { };
                restapi_2.CallerResultEvent += (ws, we) =>
                {
                    //wavcollector.WavFiles[cnt++] = we.WavFileName;
                    wavcollector.Wav2 = we.WavFileName;
                };
                restapi_2.Start();

                var restapi_3 = new LastWavResultApiCaller(compareitems[2].ipaddress);//, spec_chart.QueueFiles);
                restapi_3.CallerResultEvent -= (ws, we) => { };
                restapi_3.CallerResultEvent += (ws, we) =>
                {
                    //wavcollector.WavFiles[cnt++] = we.WavFileName;
                    wavcollector.Wav3 = we.WavFileName;
                };
                restapi_3.Start();

                var restapi_4 = new LastWavResultApiCaller(compareitems[3].ipaddress);//, spec_chart.QueueFiles);
                restapi_4.CallerResultEvent -= (ws, we) => { };
                restapi_4.CallerResultEvent += (ws, we) =>
                {
                    //wavcollector.WavFiles[cnt++] = we.WavFileName;
                    wavcollector.Wav4 = we.WavFileName;
                };
                restapi_4.Start();

                //}
                SpectrogramWindow wnd = new SpectrogramWindow($"{compareitems[0].name} - {compareitems[1].name} - {compareitems[2].name} - {compareitems[3].name}");
                wnd.dtresult_control.Visibility = Visibility.Collapsed;
                //wnd.gridChart.Visibility = Visibility.Visible;
                SpectrumChartControl spec_chart = new SpectrumChartControl();
                spec_chart.FileOpenEvent -= wnd.spec_chart_FileOpenEvent;
                spec_chart.FileOpenEvent += wnd.spec_chart_FileOpenEvent;
                spec_chart.gridChart.Visibility = Visibility.Visible;
                spec_chart.comboBoxFFTWindowLength.SelectedIndex = 1;
                //spec_chart.FitY();
                spec_chart.IsFitY = false;
                wnd.spec_content.Content = spec_chart;
                _specwndqueue.Enqueue(wnd);
                wnd.Closing += (ws, we) =>
                {
                    spec_chart.Dispose();
                    restapi_1.Dispose();
                    restapi_2.Dispose();
                    restapi_3.Dispose();
                    restapi_4.Dispose();
                    _specwndqueue.Dequeue();
                };
                //wnd.Owner = this;
                //wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                //wnd.ShowDialog();

                bool wav1change = false;
                bool wav2change = false;
                bool wav3change = false;
                bool wav4change = false;
                object lockFlags = new object();

                bool _isfit = false;
                wavcollector.PropertyChanged += (ps, pe) =>
                {
                    lock (lockFlags)
                    {
                        if (pe.PropertyName == nameof(wavcollector.Wav1))
                        {
                            wav1change = true;
                        }
                        if (pe.PropertyName == nameof(wavcollector.Wav2))
                        {
                            wav2change = true;
                        }
                        if (pe.PropertyName == nameof(wavcollector.Wav3))
                        {
                            wav3change = true;
                        }
                        if (pe.PropertyName == nameof(wavcollector.Wav4))
                        {
                            wav4change = true;
                        }

                        if (wav1change && wav2change && wav3change && wav4change)
                        {
                            //spec_chart.QueueFiles.Enqueue(wavcollector.MergeFilePath);
                            spec_chart.ChartWavFileQueue.Enqueue(wavcollector.MergeFilePath);

                            wav1change = false;
                            wav2change = false;
                            wav3change = false;
                            wav4change = false;
                            //if (!_isfit)
                            //{
                            //    spec_chart.FitY();
                            //    _isfit = true;
                            //}
                        }
                    }
                };
                //_curdevice = null;
                wnd.Owner = this;
                wnd.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                wnd.ShowDialog();

            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }



        /// <summary>
        /// Export Device Information - ini file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_PreviewMouseLeftButtonUp_7(object sender, MouseButtonEventArgs e)
        {
            //var parser = new FileIniDataParser();
            //var data = parser.ReadFile("config.ini");

            var devices = list_device.Items.OfType<DeviceModel>().ToList();
            UpdateIniFile(devices);
        }


        public void UpdateIniFile(List<DeviceModel> devices = null)
        {
            try
            {
                if (devices == null)
                {
                    devices = list_device.Items.OfType<DeviceModel>().ToList();
                }
                IniFile file = new IniFile();
                foreach (var item in devices)
                {
                    file.SetSetting(item.name, "name", item.name);
                    file.SetSetting(item.name, "ipaddress", item.ipaddress);
                    file.SetSetting(item.name, "subnet", item.subnet);
                    file.SetSetting(item.name, "historyurl", item.historyurl);
                    file.SetSetting(item.name, "edgeurl", item.edgeurl);
                    file.SetSetting(item.name, "desc", item.desc);
                    file.SetSetting(item.name, "guid", item.guid);
                }

                //file.SetSetting("history_url", "name", _history_url.name);
                //file.SetSetting("history_url", "ipaddress", _history_url.ipaddress);
                ////file.SetSetting("history_url", "subnet", _history_url.subnet);
                //file.SetSetting("history_url", "desc", _history_url.desc);
                ////file.SetSetting("history_url", "guid", _history_url.guid);

                //file.SetSetting("edge_url", "name", _edge_url.name);
                //file.SetSetting("edge_url", "ipaddress", _edge_url.ipaddress);
                ////file.SetSetting("edge_url", "subnet", _edge_url.subnet);
                //file.SetSetting("edge_url", "desc", _edge_url.desc);
                ////file.SetSetting("edge_url", "guid", _edge_url.guid);
                file.Save(System.IO.Path.Combine(Environment.CurrentDirectory, _deviceinifile));

            }
            catch (Exception ex)
            {
                Logger.Error($"UpdateIniFIle Error : {ex.ToString()}");
            }
        }

        DeviceModel _history_url, _edge_url = null;


        public List<DeviceModel> GetIniFile()
        {
            try
            {
                IniFile file = new IniFile();

                file.Load(System.IO.Path.Combine(Environment.CurrentDirectory, _deviceinifile));

                IEnumerable<string> sections = file.GetSections();
                List<DeviceModel> devices = new List<DeviceModel>();
                foreach (string section in sections)
                {
                    var model = new DeviceModel();
                    model.name = file.GetSetting(section, "name");
                    model.ipaddress = file.GetSetting(section, "ipaddress");
                    model.subnet = file.GetSetting(section, "subnet");
                    model.historyurl = file.GetSetting(section, "historyurl");
                    model.edgeurl = file.GetSetting(section, "edgeurl");
                    model.guid = file.GetSetting(section, "guid");
                    model.desc = file.GetSetting(section, "desc");
                    devices.Add(model);

                    //switch (model.name)
                    //{
                    //    case "history_url":
                    //        {
                    //            _history_url = model;
                    //        }
                    //        break;
                    //    case "edge_url":
                    //        {
                    //            _edge_url = model;
                    //        }
                    //        break;
                    //    default:
                    //        {
                    //            model.subnet = file.GetSetting(section, "subnet");
                    //            model.guid = file.GetSetting(section, "guid");
                    //            devices.Add(model);
                    //        }
                    //        break;
                    //}
                }
                return devices;
            }
            catch (Exception ex)
            {
                Logger.Error($"GetIniFile Error : {ex}");
            }
            return new List<DeviceModel>();
        }


        /// <summary>
        /// all checked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {
            foreach (var item in list_device.Items.OfType<DeviceModel>().ToList())
            {
                item.isselected = true;
            }
        }

        /// <summary>
        /// all unchecked
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CheckBox_Unchecked_1(object sender, RoutedEventArgs e)
        {
            foreach (var item in list_device.Items.OfType<DeviceModel>().ToList())
            {
                item.isselected = false;
            }
        }

        private void tab_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //list_device_SelectionChanged(sender, e);
            var tabitem = tab.SelectedItem as TabItem;
            switch (tabitem.Tag.ToString())
            {
                case "0":
                    {
                        //list_device_SelectionChanged(list_device, null);
                    }

                    break;
                case "1":
                    {
                        list_device_SelectionChanged(this, null);
                    }
                    break;
                case "2":
                    {
                        list_device_SelectionChanged(this, null);
                    }
                    break;
                case "3":
                    {
                        var navurl = ConfigurationManager.AppSettings["COLLECTING_SERVER_IP"].ToString();
                        //edgewebview.CoreWebView2.Navigate(string.Format(navurl, _curdevice.ipaddress));

                        if (analysiswebview.Source == null)
                        {
                            analysiswebview.Source = new Uri(navurl);
                        }
                        else
                        {
                            analysiswebview.CoreWebView2.Navigate(navurl);
                        }

                        analysiswebview.NavigationCompleted += async (ns, ne) =>
                        {
                            //string scripts = $"setInterval(() => document.getElementsByClassName('tb-powered-by-footer ng-star-inserted')[0].innerHTML = '', 2000);";
                            string scripts = @"
                                        let nIntervId;

                                        function removeBrand() {
                                          if (!nIntervId) {
                                            nIntervId = setInterval(removeHTML, 500);
                                          }
                                        }

                                        function removeHTML() {
                                            if (document.getElementsByClassName('tb-powered-by-footer ng-star-inserted').length >= 1) {
	                                        document.getElementsByClassName('tb-powered-by-footer ng-star-inserted')[0].innerHTML = '';
	                                        clearInterval(nIntervId);
	                                        nInterId = null;
	                                        }
                                        } removeBrand();";

                            await analysiswebview.CoreWebView2.ExecuteScriptAsync(scripts);
                        };


                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// 디바이스 수동 추가
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MenuItem_PreviewMouseLeftButtonUp_8(object sender, MouseButtonEventArgs e)
        {
            OpenManualDeviceWindow();
        }
    }



    public class Wav2MergeMonitor : INotifyPropertyChanged
    {
        public string Wav1
        {
            get => _wavfiles[0];
            set
            {
                _wavfiles[0] = value;
                OnPropertyChanged(nameof(Wav1));
            }
        }
        public string Wav2
        {
            get => _wavfiles[1];
            set
            {
                _wavfiles[1] = value;
                OnPropertyChanged(nameof(Wav2));
            }
        }

        public string Wav3
        {
            get => _wavfiles[2];
            set
            {
                _wavfiles[2] = value;
                OnPropertyChanged(nameof(Wav3));
            }
        }
        public string Wav4
        {
            get => _wavfiles[3];
            set
            {
                _wavfiles[3] = value;
                OnPropertyChanged(nameof(Wav4));
            }
        }

        public string[] _wavfiles;// = new string[4];

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string[] WavFiles
        {
            get => _wavfiles;
            set
            {
                _wavfiles = value;
                OnPropertyChanged(nameof(WavFiles));
            }
        }
        private string _wavfilesavefolder = ConfigurationManager.AppSettings["WAV_FILE_SAVE_FOLDER"].ToString();

        public string MergeFilePath
        {
            get
            {
                try
                {
                    switch (_wavfiles.Length)
                    {
                        case 2:
                            {
                                if (_wavfiles[0] != null && _wavfiles[1] != null)
                                {
                                    string outputpath = System.IO.Path.Combine(_wavfilesavefolder, $"{Guid.NewGuid().ToString()}.wav");
                                    //string outputpath = $"{Guid.NewGuid().ToString()}.wav";
                                    Merge2WavFiles.Wav2Merge.Merge(_wavfiles[1], _wavfiles[0], ref outputpath);
                                    return outputpath;
                                }
                            }
                            break;
                        case 3:
                            {
                                if (_wavfiles[0] != null && _wavfiles[1] != null && _wavfiles[2] != null)
                                {
                                    string outputpath = System.IO.Path.Combine(_wavfilesavefolder, $"{Guid.NewGuid().ToString()}.wav");
                                    //string outputpath = $"{Guid.NewGuid().ToString()}.wav";
                                    Merge2WavFiles.Wav2Merge.Merge(_wavfiles[0], _wavfiles[1], _wavfiles[2], ref outputpath);
                                    return outputpath;
                                }
                            }
                            break;
                        case 4:
                            {
                                if (_wavfiles[0] != null && _wavfiles[1] != null && _wavfiles[2] != null && _wavfiles[3] != null)
                                {
                                    string outputpath = System.IO.Path.Combine(_wavfilesavefolder, $"{Guid.NewGuid().ToString()}.wav");
                                    //string outputpath = $"{Guid.NewGuid().ToString()}.wav";
                                    Merge2WavFiles.Wav2Merge.Merge(_wavfiles[0], _wavfiles[1], _wavfiles[2], _wavfiles[3], ref outputpath);
                                    return outputpath;
                                }
                            }
                            break;
                        default:
                            break;
                    }

                }
                catch (Exception ex)
                {
                    MainWindow.Logger.Error(ex);
                }
                return null;
            }
        }
        public Wav2MergeMonitor(int wavcount)
        {
            _wavfiles = new string[wavcount];
        }
        //public Wav2MergeMonitor(string wav1, string wav2)
        //{
        //    _wavfiles[0] = wav1;
        //    _wavfiles[1] = wav2;

        //    //_wav1 = wav1;
        //    //_wav2 = wav2;
        //}

        //public Wav2MergeMonitor(string wav1, string wav2, string wav3)
        //{
        //    _wavfiles[0] = wav1;
        //    _wavfiles[1] = wav2;
        //    _wavfiles[2] = wav3;

        //    //_wav1 = wav1;
        //    //_wav2 = wav2;
        //}

    }
}
