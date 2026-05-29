using QuickNET.MetaCommands;

namespace QuickNET.Tests.MetaCommands;

[TestClass]
public sealed class MetaCommandParserTests
{
    [TestMethod]
    public void IsMetaCommand_PrefixSlash_ReturnsTrue()
    {
        Assert.IsTrue(MetaCommandParser.IsMetaCommand("/help"));
    }

    [TestMethod]
    public void IsMetaCommand_PrefixSlashWithSpaces_ReturnsTrue()
    {
        Assert.IsTrue(MetaCommandParser.IsMetaCommand("  /help"));
    }

    [TestMethod]
    public void IsMetaCommand_PlainCode_ReturnsFalse()
    {
        Assert.IsFalse(MetaCommandParser.IsMetaCommand("2 + 2"));
    }

    [TestMethod]
    public void IsMetaCommand_EmptyString_ReturnsFalse()
    {
        Assert.IsFalse(MetaCommandParser.IsMetaCommand(""));
    }

    [TestMethod]
    public void IsMetaCommand_NullOrWhitespace_ReturnsFalse()
    {
        Assert.IsFalse(MetaCommandParser.IsMetaCommand("   "));
    }

    [TestMethod]
    public void Parse_CommandOnly_ReturnsCommandNoArgs()
    {
        var (command, args) = MetaCommandParser.Parse("/help");
        Assert.AreEqual("help", command);
        Assert.IsNull(args);
    }

    [TestMethod]
    public void Parse_CommandWithArgs_ReturnsCommandAndArgs()
    {
        var (command, args) = MetaCommandParser.Parse("/lang vb");
        Assert.AreEqual("lang", command);
        Assert.AreEqual("vb", args);
    }

    [TestMethod]
    public void Parse_CommandWithMultipleArgs_ReturnsAllArgs()
    {
        var (command, args) = MetaCommandParser.Parse("/timeout 60 seconds");
        Assert.AreEqual("timeout", command);
        Assert.AreEqual("60 seconds", args);
    }

    [TestMethod]
    public void Parse_ArgsAreTrimmed_ReturnsTrimmedArgs()
    {
        var (command, args) = MetaCommandParser.Parse("/import   System.Text.Json  ");
        Assert.AreEqual("import", command);
        Assert.AreEqual("System.Text.Json", args);
    }

    [TestMethod]
    public void Parse_CommandIsLowercased_ReturnsLowercase()
    {
        var (command, args) = MetaCommandParser.Parse("/HELP");
        Assert.AreEqual("help", command);
        Assert.IsNull(args);
    }

    [TestMethod]
    public void Parse_NotMetaCommand_ReturnsEmpty()
    {
        var (command, args) = MetaCommandParser.Parse("2 + 2");
        Assert.AreEqual("", command);
        Assert.IsNull(args);
    }
}
