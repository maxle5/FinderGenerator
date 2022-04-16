namespace Maxle5.Finder
{
    internal static class FinderTemplates
    {
        internal const string Attribute = @"
using System;
namespace Maxle5.Finder
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public class FinderGeneratorAttribute : Attribute
    {
        public FinderGeneratorAttribute()
        {
        }
    }
}";

        internal const string EmptySourceCode = @"
using System;
using System.Linq;
namespace Maxle5.FinderPlayground
{
    public static partial class DateTimeFinder
    {
        public static partial IEnumerable<DateTime> Find(Parent parent)
        {
            return Enumerable.Empty<DateTime>();
        }
    }
}";
    }
}
