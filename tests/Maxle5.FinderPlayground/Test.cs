//using Maxle5.TypeFinder;

//namespace Maxle5.TypeFinderPlayground
//{    
//    [FinderGenerator]
//    public static class Test
//    {
//        public static void Main()
//        {
//            Maxle5.TypeFinder.DateTimeFinderHelper.DateTimeFinder(new Parent());
//        }
//    }
//}

namespace Maxle5.FinderPlayground
{
    public static class Program
    {
        public static void Main()
        {
            var ints = DateTimeFinder.Find(new Parent
            {
                Id = 1,
                Name = "Alain",
                Date = new DateTime(1965, 12, 07),
                Children = new[]
                {
                    new Child
                    {
                        Id = 1,
                        Name = "Max",
                        Date = new DateTime(1995, 12, 30)
                    },
                    new Child
                    {
                        Id = 2,
                        Name = "Luc",
                        Date = new DateTime(1997, 12, 10)
                    },
                    new Child
                    {
                        Id = 3,
                        Name = "Liam",
                        Date = new DateTime(2000, 2, 17)
                    }
                }
            });

            foreach (var integer in ints)
            {
                Console.WriteLine(integer.ToString());
            }
        }
    }

    public class Parent
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime Date { get; set; }
        public IEnumerable<Child> Children { get; set; }
    }

    public class Child
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Age { get; set; }
        public DateTime Date { get; set; }
    }

    public static partial class DateTimeFinder
    {
        [Finder.FinderGenerator]
        public static partial IEnumerable<DateTime> Find(Parent parent);
    }
}
