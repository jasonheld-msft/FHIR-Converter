// -------------------------------------------------------------------------------------------------
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

        public static object Slice(object input, int s, int n = 1)
        {
            if (input != null)
            {
                if (input is string inputString)
                {
                    if (inputString.Length > s + n)
                    {
                        return inputString.Skip(s).Take(n);
                    }
                }
                else if (input is object[] collection)
                {
                    if (collection.Count() > s + n)
                    {
                        return collection.Skip(s).Take(n);
                    }
                }
            }

            return null;
        }

        public static object[] Select(object[] input, string path, string value = null)
        {
            Queue<string> pathKeys = SplitObjectPath(path);

            List<object> ret = new List<object>();

            foreach (object obj in input)
            {
                var localPath = new Queue<string>(pathKeys);
                if (ObjHasValueAtPath(obj, localPath, value))
                {
                    ret.Add(obj);
                }
            }

            return ret.ToArray<object>();
        }

        private static bool ObjHasValueAtPath(object input, Queue<string> path, string value)
        {
            var key = path.Dequeue();

            if (key.Contains('['))
            {
                if (!(input is Array))
                {
                    return false;
                }

                var inputArray = (object[])input;

                if (key == "[]")
                {
                    if (path.Count == 0)
                    {
                        // TODO: Can value matter when path ends in indiscriminite array?
                        return true;
                    }

                    foreach (object obj in inputArray)
                    {
                        var localPath = new Queue<string>(path);
                        if (ObjHasValueAtPath(obj, localPath, value)) {
                            return true;
                        }
                    }

                    return false;
                }
                else
                {
                    var indexStr = key.TrimStart('[').TrimEnd(']');
                    var index = int.Parse(indexStr);

                    if (inputArray.Length < index)
                    {
                        return false;
                    }
                    else
                    {
                        if (path.Count == 0)
                        {
                            if (value != null)
                            {
                                if (!(inputArray[index] is string) || (string)inputArray[index] != value)
                                {
                                    return false;
                                }
                            }
                        }

                        return true;
                    }
                }
            }
            else
            {
                Dictionary<string, object> obj = (Dictionary<string, object>)input;

                if (obj.ContainsKey(key))
                {
                    if (path.Count == 0)
                    {
                        if (value != null)
                        {
                            if (!(obj[key] is string) || (string)obj[key] != value)
                            {
                                return false;
                            }
                        }

                        return true;
                    }
                    else
                    {
                        return ObjHasValueAtPath(obj[key], path, value);
                    }
                }
                else
                {
                    return false;
                }
            }
        }

        private static Queue<string> SplitObjectPath(string path)
        {
            var delimeters = new[] { '.', '[', ']' };
            var parts = new Queue<string>();
            var current = new StringBuilder();
            var bracket = false;

            for (int i = 0; i < path.Length; i++)
            {

                if (delimeters.Contains(path[i]) == false)
                {
                    current.Append(path[i]);
                }

                if (path[i] == '[')
                {
                    parts.Enqueue(current.ToString());
                    current.Length = 0;
                    current.Append(path[i]);
                    bracket = true;
                }

                if (path[i] == ']')
                {
                    current.Append(path[i]);
                    bracket = false;
                }

                if ((path[i] == '.' && !bracket) || i == path.Length - 1)
                {
                    parts.Enqueue(current.ToString());
                    current.Length = 0;
                }
            }

            parts.ToList<string>().ForEach(x => Console.WriteLine(x));

            return parts;
        }

        public static object FilterByKeyWithValue(object[] input, string key, string needle)
        {
            string[] keys;
            if (key.Contains('.'))
            {
                keys = key.Split('.');
            } else {
                keys = new string[] { key };
            }

            List<Dictionary<string, object>> ret = new List<Dictionary<string, object>>();
            foreach (Dictionary<string, object> resource in input)
            {
                string value;

                var res = resource;
                for (int k = 0; k < keys.Count() - 1; k++)
                {
                    var cKey = keys[k];
                    //Console.WriteLine(cKey);
                    if (res.ContainsKey(cKey))
                    {
                        res = (Dictionary<string, object>)res[cKey];
                    }
                    else
                    {
                        // TODO: x_x skip resource loop
                    }
                }

                key = keys.Last();
                if (res.ContainsKey(key))
                {
                    value = (string)res[key];
                }
                else
                {
                    continue;
                }

                if (needle == value)
                {
                    ret.Add(resource);
                }
            }

            return ret.ToArray<object>();
        }
    }
}
