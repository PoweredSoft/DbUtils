using System;
using System.Collections.Generic;
using System.Text;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator.MySql
{

    public class DataTypeResolver : IDataTypeResolver
    {
        public bool IsFixLength(IHasDataType meta)
        {
            return meta.DataType == "char";
        }

        public bool IsString(IHasDataType meta)
        {
            return meta.DataType == "varchar" || meta.DataType == "char" || meta.DataType == "text";
        }

        public bool IsUnicode(IHasDataType meta) => true;
        public bool NeedFluentPrecisionSpecification(IHasDataType meta) => true;

        public Type ResolveType(IHasDataType meta)
        {
            if (meta.DataType == "int" && meta.IsUnsigned)
                return typeof(uint);
            else if (meta.DataType == "int")
                return typeof(int);
            else if (meta.DataType == "tinyint" && meta.IsUnsigned)
                return typeof(byte);
            else if (meta.DataType == "tinyint")
                return typeof(sbyte);
            if (meta.DataType == "bigint" && meta.IsUnsigned)
                return typeof(ulong);
            else if (meta.DataType == "bigint")
                return typeof(long);
            else if (meta.DataType == "smallint" && meta.IsUnsigned)
                return typeof(ushort);
            else if (meta.DataType == "smallint")
                return typeof(short);
            else if (meta.DataType == "date" || meta.DataType == "datetime")
                return typeof(DateTime);
            else if (meta.DataType == "datetimeoffset")
                return typeof(DateTimeOffset);
            else if (meta.DataType == "time")
                return typeof(TimeSpan);
            else if (meta.DataType == "decimal")
                return typeof(decimal);
            else if (meta.DataType == "double")
                return typeof(double);
            else if (meta.DataType == "boolean")
                return typeof(bool);
            else if (meta.DataType == "float")
                return typeof(float);

            return typeof(string);
        }
    }
}
