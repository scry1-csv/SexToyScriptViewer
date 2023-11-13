using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace SexToyScriptViewer.Script
{
    public partial class UFOTW : IScript
    {
        public int PlotMax { get { return 100; } }
        public int PlotMin { get { return -100; } }
        public string FileName { get; init; } = "";
        public string TrackerFormatString { get { return "{1}: {HHMMSS} ({ScriptTime})\n{3}: {4}"; } }


        // Dataに不適正な内容を直接加えることを防ぐため、隠蔽してメソッドで操作を提供する
        //private (List<SeparatedScriptLine> left, List<SeparatedScriptLine> right) _separatedScriptData = (new(), new());
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

        public List<(double start, double end)> DetectDeference()
        {
            List<(double start, double end)> result = new();
            bool prevWasDiffernt = false;
            double tmp_start = 0, tmp_end = 0;
            foreach (var l in _scriptData)
            {
                if (l.LeftDirection != l.RightDirection | l.LeftPower != l.RightPower)
                {
                    if (!prevWasDiffernt)
                    {
                        prevWasDiffernt = true;
                        tmp_start = l.Milliseconds;
                    }
                }
                else
                {
                    if (prevWasDiffernt)
                    {
                        prevWasDiffernt = false;
                        tmp_end = l.Milliseconds;
                        result.Add((tmp_start, tmp_end));
                    }
                }
            }
            return result;
        }

        public static UFOTW? LoadScript(string path)
        {
            using var f = new StreamReader(path);

            var csv_str = f.ReadToEnd();


            var lines = ScriptUtil.RawCsvToLines(csv_str);

            List<ScriptLine> script = new();

            foreach (var line in lines)
            {
                if (!ValidatorRegex().IsMatch(line))
                    return null;
                var splitted = line.Split(',');

                int time = int.Parse(splitted[0]);
                bool leftDirection = splitted[1] == "1";
                bool rightDirection = splitted[3] == "1";
                int leftPower = int.Parse(splitted[2]);
                int rightPower = int.Parse(splitted[4]);

                script.Add(new ScriptLine()
                {
                    InternalTime = time,
                    LeftDirection = leftDirection,
                    LeftPower = leftPower,
                    RightDirection = rightDirection,
                    RightPower = rightPower
                });
            }

            return new UFOTW()
            {
                _scriptData = script,
                FileName = Path.GetFileName(path),
            };
        }

        public IDataPointProvider[] ToPlot()
        {
            List<CustomDataPoint> result = new() { new CustomDataPoint(0, 0, 0) };

            bool prevDirection = true;
            int prevPower = 0;

            foreach (var line in _scriptData)
            {
                if (line.LeftDirection != prevDirection || line.LeftPower != prevPower)
                {
                    int power;
                    if (line.LeftDirection == true)
                        power = line.LeftPower;
                    else
                        power = -line.LeftPower;

                    result.Add(new CustomDataPoint(line.Milliseconds, prevPower, line.InternalTime));
                    result.Add(new CustomDataPoint(line.Milliseconds, power, line.InternalTime));
                    prevDirection = line.LeftDirection;
                    prevPower = line.LeftPower;
                }
            }

            return result.ToArray();
        }


        public IDataPointProvider[] ToPlotRight()
        {
            List<CustomDataPoint> result = new() { new CustomDataPoint(0, 0, 0) };

            bool prevDirection = true;
            int prevPower = 0;

            foreach (var line in _scriptData)
            {
                if (line.RightDirection != prevDirection || line.RightPower != prevPower)
                {
                    int power;
                    if (line.RightDirection == true)
                        power = line.RightPower;
                    else
                        power = -line.RightPower;

                    result.Add(new CustomDataPoint(line.Milliseconds, prevPower, line.InternalTime));
                    result.Add(new CustomDataPoint(line.Milliseconds, power, line.InternalTime));
                    prevDirection = line.RightDirection;
                    prevPower = line.RightPower;
                }
            }

            return result.ToArray();
        }

        public record ScriptLine
        {
            public int InternalTime;
            public bool LeftDirection;
            public int LeftPower;
            public bool RightDirection;
            public int RightPower;
            public double Milliseconds { get => (double)InternalTime * 100; }
            public override string ToString()
            {
                var builder = new StringBuilder();
                builder.Append($"InternalTime:{InternalTime}, ");
                builder.Append($"LeftDirection:{LeftDirection}, ");
                builder.Append($"LeftPower:{LeftPower}");
                builder.Append($"RightDirection:{RightDirection}, ");
                builder.Append($"RightPower:{RightPower}");
                return builder.ToString();
            }
        }

        /// <summary>
        /// csvの行のデータを保持する構造体
        /// </summary>
        public record SeparatedScriptLine
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
                HHMMSS = ScriptUtil.MillisecondsToHHMMSS(x);
                ScriptTime = internalTime.ToString();
            }

            public DataPoint GetDataPoint() => new(X, Y);
        }

        [GeneratedRegex("^([0-9]+),([01]),(100|[0-9]{1,2}),([01]),(100|[0-9]{1,2})")]
        private static partial Regex ValidatorRegex();
    }
}
