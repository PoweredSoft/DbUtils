using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using PoweredSoft.CodeGenerator;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace PoweredSoft.DbUtils.EF.Generator
{
    public abstract class DatabaseGeneratorBase<TSchema, TOptions> : IGenerator<TOptions>
        where TOptions : IGeneratorOptions
        where TSchema : IDatabaseSchema
    {
        public TOptions Options { get; set; }
        protected TSchema Schema { get; set; }
        protected List<ITable> TablesToGenerate { get; set; }
        protected GenerationContext GenerationContext { get; set; }
        protected abstract IDataTypeResolver DataTypeResolver { get; }

        public abstract TSchema CreateSchema();

        public void Generate()
        {
            Schema = CreateAndLoadSchema();
            TablesToGenerate = ResolveTablesToGenerate();
            GenerationContext = GenerationContext.Create();
            GenerateCode();
        }

        protected abstract void GenerateCode();

        public virtual List<ITable> ResolveTablesToGenerate()
        {
            List<ITable> ret;

            // table name filtering.
            if (Options.IncludedTables?.Any() == true)
                ret = Schema.Tables.Where(table => Options.IncludedTables.Any(table.IsNamed)).ToList();
            else if (Options.ExcludedTables?.Any() == true)
                ret = Schema.Tables.Where(table => !Options.ExcludedTables.Any(table.IsNamed)).ToList();
            else
                ret = Schema.Tables;

            return ret;
        }

        protected TSchema CreateAndLoadSchema()
        {
            var schema = CreateSchema();
            schema.ConnectionString = Options.ConnectionString;
            schema.LoadSchema();
            return schema;
        }
    }
}
