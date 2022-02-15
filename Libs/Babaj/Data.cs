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
using Microsoft.Toolkit.Mvvm.ComponentModel;

namespace Nox.Libs.Data.Babaj
{
    public class DataObjectBase : ObservableObject, IDisposable
    {
        protected readonly DataModel dataModel;

        #region Properties
        public string ConnectionString =>
            dataModel.ConnectionString;
        #endregion
        
        public DataObjectBase(DataModel dataModel) =>
            this.dataModel = dataModel;

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                }

                disposedValue = true;
            }
        }

        public void Dispose() =>
            Dispose(true);
        #endregion

        //private string BuildSqlSelect(string Table) =>
        //    $"SELECT * FROM {_db_prefix}{Table} WHERE ID = @ID";

        //private string BuildSqlInsert(string Table, params string[] Fields) =>
        //    $"INSERT INTO {_db_prefix}{Table}(ID, {Fields.Aggregate((c, n) => c + ", " + n)} " +
        //    $"VALUES(@ID, {Fields.Aggregate((c, n) => c + ", @" + n)})";

        //private string BuildSqlUpdate(string Table, params string[] Fields) =>
        //    $"UPDATE {_db_prefix}{Table} SET {Fields.Aggregate((c, n) => c + ", " + n + " = @" + n)} " +
        //    $"WHERE ID = @ID";

        //private string BuildSqlDelete(string Table, Guid Id) =>
        //    $"DELETE FROM {_db_prefix}{Table} WHERE ID = @ID";

        //protected DataObjectBase Load(string Table, Guid Id, Func<SqlDataReader, DataObjectBase> assign)
        //{
        //    using (var r = _dba.GetReader(BuildSqlSelect(Table),
        //        new SqlParameter("@ID", Id)))
        //    {
        //        var Result = assign.Invoke(r);

        //        // assign base properties but always overwrite id
        //        Result.Id = r.GetGuid(r.GetOrdinal("id"));

        //        return Result;
        //    }
        //}

        //protected long Insert(string Table, Guid Id, Func<SqlParameter[]> GetSqlParams)
        //{
        //    var SqlParams = GetSqlParams.Invoke();
        //    var Fields = SqlParams.Select(f => f.ParameterName);

        //    if (Fields.Contains("id", StringComparer.InvariantCultureIgnoreCase))
        //    {
        //        Fields.Append("id");
        //        SqlParams.Append(new SqlParameter("id", Id));
        //    }
        //    else
        //        SqlParams.Where(f => f.ParameterName.Equals(nameof(Id), StringComparison.InvariantCultureIgnoreCase)).First().Value = Id;

        //    return _dba.Execute(BuildSqlInsert(Table, Fields.ToArray()), SqlParams);
        //}

        //protected long Update(string Table, Guid Id, Func<SqlParameter[]> GetSqlParameters)
        //{
        //    var SqlParams = GetSqlParameters.Invoke();
        //    var Fields = SqlParams.Select(f => f.ParameterName);

        //    if (Fields.Contains(nameof(Id), StringComparer.InvariantCultureIgnoreCase))
        //    {
        //        Fields.Append(nameof(Id));
        //        SqlParams.Append(new SqlParameter("id", Id));
        //    }
        //    else
        //        SqlParams.Where(f => f.ParameterName.Equals(nameof(Id), StringComparison.InvariantCultureIgnoreCase)).First().Value = Id;

        //    return _dba.Execute(BuildSqlUpdate(Table, Fields.ToArray()), SqlParams);
        //}

        //protected long Delete(string Table) =>
        //    _dba.Execute(BuildSqlUpdate(Table));

        //public DataObjectBase(string ConnectionString, string DbPrefix = "")
        //{
        //    _dba = new SqlDbAccess(ConnectionString);
        //    _db_prefix = DbPrefix;
        //}
    }

    public class DatabaseRelationDescriptor
    {
        /// <summary>
        /// DataRow派生のPropertyInfo
        /// </summary>
        public PropertyInfo Property { get; set; } = null;

        /// <summary>
        /// 関係の名前
        /// </summary>
        public string Name =>
            Property.Name;

        /// <summary>
        /// リレーションで使用されるターゲットクラスのデータ型
        /// </summary>
        public Type RelatedDataModel { get; set; }

        /// <summary>
        /// ターゲットデータ型の外部キー
        /// </summary>
        public ColumnMappingDescriptor ForeignKey { get; set; }

        public DatabaseRelationDescriptor(PropertyInfo Property) =>
            this.Property = Property;
    }

    

}
