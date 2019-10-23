using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;

namespace MyFCL.Collections.Generic
{
    /// <summary>
    /// Implements a variable-size List that uses an array of objects to store the elements.
    /// A List has a capacity, which is the allocated length of the internal array.
    /// As elements are added to a List, the capacity of the List is automatically increased 
    /// as required by reallocating the internal array.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MyList<T> : IList<T>, IList//
    {
        private const int _defaultCapacity = 4;

        private T[] _items;
        private int _size;
        private int _version;

        private object _syncRoot;

        static readonly T[] _emptyArray = new T[0];

        #region ctor

        /// <summary>
        /// Constructs a List.
        /// The list is initially empty and has a capacity of zero.
        /// Upon adding the first element to the list the capacity is increased to 16, and then increased in multiples of two as required.
        /// </summary>
        public MyList()
        {
            _items = _emptyArray;
        }

        /// <summary>
        /// Constructs a List with a given initial capacity.
        /// The list is initially empty, but will have room for the given number of elements
        /// before any reallocations are required.
        /// </summary>
        /// <param name="capacity"></param>
        public MyList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException(nameof(capacity), "Negitive numbers are not allowed");

            Contract.EndContractBlock();

            if (capacity == 0)
                _items = _emptyArray;
            else
                _items = new T[capacity];

        }

        /// <summary>
        /// Constructs a List, copying the contents of the given collection.
        /// The size and capacity of the new list will both be equal to the size of the given collection.
        /// </summary>
        /// <param name="collection"></param>
        public MyList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            Contract.EndContractBlock();

