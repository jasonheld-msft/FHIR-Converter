﻿// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using DotLiquid;
using Microsoft.Health.Fhir.Liquid.Converter.DotLiquids;
using Microsoft.Health.Fhir.Liquid.Converter.Exceptions;
using Microsoft.Health.Fhir.Liquid.Converter.Models;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;

namespace Microsoft.Health.Fhir.Liquid.Converter
{
    /// <summary>
    /// Filters for conversion
    /// </summary>
    public partial class Filters
    {
        public static List<object> ToArray(object input)
        {
            return input switch
            {
                null => new List<object>(),
                IEnumerable<object> enumerableObject => enumerableObject.ToList(),
                _ => new List<object> { input }
            };
        }

        public static List<object> Concat(List<object> l1, List<object> l2)
        {
            return new List<object>().Concat(l1 ?? new List<object>()).Concat(l2 ?? new List<object>()).ToList();
        }

        public static string BatchRender(Context context, List<object> collection, string templateName, string variableName)
        {
            var templateFileSystem = context.Registers["file_system"] as IFhirConverterTemplateFileSystem;
            var template = templateFileSystem?.GetTemplate(templateName);

            if (template == null)
            {
                throw new RenderException(FhirConverterErrorCode.TemplateNotFound, string.Format(Resources.TemplateNotFound, templateName));
            }

            var sb = new StringBuilder();
            collection?.ForEach(entry =>
            {
                context[variableName] = entry;
                sb.Append(template.Render(RenderParameters.FromContext(context, CultureInfo.InvariantCulture)));
            });

            return sb.ToString();
        }

        public static object GetIndex(object[] collection, int index)
        {
            if (collection != null && collection.Count() > index)
            {
                return collection[index];
            }

            return null;
        }

        public static object Slice(string input, int offset, int length = 1)
        {
            length = (length == -1) ? input.Length : length;
            return DotLiquid.StandardFilters.Slice(input, offset, length);
        }

        public static object[] SliceArray(object[] input, int s, int? n = null)
        {
            var result = input?.Skip(s);

            if (n.HasValue)
            {
                result = result?.Take(n.Value);
            }

            return result?.ToArray() ?? new object[0];
        }

        public static object Keys(object input)
        {
            if (input is Dictionary<string, object> inputObj)
            {
                return inputObj.Keys;
            }

            return new string[] { };
        }

        public static object[] SelectOr(object[] input, string path, string[] values)
        {
            return ComplexObjectFilterUtility.Select(input, path, values);
        }

        public static object[] Select(object[] input, string path, string values)
        {
            return ComplexObjectFilterUtility.Select(input, path, new string[] { values });
        }

        public static Dictionary<string, object> Filter(object input, string path, string values)
        {
            return ComplexObjectFilterUtility.Filter(input, path, new string[] { values });
        }
    }
}
