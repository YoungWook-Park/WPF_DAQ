using Bi.ConSightCommon;
using Bi.nsExpException;

namespace ConSight.DAQ.Data
{
    public class Write_TimeTriggerDataArgs
    {
        private long _TickCount;
        private int  _TriggerTime_Ms;

        public object? Param { get; set; }
        public ePLCWriteWord_TimeTrigger TriggerJob { get; set; }

        public Write_TimeTriggerDataArgs(ePLCWriteWord_TimeTrigger triggerJob, object? param = null, int triggerTime_Ms = 1000)
        {
            _TickCount      = BiTimer.GetTickCount();
            _TriggerTime_Ms = triggerTime_Ms;
            TriggerJob      = triggerJob;
            Param           = param;
        }

        public bool Timeout()
        {
            try
            {
                return BiTimer.TimeoutCheck_NonBlocking(ref _TickCount, _TriggerTime_Ms, false);
            }
            catch (ExpException expEx) { ExpException.RaiseExpException(expEx); return false; }
            catch (Exception ex)       { ExpException.RaiseException(ex);       return false; }
            finally { }
        }
    }
}
