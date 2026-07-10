namespace DockTabControlDemo;

using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;

public abstract class BindableBase : INotifyPropertyChanged
{
    public delegate void ValueChangedEventHandler<T>(T? oldValue);

    private readonly Dictionary<SubscriptionKey, ICollectionSubscription> _collectionSubscriptions = new(SubscriptionKey.Comparer.Instance);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual bool Set<T>(ref T storage, T value, Action? onChanged = null, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        onChanged?.Invoke();
        RaisePropertyChanged(propertyName);
        return true;
    }

    protected virtual bool Set<TValue, TStorage>(
        TStorage storage,
        Expression<Func<TStorage, TValue>> propertyExpression,
        TValue value,
        Action? onChanged = null,
        [CallerMemberName] string? propertyName = null)
    {
        if (propertyExpression.Body is not MemberExpression { Member: PropertyInfo propertyInfo })
            throw new ArgumentException("Expression should be a property.");

        if (propertyInfo.PropertyType != typeof(TValue))
            throw new ArgumentException($"Expression should be '{nameof(TValue)}' type.");

        var currentValue = (TValue)propertyInfo.GetValue(storage)!;

        if (EqualityComparer<TValue>.Default.Equals(currentValue, value))
            return false;

        propertyInfo.SetValue(storage, value);
        onChanged?.Invoke();
        RaisePropertyChanged(propertyName);
        return true;
    }

    protected bool SetObservableCollection<T>(
        ref ObservableCollection<T> storage,
        ObservableCollection<T> value,
        Action<T>? onItemAdded = null,
        Action<T>? onItemRemoved = null,
        [CallerMemberName] string? propertyName = null)
        where T : class
    {
        if (ReferenceEquals(storage, value))
            return false;

        DetachCollection(storage, propertyName);
        foreach (var item in storage.Except(value))
            onItemRemoved?.Invoke(item);

        var oldStorage = storage;
        storage = value;
        AttachCollection(value, propertyName, onItemAdded, onItemRemoved);
        foreach (var item in storage.Except(oldStorage))
            onItemAdded?.Invoke(item);

        RaisePropertyChanged(propertyName);
        return true;
    }

    protected void RaisePropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new(propertyName));


    private void AttachCollection<T>(
        ObservableCollection<T>? collection,
        string? propertyName,
        Action<T>? onItemAdded,
        Action<T>? onItemRemoved)
        where T : class
    {
        if (collection is null || propertyName is null)
            return;

        var key = new SubscriptionKey(collection, propertyName);
        if (_collectionSubscriptions.ContainsKey(key))
            return;

        var subscription = new CollectionSubscription<T>(
            collection,
            () => RaisePropertyChanged(propertyName),
            onItemAdded,
            onItemRemoved);

        _collectionSubscriptions.Add(key, subscription);
        subscription.Attach();
    }

    private void DetachCollection<T>(ObservableCollection<T>? collection, string? propertyName)
        where T : class
    {
        if (collection is null || propertyName is null)
            return;

        var key = new SubscriptionKey(collection, propertyName);
        if (!_collectionSubscriptions.TryGetValue(key, out var subscription))
            return;

        subscription.Detach();
        _collectionSubscriptions.Remove(key);
    }

    private interface ICollectionSubscription
    {
        void Attach();
        void Detach();
    }

    private sealed class CollectionSubscription<T> : ICollectionSubscription
        where T : class
    {
        private readonly ObservableCollection<T> _collection;
        private readonly NotifyCollectionChangedEventHandler _collectionChangedHandler;
        private readonly PropertyChangedEventHandler _itemPropertyChangedHandler;

        public CollectionSubscription(
            ObservableCollection<T> collection,
            Action raiseOwnerPropertyChanged,
            Action<T>? onItemAdded,
            Action<T>? onItemRemoved)
        {
            _collection = collection;

            _itemPropertyChangedHandler = (_, __) => raiseOwnerPropertyChanged();

            _collectionChangedHandler = (_, e) =>
            {
                foreach (var item in e.NewItems?.OfType<INotifyPropertyChanged>() ?? [])
                {
                    item.PropertyChanged += _itemPropertyChangedHandler;
                    if (item is T typed)
                        onItemAdded?.Invoke(typed);
                }

                foreach (var item in e.OldItems?.OfType<INotifyPropertyChanged>() ?? [])
                {
                    item.PropertyChanged -= _itemPropertyChangedHandler;
                    if (item is T typed)
                        onItemRemoved?.Invoke(typed);
                }

                raiseOwnerPropertyChanged();
            };
        }

        public void Attach()
        {
            _collection.CollectionChanged += _collectionChangedHandler;

            foreach (var item in _collection.OfType<INotifyPropertyChanged>())
                item.PropertyChanged += _itemPropertyChangedHandler;
        }

        public void Detach()
        {
            _collection.CollectionChanged -= _collectionChangedHandler;

            foreach (var item in _collection.OfType<INotifyPropertyChanged>())
                item.PropertyChanged -= _itemPropertyChangedHandler;
        }
    }

    private sealed class SubscriptionKey
    {
        private readonly object _collection;
        private readonly string _propertyName;

        public SubscriptionKey(object collection, string propertyName)
        {
            _collection = collection;
            _propertyName = propertyName;
        }

        public sealed class Comparer : IEqualityComparer<SubscriptionKey>
        {
            public static readonly Comparer Instance = new();

            public bool Equals(SubscriptionKey? x, SubscriptionKey? y)
                => ReferenceEquals(x?._collection, y?._collection) && x?._propertyName == y?._propertyName;

            public int GetHashCode(SubscriptionKey obj)
                => HashCode.Combine(RuntimeHelpers.GetHashCode(obj._collection), obj._propertyName);
        }
    }
}