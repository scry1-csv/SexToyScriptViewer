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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace SexToyScriptViewer
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _annotationsSyncTimer;
        private readonly DispatcherTimer _seekbarTimer;
        private readonly MainViewModel _viewModel;
        private string _mediaDuration = "";
        public bool IsUserDragging = false;

        private readonly List<ChartControl> _chartControls = new();
        
        public MainWindow()
        {
            InitializeComponent();
            RadioButton_HHMMSS.IsChecked = true;

            _annotationsSyncTimer = new() { Interval = TimeSpan.FromMilliseconds(10) };
            _annotationsSyncTimer.Tick += AnnotationsSyncTimer_Tick;

            _seekbarTimer = new() { Interval = TimeSpan.FromSeconds(1) };
            _seekbarTimer.Tick += SeekbarTimer_Tick;

            DataContext = _viewModel = new MainViewModel();
        }
        public void CloseChart(ChartControl c)
        {
            _chartControls.Remove(c);
            RefleshCharts();
        }

        public void SyncChartsRange(ChartControl sender)
        {
            var min = sender.TimeAxis.InternalAxis.ActualMinimum;
            var max = sender.TimeAxis.InternalAxis.ActualMaximum;
            foreach (var item in _chartControls)
                if (item != sender)
                    item.ZoomTimeAxis(min, max);
        }

        private void MovePlayingAnnotations(double milliseconds)
        {
            foreach (var c in _chartControls)
                c.MovePlayingAnnotation(milliseconds);
        }

        private void RefleshCharts()
        {
            ChartsPanel.Children.Clear();
            ChartsPanel.RowDefinitions.Clear();

            int i = 0;
            foreach (var c in _chartControls)
            {
                RowDefinition row = new();
                if(c.IsUFOTW) row.Height = new GridLength(1.55, GridUnitType.Star);
                ChartsPanel.RowDefinitions.Add(row);

                Grid.SetRow(_chartControls[i], i);
                ChartsPanel.Children.Add(_chartControls[i]);
                i++;
            }

            if(_chartControls.Count > 2)
                SyncChartsRange(_chartControls[0]);
        }


        private void OpenFile(string path)
        {
            switch (Path.GetExtension(path))
            {
                case ".csv":
                case ".funscript":
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

        private void OpenScript(string path)
        {
            var script = ScriptUtil.LoadScript(path);

            if (script == null)
                Util.ShowMessageBoxTopMost("スクリプトの読み込みに失敗しました。");
            else
            {
                ChartControl control;
                if(script is UFOTW ufotw)
                    control = new ChartControl(this, ufotw);
                else   
                    control = new ChartControl(this, script);

                _chartControls.Add(control);
                if (RadioButton_HHMMSS.IsChecked ?? false)
                    control.SetTimeAxisLabelHHMMSS();
                else
                    control.SetTimeAxisLabelScriptTime();
                RefleshCharts();
            }
        }

        private void EnablePlayerElements()
        {
            PlayButton.IsEnabled = true;
            PauseButton.IsEnabled = true;
            StopButton.IsEnabled = true;
            VolumeSlider.IsEnabled = true;
            VolumeLabel.IsEnabled = true;
            MediaSeekbarSlider.IsEnabled = true;
        }

        private void UpdateMediaElapsedLabel(double milliseconds)
        {
            var elapsed = TimeSpan.FromMilliseconds(milliseconds).ToString(@"hh\:mm\:ss");
            MediaElapsedLabel.Content = elapsed + " / " + _mediaDuration;
        }


        /// <summary>
        /// 開くボタンが押されたときのイベント
        /// </summary>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            MediaElem.Stop();
            _annotationsSyncTimer.Stop();
            _seekbarTimer.Stop();

            OpenFileDialog dlg = new() { Filter = Util.FileDialogFilter };

            bool? result = dlg.ShowDialog();

            if (result == true)
                OpenFile(dlg.FileName);
        }


        /// <summary>
        /// 何かをドラッグしてマウスオーバーしたときのイベント
        /// </summary>
        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            // ドラッグされているのがファイルなら許容、それ以外なら不許可
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        /// <summary>
        /// 何かがドロップされたときのイベント
        /// </summary>
        private void Window_Drop(object sender, DragEventArgs e)
        {
            string[]? dropped = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (dropped is null)
                throw new InvalidDataException();

            if (dropped.Length == 1)
                OpenFile(dropped[0]);
            else
                Util.ShowMessageBoxTopMost("開けるのは同時に一つのファイルだけです！");
        }


        /// <summary>時分秒表示ラジオボタンが押されたときのイベント</summary>
        private void RadioButton_HHMMSS_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var c in _chartControls)
                c.SetTimeAxisLabelHHMMSS();
        }

        /// <summary>内部値表示ラジオボタンが押されたときのイベント</summary>
        private void RadioButton_InternalTime_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var c in _chartControls)
                c.SetTimeAxisLabelScriptTime();
        }

        private void AnnotationsSyncTimer_Tick(object? sender, EventArgs e)
        {
            if (!IsUserDragging)
                MovePlayingAnnotations(MediaElem.Position.TotalMilliseconds);
        }

        private void SeekbarTimer_Tick(object? sender, EventArgs e)
        {
            if ((MediaElem.Source != null) && MediaElem.NaturalDuration.HasTimeSpan && !IsUserDragging)
            {
                double position = MediaElem.Position.TotalMilliseconds;
                MediaSeekbarSlider.Value = position;
                UpdateMediaElapsedLabel(position);
            }
        }

        private void LoadMedia(string path)
        {
            MediaElem.ScrubbingEnabled = false;
            var uri = new Uri(path);
            Debug.WriteLine("try load: " + uri.ToString());
            MediaElem.Source = uri;
            MediaElem.Play();
            MediaElem.Stop();
        }

        private void Play_Click(object sender, EventArgs e)
        {
            MediaElem.Play();
            _annotationsSyncTimer.Start();
            _seekbarTimer.Start();
        }

        private void Pause_Click(object sender, EventArgs e)
        {
            MediaElem.Pause();
            _annotationsSyncTimer.Stop();
            _seekbarTimer.Stop();
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            MediaElem.Stop();
            _annotationsSyncTimer.Stop();
            _seekbarTimer.Stop();
        }

        private void MediaProgressSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            IsUserDragging = true;
        }

        private void MediaProgressSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            IsUserDragging = false;
            MediaElem.Position = TimeSpan.FromMilliseconds(MediaSeekbarSlider.Value);
            MovePlayingAnnotations(MediaElem.Position.TotalMilliseconds);
        }

        private void MediaProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            var milliseconds = MediaSeekbarSlider.Value;
            UpdateMediaElapsedLabel(milliseconds);
            MovePlayingAnnotations(milliseconds);
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            MediaElem.Volume += (e.Delta > 0) ? 0.1 : -0.1;
        }

        private void MediaElem_MediaOpened(object sender, RoutedEventArgs e)
        {
            EnablePlayerElements();
            _mediaDuration = MediaElem.NaturalDuration.TimeSpan.ToString(@"hh\:mm\:ss");
            MediaSeekbarSlider.Minimum = 0;
            MediaSeekbarSlider.Maximum = MediaElem.NaturalDuration.TimeSpan.TotalMilliseconds;
            MediaSeekbarSlider.Value = 0;
            UpdateMediaElapsedLabel(0);

            if (MediaElem.NaturalVideoHeight > 0)
            {
                Debug.WriteLine("video");
                MediaElem.ScrubbingEnabled = true;
                Task.Run(() =>
                {
                    while (MediaElem.ActualHeight == 0)
                        Thread.Sleep(5);
                    _viewModel.Height += (int)MediaElem.ActualHeight;

                    Dispatcher.Invoke(() =>
                    {
                        Resizer.Visibility = Visibility.Visible;
                    });
                });
            }
            else
                Resizer.Visibility = Visibility.Hidden;
        }

        private void MediaElem_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            Util.ShowMessageBoxTopMost("メディアの読み込みに失敗しました");
        }
    }
}
