using Microsoft.VisualStudio.TestTools.UnitTesting;
using Args.NET;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Args.NET.Tests
{
    [TestClass()]
    public class ArgParserTests
    {
        public readonly List<ArgDefinition> TestDefs = new()
        {
            new() { Name = "optionalArg", Description = "An optional non-flag arg.", IsFlag = false, Required = false, Usage = "--optionalArg <VALUE>" },
            new() { Name = "requiredArg", Description = "A required non-flag arg.", IsFlag = false, Required = true, Usage = "--requiredArg <VALUE>" },
            new() { Name = "optionalFlagArg", Description = "An optional flag arg.", IsFlag = true, Required = false, Usage = "--optionalFlagArg" },
            new() { Name = "requiredFlagArg", Description = "A required flag arg.", IsFlag = true, Required = true, Usage = "--requiredFlagArg" },
        };

        /// Arrays of optional and required args as defined in <see cref="TestDefs"/>
        readonly string[] optionalArgs = { "--optionalArg", "12", "--optionalFlagArg" };
        readonly string[] requiredArgs = { "--requiredArg", "True", "--requiredFlagArg" };
        readonly string[] requiredArgsWrongCase = { "--reQuirEdArg", "True", "--ReQuiredFlagArg" };

        [TestMethod()]
        public void ArgParserTest()
        {
            // Parse case-sensitive with all args passed, should work normally
            ArgParser parser = new(TestDefs, [.. optionalArgs, .. requiredArgs], StringComparison.CurrentCulture);
            // Parse case-sensitive with only required args passed, should work normally
            parser = new(TestDefs, requiredArgs, StringComparison.CurrentCulture);
            // Parse case-insensitive with all args passed, should work normally
            parser = new(TestDefs, [.. optionalArgs, .. requiredArgs], StringComparison.CurrentCultureIgnoreCase);
            // Parse case-insensitive with only required args passed, should work normally
            parser = new(TestDefs, requiredArgs, StringComparison.CurrentCultureIgnoreCase);
            // Parse case-insensitive with incorrectly cased required args passed, should work normally
            parser = new(TestDefs, requiredArgsWrongCase, StringComparison.CurrentCultureIgnoreCase);

            // Parse case-sensitive with incorrectly cased required args passed, should throw exception
            Assert.ThrowsException<ArgumentException>(() => parser = new(TestDefs, requiredArgsWrongCase, StringComparison.CurrentCulture));
            // Parse without required args passed, should throw exception
            Assert.ThrowsException<ArgumentException>(() => parser = new(TestDefs, optionalArgs, StringComparison.CurrentCulture));
            // Parse without any args passed, should throw exception
            Assert.ThrowsException<ArgumentException>(() => parser = new(TestDefs, [], StringComparison.CurrentCulture));
        }

        [TestMethod()]
        public void ParseArgTest()
        {
            ArgParser parser = new(TestDefs, [.. optionalArgs, .. requiredArgs], StringComparison.CurrentCulture);

            // Make sure optionalArg and requiredArg are able to be parsed to int and bool, respectively
            int optionalArgVal = parser.ParseArg<int>("optionalArg");
            Assert.AreEqual(12, optionalArgVal);
            bool requiredArgVal = parser.ParseArg<bool>("requiredArg");
            Assert.IsTrue(requiredArgVal);

            // Make sure both given flag args parse to true
            bool optionalFlagArgVal = parser.ParseArg<bool>("optionalFlagArg");
            Assert.IsTrue(optionalFlagArgVal);
            bool requiredFlagArg = parser.ParseArg<bool>("requiredFlagArg");
            Assert.IsTrue(requiredFlagArg);

            // Make sure parsing an undefined arg throws an error
            Assert.ThrowsException<ArgumentException>(() => parser.ParseArg<int>("undefinedArg"));

            // Reconstruct parser with only requiredArgs
            parser = new(TestDefs, requiredArgs, StringComparison.CurrentCulture);

            // Make sure parsing a missing (but defined) flag arg parses to false
            bool missingFlagArgVal = parser.ParseArg<bool>("optionalFlagArg");
            Assert.IsFalse(missingFlagArgVal);
            // Make sure parsing a missing (but defined) value arg results in a default value
            int missingArgVal = parser.ParseArg<int>("optionalArg", 30);
            Assert.AreEqual(30, missingArgVal);

            // Make sure raw string args work
            string? requiredStringArgVal = parser.ParseArg("requiredArg");
            Assert.AreEqual("True", requiredStringArgVal);
        }
    }
}