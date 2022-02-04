using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection.Emit;
using System.Reflection;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.ComponentModel.Design.Serialization;
using Nox.Libs;
using System.ComponentModel.DataAnnotations;

namespace Nox.Libs.Data.Babaj
{

    [Flags]
    public enum DataRowStateEnum
    {
        Detached = 1,
        Added = 2,
        Deleted = 3,
        Modified = 8,
    }

    //public class DataRowEventArgs : EventArgs
    //{
    //    public DataRow dataRow { get; set; }

    //    public DataRowEventArgs(DataRow dataRow) :
    //        base()
    //    {
    //        this.dataRow = dataRow;
    //    }
    //}
    //public class DataRowCancelEventArgs : CancelEventArgs
    //{
    //    public DataRow dataRow { get; set; }

    //    public DataRowCancelEventArgs(DataRow dataRow) :
    //        base()
    //    {
    //        this.dataRow = dataRow;
    //    }
    //}

    //public delegate void DataRowCancelEventHandler(object sender, DataRowCancelEventArgs e);
    //public delegate void DataRowEventHandler(object sender, DataRowEventArgs e);

    public abstract class DataRow : DataObjectBase
    {
        private DataTable _dataTable;

        //public event DataRowCancelEventHandler BeforeAttachRow;
        //public event DataRowEventHandler AfterAttachRow;

        //public event DataRowCancelEventHandler BeforeDetachRow;
        //public event DataRowEventHandler AfterDetachRow;

        //public event DataRowCancelEventHandler BeforeInsertRow;
        //public event DataRowEventHandler AfterInsertRow;

        //public event DataRowCancelEventHandler BeforeUpdateRow;
        //public event DataRowEventHandler AfterUpdateRow;

        //public event DataRowCancelEventHandler BeforeDeleteRow;
        //public event DataRowEventHandler AfterDeleteRow;


        public T GetRelation<T>()
        {
            return default(T);
        }


        #region Properties
        public DataTable dataTable
        {
            get => _dataTable;
            set 
            {

                if (_dataTable != value)
                {
                    SetProperty(ref _dataTable, value);

                    // set detached-flag
                    if (value != null)
                    {
                        DataRowState &= (~DataRowStateEnum.Detached);
                        DataRowState |= DataRowStateEnum.Added;
                    }
                    else
                    {
                        DataRowState |= DataRowStateEnum.Detached;
                        DataRowState &= (~DataRowStateEnum.Added);
                    }


                }
            }
        }

        public DataRowStateEnum DataRowState { get; private set; } = DataRowStateEnum.Detached;

        public bool IsDetachted =>
            ((DataRowState & DataRowStateEnum.Detached) == DataRowStateEnum.Detached);

        public bool IsAdded =>
            ((DataRowState & DataRowStateEnum.Added) == DataRowStateEnum.Added);

        public bool IsDeleted =>
            ((DataRowState & DataRowStateEnum.Deleted) == DataRowStateEnum.Deleted);

        public bool IsModified =>
            ((DataRowState & DataRowStateEnum.Modified) == DataRowStateEnum.Modified);
        #endregion

        /// <summary>
        /// Wird ausgelöst, wenn eine Zeile neu der DataTable hinzugefügt wird.
        /// </summary>
        public virtual void OnBeforeInsertRow()
        {

        }

        /// <summary>
        /// Wird ausgelöst, wenn eine vorhandene Zeile in der DataTable geändert wird.
        /// </summary>
        public virtual void OnBeforeUpdateRow()
        {

        }

        /// <summary>
        /// Wird ausgelöst, wenn eine vorhandene Zeile in der DataTable gelöscht wird.
        /// </summary>
        public virtual void OnBeforeDeleteRow()
        {

        }

        public virtual void OnBeforeAttachRow()
        {

        }

        public virtual void OnDetachRow()
        {

        }

        #region Helpers
        //public void PropertyChange([CallerMemberName] string PropertyName = "") =>
        //    PropertyChanged.Invoke(this, new PropertyChangedEventArgs(PropertyName));

