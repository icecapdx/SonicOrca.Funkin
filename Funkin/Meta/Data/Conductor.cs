using System;
using System.Collections.Generic;

namespace SonicOrca.Funkin.Meta.Data
{
    public struct BpmChange
    {
        public double Bpm;
        public double Time;
        public double Step;
    }

    public static class Conductor
    {
        private static double _bpm = 100.0;

        public static double Bpm
        {
            get => _bpm;
            set
            {
                _bpm = value;
                Crochet = 60.0 / _bpm * 1000.0;
                StepCrochet = Crochet / 4.0;
            }
        }

        public static double Crochet { get; private set; } = (60.0 / 100.0) * 1000.0;
        public static double StepCrochet { get; private set; } = (60.0 / 100.0) * 1000.0 / 4.0;

        public static event Action<int> OnStepHit;
        public static event Action<int> OnBeatHit;
        public static event Action<int> OnMeasureHit;

        public static List<BpmChange> BpmChanges { get; set; } = new List<BpmChange>();

        public static double SongPosition { get; set; }

        public static int CurStep { get; private set; }
        public static int CurBeat { get; private set; }
        public static int CurMeasure { get; private set; }

        public static void MapBpmChanges(ChartFormat song)
        {
            BpmChanges = new List<BpmChange>();
            if (song == null || song.Events == null)
                return;

            double curBpm = song.Bpm;
            double time = 0.0;
            double step = 0.0;

            foreach (ChartEvent e in song.Events)
            {
                if (e?.Name != "BPM Change" || e.Parameters == null || e.Parameters.Length == 0)
                    continue;

                if (!double.TryParse(e.Parameters[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double newBpm))
                    continue;

                if (Math.Abs(newBpm - curBpm) < double.Epsilon)
                    continue;

                double stepLen = (60.0 / curBpm) * 1000.0 / 4.0;
                double steps = (e.Time - time) / stepLen;
                step += steps;
                time = e.Time;
                curBpm = newBpm;

                BpmChanges.Add(new BpmChange
                {
                    Step = step,
                    Time = time,
                    Bpm = curBpm
                });
            }
        }

        public static void Update(double elapsedSeconds)
        {
            SongPosition += elapsedSeconds * 1000.0;
            UpdateTime();
        }

        public static void UpdateTime()
        {
            double songPos = SongPosition;
            BpmChange bpmChange = new BpmChange { Step = 0.0, Time = 0.0, Bpm = 0.0 };

            foreach (BpmChange ev in BpmChanges)
            {
                if (songPos >= ev.Time)
                    bpmChange = ev;
            }

            if (bpmChange.Bpm > 0.0 && Math.Abs(Bpm - bpmChange.Bpm) > double.Epsilon)
                Bpm = bpmChange.Bpm;

            int oldStep = CurStep;
            CurStep = (int)Math.Floor(bpmChange.Step + (songPos - bpmChange.Time) / StepCrochet);
            CurBeat = (int)Math.Floor(CurStep / 4.0);
            CurMeasure = (int)Math.Floor(CurBeat / 4.0);

            if (oldStep != CurStep)
                StepHit(CurStep);
        }

        public static void StepHit(int step)
        {
            OnStepHit?.Invoke(step);
            if (step % 4 == 0)
                BeatHit(CurBeat);
        }

        public static void BeatHit(int beat)
        {
            OnBeatHit?.Invoke(beat);
            if (beat % 4 == 0)
                MeasureHit(CurMeasure);
        }

        public static void MeasureHit(int measure)
        {
            OnMeasureHit?.Invoke(measure);
        }
    }
}