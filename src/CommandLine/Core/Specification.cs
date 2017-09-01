// Copyright 2005-2015 Giacomo Stelluti Scala & Contributors. All rights reserved. See License.md in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CSharpx;

namespace CommandLine.Core
{
    internal enum SpecificationType
    {
        Option,
        Value
    }

    public enum TargetType
    {
        Switch,
        Scalar,
        Sequence
    }

    public abstract class Specification
    {
        private readonly SpecificationType tag;
        private readonly bool required;
        private readonly bool hidden;
        private readonly Maybe<int> min;
        private readonly Maybe<int> max;
        private readonly Maybe<object> defaultValue;
        private readonly string helpText;
        private readonly string metaValue;
        private readonly IEnumerable<string> enumValues;
        /// This information is denormalized to decouple Specification from PropertyInfo.
        private readonly Type conversionType;
        private readonly TargetType targetType;

        internal Specification(SpecificationType tag, bool required, Maybe<int> min, Maybe<int> max,
            Maybe<object> defaultValue, string helpText, string metaValue, IEnumerable<string> enumValues,
            Type conversionType, TargetType targetType, bool hidden = false)
        {
            this.tag = tag;
            this.required = required;
            this.min = min;
            this.max = max;
            this.defaultValue = defaultValue;
            this.conversionType = conversionType;
            this.targetType = targetType;
            this.helpText = helpText;
            this.metaValue = metaValue;
            this.enumValues = enumValues;
            this.hidden = hidden;
        }

        internal SpecificationType Tag => this.tag;

        public bool Required => this.required;

        internal Maybe<int> Min => this.min;

        internal Maybe<int> Max => this.max;

        public int? MinCount => this.Min.MapValueOrDefault<int, int?>(val => val, null);

        public int? MaxCount => this.Max.MapValueOrDefault<int, int?>(val => val, null);

        internal Maybe<object> DefaultValue => this.defaultValue;

        public object Default => this.DefaultValue.GetValueOrDefault(null);

        public string HelpText => this.helpText;

        public string MetaValue => this.metaValue;

        public IEnumerable<string> EnumValues => this.enumValues;

        public Type ConversionType => this.conversionType;

        public TargetType TargetType => this.targetType;

        public bool Hidden => this.hidden;

        internal static Specification FromProperty(PropertyInfo property)
        {       
            var attrs = property.GetCustomAttributes(true);
            var oa = attrs.OfType<OptionAttribute>();
            if (oa.Count() == 1)
            {
                var spec = OptionSpecification.FromAttribute(oa.Single(), property.PropertyType,
                    property.PropertyType.GetTypeInfo().IsEnum
                        ? Enum.GetNames(property.PropertyType)
                        : Enumerable.Empty<string>());
                if (spec.ShortName.Length == 0 && spec.LongName.Length == 0)
                {
                    return spec.WithLongName(property.Name.ToLowerInvariant());
                }
                return spec;
            }

            var va = attrs.OfType<ValueAttribute>();
            if (va.Count() == 1)
            {
                return ValueSpecification.FromAttribute(va.Single(), property.PropertyType,
                    property.PropertyType.GetTypeInfo().IsEnum
                        ? Enum.GetNames(property.PropertyType)
                        : Enumerable.Empty<string>());
            }

            throw new InvalidOperationException();
        }
    }
}
