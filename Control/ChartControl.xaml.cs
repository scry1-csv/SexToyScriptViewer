using OxyPlot.Annotations;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
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
        private readonly Controller _controller;
        private readonly List<OxyPlot.Wpf.RectangleAnnotation> UfotwDefferenceAnnotations = new();
        private readonly List<OxyPlot.Wpf.RectangleAnnotation> UfotwDefferenceAnnotations2 = new();
        private Func<double, string>? _scriptTimeFormatter;

        public bool IsDualChart { get; private set; }

        internal ChartControl(Controller controller)
        {
            InitializeComponent();
            _controller = controller;
        }

        public void InitializeChart(
            string fileName,
            double plotMin, double plotMax,
            string trackerFormatString,
            System.Collections.IEnumerable itemsSource,
            System.Collections.IEnumerable? itemsSource2,
            Func<double, string>? scriptTimeFormatter,
            IEnumerable<(double start, double end)>? differenceRanges)
        {
            FileNameBlock.Text = fileName;
            IsDualChart = itemsSource2 != null;
            _scriptTimeFormatter = scriptTimeFormatter;

            PowerAxis.Maximum = PowerAxis.AbsoluteMaximum = plotMax;
            PowerAxis.Minimum = PowerAxis.AbsoluteMinimum = plotMin;
            LineSeries.ItemsSource = itemsSource;
            LineSeries.TrackerFormatString = trackerFormatString;

            TimeAxis.InternalAxis.AxisChanged += AxisChangedEvent;

            if (_scriptTimeFormatter != null)
            {
                TimeAxis.LabelFormatter = _scriptTimeFormatter;
                TimeAxis2.LabelFormatter = _scriptTimeFormatter;
            }

            if (IsDualChart)
            {
                PlotsGrid.RowDefinitions.Add(new() { Height = new GridLength(1.23, GridUnitType.Star) });
                TimeAxis.TextColor = Colors.Transparent;
                TimeAxis.TickStyle = TickStyle.None;

                OxyPlotView.Padding = new(8, 8, 8, 8);
                var margins = OxyPlotView.PlotMargins;
                margins.Bottom = 0;
                OxyPlotView.PlotMargins = margins;

                TimeAxis.TitleFontSize = 1;

                LineSeries2.ItemsSource = itemsSource2;
                LineSeries2.TrackerFormatString = trackerFormatString;
                OxyPlotView2.Visibility = Visibility.Visible;
                TimeAxis2.InternalAxis.AxisChanged += Axis2ChangedEvent;

                if (differenceRanges != null)
                {
                    CheckBox_UfotwLRDifferent.Visibility = Visibility.Visible;
                    foreach (var (start, end) in differenceRanges)
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
                }
            }

            OxyPlotView.ResetAllAxes();
            OxyPlotView2.ResetAllAxes();
        }

        public void SetTimeAxisLabelScriptTime()
        {
            if (_scriptTimeFormatter != null)
            {
                TimeAxis.LabelFormatter = _scriptTimeFormatter;
                TimeAxis2.LabelFormatter = _scriptTimeFormatter;
            }
        }

        public void SetTimeAxisLabelHHMMSS()
        {
            TimeAxis.LabelFormatter = TimeAxis2.LabelFormatter = LabelFormatter_HHMMSS;
        }

        private static string LabelFormatter_HHMMSS(double milliseconds) =>
            SexToyScriptViewer.Script.ScriptUtil.MillisecondsToHHMMSS(milliseconds);

        public void MovePlayingAnnotation(double milliseconds)
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

            if (IsDualChart)
            {
                if (min < 0)
                    TimeAxis2.InternalAxis.Zoom(0, range);
                else
                    TimeAxis2.InternalAxis.Zoom(min, position + range / 2);
            }
        }

        public void ZoomTimeAxis(double min, double max)
        {
            if (min == TimeAxis.InternalAxis.ActualMinimum &&
                max == TimeAxis.InternalAxis.ActualMaximum)
                return;

            TimeAxis.InternalAxis.Zoom(min, max);
            OxyPlotView.InvalidatePlot();
            if (IsDualChart)
            {
                TimeAxis2.InternalAxis.Zoom(min, max);
                OxyPlotView2.InvalidatePlot();
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            _controller.ReloadChart(this);
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _controller.CloseChart(this);
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
                Dispatcher.Invoke(() => _controller.SyncChartsRange(this, min, max));
                if (IsDualChart)
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
                Dispatcher.Invoke(() => _controller.SyncChartsRange(this, min, max));
            }
        }

        private void OxyPlotView_PreviewMouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _controller.IsUserDragging = true;
        }

        private void OxyPlotView_PreviewMouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            _controller.IsUserDragging = false;
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
            foreach (var a in UfotwDefferenceAnnotations2)
                OxyPlotView2.Annotations.Remove(a);

            OxyPlotView.InvalidatePlot();
            OxyPlotView2.InvalidatePlot();
        }
    }
}
