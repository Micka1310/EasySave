using EasyLog;

// Tests de la classe LogFile
[TestClass]
public class LogFileTests
{
    [TestInitialize]
    public void Init()
    {
        LogFormatSettings.ResetToDefault();
    }

    [TestCleanup]
    public void Cleanup()
    {
        LogFormatSettings.ResetToDefault();
    }

    // Test : une entrée de log est correctement écrite dans le fichier JSON du jour
    [TestMethod]
    public void WriteLogs_ValidEntry_ShouldCreateLogFile()
    {
        // Arrange
        Logger logger = new Logger(AppContext.BaseDirectory);
        string expectedFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string expectedPath = Path.Combine(AppContext.BaseDirectory, expectedFileName);

        // Act
        logger.WriteLogs("TestWork", @"\\server\source\file.txt", @"\\server\dest\file.txt", 1024, 150);

        // Assert
        Assert.IsTrue(File.Exists(expectedPath), "Le fichier log doit exister après l'écriture.");
    }

    // Test : plusieurs entrées sont toutes écrites dans le même fichier journalier
    [TestMethod]
    public void WriteLogs_MultipleEntries_ShouldAppendToSameFile()
    {
        // Arrange
        Logger logger = new Logger(AppContext.BaseDirectory);
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);

        // Act
        logger.WriteLogs("Work1", @"\\server\source\a.txt", @"\\server\dest\a.txt", 512, 100);
        logger.WriteLogs("Work2", @"\\server\source\b.txt", @"\\server\dest\b.txt", 2048, 200);

        // Assert
        string content = File.ReadAllText(filePath);
        Assert.Contains("Work1", content);
        Assert.Contains("Work2", content);
    }

    // Test : un temps de transfert négatif est accepté (indique une erreur)
    [TestMethod]
    public void WriteLogs_NegativeTransferTime_ShouldWriteEntry()
    {
        // Arrange
        Logger logger = new Logger(AppContext.BaseDirectory);

        // Act - aucune exception ne doit être levée
        logger.WriteLogs("ErrorWork", @"\\server\source\file.txt", @"\\server\dest\file.txt", 0, -1);

        // Assert
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        Assert.IsTrue(File.Exists(filePath));
    }

    [TestMethod]
    public void WriteLogs_XmlFormat_ShouldCreateXmlFileWithEntries()
    {
        LogFormatSettings.Current = LogFormat.Xml;
        Logger logger = new Logger(AppContext.BaseDirectory);
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".xml";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        if (File.Exists(filePath))
            File.Delete(filePath);

        logger.WriteLogs("XmlWork", @"C:\src\a.txt", @"C:\dst\a.txt", 100, 10);
        logger.WriteLogs("XmlWork2", @"C:\src\b.txt", @"C:\dst\b.txt", 200, 20);

        Assert.IsTrue(File.Exists(filePath));
        string xml = File.ReadAllText(filePath);
        StringAssert.Contains(xml, "XmlWork");
        StringAssert.Contains(xml, "XmlWork2");
        StringAssert.Contains(xml, "<Logs");
    }
}
