// BiNotifyPropertyBase, BiRelayCommand, ObservableRangeCollection — 원본 Bi.ConSightCommon과 동일 API
// CommunityToolkit.Mvvm 기반 WPF 앱에서 직접 사용할 수 있도록 최소 구현 제공

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Bi.ConSightCommon
{
    public abstract class BiNotifyPropertyBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string? propertyName = null)
        {
            if (object.Equals(storage, value)) return false;
            storage = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            return true;
        }

        protected void OnPropertyChanged(string propertyName)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public class BiRelayCommand : ICommand
    {
        private Action<object?> _execute;
        private Predicate<object?> _canExecute;
        private event EventHandler? CanExecuteChangedInternal;

        public BiRelayCommand(Action<object?> execute) : this(execute, _ => true) { }

        public BiRelayCommand(Action<object?> execute, Predicate<object?> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute ?? throw new ArgumentNullException(nameof(canExecute));
        }

        public event EventHandler? CanExecuteChanged
        {
            add { System.Windows.Input.CommandManager.RequerySuggested += value; CanExecuteChangedInternal += value; }
            remove { System.Windows.Input.CommandManager.RequerySuggested -= value; CanExecuteChangedInternal -= value; }
        }

        public bool CanExecute(object? parameter) => _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);

        public void OnCanExecuteChanged() => CanExecuteChangedInternal?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Xamarin.CommunityToolkit.ObjectModel.ObservableRangeCollection 대체.
    /// AddRange / ReplaceRange를 지원하는 ObservableCollection 래퍼.
    /// </summary>
    public class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var item in items) Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void ReplaceRange(IEnumerable<T> items)
        {
            Items.Clear();
            foreach (var item in items) Items.Add(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }

    public static class MessageBoxUtil
    {
        public static void ShowMessageBox_Error(string message)
            => System.Windows.MessageBox.Show(message, "오류", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
    }

    /// <summary>BiTimer — 고해상도 타이머 (원본 Bi.ConSightCommon에서 사용하는 정적 메서드만 제공)</summary>
    public sealed class BiTimer
    {
        private static readonly Stopwatch _Clock = Stopwatch.StartNew();

        public static long GetTickCount() => _Clock.ElapsedMilliseconds;

        public static bool TimeoutCheck_NonBlocking(ref long deadlineMs, long dueTimeMs, bool autoRestart = true)
        {
            if (dueTimeMs <= 0) { if (autoRestart) deadlineMs = GetTickCount(); return true; }
            long now = GetTickCount();
            if (now < deadlineMs + dueTimeMs) return false;
            if (autoRestart)
            {
                long period = Math.Max(1, dueTimeMs);
                long periods = Math.Max(1L, (now - deadlineMs) / period + 1);
                deadlineMs += periods * period;
            }
            return true;
        }
    }

    /// <summary>NormValueDictionary — thread-safe string→object 딕셔너리 (원본 Bi.ConSightCommon 호환)</summary>
    public class NormValueDictionary
    {
        private readonly Dictionary<string, object?> _dict = new();
        private readonly object _lock = new();

        public object? this[string key]
        {
            get { lock (_lock) { return _dict.TryGetValue(key, out var v) ? v : null; } }
            set { lock (_lock) { _dict[key] = value; } }
        }

        public bool ContainsKey(string key) { lock (_lock) { return _dict.ContainsKey(key); } }
        public void Clear() { lock (_lock) { _dict.Clear(); } }
    }
}
