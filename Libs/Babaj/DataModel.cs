using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Nox.Libs;

namespace Nox.Libs.Data.Babaj
{
    public class DataModel : IDisposable
    {
        public readonly string ConnectionString;

        private Cache<TableDescriptor> TableDesciptors = new Cache<TableDescriptor>();
        
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

        private void CacheAttributes(Type ClassType)
        {
            var assembly = GetType().Assembly;
            var self = assembly.GetTypes().Where(f => this.GetType().IsSubclassOf(typeof(DataModel))).FirstOrDefault();

            // babaj needs an implementation of the datamodel class
            if (self == null)
                throw new Exception("datamodel not implemented");

            // check if namespace of types is subordinated to datamodel
            foreach (var item in assembly.GetTypes())
                if (item.BaseType.IsGenericType && item.BaseType.GetGenericTypeDefinition() == typeof(DataTable<>))
                    if (item.Namespace.IsLike(self.Namespace))
                    {
                        string Key = $"{item.Namespace}.{item.Name}";
                        if (!TableDesciptors.CacheValueExists(Key))
                        {
                            var TableDescriptorItem = new TableDescriptor(item.Name);

                            TableDescriptorItem.TableSource = item.GetCustomAttribute<TableAttribute>()?.TableSource ?? "";

                            var RowType = item.BaseType.GetGenericArguments().FirstOrDefault();
                            if (RowType != null)
                                foreach (var property in RowType.GetProperties())
                                {
                                    var PropertyDescriptorItem = new PropertyDescriptor(property);

                                    foreach (var attribute in property.GetCustomAttributes())
                                    {
                                        // check primarykey status
                                        if (typeof(PrimaryKeyAttribute).IsAssignableFrom(attribute.GetType()))
                                            PropertyDescriptorItem.IsPrimaryKey = true;

                                        // check if required
                                        if (typeof(Required).IsAssignableFrom(attribute.GetType()))
                                            PropertyDescriptorItem.IsRequired = true;

                                        // get table source field
                                        if (typeof(ColumnAttribute).IsAssignableFrom(attribute.GetType()))
                                        {
                                            PropertyDescriptorItem.Source = (attribute as ColumnAttribute).Source;
                                            PropertyDescriptorItem.MappingDescriptor = new ColumnMappingDescriptor(property);

                                            TableDescriptorItem.Add(PropertyDescriptorItem);
                                        }
                                    }
                                }

                            TableDesciptors.SetCacheValue(Key, () => TableDescriptorItem);
                        }
                    }
        }

        public TableDescriptor GetTableDescriptor(string Key) =>
            TableDesciptors.GetCacheValue(Key);

        public T CreateInstance<T>(Type type) where T : DataObjectBase
                => (T)Activator.CreateInstance(type, this);

        public DataModel(string ConnectionString)
        {
            CacheAttributes(this.GetType());
            this.ConnectionString = ConnectionString;
        }

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                    TableDesciptors.Dispose();

                disposedValue = true;
            }
        }

        public void Dispose() =>
            Dispose(true);
        #endregion
    }




}
