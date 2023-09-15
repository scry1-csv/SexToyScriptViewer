using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private (List<ScriptLine> left, List<ScriptLine> right) _scriptData = (new(), new());

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

        public static UFOTW? LoadScript(string path)
        {
            using var f = new StreamReader(path);
            
            var csv_str = f.ReadToEnd();


            var lines = ScriptUtil.RawCsvToLines(csv_str);

            (List<ScriptLine> left, List<ScriptLine> right) result = (new(), new());

            ScriptLine prevLeft = new();
            ScriptLine prevRight = new();
            foreach (var line in lines)
            {
                if (!ValidatorRegex().IsMatch(line))
                    return null;
                var splitted = line.Split(',');

                int time = int.Parse(splitted[0]);
                bool direction1 = splitted[1] == "1";
                bool direction2 = splitted[3] == "1";

                ScriptLine left = new()
                {
                    InternalTime = time,
                    Direction = direction1,
                    Power = int.Parse(splitted[2])
                };

                ScriptLine right = new()
                {
                    InternalTime = time,
                    Direction = direction2,
                    Power = int.Parse(splitted[4])
                };

                if (left != prevLeft)
                    result.left.Add(left);
                prevLeft = left;
                if (right != prevRight)
                    result.right.Add(right);
                prevRight = right;
            }

            return new UFOTW()
            {
                _scriptData = result,
                FileName = Path.GetFileName(path),
            };
        }

        public IDataPointProvider[] ToPlot()
        {
            List<CustomDataPoint> result = new() { new CustomDataPoint(0, 0, 0) };

            int prevPower = 0;

            foreach (var line in _scriptData.left)
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

            foreach (var line in _scriptData.right)
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
                HHMMSS = ScriptUtil.MillisecondsToHHMMSS(x);
                ScriptTime = internalTime.ToString();
            }

            public DataPoint GetDataPoint() => new(X, Y);
        }

        [GeneratedRegex("^([0-9]+),([01]),(100|[0-9]{1,2}),([01]),(100|[0-9]{1,2})")]
        private static partial Regex ValidatorRegex();
    }
}
