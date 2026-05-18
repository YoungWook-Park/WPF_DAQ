using System;
using System.Windows;
using ConSight.DONGBO.PlcSimulator.Logic;
using ConSight.DONGBO.PlcSimulator.Memory;

namespace ConSight.DONGBO.PlcSimulator
{
    public partial class MainWindow : Window
    {
        private readonly PlcMemory _memory;

        public MainWindow()
        {
            InitializeComponent();
            _memory = ((App)Application.Current).Memory;
            _memory.Written += OnMemoryWritten;
            RefreshSnapshot();
        }

        private void BtnTriggerOp200_Click(object sender, RoutedEventArgs e)
        {
            _memory.Write("D1900", MockArrayBuilder.BuildOp200SettingArray());
            _memory.Write("D2000", MockArrayBuilder.BuildOp200ProcArray());
            AppendLog($"[{DateTime.Now:HH:mm:ss}] OP200 Triggered  (D1900 setting + D2000 proc)");
        }

        private void BtnTriggerOp210_Click(object sender, RoutedEventArgs e)
        {
            _memory.Write("D2200", MockArrayBuilder.BuildOp210ProcArray());
            AppendLog($"[{DateTime.Now:HH:mm:ss}] OP210 Triggered  (D2200 proc)");
        }

        private void BtnTriggerOp220_Click(object sender, RoutedEventArgs e)
        {
            _memory.Write("D2300", MockArrayBuilder.BuildOp220ProcArray());
            AppendLog($"[{DateTime.Now:HH:mm:ss}] OP220 Triggered  (D2300 proc)");
        }

        private void BtnTriggerOp230_Click(object sender, RoutedEventArgs e)
        {
            _memory.Write("D1800", MockArrayBuilder.BuildOp230SettingArray());
            _memory.Write("D2400", MockArrayBuilder.BuildOp230ProcArray());
            AppendLog($"[{DateTime.Now:HH:mm:ss}] OP230 Triggered  (D1800 setting + D2400 proc)");
        }

        // Written 이벤트는 백그라운드 스레드(TCP 수신)와 UI 스레드(버튼 클릭) 양쪽에서 발화
        private void OnMemoryWritten(string addr, short[] data)
        {
            Dispatcher.InvokeAsync(() =>
            {
                AppendLog($"[{DateTime.Now:HH:mm:ss}] MEM  {addr,-6} [{data.Length} words]  w[0]={data[0]}");
                RefreshSnapshot();
            });
        }

        private void RefreshSnapshot()
        {
            TxOp200Snap.Text = BuildOpSnap("D2000", "D2001", procWords: 100, flagIndex: 1);
            TxOp210Snap.Text = BuildOpSnap("D2200", "D2201", procWords:  70, flagIndex: 0);
            TxOp220Snap.Text = BuildOpSnap("D2300", "D2301", procWords:  70, flagIndex: 0);
            TxOp230Snap.Text = BuildOpSnap("D2400", "D2401", procWords:  80, flagIndex: 0);
        }

        private string BuildOpSnap(string procAddr, string writeAddr, int procWords, int flagIndex)
        {
            var proc  = _memory.Read(procAddr, procWords);
            var write = _memory.Read(writeAddr, flagIndex + 1);
            string backup = proc.Length  > 0            ? proc[0].ToString()  : "?";
            string flag   = write.Length > flagIndex    ? write[flagIndex].ToString() : "?";
            return $"BackUp_Start   : {backup}\nPC_Complete    : {flag}";
        }

        private void AppendLog(string text)
        {
            TxLog.AppendText(text + "\n");
            TxLog.ScrollToEnd();
        }

        private void Window_Closed(object? sender, EventArgs e)
        {
            _memory.Written -= OnMemoryWritten;
        }
    }
}
