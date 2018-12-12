using System;
using System.Collections.Generic;
using PoweredSoft.CodeGenerator;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.EF.Generator.SqlServer;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.EF.Generator.EFCore.SqlServer
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

        protected override RawLineBuilder UseDatabaseEngineConnectionStringLine() => RawLineBuilder.Create($"optionsBuilder.UseSqlServer(\"{Options.ConnectionString}\")");
        protected override string ToTableFluent(ITable table) => $"ToTable(\"{table.Name}\", \"{((Table)table).Schema}\")";
        protected override bool IsCascade(string action) => action == "CASCADE";
        protected override bool IsSetNull(string action) => action == "SET_NULL";

        protected override string GetNextValueRawSql(ISequence sequence)
        {
            var sqlServerSequence = (Sequence) sequence;
            return $"SELECT NEXT VALUE FOR [{sqlServerSequence.Schema}].[{sequence.Name}];";
        }

        public override string EmptyMetas(string text) => base.EmptyMetas(text).EmptyMetas();
        public override string ReplaceMetas(string text, ITable table) => base.ReplaceMetas(text, table).ReplaceMetas((Table)table);
        public override List<ITable> ResolveTablesToGenerate() => base.ResolveTablesToGenerate().ShouldGenerate(Options);
        public override List<ISequence> ResolveSequencesToGenerate() => base.ResolveSequencesToGenerate().ShouldGenerate(Options);

        protected override void OnBeforeIndexLineAdded(RawLineBuilder line, IIndex index)
        {
            var sqlServerIndex = (Index)index;
            if (!string.IsNullOrWhiteSpace(sqlServerIndex.FilterDefinition))
                line.Append($"\n\t.HasFilter(\"{sqlServerIndex.FilterDefinition}\")");
        }
    }
}
