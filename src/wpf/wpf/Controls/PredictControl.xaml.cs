using Arction.Wpf.Charting;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
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
using System.Windows.Navigation;
using System.Windows.Shapes;
using wpf.Models;
using wpf.Rest;

namespace wpf.Controls
{
    /// <summary>
    /// PredictControl.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class PredictControl : UserControl, IDisposable
    {
        protected DeviceModel _device;
        protected ClassifierResultApiCaller _classifierapi;
        protected RulResultApiCaller _rulapi;
        public PredictControl()
        {
            InitializeComponent();
            _classifierapi = new ClassifierResultApiCaller();
            _rulapi = new RulResultApiCaller();

            this.Loaded += (s, e) =>
            {
                InitializeHeatmapModel3();

                DataContext = this;
            };
        }

        public PredictControl(DeviceModel device) : this()
        {
            _device = device;
        }

        //private List<List<int>> heatmapData = new List<List<int>>();
        private double[,] heatmapData = new double[10, 10]
        {
            { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
            { 2, 4, 6, 8, 10, 12, 14, 16, 18, 20 },
            { 3, 6, 9, 12, 15, 18, 21, 24, 27, 30 },
            { 4, 8, 12, 16, 20, 24, 28, 32, 36, 40 },
            { 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 },
            { 6, 12, 18, 24, 30, 36, 42, 48, 54, 60 },
            { 7, 14, 21, 28, 35, 42, 49, 56, 63, 70 },
            { 8, 16, 24, 32, 40, 48, 56, 64, 72, 80 },
            { 9, 18, 27, 36, 45, 54, 63, 72, 81, 90 },
            { 10, 20, 30, 40, 50, 60, 70, 80, 90, 100 }
        };

        public PlotModel HeatmapModel { get; set; }

        private void InitializeHeatmapModel()
        {
            //for (int i = 0; i < 10; i++)
            //{
            //    for (int x = 0; x < 10; x++)
            //    {
            //    }
            //}

            HeatmapModel = new PlotModel();

            var heatSeries = new HeatMapSeries
            {
                X0 = 0,
                X1 = heatmapData.GetLength(1) - 1,
                Y0 = 0,
                Y1 = heatmapData.GetLength(0) - 1,
                Interpolate = false
            };

            //for (int i = 0; i < heatmapData.GetLength(0); i++)
            //{
            //    for (int j = 0; j < heatmapData.GetLength(1); j++)
            //    {
            //        //heatSeries.Data.Add(new HeatMapData(j, i, heatmapData[i, j]));
            //        heatSeries.Data[i, j] = heatmapData[i, j];
            //    }
            //}
            heatSeries.Data = heatmapData;

            HeatmapModel.Series.Add(heatSeries);

            HeatmapModel.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Minimum = -0.5, Maximum = heatmapData.GetLength(1) - 0.5 });
            HeatmapModel.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = -0.5, Maximum = heatmapData.GetLength(0) - 0.5 });

            HeatmapModel.PlotType = PlotType.Cartesian;

            //HeatmapModel.DefaultColors = OxyPalettes.Jet(256);
            //HeatmapModel.DefaultColors = (IList<OxyColor>)OxyPalettes.Jet(256);
            //HeatmapModel.DefaultColors = OxyPalettes.Jet(256).Colors;
            //HeatmapModel.DefaultColors = 
            // Color axis
            HeatmapModel.Axes.Add(new LinearColorAxis
            {
                Palette = OxyPalettes.Jet(256)
            });


            HeatmapModel.Title = "Heatmap Example";
            this.plotview.Model = HeatmapModel;
        }

        public void InitializeHeatmapModel2()
        {
            var model = new PlotModel { Title = "Heatmap" };

            // Color axis (the X and Y axes are generated automatically)
            model.Axes.Add(new LinearColorAxis
            {
                Palette = OxyPalettes.Rainbow(100)
            });

            // generate 1d normal distribution
            var singleData = new double[100];
            for (int x = 0; x < 100; ++x)
            {
                singleData[x] = Math.Exp((-1.0 / 2.0) * Math.Pow(((double)x - 50.0) / 20.0, 2.0));
            }

            // generate 2d normal distribution
            var data = new double[100, 100];
            for (int x = 0; x < 100; ++x)
            {
                for (int y = 0; y < 100; ++y)
                {
                    data[y, x] = singleData[x] * singleData[(y + 30) % 100] * 100;
                }
            }

            var heatMapSeries = new HeatMapSeries
            {
                X0 = 0,
                X1 = 99,
                Y0 = 0,
                Y1 = 99,
                Interpolate = true,
                RenderMethod = HeatMapRenderMethod.Bitmap,
                Data = data
            };

            model.Series.Add(heatMapSeries);
            plotview.Model = model;
        }

        public void InitializeHeatmapModel3()
        {
            var model = new PlotModel { Title = "Classifier Result Heatmap" };

            // Weekday axis (horizontal)
            model.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Bottom,

                // Key used for specifying this axis in the HeatMapSeries
                Key = "WeekdayAxis",

                // Array of Categories (see above), mapped to one of the coordinates of the 2D-data array
                ItemsSource = new[]
                {
                        "Monday",
                        "Tuesday",
                        "Wednesday",
                        "Thursday",
                        "Friday",
                        "Saturday",
                        "Sunday"
                }
            });

            // Cake type axis (vertical)
            model.Axes.Add(new CategoryAxis
            {
                Position = AxisPosition.Left,
                Key = "CakeAxis",
                ItemsSource = new[]
                {
                    "Apple cake",
                    "Baumkuchen",
                    "Bundt cake",
                    "Chocolate cake",
                    "Carrot cake"
                    }
            });

            // Color axis
            model.Axes.Add(new LinearColorAxis
            {
                Palette = OxyPalettes.Hot(5)
                //Palette = OxyPalettes.Jet(5)
            });

            var rand = new Random();
            var data = new double[100, 10];
            for (int x = 0; x < 10; ++x)
            {
                for (int y = 0; y < 100; ++y)
                {
                    data[y, x] = rand.Next(0, 200) * (0.13 * (y + 1));
                }
            }

            var heatMapSeries = new HeatMapSeries
            {
                X0 = 0,
                X1 = 6,
                Y0 = 0,
                Y1 = 4,
                XAxisKey = "WeekdayAxis",
                YAxisKey = "CakeAxis",
                RenderMethod = HeatMapRenderMethod.Rectangles,
                LabelFontSize = 0.2, // neccessary to display the label
                Data = data
            };
            //heatMapSeries.IsVisible = false;
            model.Series.Add(heatMapSeries);
            plotview.Model = model;
        }

        /// <summary>
        /// classfier stop button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Task.Run(async () =>
            {
                progress_classifier.IsActive = true;

                var model = new Rest.ClassifierModel(_device);
                var re = await _classifierapi.StopAsync(model);
                await Task.Delay(3000);

                progress_classifier.IsActive = false;
            });
        }

        /// <summary>
        /// classifier start button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_PreviewMouseUp_1(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                progress_classifier.IsActive = true;
                progress_classifier.Visibility = Visibility.Visible;

                var model = new Rest.ClassifierModel(_device.name, (int)classifier_num.Value);
                var re = await _classifierapi.StartAsync(model) as List<ClassifierModel>;

                classifier_list.Items.Clear();
                foreach (var item in re.ToList())
                {
                    classifier_list.Items.Add(item);
                }
                //classifier_list.ItemsSource = re;
                classifier_list.UpdateLayout();

                //await Task.Delay(3000);
                progress_classifier.IsActive = false;
                progress_classifier.Visibility = Visibility.Collapsed;
            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        /// <summary>
        /// rul start button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_PreviewMouseUp_2(object sender, MouseButtonEventArgs e)
        {
            Dispatcher.Invoke(async () =>
            {
                progress_rul.IsActive = true;
                progress_rul.Visibility = Visibility.Visible;
                var model = new RulModel(_device);
                var re = await _rulapi.StartAsync(model) as List<RulModel>;
                txt_predict.Text = re[0].predict.ToString();
                txt_similarity.Text = $"{re[0].similarity.ToString()}%";
                await Task.Delay(1000);
                progress_rul.IsActive = false;
                progress_rul.Visibility = Visibility.Collapsed;
            }, System.Windows.Threading.DispatcherPriority.Normal);
        }

        /// <summary>
        /// rul stop button click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Button_PreviewMouseUp_3(object sender, MouseButtonEventArgs e)
        {

        }

        public void Dispose()
        {
            _classifierapi = null;
            _rulapi = null;
            plotview.ResetAllAxes();
            plotview = null;
        }
    }
}
