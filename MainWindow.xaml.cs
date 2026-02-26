using Microsoft.Win32;
using SexToyScriptViewer.Control;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace SexToyScriptViewer
{
    public partial class MainWindow : Window
    {
        private readonly Controller _controller;

        public MainWindow()
        {
            InitializeComponent();
            RadioButton_HHMMSS.IsChecked = true;

            _controller = new Controller(this);
        }

        internal void EnablePlayerElements()
        {
            PlayButton.IsEnabled = true;
            PauseButton.IsEnabled = true;
            VolumeSlider.IsEnabled = true;
            VolumeLabel.IsEnabled = true;
            MediaSeekbarSlider.IsEnabled = true;
        }

        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            _controller.OnOpenButtonClicked();
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            // ドラッグされているのがファイルなら許容、それ以外なら不許可
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
                e.Effects = DragDropEffects.Copy;
            else
                e.Effects = DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetData(DataFormats.FileDrop) is string[] dropped)
                _controller.OnFileDropped(dropped);
        }

        private void RadioButton_HHMMSS_Checked(object sender, RoutedEventArgs e)
        {
            _controller?.OnRadioButtonHHMMSSChecked();
        }

        private void RadioButton_InternalTime_Checked(object sender, RoutedEventArgs e)
        {
            _controller?.OnRadioButtonInternalTimeChecked();
        }

        private void Play_Click(object sender, EventArgs e)
        {
            _controller.PlayMedia();
        }

        private void Pause_Click(object sender, EventArgs e)
        {
            _controller.PauseMedia();
        }

        private void Stop_Click(object sender, EventArgs e)
        {
            _controller.StopMedia();
        }

        private void MediaProgressSlider_DragStarted(object sender, DragStartedEventArgs e)
        {
            _controller.OnMediaProgressDragStarted();
        }

        private void MediaProgressSlider_DragCompleted(object sender, DragCompletedEventArgs e)
        {
            _controller.OnMediaProgressDragCompleted();
        }

        private void MediaProgressSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _controller?.OnMediaProgressValueChanged(MediaSeekbarSlider.Value);
        }

        private void Grid_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            _controller.OnMouseWheel(e.Delta);
        }

        private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_controller != null && MediaElem != null && VolumeSlider.IsLoaded)
                _controller.OnVolumeSliderValueChanged(VolumeSlider.Value);
        }

        private void MediaElem_MediaOpened(object sender, RoutedEventArgs e)
        {
            _controller.OnMediaOpened();
        }

        private void MediaElem_MediaFailed(object sender, ExceptionRoutedEventArgs e)
        {
            _controller.OnMediaFailed();
        }

        private void PlaybackSpeedSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            _controller?.OnPlaybackSpeedValueChanged(PlaybackSpeedSlider.Value / 10);
        }

        private void Speed1xButton_Click(object sender, RoutedEventArgs e)
        {
            _controller.OnSpeed1xButtonClicked();
        }
    }
}
