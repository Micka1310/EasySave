using ConsoleStrategyFile;
using WorkListFile;

[TestClass]
[DoNotParallelize]
public class ExecuteWork3Tests
{
    // Répertoires temporaires pour les tests
    private string sourceDir = "";
    private string destinationDir = "";

    // Initialisation : créer les répertoires temporaires avant chaque test
    [TestInitialize]
    public void Setup()
    {
        sourceDir = Path.Combine(Path.GetTempPath(), "EasySave_Source_" + Guid.NewGuid());
        destinationDir = Path.Combine(Path.GetTempPath(), "EasySave_Dest_" + Guid.NewGuid());
        Directory.CreateDirectory(sourceDir);
        Directory.CreateDirectory(destinationDir);
        string worksFile = Path.Combine(AppContext.BaseDirectory, "works.json");
        try { if (File.Exists(worksFile)) File.Delete(worksFile); } catch { }
    }

    // Nettoyage : supprimer les répertoires temporaires après chaque test
    [TestCleanup]
    public void Cleanup()
    {
        if (Directory.Exists(sourceDir)) Directory.Delete(sourceDir, true);
        if (Directory.Exists(destinationDir)) Directory.Delete(destinationDir, true);
    }

    // Test : parsing "2" → index [1]
    [TestMethod]
    public void ParseIndexes_SingleWork_ShouldReturnOneIndex()
    {
        // Arrange
        WorkList workList = new WorkList();
        workList.AddWork(["Work1", sourceDir, destinationDir, "1"]);
        workList.AddWork(["Work2", sourceDir, destinationDir, "1"]);

        ExecuteWork3 strategy = new ExecuteWork3();

        // Act
        string result = strategy.Execution(["2"], workList);

        // Assert
        Assert.IsNotNull(result);
    }

    // Test : parsing « 1 2 » → exécute les travaux 1 et 2
    [TestMethod]
    public void ParseIndexes_SpaceSeparated_ShouldExecuteTwoWorks()
    {
        // Arrange — créer un fichier source
        File.WriteAllText(Path.Combine(sourceDir, "file.txt"), "contenu test");

        WorkList workList = new WorkList();
        workList.AddWork(["Work1", sourceDir, destinationDir, "1"]);
        workList.AddWork(["Work2", sourceDir, destinationDir, "1"]);

        ExecuteWork3 strategy = new ExecuteWork3();

        // Act
        string result = strategy.Execution(["1 2"], workList);

        // Assert
        Assert.AreEqual("true", result);
    }

    // Test : plusieurs espaces / tabulations entre les numéros
    [TestMethod]
    public void ParseIndexes_ExtraWhitespace_ShouldExecuteTwoWorks()
    {
        // Arrange
        File.WriteAllText(Path.Combine(sourceDir, "file.txt"), "contenu test");

        WorkList workList = new WorkList();
        workList.AddWork(["Work1", sourceDir, destinationDir, "1"]);
        workList.AddWork(["Work2", sourceDir, destinationDir, "1"]);

        ExecuteWork3 strategy = new ExecuteWork3();

        // Act
        string result = strategy.Execution(["  1 \t 2  "], workList);

        // Assert
        Assert.AreEqual("true", result);
    }

    // Test : sauvegarde complète → tous les fichiers sont copiés en destination
    [TestMethod]
    public void ExecuteFullBackup_ShouldCopyAllFiles()
    {
        // Arrange
        File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "contenu 1");
        File.WriteAllText(Path.Combine(sourceDir, "file2.txt"), "contenu 2");

        WorkList workList = new WorkList();
        workList.AddWork(["WorkFull", sourceDir, destinationDir, "1"]);

        ExecuteWork3 strategy = new ExecuteWork3();

        // Act
        string result = strategy.Execution(["1"], workList);

        // Assert
        Assert.AreEqual("true", result);
        Assert.IsTrue(File.Exists(Path.Combine(destinationDir, "file1.txt")), "file1.txt doit être copié.");
        Assert.IsTrue(File.Exists(Path.Combine(destinationDir, "file2.txt")), "file2.txt doit être copié.");
    }

    // Test : sauvegarde différentielle → seuls les fichiers nouveaux ou modifiés sont copiés
    [TestMethod]
    public void ExecuteDifferentialBackup_ShouldCopyOnlyNewOrModifiedFiles()
    {
        // Arrange — file1 existe déjà en destination, file2 est nouveau
        File.WriteAllText(Path.Combine(sourceDir, "file1.txt"), "contenu 1");
        File.WriteAllText(Path.Combine(sourceDir, "file2.txt"), "contenu 2");
        File.WriteAllText(Path.Combine(destinationDir, "file1.txt"), "contenu 1");

        // S'assurer que file1 en destination est plus récent que la source
        File.SetLastWriteTime(Path.Combine(destinationDir, "file1.txt"), DateTime.Now.AddHours(1));

        WorkList workList = new WorkList();
        workList.AddWork(["WorkDiff", sourceDir, destinationDir, "2"]);

        ExecuteWork3 strategy = new ExecuteWork3();

        // Act
        string result = strategy.Execution(["1"], workList);

        // Assert
        Assert.AreEqual("true", result);
        Assert.IsTrue(File.Exists(Path.Combine(destinationDir, "file2.txt")), "file2.txt (nouveau) doit être copié.");
    }

    // Test : erreur sur un fichier (répertoire source inexistant) → retourne "false"
    [TestMethod]
    public void ExecuteFullBackup_WithError_ShouldReturnFalse()
    {
        // Arrange — répertoire source inexistant pour provoquer une erreur
        WorkList workList = new WorkList();
        workList.AddWork(["WorkError", @"C:\repertoire\inexistant\", destinationDir, "1"]);

        ExecuteWork3 strategy = new ExecuteWork3();

        // Act
        string result = strategy.Execution(["1"], workList);

        // Assert — doit retourner "false" sans lever d'exception
        Assert.AreEqual("false", result, "Doit retourner false si le répertoire source n'existe pas.");
    }
}
