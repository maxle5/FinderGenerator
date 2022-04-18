namespace Maxle5.FinderGenerator.UnitTests
{
    public interface IContract
    {

    }

    public class Contract : IContract
    {

    }

    public class Test
    {
        public int MyInt { get; set; }
        public Contract MyContract { get; set; }
        public List<Contract> MyStrings { get; set; }
    }

    public static partial class IRateableFinder
    {
        //[FinderGenerator]
        //private static partial IEnumerable<int> FindInts(Test test);

        //[FinderGenerator]
        //private static partial IEnumerable<string> FindStrings(Test test);

        [FinderGenerator]
        private static partial IEnumerable<IContract> FindContracts(Test test);
    }
}