            ICollection<T> c = collection as ICollection<T>;
            if (c != null)
            {
                int count = c.Count;
                if (count == 0)
                {
                    _items = _emptyArray;
                }
                else
                {
                    _items = new T[count];
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }
            else
            {
                _size = 0;
                _items = _emptyArray;

                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        // To implement.
                    }
                }
            }
        }
        #endregion

        /// <summary>
        /// Gets and sets the capacity of this list.
        /// The capacity is the size of the internal array used to hold items.
        /// When set, the internal array of the list is reallocated to the given capacity.
        /// </summary>
        public int Capacity
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _items.Length;
            }
            set
            {
                if (value < _size)
                    throw new ArgumentOutOfRangeException(nameof(value), "Small Capacity.");

                Contract.EndContractBlock();

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (_size > 0)
                        {
                            Array.Copy(_items, 0, newItems, 0, _size);
                        }
                        _items = newItems;
                    }
                    else
                    {
                        // _size = 0
                        _items = _emptyArray;
                    }
                }
            }
        }

        public void AddRange(IEnumerable<T> collection)
        {
            Contract.Ensures(Count >= Contract.OldValue(Count));

            InsertRange(_size, collection);
        }

        /// <summary>
        /// Inserts the elements of the given collection at a given index.
        /// If required, the capacity of the list is increased to twice the previous capacity or the new size,
        /// which is larger.
        /// Ranges may be added to the end of the list by setting index to the List's size.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="collection"></param>
        public void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException();

            if ((uint)index > (uint)_size)
                throw new ArgumentOutOfRangeException();

            Contract.EndContractBlock();

            ICollection<T> c = collection as ICollection<T>;
            if (c != null)
            {
                int count = c.Count;
                if (count > 0)
                    EnsureCapacity(_size + count);
                if (index < _size)
                {
                    Array.Copy(_items, index, _items, index + count, _size - index);
                }

                // If we're inserting a List into itself, we want to be able to deal with that.
                if (this == c)
                {
                    // Copy first part of _items to insert location
                    Array.Copy(_items, 0, _items, index, index);
                    // Copy last part of _items back to inserted location
                    Array.Copy(_items, index + count, _items, index * 2, _size - index);
                }
                else
                {
                    T[] itemsToInsert = new T[count];
                    c.CopyTo(itemsToInsert, 0);
                    itemsToInsert.CopyTo(_items, index);
                }
                _size += count;
            }
            else
            {
                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Insert(index++, en.Current);
                    }
                }
            }
            _version++;
        }


        /// <summary>
        /// Read-only property describing how many elements are in the List.
        /// </summary>
        int ICollection.Count
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _size;
            }
        }

        /// <summary>
        /// Synchronization root for this object.
        /// </summary>
        object ICollection.SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                    System.Threading.Interlocked.CompareExchange<object>(ref _syncRoot, new object(), null);

                return _syncRoot;
            }
        }

        /// <summary>
        /// Is this List synchronized (thread-safe)?
        /// </summary>
        bool ICollection.IsSynchronized
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Copies this List into array, which must be of a compatible array type.  
        /// </summary>
        /// <param name="array"></param>
        /// <param name="arrayIndex"></param>
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
            {
                //ThrowHelper.ThrowArgumentException(ExceptionResource.Arg_RankMultiDimNotSupported);
                throw new ArgumentException();
            }
            Contract.EndContractBlock();

            try
            {
                // Array.Copy will check for NULL.
                Array.Copy(_items, 0, array, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                //ThrowHelper.ThrowArgumentException(ExceptionResource.Argument_InvalidArrayType);
                throw new ArgumentException();
            }
        }



        Object IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                //ThrowHelper.IfNullAndNullsAreIllegalThenThrow<T>(value, ExceptionArgument.value);
                try
                {
                    this[index] = (T)value;
                }
                catch (InvalidCastException)
                {
                    //ThrowHelper.ThrowWrongValueTypeArgumentException(value, typeof(T));
                    throw new ArgumentException();
                }
            }
        }

        bool IList.IsReadOnly
        {
            get { return false; }
        }

        bool IList.IsFixedSize
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        /// <returns> The position into which the new element was inserted, or -1 to indicate that the item was not inserted into the collection. </returns>
        int IList.Add(object value)
        {
            if (value == null && default(T) != null)
                throw new ArgumentException();

            try
            {
                Add((T)value);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException();
            }
            return Count - 1;
        }

        //void IList.Clear()
        //{
        //    Clear();
        //}

        bool IList.Contains(object value)
        {
            if (IsCompatibleObject(value))
                return Contains((T)value);
            return false;
        }

        int IList.IndexOf(object value)
        {
            if (IsCompatibleObject(value))
            {
                return IndexOf((T)value);
            }
            return -1;
        }

        void IList.Insert(int index, object value)
        {
            if (value == null && default(T) != null)
                throw new ArgumentNullException();

            try
            {
                Insert(index, (T)value);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException();
            }

        }

        void IList.Remove(object value)
        {
            if (IsCompatibleObject(value))
            {
                Remove((T)value);
            }
        }

        //void IList.RemoveAt(int index)
        //{
        //    RemoveAt(index);
        //}


        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }



        public int Count
        {
            get
            {
                Contract.Ensures(Contract.Result<int>() >= 0);
                return _size;
            }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public void Add(T item)
        {
            if (_size == _items.Length)
                EnsureCapacity(_size + 1);

            _items[_size++] = item;
            _version++;
        }

        public void Clear()
        {
            if (_size > 0)
            {
                Array.Clear(_items, 0, _size);
                _size = 0;
            }
            _version++;
        }

        public bool Contains(T item)
        {
            if ((Object)item == null)
            {
                for (int i = 0; i < _size; i++)
                {
                    if ((Object)_items[i] == null)
                        return true;
                }
                return false;
            }
            else
            {
                EqualityComparer<T> c = EqualityComparer<T>.Default;
                for (int i = 0; i < _size; i++)
                {
                    if (c.Equals(_items[i], item))
                        return true;
                }
                return false;
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            if (array != null && array.Rank != 1)
                throw new ArgumentException();
            Contract.EndContractBlock();

            try
            {
                // Array.Copy will check for NULL.
                Array.Copy(_items, 0, array, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw;
            }
        }

        public bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }
            return false;
        }



        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));
                }

                Contract.EndContractBlock();
                return _items[index];
            }
            set
            {
                if ((uint)index >= (uint)_size)
                {
                    throw new ArgumentOutOfRangeException(nameof(index));

                }

                Contract.EndContractBlock();

                _items[index] = value;
                _version++;
            }
        }

        public int IndexOf(T item)
        {
            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(Contract.Result<int>() < Count);
            return Array.IndexOf(_items, item, 0, _size);
        }

        /// <summary>
        /// Inserts an element into this list at a given index.
        /// The size of the list is increased by one.
        /// If required, the capacity of the list is doubled before inserting the new element.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="item"></param>
        public void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException();
            }
            Contract.EndContractBlock();

            if (_size == _items.Length)
                EnsureCapacity(_size + 1);

            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }

            _items[index] = item;
            _size++;
            _version++;
        }

        /// <summary>
        /// Removes the element at the given index.
        /// The size of the list is decreased by one.
        /// </summary>
        /// <param name="index"></param>
        public void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException();
            }
            Contract.EndContractBlock();

            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            _items[_size] = default(T);
            _version++;
        }






        #region private methods
        private static bool IsCompatibleObject(object value)
        {
            // Non-null values are fine.
            // Only accept nulls if T is a class or Nullable<U>
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return ((value is T)
                || (value == null && default(T) == null));
        }

        private void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? _defaultCapacity : _items.Length * 2;

                // Allow the list to grow to maximun possible capacity(~2G elements) 
                // before encountering overflow.
                // Note that this check works even when _item.Length overflowed thanks to the (uint) cast.
                if ((uint)newCapacity > 0X7FEFFFFF) //Array.MaxArrayLength is declared as a internal member.
                    newCapacity = 0X7FEFFFFF;

                if (newCapacity < min)
                    newCapacity = min;

                Capacity = newCapacity;
            }
        }
        #endregion

        #region

        public MyList<T> GetRange(int index, int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException();
            if (count < 0)
                throw new ArgumentOutOfRangeException();
            if (_size < index + count)
                throw new ArgumentException();

            Contract.Ensures(Contract.Result<List<T>>() != null);
            Contract.EndContractBlock();

            MyList<T> list = new MyList<T>(count);
            Array.Copy(_items, index, list._items, 0, count);
            list._size = count;
            return list;
        }

        public int LastIndexOf(T item)
        {
            return LastIndexOf(item, _size -1, _size);
        }

        public int LastIndexOf(T item, int index)
        {
            return LastIndexOf(item, index, index + 1);
        }

        /// <summary>
        /// Returns the index of the last occurrence of a given value in a range of this list.
        /// The list is searched backwards, starting at index and upto count elements.
        /// The elements of the list are compared to the given value using the Object.Equals method.
        /// 
        /// This method uses the Array.LastIndexOf method to perform the search.
        /// </summary>
        /// <param name="item"></param>
        /// <param name="index"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        public int LastIndexOf(T item, int index, int count)
        {
            if ((Count != 0) && (index < 0))
            {
                throw new ArgumentOutOfRangeException();
            }
            if ((Count != 0) && (count < 0))
            {
                throw new ArgumentOutOfRangeException();
            }

            Contract.Ensures(Contract.Result<int>() >= -1);
            Contract.Ensures(((Count == 0) && (Contract.Result<int>() == -1)) || ((Count > 0) && (Contract.Result<int>() <= index)));
            Contract.EndContractBlock();

            if (_size == 0)
                return -1;

            if (index >= _size)
                throw new ArgumentOutOfRangeException();

            return Array.LastIndexOf(_items, item, index, count);
        }


        public void Reverse(int index,int count)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException();

            if (count < 0)
                throw new ArgumentOutOfRangeException();

            if (_size < count + index)
                throw new ArgumentException();

            Array.Reverse(_items, index, count);
            _version++;
        }

        public void Sort(int index,int count,IComparer<T> comparer)
        {
            Array.Sort<T>(_items, index, count, comparer);
            _version++;
        }

        public T[] ToArray()
        {
            T[] array = new T[_size];
            Array.Copy(_items, 0, array, 0, _size);
            return array;
        }

        public void TrimExcess()
        {
            int threshold = (int)(((double)_items.Length) * 0.9);
            if (_size < threshold)
                Capacity = _size;
        }


        #endregion


        #region Nested Class/Struct

        [Serializable]
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            private MyList<T> list;
            private int index;
            private int version;
            private T current;

            internal Enumerator(MyList<T> list)
            {
                this.list = list;
                version = list._version;
                index = 0;
                current = default(T);
            }


            public T Current
            {
                get
                {
                    return current;
                }
            }

            object IEnumerator.Current
            {
                get
                {
                    if (index == 0 || index == list._size + 1)
                    {
                        throw new InvalidOperationException();
                    }
                    return Current;
                }
            }

            public void Dispose() { }

            public bool MoveNext()
            {
                MyList<T> localList = list;
                if (version == localList._version && ((uint)index < (uint)localList._size))
                {
                    current = localList._items[index];
                    index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (version != list._version)
                {
                    throw new InvalidOperationException();
                }
                index = list._size + 1;
                current = default(T);
                return false;
            }

            public void Reset()
            {
                if (version != list._version)
                    throw new InvalidOperationException();

                index = 0;
                current = default(T);
            }
        }

        #endregion



    }
}
