using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using SexToyScriptViewer.Script;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace SexToyScriptViewer.Control
{
    /// <summary>
    /// OxyChart.xaml の相互作用ロジック
    /// </summary>
    public partial class ChartControl : UserControl
    {
        private readonly IScript _script;
        private readonly MainWindow _mainWindow;
        private readonly List<OxyPlot.Wpf.RectangleAnnotation> UfotwDefferenceAnnotations = new();
        private readonly List<OxyPlot.Wpf.RectangleAnnotation> UfotwDefferenceAnnotations2 = new();

        public bool IsUFOTW { get; init; }

        public ChartControl(MainWindow mainWindow, IScript script)
        {
            InitializeComponent();

            _script = script;
            _mainWindow = mainWindow;
            FileNameBlock.Text = _script.FileName;

            PowerAxis.Maximum = PowerAxis.AbsoluteMaximum = script.PlotMax;
            PowerAxis.Minimum = PowerAxis.AbsoluteMinimum = script.PlotMin;
            LineSeries.ItemsSource = script.ToPlot();
            LineSeries.TrackerFormatString = _script.TrackerFormatString;
            TimeAxis.InternalAxis.AxisChanged += AxisChangedEvent;
            OxyPlotView.ResetAllAxes();
        }

        public ChartControl(MainWindow mainWindow, UFOTW script) : this(mainWindow, (IScript)script)
        {
            IsUFOTW = true;

            PlotsGrid.RowDefinitions.Add(new() { Height = new GridLength(1.23, GridUnitType.Star) });
            TimeAxis.TextColor = Colors.Transparent;
            TimeAxis.TickStyle = TickStyle.None;

            OxyPlotView.Padding = new(8, 8, 8, 8);
            OxyPlotView2.Padding = new(8, 0, 8, 4);

            var margins = OxyPlotView.PlotMargins;
            margins.Bottom = 0;
            OxyPlotView.PlotMargins = margins;

            margins = OxyPlotView2.PlotMargins;
            margins.Top = 0;
            OxyPlotView2.PlotMargins = margins;

            TimeAxis.TitleFontSize = 1;
            PowerAxis2.Maximum = PowerAxis2.AbsoluteMaximum = 100;
            PowerAxis2.Minimum = PowerAxis2.AbsoluteMinimum = -100;
            LineSeries2.ItemsSource = script.ToPlotRight();
            LineSeries2.TrackerFormatString = script.TrackerFormatString;
            OxyPlotView2.Visibility = Visibility.Visible;
            TimeAxis2.InternalAxis.AxisChanged += Axis2ChangedEvent;

            CheckBox_UfotwLRDifferent.Visibility = Visibility.Visible;
            var deference = script.DetectDeference();
            foreach (var (start, end) in deference)
            {
                UfotwDefferenceAnnotations.Add(new OxyPlot.Wpf.RectangleAnnotation()
                {
                    MinimumX = start,
                    MaximumX = end,
                    MinimumY = -100,
                    MaximumY = 100,
                    Fill = Colors.NavajoWhite,
                    Layer = AnnotationLayer.BelowSeries
                });

                UfotwDefferenceAnnotations2.Add(new OxyPlot.Wpf.RectangleAnnotation()
                {
                    MinimumX = start,
                    MaximumX = end,
                    MinimumY = -100,
                    MaximumY = 100,
                    Fill = Colors.NavajoWhite,
                    Layer = AnnotationLayer.BelowSeries
                });
            }

            OxyPlotView.ResetAllAxes();
            OxyPlotView2.ResetAllAxes();
        }



        public void SetTimeAxisLabelScriptTime()
        {
            TimeAxis.LabelFormatter = TimeAxis2.LabelFormatter = _script.LabelFormatter_ScriptTime;
        }

        public virtual void SetTimeAxisLabelHHMMSS()
        {
            TimeAxis.LabelFormatter = TimeAxis2.LabelFormatter = LabelFormatter_HHMMSS;
        }

        private static string LabelFormatter_HHMMSS(double milliseconds) => ScriptUtil.MillisecondsToHHMMSS((int)milliseconds);

        public virtual void MovePlayingAnnotation(double milliseconds)
        {
            double position = milliseconds;
            double actualMax = TimeAxis.InternalAxis.ActualMaximum;
            double actualMin = TimeAxis.InternalAxis.ActualMinimum;
            double range = actualMax - actualMin;

            PlayingAnnotation.X = position;
            PlayingAnnotation2.X = position;

            double min = position - range / 2;
            if (min < 0)
                TimeAxis.InternalAxis.Zoom(0, range);
            else
                TimeAxis.InternalAxis.Zoom(min, position + range / 2);

            if (_script is UFOTW)
                if (min < 0)
                    TimeAxis2.InternalAxis.Zoom(0, range);
                else
                    TimeAxis2.InternalAxis.Zoom(min, position + range / 2);

        }

        public void ZoomTimeAxis(double min, double max)
        {
            if (min == TimeAxis.InternalAxis.ActualMinimum &&
                max == TimeAxis.InternalAxis.ActualMaximum)
                return;

            TimeAxis.InternalAxis.Zoom(min, max);
            OxyPlotView.InvalidatePlot();
            if (_script is UFOTW)
            {
                TimeAxis2.InternalAxis.Zoom(min, max);
                OxyPlotView2.InvalidatePlot();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _mainWindow.CloseChart(this);
        }

        private void AxisChangedEvent(object? sender, AxisChangedEventArgs e)
        {
            var min = TimeAxis.InternalAxis.ActualMinimum;
            var max = TimeAxis.InternalAxis.ActualMaximum;

            if (min == TimeAxis2.InternalAxis.ActualMinimum &&
                max == TimeAxis2.InternalAxis.ActualMaximum)
                return;

            var type = e.ChangeType;
            if (type == AxisChangeTypes.Pan | type == AxisChangeTypes.Zoom)
            {
                Dispatcher.Invoke(() => _mainWindow.SyncChartsRange(this));
                if (_script is UFOTW)
                {
                    TimeAxis2.InternalAxis.Zoom(min, max);
                    Dispatcher.Invoke(() => OxyPlotView2.InvalidatePlot());
                }
            }
        }

        private void Axis2ChangedEvent(object? sender, AxisChangedEventArgs e)
        {
            var min = TimeAxis2.InternalAxis.ActualMinimum;
            var max = TimeAxis2.InternalAxis.ActualMaximum;

            if (min == TimeAxis.InternalAxis.ActualMinimum &&
                max == TimeAxis.InternalAxis.ActualMaximum)
                return;

            var type = e.ChangeType;
            if (type == AxisChangeTypes.Pan | type == AxisChangeTypes.Zoom)
            {
                TimeAxis.InternalAxis.Zoom(min, max);
                Dispatcher.Invoke(() => OxyPlotView.InvalidatePlot());
                Dispatcher.Invoke(() => _mainWindow.SyncChartsRange(this));
            }
        }

        private void OxyPlotView_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _mainWindow.IsUserDragging = true;
        }

        private void OxyPlotView_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _mainWindow.IsUserDragging = false;
        }

        private void CheckBox_UfotwLRDifferent_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var a in UfotwDefferenceAnnotations)
                OxyPlotView.Annotations.Add(a);
            foreach (var a in UfotwDefferenceAnnotations2)
                OxyPlotView2.Annotations.Add(a);

            OxyPlotView.InvalidatePlot();
            OxyPlotView2.InvalidatePlot();
        }

        private void CheckBox_UfotwLRDifferent_Unchecked(object sender, RoutedEventArgs e)
        {
            foreach (var a in UfotwDefferenceAnnotations)
                OxyPlotView.Annotations.Remove(a);
            foreach (var a2 in UfotwDefferenceAnnotations2)
                OxyPlotView2.Annotations.Remove(a2);

            OxyPlotView.InvalidatePlot();
            OxyPlotView2.InvalidatePlot();
        }
    }
}
