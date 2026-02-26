using Microsoft.Win32;
using SexToyScriptViewer.Control;
using SexToyScriptViewer.Script;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace SexToyScriptViewer
{
    internal class Controller
    {
        private readonly MainWindow _mainWindow;
        private readonly System.Windows.Threading.DispatcherTimer _annotationsSyncTimer;
        private readonly System.Windows.Threading.DispatcherTimer _seekbarTimer;

        public bool IsUserDragging = false;
        private double zoomMin = 0;
        private double zoomMax = 0;

        private readonly List<ChartControl> _chartControls = new();
        private readonly Dictionary<ChartControl, string> _chartFilePaths = new();
        private string _mediaDuration = "";

        public Controller(MainWindow mainWindow)
        {
            _mainWindow = mainWindow;

            _annotationsSyncTimer = new() { Interval = TimeSpan.FromMilliseconds(10) };
            _annotationsSyncTimer.Tick += AnnotationsSyncTimer_Tick;

            _seekbarTimer = new() { Interval = TimeSpan.FromSeconds(1) };
            _seekbarTimer.Tick += SeekbarTimer_Tick;
        }

        public void CloseChart(ChartControl control)
        {
            _chartControls.Remove(control);
            _chartFilePaths.Remove(control);
            RefleshCharts();
        }

        public void SyncChartsRange(ChartControl sender, double min, double max)
        {
            zoomMin = min;
            zoomMax = max;
            foreach (var item in _chartControls)
                if (item != sender)
                    item.ZoomTimeAxis(min, max);
        }

        public void MovePlayingAnnotations(double milliseconds)
        {
            foreach (var c in _chartControls)
                c.MovePlayingAnnotation(milliseconds);
        }

        public void RefleshCharts()
        {
            _mainWindow.ChartsPanel.Children.Clear();
            _mainWindow.ChartsPanel.RowDefinitions.Clear();

            int i = 0;
            foreach (var c in _chartControls)
            {
                System.Windows.Controls.RowDefinition row = new();
                if (c.IsDualChart) row.Height = new GridLength(1.55, GridUnitType.Star);
                _mainWindow.ChartsPanel.RowDefinitions.Add(row);

                System.Windows.Controls.Grid.SetRow(_chartControls[i], i);
                _mainWindow.ChartsPanel.Children.Add(_chartControls[i]);
                i++;

                if (zoomMin > 0 | zoomMax > 0)
                    c.ZoomTimeAxis(zoomMin, zoomMax);
            }
        }

        public void OnFileDropped(string[] dropped)
        {
            if (dropped.Length == 1)
                OpenFile(dropped[0]);
            else
                Util.ShowMessageBoxTopMost("開けるのは同時に一つのファイルだけです！");
        }

        public void OpenFile(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".csv":
                case ".funscript":
                case ".coyotescript":
                    OpenScript(path);
                    break;
                case ".mp3":
                case ".m4a":
                case ".wav":
                case ".mp4":
                case ".webm":
                case ".mpg":
                    LoadMedia(path);
                    break;
                default:
                    Util.ShowMessageBoxTopMost("対応していないファイル形式です");
                    break;
            }
        }

        public void OpenScript(string path)
        {
            var script = ScriptUtil.LoadScript(path);

            if (script == null)
            {
                Util.ShowMessageBoxTopMost("スクリプトの読み込みに失敗しました。");
                return;
            }

            ChartControl control = new ChartControl(this);
            InitializeChartControlWithScript(control, script);

            _chartControls.Add(control);
            _chartFilePaths[control] = path;

            if (_mainWindow.RadioButton_HHMMSS.IsChecked ?? false)
                control.SetTimeAxisLabelHHMMSS();
            else
                control.SetTimeAxisLabelScriptTime();

            RefleshCharts();
        }

        public void ReloadChart(ChartControl control)
        {
            if (_chartFilePaths.TryGetValue(control, out var path))
            {
                var script = ScriptUtil.LoadScript(path);
                if (script != null)
                {
                    InitializeChartControlWithScript(control, script);
                    RefleshCharts();
                }
            }
        }

        private void InitializeChartControlWithScript(ChartControl control, IScript script)
        {
            string fileName = "";
            double plotMin = 0, plotMax = 100;
            string trackerFormat = "";
            System.Collections.IEnumerable? itemsSource = null;
            System.Collections.IEnumerable? itemsSource2 = null;
            Func<double, string>? scriptTimeFormatter = null;
            IEnumerable<(double start, double end)>? differenceRanges = null;

            fileName = script.FileName;
            plotMin = script.PlotMin;
            plotMax = script.PlotMax;
            trackerFormat = script.TrackerFormatString;
            itemsSource = script.ToPlot();
            scriptTimeFormatter = script.LabelFormatter_ScriptTime;
            
            if (script is UFOTW u)
            {
                itemsSource2 = u.ToPlotRight();
                differenceRanges = u.DetectDeference();
            }

            control.InitializeChart(
                fileName,
                plotMin, plotMax,
                trackerFormat,
                itemsSource,
                itemsSource2,
                scriptTimeFormatter,
                differenceRanges
            );
        }

        private void UpdateMediaElapsedLabel(double milliseconds)
        {
            var elapsed = TimeSpan.FromMilliseconds(milliseconds).ToString(@"hh\:mm\:ss");
            _mainWindow.MediaElapsedLabel.Content = elapsed + " / " + _mediaDuration;
        }

        public void LoadMedia(string path)
        {
            _mainWindow.MediaElem.ScrubbingEnabled = false;
            var uri = new Uri(path);
            Debug.WriteLine("try load: " + uri.ToString());
            _mainWindow.MediaElem.Source = uri;
            _mainWindow.MediaElem.Play();
            _mainWindow.MediaElem.Stop();
        }

        public void OnOpenButtonClicked()
        {
            StopMedia();

            OpenFileDialog dlg = new() { Filter = Util.FileDialogFilter };
            bool? result = dlg.ShowDialog();
            if (result == true)
                OpenFile(dlg.FileName);
        }

        public void OnRadioButtonHHMMSSChecked()
        {
            foreach (var c in _chartControls)
                c.SetTimeAxisLabelHHMMSS();
        }

        public void OnRadioButtonInternalTimeChecked()
        {
            foreach (var c in _chartControls)
                c.SetTimeAxisLabelScriptTime();
        }

        private void AnnotationsSyncTimer_Tick(object? sender, EventArgs e)
        {
            if (!IsUserDragging)
                MovePlayingAnnotations(_mainWindow.MediaElem.Position.TotalMilliseconds);
        }

        private void SeekbarTimer_Tick(object? sender, EventArgs e)
        {
            if ((_mainWindow.MediaElem.Source != null) && _mainWindow.MediaElem.NaturalDuration.HasTimeSpan && !IsUserDragging)
            {
                double position = _mainWindow.MediaElem.Position.TotalMilliseconds;
                _mainWindow.MediaSeekbarSlider.Value = position;
                UpdateMediaElapsedLabel(position);
            }
        }

        public void PlayMedia()
        {
            _mainWindow.MediaElem.Play();
            _annotationsSyncTimer.Start();
            _seekbarTimer.Start();
        }

        public void PauseMedia()
        {
            _mainWindow.MediaElem.Pause();
            _annotationsSyncTimer.Stop();
            _seekbarTimer.Stop();
        }

        public void StopMedia()
        {
            _mainWindow.MediaElem.Stop();
            _annotationsSyncTimer.Stop();
            _seekbarTimer.Stop();
        }

        public void OnMediaProgressDragStarted()
        {
            IsUserDragging = true;
        }

        public void OnMediaProgressDragCompleted()
        {
            IsUserDragging = false;
            _mainWindow.MediaElem.Position = TimeSpan.FromMilliseconds(_mainWindow.MediaSeekbarSlider.Value);
            MovePlayingAnnotations(_mainWindow.MediaElem.Position.TotalMilliseconds);
        }

        public void OnMediaProgressValueChanged(double milliseconds)
        {
            UpdateMediaElapsedLabel(milliseconds);
            MovePlayingAnnotations(milliseconds);
        }

        public void OnVolumeSliderValueChanged(double volume)
        {
            if (_mainWindow.MediaElem != null)
                _mainWindow.MediaElem.Volume = volume;
        }

        public void OnMouseWheel(double delta)
        {
            _mainWindow.MediaElem.Volume += (delta > 0) ? 0.1 : -0.1;
            _mainWindow.VolumeSlider.Value = _mainWindow.MediaElem.Volume;
        }

        public void OnMediaOpened()
        {
            _mainWindow.EnablePlayerElements();
            _mediaDuration = _mainWindow.MediaElem.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
            _mainWindow.MediaSeekbarSlider.Minimum = 0;
            _mainWindow.MediaSeekbarSlider.Maximum = _mainWindow.MediaElem.NaturalDuration.TimeSpan.TotalMilliseconds;
            _mainWindow.MediaSeekbarSlider.Value = 0;
            UpdateMediaElapsedLabel(0);

            if (_mainWindow.MediaElem.NaturalVideoHeight > 0)
            {
                Debug.WriteLine("video");
                _mainWindow.MediaElem.ScrubbingEnabled = true;
                Task.Run(() =>
                {
                    while (_mainWindow.MediaElem.ActualHeight == 0)
                        Thread.Sleep(5);

                    _mainWindow.Dispatcher.Invoke(() =>
                    {
                        _mainWindow.Height += (int)_mainWindow.MediaElem.ActualHeight;
                        _mainWindow.Resizer.Visibility = Visibility.Visible;
                    });
                });
            }
            else
                _mainWindow.Resizer.Visibility = Visibility.Hidden;
        }

        public void OnMediaFailed()
        {
            Util.ShowMessageBoxTopMost("メディアの読み込みに失敗しました");
        }

        public void OnPlaybackSpeedValueChanged(double ratio)
        {
            if (_mainWindow.MediaElem is not null)
                _mainWindow.MediaElem.SpeedRatio = ratio;
        }

        public void OnSpeed1xButtonClicked()
        {
            _mainWindow.MediaElem.SpeedRatio = 1;
            _mainWindow.PlaybackSpeedSlider.Value = 10;
        }
    }
}
