# confmanager

An optimized .cfg file reader / writer, used in my projects ( and games ).
The values are stored as a string, so you have to convert them to the desired type

## Features

For more details on each feature, check the docs.

- [ ] Annotations ( special flags at the top of the file )
- [ ] Customizable bools ( instead of true / false, you could use 1 / 0, on / off, etc. )
- [ ] Customizable comment characters ( default is `//` )
- [ ] Customizable key/value separator
- [x] Customizable logger ( eg. use `GD.Print` instead of `Console.WriteLine` )
- [ ] Ability to store all values on 1 line ( if separated by a semicolon, can be customized )
