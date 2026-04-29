using LogFileLib;

// Tests de la classe LogFile
[TestClass]
public class LogFileTests
{
    // Test : une entrée de log est correctement écrite dans le fichier JSON du jour
    [TestMethod]
    public void WriteLogs_ValidEntry_ShouldCreateLogFile()
    {
        // Arrange
        LogFile logFile = new LogFile();
        string expectedFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string expectedPath = Path.Combine(AppContext.BaseDirectory, expectedFileName);

        // Act
        logFile.WriteLogs("TestWork", @"\\server\source\file.txt", @"\\server\dest\file.txt", 1024, 150);

        // Assert
        Assert.IsTrue(File.Exists(expectedPath), "Le fichier log doit exister après l'écriture.");
    }

    // Test : plusieurs entrées sont toutes écrites dans le même fichier journalier
    [TestMethod]
    public void WriteLogs_MultipleEntries_ShouldAppendToSameFile()
    {
        // Arrange
        LogFile logFile = new LogFile();
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);

        // Act
        logFile.WriteLogs("Work1", @"\\server\source\a.txt", @"\\server\dest\a.txt", 512, 100);
        logFile.WriteLogs("Work2", @"\\server\source\b.txt", @"\\server\dest\b.txt", 2048, 200);

        // Assert
        string content = File.ReadAllText(filePath);
        Assert.IsTrue(content.Contains("Work1"), "Le fichier log doit contenir la première entrée.");
        Assert.IsTrue(content.Contains("Work2"), "Le fichier log doit contenir la deuxième entrée.");
    }

    // Test : un temps de transfert négatif est accepté (indique une erreur)
    [TestMethod]
    public void WriteLogs_NegativeTransferTime_ShouldWriteEntry()
    {
        // Arrange
        LogFile logFile = new LogFile();

        // Act - aucune exception ne doit être levée
        logFile.WriteLogs("ErrorWork", @"\\server\source\file.txt", @"\\server\dest\file.txt", 0, -1);

        // Assert
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        Assert.IsTrue(File.Exists(filePath));
    }
}
