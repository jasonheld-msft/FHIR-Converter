// -------------------------------------------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License (MIT). See LICENSE in the repo root for license information.
// -------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Health.Fhir.Liquid.Converter.Utilities;
using Newtonsoft.Json.Linq;
using Xunit;

namespace Microsoft.Health.Fhir.Liquid.Converter.UnitTests.Utilities
{
    public class ComplexObjectFilterUtilityTests
    {
        public struct TestCase
        {
            public object[] Input;
            public string Path;
            public string Value;
            public object[] Expected;

            public TestCase(object[] input, string path, string value, object[] expected)
            {
                Input = input;
                Path = path;
                Value = value;
                Expected = expected;
            }
        }

        [Fact]
        public void GivenObjectAndPath_SelectCorrectObjectsFromArray()
        {
            var testContent = File.ReadAllText("./TestData/ComplexObject.json");
            var testData = JToken.Parse(testContent).ToObject<object[]>();
            var tests = new TestCase[]
            {
                new TestCase(testData, "age", "27", new object[] { testData[0] }),
                new TestCase(testData, "upgrades.vision.system", "http://infrared.org/other", new object[] { testData[6] }),
            };

            foreach (TestCase testCase in tests)
            {
                var expected = testCase.Expected;
                var actualData = ComplexObjectFilterUtility.Select(testCase.Input, testCase.Path, testCase.Value);
                Assert.Equal(expected, actualData);
            }
        }

        [Fact]
        public void GivenPath_WhenSplitToParts_CorrectPartsShouldBeReturned()
        {
            var testData = new Dictionary<string, string[]>
            {
                // Path                      Expected parts
                { "test.path[].to[5].value", new string[] { "test", "path", "[]", "to", "[5]", "value" } },
                { "test",                    new string[] { "test" } },
                { "test.path",               new string[] { "test", "path" } },
                { "[].[5].test.path[5]",     new string[] { "[]", "[5]", "test", "path", "[5]" } },
                { "[5]",                     new string[] { "[5]" } },
                { "[]",                      new string[] { "[]" } },
            };

            foreach (KeyValuePair<string, string[]> data in testData)
            {
                var path = data.Key;
                var expected = new Queue<string>(data.Value);
                var actual = ComplexObjectFilterUtility.SplitObjectPath(path);
                Assert.Equal(expected, actual);
            }
        }
    }
}
