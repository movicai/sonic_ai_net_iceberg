using Arction.Wpf.Charting.Axes;
using Arction.Wpf.Charting.SeriesXY;
using Arction.Wpf.Charting.Views.ViewXY;
using Arction.Wpf.Charting;
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

namespace InteractiveExamples
{
    /// <summary>
    /// Interaction logic for ExampleCursorProjectOnAxes.xaml
    /// </summary>
    public partial class DetectTrendChartControl : UserControl, IDisposable
    {
        private LightningChart _chart;

        public DetectTrendChartControl()
        {
            InitializeComponent();
            CreateChart();
            CreateProjections(1000, 800);
        }

        /// <summary>
        /// Create chart.
        /// </summary>
        private void CreateChart()
        {
            // Create a new chart.
            _chart = new LightningChart
            {
                ChartName = "Cursor tracking chart"
            };

            //Disable rendering, strongly recommended before updating chart properties
            _chart.BeginUpdate();



            //Chart title
            _chart.Title.Visible = false;

            //Hide legend box
            _chart.ViewXY.LegendBoxes[0].Visible = false;

            ViewXY viewXY = _chart.ViewXY;
            AxisX xAxis = viewXY.XAxes[0];
            AxisY yAxis = viewXY.YAxes[0];

            // Configure x-axis.
            xAxis.ValueType = AxisValueType.DateTime;
            xAxis.ScrollMode = XAxisScrollMode.None;
            xAxis.AutoFormatLabels = false;
            xAxis.LabelsAngle = 90;
            xAxis.AutoDivSpacing = false;
            xAxis.MajorDiv = 0.001; //1 ms
            xAxis.KeepDivCountOnRangeChange = false;
            xAxis.LabelsTimeFormat = "dd-MM-yyyy HH:mm.ss.fff";
            xAxis.Title.Visible = false;

            //Set y-axis
            yAxis.Title.Text = "EUR/USD";

            PointLineSeries pointLineSeries = new PointLineSeries(viewXY, xAxis, yAxis);
            pointLineSeries.LineStyle.Width = 1;
            pointLineSeries.AllowUserInteraction = false;
            viewXY.PointLineSeries.Add(pointLineSeries);

            int pointsCount = 100;

            //Create series
            SeriesPoint[] points = new SeriesPoint[pointsCount];

            DateTime startTime = DateTime.Now;
            startTime = new DateTime(startTime.Year, startTime.Month, startTime.Day, startTime.Hour, startTime.Minute, startTime.Second);

            Random rand = new Random();
            double y = 1.3;

            //Randomize some data
            for (int i = 0; i < pointsCount; i++)
            {
                points[i].X = xAxis.DateTimeToAxisValue(startTime + TimeSpan.FromMilliseconds(i));

                y += (rand.NextDouble() - 0.5) * 0.01;
                if (y > 1.35)
                {
                    y = 1.35;
                }

                if (y < 1.25)
                {
                    y = 1.25;
                }

                points[i].Y = y;
            }
            pointLineSeries.Points = points;

            //Create secondary axes, to show cursor intersections on axes by using their CustomAxisTicks with one value only. 
            //The CustomAxisTick gets intersection value, nothing else, and shows a major tick, label and grid line on that position.

            //Disable automatic axis placement, so that the axes are in same positions (Position = 0 for YAxes, Position = 100 for XAxes); 
            viewXY.AxisLayout.YAxisAutoPlacement = YAxisAutoPlacement.LeftThenRight;
            viewXY.AxisLayout.XAxisAutoPlacement = XAxisAutoPlacement.BottomThenTop;

            AxisX secondaryXAxis = new AxisX(viewXY)
            {
                AllowUserInteraction = false,
                AutoFormatLabels = false,
                CustomTicksEnabled = true,
                AxisColor = Colors.Transparent
            };
            secondaryXAxis.MajorGrid.Color = _chart.Title.Color;
            secondaryXAxis.MajorGrid.Pattern = LinePattern.SmallDot;
            secondaryXAxis.LabelsColor = _chart.Title.Color;
            secondaryXAxis.Title.Visible = false;
            secondaryXAxis.MajorDivTickStyle.Color = _chart.Title.Color;
            secondaryXAxis.LabelsFont = new WpfFont("Segoe UI", 10.0, "10", true, false);
            secondaryXAxis.AllowScaling = false;
            viewXY.XAxes.Add(secondaryXAxis);

            AxisY secondaryYAxis = new AxisY(viewXY)
            {
                AllowUserInteraction = false,
                AutoFormatLabels = false,
                CustomTicksEnabled = true,
                AxisColor = Colors.Transparent
            };
            secondaryYAxis.MajorGrid.Color = _chart.Title.Color;
            secondaryYAxis.MajorGrid.Pattern = LinePattern.SmallDot;
            secondaryYAxis.LabelsColor = _chart.Title.Color;
            secondaryYAxis.Title.Visible = false;
            secondaryYAxis.MajorDivTickStyle.Color = _chart.Title.Color;
            secondaryYAxis.LabelsFont = new WpfFont("Segoe UI", 10.0, "10", true, false);
            secondaryYAxis.AllowScaling = false;
            viewXY.YAxes.Add(secondaryYAxis);

            xAxis.RangeChanged += xAxis_RangeChanged;
            yAxis.RangeChanged += yAxis_RangeChanged;

            viewXY.ZoomToFit();
            bool scaleChanged;
            yAxis.Fit(10.0, out scaleChanged, true, false);

            _chart.MouseMove += new MouseEventHandler(_chart_MouseMove);
            _chart.EndUpdate();

            gridChart.Children.Add(_chart);

        }

