﻿namespace Maxle5.FinderGenerator.UnitTests
{
    public class MySampleChildObject
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public IEnumerable<int> Numbers { get; set; }
        public IEnumerable<MySampleGrandChildObject> GrandChildren { get; set; }
    }

    //public static partial class StringFinder
    //{
    //    [FinderGenerator]
    //    private static partial IEnumerable<string> Find(MyComplexObject tests);
    //}

    //public static partial class DecimalFinder
    //{
    //    [FinderGenerator]
    //    private static partial IEnumerable<decimal> Find(MyComplexObject tests);
    //}

    //public static partial class ChildFinder
    //{
    //    [FinderGenerator]
    //    private static partial IEnumerable<MyChildObject> Find(MyComplexObject tests);
    //}
}