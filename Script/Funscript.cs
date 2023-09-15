using OxyPlot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using System.Windows.Navigation;

namespace SexToyScriptViewer.Script
{

    class Funscript : IScript
    {
        public int PlotMax { get { return 100; } }
        public int PlotMin { get { return 0; } }
        public string FileName { get; init; }
        public string TrackerFormatString { get { return "{1}: {HHMMSS} ({ScriptTime})\n{3}: {4}\n移動時間: {Duration}"; } }

        private readonly FunscriptJson Data;

        public Funscript(FunscriptJson data, string filename)
        {
            Data = data;
            FileName = filename;
        }

        public static Funscript? LoadScript(string path) {
            var result = LoadJson(path);
            if(result is null) 
                return null;
            else
                return new Funscript(result, Path.GetFileName(path));            
        }

        public IDataPointProvider[] ToPlot()
        {
            List<CustomDataPoint> result = new() { new(0, Data.actions[0].pos, 0) };

            int prevtime = 0;
            foreach (var item in Data.actions)
            {
                int at = item.at;
                result.Add(new(at, item.pos, at - prevtime));
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


#pragma warning disable IDE1006 // 命名スタイル
#pragma warning disable CS8981 // 型名には、小文字の ASCII 文字のみが含まれています。このような名前は、プログラミング言語用に予約されている可能性があります。

    public class FunscriptJson
    {
        public string version { get; set; } = "";
        public bool inverted { get; set; }
        public int range { get; set; }
        public List<action> actions { get; set; } = new List<action>();

        public class action
        {
            public int pos { get; set; }
            public int at { get; set; }
            public override string ToString() => $"pos: {pos}, at: {at}";
        }
    }
#pragma warning restore IDE1006
#pragma warning restore CS8981 // 型名には、小文字の ASCII 文字のみが含まれています。このような名前は、プログラミング言語用に予約されている可能性があります。

}