        private void yAxis_RangeChanged(object sender, RangeChangedEventArgs e)
        {
            //Set the same range for secondary Y axis
            e.CancelRendering = true;
            _chart.ViewXY.YAxes[1].SetRange(e.NewMin, e.NewMax);
        }

        private void xAxis_RangeChanged(object sender, RangeChangedEventArgs e)
        {
            //Set the same range for secondary X axis
            e.CancelRendering = true;
            _chart.ViewXY.XAxes[1].SetRange(e.NewMin, e.NewMax);
        }

        private void _chart_MouseMove(object sender, MouseEventArgs e)
        {
            Point point = e.GetPosition(_chart);
            CreateProjections((int)point.X, (int)point.Y);
        }

        private void CreateProjections(int pX, int pY)
        {
            double x, y;
            int nearestIndex = 0;

            AxisX secondaryXAxis = _chart.ViewXY.XAxes[1];
            AxisY secondaryYAxis = _chart.ViewXY.YAxes[1];

            bool solved = _chart.ViewXY.PointLineSeries[0].SolveNearestDataPointByCoord(pX, pY, out x, out y, out nearestIndex);
            if (solved)
            {
                _chart.BeginUpdate();

                secondaryXAxis.CustomTicks.Clear(); // Remove existing custom tickmarkers.
                secondaryXAxis.CustomTicks.Add(new CustomAxisTick(secondaryXAxis, x, secondaryXAxis.AxisValueToDateTime(x).ToString("dd-MM-yyyy HH:mm.ss.fff")));
                secondaryXAxis.InvalidateCustomTicks();

                secondaryYAxis.CustomTicks.Clear(); // Remove existing custom tickmarkers.
                secondaryYAxis.CustomTicks.Add(new CustomAxisTick(secondaryYAxis, y, y.ToString("0.000")));
                secondaryYAxis.InvalidateCustomTicks();

                _chart.EndUpdate();
            }
            else
            {
                _chart.BeginUpdate();

                secondaryXAxis.CustomTicks.Clear(); // Remove existing custom tickmarkers.
                secondaryXAxis.InvalidateCustomTicks();

                secondaryYAxis.CustomTicks.Clear(); // Remove existing custom tickmarkers.
                secondaryYAxis.InvalidateCustomTicks();

                _chart.EndUpdate();
            }
        }

        /// <summary>
        /// Performes releasing of unmanaged objects.
        /// </summary>
        public void Dispose()
        {
            // Don't forget to clear chart grid child list.
            gridChart.Children.Clear();

            if (_chart != null)
            {
                // Chart's Dispose method needs to be called when chart is 
                // no longer needed so that all unmanaged resources 
                // (DirectX etc.) are released.
                _chart.Dispose();
                _chart = null;
            }


        }
    }

}
