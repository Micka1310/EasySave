/* 
 * Command line for testing :
 * dotnet test --logger "console;verbosity=detailed"
 * dotnet test --filter "FullyQualifiedName~Namespace.NomDeClasse.NomDeMéthode"
 * dotnet test
*/

using StateFileLib;

// Tests de la classe StateFile
[TestClass]
public class StateFileTests
{
    // Test : l'état d'un travail est correctement écrit dans state.json
    [TestMethod]
    public void WriteProcess_ValidState_ShouldCreateStateFile()
    {
        // Arrange
        StateFile stateFile = new StateFile();
        string filePath = Path.Combine(AppContext.BaseDirectory, "state.json");

        WorkState state = new WorkState
        {
            WorkName = "Backup1",
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            Status = "Active",
            TotalFiles = 10,
            TotalSize = 5000,
            RemainingFiles = 5,
            RemainingSize = 2500,
            Progression = 50,
            CurrentSourceFile = @"\\server\source\file.txt",
            CurrentDestinationFile = @"\\server\dest\file.txt"
        };

        // Act
        stateFile.WriteProcess(state);

        // Assert
        Assert.IsTrue(File.Exists(filePath), "state.json doit exister après l'écriture.");
    }

    // Test : mettre à jour un travail existant ne crée pas de doublon
    [TestMethod]
    public void WriteProcess_UpdateExistingWork_ShouldNotDuplicate()
    {
        // Arrange
        StateFile stateFile = new StateFile();
        string filePath = Path.Combine(AppContext.BaseDirectory, "state.json");

        WorkState state = new WorkState { WorkName = "Backup2", Status = "Active" };
        WorkState updatedState = new WorkState { WorkName = "Backup2", Status = "Inactive" };

        // Act
        stateFile.WriteProcess(state);
        stateFile.WriteProcess(updatedState);

        // Assert
        string content = File.ReadAllText(filePath);
        int count = content.Split("Backup2").Length - 1;
        Assert.AreEqual(1, count, "Le travail ne doit apparaître qu'une seule fois dans state.json.");
    }

    // Test : deux travaux différents sont tous les deux présents dans state.json
    [TestMethod]
    public void WriteProcess_TwoDifferentWorks_ShouldBothExist()
    {
        // Arrange
        StateFile stateFile = new StateFile();
        string filePath = Path.Combine(AppContext.BaseDirectory, "state.json");

        WorkState state1 = new WorkState { WorkName = "WorkA", Status = "Active" };
        WorkState state2 = new WorkState { WorkName = "WorkB", Status = "Inactive" };

        // Act
        stateFile.WriteProcess(state1);
        stateFile.WriteProcess(state2);

        // Assert
        string content = File.ReadAllText(filePath);
        Assert.Contains("WorkA", content);
        Assert.Contains("WorkB", content);
    }
}
