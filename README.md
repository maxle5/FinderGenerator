# Maxle5.FinderGenerator

[![CI](https://github.com/maxle5/FinderGenerator/actions/workflows/ci.yml/badge.svg)](https://github.com/maxle5/FinderGenerator/actions/workflows/ci.yml)

Maxle5.FinderGenerator is a high performance library used for finding all instances of a given Type from a complex object. 

**Built without reflection** by leveraging .NET Source Code Generators that were introduced in .NET 5!

## Usage:
### Prerequisites:
- method has the `[FinderGenerator]` Attribute
- method is marked as `static partial`
- method returns an `IEnumerable<T>` (`T` is the type to find)
- method accepts a single argument (argument is the Type to look through)

### Example
```
public static partial class IntegerFinder
{
    [FinderGenerator]
    public static partial IEnumerable<int> Find(MyComplexObject test);
}

// Sample Object To Look through
public class MyComplexObject
{
    public int Id { get; set; }
    public string Name { get; set; }
    public IEnumerable<int> Numbers { get; set; }
}
```

### What it generates behind the scenes:
```
public static partial class IntegerFinder
{
    public static partial IEnumerable<int> Find(MyComplexObject test)
    {
        var instances = new List<int>();
        instances.Add(test.Id);

        foreach (var y in test.Numbers)
        {
            instances.Add(y);
        }

        return instances;
    }
}
```
