using LiveSplit.Model;
using System;

namespace LiveSplit.UI.Components
{
    public class AverageCompletionTimeComponentFactory : IComponentFactory
    {
        public string ComponentName => "Average Completion Time";

        public string Description => "Displays the average time of completed runs.";

        public ComponentCategory Category => ComponentCategory.Information;

        public IComponent Create(LiveSplitState state) => new AverageCompletionTimeComponent(state);

        public string UpdateName => ComponentName;

        public string XMLURL => "http://livesplit.org/update/Components/update.LiveSplit.AverageCompletionTime.xml";

        public string UpdateURL => "http://livesplit.org/update/";

        public Version Version => Version.Parse("1.8.0");
    }
}
