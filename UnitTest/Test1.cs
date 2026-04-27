namespace UnitTest;

using LibFile;
using System.Diagnostics;

[TestClass]
public sealed class Test1
{
    [TestMethod]
    public void TestMethod1()
    {
        Library library1 = new();
        int result = library1.Test2(10, 10);

        Console.WriteLine("On est censé avoir 20.");
        Trace.WriteLine(result);
        Assert.AreEqual(20, result);
    }

    [TestMethod]
    public void TestMethod2()
    {
        Library library1 = new();
        int result = library1.Test2(10, 20); // À modifier "20" par "10" pour réussir le test

        Console.WriteLine("On est censé avoir 20.");
        Trace.WriteLine(result);
        Assert.AreEqual(20, result);
    }
}
