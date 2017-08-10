// Copyright 2005-2015 Giacomo Stelluti Scala & Contributors. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
#if PLATFORM_DOTNET
using System.Reflection;
#endif
using CommandLine.Infrastructure;
using CSharpx;
using RailwaySharp.ErrorHandling;
using System.Reflection;

namespace CommandLine.Core
{
    static class InstanceBuilder
    {
        public static ParserResult<T> Build<T>(
            Type typeInfo,
            Maybe<Func<T>> factory,
            Func<IEnumerable<string>, IEnumerable<OptionSpecification>, Result<IEnumerable<Token>, Error>> tokenizer,
            IEnumerable<string> arguments,
            StringComparer nameComparer,
            bool ignoreValueCase,
            CultureInfo parsingCulture,
            IEnumerable<ErrorType> nonFatalErrors)
        {
            var isMutable = typeInfo.IsMutable();

            if (!isMutable && factory.IsJust())
            {
                throw new ArgumentException("Cannot use factor for immutable types.", "factory");
            }

            var specProps = typeInfo.GetSpecifications(pi => SpecificationProperty.Create(
                    Specification.FromProperty(pi), pi, Maybe.Nothing<object>())).Memorize();

            var specs = from pt in specProps select pt.Specification;

            var optionSpecs = specs
                .ThrowingValidate(SpecificationGuards.Lookup)
                .OfType<OptionSpecification>();

            Func<IEnumerable<Error>, ParserResult<T>> notParsed =
                errs => new NotParsed<T>(typeInfo.ToTypeInfo(), errs);

            Func<ParserResult<T>> buildUp = () =>
            {
                var tokenizerResult = tokenizer(arguments, optionSpecs);

                var tokens = tokenizerResult.SucceededWith();

                var partitions = TokenPartitioner.Partition(
                    tokens,
                    name => TypeLookup.FindTypeDescriptorAndSibling(name, optionSpecs, nameComparer));
                var optionsPartition = partitions.Item1;
                var valuesPartition = partitions.Item2;
                var errorsPartition = partitions.Item3;

                var optionSpecPropsResult =
                    OptionMapper.MapValues(
                        (from pt in specProps where pt.Specification.IsOption() select pt),
                        optionsPartition,
                        (vals, type, isScalar) => TypeConverter.ChangeType(vals, type, isScalar, parsingCulture, ignoreValueCase),
                        nameComparer);

                var valueSpecPropsResult =
                    ValueMapper.MapValues(
                        (from pt in specProps where pt.Specification.IsValue() orderby ((ValueSpecification)pt.Specification).Index select pt),
                        valuesPartition,
                        (vals, type, isScalar) => TypeConverter.ChangeType(vals, type, isScalar, parsingCulture, ignoreValueCase));

                var missingValueErrors = from token in errorsPartition
                                         select
                        new MissingValueOptionError(
                            optionSpecs.Single(o => token.Text.MatchName(o.ShortName, o.LongName, nameComparer))
                                .FromOptionSpecification());

                var specPropsWithValue =
                    optionSpecPropsResult.SucceededWith().Concat(valueSpecPropsResult.SucceededWith()).Memorize();

                Func<T> buildMutable = () =>
                {
                    var mutable = factory.MapValueOrDefault(f => f(), (T)Activator.CreateInstance(typeInfo));
                    mutable =
                        mutable.SetProperties(specPropsWithValue, sp => sp.Value.IsJust(), sp => sp.Value.FromJustOrFail())
                            .SetProperties(
                                specPropsWithValue,
                                sp => sp.Value.IsNothing() && sp.Specification.DefaultValue.IsJust(),
                                sp => sp.Specification.DefaultValue.FromJustOrFail())
                            .SetProperties(
                                specPropsWithValue,
                                sp =>
                                    sp.Value.IsNothing() && sp.Specification.TargetType == TargetType.Sequence
                                    && sp.Specification.DefaultValue.MatchNothing(),
                                sp => sp.Property.PropertyType.GetTypeInfo().GetGenericArguments().Single().CreateEmptyArray());
                    return mutable;
                };

                Func<T> buildImmutable = () =>
                {
                    var ctor = typeInfo.GetTypeInfo().GetConstructor((from sp in specProps select sp.Property.PropertyType).ToArray());
                    var values = (from prms in ctor.GetParameters()
                        join sp in specPropsWithValue on prms.Name.ToLower() equals sp.Property.Name.ToLower()
                        select
                            sp.Value.GetValueOrDefault(
                                sp.Specification.DefaultValue.GetValueOrDefault(
                                    sp.Specification.ConversionType.CreateDefaultForImmutable()))).ToArray();
                    var immutable = (T)ctor.Invoke(values);
                    return immutable;
                };

                var instance = isMutable ? buildMutable() : buildImmutable();
                
                var validationErrors = specPropsWithValue.Validate(SpecificationPropertyRules.Lookup(tokens));

                var allErrors =
                    tokenizerResult.SuccessfulMessages()
                        .Concat(missingValueErrors)
                        .Concat(optionSpecPropsResult.SuccessfulMessages())
                        .Concat(valueSpecPropsResult.SuccessfulMessages())
                        .Concat(validationErrors)
                        .Memorize();

                var warnings = from e in allErrors where nonFatalErrors.Contains(e.Tag) select e;

                return allErrors.Except(warnings).ToParserResult(instance);
            };

            var preprocessorErrors = arguments.Any()
                ? arguments.Preprocess(PreprocessorGuards.Lookup(nameComparer))
                : Enumerable.Empty<Error>();

            var result = arguments.Any()
                ? preprocessorErrors.Any()
                    ? notParsed(preprocessorErrors)
                    : buildUp()
                : buildUp();

            return result;
        }
    }
}