        //public void SetPropertyValue<T>(ref T TargetVar, T value, [CallerMemberName] string PropertyName = "") where T : IComparable
        //{
        //    if (TargetVar?.CompareTo(value) != 0)
        //    {
        //        // Assign 
        //        TargetVar = value;

        //        // Notify
        //        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
        //    }
        //}

        public object GetPropertyValue(PropertyInfo info)
        {
            if (info != null)
                return info.GetValue(this, null);
            else
                return null;
        }

        public object GetPropertyValue(string PropertyName) =>
            GetPropertyValue(this.GetType().GetProperty(PropertyName));

        public T GetPropertyValue<T>(PropertyInfo info)
        {
            var value = GetPropertyValue(info);
            if (value != null)
                if (!Convert.IsDBNull(value))
                {
                    /* error if try to convert double to float, use invariant cast from String!!!
                     * http://stackoverflow.com/questions/1667169/why-do-i-get-invalidcastexception-when-casting-a-double-to-decimal
                     */
                    if (typeof(T) == typeof(double))
                        return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromInvariantString(value.ToString());
                    else
                        return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFromString(value.ToString());
                }
                else
                    return default(T);
            else
                return default(T);
        }

        public T GetPropertyValue<T>(string PropertyName) =>
            GetPropertyValue<T>(this.GetType().GetProperty(PropertyName));
        #endregion

        /// <summary>
        /// Akzeptiert die Änderungen und setzt den RowState auf Unchanged zurück. 
        /// </summary>
        public void AcceptChanges()
        {
            if ((DataRowState & DataRowStateEnum.Detached) == DataRowStateEnum.Detached)
                throw new RowNotInTableException();

            DataRowState &= (~(DataRowStateEnum.Modified | DataRowStateEnum.Added));
        }

        public static T Get<T>(DataModel dataModel, Guid Id) where T : DataRow
        {
            var Result = (T)Activator.CreateInstance(typeof(T), dataModel);

            return Result;
        }

        public static T New<T>(DataModel dataModel) where T : DataRow =>
            (T)Activator.CreateInstance(typeof(T), dataModel);

        public DataRow(DataModel dataModel)
            : base(dataModel) =>
            PropertyChanged += (object sender, PropertyChangedEventArgs e) => DataRowState |= DataRowStateEnum.Modified;
    }

    public class DataRowColl<T> : DataObjectBase, IList<T>, IEnumerable<T>, INotifyCollectionChanged where T : DataRow
    {
        private DataTable _dataTable;
        private List<T> _Data = new List<T>();

        #region Properties
        public DataTable dataTable { get => _dataTable; set => _dataTable = value; }

        public T this[int index] { get => ((IList<T>)_Data)[index]; set => ((IList<T>)_Data)[index] = value; }

        public int Count => ((IList<T>)_Data).Count;

        public bool IsReadOnly => ((IList<T>)_Data).IsReadOnly;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        #endregion

        #region Collection Methods
        public void Add(T item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>() { item }));

            item.dataTable = dataTable;
            _Data.Add(item);
        }

        public void Clear()
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, this));

            _Data.Clear();
        }

        public bool Contains(T item) =>
            _Data.Contains(item);

        public void CopyTo(T[] array, int arrayIndex) =>
            _Data.CopyTo(array, arrayIndex);

        public IEnumerator<T> GetEnumerator() =>
            _Data.GetEnumerator();

        public int IndexOf(T item) =>
            _Data.IndexOf(item);

        public void Insert(int index, T item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>() { item }, index));

            _Data.Insert(index, item);
        }

        public bool Remove(T item)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T>() { item }));

            return _Data.Remove(item);
        }

        public void RemoveAt(int index)
        {
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, new List<T>() { this[index] }, index));

            _Data.RemoveAt(index);
        }

        IEnumerator IEnumerable.GetEnumerator() =>
            _Data.GetEnumerator();
        #endregion

        public DataRowColl(DataModel dataModel, DataTable dataTable)
            : base(dataModel)
        {
            this._dataTable = dataTable;
        }
    }

}
