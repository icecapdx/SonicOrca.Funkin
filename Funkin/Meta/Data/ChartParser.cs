using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace SonicOrca.Funkin.Meta.Data
{
    public static class ChartParser
    {
        public static ChartFormat Parse(string song, string difficulty, Func<string, string> loadJsonText)
        {
            if (loadJsonText == null)
                throw new ArgumentNullException(nameof(loadJsonText));
            string pathKey = "songs/" + song.ToLowerInvariant() + "/" + difficulty.ToLowerInvariant();
            string text = loadJsonText(pathKey);
            if (string.IsNullOrEmpty(text))
                throw new InvalidOperationException("Chart JSON is empty for '" + pathKey + "'.");
            return ParseJson(text);
        }

        public static ChartFormat ParseJson(string jsonText)
        {
            using JsonDocument doc = JsonDocument.Parse(jsonText);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("codenameChart", out JsonElement cne) && cne.ValueKind == JsonValueKind.True)
                return ParseCne(root);

            if (!root.TryGetProperty("song", out JsonElement song) || song.ValueKind != JsonValueKind.Object)
                throw new InvalidOperationException("Chart JSON must contain a 'song' object or codenameChart format.");

            return ParseVanilla(song);
        }

        private static ChartFormat ParseCne(JsonElement json)
        {
            var parsed = new ChartFormat
            {
                Bpm = TryGetDouble(json, "bpm") ?? 0.0,
                ScrollSpeed = TryGetDouble(json, "speed") ?? TryGetDouble(json, "scrollSpeed") ?? 0.0,
                Notes = new List<ChartNote>(),
                Events = new List<ChartEvent>()
            };
            return parsed;
        }

        private static ChartFormat ParseVanilla(JsonElement json)
        {
            var parsed = new ChartFormat
            {
                Bpm = GetDouble(json, "bpm"),
                ScrollSpeed = GetDouble(json, "speed"),
                Notes = new List<ChartNote>(),
                Events = new List<ChartEvent>()
            };

            double curBpm = parsed.Bpm;
            double curTime = 0.0;
            double curCrochet = 60.0 / parsed.Bpm * 1000.0;

            if (!json.TryGetProperty("notes", out JsonElement sections) || sections.ValueKind != JsonValueKind.Array)
                return SortNotes(parsed);

            foreach (JsonElement section in sections.EnumerateArray())
            {
                if (section.ValueKind == JsonValueKind.Null)
                    continue;

                bool mustHitSection = section.TryGetProperty("mustHitSection", out JsonElement mhs) && mhs.GetBoolean();
                double eventTime = curTime <= 0.05 ? double.NegativeInfinity : curTime;
                parsed.Events.Add(new ChartEvent
                {
                    Time = eventTime,
                    Name = "Camera Pan",
                    Parameters = new[] { mustHitSection ? "1" : "0" }
                });

                if (section.TryGetProperty("changeBPM", out JsonElement ch) && ch.GetBoolean()
                    && section.TryGetProperty("bpm", out JsonElement sbpm))
                {
                    double sectionBpm = sbpm.GetDouble();
                    if (Math.Abs(sectionBpm - curBpm) > double.Epsilon)
                    {
                        curBpm = sectionBpm;
                        curCrochet = 60.0 / sectionBpm * 1000.0;
                        parsed.Events.Add(new ChartEvent
                        {
                            Time = curTime,
                            Name = "BPM Change",
                            Parameters = new[] { sectionBpm.ToString(CultureInfo.InvariantCulture) }
                        });
                    }
                }

                if (!section.TryGetProperty("sectionNotes", out JsonElement daNotes) || daNotes.ValueKind != JsonValueKind.Array)
                {
                    curTime += curCrochet * 4.0;
                    continue;
                }

                foreach (JsonElement note in daNotes.EnumerateArray())
                {
                    if (note.ValueKind != JsonValueKind.Array)
                        continue;
                    int len = note.GetArrayLength();
                    if (len < 3)
                        continue;

                    double noteTime = note[0].GetDouble();
                    int rawId = (int)note[1].GetDouble();
                    double noteLen = note[2].GetDouble();
                    int lane = ModPositive(rawId, 4);
                    bool mustPress = mustHitSection != (rawId >= 4);

                    parsed.Notes.Add(new ChartNote
                    {
                        Time = noteTime,
                        Id = lane,
                        Length = noteLen,
                        MustPress = mustPress
                    });
                }

                curTime += curCrochet * 4.0;
            }

            return SortNotes(parsed);
        }

        private static ChartFormat SortNotes(ChartFormat parsed)
        {
            parsed.Notes = parsed.Notes.OrderBy(n => n.Time).ToList();
            return parsed;
        }

        private static int ModPositive(int value, int m)
        {
            int r = value % m;
            return r < 0 ? r + m : r;
        }

        private static double GetDouble(JsonElement e, string name)
        {
            if (!e.TryGetProperty(name, out JsonElement p))
                return 0.0;
            return p.ValueKind switch
            {
                JsonValueKind.Number => p.GetDouble(),
                JsonValueKind.String => double.TryParse(p.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : 0.0,
                _ => 0.0
            };
        }

        private static double? TryGetDouble(JsonElement e, string name)
        {
            if (!e.TryGetProperty(name, out JsonElement p))
                return null;
            return p.ValueKind switch
            {
                JsonValueKind.Number => p.GetDouble(),
                JsonValueKind.String => double.TryParse(p.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double d) ? d : (double?)null,
                _ => null
            };
        }
    }
}