﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Reflection;
using IntelliTect.Coalesce.Utilities;
using System.ComponentModel.DataAnnotations;

namespace IntelliTect.Coalesce.TypeDefinition
{
    internal class ReflectionParameterViewModel : ParameterViewModel
    {
        protected internal ParameterInfo Info { get; internal set; }

        public ReflectionParameterViewModel(MethodViewModel parent, ParameterInfo info) 
            : base(parent, ReflectionTypeViewModel.GetOrCreate(
                parent.Parent.ReflectionRepository,
                info.ParameterType.IsByRef
                    ? info.ParameterType.GetElementType()!
                    : info.ParameterType
            ))
        {
            Info = info;

#if NET6_0_OR_GREATER
            var nullable = new NullabilityInfoContext().Create(Info);
            Nullability = nullable.WriteState;
#endif
        }

        public override string Name => Info.Name ?? throw new Exception("Parameter has no name???");

        public override bool HasDefaultValue => Info.HasDefaultValue;

        protected override object? RawDefaultValue => Info.RawDefaultValue;

        public override object? GetAttributeValue<TAttribute>(string valueName)
        {
            return Info.GetAttributeValue<TAttribute>(valueName);
        }

        public override bool HasAttribute<TAttribute>()
        {
            return Info.HasAttribute<TAttribute>();
        }

        private IReadOnlyList<ValidationAttribute>? _validationAttributes;
        internal IReadOnlyList<ValidationAttribute> GetValidationAttributes()
        {
            if (_validationAttributes is not null) return _validationAttributes;

            var attrs = Info
                .GetCustomAttributes(typeof(ValidationAttribute), true)
                .OfType<ValidationAttribute>();
            if (IsRequired && !attrs.Any(a => a is RequiredAttribute))
            {
                // Implicitly add required validation to non-nullable reference type parameters
                attrs = attrs.Append(new RequiredAttribute());
            }

            return _validationAttributes = attrs
                // RequiredAttribute first (descending: true first)
                .OrderByDescending(a => a is RequiredAttribute)
                .ToList();
        }
    }
}
