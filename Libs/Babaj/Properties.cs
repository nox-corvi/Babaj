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
    public class PropertyDescriptor
    {
        private bool _IsRequired = default;
        private bool _IsPrimaryKey = default; 

        #region Properties
        public PropertyInfo Property { get; }

        public string Name { get => Property.Name; }


        public string Source { get; set; }

        public ColumnMappingDescriptor MappingDescriptor { get; set; }

        // ----
        public bool IsRequired { get => _IsRequired | _IsPrimaryKey; set => _IsRequired = value; }
        public bool IsPrimaryKey { get => _IsPrimaryKey; set => _IsPrimaryKey = value; }
        #endregion

        public override string ToString() =>
                $"{Name}#{Property.PropertyType.Name}";

        public PropertyDescriptor(PropertyInfo Property) =>
            this.Property = Property;
    }

    public class TableDescriptor :  List<PropertyDescriptor>
    {
        #region Property
        public string Key { get; }
        
        public string TableSource { get; set; }
        #endregion



        public TableDescriptor(string Key) =>
            this.Key = Key;
    }

    public class DataRowAttributesCollector
    {
        public readonly MemberInfo memberInfo;

        public DataRowAttributesCollector(MemberInfo memberInfo) =>
            this.memberInfo = memberInfo;
    }


    public class PropertyAttributes
    {
        #region Properties
        public string Name { get => Property.Name; }

        public PropertyInfo Property { get; set; }

        public bool IsPrimaryKeyColumn { get; set; } = false;

        public bool IsRequired { get; set; } = false;

//        public ColumnCastDescriptor CastDescriptor { get; set; }

        public List<ColumnAttribute> Attributes { get; } = new List<ColumnAttribute>(); 
        #endregion

        public PropertyAttributes(PropertyInfo Property) 
            : base() => this.Property = Property;
        public PropertyAttributes(PropertyInfo Property, List<ColumnAttribute> Attributes)
            : this(Property) => this.Attributes.AddRange(Attributes);
    }

    //public class ClassAttributeCollector<T> : List<T>, IEnumerable
    //{
    //    #region Properties
    //    public MemberInfo Member { get; }
    //    #endregion

    //    public ClassAttributeCollector(MemberInfo Member)
    //        : base() => this.Member = Member;
    //}

    public class PropertyAttributeCollector : List<PropertyAttributes>, IEnumerable
    {
        #region Properties
        public MemberInfo Member { get; }
        #endregion

        public PropertyAttributeCollector(MemberInfo Member)
            : base() => this.Member = Member;
    }

    //public class AttributesCollector
    //{
    //    #region Properties
    //    public string Name { 
    //        get => Member.Name; }

    //    public MemberInfo Member { get; }

    //    public List<TableAttribute> TableAttributes { get; } = new List<TableAttribute>();

    //    public List<PropertyAttributes> PropertyAttributes { get; } = new List<PropertyAttributes>();

    //    #endregion

    //    public AttributesCollector(MemberInfo Member)
    //        : base() => this.Member = Member;
    //    public AttributesCollector(MemberInfo Member, List<TableAttribute> TableAttributes, List<PropertyAttributes> PropertyAttributes)
    //        : this(Member) 
    //    { 
    //        this.TableAttributes.AddRange(TableAttributes);
    //        this.PropertyAttributes.AddRange(PropertyAttributes);
    //    }
    //}

    [Flags]
    public enum PropertyUsageEnum
    {
        None,
        ColMap,
        Relation,
    }

    public class ColumnCastDescriptor
    {
        // statics
        public static readonly ColumnCastDescriptor String60;
        public static readonly ColumnCastDescriptor String60Nullable;
        public static readonly ColumnCastDescriptor Numeric;
        public static readonly ColumnCastDescriptor NumericNullable;
        public static readonly ColumnCastDescriptor Decimal;
        public static readonly ColumnCastDescriptor DecimalNullable;

        #region Properties
        public SqlDbType TargetType { get; private set; }

        public bool AllowNull { get; set; } = false;

        public int Length { get; set; } = -1;

        public int Precision { get; set; } = -1;

        public int Scale { get; set; } = -1;
        #endregion

        public static ColumnCastDescriptor From(PropertyInfo Property)
        {
            Func<ColumnCastDescriptor> NotSupported(string TypeName) => throw new NotSupportedException($"{TypeName} is not supported for database mapping");

            // https://docs.microsoft.com/en-us/dotnet/framework/data/adonet/sql/linq/sql-clr-type-mapping
            // https://docs.microsoft.com/en-us/dotnet/api/system.data.dbtype?view=netcore-3.1

            bool IsNullableDerived = false;

            var Type = Property.PropertyType;
            while (true)
            {
                switch (Type.Name)
                {
                    case nameof(System.Object):
                        if (Type.IsSerializable)
                            return new ColumnCastDescriptor() { TargetType = SqlDbType.NText, AllowNull = IsNullableDerived };
                        else
                            return NotSupported(Type.FullName).Invoke();
                    case nameof(System.Boolean):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.Bit, AllowNull = IsNullableDerived };
                    case nameof(System.Byte):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.TinyInt, AllowNull = IsNullableDerived };
                    case nameof(System.Int16):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.SmallInt, AllowNull = IsNullableDerived };
                    case nameof(System.Int32):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.Int, AllowNull = IsNullableDerived };
                    case nameof(System.Int64):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.BigInt, AllowNull = IsNullableDerived };
                    case nameof(System.Single):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.Real, AllowNull = IsNullableDerived };
                    case nameof(System.Double):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.Float, AllowNull = IsNullableDerived };
                    case nameof(System.Decimal):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.Decimal, AllowNull = IsNullableDerived };
                    case nameof(System.Char):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.NChar, AllowNull = IsNullableDerived };
                    case nameof(System.String):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.NVarChar, AllowNull = IsNullableDerived };
                    case nameof(System.Guid):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.UniqueIdentifier, AllowNull = IsNullableDerived };
                    case nameof(DateTime):
                        return new ColumnCastDescriptor() { TargetType = SqlDbType.DateTime2, AllowNull = IsNullableDerived };
                    default:
                        if (Type.GetGenericTypeDefinition() == typeof(Nullable<>))
                        {
                            IsNullableDerived = true;
                            Type = Type.GenericTypeArguments.First();

                            break; // next round
                        }
                        else
                            return NotSupported(Type.FullName).Invoke();
                }
            }
        }

        static ColumnCastDescriptor()
        {
            String60 = new ColumnCastDescriptor() { AllowNull = false, Length = 60 };
            String60Nullable = new ColumnCastDescriptor() { AllowNull = true, Length = 60 };
            Numeric = new ColumnCastDescriptor() { AllowNull = false, Length = 18, Precision = 0 };
            NumericNullable = new ColumnCastDescriptor() { AllowNull = true, Length = 18, Precision = 0 };
            Decimal = new ColumnCastDescriptor() { AllowNull = false, Length = 18, Precision = 0 };
            DecimalNullable = new ColumnCastDescriptor() { AllowNull = true, Length = 18, Precision = 0 };
        }
    }

    public class ColumnMappingDescriptor
    {
        #region Properties
        public string Name =>
            Property.Name;

        public PropertyInfo Property { get; set; }

        public ColumnCastDescriptor CastDescriptor { get; set; }
        #endregion

        public ColumnMappingDescriptor(PropertyInfo Property)
        {
            this.Property = Property;
            CastDescriptor = ColumnCastDescriptor.From(Property);
        }
    }

    //public class PrimaryKeyDescriptor : ColumnMappingDescriptor
    //{
    //    public PrimaryKeyDescriptor(PropertyInfo Property)
    //        : base(Property) { }
    //}
}
