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
            if (meta.DataType == "bigint" && meta.NumericPrecision == 20)
                return typeof(ulong);
            else if (meta.DataType == "bigint")
                return typeof(long);

            return typeof(string);
        }
    }
}
