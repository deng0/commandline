// Copyright 2005-2015 Giacomo Stelluti Scala & Contributors. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;

namespace CommandLine.Text
{
    /// <summary>
    /// Exposes standard delegates to provide a mean to customize part of help screen generation.
    /// This type is consumed by <see cref="CommandLine.Text.HelpText"/>.
    /// </summary>
    public abstract class SentenceBuilder
    {
        /// <summary>
        /// Create instance of <see cref="CommandLine.Text.SentenceBuilder"/>,
        /// </summary>
        /// <returns>The <see cref="CommandLine.Text.SentenceBuilder"/> instance.</returns>
        public static SentenceBuilder Create()
        {
            return SentenceBuilderFactory.CreateInstance();
        }

        /// <summary>
        /// Gets a delegate that returns the word 'required'.
        /// </summary>
        public abstract Func<string> RequiredWord { get; }

        /// <summary>
        /// Gets a delegate that returns that errors block heading text.
        /// </summary>
        public abstract Func<string> ErrorsHeadingText { get; }

        /// <summary>
        /// Gets a delegate that returns usage text block heading text.
        /// </summary>
        public abstract Func<string> UsageHeadingText { get; } 

        /// <summary>
        /// Get a delegate that returns the help text of help command.
        /// The delegates must accept a boolean that is equal <value>true</value> for options; otherwise <value>false</value> for verbs.
        /// </summary>
        public abstract Func<bool, string> HelpCommandText { get; }

        /// <summary>
        /// Get a delegate that returns the help text of vesion command.
        /// The delegates must accept a boolean that is equal <value>true</value> for options; otherwise <value>false</value> for verbs.
        /// </summary>
        public abstract Func<bool, string> VersionCommandText { get; } 

        /// <summary>
        /// Gets a delegate that handles singular error formatting.
        /// The delegates must accept an <see cref="Error"/> and returns a string.
        /// </summary>
        public abstract Func<Error, string> FormatError { get; }

        /// <summary>
        /// Gets a delegate that handles mutually exclusive set errors formatting.
        /// The delegates must accept a sequence of <see cref="MutuallyExclusiveSetError"/> and returns a string.
        /// </summary>
        public abstract Func<IEnumerable<MutuallyExclusiveSetError>, string> FormatMutuallyExclusiveSetErrors { get; }
    }
}
