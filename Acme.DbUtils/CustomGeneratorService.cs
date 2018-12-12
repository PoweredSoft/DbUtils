using System;
using System.IO;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace Acme.DbUtils
{
    public class ContextService : IContextService
    {
        public void OnContext(IGenerator generator)
        {
            if (!(generator is IGeneratorUsingGenerationContext) || !(generator is IGeneratorWithMeta))
                throw new Exception("Not the kind of generator expected.");

            var gen = generator as IGeneratorUsingGenerationContext;
            var genMeta = generator as IGeneratorWithMeta;
            var options = gen.GetOptions();
            var gc = gen.GetGenerationContext();
            var contextClassName = genMeta.ContextClassName();
            var contextClass = gc.FindClass(contextClassName);
            contextClass.Method(m => m.ReturnType("void").Name("CreateMethodBlah"));
        }
    }

    public class EachTableService : IEachTableService
    {
        public void OnTable(IGenerator generator, ITable table)
        {
            if (!(generator is IGeneratorUsingGenerationContext) || !(generator is IGeneratorWithMeta))
                throw new Exception("Not the kind of generator expected.");

            var gen = generator as IGeneratorUsingGenerationContext;
            var genMeta = generator as IGeneratorWithMeta;
            var options = generator.GetOptions();
            var ctx = gen.GetGenerationContext();


            // model.
            var modelClassName = genMeta.ModelClassName(table);
            var modelClassNamespace = genMeta.ModelNamespace(table);
            var modelFullClassName = genMeta.ModelClassFullName(table);

            // poco.
            var pocoClassName = genMeta.TableClassName(table);
            var pocoClassNamespace = genMeta.TableNamespace(table);
            var pocoFullClassName = genMeta.TableClassFullName(table);

            // classes
            var pocoClass = ctx.FindClass(pocoClassName, pocoClassNamespace);

            var path = $"{options.OutputDir}{Path.DirectorySeparatorChar}transformations.generated.cs";

            ctx.File(path, fb => 
            {
                fb.Namespace("Acme.Models", true, ns =>
                    {
                        ns.Class($"{table.Name}ModelTransformationService", true, c =>
                        {
                            c.Method(m =>
                            {
                                m
                                    .AccessModifier(AccessModifiers.Public)
                                    .Virtual(true)
                                    .Name("ToModel")
                                    .ReturnType("void")
                                    .Parameter(p => p.Name("source").Type(pocoFullClassName))
                                    .Parameter(p => p.Name("model").Type(modelFullClassName));
                                
                                table.Columns.ForEach(column =>
                                {
                                    m.RawLine($"model.{column.Name} = source.{column.Name}");
                                });
                            });

                            c.Method(m =>
                            {
                                m
                                    .AccessModifier(AccessModifiers.Public)
                                    .Virtual(true)
                                    .Name("FromModel")
                                    .ReturnType("void")
                                    .Parameter(p => p.Name("model").Type(modelFullClassName))
                                    .Parameter(p => p.Name("destination").Type(pocoFullClassName));

                                table.Columns.ForEach(column =>
                                {
                                    bool isPropertyNullable =
                                        column.IsNullable || options.GenerateModelPropertyAsNullable;
                                    if (isPropertyNullable && !column.IsNullable)
                                    {
                                        var matchingProp = pocoClass.FindByMeta<PropertyBuilder>(column);
                                        var ternary = TernaryBuilder
                                            .Create()
                                            .RawCondition(rc => rc.Condition($"model.{column.Name} != null"))
                                            .True(RawInlineBuilder.Create(
                                                $"destination.{column.Name} = ({matchingProp.GetTypeName()})model.{column.Name}"))
                                            .False(RawInlineBuilder.Create(
                                                $"destination.{column.Name} = default({matchingProp.GetTypeName()})"));

                                        m.RawLine($"destination.{column.Name} = {ternary.GenerateInline()}");
                                    }
                                    else
                                    {
                                        m.RawLine($"destination.{column.Name} = model.{column.Name}");
                                    }
                                });
                            });
                        });
                    });
            });
        }
    }
}
