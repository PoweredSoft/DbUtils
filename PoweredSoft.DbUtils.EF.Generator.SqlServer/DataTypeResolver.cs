using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer
{
    public class DataTypeResolver : IDataTypeResolver
    {
        private static readonly List<string> stringTypes = new List<string>
        {
            "nvarchar", "nchar", "ntext",
            "varchar", "char", "text"
        };

        private static readonly List<string> fixedLengthTypes = new List<string>()
        {
            "char", "nchar", "binary"
        };

        public static readonly List<string> noNeedOfScaleAndPrecision = new List<string>
        {
            "money",
            "smallmoney",
        };

        private static readonly Dictionary<string, Type> mapping = new Dictionary<string, Type>
        {
            ["bigint"] = typeof(long),
            ["smallint"] = typeof(short),
            ["int"] = typeof(int),
            ["uniqueidentifier"] = typeof(Guid),
            ["smalldatetime"] = typeof(DateTime),
            ["datetime"] = typeof(DateTime),
            ["datetime2"] = typeof(DateTime),
            ["date"] = typeof(DateTime),
            ["datetimeoffset"] = typeof(DateTimeOffset),
            ["table type"] = typeof(System.Data.DataTable),
            ["time"] = typeof(TimeSpan),
            ["float"] = typeof(double),
            ["real"] = typeof(float),
            ["numeric"] = typeof(decimal),
            ["smallmoney"] = typeof(decimal),
            ["decimal"] = typeof(decimal),
            ["money"] = typeof(decimal),
            ["tinyint"] = typeof(byte),
            ["bit"] = typeof(bool),

            ["image"] = typeof(byte[]),
            ["binary"] = typeof(byte[]),
            ["varbinary"] = typeof(byte[]),
            ["varbinary(max)"] = typeof(byte[]), // ?? is this really a sql server type?
            ["timestamp"] = typeof(byte[]),

            /*
            ["geography"] = typeof(System.Data.Entity.Spatial.DbGeography),
            ["geometry"] = typeof(System.Data.Entity.Spatial.DbGeometry)*/

            // i think this one is depercated.
            //["hierarchyid"] =  System.Data.Entity.Hierarchy.HierarchyId,
        };

        public Type ResolveType(IHasDataType meta)
        {
            var sqlType = meta.DataType;
            if (mapping.ContainsKey(sqlType))
            {
                var type = mapping[sqlType];
                return type;
            }

            return typeof(string);
        }

        public bool IsFixLength(IHasDataType meta)
        {
            return fixedLengthTypes.Contains(meta.DataType);
        }

        public bool IsString(IHasDataType meta)
        {
            return stringTypes.Contains(meta.DataType);
        }

        public bool IsUnicode(IHasDataType meta)
        {
            return meta.DataType.StartsWith("n");
        }

        public bool NeedFluentPrecisionSpecification(IHasDataType meta)
        {
            return !noNeedOfScaleAndPrecision.Contains(meta.DataType);
        } 
    }
}
