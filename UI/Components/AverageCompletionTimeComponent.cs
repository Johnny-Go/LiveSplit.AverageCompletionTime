﻿using LiveSplit.Extensions;
using LiveSplit.Model;
using LiveSplit.Model.Comparisons;
using LiveSplit.TimeFormatters;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public class AverageCompletionTimeComponent : IComponent
    {
        public string ComponentName => "Average Completion Time";
        public float HorizontalWidth => InternalComponent.HorizontalWidth;
        public float MinimumHeight => InternalComponent.MinimumHeight;
        public float VerticalHeight => InternalComponent.VerticalHeight;
        public float MinimumWidth => InternalComponent.MinimumWidth;
        public float PaddingTop => InternalComponent.PaddingTop;
        public float PaddingBottom => InternalComponent.PaddingBottom;
        public float PaddingLeft => InternalComponent.PaddingLeft;
        public float PaddingRight => InternalComponent.PaddingRight;
        public IDictionary<string, Action> ContextMenuControls => null;

        private InfoTimeComponent InternalComponent { get; set; }
        private AverageCompletionTimeSettings Settings { get; set; }
        private LiveSplitState CurrentState { get; set; }
        private TimingMethod PreviousTimingMethod { get; set; }
        private RegularAverageCompletionTimeTimeFormatter Formatter { get; set; }
        private AverageSegmentsComparisonGenerator AverageComparison { get; set; }
        private TimeSpan? AverageCompletionTimeValue { get; set; }

        private int NumCompleted { get; set; }
        private bool UseLatest { get; set; }
        private bool UseBest { get; set; }
        private bool UseAverageComparison { get; set; }
        private bool UseAllRuns { get; set; }
        private bool UseNoOutliers { get; set; }

        public AverageCompletionTimeComponent(LiveSplitState state)
        {
            Formatter = new RegularAverageCompletionTimeTimeFormatter();
            InternalComponent = new InfoTimeComponent("Average Completion Time", null, Formatter)
            {
                AlternateNameText = new string[]
                {
                    "Average Completion Time",
                    "Avg Time"
                }
            };
            Settings = new AverageCompletionTimeSettings();
            Settings.SettingsChanged += settings_SettingsChanged;
            NumCompleted = Settings.NumCompleted;
            UseLatest = Settings.UseLatest;
            UseBest = Settings.UseBest;
            UseAverageComparison = Settings.UseAverageComparison;
            UseAllRuns = Settings.UseAllRuns;
            UseNoOutliers = Settings.UseNoOutliers;
            state.OnReset += state_OnReset;
            state.RunManuallyModified += state_RunModified;
            CurrentState = state;
            AverageComparison = new AverageSegmentsComparisonGenerator(state.Run);

            UpdateAverageCompletionTime(state);
        }

        private void settings_SettingsChanged(object sender, EventArgs e)
        {
            UpdateAverageCompletionTime(CurrentState);
        }

        private void state_OnReset(object sender, TimerPhase e)
        {
            UpdateAverageCompletionTime((LiveSplitState)sender);
        }

        void state_RunModified(object sender, EventArgs e)
        {
            UpdateAverageCompletionTime((LiveSplitState)sender);
        }

        private void UpdateAverageCompletionTime(LiveSplitState state)
        {
            var method = state.CurrentTimingMethod;
            var run = state.Run;

            if (Settings.UseLatest || Settings.UseBest || Settings.UseAllRuns || Settings.UseNoOutliers)
            {
                IEnumerable<TimeSpan?> completedRuns;
                if (method == TimingMethod.GameTime)
                {
                    completedRuns = run.AttemptHistory.Where(h => h.Time.GameTime != null).Select(h => h.Time.GameTime);
                }
                else
                {
                    completedRuns = run.AttemptHistory.Where(h => h.Time.RealTime != null).Select(h => h.Time.RealTime);
                }

                if (Settings.UseLatest)
                {
                    completedRuns = completedRuns.TakeLast(Settings.NumCompleted);
                }
                else if (Settings.UseBest)
                {
                    completedRuns = completedRuns.OrderByDescending(x => x.Value).TakeLast(Settings.NumCompleted);
                }
                else if (Settings.UseNoOutliers)
                {
                    var count = completedRuns.Count();

                    //skip if 1 or less completetd runs
                    if (count > 1)
                    {
                        completedRuns = completedRuns.OrderBy(x => x);

                        //calculate median
                        var median = Median(completedRuns);

                        //calculate Q1 (median for values < median)
                        var bottomHalf = completedRuns.Take(count / 2);
                        var q1 = Median(bottomHalf.Where(x => x.Value < median));

                        //calculate Q3 (median for values > median)
                        var topHalf = completedRuns.Skip(count / 2);
                        var q3 = Median(topHalf.Where(x => x.Value > median));

                        //calculate IQR (Q3-Q1)
                        var iqr = (q3 - q1);

                        //calculate High boundary (Q3 + (1.5 x IQR))
                        var highBoundary = q3 + new TimeSpan((long)(1.5 * (iqr?.Ticks ?? 0)));

                        //calculate Low boundary (Q1 − (1.5 x IQR))
                        var lowBoundary = q1 - new TimeSpan((long)(1.5 * (iqr?.Ticks ?? 0)));

                        //remove outliers
                        completedRuns = completedRuns.Where(x => x.Value > lowBoundary && x.Value < highBoundary);
                    }
                }

                var totalTime = completedRuns.DefaultIfEmpty().Aggregate((s, a) => s + a);
                if (totalTime.HasValue)
                {
                    AverageCompletionTimeValue = TimeSpan.FromSeconds(totalTime.Value.TotalSeconds / completedRuns.Count());
                }
                else
                {
                    AverageCompletionTimeValue = null;
                }
            }
            else if (Settings.UseAverageComparison)
            {
                AverageComparison.Generate(method);
                AverageCompletionTimeValue = AverageComparison.Run[run.Count - 1].Comparisons["Average Segments"][method];
            }
            else
            {
                AverageCompletionTimeValue = null;
            }

            PreviousTimingMethod = state.CurrentTimingMethod;
        }

        private TimeSpan? Median(IEnumerable<TimeSpan?> list)
        {
            var orderedList = list.OrderBy(x => x);
            int count = orderedList.Count();
            TimeSpan? median = default(TimeSpan);
            if(count % 2 != 0)
            {
                median = orderedList.ElementAt((count - 1) / 2);
            }
            else
            {
                var mid = count / 2;
                var lower = orderedList.ElementAt(mid - 1);
                var higher = orderedList.ElementAt(mid);
                var ticksAverage = ((lower + higher)?.Ticks ?? 0) / 2;
                median = new TimeSpan(ticksAverage);
            }

            return median;
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.A > 0
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush (
                    new PointF(0, 0),
                    Settings.BackgroundGradient == GradientType.Horizontal
                        ? new PointF(width, 0)
                        : new PointF(0, height),
                    Settings.BackgroundColor,
                    Settings.BackgroundGradient == GradientType.Plain
                        ? Settings.BackgroundColor
                        : Settings.BackgroundColor2);

                g.FillRectangle(gradientBrush, 0, 0, width, height);
            }
        }

        public void DrawHorizontal(Graphics g, LiveSplitState state, float height, Region clipRegion)
        {
            DrawBackground(g, state, HorizontalWidth, height);

            InternalComponent.NameLabel.HasShadow
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            Formatter.Accuracy = Settings.Accuracy;

            if (SettingsChanged())
            {
                UpdateSettings();
                UpdateAverageCompletionTime(state);
            }

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideTimeColor ? Settings.TimeColor : state.LayoutSettings.TextColor;

            InternalComponent.DrawHorizontal(g, state, height, clipRegion);
        }

        public void DrawVertical(Graphics g, LiveSplitState state, float width, Region clipRegion)
        {
            DrawBackground(g, state, width, VerticalHeight);

            InternalComponent.DisplayTwoRows = Settings.Display2Rows;

            InternalComponent.NameLabel.HasShadow
                = InternalComponent.ValueLabel.HasShadow
                = state.LayoutSettings.DropShadows;

            Formatter.Accuracy = Settings.Accuracy;

            if (SettingsChanged())
            {
                UpdateSettings();
                UpdateAverageCompletionTime(state);
            }

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideTimeColor ? Settings.TimeColor : state.LayoutSettings.TextColor;

            InternalComponent.DrawVertical(g, state, width, clipRegion);
        }

        private bool SettingsChanged()
        {
            return NumCompleted != Settings.NumCompleted
                || UseLatest != Settings.UseLatest
                || UseBest != Settings.UseBest
                || UseAverageComparison != Settings.UseAverageComparison
                || UseAllRuns != Settings.UseAllRuns
                || UseNoOutliers != Settings.UseNoOutliers;
        }

        private void UpdateSettings()
        {
            NumCompleted = Settings.NumCompleted;
            UseLatest = Settings.UseLatest;
            UseBest = Settings.UseBest;
            UseAverageComparison = Settings.UseAverageComparison;
            UseAllRuns = Settings.UseAllRuns;
            UseNoOutliers = Settings.UseNoOutliers;
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            return Settings.GetSettings(document);
        }

        public Control GetSettingsControl(LayoutMode mode)
        {
            Settings.Mode = mode;
            return Settings;
        }

        public void SetSettings(XmlNode settings)
        {
            Settings.SetSettings(settings);
        }

        public void Update(IInvalidator invalidator, LiveSplitState state, float width, float height, LayoutMode mode)
        {
            if (CheckIfRunChanged(state))
            {
                UpdateAverageCompletionTime(state);
            }

            InternalComponent.TimeValue = AverageCompletionTimeValue;
            InternalComponent.Update(invalidator, state, width, height, mode);
        }

        private bool CheckIfRunChanged(LiveSplitState state)
        {
            if (PreviousTimingMethod != state.CurrentTimingMethod)
            {
                return true;
            }

            return false;
        }

        public void Dispose()
        {
            CurrentState.OnReset -= state_OnReset;
            CurrentState.RunManuallyModified -= state_RunModified;
        }

        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}
