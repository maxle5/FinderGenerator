using FluentAssertions;
using Xunit;

namespace Maxle5.FinderGenerator.UnitTests
{
    public static partial class Finder
    {
        [FinderGenerator]
        public static partial IEnumerable<int> FindIntegers(MySampleObject test);

        [FinderGenerator]
        public static partial IEnumerable<MySampleChildObject> FindChildren(MySampleObject test);
    }

    public class GeneratorTests
    {
        [Fact]
        public void Find_ShouldReturn_PrimitiveTypes()
        {
            // Arrange
            var obj = new MySampleObject
            {
                Id = 99,
                Numbers = new[] { 98, 97, 96 },
                Child = new MySampleChildObject
                {
                    Id = 95,
                    Numbers = new[] { 94, 93, 92, 91, 90 }
                }
            };

            // Act
            var ints = Finder.FindIntegers(obj);

            // Assert
            var expectedInts = new[] { 99, 98, 97, 96, 95, 94, 93, 92, 91, 90 };
            ints.Except(expectedInts).Should().BeEmpty();
        }

        [Fact]
        public void Find_ShouldReturn_ComplexTypes()
        {
            // Arrange
            var child = new MySampleChildObject
            {
                Id = 95,
                Numbers = new[] { 94, 93, 92, 91, 90 }
            };
            var obj = new MySampleObject
            {
                Id = 99,
                Numbers = new[] { 98, 97, 96 },
                Child = child
            };

            // Act
            var children = Finder.FindChildren(obj);

            // Assert
            children.Should().BeEquivalentTo(new[] { child });
        }
    }
}
