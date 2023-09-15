using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Collections.Immutable;

namespace SexToyScriptViewer
{
    public static class Util
    {
        public static readonly ImmutableArray<string> MediaExts = ImmutableArray.Create(".mp3", ".m4a", ".wav", ".mp4", ".webm", ".mpg");
        public static readonly ImmutableArray<string> ScriptExts = ImmutableArray.Create(".csv", ".funscript");
        public static readonly ImmutableArray<string> Exts = ImmutableArray.Create(MediaExts.Concat(ScriptExts).ToArray());

        public static string FileDialogFilter { get
            {
                //"スクリプト,音声,動画 (*.csv;*.funscript;*.mp3;*.m4a;*.wav;*.mp4;*.webm;*.mpg)|*.csv;*.funscript;*.mp3;*.m4a;*.wav;*.mp4;*.webm;*.mpg"

                string joined = string.Join(";", Exts.Select((s)=>"*"+s));
                return $"スクリプト,音声,動画 ({joined})|{joined}";
            } }

        public static void ShowMessageBoxTopMost(string message)
        {
            MessageBox.Show(message, "エラー", MessageBoxButton.OK, MessageBoxImage.Exclamation, MessageBoxResult.OK, MessageBoxOptions.DefaultDesktopOnly);
        }
    }
}
