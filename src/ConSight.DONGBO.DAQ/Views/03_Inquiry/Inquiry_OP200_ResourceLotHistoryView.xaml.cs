using System.Windows.Controls;

namespace ConSight.DAQ.Views
{
    public partial class Inquiry_OP200_ResourceLotHistoryView : UserControl
    {
        public Inquiry_OP200_ResourceLotHistoryView(string connectionString)
        {
            InitializeComponent();
            DataContext = new Inquiry_OP200_ResourceLotHistoryViewModel(connectionString);
        }
    }
}
