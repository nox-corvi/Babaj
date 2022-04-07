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
    public class Operate : DataObjectBase
    {
        protected int _SqlCommandTimeout = 300;

        protected SqlConnection _DatabaseConnection;
        protected SqlTransaction _Transaction;

        #region Database Operations
        /// <summary>
		/// ensure that the database connection object is created and has a valid state. the connection will not open
		/// </summary>
        protected void EnsureConnectionEstablished()
        {
            if (_DatabaseConnection == null)
                _DatabaseConnection = new SqlConnection(ConnectionString);

            switch (_DatabaseConnection.State)
            {
                case ConnectionState.Broken:
                    // dispose and create new ...
                    _DatabaseConnection.Dispose();
                    _DatabaseConnection = new SqlConnection(ConnectionString);

                    break;
                case ConnectionState.Open:
                    // keep connection open .. 

                    break;
                case ConnectionState.Closed:
                    break;

                case ConnectionState.Connecting:
                case ConnectionState.Executing:
                case ConnectionState.Fetching:
                    // already in use, quit

                    throw new InvalidOperationException("connection already in use");
            }
        }

        /// <summary>
        /// open the database connection 
        /// </summary>
        protected void OpenDatabaseConnection()
        {
            EnsureConnectionEstablished();

            if (_DatabaseConnection.State != ConnectionState.Open)
                _DatabaseConnection.Open();
        }

        /// <summary>
        /// close the database connection. the connection object will retain
        /// </summary>
        protected void CloseDatabaseConnection()
        {
            if (_DatabaseConnection != null)
                switch (_DatabaseConnection.State)
                {
                    case ConnectionState.Broken:
                        // he's dead jim .. 

                        break;
                    case ConnectionState.Open:
                        _DatabaseConnection.Close();

                        break;
                    case ConnectionState.Closed:
                        break;
                    case ConnectionState.Connecting:
                    case ConnectionState.Executing:
                    case ConnectionState.Fetching:
                        // already in use, quit
                        throw new InvalidOperationException("connection already in use");
                }
            else
                return;
        }

        /// <summary>
        /// ensures the database connection is established and starts a transaction
        /// </summary>
        public void BeginTransaction()
        {
            EnsureConnectionEstablished();
            OpenDatabaseConnection();

            if (_Transaction == null)
                _Transaction = _DatabaseConnection.BeginTransaction();
        }

        /// <summary>
        /// rolls back if a transaction is running. otherwise do nothing
        /// </summary>
        public void Rollback()
        {
            if (_Transaction != null)
            {
                _Transaction.Rollback();
                _Transaction = null;
            }
        }

        /// <summary>
        /// commits a transaction if running. otherwise do nothing
        /// </summary>
        public void Commit()
        {
            _Transaction?.Commit();
            _Transaction = null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2100:SQL-Abfragen auf Sicherheitsrisiken überprüfen")]
        public SqlDataReader GetReader(string SQL, CommandType commandType = CommandType.Text, params SqlParameter[] Parameters)
        {
            EnsureConnectionEstablished();

            SqlCommand CMD = new SqlCommand(SQL, _DatabaseConnection)
            {
                CommandType = commandType,
                CommandTimeout = _SqlCommandTimeout,

                Transaction = _Transaction
            };

            OpenDatabaseConnection();

            if (Parameters != null)
                foreach (SqlParameter Param in Parameters)
                    CMD.Parameters.Add(Param);

            return CMD.ExecuteReader(CommandBehavior.CloseConnection);
        }

        public SqlDataReader GetReader(string SQL, params SqlParameter[] Parameters) =>
            GetReader(SQL, CommandType.Text, Parameters);

        public SqlDataReader GetReader(string SQL) =>
            GetReader(SQL, CommandType.Text, null);

        public long Execute(string SQL, CommandType commandType = CommandType.Text, params SqlParameter[] Parameters)
        {
            EnsureConnectionEstablished();

            using (SqlCommand CMD = new SqlCommand(SQL, _DatabaseConnection)
            {
                CommandType = commandType,
                CommandTimeout = _SqlCommandTimeout,

                Transaction = _Transaction
            })
            {
                OpenDatabaseConnection();

                if (Parameters != null)
                    foreach (SqlParameter Param in Parameters)
                        CMD.Parameters.Add(Param);

                var Result = CMD.ExecuteNonQuery();
                return Result;
            }
        }

        public long Execute(string SQL, params SqlParameter[] Parameters) =>
            Execute(SQL, CommandType.Text, Parameters);

        public long Execute(string SQL) =>
            Execute(SQL, CommandType.Text, null);

        public K GetValue<K>(string SQL, CommandType commandType, SqlParameter[] Parameters, K DefaultValue = default(K)) where K : IComparable
        {
            using (var Reader = GetReader(SQL, commandType, Parameters))
                if (Reader.Read())
                {
                    try
                    {
                        return Helpers.N<K>(Reader.GetFieldValue<K>(0), DefaultValue);
                    }
                    catch (Exception)
                    {
                        return DefaultValue;
                    }
                }

            return DefaultValue;
        }

        public K GetValue<K>(string SQL, SqlParameter[] Parameters, K DefaultValue = default(K)) where K : IComparable =>
            GetValue<K>(SQL, CommandType.Text, Parameters, DefaultValue);

        public K GetValue<K>(string SQL, K DefaultValue = default(K)) where K : IComparable =>
            GetValue<K>(SQL, CommandType.Text, null, DefaultValue);

        public K GetValue<K>(string SQL) where K : IComparable =>
            GetValue<K>(SQL, CommandType.Text, null, default(K));
        #endregion

        public Operate(DataModel dataModel)
            : base(dataModel)
        {

        }

        #region IDisposable Support
        private bool disposedValue = false; // Dient zur Erkennung redundanter Aufrufe.

        protected override void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Rollback();
                    CloseDatabaseConnection();

                    _DatabaseConnection?.Dispose();
                }
            }

            base.Dispose();
        }
        #endregion
    }

    /// <summary>
    /// Stellt Werkzeuge zur Datenmanipulation zur Verfügung
    /// </summary>
    public class Operate<T> : Operate where T : DataRow
    {
        private Guid ObjectId { get; } = Guid.NewGuid();

        protected readonly DataTable dataTable;

        protected TableDescriptor _TableDescriptor;
        private PropertyDescriptor _PrimaryKeyPropertyDescriptor;

        protected string _DatabaseTableSource;
        protected string _DatabasePrimaryKeyField;


        private DatabaseRelationDescriptor[] _RelationDescriptors;

        #region Properties
        //public string TableSource =>
        //    dataTable.TableSource;

        public string DatabasePrimaryKey =>
            _PrimaryKeyPropertyDescriptor.Source;

        //public ColumnMappingDescriptor[] MappingDescriptors =>
        //    _MappingDescriptors;

        public PropertyDescriptor PrimaryKeyPropertyDescriptor =>
            _PrimaryKeyPropertyDescriptor;
        #endregion

        #region Stmt
        private string CreateSchemaSelect =>
            $"SELECT TOP 0 * FROM {_DatabaseTableSource}";

        private string CreateKeySelect =>
            $"SELECT * FROM {_DatabaseTableSource} WHERE {DatabasePrimaryKey} = @{DatabasePrimaryKey}";

        private string CreateExistsSelect(string SubSelect) =>
            $"SELECT CASE WHEN EXISTS ({SubSelect}) THEN 1 ELSE 0 END";

        private string CreateSelectStmt(string Where) =>
           $"SELECT * FROM {dataTable.TableSource} WHERE {Where}";

        private string CreateSelectStmt(string Where, List<string> FieldList)
        {
            StringBuilder Fields = new StringBuilder();

            for (int i = 0; i < FieldList.Count; i++)
                Fields.Append(i > 0 ? ", " : "").Append(FieldList[i]);

            return $"SELECT {Fields.ToString()} FROM {_DatabaseTableSource} WHERE ";
        }

        private string CreateInsertStmt(List<string> FieldList)
        {
            StringBuilder Fields = new StringBuilder(), Values = new StringBuilder();

            for (int i = 0; i < FieldList.Count; i++)
            {
                Fields.Append(i > 0 ? ", " : "").Append(FieldList[i]);
                Values.Append(i > 0 ? ", " : "").Append("@" + FieldList[i]);
            }

            return $"INSERT INTO {_DatabaseTableSource}({Fields.ToString()}) VALUES({Values.ToString()})";
        }

        private string CreateUpdateStmt(List<string> FieldList)
        {
            StringBuilder FieldValuePair = new StringBuilder();

            for (int i = 0; i < FieldList.Count; i++)
                FieldValuePair.Append(i > 0 ? ", " : "").Append(FieldList[i] + " = @" + FieldList[i]);

            return $"UPDATE {_DatabaseTableSource} SET {FieldValuePair.ToString()} WHERE {DatabasePrimaryKey} = @{DatabasePrimaryKey}";
        }
        private string CreateDeleteStmt =>
            $"DELETE FROM {_DatabaseTableSource} WHERE {DatabasePrimaryKey} = @{DatabasePrimaryKey}";
        #endregion

        #region Database Operations
        public bool Exists(string SQL, CommandType commandType = CommandType.Text, params SqlParameter[] Parameters) =>
            GetValue<bool>(CreateExistsSelect(SQL), commandType, Parameters);

        public bool Exists(string SQL, params SqlParameter[] Parameters) =>
            Exists(SQL, CommandType.Text, Parameters);


        public bool Exists(string SQL) =>
            Exists(SQL, CommandType.Text, null);
        #endregion

        private Guid IdPropertyValue(T row) =>
            row.GetPropertyValue<Guid>(_PrimaryKeyPropertyDescriptor.Property);

        public void Insert(T row)
        {
            var KeyFieldValue = row.GetPropertyValue<Guid>(_PrimaryKeyPropertyDescriptor.Property);

            if (!Exists(CreateKeySelect, new SqlParameter($"@{DatabasePrimaryKey}", KeyFieldValue)))
            {
                var Fields = new List<string>();
                var Params = new List<SqlParameter>();

                // add key
                Fields.Add(_PrimaryKeyPropertyDescriptor.Name);
                Params.Add(new SqlParameter($"@{_PrimaryKeyPropertyDescriptor.Source}", KeyFieldValue));

                // add data
                foreach (var pd in _TableDescriptor.Where(f => !f.IsPrimaryKey))
                {
                    var md = pd.MappingDescriptor;
                    var cd = md.CastDescriptor;

                    Fields.Add(pd.Source);

                    var sqlParam = new SqlParameter($"@{pd.Source}", cd.TargetType);

                    var value = row.GetPropertyValue(pd.Property);
                    if (value == null)
                        sqlParam.Value = DBNull.Value;
                    else
                        sqlParam.Value = value;

                    Params.Add(sqlParam);
                }

                // and go ... 
                Execute(CreateInsertStmt(Fields), Params.ToArray());
            }
            else
                throw new Exception("row already exists");
        }

        public void Update(T row)
        {
            // get primary key of row ...
            var KeyFieldValue = row.GetPropertyValue<Guid>(_PrimaryKeyPropertyDescriptor.Property);

            // test is row exists ... 
            if (Exists(CreateKeySelect, new SqlParameter($"@{DatabasePrimaryKey}", KeyFieldValue)))
            {
                var Fields = new List<string>();
                var Params = new List<SqlParameter>();

                foreach (var pd in _TableDescriptor.Where(f => !f.IsPrimaryKey))
                {
                    var md = pd.MappingDescriptor;
                    var cd = md.CastDescriptor;

                    Fields.Add(pd.Source);

                    var sqlParam = new SqlParameter($"@{pd.Source}", cd.TargetType);

                    var value = row.GetPropertyValue(pd.Property);
                    if (value == null)
                        sqlParam.Value = DBNull.Value;
                    else
                        sqlParam.Value = value;

                    Params.Add(sqlParam);
                }

                // add parameter used in where-condition ... 
                Params.Add(new SqlParameter($"@{_PrimaryKeyPropertyDescriptor.Name}", KeyFieldValue));

                // and go ... 
                Execute(CreateUpdateStmt(Fields), Params.ToArray());
            }
            else
                throw new Exception("row not found");
        }

        public void Delete(DataRow r)
        {
            var KeyFieldValue = r.GetPropertyValue<Guid>(_PrimaryKeyPropertyDescriptor.Property);

            Execute(CreateDeleteStmt, new SqlParameter($"@{_PrimaryKeyPropertyDescriptor.Name}", KeyFieldValue));
        }

        public void Schema()
        {
            EnsureConnectionEstablished();
            OpenDatabaseConnection();
        }

        public DataRowColl<T> Load(string WhereCondition, params SqlParameter[] Parameters)
        {
            var Result = (DataRowColl<T>)Activator.CreateInstance(typeof(DataRowColl<T>), dataModel, dataTable);

            using (var r = GetReader(CreateSelectStmt(WhereCondition), Parameters))
                while (r.Read())
                {
                    var NewRow = DataRow.New<T>(dataModel);

                    NewRow.dataTable = dataTable;

                    // add primary key ..
                    _PrimaryKeyPropertyDescriptor.Property.SetValue(NewRow, (r.GetValue(r.GetOrdinal(_PrimaryKeyPropertyDescriptor.Name))));

                    // add data ... 
                    foreach (var md in _TableDescriptor.Where(f => !f.IsPrimaryKey).Select(f => f.MappingDescriptor))
                    {
                        var data = r.GetValue(r.GetOrdinal(md.Name));

                        // Test if DBNull, use null instead ...
                        if (!Convert.IsDBNull(data))
                            md.Property.SetValue(NewRow, data);
                        else
                            md.Property.SetValue(NewRow, null);
                    }
                    NewRow.AcceptChanges();

                    Result.Add(NewRow);
                }

            return Result;
        }

        //private void BuildAttributes()
        //{
        //    var TypeAttributes = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        //    var MappingDescriptors = new List<ColumnMappingDescriptor>();
        //    var RelationDescriptors = new List<DatabaseRelationDescriptor>();
        //    foreach (var item in TypeAttributes)

        //    {
        //        // Check if Property has a PrimaryKeyAttribute
        //        var DatabaseColumnAttribute = item.GetCustomAttribute<ColumnAttribute>();
        //        if (DatabaseColumnAttribute != null)
        //        {
        //            if (DatabaseColumnAttribute.Source.Equals(DatabasePrimaryKey, StringComparison.InvariantCultureIgnoreCase))
        //                this._PrimaryKeyPropertyDescriptor = new PrimaryKeyDescriptor(item);
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

        //    this._MappingDescriptors = MappingDescriptors.ToArray();
        //}

        public Operate(DataModel dataModel, DataTable dataTable) :
            base(dataModel)
        {
            this.dataTable = dataTable;

            var data = dataTable.GetType();
            string Key = $"{data.Namespace}.{data.Name}";

            _TableDescriptor = dataModel.GetTableDescriptor(Key);

            _DatabaseTableSource = _TableDescriptor.TableSource;
            _DatabasePrimaryKeyField = _TableDescriptor.Where(f => f.IsPrimaryKey).FirstOrDefault()?.Source;

            _PrimaryKeyPropertyDescriptor = _TableDescriptor.Where(f => f.IsPrimaryKey).FirstOrDefault();
        }
    }
}
