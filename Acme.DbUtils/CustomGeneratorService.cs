using System;
using System.IO;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace Acme.DbUtils
{
    public class EachTableService : IEachTableService
    {
        public void OnTable(IGenerator generator, ITable table)
        {
            if (!(generator is IGeneratorUsingGenerationContext))
                throw new Exception("Not the kind of generator expected.");

            var gen = generator as IGeneratorUsingGenerationContext;
            var options = generator.GetOptions();
            var ctx = gen.GetGenerationContext();

            var modelClassName = $"{table.Name}ModelBase";
            var modelClass = ctx.FindClass(modelClassName);
            var pocoClass = ctx.FindClass(table.Name);

            var path = $"{options.OutputDir}{Path.DirectorySeparatorChar}transformations.generated.cs";

            ctx.File(path, fb => 
            {
                fb.Namespace("Acme.Models", true, ns =>
                    {
                        ns.Class($"{table.Name}TransformationService", true, c =>
                        {
                            c.Method(m =>
                            {
                                m
                                    .AccessModifier(AccessModifiers.Public)
                                    .Virtual(true)
                                    .Name("ToModel")
                                    .ReturnType("void");

                                m.Parameter(p => p.Name("source").Type("object"));
                                m.Parameter(p => p.Name("destination").Type("object"));
                                m.RawLine("int todo")
                                    ;
                            });
                        });
                    });
            });
        }
    }
}
