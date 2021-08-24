using LiveSplit.Extensions;
using LiveSplit.Model;
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
        protected InfoTimeComponent InternalComponent { get; set; }
        public AverageCompletionTimeSettings Settings { get; set; }
        private LiveSplitState CurrentState { get; set; }
        private TimingMethod PreviousTimingMethod { get; set; }
        private RegularAverageCompletionTimeTimeFormatter Formatter { get; set; }
        private int LatestCompleted { get; set; }
        private bool UseAllRuns { get; set; }

        private TimeSpan? AverageCompletionTimeValue { get; set; }

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
            Settings = new AverageCompletionTimeSettings(state);
            Settings.SettingsChanged += Settings_SettingsChanged;
            LatestCompleted = Settings.LatestCompleted;
            UseAllRuns = Settings.UseAllRuns;
            state.OnSplit += state_OnSplit;
            state.OnUndoSplit += state_OnUndoSplit;
            state.OnReset += state_OnReset;
            CurrentState = state;
            CurrentState.RunManuallyModified += CurrentState_RunModified;
            UpdateAverageCompletionTime(state);
        }

        void Settings_SettingsChanged(object sender, EventArgs e)
        {
            UpdateAverageCompletionTime(CurrentState);
        }

        private void state_OnReset(object sender, TimerPhase e)
        {
            UpdateAverageCompletionTime((LiveSplitState)sender);
        }

        private void state_OnUndoSplit(object sender, EventArgs e)
        {
            UpdateAverageCompletionTime((LiveSplitState)sender);
        }

        private void state_OnSplit(object sender, EventArgs e)
        {
            UpdateAverageCompletionTime((LiveSplitState)sender);
        }

        private void CurrentState_RunModified(object sender, EventArgs e)
        {
            UpdateAverageCompletionTime(CurrentState);
        }

        private void UpdateAverageCompletionTime(LiveSplitState state)
        {
            LatestCompleted = Settings.LatestCompleted;
            UseAllRuns = Settings.UseAllRuns;

            var method = state.CurrentTimingMethod;
            var run = state.Run;

            IEnumerable<TimeSpan?> completedRuns;
            if (method == TimingMethod.GameTime)
            {
                completedRuns = run.AttemptHistory.Where(h => h.Time.GameTime != null).Select(h => h.Time.GameTime);
            }
            else
            {
                completedRuns = run.AttemptHistory.Where(h => h.Time.RealTime != null).Select(h => h.Time.RealTime);
            }

            if (!UseAllRuns)
            {
                completedRuns = completedRuns.TakeLast(LatestCompleted);
            }

            var totalTime = completedRuns.Aggregate((s, a) => s + a);

            AverageCompletionTimeValue = TimeSpan.FromSeconds(totalTime.Value.TotalSeconds / completedRuns.Count());
            PreviousTimingMethod = state.CurrentTimingMethod;
        }

        private void DrawBackground(Graphics g, LiveSplitState state, float width, float height)
        {
            if (Settings.BackgroundColor.A > 0
                || Settings.BackgroundGradient != GradientType.Plain
                && Settings.BackgroundColor2.A > 0)
            {
                var gradientBrush = new LinearGradientBrush(
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

            InternalComponent.NameLabel.ForeColor = Settings.OverrideTextColor ? Settings.TextColor : state.LayoutSettings.TextColor;
            InternalComponent.ValueLabel.ForeColor = Settings.OverrideTimeColor ? Settings.TimeColor : state.LayoutSettings.TextColor;

            InternalComponent.DrawVertical(g, state, width, clipRegion);
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
            CurrentState.OnSplit -= state_OnSplit;
            CurrentState.OnUndoSplit -= state_OnUndoSplit;
            CurrentState.OnReset -= state_OnReset;
        }

        public int GetSettingsHashCode() => Settings.GetSettingsHashCode();
    }
}
