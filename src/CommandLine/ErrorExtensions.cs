// Copyright 2005-2015 Giacomo Stelluti Scala & Contributors. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using CommandLine.Core;
using CommandLine.Infrastructure;

namespace CommandLine
{
    static class ErrorExtensions
    {
        public static ParserResult<T> ToParserResult<T>(this IEnumerable<Error> errors, T instance, List<IDisposable> disposableOptions)
        {
            if (errors.Any())
            {
                try
                {
                    disposableOptions.ForEach(d => d.Dispose());
                }
                catch
                {
                }

                return new NotParsed<T>(instance.GetType().ToTypeInfo(), errors);
            }
            else
            {
                return new Parsed<T>(instance, disposableOptions);
            }
        }

        public static IEnumerable<Error> OnlyMeaningfulOnes(this IEnumerable<Error> errors)
        {
            return errors
                .Where(e => !e.StopsProcessing)
                .Where(e => !(e.Tag == ErrorType.UnknownOptionError
                    && ((UnknownOptionError)e).Token.EqualsOrdinalIgnoreCase("help")));
        }
    }
}
