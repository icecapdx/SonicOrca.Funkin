using System.Collections.Generic;

namespace SonicOrca.Funkin.Meta.Data
{
    public sealed class ChartNote
    {
        public double Time { get; set; }
        public int Id { get; set; }
        public double Length { get; set; }
        public bool MustPress { get; set; }
    }

    public sealed class ChartEvent
    {
        public double Time { get; set; }
        public string Name { get; set; }
        public string[] Parameters { get; set; }
    }

    public sealed class ChartFormat
    {
        public double Bpm { get; set; }
        public double ScrollSpeed { get; set; }
        public List<ChartNote> Notes { get; set; } = new List<ChartNote>();
        public List<ChartEvent> Events { get; set; } = new List<ChartEvent>();
    }
}