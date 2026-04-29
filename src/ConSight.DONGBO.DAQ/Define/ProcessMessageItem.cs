namespace ConSight.DAQ
{
    public enum ProcessMessageType { Infomation, ProcessComplete, Error }

    public class ProcessMessageItem
    {
        public ProcessMessageType MessageType { get; set; }
        public string SenderName { get; set; } = string.Empty;
        public string Message    { get; set; } = string.Empty;
        public object? MsgObject { get; set; }
    }
}
