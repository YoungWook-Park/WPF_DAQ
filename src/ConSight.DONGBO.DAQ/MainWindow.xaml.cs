using System.Windows;
using ConSight.DAQ.Views;
using ConSight.DAQ.Views.Monitoring;
using ConSight.DONGBO.DAQ.Views;

namespace ConSight.DONGBO.DAQ;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        var vm = (MainWindowViewModel)DataContext;
        vm.Initialize();
        AdoViewHost.Content      = new Inquiry_OP200_ResourceLotHistoryView(MainCore.ConnectionString);
        EfViewHost.Content       = new Inquiry_OP200_ResourceLotHistoryView(vm.EfVm);
        TestViewHost.Content     = new ProcessPipelineTestView(MainCore.ConnectionString, MainCore.Instance.EventBus);
        MonitoringViewHost.Content = new MonitoringView(new MonitoringViewModel(MainCore.Instance.EventBus));
    }

    private void Window_Closed(object? sender, EventArgs e) =>
        ((MainWindowViewModel)DataContext).Shutdown();
}
