using System.Windows;
using ConSight.DONGBO.PlcSimulator.Logic;
using ConSight.DONGBO.PlcSimulator.Memory;
using ConSight.DONGBO.PlcSimulator.Net;

namespace ConSight.DONGBO.PlcSimulator
{
    public partial class App : Application
    {
        internal PlcMemory Memory { get; } = new();
        internal PlcSimulatorServer Server { get; private set; } = null!;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            Server = new PlcSimulatorServer(Memory, port: 5000);
            _ = new SimulatorSignalHandler(Memory);
            Server.Start();
            new MainWindow().Show();
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Server.Stop();
            base.OnExit(e);
        }
    }
}
