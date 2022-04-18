namespace Maxle5.FinderGenerator.UnitTests
{
    public class MyComplexObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<int> Numbers { get; set; }
    }

    public static partial class IntegerFinder
    {
        [FinderGenerator]
        private static partial IEnumerable<int> Find(MyComplexObject test);
    }
    
    public static partial class StringFinder
    {
        [FinderGenerator]
        private static partial IEnumerable<string> Find(MyComplexObject tests);
    }
}