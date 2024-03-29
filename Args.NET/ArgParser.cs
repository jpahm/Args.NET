﻿using System.Xml.Linq;

namespace Args.NET
{
    /// <summary>
    /// The definition of a CLI argument.
    /// </summary>
    public struct ArgDefinition(string name, bool isFlag, bool required, string description, string usage)
    {
        /// <summary>
        /// The argument's name. Will automatically be prefixed by '--' during searches.
        /// </summary>
        public string Name { get; set; } = name;

        /// <summary>
        /// Whether the argument is a flag or not. If true, the parser will not look for a value after the arg and a missing value will not trigger an exception.
        /// </summary>
        public bool IsFlag { get; set; } = isFlag;

        /// <summary>
        /// Whether the argument is required or not. Missing required args will trigger an exception.
        /// </summary>
        public bool Required { get; set; } = required;

        /// <summary>
        /// The description of the argument. Describes what the argument does in simple terms.
        /// </summary>
        public string Description { get; set; } = description;

        /// <summary>
        /// The usage string for this argument. Shows how to properly use the argument in syntax notation.
        /// </summary>
        public string Usage { get; set; } = usage;
    }

    /// <summary>
    /// Simple, lightweight CLI arg parser with no reflection or fancy features.
    /// </summary>
    public class ArgParser
    {
        private readonly StringComparison ComparisonType;
        private readonly Dictionary<string, ArgDefinition> ArgDefs = [];
        private readonly Dictionary<string, string?> ArgVals = [];

        /// <summary>
        /// Creates a new ArgParser with the given <paramref name="argDefs"/> which immediately finds and stores the values associated with each defined arg in <paramref name="args"/>. Arg names are matched based on the <paramref name="comparisonType"/>.
        /// </summary>
        /// <param name="argDefs">The definitions of the arguments.</param>
        /// <param name="args">The raw string arguments, i.e. from Main().</param>
        /// <param name="comparisonType">The string comparison type to use for matching args. Defaults to case-sensitive.</param>
        public ArgParser(IEnumerable<ArgDefinition> argDefs, string[] args, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            ComparisonType = comparisonType;

            // Enforce naming, descriptions, and usage strings
            foreach (var def in argDefs)
            {
                if (def.Name is null || def.Name == string.Empty)
                    throw new ArgumentException("An argument definition was missing a name.");
                if (def.Description is null || def.Description == string.Empty)
                    throw new ArgumentException($"Argument --{def.Name} is missing a description.");
                if (def.Usage is null || def.Usage == string.Empty)
                    throw new ArgumentException($"Argument --{def.Name} is missing a usage string.");
            }

            // If no custom help arg defined, create one with default behavior and find it before everything else
            if (!argDefs.Any(x => x.Name.Equals("help", StringComparison.CurrentCultureIgnoreCase)))
            {
                ArgDefinition helpDef = new() { Name = "help", Required = false, IsFlag = true, Description = "Displays the help page.", Usage = "--help" };
                ArgDefs["help"] = helpDef;
                ArgVals["help"] = FindArg(args, helpDef);
                if (ArgVals["help"] != false.ToString())
                {
                    Console.Write("Usage:\n\n");
                    foreach (var def in argDefs.Append(helpDef))
                    {
                        if (def.Required)
                            Console.Write($"{def.Usage}\n{def.Description}\n\n");
                        else
                            Console.Write($"[{def.Usage}]\n{def.Description}\n\n");
                    }
                    Environment.Exit(0);
                }
            }
            // Read args based on argDefs, store defs keyed by name
            foreach (var def in argDefs)
            {
                // Extract def.Name
                string defName = def.Name;
                // Perform ToLower() on name if comparisonType is case-insensitive
                if ((int)ComparisonType % 2 == 1)
                    defName = defName.ToLower();

                ArgDefs[defName] = def;
                ArgVals[defName] = FindArg(args, def);
            }
        }

        /// <summary>
        /// Attempts to parse an arg as a given type <typeparamref name="T"/>. Throws an exception if the arg is undefined or <paramref name="defaultValue"/> if the arg hasn't been given.
        /// </summary>
        /// <typeparam name="T">The type to try to parse the arg as, must implement <see cref="IParsable{TSelf}"/>.</typeparam>
        /// <param name="name">The name of the arg to find. Case-sensitivity applies based on the comparisonType chosen in the <see cref="ArgParser"/> constructor.</param>
        /// <param name="defaultValue">The default value to fall back to if the arg hasn't been specified; only necessary if the arg isn't defined as required.</param>
        /// <returns>The parsed arg value, or <paramref name="defaultValue"/> if the arg wasn't found.</returns>
        /// <exception cref="ArgumentException">What went wrong parsing the arg. Includes the usage string if the argument was found but failed to parse.</exception>
        public T? ParseArg<T>(string name, T? defaultValue = default) where T : IParsable<T>
        {
            // Perform ToLower() if comparisonType is case-insensitive
            if ((int)ComparisonType % 2 == 1)
                name = name.ToLower();
            if (!ArgVals.TryGetValue(name, out string? value))
                throw new ArgumentException($"Tried to parse undefined argument '--{name}'.");
            else if (value == null)
                return defaultValue;
            // Throw error with usage string if parsing fails
            if (!T.TryParse(value, null, out T? res))
                throw new ArgumentException($"Argument '--{name}` failed to parse.\n\nUsage: {ArgDefs[name].Usage}");
            return res;
        }

        /// <summary>
        /// Attempts to parse a string arg. Throws an exception if the arg is undefined or <paramref name="defaultValue"/> if the arg hasn't been given.
        /// </summary>
        /// <param name="name">The name of the arg to find. Case-sensitivity applies based on the comparisonType chosen in the <see cref="ArgParser"/> constructor.</param>
        /// <param name="defaultValue">The default value to fall back to if the arg hasn't been specified; only necessary if the arg isn't defined as required.</param>
        /// <returns>The parsed arg value, or <paramref name="defaultValue"/> if the arg wasn't found.</returns>
        public string? ParseArg(string name, string? defaultValue = null)
        {
            // Perform ToLower() if comparisonType is case-insensitive
            if ((int)ComparisonType % 2 == 1)
                name = name.ToLower();
            if (!ArgVals.TryGetValue(name, out string? value))
                throw new ArgumentException($"Tried to parse undefined argument '--{name}'.");
            else if (value == null)
                return defaultValue;
            return value;
        }

        private string? FindArg(string[] args, ArgDefinition argDef)
        {
            // Find arg with matching name using string ComparisonType
            int idx = -1;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i].Equals($"--{argDef.Name}", ComparisonType))
                {
                    idx = i;
                    break;
                }
            }
            if (idx == -1)
            {
                // Throw if required arg not found
                if (argDef.Required)
                    throw new ArgumentException($"Required argument '--{argDef.Name}' not found.");
                else if (argDef.IsFlag) // Parse as "false" if flag arg is missing
                    return false.ToString();
                else // Parse as null if arg isn't a flag
                    return null;
            }
            // If arg is a flag, we don't need to check for a value after it -- parse as "true"
            if (argDef.IsFlag)
                return true.ToString();
            // Otherwise, throw an exception if we can't find a value after the arg
            if (idx + 1 >= args.Length || args[idx + 1].StartsWith("--"))
                throw new ArgumentException($"Non-flag argument '--{argDef.Name}' is missing a value.");
            else
                return args[idx + 1];
        }
    }
}
