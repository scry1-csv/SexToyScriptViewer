using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows.Navigation;

namespace SexToyScriptViewer.Script
{

    class Funscript : IScript
    {
        public int PlotMax { get { return 100; } }
        public int PlotMin { get { return 0; } }
        public string FileName { get; init; }
        public string TrackerFormatString { get { return "{1}: {HHMMSS} ({ScriptTime})\n{3}: {4}\n移動時間: {Duration}"; } }

        public string FilePath { get; init; }

        private readonly FunscriptJson Data;

        public Funscript(FunscriptJson data, string filename, string filePath)
        {
            Data = data;
            FileName = filename;
            FilePath = filePath;
        }

        public static Funscript? LoadScript(string path) {
            var result = LoadJson(path);
            if(result is null) 
                return null;
            else
                return new Funscript(result, Path.GetFileName(path), path);            
        }

        public IDataPointProvider[] ToPlot()
        {
            List<CustomDataPoint> result = new() { new(0, Data.Actions[0].Pos, 0) };

            int prevtime = 0;
            foreach (var item in Data.Actions)
            {
                int at = item.At;
                result.Add(new(at, item.Pos, at - prevtime));
                prevtime = at;
            }
            return result.ToArray();
        }

        private static FunscriptJson? LoadJson(string path)
        {
            var jsonstr = File.ReadAllText(path);
            var options = new JsonSerializerOptions()
            {
                AllowTrailingCommas = true
            };
            var result = JsonSerializer.Deserialize<FunscriptJson>(jsonstr, options);
            return result;
        }


        public static int Validate(string script_str)
        {
            return 0;
        }

        public int MillisecondsToInternalTime(double milliseconds) => (int)milliseconds;

        public string LabelFormatter_ScriptTime(double milliseconds) => milliseconds.ToString();
        
        public string LabelFormatter_HHMMSS(double milliseconds) => ScriptUtil.MillisecondsToHHMMSS(milliseconds);

        /// <summary>
        /// csvの行のデータを保持する構造体
        /// </summary>

        public class CustomDataPoint : IDataPointProvider, IScriptDataPoint
        {
            public double X { get; }
            public double Y { get; }
            public string Duration { get; }
            public string HHMMSS { get; }
            public string ScriptTime { get; }

            public CustomDataPoint(double x, double y, int duration)
            {
                X = x;
                Y = y;
                Duration = $"{duration}ms";
                HHMMSS = ScriptUtil.MillisecondsToHHMMSS(x);
                ScriptTime = x.ToString();
            }

            public DataPoint GetDataPoint()
            {
                return new DataPoint(X, Y);
            }
        }
    }

    public class FunscriptJson
    {
        [JsonPropertyName("version")]
        public string Version { get; set; } = "";
        [JsonPropertyName("inverted")]
        public bool Inverted { get; set; }
        [JsonPropertyName("range")]
        public int Range { get; set; }
        [JsonPropertyName("actions")]
        public List<Action> Actions { get; set; } = new List<Action>();

        public class Action
        {
            [JsonPropertyName("pos")]
            public int Pos { get; set; }
            [JsonPropertyName("at")]
            public int At { get; set; }
            public override string ToString() => $"pos: {Pos}, at: {At}";
        }
    }
}
