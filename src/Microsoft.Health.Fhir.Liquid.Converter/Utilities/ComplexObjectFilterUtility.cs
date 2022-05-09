// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Health.Fhir.Liquid.Converter.Utilities
{
    public static class ComplexObjectFilterUtility
    {
        private static readonly char[] Delimeters = new[] { '.', '[', ']' };

        public static object[] Select(object[] input, string path, string value = null)
        {
            // Split path on . and []
            Queue<string> pathKeys = SplitObjectPath(path);

            List<object> ret = new List<object>();

            foreach (object obj in input)
            {
                // Clone our queue so we can check this path on every object
                // in this loop
                var localPath = new Queue<string>(pathKeys);
                if (ObjHasValueAtPath(obj, localPath, value))
                {
                    // This object has our value at the path so add it to our
                    // return
                    ret.Add(obj);
                }
            }

            return ret.ToArray<object>();
        }

        public static bool ObjHasValueAtPath(object input, Queue<string> path, string value)
        {
            // Get our key name
            var key = path.Dequeue();
            var endOfPath = path.Count == 0;

            // Check if our key is an object property or an array reference
            if (key.Contains('['))
            {
                // Our required path dictates we should have an array and if not
                // then this is not a match
                if ((input is Array) == false)
                {
                    return false;
                }

                var inputArray = (object[])input;

                // If key is [] then we want to loop over every object in the array
                // and check for our future paths
                if (key == "[]")
                {
                    if (endOfPath)
                    {
                        // TODO: Can value matter when path ends in indiscriminite array?
                        return true;
                    }

                    foreach (object obj in inputArray)
                    {
                        // We need to clone our queue to prevent loop items from
                        // overriding eachother
                        var localPath = new Queue<string>(path);
                        if (ObjHasValueAtPath(obj, localPath, value))
                        {
                            return true;
                        }
                    }

                    return false;
                }

                // If our array key references a specific index
                else
                {
                    // Trim and parse out our numeric index
                    var indexStr = key.TrimStart('[').TrimEnd(']');
                    var index = int.Parse(indexStr);

                    // If it's not possible that this array contains the specified index
                    // then this is not a match
                    if (index < 0 || inputArray.Length < index)
                    {
                        return false;
                    }
                    else
                    {
                        // If we're at the end then check the value, otherwise recurse
                        if (endOfPath)
                        {
                            return CheckValueTrueIfNull(inputArray[index], value);
                        }
                        else
                        {
                            return ObjHasValueAtPath(inputArray[index], path, value);
                        }
                    }
                }
            }

            // Our key is referencing an object property
            else
            {
                Dictionary<string, object> obj = (Dictionary<string, object>)input;

                // If our object does not have the specified key, it is not a match
                if (obj.ContainsKey(key))
                {
                    // If we're at the end then check the value, otherwise recurse
                    if (endOfPath)
                    {
                        return CheckValueTrueIfNull(input, value);
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

        public static bool CheckValueTrueIfNull(object input, string value)
        {
            // Return true if value is null and an equality check otherwise
            if (value == null)
            {
                return true;
            }
            else
            {
                return input.Equals(value);
            }
        }

        // `test.path[].to[5].value` -> ['test', 'path', '[]', 'to', '[5]', 'value' ]
        public static Queue<string> SplitObjectPath(string path)
        {
            var parts = new Queue<string>();
            var part = new StringBuilder();
            var bracket = false;

            for (int i = 0; i < path.Length; i++)
            {
                if (Delimeters.Contains(path[i]) == false)
                {
                    // Any char that isn't a delimeter gets saved
                    part.Append(path[i]);
                }

                if (path[i] == '[')
                {
                    // Perform a split
                    parts.Enqueue(part.ToString());
                    part.Clear();

                    // Brackets do not get stripped
                    part.Append(path[i]);

                    // Begin a bracket split
                    bracket = true;
                }

                if (path[i] == ']')
                {
                    // Brackets do not get stripped
                    part.Append(path[i]);

                    // Break out of the bracket split
                    bracket = false;
                }

                if ((path[i] == '.' && !bracket) || i == path.Length - 1)
                {
                    // Split on . if we're not in a bracket or if we're at the end of string
                    parts.Enqueue(part.ToString());
                    part.Clear();
                }
            }

            return parts;
        }
    }
}
