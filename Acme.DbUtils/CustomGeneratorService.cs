using System;
using System.IO;
using PoweredSoft.CodeGenerator;
using PoweredSoft.CodeGenerator.Constants;
using PoweredSoft.CodeGenerator.Extensions;
using PoweredSoft.DbUtils.EF.Generator.Core;
using PoweredSoft.DbUtils.Schema.Core;

namespace Acme.DbUtils
{
    public class ContextInterceptor : IContextInterceptor
    {
        public ContextInterceptor(IGenerator generator)
        {
            Generator = generator;
        }

        public IGenerator Generator { get; }

        public void InterceptContext()
        {
            if (!(Generator is IGeneratorUsingGenerationContext) || !(Generator is IGeneratorWithMeta))
                throw new Exception("Not the kind of generator expected.");

            var gen = Generator as IGeneratorUsingGenerationContext;
            var genMeta = Generator as IGeneratorWithMeta;
            var options = gen.GetOptions();
            var gc = gen.GetGenerationContext();
            var contextClassName = genMeta.ContextClassName();
            var contextClass = gc.FindClass(contextClassName);
            contextClass.Method(m => m.ReturnType("void").Name("CreateMethodBlah"));
        }
    }

    public class ResolveTypeInterceptor : IResolveTypeInterceptor
    {
        public ResolveTypeInterceptor(IGenerator generator)
        {
            Generator = generator;
        }

        public IGenerator Generator { get; }

        public Tuple<string, bool> InterceptResolveType(IColumn column)
        {
            /*
            if (column.Table.Name == "Phone" && column.Name == "PhoneTypeId")
                return new Tuple<string, bool>("Acme.Enums.PhoneTypes", true);*/
            
            return null;
        }
    }

    public class TableInterceptor : ITableInterceptor
    {
        public TableInterceptor(IGenerator generator)
        {
            Generator = generator;
        }

        public IGenerator Generator { get; }

        public void InterceptTable(ITable table)
        {
            if (!(Generator is IGeneratorUsingGenerationContext) || !(Generator is IGeneratorWithMeta))
                throw new Exception("Not the kind of generator expected.");

            var gen = Generator as IGeneratorUsingGenerationContext;
            var genMeta = Generator as IGeneratorWithMeta;
            var options = Generator.GetOptions();
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
                                bool isPropertyNullable = genMeta.IsModelPropertyNullable(column);// column.IsNullable || genMeta.ShouldGenerateModelPropertyAsNullable(column);
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
