using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SexToyScriptViewer.Script
{
    partial class CoyoteScript : IScript
    {
        public int PlotMax { get { return 100; } }
        public int PlotMin { get { return 0; } }
        public required string FileName { get; init; } = "";
        public required string FilePath { get; init; }
        public string TrackerFormatString { get { return "{1}: {HHMMSS} ({ScriptTime})\n{3}: {4}"; } }

        // Dataに不適正な内容を直接加えることを防ぐため、隠蔽してメソッドで操作を提供する
        private List<ScriptLine> _scriptData = new() { };

        public static int Validate(string script_str)
        {
            Regex r = ValidatorRegex();

            var lines = ScriptUtil.RawCsvToLines(script_str);
            int result = 0;
            int max = lines.Count < 20 ? lines.Count : 20;
            foreach (var line in lines.GetRange(0, max))
                if (r.IsMatch(line))
                    result++;

            return result;
        }

        public int MillisecondsToInternalTime(double milliseconds) => (int)milliseconds;

        public string LabelFormatter_ScriptTime(double milliseconds) => milliseconds.ToString();

        public static string? Inspect(string csv_str)
        {
            var lines = ScriptUtil.RawCsvToLines(csv_str);

            StringBuilder result = new();

            var emptyline = new Regex(@"^$");
            //int prevtime = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                if (emptyline.IsMatch(lines[i]))
                    result.AppendLine($"{i + 1}行目 空行です！");

                if (!ValidatorRegex().IsMatch(lines[i]))
                    result.AppendLine($"{i + 1}行目 構文エラー: {lines[i]}");
            }

            if (result.Length > 0)
                return result.ToString();
            else
                return null;
        }

        public static CoyoteScript? LoadScript(string path)
        {
            using var f = new StreamReader(path);
            var csv_str = f.ReadToEnd();

            var lines = ScriptUtil.RawCsvToLines(csv_str);

            List<ScriptLine> result = new();

            foreach (var line in lines)
            {
                if(!ValidatorRegex().IsMatch(line))
                    return null;

                var splitted = line.Split(',');
                result.Add(new ScriptLine()
                {
                    InternalTime = int.Parse(splitted[0]),
                    Frequency = int.Parse(splitted[1]),
                    Strength = int.Parse(splitted[2])
                });
            }

            return new CoyoteScript()
            {
                _scriptData = result,
                FileName = Path.GetFileName(path),
                FilePath = path
            };
        }

        public IDataPointProvider[] ToPlot()
        {
            List<CustomDataPoint> result = new() { new CustomDataPoint(0, 0) };

            int prevPower = 0;

            foreach (var line in _scriptData)
            {
                result.Add(new CustomDataPoint(line.Milliseconds, prevPower));
                result.Add(new CustomDataPoint(line.Milliseconds, line.Strength));
                prevPower = line.Strength;
            }

            return result.ToArray();
        }

        /// <summary>
        /// csvの行のデータを保持する構造体
        /// </summary>
        public struct ScriptLine
        {
            /// <summary>ミリ秒単位</summary>
            public int InternalTime;
            /// <summary>0～100まで</summary>
            public int Frequency;
            /// <summary>0～100まで</summary>
            public int Strength;
            public readonly double Milliseconds { get => InternalTime; }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append($"InternalTime:{InternalTime}, ");
                builder.Append($"Frequency:{Frequency}, ");
                builder.Append($"Strength:{Strength}");
                return builder.ToString();
            }
        }

        public class CustomDataPoint : IDataPointProvider, IScriptDataPoint
        {
            public double X { get; }
            public double Y { get; }
            public int Frequency { get; }
            public string HHMMSS { get; }
            public string ScriptTime { get; }
            public DataPoint GetDataPoint()
            {
                return new DataPoint(X, Y);
            }

            public CustomDataPoint(double x, double y)
            {
                X = x;
                Y = y;
                HHMMSS = ScriptUtil.MillisecondsToHHMMSS(x);
                ScriptTime = x.ToString();
            }
        }

        [GeneratedRegex("^([0-9]+),(100|[0-9]{1,2}),(100|[0-9]{1,2})$")]
        private static partial Regex ValidatorRegex();
    }
}
