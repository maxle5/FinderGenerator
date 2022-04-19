namespace Maxle5.FinderGenerator
{
    internal static class Templates
    {
        public const string FinderGeneratorAttributeName = "FinderGeneratorAttribute";
        public const string FinderGeneratorAttributeNamespace = "Maxle5.FinderGenerator";
        public static string FinderGeneratorAttributeFullName = $"{FinderGeneratorAttributeNamespace}.{FinderGeneratorAttributeName}";
        public static string FinderGeneratorAttribute = $@"
using System;
using System.Collections.Generic;

namespace {FinderGeneratorAttributeNamespace}
{{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class {FinderGeneratorAttributeName} : Attribute
    {{
        public {FinderGeneratorAttributeName}()
        {{
        }}
    }}
}}";
    }
}
