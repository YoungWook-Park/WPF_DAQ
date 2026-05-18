using System.Windows.Controls;

namespace ConSight.DAQ.Views.Monitoring
{
    public partial class MonitoringView : UserControl
    {
        public MonitoringView(MonitoringViewModel vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
