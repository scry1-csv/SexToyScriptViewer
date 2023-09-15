using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace SexToyScriptViewer.Script
{
    partial class Vorze_SA : IScript
    {
        public int PlotMax { get { return 100; } }
        public int PlotMin { get { return -100; } }
        public string FileName { get; init; } = "";
        public string TrackerFormatString { get { return "{1}: {HHMMSS} ({ScriptTime})\n{3}: {4}"; } }

        // Dataに不適正な内容を直接加えることを防ぐため、隠蔽してメソッドで操作を提供する
        private List<ScriptLine> _scriptData = new();

        public static int Validate(string csv_str)
        {
            Regex r = ValidatorRegex();

            var lines = ScriptUtil.RawCsvToLines(csv_str);
            int result = 0;
            foreach (var line in lines)
                if (r.IsMatch(line))
                    result++;

            return result;
        }

        public int MillisecondsToInternalTime(double milliseconds)
        {
            return Convert.ToInt32(milliseconds / 100);
        }

        public string LabelFormatter_ScriptTime(double milliseconds) => (milliseconds / 100).ToString();


        public static List<ScriptLine> ParseCSV(string csv_str)
        {
            var lines = ScriptUtil.RawCsvToLines(csv_str);

            List<ScriptLine> result = new();

            foreach (var line in lines)
            {
                var splitted = line.Split(',');

                int time = int.Parse(splitted[0]);

                result.Add(new()
                {
                    InternalTime = time,
                    Direction = splitted[1] == "1",
                    Power = int.Parse(splitted[2])
                });
            }
            return result;
        }

        public static Vorze_SA? LoadScript(string path)
        {
            using var f = new StreamReader(path);
            var lines = ScriptUtil.RawCsvToLines(f.ReadToEnd());
            List<ScriptLine> result = new();

            foreach (var line in lines)
            {
                if (!ValidatorRegex().IsMatch(line))
                    return null;

                var splitted = line.Split(',');

                int time = int.Parse(splitted[0]);

                result.Add(new()
                {
                    InternalTime = time,
                    Direction = splitted[1] == "1",
                    Power = int.Parse(splitted[2])
                });
            }
            return new Vorze_SA()
            {
                _scriptData = result,
                FileName = Path.GetFileName(path),
            };
            
        }

        public IDataPointProvider[] ToPlot()
        {
            List<CustomDataPoint> result = new() { new CustomDataPoint(0, 0, 0) };

            int prevPower = 0;

            foreach (var line in _scriptData)
            {
                int power;
                if (line.Direction == true)
                    power = line.Power;
                else
                    power = -line.Power;

                result.Add(new CustomDataPoint(line.Milliseconds, prevPower, line.InternalTime));
                result.Add(new CustomDataPoint(line.Milliseconds, power, line.InternalTime));
                prevPower = power;
            }

            return result.ToArray();
        }


        public IDataPointProvider[] ToPlotRight()
        {
            List<CustomDataPoint> result = new() { new CustomDataPoint(0, 0, 0) };

            int prevPower = 0;

            foreach (var line in _scriptData)
            {
                int power;
                if (line.Direction == true)
                    power = line.Power;
                else
                    power = -line.Power;

                result.Add(new CustomDataPoint(line.Milliseconds, prevPower, line.InternalTime));
                result.Add(new CustomDataPoint(line.Milliseconds, power, line.InternalTime));
                prevPower = power;
            }

            return result.ToArray();
        }


        /// <summary>
        /// csvの行のデータを保持する構造体
        /// </summary>
        public record ScriptLine
        {
            public int InternalTime;
            public bool Direction;
            public int Power;
            public double Milliseconds { get => (double)InternalTime * 100; }
            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append($"InternalTime:{InternalTime}, ");
                builder.Append($"Direction:{Direction}, ");
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

            public CustomDataPoint(double x, double y, int internalTime)
            {
                X = x;
                Y = y;
                HHMMSS = MillisecondsToHHMMSS(x);
                ScriptTime = internalTime.ToString();
            }

            private static string MillisecondsToHHMMSS(double milliseconds) =>
                ScriptUtil.TimeSpanToHHMMSS(new TimeSpan(0, 0, 0, 0, (int)milliseconds));

            public DataPoint GetDataPoint() => new(X, Y);
        }

        [GeneratedRegex("^([0-9]+),([01]),(100|[0-9]{1,2})")]
        private static partial Regex ValidatorRegex();
    }
}
