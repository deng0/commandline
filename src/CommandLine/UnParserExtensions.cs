// Copyright 2005-2015 Giacomo Stelluti Scala & Contributors. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Text;
using CommandLine.Core;
using CommandLine.Infrastructure;
using CSharpx;

namespace CommandLine
{
    /// <summary>
    /// Provides settings for when formatting command line from an options instance../>.
    /// </summary>
    public class UnParserSettings
    {
        private bool preferShortName;
        private bool groupSwitches;
        private bool useEqualToken;

        /// <summary>
        /// Gets or sets a value indicating whether unparsing process shall prefer short or long names.
        /// </summary>
        public bool PreferShortName
        {
            get { return preferShortName; }
            set { PopsicleSetter.Set(Consumed, ref preferShortName, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether unparsing process shall group switches.
        /// </summary>
        public bool GroupSwitches
        {
            get { return groupSwitches; }
            set { PopsicleSetter.Set(Consumed, ref groupSwitches, value); }
        }

        /// <summary>
        /// Gets or sets a value indicating whether unparsing process shall use equal sign with long names.
        /// </summary>
        public bool UseEqualToken
        {
            get { return useEqualToken; }
            set { PopsicleSetter.Set(Consumed, ref useEqualToken, value); }
        }

        /// <summary>
        /// Factory method that creates an instance of <see cref="CommandLine.UnParserSettings"/> with GroupSwitches set to true.
        /// </summary>
        /// <returns>A properly initalized <see cref="CommandLine.UnParserSettings"/> instance.</returns>
        public static UnParserSettings WithGroupSwitchesOnly()
        {
            return new UnParserSettings { GroupSwitches = true };
        }

        /// <summary>
        /// Factory method that creates an instance of <see cref="CommandLine.UnParserSettings"/> with UseEqualToken set to true.
        /// </summary>
        /// <returns>A properly initalized <see cref="CommandLine.UnParserSettings"/> instance.</returns>
        public static UnParserSettings WithUseEqualTokenOnly()
        {
            return new UnParserSettings { UseEqualToken = true };
        }

        internal bool Consumed { get; set; }
    }

    /// <summary>
    /// Provides overloads to unparse options instance.
    /// </summary>
    public class UnParser
    {
        public bool EnableDashDash { get; set; } = true;

        public bool IncludeApplicationAlias { get; set; } = true;

        /// <summary>
        /// Format a command line argument string from a parsed instance. 
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="options"/>.</typeparam>
        /// <param name="options">A parsed (or manually correctly constructed instance).</param>
        /// <returns>A string with command line arguments.</returns>
        public string FormatCommandLine<T>(T options)
        {
            return FormatCommandLine(options, config => { });
        }

        /// <summary>
        /// Format a command line argument string from a parsed instance. 
        /// </summary>
        /// <typeparam name="T">Type of <paramref name="options"/>.</typeparam>
        /// <param name="options">A parsed (or manually correctly constructed instance).</param>
        /// <param name="configuration">The <see cref="Action{UnParserSettings}"/> lambda used to configure
        /// aspects and behaviors of the unparsersing process.</param>
        /// <returns>A string with command line arguments.</returns>
        public virtual string FormatCommandLine<T>(T options, Action<UnParserSettings> configuration)
        {
            if (options == null) throw new ArgumentNullException("options");

            var settings = new UnParserSettings();
            configuration(settings);
            settings.Consumed = true;

            var type = options.GetType();
            var builder = new StringBuilder();

            type.GetVerbSpecification()
                .MapValueOrDefault(verb => builder.Append(verb.Name).Append(' '), builder);

            var specs =
                (from info in
                    type.GetSpecifications(
                        pi => new
                        {
                            Specification = Specification.FromProperty(pi),
                            Value = pi.GetValue(options, null).NormalizeValue(),
                            PropertyValue = pi.GetValue(options, null)
                        })
                 where !info.PropertyValue.IsEmpty()
                 select info)
                    .Memorize();

            var allOptSpecs = from info in specs.Where(i => i.Specification.Tag == SpecificationType.Option)
                              let o = (OptionSpecification)info.Specification
                              where o.TargetType != TargetType.Switch || (o.TargetType == TargetType.Switch && ((bool)info.Value))
                              //orderby o.UniqueName()
                              select info;

            var shortSwitches = from info in allOptSpecs
                                let o = (OptionSpecification)info.Specification
                                where o.TargetType == TargetType.Switch
                                where o.ShortName.Length > 0
                                //orderby o.UniqueName()
                                select info;

            var optSpecs = settings.GroupSwitches
                ? allOptSpecs.Where(info => !shortSwitches.Contains(info))
                : allOptSpecs;

            var valSpecs = from info in specs.Where(i => i.Specification.Tag == SpecificationType.Value)
                           let v = (ValueSpecification)info.Specification
                           orderby v.Index
                           select info;

            builder = settings.GroupSwitches && shortSwitches.Any()
                ? builder.Append('-').Append(string.Join(string.Empty, shortSwitches.Select(
                    info => ((OptionSpecification)info.Specification).ShortName).ToArray())).Append(' ')
                : builder;
            optSpecs.ForEach(
                opt =>
                    builder
                        .Append(((OptionSpecification)opt.Specification).FormatOption(opt.Value, settings))
                        .Append(' ')
                );

            builder.AppendWhen(valSpecs.Any() && this.EnableDashDash, "-- ");

            valSpecs.ForEach(
                val => builder.Append(FormatValue(val.Specification, val.Value)).Append(' '));

            return builder
                .ToString().TrimEnd(' ');
        }

        internal static string FormatValue(Specification spec, object value)
        {
            var builder = new StringBuilder();
            switch (spec.TargetType)
            {
                case TargetType.Scalar:
                    builder.Append(FormatWithQuotes(value));
                    break;
                case TargetType.Sequence:
                    var sep = spec.SeperatorOrSpace();
                    Func<object, object> format = v
                        => sep == ' ' ? FormatWithQuotes(v) : Convert.ToString(v, CultureInfo.InvariantCulture);
                    var e = ((IEnumerable)value).GetEnumerator();
                    while (e.MoveNext())
                        builder.Append(format(e.Current)).Append(sep);
                    builder.TrimEndIfMatch(sep);
                    break;
            }
            return builder.ToString();
        }

        internal static string FormatWithQuotes(object value)
        {
            if (value is null)
            {
                return string.Empty;
            }

            string strVal;

            if (value is string str)
            {
                strVal = str;
            }
            else if (Type.GetTypeCode(value.GetType()) == TypeCode.Object)
            {
                strVal = value.ToString();
            }
            else
            {
                strVal = Convert.ToString(value, CultureInfo.InvariantCulture);
            }

            if (string.IsNullOrEmpty(strVal))
            {
                return string.Empty;
            }

            if (strVal.Contains("\""))
            {
                strVal = strVal.Replace("\"", "\\\"");
            }

            if (strVal.Contains(' '))
            {
                strVal = "\"" + strVal + "\"";
            }

            return strVal;
        }
    }

    internal static class UnParserHelperExtensions
    {
        internal static char SeperatorOrSpace(this Specification spec)
        {
            return (spec as OptionSpecification).ToMaybe()
                .MapValueOrDefault(o => o.Separator != '\0' ? o.Separator : ' ', ' ');
        }

        internal static string FormatOption(this OptionSpecification spec, object value, UnParserSettings settings)
        {
            return new StringBuilder()
                    .Append(spec.FormatName(settings))
                    .AppendWhen(spec.TargetType != TargetType.Switch, UnParser.FormatValue(spec, value))
                .ToString();
        }

        internal static string FormatName(this OptionSpecification optionSpec, UnParserSettings settings)
        {
            var longName =
                optionSpec.LongName.Length > 0
                && !settings.PreferShortName;

            return
                new StringBuilder(longName
                    ? "--".JoinTo(optionSpec.LongName)
                    : "-".JoinTo(optionSpec.ShortName))
                        .AppendWhen(optionSpec.TargetType != TargetType.Switch, longName && settings.UseEqualToken ? "=" : " ")
                    .ToString();
        }

        internal static object NormalizeValue(this object value)
        {
#if !SKIP_FSHARP
            if (value != null
                && ReflectionHelper.IsFSharpOptionType(value.GetType())
                && FSharpOptionHelper.IsSome(value))
            {
                return FSharpOptionHelper.ValueOf(value);
            }
#endif
            return value;
        }

        internal static bool IsEmpty(this object value)
        {
            if (value == null) return true;
#if !SKIP_FSHARP
            if (ReflectionHelper.IsFSharpOptionType(value.GetType()) && !FSharpOptionHelper.IsSome(value)) return true;
#endif
            if (value is ValueType && value.Equals(value.GetType().GetDefaultValue())) return true;
            if (value is string && ((string)value).Length == 0) return true;
            if (value is IEnumerable && !((IEnumerable)value).GetEnumerator().MoveNext()) return true;
            return false;
        }
    }
}
