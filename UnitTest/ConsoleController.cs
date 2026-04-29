/* 
 * Command line for testing :
 * dotnet test --logger "console;verbosity=detailed"
 * dotnet test
*/

using ControllerFile;
using System.Diagnostics;

[TestClass]
public sealed class TestConsoleStrategy
{
    // Get all the options
    [TestMethod]
    public void GetOption()
    {
        Controller controller1 = new();
        List<string> result1 = controller1.GetOption();

        // Standard message :
        Console.WriteLine("test \"GetOption()\"");

        // Display for debugging :
        Trace.WriteLine("| Options |");

        foreach (string elem1 in result1)
        {
            Trace.WriteLine(elem1);
        }
    }

    // Get all the message for inputing parameters from a specific option
    [TestMethod]
    public void GetParameter()
    {
        Controller controller1 = new();

        List<string> result1 = controller1.GetParameterMessage(2);
        List<string> result2 = controller1.GetParameterMessage(4);

        // Standard message :
        Console.WriteLine("test \"GetParameters()\"");

        // Display for debugging :
        Trace.WriteLine("| Parameter message |");

        foreach (string elem1 in result1)
        {
            Trace.WriteLine(elem1);
        }

        Trace.WriteLine("");

        foreach (string elem2 in result2)
        {
            Trace.WriteLine(elem2);
        }
    }

    // Add works and display the works
    [TestMethod]
    public void ExecuteOption()
    {
        Controller controller1 = new();
        List<string> parameter1 = ["name1", "sourceDirectory1", "destinationDirectory1", "type1"];
        List<string> parameter2 = ["name2", "sourceDirectory2", "destinationDirectory2", "type2"];

        controller1.OptionExecuted(2, parameter1);
        controller1.OptionExecuted(2, parameter2);

        List<string> parameter3 = new List<string>();
        var result1 = controller1.OptionExecuted(1, parameter3);

        // Standard message :
        Console.WriteLine("test \"ExecuteOption()\"");

        // Display for debugging :
        Trace.WriteLine("| Work added and displayed |");
        Trace.WriteLine(result1);
    }
}