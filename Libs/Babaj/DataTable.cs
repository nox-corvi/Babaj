﻿using System;
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
    public abstract class DataTable : DataObjectBase
    {
        public string _DatabaseTableSource;

        public string _DatabasePrimaryKeyField;

        #region Properties
        public string Name { get; }

        public string TableSource { get; protected set; }
        public string PrimeryKeyField { get; protected set; }
        #endregion
        public DataTable(DataModel dataModel) :
            base(dataModel)
        {
            // assume attribute exists ... 
            //var PrimaryKeyColAttributes = dataModel.GetPropertyTypeAttributes<DbPrimaryKeyAttribute>(GetType()).First();
            //DatabaseTableSource = dataModel.GetClassTypeAttributes<DbTableAttribute>(GetType()).First().TableSource;
            //DatabasePrimaryKeyField = dataModel.GetPropertyTypeAttributes<PrimaryKeyAttribute>(GetType()).First();
        }
    }

    public abstract class DataTable<T> : DataTable where T : DataRow
    {
        protected Operate<T> _Operate;

        #region Properties
        #endregion

        public DataRowColl<T> Get(string Where, params KeyValuePair<string, string>[] Parameters)
        {
            var Result = _Operate.Load(Where, Parameters.Select(f => new SqlParameter(f.Key, f.Value)).ToArray());

            return Result;
        }

        public T GetWhereId(Guid Id) => Get("id = @id",
            new KeyValuePair<string, string>("id", Id.ToString())).FirstOrDefault();

        public void Update(T r)
        {
            if (r.IsAdded)
                _Operate.Insert(r);
            else if (r.IsModified)
                _Operate.Update(r);
        }

        public void Update(DataRowColl<T> rc)
        {
            _Operate.BeginTransaction();

            try
            {
                for (int i = 0; i < rc.Count; i++)
                    Update(rc[i]);

                _Operate.Commit();
            }
            catch (Exception)
            {
                //Log.LogException(e);
                _Operate.Rollback();

                throw;
            }
        }

        public void Delete(T r)
        {
            if (r.IsDeleted)
                _Operate.Delete(r);
        }

        public void Delete(DataRowColl<T> rc)
        {
            _Operate.BeginTransaction();

            try
            {
                for (int i = 0; i < rc.Count; i++)
                    Delete(rc[i]);

                _Operate.Commit();
            }
            catch (Exception)
            {
                //Log.LogException(e);
                _Operate.Rollback();

                throw;
            }
        }


        /// <summary>
        /// create a new row an attach it to this datatable
        /// </summary>
        /// <returns></returns>
        public T NewRow()
        {
            var r = (T)Activator.CreateInstance(typeof(T), dataModel);

            r.dataTable = this;

            return r;
        }

        public DataTable(DataModel dataModel) :
            base(dataModel)
        {
            //this.PrimeryKeyField = Collector.PropertyAttributes.Find(f => f.IsPrimaryKeyColumn)?.Name;
            //this.TableSource = Collector.TableAttributes.FirstOrDefault()?.TableSource;

            _Operate = new Operate<T>(dataModel, (DataTable)this);
        }
    }
}