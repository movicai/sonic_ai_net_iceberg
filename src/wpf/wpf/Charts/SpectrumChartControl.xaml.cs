// ------------------------------------------------------------------------------------------------------------------------
// LightningChart® example code: Shows Singal Reader component to read data from file and diplay on differend FFT displays.
// E.g. 3D spectrum; Spectrogram; Line spectrum; and Waveform.
//
// If you need any assistance, or notice error in this example code, please contact support@lightningchart.com. 
//
// Permission to use this code in your application comes with LightningChart® license. 
//
// https://lightningchart.com | support@lightningchart.com | sales@lightningchart.com
//
// © LightningChart Ltd 2009-2023. All rights reserved.  
// ------------------------------------------------------------------------------------------------------------------------
using Arction.Wpf.Charting;
using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.ChartManager;
using Arction.Wpf.Charting.Series3D;
using Arction.Wpf.Charting.SeriesXY;
using Arction.Wpf.Charting.Views.ViewXY;
using Arction.Wpf.SignalProcessing;
using lc.spec_chart;
using MahApps.Metro.Controls;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Media;
using System.Windows.Threading;
using wpf;
using wpf.Charts;
using static Arction.Wpf.Charting.LightningChart;

namespace InteractiveExamples
{
    /// <summary>
    /// Interaction logic for ExampleAudioSpectrogram3D.xaml.
    /// </summary>
    public partial class SpectrumChartControl : UserControl, IDisposable, INotifyPropertyChanged
    {
        private bool _clean;
        private LightningChart _chart;
        private ChartManager _chartManager;
        private SignalReader _signalReader;
        protected int _maxPf;
        private double _newX = 0;
        private int _intervalFFT = 50;
        private int _channelCount = 0;
        private int _samplingFrequency = 0;
        private bool m_bGridVisible = false;
        private int _resolution = 512;
        private static int _topFrequency = 0;
        public static int TopFrequency { get => _topFrequency; }
        private OpenFileDialog _openFileDialog;
        private DispatcherTimer _dispatcherTimerAutoYFit;
        private AreaSpectrumMonitor[] _lineSpectrumMonitors;
        private Spectrogram3D[] _spectrograms3D;
        private Spectrogram2D[] _spectrograms2D;
        private bool _calculateFFT = false;
        private RealtimeFFTCalculator _calculatorFFT;
        public event SpectrumOpenFileEventHanlder FileOpenEvent;
        private ChartWavFileQueue _chartWavFileQueue = new ChartWavFileQueue();
        public ChartWavFileQueue ChartWavFileQueue { get => _chartWavFileQueue; set => _chartWavFileQueue = value; }
        private int _chartReopenCount = 0;
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _fity = false;
        public bool IsFitY { get => _fity; set => _fity = value; }
        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public SpectrumChartControl()
        {
            InitializeComponent();

            _clean = false;
            _chart = null;

            CreateChart();

            _signalReader = new SignalReader();
            _signalReader.Started += _signalReader_Started;
            _signalReader.Stopped += _signalReader_Stopped;
            _signalReader.DataGenerated += _signalReader_DataGenerated;
            _signalReader.OutputInterval = 1;
            _signalReader.ThreadType = ThreadType.Thread; // Do not use ThreadType.Timer in WPF.
            _signalReader.ThreadInvoking = true; // Signal reader will automatically synchronize events with the main UI thread.
            _signalReader.IsLooping = false; // checkBoxLooping.IsChecked.Value;

            _chartManager = new ChartManager
            {
                Name = "chartManager",
                MemoryGarbageCollecting = true
            };
            _chart.ChartManager = _chartManager;

            _dispatcherTimerAutoYFit = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 0, 0, 200)
            };
            _dispatcherTimerAutoYFit.Tick += new EventHandler(_dispatcherTimerAutoYFit_Tick);

            _openFileDialog = new OpenFileDialog
            {
                Filter = SignalReader.FileFilterString,
                FilterIndex = 0
            };

            comboBoxFFTDisplay.SelectedIndex = Convert.ToInt32(ConfigurationManager.AppSettings["CHART_FFT_DISPLAY"]);
            comboBoxFFTWindowLength.SelectedIndex = Convert.ToInt32(ConfigurationManager.AppSettings["CHART_FFT_WINDOW_LENGTH"]); ;
            comboBoxHighFreq.SelectedIndex = Convert.ToInt32(ConfigurationManager.AppSettings["CHART_HIGH_FREQ"]);
            cmb_maxpf.SelectedIndex = Convert.ToInt32(ConfigurationManager.AppSettings["CHART_MAX_PF_2D_SPECTRUM"]);

            //_resolution = Convert.ToInt32(comboBoxFFTWindowLength.SelectedItem.ToString());
            _topFrequency = Convert.ToInt32(ConfigurationManager.AppSettings["CHART_TOP_FREQ"]);
            _maxPf = Convert.ToInt32(ConfigurationManager.AppSettings["CHART_MAX_PF_2D_SPECTRUM_VALUE"]);
            HandleFFTDisplayChange();
            //Start();
            Application.Current.MainWindow.Closing += ApplicationClosingDispose;


            _chartReopenCount = Convert.ToInt32(ConfigurationManager.AppSettings["CHART_REOPEN_COUNT"].ToString());
            object lockFlags = new object();

            _chartWavFileQueue.PropertyChanged += (ps, pe) =>
            {

                Dispatcher.Invoke(async () =>
                {
                    while (true)
                    {
                        if (!this.IsRunning && _chartWavFileQueue.Count > 0)
                        {
                            if (checkBoxAutoYFit.IsChecked.Value == true)
                            {
                                FitY();
                            }
                            if (!_fity)
                            {
                                _fity = true;
                            }
                            if (_specchartInit == false)
                            {
                                OpenFile(_chartWavFileQueue.Dequeue());
                            }
                            else
                            {
                                OpenFile();
                            }
                            break;
                        }
                        await Task.Delay(20);
                    }
                }, DispatcherPriority.DataBind);

            };

