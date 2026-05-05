/* 
 * Command line for testing :
 * dotnet test --logger "console;verbosity=detailed"
 * dotnet test --filter "FullyQualifiedName~Namespace.NomDeClasse.NomDeMéthode"
 * dotnet test
*/

using WorkFile;
using WorkListFile;

[TestClass]
public sealed class TestWork
{
    [TestMethod]
    public void Work_Constructor_StoresValues()
    {
        Work work = new("backup1", "C:\\src", "C:\\dest", "Complet");

        Assert.AreEqual("backup1", work.GetName());
        Assert.AreEqual("C:\\src", work.GetSourceDirectory());
        Assert.AreEqual("C:\\dest", work.GetDestinationDirectory());
        Assert.AreEqual("Complet", work.GetWorkType());
    }

    [TestMethod]
    public void Work_SpecialCharacters_PreservedCorrectly()
    {
        Work work = new("mon fichier (1)", "C:\\Users\\malik\\Documents", "D:\\backup path", "Différentielle");

        Assert.AreEqual("mon fichier (1)", work.GetName());
        Assert.AreEqual("C:\\Users\\malik\\Documents", work.GetSourceDirectory());
        Assert.AreEqual("D:\\backup path", work.GetDestinationDirectory());
        Assert.AreEqual("Différentielle", work.GetWorkType());
    }
}

[TestClass]
public sealed class TestWorkList
{
    [TestMethod]
    public void WorkList_InitiallyEmpty()
    {
        WorkList workList = new();

        Assert.HasCount(0, workList.GetWork());
    }

    [TestMethod]
    public void WorkList_AddWork_IncreasesCount()
    {
        WorkList workList = new();

        workList.AddWork(["file1", "src1", "dest1", "Complet"]);

        Assert.HasCount(1, workList.GetWork());
    }

    [TestMethod]
    public void WorkList_AddMultipleWorks_AllStored()
    {
        WorkList workList = new();

        workList.AddWork(["file1", "src1", "dest1", "Complet"]);
        workList.AddWork(["file2", "src2", "dest2", "Différentielle"]);
        workList.AddWork(["file3", "src3", "dest3", "Complet"]);

        List<Work> works = workList.GetWork();
        Assert.HasCount(3, works);
        Assert.AreEqual("file1", works[0].GetName());
        Assert.AreEqual("file2", works[1].GetName());
        Assert.AreEqual("file3", works[2].GetName());
    }

    [TestMethod]
    public void WorkList_AddWork_ValuesCorrectlyMapped()
    {
        WorkList workList = new();

        workList.AddWork(["monFichier", "C:\\source", "D:\\destination", "Complet"]);

        Work work = workList.GetWork()[0];
        Assert.AreEqual("monFichier", work.GetName());
        Assert.AreEqual("C:\\source", work.GetSourceDirectory());
        Assert.AreEqual("D:\\destination", work.GetDestinationDirectory());
        Assert.AreEqual("Complet", work.GetWorkType());
    }
}
