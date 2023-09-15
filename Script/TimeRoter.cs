using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SexToyScriptViewer.Script
{
    partial class TimeRoter : IScript
    {
        public int PlotMax { get { return 1000; } }
        public int PlotMin { get { return 0; } }
        public string FileName { get; init; } = "";
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

        public int MillisecondsToInternalTime(double milliseconds)
        {
            return Convert.ToInt32(milliseconds / 10);
        }

        public string LabelFormatter_ScriptTime(double milliseconds) => ((decimal)milliseconds / 10).ToString();

        public static string? Inspect(string csv_str)
        {
            var lines = ScriptUtil.RawCsvToLines(csv_str);

            StringBuilder result = new();

            var emptyline = new Regex(@"^$");
            var syntax = new Regex(@"^[0-9]+,[01],(100|[0-9]{1,2})$");
            //int prevtime = -1;

            for (int i = 0; i < lines.Count; i++)
            {
                if (emptyline.IsMatch(lines[i]))
                    result.AppendLine($"{i + 1}行目 空行です！");

                if (!syntax.IsMatch(lines[i]))
                    result.AppendLine($"{i + 1}行目 構文エラー: {lines[i]}");
            }

            if (result.Length > 0)
                return result.ToString();
            else
                return null;
        }

        public static List<ScriptLine> ParseCSV(string csv_str)
        {
            var lines = ScriptUtil.RawCsvToLines(csv_str);

            List<ScriptLine> result = new();

            foreach (var line in lines)
            {
                var splitted = line.Split(',');

                var d = decimal.Parse(splitted[0]);
                int time = decimal.ToInt32(d * 100);

                result.Add(new ScriptLine()
                {
                    InternalTime = time,
                    Power = int.Parse(splitted[1])
                });
            }

            return result;
        }

        public static TimeRoter? LoadScript(string path)
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
                var d = decimal.Parse(splitted[0]);
                int time = decimal.ToInt32(d * 100);

                result.Add(new ScriptLine()
                {
                    InternalTime = time,
                    Power = int.Parse(splitted[1])
                });
            }

            return new TimeRoter()
            {
                _scriptData = result,
                FileName = Path.GetFileName(path),
            };
        }

        public IDataPointProvider[] ToPlot()
        {
            List<CustomDataPoint> result = new() { new CustomDataPoint(0, 0) };

            int prevPower = 0;

            foreach (var line in _scriptData)
            {
                result.Add(new CustomDataPoint(line.Milliseconds, prevPower));
                result.Add(new CustomDataPoint(line.Milliseconds, line.Power));
                prevPower = line.Power;
            }

            return result.ToArray();
        }

        /// <summary>
        /// csvの行のデータを保持する構造体
        /// </summary>
        public struct ScriptLine
        {
            /// <summary>1/100秒単位</summary>
            public int InternalTime;
            /// <summary>0～1000まで</summary>
            public int Power;
            public readonly double Milliseconds { get => (double)InternalTime * 10; }

            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append($"InternalTime:{InternalTime}, ");
                builder.Append($"Power:{Power}");
                return builder.ToString();
            }
        }

        public class CustomDataPoint : IDataPointProvider, IScriptDataPoint
        {
            public double X { get; }
            public double Y { get; }
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
                ScriptTime = $"{x / 1000:F2}";
            }
        }

        [GeneratedRegex("^([0-9]+)(\\.[0-9]{1,2})?,(1000|[0-9]{1,3})$")]
        private static partial Regex ValidatorRegex();
    }
}
