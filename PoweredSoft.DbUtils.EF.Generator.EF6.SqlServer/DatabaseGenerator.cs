using System;
using System.Collections.Generic;
using PoweredSoft.CodeGenerator;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.EF.Generator.SqlServer;
using PoweredSoft.DbUtils.Schema.Core;
using PoweredSoft.DbUtils.Schema.SqlServer;

namespace PoweredSoft.DbUtils.EF.Generator.EF6.SqlServer
{
    public class DatabaseGenerator : EF6DatabaseGeneratorBase<IDatabaseSchema, GeneratorOptions>
    {
        public override IDataTypeResolver DataTypeResolver { get; } = new DataTypeResolver();
        public override IDatabaseSchema CreateSchema() => new DatabaseSchema();
        public override IGeneratorOptions GetDefaultOptions() => new GeneratorOptions();

        public override void InitializeOptionsWithDefault()
        {
            Options = GetDefaultOptions() as GeneratorOptions;;
        }

        protected override void GenerateGetNextSequenceLines(MethodBuilder method, string outputType, ISequence sequence)
        {
            var sqlServerSequence = (Sequence)sequence;
            method.RawLine($"return Database.SqlQuery<{outputType}>(\"SELECT NEXT VALUE FOR [{sqlServerSequence.Schema}].[{sequence.Name}];\").First()");
        }

        public override string EmptyMetas(string text) => base.EmptyMetas(text).EmptyMetas();
        public override string ReplaceMetas(string text, ITable table) => base.ReplaceMetas(text, table).ReplaceMetas((Table)table);
        public override List<ITable> ResolveTablesToGenerate() => base.ResolveTablesToGenerate().ShouldGenerate(Options);
        public override List<ISequence> ResolveSequencesToGenerate() => base.ResolveSequencesToGenerate().ShouldGenerate(Options);
        protected override bool MatchColumnTypeMapping(ColumnTypeMapping mapping, IColumn column) => mapping.MatchMappingColumnType(column);

        protected override bool IsGenerateOptionIdentity(IColumn column)
        {
            if (column.DefaultValue?.IndexOf("newsequentialid", StringComparison.InvariantCultureIgnoreCase) > -1)
                return true;

            return base.IsGenerateOptionIdentity(column);
        }

        protected override string ToTableFluent(ITable table)
        {
            var sqlServerTable = (Table)table;
            return $"ToTable(\"{table.Name}\", \"{sqlServerTable.Schema}\")";
        }
    }
}
