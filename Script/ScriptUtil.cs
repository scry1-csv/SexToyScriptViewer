using OxyPlot;
using SexToyScriptViewer.Control;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace SexToyScriptViewer.Script
{
    internal static partial class ScriptUtil
    {
        public static IScript? LoadScript(string path)
        {
            try
            {
                IScript? result;
                if (Path.GetExtension(path) == ".funscript")
                    result = Funscript.LoadScript(path);
                else
                {
                    result = UFOTW.LoadScript(path);
                    result ??= Vorze_SA.LoadScript(path);
                    result ??= TimeRoter.LoadScript(path);
                }
                return result;
            }
            catch (Exception e)
            {
#if DEBUG
                Debug.WriteLine(e);
                throw;
#else
                MessageBox.Show("ファイル読み込み時に例外が発生しました：" + e);
                return null;
#endif
            }
        }

        public static string MillisecondsToHHMMSS(double milliseconds) => TimeSpanToHHMMSS(new TimeSpan(0, 0, 0, 0, (int)milliseconds));
        public static string TimeSpanToHHMMSS(TimeSpan time)
        {
            string h = time.Hours == 0 ? "" : $"{time.Hours}時間";
            string m = time.Minutes == 0 ? "" : $"{time.Minutes}分";
            string s = $"{time.Seconds}秒";
            string milli = time.Milliseconds == 0 ? "" : $"{time.Milliseconds:000}ms";

            return $"{h}{m}{s}{milli}";
        }

        public static List<string> RawCsvToLines(string csv_str)
        {
            var tmp = _newLine().Replace(csv_str, "\n");
            List<string> lines = new(tmp.Split('\n'));
            if (lines.Last() == "")
                lines.RemoveAt(lines.Count - 1);

            return lines;
        }

        [GeneratedRegex("\r\n|\r")]
        private static partial Regex _newLine();
    }
}
