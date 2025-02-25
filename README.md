# confmanager

An optimized ( hopefully ) .cfg file reader / writer, made in 2 days for my projects / games.

## Features

- [x] Annotations ( special flags at the top of the file )
- [x] Customizable comment characters ( default is `//` )
- [x] Customizable key/value separator
- [x] Customizable logger ( eg. use `GD.Print` instead of `Console.WriteLine` )
- [x] Ability to store all values on 1 line ( if every is separated by a semicolon )
- [x] Non-destructive changes to the config ( changes are not applied without calling `ApplyModified` )
