using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ConSight.DAQ.Views
{
    /// <summary>
    /// ObservableCollection을 기반으로, 다수의 항목을 한 번의 CollectionChanged(Reset)로 교체.
    /// 반복 조회 시 new ObservableCollection 생성을 피해 ListCollectionView 재생성 없이 메모리 안정.
    /// </summary>
    public sealed class ObservableRangeCollection<T> : ObservableCollection<T>
    {
        /// <summary>기존 항목을 모두 제거하고 <paramref name="items"/>로 교체. Reset 알림 1회 발생.</summary>
        public void ReplaceAll(IEnumerable<T> items)
        {
            Items.Clear();
            foreach (var item in items)
                Items.Add(item);

            OnPropertyChanged(new PropertyChangedEventArgs("Count"));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
