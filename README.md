# confmanager

An optimized .cfg file reader / writer, used in my projects ( and games ).
The values are stored as a string, so you have to convert them to the desired type

## Features

For more details on each feature, check the docs.

- [x] Annotations ( special flags at the top of the file )
- [x] Customizable comment characters ( default is `//` )
- [x] Customizable key/value separator
- [x] Customizable logger ( eg. use `GD.Print` instead of `Console.WriteLine` )
- [x] Ability to store all values on 1 line ( if every is separated by a semicolon )
- [x] Non-destructive changes to the config ( changes are not applied without calling `ApplyModified` )

## Additional information

- Values are stored as string ( for simplicity; converting values from string to the desired type is pretty simple )
- ConfManager only provides you methods to open, modify, and save a config file.
- Changes you make to the file are sandboxed; you have to call `ApplyModified` to apply changes to annotations and values.
- Saving the file doesn't apply the changes, call `SaveFile(applyBeforeSave: true)` to apply the changes, or just call `ApplyModified` before saving.
