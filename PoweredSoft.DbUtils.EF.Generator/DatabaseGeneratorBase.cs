using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pluralize.NET;
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

        protected Pluralizer Plurializer { get; } = new Pluralizer();

        public abstract TSchema CreateSchema();

        protected virtual void CleanOutputDir()
        {
            if (!Options.CleanOutputDir)
                return;
            
            var dir = new DirectoryInfo(Options.OutputDir);
            foreach (var fi in dir.GetFiles())
                fi.Delete();

            foreach (var di in dir.GetDirectories())
                di.Delete(true);
        }

        public void Generate()
        {
            Schema = CreateAndLoadSchema();
            TablesToGenerate = ResolveTablesToGenerate();
            GenerationContext = GenerationContext.Create();
            CleanOutputDir();
            GenerateCode();
        }

        protected abstract void GenerateCode();

        protected virtual string Pluralize(string text)
        {
            var ret = Plurializer.Pluralize(text);
            return ret;
        }

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

        protected string RemoveIdSuffixFromColumnName(string columnName)
        {
            var suffix = "Id";
            if (columnName.EndsWith(suffix))
                return columnName.Substring(0, columnName.Length - suffix.Length);
   
            return columnName;
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
