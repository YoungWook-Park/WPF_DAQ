using Bi.nsExpException;

namespace ConSight.DAQ.Data
{
    internal class TimeTriggerQueue : IDisposable
    {
        private bool _DisposedValue;
        private static readonly object _Lock = new();
        private List<Write_TimeTriggerDataArgs> _List = new();

        protected void Dispose(bool disposing)
        {
            if (!_DisposedValue)
            {
                if (disposing) { _List.Clear(); }
                _DisposedValue = true;
            }
        }
        void IDisposable.Dispose() { Dispose(true); GC.SuppressFinalize(this); }

        public int Count
        {
            get
            {
                try { Monitor.Enter(_Lock); return _List.Count; }
                catch (ExpException expEx) { ExpException.RaiseExpException(expEx); return 0; }
                catch (Exception ex)       { ExpException.RaiseException(ex);       return 0; }
                finally { Monitor.Exit(_Lock); }
            }
        }

        public void Enqueue(Write_TimeTriggerDataArgs data)
        {
            try { Monitor.Enter(_Lock); _List.Add(data); }
            catch (ExpException expEx) { ExpException.RaiseExpException(expEx); }
            catch (Exception ex)       { ExpException.RaiseException(ex); }
            finally { Monitor.Exit(_Lock); }
        }

        public Write_TimeTriggerDataArgs? Dequeue()
        {
            try
            {
                Monitor.Enter(_Lock);
                if (_List.Count == 0) return null;
                var data = _List.First();
                if (data.Timeout()) { _List.Remove(data); return data; }
                return null;
            }
            catch (ExpException expEx) { ExpException.RaiseExpException(expEx); return null; }
            catch (Exception ex)       { ExpException.RaiseException(ex);       return null; }
            finally { Monitor.Exit(_Lock); }
        }

        public void Clear()
        {
            try { Monitor.Enter(_Lock); _List.Clear(); }
            catch (ExpException expEx) { ExpException.RaiseExpException(expEx); }
            catch (Exception ex)       { ExpException.RaiseException(ex); }
            finally { Monitor.Exit(_Lock); }
        }
    }
}
