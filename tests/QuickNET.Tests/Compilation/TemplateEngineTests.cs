using QuickNET.Models;
using QuickNET.Templates;

namespace QuickNET.Tests.Compilation;

[TestClass]
public sealed class TemplateEngineTests
{
    private readonly CSharpTemplateEngine _csharp = new();
    private readonly VbTemplateEngine _vbNet = new();

    [TestMethod]
    public void CSharp_SimpleExpression_WrapsWithReturn()
    {
        var code = _csharp.GenerateCode("2 + 2");

        StringAssert.Contains(code, "return 2 + 2;");
    }

    [TestMethod]
    public void CSharp_Statement_NoWrapping()
    {
        var code = _csharp.GenerateCode("var x = 10;");

        StringAssert.Contains(code, "var x = 10;");
    }

    [TestMethod]
    public void CSharp_MultiLine_Block_NoWrapping()
    {
        var userCode = """
            if (true)
            {
                var x = 10;
            }
            """;

        var code = _csharp.GenerateCode(userCode);

        StringAssert.Contains(code, "if (true)");
        StringAssert.Contains(code, "var x = 10;");
    }

    [TestMethod]
    public void CSharp_UsesDefaultNamespaces()
    {
        var code = _csharp.GenerateCode("2 + 2");

        Assert.IsTrue(code.Contains("using System;"));
        Assert.IsTrue(code.Contains("using System.IO;"));
        Assert.IsTrue(code.Contains("using System.Linq;"));
    }

    [TestMethod]
    public void VbNet_SimpleExpression_WrapsWithReturn()
    {
        var code = _vbNet.GenerateCode("2 + 2");

        StringAssert.Contains(code, "Return 2 + 2");
    }

    [TestMethod]
    public void VbNet_Statement_NoWrapping()
    {
        var code = _vbNet.GenerateCode("Dim x = 10");

        StringAssert.Contains(code, "Dim x = 10");
    }

    [TestMethod]
    public void VbNet_UsesDefaultImports()
    {
        var code = _vbNet.GenerateCode("2 + 2");

        Assert.IsTrue(code.Contains("Imports System"));
        Assert.IsTrue(code.Contains("Imports System.IO"));
        Assert.IsTrue(code.Contains("Imports System.Linq"));
    }

    [TestMethod]
    public void SupportedLanguage_Returns_CorrectEnum()
    {
        Assert.AreEqual(Language.CSharp, _csharp.SupportedLanguage);
        Assert.AreEqual(Language.VisualBasic, _vbNet.SupportedLanguage);
    }
}
