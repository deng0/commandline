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
    static class TypeConverter
    {
        public static Maybe<object> ChangeType(IEnumerable<string> values, Type conversionType, bool scalar, CultureInfo conversionCulture, bool ignoreValueCase, List<IDisposable> disposables)
        {
            return scalar
                ? ChangeTypeScalar(values.Single(), conversionType, conversionCulture, ignoreValueCase, disposables)
                : ChangeTypeSequence(values, conversionType, conversionCulture, ignoreValueCase, disposables);
        }

        private static Maybe<object> ChangeTypeSequence(IEnumerable<string> values, Type conversionType, CultureInfo conversionCulture, bool ignoreValueCase, List<IDisposable> disposables)
        {
            if (!conversionType.GetTypeInfo().IsGenericType ||
                conversionType.GetTypeInfo().GetGenericTypeDefinition() != typeof(List<>))
                throw new InvalidOperationException("Sequence properties should be of type List<T>.");

            var type = conversionType.GetTypeInfo().GetGenericArguments()[0];

            var converted = values.Select(value => ChangeTypeScalar(value, type, conversionCulture, ignoreValueCase, disposables)).Memorize();

            return converted.Any(a => a.MatchNothing())
                ? Maybe.Nothing<object>()
                : Maybe.Just(converted.Select(c => ((Just<object>)c).Value).ToTypedList(type));
        }

        private static Maybe<object> ChangeTypeScalar(string value, Type conversionType, CultureInfo conversionCulture, bool ignoreValueCase, List<IDisposable> disposables)
        {
            var result = ChangeTypeScalarImpl(value, conversionType, conversionCulture, ignoreValueCase, disposables);
            result.Match((_, __) => { }, e => e.First().RethrowWhenAbsentIn(
                new[] { typeof(InvalidCastException), typeof(FormatException), typeof(OverflowException) }));
            return result.ToMaybe();
        }

        private static Result<object, Exception> ChangeTypeScalarImpl(string value, Type conversionType, CultureInfo conversionCulture, bool ignoreValueCase, List<IDisposable> disposables)
        {
            if (value != null)
            {
                if (value.StartsWith("|{"))
                {
                    value = value.Substring(1);
                }

                if (value.EndsWith("}|"))
                {
                    value = value.Substring(0, value.Length - 1);
                }
            }

            Func<object> changeType = () =>
            {
                Func<object> empty = () => null;

                if (value == null)
                {
                    return empty();
                }

                Func<Type> getUnderlyingType =
                        () => Nullable.GetUnderlyingType(conversionType);

                var type = getUnderlyingType() ?? conversionType;

                Func<object> safeChangeType = () =>
                {
                    Func<object> withValue =
                        () => Convert.ChangeType(value, type, conversionCulture);

                    return withValue();
                };

                object result = value.IsBooleanString() && type == typeof(bool)
                    ? value.ToBoolean() : type.GetTypeInfo().IsEnum
                        ? value.ToEnum(type, ignoreValueCase) : safeChangeType();

                if (result is IDisposable disposable)
                {
                    disposables.Add(disposable);
                }

                return result;
            };

            Func<object> makeType = () =>
            {
                try
                {
                    var ctor = conversionType.GetTypeInfo().GetConstructor(new[] { typeof(string) });
                    object result = ctor.Invoke(new object[] { value });
                    if (result is IDisposable disposable)
                    {
                        disposables.Add(disposable);
                    }
                    return result;
                }
                catch (Exception)
                {
                    throw new FormatException("Destination conversion type must have a constructor that accepts a string.");
                }
            };

            return Result.Try(
                conversionType.IsPrimitiveEx()
                    ? changeType
                    : makeType);
        }

        private static object ToEnum(this string value, Type conversionType, bool ignoreValueCase)
        {
            object parsedValue;
            try
            {
                parsedValue = Enum.Parse(conversionType, value, ignoreValueCase);
            }
            catch (ArgumentException)
            {
                throw new FormatException();
            }
            if (Enum.IsDefined(conversionType, parsedValue))
            {
                return parsedValue;
            }
            throw new FormatException();
        }
    }
}