using EasyLog;

// Tests de la classe LogFile
[TestClass]
public class LogFileTests
{
    [TestInitialize]
    public void Setup()
    {
        LogSettings.Reset();
    }

    // Test : une entrée de log est correctement écrite dans le fichier JSON du jour
    [TestMethod]
    public void WriteLogs_ValidEntry_ShouldCreateLogFile()
    {
        LogSettings.Format = LogFormat.Json;
        Logger logger = new Logger(AppContext.BaseDirectory);
        string expectedFileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string expectedPath = Path.Combine(AppContext.BaseDirectory, expectedFileName);

        logger.WriteLogs("TestWork", @"\\server\source\file.txt", @"\\server\dest\file.txt", 1024, 150);

        Assert.IsTrue(File.Exists(expectedPath), "Le fichier log doit exister après l'écriture.");
    }

    [TestMethod]
    public void WriteLogs_MultipleEntries_ShouldAppendToSameFile()
    {
        LogSettings.Format = LogFormat.Json;
        Logger logger = new Logger(AppContext.BaseDirectory);
        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);

        logger.WriteLogs("Work1", @"\\server\source\a.txt", @"\\server\dest\a.txt", 512, 100);
        logger.WriteLogs("Work2", @"\\server\source\b.txt", @"\\server\dest\b.txt", 2048, 200);

        string content = File.ReadAllText(filePath);
        Assert.Contains("Work1", content);
        Assert.Contains("Work2", content);
    }

    [TestMethod]
    public void WriteLogs_NegativeTransferTime_ShouldWriteEntry()
    {
        LogSettings.Format = LogFormat.Json;
        Logger logger = new Logger(AppContext.BaseDirectory);

        logger.WriteLogs("ErrorWork", @"\\server\source\file.txt", @"\\server\dest\file.txt", 0, -1);

        string fileName = DateTime.Now.ToString("yyyy-MM-dd") + ".json";
        string filePath = Path.Combine(AppContext.BaseDirectory, fileName);
        Assert.IsTrue(File.Exists(filePath));
    }

    [TestMethod]
    public void WriteLogs_XmlFormat_ShouldCreateXmlFileWithEntries()
    {
        LogSettings.Format = LogFormat.Xml;
        string dir = Path.Combine(Path.GetTempPath(), "EasyLogXml_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            Logger logger = new Logger(dir);
            logger.WriteLogs("XmlJob", @"C:\src\a.txt", @"C:\dst\a.txt", 256, 42);

            string path = Path.Combine(dir, DateTime.Now.ToString("yyyy-MM-dd") + ".xml");
            Assert.IsTrue(File.Exists(path));
            string xml = File.ReadAllText(path);
            Assert.Contains("EasySaveLogs", xml);
            Assert.Contains("XmlJob", xml);
        }
        finally
        {
            try
            {
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
            }
            catch
            {
                // ignore cleanup temp
            }

            LogSettings.Reset();
        }
    }
}
