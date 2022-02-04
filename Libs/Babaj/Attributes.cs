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
    /// <summary>
    /// assign database table to object
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TableAttribute : Attribute
    {
        public string TableSource { get; set; }

        public TableAttribute(string TableSource)
            => this.TableSource = TableSource;
    }

    /// <summary>
    /// assign database column to property or field
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnAttribute : Attribute
    {
        public string Source { get; set; }

        public SqlDbType Type { get; set; }

        public ColumnAttribute(string Source, SqlDbType Type)
        {
            this.Source = Source;
            this.Type = Type;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnGuidAttribute : ColumnAttribute
    {
        public ColumnGuidAttribute(string Source)
            : base(Source, SqlDbType.UniqueIdentifier) { }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnStringAttribute : ColumnAttribute
    {
        public int MaxLength { get; set; } = -1;

        public ColumnStringAttribute(string Source, int MaxLength) 
            : base(Source, SqlDbType.NVarChar) =>
            this.MaxLength = MaxLength;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class ColumnDecimalAttribute : ColumnAttribute
    {
        public int Precission { get; set; } = 18;
        public int Scale { get; set; } = 0;

        public ColumnDecimalAttribute(string Source)
            : base(Source, SqlDbType.Decimal) { }

        public ColumnDecimalAttribute(string Source, int Precission) 
            : base(Source, SqlDbType.Decimal) =>
            this.Precission = Precission;

        public ColumnDecimalAttribute(string Source, int Precission, int Scale)
            : this(Source, Precission) => this.Scale = Scale;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class ColumnDateTimeAttribute : ColumnAttribute
    {
        public bool WideRange { get; set; } = false;

        public ColumnDateTimeAttribute(string Source, bool WideRange) 
            : base(Source, WideRange ? SqlDbType.DateTime : SqlDbType.DateTime2) =>
            this.WideRange = WideRange;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class PrimaryKeyAttribute : RequiredAttribute { }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class Index : Attribute
    {
        public string Name { get; set; }

        public bool IsUnique { get; set; } = false;

        public Index(string Name)
            => this.Name = Name;

        public Index(string Name, bool IsUnique)
            : this(Name) =>
            this.IsUnique = IsUnique;
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class Identity : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class Required : RequiredAttribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class NotNull : Attribute { }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class RelationAttribute : Attribute
    {
        public Type RelatedDataModel { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public string ForeignKey { get; set; }

        public RelationAttribute(Type RelatedDataModel, string ForeignKeys)
        {
            this.RelatedDataModel = RelatedDataModel;
            this.ForeignKey = ForeignKey;
        }
    }

}
