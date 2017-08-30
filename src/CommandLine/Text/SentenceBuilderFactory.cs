using System;
using System.Collections.Generic;
using System.Text;

namespace CommandLine.Text
{

    /// <summary>
    /// Static helper class for creation of new <see cref="SentenceBuilder"/> objects.
    /// </summary>
    public static class SentenceBuilderFactory
    {

        internal static SentenceBuilder CreateInstance() => CreateFunction();

        /// <summary>
        /// Factory function to allow custom SentenceBuilder injection
        /// </summary>
        public static Func<SentenceBuilder> CreateFunction { get; set; } = () => new DefaultSentenceBuilder();

    }
}
