# confmanager

An optimized ( hopefully ) .cfg file reader / writer, made in 2 days for my projects / games.

## Features

- [x] Annotations ( special flags at the top of the file )
- [x] Customizable comment characters ( default is `//` )
- [x] Customizable key/value separator
- [x] Customizable logger ( eg. use `GD.Print` instead of `Console.WriteLine` )
- [x] Ability to store all values on 1 line ( if every is separated by a semicolon )
- [x] Non-destructive changes to the config ( changes are not applied without calling `ApplyModified` )

## Using `IConfig` after version 2.0

Version 2.0 introduces many features, including more customization but most importantly: the `IConfig` interface.
This interface allows you to save a class's fields or properties to a config file easily. You can do this like this:

```cs
public class Example : IConfig
{
    // fields and properties must have the ConfigValue attribute to be saved. values like this also have to be 
    // convertable to strings. ints in this case are easily converted to strings so theyre not a problem
    [ConfigValue] public int exampleNumber = 57;
    [ConfigValue] public string exampleText = "exampleNumber";

    // however, CfgFile doesn't know what to do with arrays, or classes. the result will probably be weird
    // without your intervention
    [ConfigValue] public List<string> values { get; set; } // incorrect

    // same rules apply for annotations, they cannot be arrays or classes but have to be able to convert to string
    [ConfigAnnotation] public string annotation = "hello world!"; // it is recommended to have 1 annotation on 1 value, but you can have more

    // annotations are spearated by the separator which you can change in CfgCustomizer
    [ConfigAnnotation] public string annotations = "multiple, annotations";
}

// and now load in the class
Example ex = new();
CfgFile file = new();

file.CreateFrom(ex); 
// now you have the class saved. its not technically 'modified', so you can just call SaveFile to save it without calling ApplyModified
file.SaveFile("path/to/the/save/location");

// now say you updated some variables
ex.exampleNumber += 256;

// you can easily update this using UpdateFrom. since the file didnt change at all, we also set clearExisting as false to improve performance
file.UpdateFrom(ex, clearExisting: false);
```
