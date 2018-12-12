using PoweredSoft.CodeGenerator;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.EF.Generator.MySql;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.MySql;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PoweredSoft.DbUtils.EF.Generator.EFCore.MySql
{
    public class DatabaseGenerator : EFCoreDatabaseGeneratorBase<DatabaseSchema, GeneratorOptions>
    {
        public override IDataTypeResolver DataTypeResolver { get; } = new DataTypeResolver();
        public override DatabaseSchema CreateSchema() => new DatabaseSchema();

        public override IGeneratorOptions GetDefaultOptions() => new GeneratorOptions();

        public override void InitializeOptionsWithDefault()
        {
            Options = GetDefaultOptions() as GeneratorOptions;
        }

        protected override RawLineBuilder UseDatabaseEngineConnectionStringLine() => RawLineBuilder.Create($"optionsBuilder.UseMySQL(\"{Options.ConnectionString}\")");

        protected override bool IsCascade(string action) => action == "CASCADE";
        protected override bool IsSetNull(string action) => action == "SET NULL";

        protected override string GetNextValueRawSql(ISequence sequence)
        {
            throw new NotImplementedException();
        }

        protected override bool ShouldGenerateIndex(IIndex index)
        {
            if (Options.DontGeneratePrimaryIndexes && index.Name == "PRIMARY")
                return false;

            if (Options.DontGenerateForeignKeyIndexes && index.Table.ForeignKeys.Any(t => t.Name == index.Name))
                return false;

            return base.ShouldGenerateIndex(index);
        }

        protected override string FluentColumnType(IColumn column)
        {
            return (column as Column).RawColumnType;
        }
    }
}