            this.Loaded += (s, e) =>
            {
                bool chart_autofit = Convert.ToBoolean(ConfigurationManager.AppSettings["CHART_AUTOFIT"].ToString());
                //_dispatcherTimerAutoYFit.IsEnabled = chart_autofit;
                checkBoxAutoYFit.IsChecked = chart_autofit;

                _resolution = Convert.ToInt32((comboBoxFFTWindowLength.SelectedItem as ComboBoxItem).Content);
            };
        }



        private void ApplicationClosingDispose(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!_clean)
            {
                Dispose();
            }
        }

        protected bool IsRunning
        {
            get
            {
                if (_signalReader != null)
                {
                    return _signalReader.IsReaderEnabled;
                }
                else
                {
                    return false;
                }
            }
        }

        //Queue<string> _queuefiles = new Queue<string>();
        public ChartWavFileQueue QueueFiles { get => _chartWavFileQueue; set { _chartWavFileQueue = value; NotifyPropertyChanged("QueueFiles"); } }

        private int _queuefilescount = 0;
        public int QueueFilesCount { get => _chartWavFileQueue.Count; set => _queuefilescount = value; }
        //System.Windows.Forms.Timer _timer;

        /// <summary>
        /// [for test]
        /// </summary>
        public void ReadAllFile()
        {
            var path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\Content";
            var files = Directory.GetFiles(path);
            //_queuefiles = new Queue<string>(files);

        }

        public void Start()
        {
            //if (_signalReader != null)
            //{
            //    _signalReader.Start();
            //}
            //if (_signalReader.IsReaderEnabled == false)
            //{
            //    OpenFile(Environment.CurrentDirectory + "\\Content\\Whistle_48kHz.wav");
            //}

            //ReadAllFile();
            //OpenFile(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + "\\Content\\Whistle_48kHz.wav");

            //uint retrycnt = 10;
            //while (--retrycnt == 0)
            //{
            //    if (_queuefiles.Count > 0)
            //    {
            //        this.OpenFile(_queuefiles.Dequeue());
            //        break;
            //    }
            //    else
            //    {
            //        _specchartInit = false;
            //        await Task.Delay(200);
            //    }
            //}
            _specchartInit = false;
            //this.OpenFile(_queuefiles.Dequeue());


            //if (_timer == null)
            //{
            //    _timer = new System.Windows.Forms.Timer();
            //    _timer.Interval = 100;
            //    _timer.Tick += _timer_Tick;
            //    _timer.Start();
            //    checkBoxAutoYFit.IsChecked = true;
            //    comboBoxFFTDisplay.SelectedIndex = 2;
            //    comboBoxFFTWindowLength.SelectedIndex = 4;

            //}
            //return null;
        }

        bool _specchartInit = false;
        //private void _timer_Tick(object sender, EventArgs e)
        //{
        //    if (!this.IsRunning && _queuefiles.Count > 0)
        //    {
        //        if (_specchartInit == false)
        //        {
        //            OpenFile(_queuefiles.Dequeue());
        //        }
        //        else
        //        {
        //            OpenFile();
        //        }
        //    }
        //}

        public void Stop()
        {
            if (_signalReader != null)
            {
                _signalReader.StopRequest();
                _specchartInit = false;
                //_timer.Stop();
            }
        }

        public void Dispose()
        {
            _clean = true;

            if (IsRunning == true)
            {
                Stop();
                HandleCleanUp();
            }
            else
            {
                HandleCleanUp();
            }
        }

        private void HandleCleanUp()
        {
            if (_signalReader != null)
            {
                _signalReader.Started -= _signalReader_Started;
                _signalReader.Stopped -= _signalReader_Stopped;
                _signalReader.DataGenerated -= _signalReader_DataGenerated;
                _signalReader.StopRequest();
                _signalReader = null;
            }

            if (_dispatcherTimerAutoYFit != null)
            {
                _dispatcherTimerAutoYFit.Stop();
                _dispatcherTimerAutoYFit.Tick -= _dispatcherTimerAutoYFit_Tick;
                _dispatcherTimerAutoYFit = null;
            }

            if (_lineSpectrumMonitors != null)
            {
                foreach (AreaSpectrumMonitor lineSpectrumMonitor in _lineSpectrumMonitors)
                {
                    lineSpectrumMonitor.Dispose();
                }
                _lineSpectrumMonitors = null;
            }

            if (_spectrograms2D != null)
            {
                foreach (Spectrogram2D spectrogram2D in _spectrograms2D)
                {
                    spectrogram2D.Dispose();
                }
                _spectrograms2D = null;
            }

            if (_spectrograms3D != null)
            {
                foreach (Spectrogram3D spectrogram3D in _spectrograms3D)
                {
                    spectrogram3D.Dispose();
                }
                _spectrograms3D = null;
            }

            if (_calculatorFFT != null)
            {
                _calculatorFFT.Dispose();
                _calculatorFFT = null;
            }

            _openFileDialog = null;

            // Don't forget to clear chart grid child list.
            gridChart.Children.Clear();

            if (_chart != null)
            {
                _chart.ChartManager = null;
                _chart.Dispose();
                _chart = null;
            }

            if (_chartManager != null)
            {
                _chartManager.Dispose();
                _chartManager = null;
            }

            // Disposing of unmanaged resources done.

        }

        private void _dispatcherTimerAutoYFit_Tick(object sender, EventArgs e)
        {
            FitY();
        }

        private void CreateChart()
        {

            // Clear any gridChart's children.
            gridChart.Children.Clear();

            if (_chart != null)
            {
                // If a chart is already created, dispose it.
                _chart.Dispose();
                _chart = null;
            }

            // Create a new chart.
            _chart = new LightningChart
            {
                ChartName = "Waveform chart"
            };

            _chart.BeginUpdate();

            _chart.ViewXY.DropOldSeriesData = true;
            _chart.ViewXY.XAxes[0].Maximum = 10;
            _chart.ViewXY.XAxes[0].SweepingGap = 2;
            _chart.ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.Scrolling;

            _chart.ViewXY.XAxes[0].Title.Text = "Range";
            _chart.ViewXY.XAxes[0].Title.VerticalAlign = XAxisTitleAlignmentVertical.Top;
            _chart.ViewXY.XAxes[0].Title.HorizontalAlign = XAxisTitleAlignmentHorizontal.Right;
            _chart.ViewXY.XAxes[0].MajorDivTickStyle.Color = _chart.ViewXY.XAxes[0].AxisColor;
            _chart.ViewXY.XAxes[0].MinorDivTickStyle.Color = _chart.ViewXY.XAxes[0].AxisColor;

            _chart.ViewXY.GraphBackground.Color = Colors.DimGray;
            _chart.ViewXY.GraphBackground.GradientColor = Colors.Black;
            _chart.ViewXY.GraphBackground.GradientDirection = 270;
            _chart.ViewXY.GraphBackground.GradientFill = GradientFill.Linear;
            //_chart.Title.Font = new WpfFont("Segoe UI", 18.0, true, false);
            //_chart.Title.Text = "Open a signal data file to start";
            //_chart.Title.Align = ChartTitleAlignment.TopCenter;
            //_chart.Title.Offset.SetValues(0, 25);

            _chart.ViewXY.YAxes.Clear();
            _chart.ViewXY.PointLineSeries.Clear();
            _chart.ViewXY.AxisLayout.YAxesLayout = YAxesLayout.Stacked;
            _chart.ViewXY.AxisLayout.SegmentsGap = 10;

            //Disable automatic axis layouts 
            _chart.ViewXY.AxisLayout.AutoAdjustMargins = false;
            _chart.ViewXY.AxisLayout.XAxisAutoPlacement = XAxisAutoPlacement.Off;
            _chart.ViewXY.AxisLayout.YAxisAutoPlacement = YAxisAutoPlacement.Off;
            _chart.ViewXY.AxisLayout.XAxisTitleAutoPlacement = false;
            _chart.ViewXY.AxisLayout.YAxisTitleAutoPlacement = false;

            _chart.ViewXY.Margins = new Thickness(0, 0, 0, 3);
            _chart.ViewXY.ZoomPanOptions.ZoomRectLine.Color = Colors.Lime;
            _chart.ViewXY.XAxes[0].LabelsPosition = Alignment.Near;
            _chart.ViewXY.XAxes[0].MajorDivTickStyle.Visible = false;
            _chart.ViewXY.XAxes[0].MinorDivTickStyle.Visible = false;
            _chart.ViewXY.XAxes[0].MajorDivTickStyle.Alignment = Alignment.Near;
            _chart.ViewXY.XAxes[0].MinorDivTickStyle.Alignment = Alignment.Near;
            _chart.ViewXY.XAxes[0].MajorGrid.Visible = m_bGridVisible;
            _chart.ViewXY.XAxes[0].MinorGrid.Visible = m_bGridVisible;
            _chart.ViewXY.XAxes[0].LabelsVisible = false;
            _chart.ViewXY.XAxes[0].LabelsFont = new WpfFont("Segoe UI", 13.0, false, false);

            _chart.ViewXY.XAxes[0].SteppingInterval = 1;

            _chart.ViewXY.LegendBoxes[0].Visible = false;
            _chart.ViewXY.LegendBoxes[0].Position = LegendBoxPositionXY.TopRight;
            _chart.ViewXY.LegendBoxes[0].Offset.SetValues(-10, 40);
            _chart.ViewXY.LegendBoxes[0].Layout = LegendBoxLayout.VerticalColumnSpan;
            _chart.ViewXY.LegendBoxes[0].Fill.Color = Color.FromArgb(120, 255, 255, 255);
            _chart.ViewXY.LegendBoxes[0].Fill.GradientColor = Color.FromArgb(120, 200, 200, 200);
            _chart.ViewXY.LegendBoxes[0].CheckBoxColor = Color.FromArgb(120, 0, 0, 0);
            _chart.ViewXY.LegendBoxes[0].CheckMarkColor = Color.FromArgb(240, 255, 0, 0);
            _chart.ViewXY.LegendBoxes[0].Shadow.Visible = false;

            LineSeriesCursor cursor1 = new LineSeriesCursor(_chart.ViewXY, _chart.ViewXY.XAxes[0])
            {
                ValueAtXAxis = 1
            };
            cursor1.LineStyle.Width = 6;

            Color color = Colors.OrangeRed;
            cursor1.LineStyle.Color = Color.FromArgb(180, color.R, color.G, color.B);

            cursor1.FullHeight = true;
            cursor1.SnapToPoints = true;
            cursor1.Style = CursorStyle.PointTracking;
            cursor1.TrackPoint.Color1 = Colors.Yellow;
            cursor1.TrackPoint.Color2 = Colors.Transparent;
            cursor1.TrackPoint.Shape = Shape.Circle;
            _chart.ViewXY.LineSeriesCursors.Add(cursor1);

            _chart.EndUpdate();

            gridChart.Children.Add(_chart);
        }

        public void FitY()
        {
            if (_chart != null)
            {
                _chart.BeginUpdate();
                foreach (AxisY axisY in _chart.ViewXY.YAxes)
                {
                    bool changed = false;
                    axisY.Fit(20, out changed, true, false);
                }
                _chart.EndUpdate();
            }
            if (_lineSpectrumMonitors != null)
            {
                foreach (AreaSpectrumMonitor chart in _lineSpectrumMonitors)
                {
                    chart.FitView();
                }
            }
            if (_spectrograms3D != null)
            {
                foreach (Spectrogram3D chart in _spectrograms3D)
                {
                    chart.FitView();
                }
            }
            if (_spectrograms2D != null)
            {
                foreach (Spectrogram2D chart in _spectrograms2D)
                {
                    chart.FitView(_maxPf);
                }
            }
        }

        private void _signalReader_Started(StartedEventArgs args)
        {
            //_chart.Title.Text = System.IO.Path.GetFileName(_signalReader.FileName)
            //                + " " + string.Format("sfreq = {0} kHz",
            //                (_samplingFrequency / 1000.0).ToString("0"));
            _chart.Title.Text = null;
            SetXRange(0, 1.0);

            _chart.BeginUpdate();

            HandleScrollModeChange();

            _chart.ViewXY.DropOldEventMarkers = true;
            _chart.ViewXY.DropOldSeriesData = true;

            foreach (SampleDataSeries sds in _chart.ViewXY.SampleDataSeries)
            {
                sds.Clear();
                sds.FirstSampleTimeStamp = 1.0 / _samplingFrequency;
                sds.SamplingFrequency = _samplingFrequency;
                sds.SeriesEventMarkers.Clear();

                sds.AllowUserInteraction = false;
                sds.LineStyle.AntiAliasing = LineAntialias.None;
            }

            _chart.ViewXY.ChartEventMarkers.Clear();

            _chart.ViewXY.ZoomPanOptions.DevicePrimaryButtonAction = UserInteractiveDeviceButtonAction.None;
            _chart.ViewXY.ZoomPanOptions.WheelZooming = WheelZooming.Off;
            _chart.ViewXY.XAxes[0].ScrollPosition = 0;

            HideCursors();
            buttonStop.IsEnabled = true;
            buttonOpen.IsEnabled = false;

            _chart.EndUpdate();

        }

        private void ShowCursors()
        {
            foreach (LineSeriesCursor cursor in _chart.ViewXY.LineSeriesCursors)
            {
                //put cursor to center of x axis 
                cursor.ValueAtXAxis = (_chart.ViewXY.XAxes[0].Minimum + _chart.ViewXY.XAxes[0].Maximum) / 2.0;
                cursor.Visible = true;
            }
        }

        private void HideCursors()
        {
            foreach (LineSeriesCursor cursor in _chart.ViewXY.LineSeriesCursors)
            {
                cursor.Visible = false;
            }
        }

        private void buttonOpen_Click(object sender, RoutedEventArgs e)
        {
            if (_openFileDialog.ShowDialog() != true)
            {
                return;
            }

            OpenFile(_openFileDialog.FileName);
        }

        private async void OpenFile()
        {
            try
            {
                if (_chartWavFileQueue.Count > 0)
                {
                    //this.OpenFile(_queuefiles.Dequeue());
                    var filepath = _chartWavFileQueue.Dequeue();
                    if (_chartWavFileQueue.DeQueueCount > _chartReopenCount)
                    {
                        InitFft();
                        _chartWavFileQueue.Clear();
                        _chartWavFileQueue.DeQueueCount = 0;
                    }
                    else
                    {
                        _signalReader.OpenFile(filepath);
                        _signalReader.Start();
                    }

                    if (FileOpenEvent != null)
                    {
                        FileOpenEvent(this, new SpectrumOpenFIleEventArgs(filepath, _chartWavFileQueue.Count));
                    }
                    //double[][] samples = new double[][];
                    //SignalReader.Marker[] makers = new SignalReader.Marker[];
                    //SignalReader.OpenResult result = _signalReader.ReadAllData(_queuefiles.Dequeue(), ref _channelCount, ref _samplingFrequency, ref samples, ref markers)
                }
            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex);
            }
        }

        private void OpenFile(string filepath)
        {
            //Open the data file. Playback does not start yet. 
            SignalReader.OpenResult result = _signalReader.OpenFile(filepath);
            if (result == SignalReader.OpenResult.FileNotAccessible)
            {
                //MessageBox.Show("File is not accessible");
                MainWindow.Logger.Error($"OpenFile : File is not accessible - {filepath}");
                return;
            }
            else if (result == SignalReader.OpenResult.UnknownExtension)
            {
                //MessageBox.Show("Unknown file extension");
                MainWindow.Logger.Error($"OpenFile : Unknown file extension - {filepath}");
                return;
            }
            else if (result == SignalReader.OpenResult.UnknownWaveFormat)
            {
                //MessageBox.Show("Unknown wave data format");
                MainWindow.Logger.Error($"OpenFile : Unknown wave data format - {filepath}");
                return;
            }

            if (FileOpenEvent != null)
            {
                FileOpenEvent(this, new SpectrumOpenFIleEventArgs(filepath, _chartWavFileQueue.Count));
            }

            _chart.BeginUpdate();
            _chart.ViewXY.SampleDataSeries.Clear();
            _chart.ViewXY.YAxes.Clear();
            _chart.ChartRenderOptions.DeviceType = RendererDeviceType.Auto;

            _channelCount = _signalReader.ChannelCount;
            _samplingFrequency = _signalReader.SamplingFrequency;

            //Set up the Y axes and SampleDataSeries
            for (int line = 0; line < _channelCount; line++)
            {
                AxisY axisY = new AxisY(_chart.ViewXY);
                axisY.SetRange(-30000, 30000);
                axisY.MajorDivTickStyle.Alignment = Alignment.Far;
                axisY.MajorDivTickStyle.Color = Colors.Gray;
                axisY.MinorDivTickStyle.Alignment = Alignment.Far;
                axisY.MinorDivTickStyle.Color = Colors.Gray;
                axisY.MajorGrid.Visible = m_bGridVisible;
                axisY.MinorGrid.Visible = m_bGridVisible;
                axisY.Title.Visible = false;
                axisY.LabelsColor = Colors.White;
                axisY.LabelsFont = new WpfFont("Segoe UI", 13.0, false, false);
                axisY.Alignment = AlignmentHorizontal.Right;
                _chart.ViewXY.YAxes.Add(axisY);

                SampleDataSeries ls = new SampleDataSeries(_chart.ViewXY, _chart.ViewXY.XAxes[0], axisY)
                {
                    SampleFormat = SampleFormat.DoubleFloat,
                    SamplingFrequency = _samplingFrequency
                };
                ls.FirstSampleTimeStamp = 1.0 / ls.SamplingFrequency;
                ls.LineStyle.Color = DefaultColors.SeriesForBlackBackgroundWpf[line % DefaultColors.SeriesForBlackBackgroundWpf.Length];
                ls.LineStyle.AntiAliasing = LineAntialias.None;
                ls.Title.Text = "Channel (Sample data)" + (line + 1).ToString();
                ls.Title.Font = new WpfFont("Segoe UI", 14.0, true, false);
                ls.Highlight = Highlight.None;
                ls.Title.Visible = false;
                ls.LineStyle.Width = 1;
                ls.PointStyle.Shape = Shape.Rectangle;
                ls.PointStyle.Angle = 45;
                ls.PointStyle.Color1 = Color.FromArgb(30, 255, 0, 0);
                ls.PointStyle.GradientFill = GradientFillPoint.Solid;
                ls.PointStyle.BorderWidth = 0;
                ls.PointsVisible = false;
                ls.ScrollModePointsKeepLevel = 50;
                ls.ScrollingStabilizing = true;
                _chart.ViewXY.SampleDataSeries.Add(ls);

                //Add the line as a zero level
                ConstantLine cls = new ConstantLine(_chart.ViewXY, _chart.ViewXY.XAxes[0], axisY);
                cls.Title.Text = "Constant line";
                cls.Title.Visible = false;
                cls.LineStyle.Color = Colors.BlueViolet;
                cls.Behind = true;
                cls.LineStyle.Width = 2;
                cls.AllowUserInteraction = false;
                cls.Value = 0;
                _chart.ViewXY.ConstantLines.Add(cls);
            }

            _chart.EndUpdate();

            InitFft();

            //Start the reader
            _signalReader.Start();
            _specchartInit = true;
            //FitY();
        }

        private void buttonFitY_Click(object sender, RoutedEventArgs e)
        {
            FitY();
        }

        private void buttonStop_Click(object sender, RoutedEventArgs e)
        {
            _signalReader.StopRequest();
        }

        private void _signalReader_DataGenerated(DataGeneratedEventArgs args)
        {
            if (_chart == null)
            {
                return;
            }

            double[][] samples = args.Samples;

            if (samples.Length == 0)
            {
                return;
            }

            _chart.BeginUpdate();

            int pointsLen = args.Samples[0].Length;
            double samplingFreq = _samplingFrequency;
            int channelNumber = 0;
            foreach (SampleDataSeries sds in _chart.ViewXY.SampleDataSeries)
            {
                sds.AddSamples(args.Samples[channelNumber], false);
                channelNumber++;
            }

            //Set latest x 
            _newX = args.FirstSampleTimeStamp + (pointsLen - 1) / samplingFreq;
            _chart.ViewXY.XAxes[0].ScrollPosition = _newX;

            _chart.EndUpdate();

            //Feed multi-channel data to FFT calculator. If it gives a calculated result, set multi-channel result in the selected FFT chart
            if (_calculatorFFT != null)
            {
                double[][][] valuesY;
                double[][][] valuesX;

                if (_calculatorFFT.FeedDataAndCalculate(samples, out valuesX, out valuesY))
                {
                    for (int iR = 0; iR < valuesX.Length; iR++)
                    {
                        for (channelNumber = 0; channelNumber < _channelCount; channelNumber++)
                        {
                            if (_lineSpectrumMonitors != null)
                            {
                                _lineSpectrumMonitors[channelNumber].SetData(valuesX[iR][channelNumber], valuesY[iR][channelNumber]);
                            }
                        }
                    }

                    int rows = valuesX.Length;

                    for (channelNumber = 0; channelNumber < _channelCount; channelNumber++)
                    {
                        if (_spectrograms3D != null)
                        {
                            _spectrograms3D[channelNumber].SetData(valuesY, channelNumber, rows);
                        }
                        if (_spectrograms2D != null)
                        {
                            _spectrograms2D[channelNumber].SetData(valuesY, channelNumber, rows);
                        }
                    }
                }
            }


        }

        private void _signalReader_Stopped()
        {
            try
            {
                _chart.ViewXY.XAxes[0].ScrollMode = XAxisScrollMode.None;

                _chart.BeginUpdate();

                _chart.ViewXY.ZoomPanOptions.DevicePrimaryButtonAction = UserInteractiveDeviceButtonAction.Zoom;

                //Set view to data end
                double min = _chart.ViewXY.XAxes[0].ScrollPosition - (_chart.ViewXY.XAxes[0].Maximum - _chart.ViewXY.XAxes[0].Minimum);
                _chart.ViewXY.XAxes[0].SetRange(Math.Max(0, min), _chart.ViewXY.XAxes[0].ScrollPosition);

                _chart.ViewXY.DropOldSeriesData = false;
                _chart.ViewXY.DropOldEventMarkers = true;
                //ShowCursors();

                _chart.EndUpdate();
                buttonOpen.IsEnabled = true;
                buttonStop.IsEnabled = false;



                if (_clean == true)
                {
                    HandleCleanUp();
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void HandleScrollModeChange()
        {
            if (_chart != null)
            {
                if (comboBoxScrollMode.SelectedIndex >= 0)
                {
                    _chart.ViewXY.XAxes[0].ScrollMode = (XAxisScrollMode)comboBoxScrollMode.SelectedIndex;
                }
            }
        }

        private void comboBoxScrollMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleScrollModeChange();
        }

        private void checkBoxLooping_CheckedChanged(object sender, RoutedEventArgs e)
        {
            if (_signalReader != null)
            {
                _signalReader.IsLooping = checkBoxLooping.IsChecked.Value;
            }
        }

        private void buttonXPlus_Click(object sender, RoutedEventArgs e)
        {
            //double the X range
            double xLen = _chart.ViewXY.XAxes[0].Maximum - _chart.ViewXY.XAxes[0].Minimum;
            SetXRange(_newX - xLen * 2.0, _newX);
        }

        private void buttonXMinus_Click(object sender, RoutedEventArgs e)
        {
            //Half the X range
            double xLen = _chart.ViewXY.XAxes[0].Maximum - _chart.ViewXY.XAxes[0].Minimum;
            SetXRange(_newX - xLen / 2.0, _newX);
        }

        private void SetXRange(double min, double max)
        {
            _chart.BeginUpdate();
            _chart.ViewXY.XAxes[0].SetRange(min, max);
            double range = max - min;
            _chart.ViewXY.XAxes[0].Title.Text = string.Format("{0} s", range.ToString("0.000"));
            _chart.EndUpdate();
        }

        private void buttonYPlus_Click(object sender, RoutedEventArgs e)
        {
            //double the Y range 
            _chart.BeginUpdate();
            foreach (AxisY axisY in _chart.ViewXY.YAxes)
            {
                axisY.SetRange(axisY.Minimum * 2.0, axisY.Maximum * 2.0);
            }
            _chart.EndUpdate();
        }

        private void buttonYMinus_Click(object sender, RoutedEventArgs e)
        {
            //Half the Y range
            _chart.BeginUpdate();
            foreach (AxisY axisY in _chart.ViewXY.YAxes)
            {
                axisY.SetRange(axisY.Minimum / 2.0, axisY.Maximum / 2.0);
            }
            _chart.EndUpdate();
        }

        private void HandleFFTDisplayChange()
        {
            InitFft();
        }

        private void comboBoxFFTDisplay_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            HandleFFTDisplayChange();
        }

        private void DisposeFFTCharts()
        {
            if (_lineSpectrumMonitors != null)
            {
                foreach (AreaSpectrumMonitor chart in _lineSpectrumMonitors)
                {
                    chart.Dispose();
                }
                _lineSpectrumMonitors = null;
            }
            if (_spectrograms3D != null)
            {
                foreach (Spectrogram3D chart in _spectrograms3D)
                {
                    chart.Dispose();
                }
                _spectrograms3D = null;
            }
            if (_spectrograms2D != null)
            {
                foreach (Spectrogram2D chart in _spectrograms2D)
                {
                    chart.Dispose();
                }
                _spectrograms2D = null;
            }
        }

        private void InitFft()
        {
            try
            {
                InitFFTCharts();
                InitFFTCalculators();

                List<LightningChart> charts = new List<LightningChart>(3)
            {
                _chart
            };

                int selectedIndex = comboBoxFFTDisplay.SelectedIndex;
                if (selectedIndex == 0) // Line spectrum.
                {
                    foreach (AreaSpectrumMonitor asm in _lineSpectrumMonitors)
                    {
                        charts.Add(asm.Chart);
                    }
                }
                else if (selectedIndex == 1) // 3D spectrum.
                {
                    foreach (Spectrogram3D s3d in _spectrograms3D)
                    {
                        charts.Add(s3d.Chart);
                    }
                }
                else if (selectedIndex == 2) // Spectrogram.
                {
                    foreach (Spectrogram2D s2d in _spectrograms2D)
                    {
                        charts.Add(s2d.Chart);
                    }
                }

            }
            catch (Exception ex)
            {
                MainWindow.Logger.Error(ex);
            }
        }

        private void InitFFTCalculators()
        {
            if (_calculateFFT == false)
            {
                if (_calculatorFFT != null)
                {
                    _calculatorFFT.Dispose();
                }
                _calculatorFFT = null;
            }
            else
            {
                _calculatorFFT = new RealtimeFFTCalculator(_intervalFFT, _samplingFrequency, _resolution, _channelCount);
            }
        }

        private void InitFFTCharts()
        {
            DisposeFFTCharts();

            _calculateFFT = false;
            int selectedIndex = comboBoxFFTDisplay.SelectedIndex;
            int resolution = _resolution;

            if (_topFrequency < _samplingFrequency / 2)
            {
                resolution = (int)Math.Round(_topFrequency / (double)(_samplingFrequency / 2) * _resolution);
            }

            if (selectedIndex == 0) //Line spectrum
            {
                ShowRightHandChart();

                _calculateFFT = true;
                _lineSpectrumMonitors = new AreaSpectrumMonitor[_channelCount];

                for (int channelNumber = 0; channelNumber < _channelCount; channelNumber++)
                {
                    string title;
                    if (_channelCount == 2)
                    {
                        if (channelNumber == 0)
                        {
                            title = "Power spectrum P(f) - L";
                        }
                        else
                        {
                            title = "Power spectrum P(f) - R";
                        }
                    }
                    else
                    {
                        title = "Power spectrum P(f)\nChannel " + (channelNumber + 1).ToString();
                    }

                    AreaSpectrumMonitor asm =
                        new AreaSpectrumMonitor(gridRightChartGrid,
                            resolution,
                            _topFrequency,
                            title,
                            _chart.ViewXY.SampleDataSeries[channelNumber].LineStyle.Color
                        );

                    asm.Chart.ChartName = "Area Spectrum Chart " + (channelNumber + 1).ToString();

                    _lineSpectrumMonitors[channelNumber] = asm;
                }
            }
            else if (selectedIndex == 1) //3D spectrum
            {
                ShowRightHandChart();

                _calculateFFT = true;
                _spectrograms3D = new Spectrogram3D[_channelCount];

                for (int channelNumber = 0; channelNumber < _channelCount; channelNumber++)
                {
                    Spectrogram3D s3d =
                        new Spectrogram3D(gridRightChartGrid,
                            resolution,
                            _intervalFFT,
                            3,
                            0,
                            _topFrequency,
                            "Power spectrum P(f)\nChannel " + (channelNumber + 1).ToString(),
                            _chart.ViewXY.SampleDataSeries[channelNumber].LineStyle.Color
                        );

                    s3d.Chart.ChartName = "Spectrum 3D chart " + (channelNumber + 1).ToString();

                    _spectrograms3D[channelNumber] = s3d;
                }
            }
            else if (selectedIndex == 2) //Spectrogram
            {
                ShowRightHandChart();

                _calculateFFT = true;
                _spectrograms2D = new Spectrogram2D[_channelCount];
                double dFFTwinOffset = (double)(_resolution / 2) / _samplingFrequency;

                for (int channelNumber = 0; channelNumber < _channelCount; channelNumber++)
                {
                    Spectrogram2D s2d =
                        new Spectrogram2D(gridRightChartGrid,
                            false,
                            resolution,
                            _intervalFFT,
                            5,
                            0,
                            _topFrequency,
                            dFFTwinOffset,
                            "Power spectrum P(f)\nChannel " + (channelNumber + 1).ToString(),
                            _chart.ViewXY.SampleDataSeries[channelNumber].LineStyle.Color
                        );

                    s2d.Chart.ChartName = "Spectrogram " + (channelNumber + 1).ToString();

                    _spectrograms2D[channelNumber] = s2d;
                }
            }
            else //Off
            {
                HideRightHandChart();
            }

            ArrangeFFTCharts();
        }

        private double _lastWidth = 0.0;

        private void gridRightChartGrid_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            _lastWidth = e.PreviousSize.Width;

            ArrangeFFTCharts();
        }

        private void ArrangeFFTCharts()
        {
            if (_channelCount == 0)
            {
                return;
            }

            int totalWidth = (int)gridRightChartGrid.ActualWidth;
            int totalHeight = (int)gridRightChartGrid.ActualHeight;
            int height = totalHeight / _channelCount;
            int x = 0;
            int top = 0;

            if (_lineSpectrumMonitors != null)
            {
                for (int channelNumber = 0; channelNumber < _channelCount; channelNumber++)
                {
                    _lineSpectrumMonitors[channelNumber].SetBounds(x, top, totalWidth, height);
                    top += height;
                }
            }
            else if (_spectrograms3D != null)
            {
                for (int channelNumber = 0; channelNumber < _channelCount; channelNumber++)
                {
                    _spectrograms3D[channelNumber].SetBounds(x, top, totalWidth, height);
                    top += height;
                }
            }
            else if (_spectrograms2D != null)
            {
                for (int channelNumber = 0; channelNumber < _channelCount; channelNumber++)
                {
                    _spectrograms2D[channelNumber].SetBounds(x, top, totalWidth, height);
                    top += height;
                }
            }
        }

        private void comboBoxFFTWindowLength_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string length = (string)(comboBoxFFTWindowLength.SelectedItem as ComboBoxItem).Content;
            _resolution = int.Parse(length);

            InitFft();
            FitY();
        }

        private void comboBoxHighFreq_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _topFrequency = _samplingFrequency / 2;

            if (comboBoxHighFreq.SelectedIndex == 0)
            {
                _topFrequency = 2000;
            }
            else if (comboBoxHighFreq.SelectedIndex == 1)
            {
                _topFrequency = 5000;
            }
            else if (comboBoxHighFreq.SelectedIndex == 2)
            {
                _topFrequency = 10000;
            }
            else if (comboBoxHighFreq.SelectedIndex == 3)
            {
                _topFrequency = 50000;
            }
            else if (comboBoxHighFreq.SelectedIndex == 4)
            {
                _topFrequency = 80000;
            }
            else if (comboBoxHighFreq.SelectedIndex == 5)
            {
                _topFrequency = 192000;
            }
            else if (comboBoxHighFreq.SelectedIndex == 6)
            {
                _topFrequency = 384000;
            }

            InitFft();
            FitY();

        }

        private void checkBoxAutoYFit_CheckedChanged(object sender, RoutedEventArgs e)
        {
            //_dispatcherTimerAutoYFit.IsEnabled = checkBoxAutoYFit.IsChecked.Value;
        }

        private void HideRightHandChart()
        {
            if (rightChartColumn.Width.Value > 0)
            {
                rightChartColumn.Width = new GridLength(0);
                gridSplitter1.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void ShowRightHandChart()
        {
            if (rightChartColumn.Width.Value < 1)
            {
                rightChartColumn.Width = new GridLength(_lastWidth);
                gridSplitter1.Visibility = System.Windows.Visibility.Visible;
            }
        }

        private void btn_start_Click(object sender, RoutedEventArgs e)
        {
            Start();
        }

        private void btn_stop_Click(object sender, RoutedEventArgs e)
        {
            Stop();
        }

        private void cmb_maxpf_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            var maxpf = Convert.ToInt32((cmb_maxpf.SelectedItem as ComboBoxItem).Content);
            _maxPf = maxpf;

            FitY();
        }

        private void UserControl_PreviewMouseWheel(object sender, System.Windows.Input.MouseWheelEventArgs e)
        {
            e.Handled = true;
        }

        private void UserControl_PreviewDragEnter(object sender, DragEventArgs e)
        {
            e.Handled = true;
        }

        private void gridRightChartGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }

    public class AreaSpectrumMonitor
    {
        [System.Runtime.InteropServices.DllImport("user32")]
        private static extern bool SetWindowPos(
            IntPtr hWnd,
            IntPtr hWndInsertAfter,
            int x,
            int y,
            int cx,
            int cy,
            uint uFlags
        );

        private LightningChart _chart;
        private int m_iResolution;

        public AreaSpectrumMonitor(
            Panel parentControl,
            int resolution,
            double xAxisMax,
            string title,
            Color lineColor
        )
        {
            m_iResolution = resolution;

            _chart = new LightningChart
            {
                ChartName = "Area spectrum chart",
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            _chart.BeginUpdate();

            //Setup custom style


            _chart.Title.Text = title;
            _chart.Title.Color = lineColor;
            _chart.Title.Font = new WpfFont("Segoe UI", 14.0, true, false);
            _chart.Title.Offset.SetValues(0, 20);

            _chart.ViewXY.GraphBackground.GradientDirection = 270;
            _chart.ViewXY.GraphBackground.GradientFill = GradientFill.Linear;
            _chart.ViewXY.LegendBoxes[0].Visible = false;
            _chart.ViewXY.ZoomPanOptions.ZoomRectLine.Color = Colors.White;
            _chart.ViewXY.Border.RenderBehindSeries = true;
            _chart.ViewXY.AxisLayout.YAxesLayout = YAxesLayout.Layered;
            _chart.ViewXY.AxisLayout.AutoAdjustMargins = false;
            _chart.ViewXY.Margins = new Thickness(70, 6, 15, 50);

            Color color = _chart.ViewXY.GraphBackground.Color;
            _chart.ViewXY.GraphBackground.Color = Color.FromArgb(150, color.R, color.G, color.B);

            AxisX axisX = _chart.ViewXY.XAxes[0];
            axisX.SetRange(0, xAxisMax);
            axisX.Title.Font = new WpfFont("Segoe UI", 13.0, true, false);
            axisX.Title.Visible = true;
            axisX.Title.Text = "Frequency (Hz)";
            axisX.Units.Visible = false;
            axisX.ValueType = AxisValueType.Number;
            axisX.Position = 100;
            axisX.LabelsPosition = Alignment.Far;
            axisX.MajorDivTickStyle.Alignment = Alignment.Far;
            axisX.MinorDivTickStyle.Alignment = Alignment.Far;
            axisX.MajorDivTickStyle.Color = Colors.Gray;
            axisX.MinorDivTickStyle.Color = Colors.DimGray;
            axisX.LabelsColor = Colors.White;
            axisX.LabelsFont = new WpfFont("Segoe UI", 13, false, false);
            axisX.ScrollMode = XAxisScrollMode.None;

            AxisY axisY = _chart.ViewXY.YAxes[0];
            axisY.MajorDivTickStyle.Color = Colors.Gray;
            axisY.MinorDivTickStyle.Color = Colors.DimGray;
            axisY.AutoFormatLabels = false;
            axisY.LabelsNumberFormat = "0";
            axisY.SetRange(0, SpectrumChartControl.TopFrequency);
            axisY.Title.Visible = false;
            axisY.LabelsColor = Colors.White;
            axisY.LabelsFont = new WpfFont("Segoe UI", 13, false, false);
            axisY.Units.Visible = false;

            AreaSeries areaSeries = new AreaSeries(_chart.ViewXY, axisX, axisY);
            areaSeries.Title.Visible = false;
            areaSeries.LineStyle.Color = lineColor;
            areaSeries.LineStyle.Width = 1f;
            areaSeries.Fill.Color = ChartTools.CalcGradient(lineColor, Colors.Black, 50);
            areaSeries.Fill.GradientFill = GradientFill.Solid;
            areaSeries.AllowUserInteraction = false;
            areaSeries.PointsVisible = false;

            _chart.ViewXY.AreaSeries.Add(areaSeries);
            _chart.EndUpdate();

            parentControl.Children.Add(_chart);
        }

        public LightningChart Chart
        {
            get
            {
                return _chart;
            }
        }

        public void FitView()
        {
            try
            {
                _chart.ViewXY.ZoomToFit();
            }
            catch (Exception ex)
            {
            }
        }

        public void Dispose()
        {
            if (_chart != null)
            {
                _chart.Dispose();
                _chart = null;
            }
        }

        public void SetData(double[] xValues, double[] yValues)
        {
            // Only accept resolution count of data points.

            if (xValues.Length > m_iResolution)
            {
                Array.Resize(ref xValues, m_iResolution);
            }

            if (yValues.Length > m_iResolution)
            {
                Array.Resize(ref yValues, m_iResolution);
            }

            _chart.BeginUpdate();

            // Set data to area series.

            int pointsCount = xValues.Length;
            AreaSeriesPoint[] aPoints = new AreaSeriesPoint[pointsCount];
            for (int i = 0; i < pointsCount; i++)
            {
                aPoints[i].X = xValues[i];
                aPoints[i].Y = yValues[i];
            }

            _chart.ViewXY.AreaSeries[0].Points = aPoints;

            _chart.EndUpdate();
        }

        public void SetBounds(double x, double y, double width, double height)
        {
            _chart.Margin = new Thickness(x, y, 0.0, 0.0);
            _chart.Width = width;
            _chart.Height = height;
        }
    }

    public class PersistentSpectrumMonitor
    {
        private LightningChart _chart;
        private int m_iResolution;

        public PersistentSpectrumMonitor(Panel parent, int resolution, double xAxisMax,
            string title, Color lineColor)
        {
            m_iResolution = resolution;

            _chart = new LightningChart();

            _chart.BeginUpdate();

            // Don't forget to set HorizontalAlignment and VerticalAlignment 
            // since we are aligning charts using Margins property in 
            // SetBounds method.
            _chart.HorizontalAlignment = HorizontalAlignment.Left;
            _chart.VerticalAlignment = VerticalAlignment.Top;

            _chart.ChartName = "Area spectrum chart";
            _chart.Title.Visible = true;
            _chart.Title.Text = title;
            _chart.Title.Color = lineColor;
            _chart.Title.Font = new WpfFont("Segoe UI", 14.0, true, false);
            _chart.Title.Offset.SetValues(0, 20);
            _chart.ChartBackground.Color = ChartTools.CalcGradient(lineColor, Colors.Black, 65);
            _chart.ChartBackground.GradientDirection = 0;
            _chart.ChartBackground.GradientFill = GradientFill.Cylindrical;
            _chart.ViewXY.GraphBackground.GradientDirection = 270;
            _chart.ViewXY.GraphBackground.GradientFill = GradientFill.Linear;
            _chart.ViewXY.ZoomPanOptions.ZoomRectLine.Color = Colors.White;
            _chart.ViewXY.Border.RenderBehindSeries = true;
            _chart.ViewXY.AxisLayout.YAxesLayout = YAxesLayout.Layered;
            //Disable automatic axis layouts 
            _chart.ViewXY.AxisLayout.AutoAdjustMargins = false;
            _chart.ViewXY.AxisLayout.XAxisAutoPlacement = XAxisAutoPlacement.Off;
            _chart.ViewXY.AxisLayout.YAxisAutoPlacement = YAxisAutoPlacement.Off;
            _chart.ViewXY.AxisLayout.XAxisTitleAutoPlacement = false;
            _chart.ViewXY.AxisLayout.YAxisTitleAutoPlacement = false;
            _chart.ViewXY.Margins = new Thickness(70, 6, 15, 50);
            _chart.ViewXY.LegendBoxes[0].Visible = false;

            Color color = _chart.ViewXY.GraphBackground.Color;
            _chart.ViewXY.GraphBackground.Color = Color.FromArgb(150, color.R, color.G, color.B);

            AxisX axisX = _chart.ViewXY.XAxes[0];
            axisX.SetRange(0, xAxisMax);
            axisX.Title.Font = new WpfFont("Segoe UI", 13.0, true, false);
            axisX.Title.Visible = true;
            axisX.Title.Text = "Frequency (Hz)";
            axisX.Units.Visible = false;
            axisX.ValueType = AxisValueType.Number;
            axisX.Position = 100;
            axisX.LabelsPosition = Alignment.Far;
            axisX.MajorDivTickStyle.Alignment = Alignment.Far;
            axisX.MinorDivTickStyle.Alignment = Alignment.Far;
            axisX.MajorDivTickStyle.Color = Colors.Gray;
            axisX.MinorDivTickStyle.Color = Colors.DimGray;
            axisX.LabelsColor = Colors.White;
            axisX.LabelsFont = new WpfFont("Segoe UI", 13, false, false);
            axisX.ScrollMode = XAxisScrollMode.None;

            AxisY axisY = _chart.ViewXY.YAxes[0];
            axisY.MajorDivTickStyle.Color = Colors.Gray;
            axisY.MinorDivTickStyle.Color = Colors.DimGray;
            axisY.LabelsNumberFormat = "0";
            axisY.AutoFormatLabels = false;
            axisY.SetRange(0, 7000000);
            axisY.Title.Visible = false;
            axisY.LabelsColor = Colors.White;
            axisY.LabelsFont = new WpfFont("Segoe UI", 13, false, false);
            axisY.Units.Visible = false;

            // Setup custom style.


            // Remove existing series.
            _chart.ViewXY.HighLowSeries.Clear();


            // Add high-low or area for historical (persistent) data.
            HighLowSeries highLowSeries = new HighLowSeries(_chart.ViewXY, axisX, axisY);
            highLowSeries.Title.Visible = false;
            highLowSeries.LineVisibleHigh = false;
            highLowSeries.LineVisibleLow = false;
            highLowSeries.Fill.Color = ChartTools.CalcGradient(lineColor, Colors.Black, 50);
            highLowSeries.Fill.GradientFill = GradientFill.Solid;
            highLowSeries.AllowUserInteraction = false;
            highLowSeries.PointsVisible = false;

            _chart.ViewXY.HighLowSeries.Add(highLowSeries);

            _chart.ViewXY.PointLineSeries.Clear();


            // Add point-line series for latest FFT data.
            PointLineSeries pls = new PointLineSeries(_chart.ViewXY, axisX, axisY);
            pls.LineStyle.Color = lineColor;
            pls.LineStyle.Width = 2.0f;

            _chart.ViewXY.PointLineSeries.Add(pls);

            _chart.EndUpdate();

            parent.Children.Add(_chart);
        }

        public LightningChart Chart
        {
            get
            {
                return _chart;
            }
        }

        public void Reset()
        {
            _chart.ViewXY.HighLowSeries[0].Clear();
        }

        public void FitView()
        {
            _chart.ViewXY.ZoomToFit();
        }

        public void Dispose()
        {
            if (_chart != null)
            {
                _chart.Dispose();
                _chart = null;
            }
        }

        public void SetData(double[] xValues, double[] yValues)
        {
            //Only accept resolution count of data points 
            if (m_iResolution + 1 < xValues.Length)
            {
                Array.Resize(ref xValues, m_iResolution + 1);
            }

            if (m_iResolution + 1 < yValues.Length)
            {
                Array.Resize(ref yValues, m_iResolution + 1);
            }

            _chart.BeginUpdate();
            int pointsCount = xValues.Length;

            //Set data to high-low series 
            HighLowSeries highLowSeries = _chart.ViewXY.HighLowSeries[0];

            HighLowSeriesPoint[] aPointsOrig = highLowSeries.Points;

            HighLowSeriesPoint[] aPoints = new HighLowSeriesPoint[pointsCount];
            if (aPointsOrig != null && pointsCount == aPointsOrig.Length)
            {
                for (int i = 0; i < pointsCount; i++)
                {
                    aPoints[i].X = aPointsOrig[i].X;
                    if (yValues[i] < aPointsOrig[i].YLow)
                    {
                        aPoints[i].YLow = yValues[i];
                    }
                    else
                    {
                        aPoints[i].YLow = aPointsOrig[i].YLow;
                    }

                    if (yValues[i] > aPointsOrig[i].YHigh)
                    {
                        aPoints[i].YHigh = yValues[i];
                    }
                    else
                    {
                        aPoints[i].YHigh = aPointsOrig[i].YHigh;
                    }
                }
            }
            else
            {
                for (int i = 0; i < pointsCount; i++)
                {
                    aPoints[i].X = xValues[i];
                    aPoints[i].YLow = yValues[i];
                    aPoints[i].YHigh = yValues[i];
                }
            }
            highLowSeries.Points = aPoints;

            //Set data to PointLineSeries displaying the latest FFT data

            PointLineSeries pls = _chart.ViewXY.PointLineSeries[0];

            SeriesPoint[] seriesPoints = new SeriesPoint[pointsCount];
            for (int i = 0; i < pointsCount; i++)
            {
                seriesPoints[i].X = xValues[i];
                seriesPoints[i].Y = yValues[i];
            }
            pls.Points = seriesPoints;

            _chart.EndUpdate();
        }

        public void SetBounds(int x, int y, int width, int height)
        {
            _chart.Margin = new Thickness(x, y, 0.0, 0.0);
            _chart.Width = width;
            _chart.Height = height;
        }
    }
    public class Spectrogram2D
    {
        private LightningChart _chart;
        private int m_iSizeTimeResolution = 1;
        private int m_iFreqResolution = 1;
        private double m_dTimeStep = 0;
        private double m_dTimeRangeLengthSec = 1;
        private IntensityGridSeries _grid = null;
        private double[][] m_aValuesData;

        public Spectrogram2D(
            Panel parentControl,
            bool verticalScrolling,
            int resolution,
            double timeStepMs,
            double timeRangeLengthSec,
            double freqMin,
            double freqMax,
            double dFFTtimeWinOffset,
            string title,
            Color toneColor
        )
        {
            double dDefaultYMax = freqMax;

            _chart = new LightningChart
            {
                ChartName = "Spectrogram",
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left
            };

            parentControl.Children.Add(_chart);

            m_iFreqResolution = resolution;
            m_dTimeRangeLengthSec = timeRangeLengthSec;

            _chart.BeginUpdate();

            //Setup custom style


            _chart.ViewXY.AxisLayout.AutoAdjustMargins = false;
            _chart.ViewXY.Margins = new Thickness(70, 40, 15, 10);
            _chart.Title.Visible = false;
            _chart.Title.Font = new WpfFont("Segoe UI", 14, true, false);
            _chart.Title.Offset.SetValues(0, 0);

            _chart.ViewXY.AxisLayout.XAxisAutoPlacement = XAxisAutoPlacement.TopThenBottom;

            _chart.ViewXY.XAxes[0].ValueType = AxisValueType.Number;
            _chart.ViewXY.XAxes[0].LabelsFont = new WpfFont("Segoe UI", 13, false, false);
            _chart.ViewXY.XAxes[0].Title.Visible = false;

            _chart.ViewXY.YAxes[0].ValueType = AxisValueType.Time;
            _chart.ViewXY.YAxes[0].LabelsFont = new WpfFont("Segoe UI", 13, false, false);
            _chart.ViewXY.YAxes[0].Title.Text = "Time";
            _chart.ViewXY.YAxes[0].Title.Visible = false;

            //Setup legend box
            _chart.ViewXY.LegendBoxes[0].SeriesTitleColor = toneColor;
            _chart.ViewXY.LegendBoxes[0].ValueLabelColor = Colors.White;
            _chart.ViewXY.LegendBoxes[0].IntensityScales.ScaleBorderColor = Colors.White;
            _chart.ViewXY.LegendBoxes[0].Position = LegendBoxPositionXY.RightCenter;
            _chart.ViewXY.LegendBoxes[0].Layout = LegendBoxLayout.Vertical;
            _chart.ViewXY.LegendBoxes[0].Offset.SetValues(-20, 0);
            _chart.ViewXY.LegendBoxes[0].Fill.Style = RectFillStyle.None;
            _chart.ViewXY.LegendBoxes[0].Shadow.Visible = false;
            _chart.ViewXY.LegendBoxes[0].BorderWidth = 0;
            _chart.ViewXY.LegendBoxes[0].IntensityScales.ScaleSizeDim1 = 100;
            _chart.ViewXY.LegendBoxes[0].IntensityScales.ScaleSizeDim2 = 15;

            _chart.Name = "Spectrogram";

            m_dTimeStep = timeStepMs / 1000.0;
            double dTimeAxisMax = dFFTtimeWinOffset;
            double dTimeAxisMin = m_dTimeStep - m_dTimeRangeLengthSec + dTimeAxisMax;
            _chart.ViewXY.XAxes[0].SetRange(freqMin, freqMax);
            _chart.ViewXY.YAxes[0].SetRange(dTimeAxisMin, dTimeAxisMax);

            m_iSizeTimeResolution = (int)Math.Round(timeRangeLengthSec / (timeStepMs / 1000.0));

            //Create external values data array that represents the grid contents 
            m_aValuesData = new double[m_iSizeTimeResolution][];
            for (int row = 0; row < m_iSizeTimeResolution; row++)
            {
                m_aValuesData[row] = new double[resolution];
            }

            _grid = new IntensityGridSeries(_chart.ViewXY, _chart.ViewXY.XAxes[0], _chart.ViewXY.YAxes[0])
            {
                ContourLineType = ContourLineTypeXY.None,
                WireframeType = SurfaceWireframeType.None,
                PixelRendering = true,
                AllowUserInteraction = false
            };
            _grid.Title.Text = "P(f)";

            //Assign the array for the grid, it sets also grid.SizeX and grid.SizeY at the same time.  
            _grid.SetValuesData(m_aValuesData, IntensityGridValuesDataOrder.RowsColumns);

            //Data array won't be needed in this example, so null it to prevent extra memory consumption 
            _grid.Data = null;

            //Set the X and Y ranges that the grid covers 
            _grid.SetRangesXY(_chart.ViewXY.XAxes[0].Minimum, _chart.ViewXY.XAxes[0].Maximum, _chart.ViewXY.YAxes[0].Minimum, _chart.ViewXY.YAxes[0].Maximum);

            //Create palette
            _grid.ValueRangePalette = CreatePalette(_grid, dDefaultYMax, toneColor);

            _chart.ViewXY.IntensityGridSeries.Add(_grid);

            _chart.EndUpdate();
        }

        public LightningChart Chart
        {
            get
            {
                return _chart;
            }
        }


        private ValueRangePalette CreatePalette(IntensityGridSeries ownerSeries, double yRange)
        {
            ValueRangePalette palette = new ValueRangePalette(ownerSeries);

            palette.Steps.Clear();

            palette.Steps.Add(new PaletteStep(palette, Colors.Black, 0));
            palette.Steps.Add(new PaletteStep(palette, Colors.Lime, 30 * yRange / 100.0));
            palette.Steps.Add(new PaletteStep(palette, Colors.Yellow, 60.0 * yRange / 100.0));
            palette.Steps.Add(new PaletteStep(palette, Colors.Red, 100.0 * yRange / 100.0));
            palette.Type = PaletteType.Gradient;

            return palette;
        }

        private ValueRangePalette CreatePalette(IntensityGridSeries ownerSeries, double yRange, Color baseColor)
        {
            ValueRangePalette palette = new ValueRangePalette(ownerSeries);

            palette.Steps.Clear();


            palette.Steps.Add(new PaletteStep(palette, Colors.Black, 0));
            palette.Steps.Add(new PaletteStep(palette, baseColor, 40.0 * yRange / 100.0));
            palette.Steps.Add(new PaletteStep(palette, Colors.White, 100.0 * yRange / 100.0));
            palette.Type = PaletteType.Gradient;

            return palette;
        }


        public void Dispose()
        {
            if (_grid != null)
            {
                _grid.Dispose();
                _grid = null;
            }

            if (_chart != null)
            {
                _chart.Dispose();
                _chart = null;
            }
        }

        public void FitView(double topFrequency = double.MaxValue)
        {
            int columns = m_iFreqResolution;
            int rows = m_iSizeTimeResolution;


            double minY = double.MaxValue;
            double maxY = double.MinValue;
            double y;

            _chart.BeginUpdate();

            if (_grid != null)
            {
                for (int iCol = 0; iCol < columns; iCol++)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        y = m_aValuesData[row][iCol];
                        if (y > maxY)
                        {
                            maxY = y;
                        }

                        if (y < minY)
                        {
                            minY = y;
                        }
                    }
                }
                _grid.ValueRangePalette = CreatePalette(_grid, topFrequency);

            }

            _chart.EndUpdate();

        }

        public void SetData(double[][][] yValues, int channelIndex, int rowCount)
        {
            if (_chart == null)
            {
                return;
            }

            _chart.BeginUpdate();

            for (int row = 0; row < rowCount; row++)
            {
                double[] aYValues = yValues[row][channelIndex];

                //Only accept resolution count of data points 
                Array.Resize(ref yValues, m_iFreqResolution);

                //move the old columns one step earlier in history
                for (int iTimeSlot = 1; iTimeSlot < m_iSizeTimeResolution; iTimeSlot++)
                {
                    m_aValuesData[iTimeSlot - 1] = m_aValuesData[iTimeSlot]; //change the reference 
                }

                m_aValuesData[m_iSizeTimeResolution - 1] = aYValues;
            }

            _grid.InvalidateValuesDataOnly();

            double dCurrentYMin = _chart.ViewXY.YAxes[0].Minimum;
            double dTotalTimeShift = m_dTimeStep * rowCount;

            _chart.ViewXY.YAxes[0].SetRange(dCurrentYMin + dTotalTimeShift, dCurrentYMin + dTotalTimeShift + m_dTimeRangeLengthSec);

            _grid.SetRangesXY(_chart.ViewXY.XAxes[0].Minimum, _chart.ViewXY.XAxes[0].Maximum,
                _chart.ViewXY.YAxes[0].Minimum, _chart.ViewXY.YAxes[0].Maximum);

            _chart.EndUpdate();

        }

        public void SetBounds(int x, int y, int width, int height)
        {
            _chart.Margin = new Thickness(x, y, 0, 0);
            _chart.Width = width;
            _chart.Height = height;
        }
    }

    public class Spectrogram3D
    {
        private LightningChart _chart = null;
        private double m_dCurrentZ = 0.0;
        private double m_dStepZ = 0.0;
        private SurfaceGridSeries3D m_surface = null;
        private WaterfallSeries3D m_waterfallFront = null;

        public Spectrogram3D(
            Panel parentControl,
            int resolution,
            double timeStepMs,
            double timeRangeLengthSec,
            double freqMin,
            double freqMax,
            string title,
            Color toneColor
        )
        {
            m_dStepZ = timeStepMs / 1000.0;

            bool bLogFreqAxis = false;
            double dDefaultYMax = SpectrumChartControl.TopFrequency;

            //Create chart 
            _chart = new LightningChart
            {
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top
            };

            parentControl.Children.Add(_chart);

            _chart.BeginUpdate();

            _chart.ActiveView = ActiveView.View3D;

            _chart.ChartBackground.Color = ChartTools.CalcGradient(toneColor, Colors.Black, 65);
            _chart.ChartBackground.GradientDirection = 0;
            _chart.ChartBackground.GradientFill = GradientFill.Radial;

            _chart.Title.Text = title;
            _chart.Title.Font = new WpfFont("Segoe UI", 14, true, false);
            _chart.Title.Offset.SetValues(0, 20);

            _chart.View3D.WallOnBack.Visible = false;
            _chart.View3D.WallOnLeft.Visible = false;
            _chart.View3D.WallOnRight.Visible = false;
            _chart.View3D.WallOnTop.Visible = false;
            _chart.View3D.WallOnFront.Visible = false;
            _chart.View3D.WallOnBottom.Visible = false;

            _chart.View3D.XAxisPrimary3D.Orientation = PlaneXAxis3D.XY;
            if (bLogFreqAxis)
            {
                _chart.View3D.XAxisPrimary3D.ScaleType = ScaleType.Logarithmic;
                if (freqMin < 1)
                {
                    freqMin = 1;
                }
            }
            _chart.View3D.XAxisPrimary3D.CornerAlignment = AxisAlignment3D.Outside;
            _chart.View3D.XAxisPrimary3D.MajorDivTickStyle.Alignment = Alignment.Far;
            _chart.View3D.XAxisPrimary3D.LabelsColor = Color.FromArgb(200, 255, 255, 255);
            _chart.View3D.XAxisPrimary3D.LabelsFont = new WpfFont("Segoe UI", 13, false, false);
            _chart.View3D.XAxisPrimary3D.MajorDivTickStyle.Color = Colors.Orange;
            _chart.View3D.XAxisPrimary3D.Title.Text = "Frequency (Hz)";
            _chart.View3D.XAxisPrimary3D.Title.Font = new WpfFont("Segoe UI", 13, true, false);

            _chart.View3D.YAxisPrimary3D.Orientation = PlaneYAxis3D.XY;
            _chart.View3D.YAxisPrimary3D.CornerAlignment = AxisAlignment3D.Outside;
            _chart.View3D.YAxisPrimary3D.MajorDivTickStyle.Alignment = Alignment.Far;
            _chart.View3D.YAxisPrimary3D.LabelsColor = Color.FromArgb(200, 255, 255, 255);
            _chart.View3D.YAxisPrimary3D.LabelsFont = new WpfFont("Segoe UI", 13, false, false);
            _chart.View3D.YAxisPrimary3D.MajorDivTickStyle.Color = Colors.Orange;
            _chart.View3D.YAxisPrimary3D.Title.Visible = false;
            _chart.View3D.YAxisPrimary3D.SetRange(0, dDefaultYMax);

            _chart.View3D.ZAxisPrimary3D.Reversed = true;
            _chart.View3D.ZAxisPrimary3D.LabelsColor = Color.FromArgb(200, 255, 255, 255);
            _chart.View3D.ZAxisPrimary3D.LabelsFont = new WpfFont("Segoe UI", 13, false, false);
            _chart.View3D.ZAxisPrimary3D.Title.Text = "Time";
            _chart.View3D.ZAxisPrimary3D.ValueType = AxisValueType.Time;
            _chart.View3D.ZAxisPrimary3D.MajorDivTickStyle.Color = Colors.Orange;
            _chart.View3D.ZAxisPrimary3D.Title.Font = new WpfFont("Segoe UI", 13, true, false);
            _chart.View3D.WallOnBottom.GridStrips = WallGridStripXZ.X;

            _chart.View3D.LegendBox.SeriesTitleColor = Colors.White;
            _chart.View3D.LegendBox.ValueLabelColor = Colors.White;
            _chart.View3D.LegendBox.SurfaceScales.ScaleBorderColor = Colors.White;
            _chart.View3D.LegendBox.Position = LegendBoxPosition.TopRight;
            _chart.View3D.LegendBox.Offset.SetValues(0, 0);
            _chart.View3D.LegendBox.Fill.Style = RectFillStyle.None;
            _chart.View3D.LegendBox.Shadow.Visible = false;
            _chart.View3D.LegendBox.BorderWidth = 0;

            _chart.View3D.Camera.RotationX = 20;
            _chart.View3D.Camera.RotationY = -30;
            _chart.View3D.Camera.RotationZ = 0;
            _chart.View3D.Camera.Target.SetValues(-9, -18, 2);

            _chart.View3D.Camera.MinimumViewDistance = 10;
            _chart.View3D.Camera.ViewDistance = 140;

            _chart.ChartName = "Spectrum 3D chart";

            double dAxisZMin = timeStepMs / 1000.0 - timeRangeLengthSec;
            double dAxisZMax = 0;
            m_dStepZ = timeStepMs / 1000.0;
            _chart.View3D.ZAxisPrimary3D.SetRange(dAxisZMin, dAxisZMax);
            _chart.View3D.XAxisPrimary3D.SetRange(freqMin, freqMax);

            m_dCurrentZ = 0;

            //Create surface grid series
            m_surface = new SurfaceGridSeries3D(_chart.View3D, Axis3DBinding.Primary,
                       Axis3DBinding.Primary, Axis3DBinding.Primary);
            m_surface.ContourPalette = CreatePalette(m_surface, dDefaultYMax);
            _chart.View3D.SurfaceGridSeries3D.Add(m_surface);

            m_surface.InitialValue = 0;
            m_surface.WireframeType = SurfaceWireframeType3D.None;
            m_surface.ContourLineType = ContourLineType3D.None;
            m_surface.SetSize(resolution, (int)Math.Round(timeRangeLengthSec / (timeStepMs / 1000.0)));
            m_surface.FadeAway = 100;
            m_surface.SuppressLighting = false;
            m_surface.SetRangesXZ(freqMin, freqMax, -timeRangeLengthSec, 0);
            m_surface.ShowInLegendBox = false;
            m_surface.Material.EmissiveColor = Color.FromArgb(255, 0, 40, 0);
            m_surface.BaseColor = Colors.White;

            //Create Waterfall series to front
            m_waterfallFront = new WaterfallSeries3D(_chart.View3D, Axis3DBinding.Primary,
                    Axis3DBinding.Primary, Axis3DBinding.Primary);
            m_waterfallFront.ContourPalette = CreatePalette(m_waterfallFront, dDefaultYMax);
            _chart.View3D.WaterfallSeries3D.Add(m_waterfallFront);
            m_waterfallFront.SetSize(resolution, 1);
            m_waterfallFront.BaseLevel = _chart.View3D.YAxisPrimary3D.Minimum;
            m_waterfallFront.FadeAway = 0;
            m_waterfallFront.SuppressLighting = false;
            m_waterfallFront.Material.EmissiveColor = Color.FromArgb(255, 0, 40, 0);
            m_waterfallFront.BaseColor = Colors.White;
            m_waterfallFront.ShowInLegendBox = false;

            //Init one row
            SurfacePoint[,] areaData = m_waterfallFront.Data;
            int iColCount = m_waterfallFront.SizeX;
            double x = _chart.View3D.XAxisPrimary3D.Minimum;
            double stepX;
            if (iColCount > 1)
            {
                stepX = (_chart.View3D.XAxisPrimary3D.Maximum - _chart.View3D.XAxisPrimary3D.Minimum) / (iColCount - 1);
            }
            else
            {
                stepX = 0;
            }

            double z = _chart.View3D.ZAxisSecondary3D.Maximum;
            double y = m_waterfallFront.InitialValue;
            for (int iCol = 0; iCol < iColCount; iCol++)
            {
                areaData[iCol, 0].X = x;
                areaData[iCol, 0].Y = y;
                areaData[iCol, 0].Z = z;
                x += stepX;
            }
            m_waterfallFront.InvalidateData();
            m_waterfallFront.ContourLineType = WaterfallContourLineType.None;
            m_waterfallFront.WireframeType = WaterfallWireframeType.None;

            _chart.EndUpdate();
        }

        public LightningChart Chart
        {
            get
            {
                return _chart;
            }
        }

        public string GetFileName()
        {
            return GetFileNameFromStackFrame();
        }

        private string GetFileNameFromStackFrame()
        {
            System.Diagnostics.StackFrame stackFrame = new System.Diagnostics.StackFrame(1, true);
            string stFileName = stackFrame.GetFileName();

            return stFileName;
        }

        private ValueRangePalette CreatePalette(SeriesBase3D ownerSeries, double yRange)
        {
            ValueRangePalette palette = new ValueRangePalette(ownerSeries);

            palette.Steps.Clear();
            palette.Steps.Add(new PaletteStep(palette, Colors.Blue, 0));
            palette.Steps.Add(new PaletteStep(palette, Colors.Lime, 30 * yRange / 100.0));
            palette.Steps.Add(new PaletteStep(palette, Colors.Yellow, 60.0 * yRange / 100.0));
            palette.Steps.Add(new PaletteStep(palette, Colors.Red, 100.0 * yRange / 100.0));
            palette.Type = PaletteType.Gradient;
            return palette;
        }

        public void Dispose()
        {
            if (m_surface != null)
            {
                m_surface.Dispose();
                m_surface = null;
            }

            if (m_waterfallFront != null)
            {
                m_waterfallFront.Dispose();
                m_waterfallFront = null;
            }

            if (_chart != null)
            {
                _chart.Dispose();
                _chart = null;
            }
        }

        public void FitView()
        {
            int iColumns = m_surface.SizeX;
            int rows = m_surface.SizeZ;
            SurfacePoint[,] data = m_surface.Data;
            double minY = double.MaxValue;
            double maxY = double.MinValue;
            double y;
            _chart.BeginUpdate();
            if (m_surface != null)
            {
                for (int iCol = 0; iCol < iColumns; iCol++)
                {
                    for (int row = 0; row < rows; row++)
                    {
                        y = data[iCol, row].Y;
                        if (y > maxY)
                        {
                            maxY = y;
                        }

                        if (y < minY)
                        {
                            minY = y;
                        }
                    }
                }
                m_surface.ContourPalette = CreatePalette(m_surface, maxY);
                _chart.View3D.YAxisPrimary3D.SetRange(0, maxY);
            }
            if (m_waterfallFront != null)
            {
                m_waterfallFront.ContourPalette = CreatePalette(m_waterfallFront, maxY);
            }
            _chart.EndUpdate();
        }

        public void SetData(double[][][] yValues, int channelIndex, int rowCount)
        {
            if (_chart == null)
            {
                return;
            }

            _chart.BeginUpdate();

            for (int row = 0; row < rowCount; row++)
            {
                double[] aYValues = yValues[row][channelIndex];
                m_dCurrentZ += m_dStepZ;

                //Surface grid series has optimized methods for adding data to back.

                //SurfacePoint[,] surfaceData = m_surface.Data;
                double dZMin = m_dCurrentZ - (_chart.View3D.ZAxisPrimary3D.Maximum - _chart.View3D.ZAxisPrimary3D.Minimum);
                double dZMax = m_dCurrentZ;
                //SurfaceGridSeries3D.SurfaceGridInsertResult res = m_surface.InsertRowBackAndScroll(aYValues, dZMin, dZMax, dZMin, dZMax);
                m_surface.InsertRowBackAndScroll(aYValues, dZMin, dZMax, dZMin, dZMax);

                //Update front waterfall
                SurfacePoint[,] areaData = m_waterfallFront.Data;

                //Shift older data, drop oldest row. Set new data to back  
                int iColCount = m_waterfallFront.SizeX;

                int iNewDataCopyCount = Math.Min(aYValues.Length, iColCount);

                for (int iCol = 0; iCol < iNewDataCopyCount; iCol++)
                {
                    //No need to update X or Z values 
                    areaData[iCol, 0].Y = aYValues[iCol];
                    areaData[iCol, 0].Z = m_dCurrentZ;
                }
            }

            m_waterfallFront.InvalidateData();

            _chart.EndUpdate();
        }

        public void SetBounds(int x, int y, int width, int height)
        {
            _chart.Margin = new Thickness(x, y, 0, 0);
            _chart.Width = width;
            _chart.Height = height;
        }
    }
    public class RealtimeFFTCalculator
    {
        /// <summary>
        /// Interval between FFT calculations.
        /// </summary>
        private double _intervalMs = 1;

        /// <summary>
        /// Sampling frequecy.
        /// </summary>
        private int _samplingFrequency = 1;

        /// <summary>
        /// First FFT data out.
        /// </summary>
        private long _startTicks = 0;

        /// <summary>
        /// Last FFT data out.
        /// </summary>
        private long _lastTicks = 0;

        /// <summary>
        /// TimeSpan interval between FFT calculations in ms.
        /// </summary>
        private long _updateInterval = 0;

        /// <summary>
        /// Amount of channels in discrete input signal.
        /// </summary>
        private int _channelCount = 0;

        /// <summary>
        /// Old discrete data was rendered.
        /// </summary>
        private double[][] _oldData;

        /// <summary>
        /// Data resolution to be presented in a monitor.
        /// </summary>
        private int _windowResolution = 512;

        /// <summary>
        /// Index of a data.
        /// </summary>
        private int _FFTEntryIndex = 0;

        /// <summary>
        /// Spectrum calculator. FFT routines with a given signal.
        /// </summary>
        private SpectrumCalculator _spectrumCalculator;

        /// <summary>
        /// FFT calculator constructor.
        /// </summary>
        /// <param name="updateIntervalMs">How often FFT should be calculated</param>
        /// <param name="samplingFrequency">Sampling frequency</param>
        /// <param name="windowLength">FFT window length. Does not have to be power of 2.</param>
        public RealtimeFFTCalculator(double updateIntervalMs, int samplingFrequency, int windowLength, int channelCount)
        {
            _channelCount = channelCount;
            _intervalMs = updateIntervalMs;
            _samplingFrequency = samplingFrequency;
            _updateInterval = TimeSpan.FromMilliseconds(updateIntervalMs).Ticks;
            _windowResolution = windowLength;
            _lastTicks = _startTicks = DateTime.Now.Ticks;

            _oldData = new double[channelCount][];
            for (int i = 0; i < channelCount; i++)
            {
                _oldData[i] = new double[0];
            }

            _spectrumCalculator = new SpectrumCalculator();
        }

        /// <summary>
        /// Calculates FFT from multi-channel sample stream.
        /// </summary>
        /// <param name="data">Sample data stream</param>
        /// <param name="xValues">X values, by channels</param>
        /// <param name="yValues">Y values, by channels</param>
        /// <returns>True if FFT result available</returns>
        public bool FeedDataAndCalculate(double[][] data, out double[][][] xValues, out double[][][] yValues)
        {
            xValues = null;
            yValues = null;

            // Validation of discrete data.
            if (data == null)
            {
                return false;
            }

            // Reference to member variable channel counter.
            int channelCounter = _channelCount;

            // Timer ticks.
            long ticksNow = DateTime.Now.Ticks;

            // Flag allows data to be rendered.
            bool giveDataOut = false;

            // Count how many samples should be rendered per update.
            int samplesPerUpdate = (int)(_intervalMs * _samplingFrequency / 1000.0);

            // Amount of repeated rounds in a loop for FFT data output. Samples length / samples per update.
            int repeatFFT;

            if (_FFTEntryIndex == 0)
            {
                repeatFFT = 1;
            }
            else
            {
                repeatFFT = (int)Math.Ceiling((double)data[0].Length / samplesPerUpdate);
            }

            // Dimensions: FFT calculations, channel, FFT result value.
            double[][][] valuesX = new double[repeatFFT][][];
            double[][][] valuesY = new double[repeatFFT][][];

            for (int i = 0; i < repeatFFT; i++)
            {
                // Calculate amount of FFT calculus points from DATA must be copied (<=samplesPerUpdate).
                int samplesCopiesAmount = Math.Min(samplesPerUpdate, data[0].Length - i * samplesPerUpdate);

                // For the 1st entry window resolution = number of points.
                if (_FFTEntryIndex == 0)
                {
                    samplesCopiesAmount = data[0].Length;
                }

                // Allocate buffers according to amount of channels.
                valuesX[i] = new double[channelCounter][];
                valuesY[i] = new double[channelCounter][];

                // Keeps information about samples amount in combined buffer.
                int samplesInCombinedData = 0;

                System.Threading.Tasks.Parallel.For(0, channelCounter, channelNumber =>
                {
                    // Create extended buffer with old and new data.
                    double[] combinedData = new double[_oldData[channelNumber].Length + samplesCopiesAmount];

                    // Copy OLD data.
                    Array.Copy(_oldData[channelNumber], 0, combinedData, 0, _oldData[channelNumber].Length);

                    // Copy NEW data.
                    Array.Copy(data[channelNumber], i * samplesPerUpdate, combinedData, _oldData[channelNumber].Length, samplesCopiesAmount);

                    // Get information how many samples now.
                    samplesInCombinedData = combinedData.Length;

                    // Initiate FFT calculus at fixed interval from Time-reference 
                    if (samplesInCombinedData >= _windowResolution &&
                        ticksNow >= (_startTicks + _updateInterval * (_FFTEntryIndex + 1)))
                    {
                        // Data will be visible with defined resolution.
                        double[] dataCombined = new double[_windowResolution];
                        int index = 0;

                        for (int j = samplesInCombinedData - _windowResolution; j < samplesInCombinedData; j++)
                        {
                            dataCombined[index++] = combinedData[j];
                        }

                        // Calculate FFT spectrum data.
                        double[] fftResult;
                        _spectrumCalculator.PowerSpectrum(dataCombined, out fftResult);

                        // FFT data quantity.
                        int length = fftResult.Length;

                        // Allocate buffers for FFT data for each channels.
                        valuesX[i][channelNumber] = new double[length];
                        valuesY[i][channelNumber] = new double[length];

                        // Period. Accurate estimate of DC (0 HZ) component.
                        double stepX = _samplingFrequency / 2.0 / (length - 1.0);

                        for (int point = 1; point < length; point++)
                        {
                            valuesX[i][channelNumber][point] = point * stepX;
                            valuesY[i][channelNumber][point] = fftResult[point];
                        }

                        // Amount of new samples will be old data samples quantity in the next round.
                        int samplesAmountNew = samplesPerUpdate;

                        if (_windowResolution > samplesAmountNew)
                        {
                            samplesAmountNew = _windowResolution;
                        }

                        if (samplesInCombinedData > samplesAmountNew)
                        {
                            //Drop oldest samples out 
                            _oldData[channelNumber] = new double[samplesAmountNew];
                            Array.Copy(combinedData, samplesInCombinedData - samplesAmountNew, _oldData[channelNumber], 0, samplesAmountNew);
                        }

                        giveDataOut = true;
                    }
                    else
                    {
                        _oldData[channelNumber] = combinedData;
                    }
                });

                // Avoid some uncertainties in a loop.
                if (samplesInCombinedData >= _windowResolution &&
                    ticksNow >= (_startTicks + _updateInterval * (_FFTEntryIndex + 1)))
                {
                    // Start tick counting when first entry comes.
                    if (_FFTEntryIndex == 0)
                    {
                        _startTicks = ticksNow;
                    }

                    _FFTEntryIndex++;
                }
            }

            if (giveDataOut)
            {
                _lastTicks = ticksNow;

                // Reduce FFT calculus rounds if it was not enough points to Calculate FFT.
                while (repeatFFT > 0 && valuesX[repeatFFT - 1][0] == null)
                {
                    repeatFFT--;
                }

                if (repeatFFT == 0)
                {
                    giveDataOut = false;
                    return giveDataOut;
                }

                xValues = new double[repeatFFT][][];
                yValues = new double[repeatFFT][][];

                for (int i = 0; i < repeatFFT; i++)
                {
                    xValues[i] = new double[channelCounter][];
                    yValues[i] = new double[channelCounter][];

                    // Copy FFT results to output.
                    for (int channelNumber = 0; channelNumber < channelCounter; channelNumber++)
                    {
                        xValues[i][channelNumber] = valuesX[i][channelNumber];
                        yValues[i][channelNumber] = valuesY[i][channelNumber];
                    }
                }
            }

            return giveDataOut;
        }


        public void Dispose()
        {
            if (_spectrumCalculator != null)
            {
                _spectrumCalculator.Dispose();
                _spectrumCalculator = null;
            }
        }
    }
}
