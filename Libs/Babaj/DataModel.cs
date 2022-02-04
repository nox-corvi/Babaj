using Nox.Libs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Nox.Libs.Data.Babaj
{
    public class DataModel : IDisposable
    {
        public readonly string ConnectionString;

        private Cache<Attribute> Attributes = new Cache<Attribute>();
        private Cache<ColumnCastDescriptor> CastDescriptor = new Cache<ColumnCastDescriptor>();

        #region Properties
        public virtual bool AutoValidateOnStartup => false;
        #endregion

        #region Validate
        
        
        #endregion

        #region Analyse

        #endregion

        #region Cache Methods

        //public AttributesCollector GetAttributes(Type TableClassType, Type RowClassType, string Key) 
        //{
        //    AttributesCollector ReturnValue;

        //    if (!AttributesCollectorCache.TryGetValue(Key, out ReturnValue))
        //    {
        //        ReturnValue = new AttributesCollector(TableClassType);

        //        // Add TableAttributes
        //        ReturnValue.TableAttributes.AddRange(TableClassType.GetCustomAttributes<TableAttribute>());

        //        foreach (var item in RowClassType.GetProperties())
        //        {
        //            var PA = new PropertyAttributes(item);

        //            foreach (var attribute in item.GetCustomAttributes())
        //            {
        //                if (typeof(PrimaryKeyAttribute).IsAssignableFrom(attribute.GetType()))
        //                    PA.IsPrimaryKeyColumn = true;

        //                if (typeof(Required).IsAssignableFrom(attribute.GetType()))
        //                    PA.IsRequired = true;


        //                if (typeof(ColumnAttribute).IsAssignableFrom(attribute.GetType()))
        //                    PA.Attributes.Add((ColumnAttribute)attribute);
        //            }

        //            if (PA.Attributes.Count > 0)
        //                ReturnValue.PropertyAttributes.Add(PA);
        //        }


        //        // finally add
        //        AttributesCollectorCache.Add(Key, ReturnValue);
        //    }

        //    return ReturnValue;
        //}


        //private AttributesCollector MakeAttributes<T, U>() where T : DataTable<U> where U : DataRow
        //{
        //    var t = typeof(T);

        //    //
        //    DatabaseTableSource = ((TableAttribute)(GetType().GetCustomAttributes(typeof(TableAttribute)).First())).TableSource
        //    var Properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        //    //var MappingDescriptors = new List<ColumnMappingDescriptor>();
        //    //var RelationDescriptors = new List<DatabaseRelationDescriptor>();
        //    foreach (var item in Properties)
        //    {
        //        // Check if Property has a PrimaryKeyAttribute
        //        var DatabaseColumnAttribute = item.GetCustomAttribute<ColumnAttribute>();
        //        if (DatabaseColumnAttribute != null)
        //        {
        //            if (DatabaseColumnAttribute.Source.Equals(DatabasePrimaryKey, StringComparison.InvariantCultureIgnoreCase))
        //                this._PrimaryKeyMappingAttribute = new PrimaryKeyDescriptor(item);
        //            else
        //            {
        //                var MappingDescriptor = new ColumnMappingDescriptor(item);

        //                MappingDescriptors.Add(MappingDescriptor);
        //            }
        //        }

        //        var RelationAttribute = item.GetCustomAttribute<RelationAttribute>();
        //        if (RelationAttribute != null)
        //        {
        //            var RelationDescriptor = new DatabaseRelationDescriptor(item)
        //            {
        //                RelatedDataModel = RelationAttribute.RelatedDataModel,
        //                ForeignKey = new ColumnMappingDescriptor(item)
        //                {
        //                    CastDescriptor = ColumnCastDescriptor.From(item)
        //                }
        //            };
        //        }
        //    }
        //}
        #endregion

        private void GetDataClasses()
        {
            var DataTypes = new List<Type>();

            var assembly = GetType().Assembly;
            foreach (var item in assembly.GetTypes())
                if (item.BaseType.IsGenericType && item.BaseType.GetGenericTypeDefinition() == typeof(DataTable<>))
                    CacheAttributes(item);
        }
        public void CacheAttributes(Type ClassType)
        {
            string root = ClassType.Name;

            // store items in <Class>.<Attribute>#<Type>
            foreach (var item in ClassType.GetCustomAttributes())
                Attributes.SetCacheValue($"{root}#{item.GetType().Name}", () => item);

            if (ClassType.BaseType.IsGenericType && ClassType.BaseType.GetGenericTypeDefinition() == typeof(DataTable<>))
            {
                var RowType = ClassType.BaseType.GetGenericArguments().FirstOrDefault();
                if (RowType != null)
                    foreach (var item in RowType.GetProperties())
                        foreach (var attribute in item.GetCustomAttributes())
                            Attributes.SetCacheValue($"{root}.{item.Name}#{attribute.GetType().Name}", () => attribute);
            }
        }

        public T CreateInstance<T>(Type type) where T : DataObjectBase
                => (T)Activator.CreateInstance(type, this);

        public DataModel(string ConnectionString)
        {
            GetDataClasses();
            
            this.ConnectionString = ConnectionString;
        }

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    Attributes.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose() =>
            Dispose(true);
        #endregion
    }




}
