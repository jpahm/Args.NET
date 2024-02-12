# Args.NET

## This is an *extremely simple*, lightweight CLI arg parser.

### There's no reflection, no support for bad practices, and no bloat.

As such, Args.NET is heavily opinionated towards simplistic elegance.

- Args have to be explicitly defined at parser construction, and are immediately searched for
- Args are required to provide a description, a usage string, and whether they're required or not
- A --help flag argument is automatically provided if not already defined
- Missing required args automatically throw an exception on parser construction
- Missing optional args will return a provided default value on parse call
- Only the '--' prefix arg naming notation is supported
- Args can be parsed to any type that implements IParsable