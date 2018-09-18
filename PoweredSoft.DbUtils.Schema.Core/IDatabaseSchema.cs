using System;
using System.Collections.Generic;
using System.Text;

namespace PoweredSoft.DbUtils.Schema.Core
{
    public interface IDatabaseSchema
    {
        string ConnectionString { get; set; }
        List<ITable> Tables { get; }

        void LoadSchema();
    }
}
