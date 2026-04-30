using System.Windows.Controls;

namespace ConSight.DAQ.Views
{
    public partial class Inquiry_OP200_ResourceLotHistoryView : UserControl
    {
        // ADO.NET ViewModel
        public Inquiry_OP200_ResourceLotHistoryView(string connectionString)
        {
            InitializeComponent();
            DataContext = new Inquiry_OP200_ResourceLotHistoryViewModel(connectionString);
        }

        // EF Core ViewModel — 같은 View, 다른 DataContext
        public Inquiry_OP200_ResourceLotHistoryView(Inquiry_OP200_ResourceLotHistoryViewModel_EfCore vm)
        {
            InitializeComponent();
            DataContext = vm;
        }
    }
}
