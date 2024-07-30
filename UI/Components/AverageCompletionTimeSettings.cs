using LiveSplit.Model;
using LiveSplit.TimeFormatters;
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

namespace LiveSplit.UI.Components
{
    public partial class AverageCompletionTimeSettings : UserControl
    {
        public Color TextColor { get; set; }
        public bool OverrideTextColor { get; set; }
        public Color TimeColor { get; set; }
        public bool OverrideTimeColor { get; set; }
        public TimeAccuracy Accuracy { get; set; }

        public Color BackgroundColor { get; set; }
        public Color BackgroundColor2 { get; set; }
        public GradientType BackgroundGradient { get; set; }
        public string GradientString
        {
            get
            {
                return BackgroundGradient.ToString();
            }
            set
            {
                BackgroundGradient = (GradientType)Enum.Parse(typeof(GradientType), value);
            }
        }

        public bool Display2Rows { get; set; }    

        public LayoutMode Mode { get; set; }
        
        public int NumCompleted { get; set; }
        public bool UseLatest { get; set; }
        public bool UseBest { get; set; }
        public bool UseAllRuns { get; set; }
        public bool UseNoOutliers { get; set; }
        public bool UseAverageComparison { get; set; }

        public event EventHandler SettingsChanged;

        public AverageCompletionTimeSettings()
        {
            InitializeComponent();

            TextColor = Color.FromArgb(255, 255, 255);
            OverrideTextColor = false;
            TimeColor = Color.FromArgb(255, 255, 255);
            OverrideTimeColor = false;
            Accuracy = TimeAccuracy.Seconds;
            BackgroundColor = Color.Transparent;
            BackgroundColor2 = Color.Transparent;
            BackgroundGradient = GradientType.Plain;
            Display2Rows = false;
            UseLatest = true;
            UseBest = false;
            UseAllRuns = false;
            UseNoOutliers = false;
            UseAverageComparison = false;
            NumCompleted = 100;
            
            nudNumCompleted.DataBindings.Add("Value", this, "NumCompleted", false, DataSourceUpdateMode.OnPropertyChanged);
            rdoUseLatest.DataBindings.Add("Checked", this, "UseLatest", false, DataSourceUpdateMode.OnPropertyChanged);
            rdoUseBest.DataBindings.Add("Checked", this, "UseBest", false, DataSourceUpdateMode.OnPropertyChanged);
            rdoUseAllRuns.DataBindings.Add("Checked", this, "UseAllRuns", false, DataSourceUpdateMode.OnPropertyChanged);
            rdoUseNoOutliers.DataBindings.Add("Checked", this, "UseNoOutliers", false, DataSourceUpdateMode.OnPropertyChanged);
            rdoUseAvgComp.DataBindings.Add("Checked", this, "UseAverageComparison", false, DataSourceUpdateMode.OnPropertyChanged);
            chkOverrideTextColor.DataBindings.Add("Checked", this, "OverrideTextColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnTextColor.DataBindings.Add("BackColor", this, "TextColor", false, DataSourceUpdateMode.OnPropertyChanged);
            chkOverrideTimeColor.DataBindings.Add("Checked", this, "OverrideTimeColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnTimeColor.DataBindings.Add("BackColor", this, "TimeColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnColor1.DataBindings.Add("BackColor", this, "BackgroundColor", false, DataSourceUpdateMode.OnPropertyChanged);
            btnColor2.DataBindings.Add("BackColor", this, "BackgroundColor2", false, DataSourceUpdateMode.OnPropertyChanged);
            cmbGradientType.DataBindings.Add("SelectedItem", this, "GradientString", false, DataSourceUpdateMode.OnPropertyChanged);
        }

        private void chkOverrideTimeColor_CheckedChanged(object sender, EventArgs e)
        {
            label2.Enabled = btnTimeColor.Enabled = chkOverrideTimeColor.Checked;
        }

        private void chkOverrideTextColor_CheckedChanged(object sender, EventArgs e)
        {
            label1.Enabled = btnTextColor.Enabled = chkOverrideTextColor.Checked;
        }

        private void AverageCompletionTimeSettings_Load(object sender, EventArgs e)
        {
            chkOverrideTextColor_CheckedChanged(null, null);
            chkOverrideTimeColor_CheckedChanged(null, null);
            rdoSeconds.Checked = Accuracy == TimeAccuracy.Seconds;
            rdoTenths.Checked = Accuracy == TimeAccuracy.Tenths;
            rdoHundredths.Checked = Accuracy == TimeAccuracy.Hundredths;
            nudNumCompleted.Value = NumCompleted;
            rdoUseLatest.Checked = UseLatest;
            rdoUseBest.Checked = UseBest;
            rdoUseAvgComp.Checked = UseAverageComparison;
            rdoUseAllRuns.Checked = UseAllRuns;
            rdoUseNoOutliers.Checked = UseNoOutliers;
            rdoComparisonGroup_Click(null, null);
            if (Mode == LayoutMode.Horizontal)
            {
                chkTwoRows.Enabled = false;
                chkTwoRows.DataBindings.Clear();
                chkTwoRows.Checked = true;
            }
            else
            {
                chkTwoRows.Enabled = true;
                chkTwoRows.DataBindings.Clear();
                chkTwoRows.DataBindings.Add("Checked", this, "Display2Rows", false, DataSourceUpdateMode.OnPropertyChanged);
            }
        }

        private void cmbGradientType_SelectedIndexChanged(object sender, EventArgs e)
        {
            btnColor1.Visible = cmbGradientType.SelectedItem.ToString() != "Plain";
            btnColor2.DataBindings.Clear();
            btnColor2.DataBindings.Add("BackColor", this, btnColor1.Visible ? "BackgroundColor2" : "BackgroundColor", false, DataSourceUpdateMode.OnPropertyChanged);
            GradientString = cmbGradientType.SelectedItem.ToString();
        }

        private void rdoAccuracyGroup_Click(object sender, EventArgs e)
        {
            var rb = sender as RadioButton;
            if (rb != null && !rb.Checked)
            {
                rb.Checked = true;
            }
            UpdateAccuracy();
        }

        private void UpdateAccuracy()
        {
            if (rdoSeconds.Checked)
            {
                Accuracy = TimeAccuracy.Seconds;
            }
            else if (rdoTenths.Checked)
            {
                Accuracy = TimeAccuracy.Tenths;
            }
            else
            {
                Accuracy = TimeAccuracy.Hundredths;
            }
        }

        private void rdoComparisonGroup_Click(object sender, EventArgs e)
        {
            var rb = sender as RadioButton;
            if (rb != null && !rb.Checked)
            {
                rb.Checked = true;
            }
            UpdateSettingsRadio();
        }

        private void UpdateSettingsRadio()
        {
            label4.Enabled = (rdoUseLatest.Checked || rdoUseBest.Checked);
            nudNumCompleted.Enabled = (rdoUseLatest.Checked || rdoUseBest.Checked);
            UseLatest = rdoUseLatest.Checked;
            UseBest = rdoUseBest.Checked;
            UseAverageComparison = rdoUseAvgComp.Checked;
            UseAllRuns = rdoUseAllRuns.Checked;
            UseNoOutliers = rdoUseNoOutliers.Checked;

            SettingsChanged(this, null);
        }

        private void nudLatestCompleted_ValueChanged(object sender, EventArgs e)
        {
            NumCompleted = (int)nudNumCompleted.Value;
            SettingsChanged(this, null);
        }

        public void SetSettings(XmlNode node)
        {
            var element = (XmlElement)node;
            NumCompleted = SettingsHelper.ParseInt(element["NumCompleted"]);
            TextColor = SettingsHelper.ParseColor(element["TextColor"]);
            OverrideTextColor = SettingsHelper.ParseBool(element["OverrideTextColor"]);
            TimeColor = SettingsHelper.ParseColor(element["TimeColor"]);
            OverrideTimeColor = SettingsHelper.ParseBool(element["OverrideTimeColor"]);
            Accuracy = SettingsHelper.ParseEnum<TimeAccuracy>(element["Accuracy"]);
            BackgroundColor = SettingsHelper.ParseColor(element["BackgroundColor"]);
            BackgroundColor2 = SettingsHelper.ParseColor(element["BackgroundColor2"]);
            GradientString = SettingsHelper.ParseString(element["BackgroundGradient"]);
            Display2Rows = SettingsHelper.ParseBool(element["Display2Rows"], false);
            UseAllRuns = SettingsHelper.ParseBool(element["UseAllRuns"], false);
            UseNoOutliers = SettingsHelper.ParseBool(element["UseNoOutliers"], false);
            UseAverageComparison = SettingsHelper.ParseBool(element["UseAverageComparison"], false);
            UseLatest = SettingsHelper.ParseBool(element["UseLatest"], false);
            UseBest = SettingsHelper.ParseBool(element["UseBest"], false);
        }

        public XmlNode GetSettings(XmlDocument document)
        {
            var parent = document.CreateElement("Settings");
            CreateSettingsNode(document, parent);
            return parent;
        }

        public int GetSettingsHashCode()
        {
            return CreateSettingsNode(null, null);
        }

        private int CreateSettingsNode(XmlDocument document, XmlElement parent)
        {
            return SettingsHelper.CreateSetting(document, parent, "Version", "1.4") ^
            SettingsHelper.CreateSetting(document, parent, "TextColor", TextColor) ^
            SettingsHelper.CreateSetting(document, parent, "OverrideTextColor", OverrideTextColor) ^
            SettingsHelper.CreateSetting(document, parent, "TimeColor", TimeColor) ^
            SettingsHelper.CreateSetting(document, parent, "OverrideTimeColor", OverrideTimeColor) ^
            SettingsHelper.CreateSetting(document, parent, "Accuracy", Accuracy) ^
            SettingsHelper.CreateSetting(document, parent, "BackgroundColor", BackgroundColor) ^
            SettingsHelper.CreateSetting(document, parent, "BackgroundColor2", BackgroundColor2) ^
            SettingsHelper.CreateSetting(document, parent, "BackgroundGradient", BackgroundGradient) ^
            SettingsHelper.CreateSetting(document, parent, "Display2Rows", Display2Rows) ^
            SettingsHelper.CreateSetting(document, parent, "UseAllRuns", UseAllRuns) ^
            SettingsHelper.CreateSetting(document, parent, "UseNoOutliers", UseNoOutliers) ^
            SettingsHelper.CreateSetting(document, parent, "UseAverageComparison", UseAverageComparison) ^
            SettingsHelper.CreateSetting(document, parent, "UseLatest", UseLatest) ^
            SettingsHelper.CreateSetting(document, parent, "UseBest", UseBest) ^
            SettingsHelper.CreateSetting(document, parent, "NumCompleted", NumCompleted);
        }

        private void ColorButtonClick(object sender, EventArgs e)
        {
            SettingsHelper.ColorButtonClick((Button)sender, this);
        }
    }
}
