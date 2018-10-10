using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.EF.Generator.SqlServer.EFCore
{
    public class SqlServerGenerator : SqlServerGeneratorBase<SqlServerGeneratorOptions>
    {
        protected override void GenerateManyToMany(Table table)
        {
            var tableNamespace = TableNamespace(table);
            var tableClassName = TableClassName(table);
            var tableClass = GenerationContext.FindClass(tableClassName, tableNamespace);

            var manyToManyList = table.ManyToMany().ToList();
            manyToManyList.ForEach(fk =>
            {
                var sqlServerFk = fk as ForeignKey;

                // get the poco of the many to many.
                var manyToManyPocoFullClass = TableClassFullName(sqlServerFk.SqlServerForeignKeyColumn.SqlServerTable);

                // pluralize this name.
                var propName = Pluralize(sqlServerFk.SqlServerForeignKeyColumn.SqlServerTable.Name);
                propName = tableClass.GetUniqueMemberName(propName);

                // the type of the property.
                var propType = $"System.Collections.Generic.ICollection<{manyToManyPocoFullClass}>";
                var defaultValue = $"new {CollectionInstanceType()}<{manyToManyPocoFullClass}>()";

                // generate property :)
                tableClass.Property(p => p.Virtual(true).Type(propType).Name(propName).DefaultValue(defaultValue).Comment("Many to Many").Meta(fk));
            });
        }

        protected override string CollectionInstanceType() => "System.Collections.Generic.HashSet";

        protected override void GenerateContext()
        {
            
        }
    }
}
