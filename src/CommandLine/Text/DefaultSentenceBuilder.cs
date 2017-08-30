// Copyright 2005-2015 Giacomo Stelluti Scala & Contributors. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CommandLine.Infrastructure;

namespace CommandLine.Text
{

    /// <summary>
    /// Default implementation for the <see cref="SentenceBuilder"/> class
    /// </summary>
    public class DefaultSentenceBuilder : SentenceBuilder
    {
        /// <inheritdoc />
        public override Func<string> RequiredWord
        {
            get { return () => "Required."; }
        }

        /// <inheritdoc />
        public override Func<string> ErrorsHeadingText
        {
            get { return () => "ERROR(S):"; }
        }

        /// <inheritdoc />
        public override Func<string> UsageHeadingText
        {
            get { return () => "USAGE:"; }
        }

        /// <inheritdoc />
        public override Func<bool, string> HelpCommandText
        {
            get
            {
                return isOption => isOption
                    ? "Display this help screen."
                    : "Display more information on a specific command.";
            }
        }

        /// <inheritdoc />
        public override Func<bool, string> VersionCommandText
        {
            get { return _ => "Display version information."; }
        }

        /// <inheritdoc />
        public override Func<Error, string> FormatError
        {
            get
            {
                return error =>
                    {
                        switch (error.Tag)
                        {
                            case ErrorType.BadFormatTokenError:
                                return "Token '".JoinTo(((BadFormatTokenError)error).Token, "' is not recognized.");
                            case ErrorType.MissingValueOptionError:
                                return "Option '".JoinTo(((MissingValueOptionError)error).NameInfo.NameText,
                                    "' has no value.");
                            case ErrorType.UnknownOptionError:
                                return "Option '".JoinTo(((UnknownOptionError)error).Token, "' is unknown.");
                            case ErrorType.MissingRequiredOptionError:
                                var errMisssing = ((MissingRequiredOptionError)error);
                                return errMisssing.NameInfo.Equals(NameInfo.EmptyName)
                                           ? "A required value not bound to option name is missing."
                                           : "Required option '".JoinTo(errMisssing.NameInfo.NameText, "' is missing.");
                            case ErrorType.BadFormatConversionError:
                                var badFormat = ((BadFormatConversionError)error);
                                return badFormat.NameInfo.Equals(NameInfo.EmptyName)
                                           ? "A value not bound to option name is defined with a bad format."
                                           : "Option '".JoinTo(badFormat.NameInfo.NameText, "' is defined with a bad format.");
                            case ErrorType.SequenceOutOfRangeError:
                                var seqOutRange = ((SequenceOutOfRangeError)error);
                                return seqOutRange.NameInfo.Equals(NameInfo.EmptyName)
                                           ? "A sequence value not bound to option name is defined with few items than required."
                                           : "A sequence option '".JoinTo(seqOutRange.NameInfo.NameText,
                                                "' is defined with fewer or more items than required.");
                            case ErrorType.BadVerbSelectedError:
                                return "Verb '".JoinTo(((BadVerbSelectedError)error).Token, "' is not recognized.");
                            case ErrorType.NoVerbSelectedError:
                                return "No verb selected.";
                            case ErrorType.RepeatedOptionError:
                                return "Option '".JoinTo(((RepeatedOptionError)error).NameInfo.NameText,
                                    "' is defined multiple times.");
                        }
                        throw new InvalidOperationException();
                    };
            }
        }

        /// <inheritdoc />
        public override Func<IEnumerable<MutuallyExclusiveSetError>, string> FormatMutuallyExclusiveSetErrors
        {
            get
            {
                return errors =>
                {
                    var bySet = from e in errors
                                group e by e.SetName into g
                                select new { SetName = g.Key, Errors = g.ToList() };

                    var msgs = bySet.Select(
                        set =>
                        {
                            var names = string.Join(
                                string.Empty,
                                (from e in set.Errors select "'".JoinTo(e.NameInfo.NameText, "', ")).ToArray());
                            var namesCount = set.Errors.Count();

                            var incompat = string.Join(
                                string.Empty,
                                (from x in
                                     (from s in bySet where !s.SetName.Equals(set.SetName) from e in s.Errors select e)
                                    .Distinct()
                                 select "'".JoinTo(x.NameInfo.NameText, "', ")).ToArray());

                            return
                                new StringBuilder("Option")
                                        .AppendWhen(namesCount > 1, "s")
                                        .Append(": ")
                                        .Append(names.Substring(0, names.Length - 2))
                                        .Append(' ')
                                        .AppendIf(namesCount > 1, "are", "is")
                                        .Append(" not compatible with: ")
                                        .Append(incompat.Substring(0, incompat.Length - 2))
                                        .Append('.')
                                    .ToString();
                        }).ToArray();
                    return string.Join(Environment.NewLine, msgs);
                };
            }
        }
    }
}
