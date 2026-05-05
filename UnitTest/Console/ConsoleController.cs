/* 
 * Command line for testing :
 * dotnet test --logger "console;verbosity=detailed"
 * dotnet test
*/

using ControllerFile;
using LanguageFile;
using System.Diagnostics;

[TestClass]
[DoNotParallelize]
public sealed class TestConsoleStrategy
{
    [TestInitialize]
    public void Setup()
    {
        Language.Reset();
    }

    private void DisplayMenu(Controller controller)
    {
        Language lang = Language.GetInstance();
        List<string> options = controller.GetOption();

        Trace.WriteLine("========================================");
        Trace.WriteLine("            EasySave Console");
        Trace.WriteLine("========================================");
        Trace.WriteLine(lang.GetString("menu_title"));
        Trace.WriteLine("");

        int index = 1;
        foreach (string option in options)
        {
            Trace.WriteLine($"  {index}. {option}");
            index++;
        }

        Trace.WriteLine("");
    }

    private void DisplayParameterPrompts(Controller controller, int option)
    {
        List<string> parameters = controller.GetParameterMessage(option);

        foreach (string param in parameters)
        {
            Trace.WriteLine($"> {param}");
        }
    }

    private void DisplayResult(string result)
    {
        if (result != "")
        {
            Trace.WriteLine("");
            Trace.WriteLine(result);
        }

        Trace.WriteLine("----------------------------------------");
        Trace.WriteLine("");
    }

    [TestMethod]
    public void GetOption_ReturnsAllOptions()
    {
        Controller controller = new();

        Trace.WriteLine("[TEST] Affichage du menu principal (FR)");
        DisplayMenu(controller);

        List<string> options = controller.GetOption();

        Assert.HasCount(4, options);
        Assert.AreEqual("Afficher les travaux", options[0]);
        Assert.AreEqual("Créer un nouveau travaux", options[1]);
        Assert.AreEqual("Exécuter un travaux", options[2]);
        Assert.AreEqual("Changer la langue", options[3]);
    }

    [TestMethod]
    public void GetParameter_Option2_Returns4Messages()
    {
        Controller controller = new();

        Trace.WriteLine("[TEST] Prompts de saisie pour Option 2 : Creer un travail (FR)");
        DisplayMenu(controller);
        Trace.WriteLine("Utilisateur choisit : 2");
        Trace.WriteLine("");
        DisplayParameterPrompts(controller, 2);

        List<string> parameters = controller.GetParameterMessage(2);

        Assert.HasCount(4, parameters);
        StringAssert.Contains(parameters[0], "nom");
        StringAssert.Contains(parameters[1], "source");
        StringAssert.Contains(parameters[2], "destination");
        StringAssert.Contains(parameters[3], "type");
    }

    [TestMethod]
    public void GetParameter_Option4_Returns1Message()
    {
        Controller controller = new();

        Trace.WriteLine("[TEST] Prompts de saisie pour Option 4 : Changer la langue (FR)");
        DisplayMenu(controller);
        Trace.WriteLine("Utilisateur choisit : 4");
        Trace.WriteLine("");
        DisplayParameterPrompts(controller, 4);

        List<string> parameters = controller.GetParameterMessage(4);

        Assert.HasCount(1, parameters);
        StringAssert.Contains(parameters[0], "FR");
        StringAssert.Contains(parameters[0], "EN");
    }

    [TestMethod]
    public void ExecuteOption_CreateAndDisplay()
    {
        Controller controller = new();

        Trace.WriteLine("[TEST] Scenario complet : creer 2 travaux puis les afficher (FR)");
        Trace.WriteLine("");

        // Création travail 1
        DisplayMenu(controller);
        Trace.WriteLine("Utilisateur choisit : 2");
        Trace.WriteLine("");
        DisplayParameterPrompts(controller, 2);
        Trace.WriteLine("  -> fichier1");
        Trace.WriteLine("  -> C:\\source1");
        Trace.WriteLine("  -> C:\\dest1");
        Trace.WriteLine("  -> Complet");
        string createResult1 = controller.OptionExecuted(2, ["fichier1", "C:\\source1", "C:\\dest1", "Complet"]);
        DisplayResult(createResult1);

        // Création travail 2
        DisplayMenu(controller);
        Trace.WriteLine("Utilisateur choisit : 2");
        Trace.WriteLine("");
        DisplayParameterPrompts(controller, 2);
        Trace.WriteLine("  -> fichier2");
        Trace.WriteLine("  -> C:\\source2");
        Trace.WriteLine("  -> C:\\dest2");
        Trace.WriteLine("  -> Différentielle");
        string createResult2 = controller.OptionExecuted(2, ["fichier2", "C:\\source2", "C:\\dest2", "Différentielle"]);
        DisplayResult(createResult2);

        Assert.AreEqual("Travaux sauvegardé", createResult1);
        Assert.AreEqual("Travaux sauvegardé", createResult2);

        // Affichage des travaux
        DisplayMenu(controller);
        Trace.WriteLine("Utilisateur choisit : 1");
        string displayResult = controller.OptionExecuted(1, []);
        DisplayResult(displayResult);

        StringAssert.Contains(displayResult, "fichier1");
        StringAssert.Contains(displayResult, "fichier2");
        StringAssert.Contains(displayResult, "C:\\source1");
        StringAssert.Contains(displayResult, "C:\\dest2");
    }

    [TestMethod]
    public void DisplayOption_EmptyList_ReturnsEmptyString()
    {
        Controller controller = new();

        Trace.WriteLine("[TEST] Affichage des travaux quand la liste est vide (FR)");
        DisplayMenu(controller);
        Trace.WriteLine("Utilisateur choisit : 1");
        string result = controller.OptionExecuted(1, []);
        DisplayResult(result);

        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void GetOption_InEnglish_ReturnsEnglishOptions()
    {
        Language.GetInstance().SetLanguage(Lang.EN);
        Controller controller = new();

        Trace.WriteLine("[TEST] Affichage du menu principal (EN)");
        DisplayMenu(controller);

        List<string> options = controller.GetOption();

        Assert.HasCount(4, options);
        Assert.AreEqual("Display works", options[0]);
        Assert.AreEqual("Create a new work", options[1]);
        Assert.AreEqual("Execute a work", options[2]);
        Assert.AreEqual("Change language", options[3]);
    }

    [TestMethod]
    public void GetParameter_Option2_InEnglish_ReturnsEnglishMessages()
    {
        Language.GetInstance().SetLanguage(Lang.EN);
        Controller controller = new();

        Trace.WriteLine("[TEST] Prompts de saisie pour Option 2 : Create a new work (EN)");
        DisplayMenu(controller);
        Trace.WriteLine("User selects: 2");
        Trace.WriteLine("");
        DisplayParameterPrompts(controller, 2);

        List<string> parameters = controller.GetParameterMessage(2);

        Assert.HasCount(4, parameters);
        StringAssert.Contains(parameters[0], "name");
        StringAssert.Contains(parameters[1], "source");
        StringAssert.Contains(parameters[2], "destination");
        StringAssert.Contains(parameters[3], "type");
    }

    [TestMethod]
    public void ExecuteOption_CreateAndDisplay_InEnglish()
    {
        Language.GetInstance().SetLanguage(Lang.EN);
        Controller controller = new();

        Trace.WriteLine("[TEST] Scenario complet en anglais : creer 1 travail puis afficher (EN)");
        Trace.WriteLine("");

        // Création
        DisplayMenu(controller);
        Trace.WriteLine("User selects: 2");
        Trace.WriteLine("");
        DisplayParameterPrompts(controller, 2);
        Trace.WriteLine("  -> myFile");
        Trace.WriteLine("  -> C:\\src");
        Trace.WriteLine("  -> C:\\dst");
        Trace.WriteLine("  -> Full");
        string createResult = controller.OptionExecuted(2, ["myFile", "C:\\src", "C:\\dst", "Full"]);
        DisplayResult(createResult);

        Assert.AreEqual("Work saved", createResult);

        // Affichage
        DisplayMenu(controller);
        Trace.WriteLine("User selects: 1");
        string displayResult = controller.OptionExecuted(1, []);
        DisplayResult(displayResult);

        StringAssert.Contains(displayResult, "myFile");
        StringAssert.Contains(displayResult, "File name");
        StringAssert.Contains(displayResult, "Source directory");
    }

    [TestMethod]
    public void ExecuteOption3_ReturnsResult()
    {
        Controller controller = new();

        Trace.WriteLine("[TEST] Execution Option 3 : Executer un travail (FR)");
        DisplayMenu(controller);
        Trace.WriteLine("Utilisateur choisit : 3");
        Trace.WriteLine("");
        DisplayParameterPrompts(controller, 3);
        Trace.WriteLine("  -> 1");
        string result = controller.OptionExecuted(3, ["1"]);
        DisplayResult(result);

        Assert.AreEqual("true", result);
    }

    [TestMethod]
    public void ChangeLanguage_ToFR_WhenAlreadyFR()
    {
        Controller controller = new();

        Trace.WriteLine("[TEST] Changement de langue FR -> FR (FR)");
        DisplayMenu(controller);
        Trace.WriteLine("Utilisateur choisit : 4");
        Trace.WriteLine("");
        DisplayParameterPrompts(controller, 4);
        Trace.WriteLine("  -> 1");
        string result = controller.OptionExecuted(4, ["1"]);
        DisplayResult(result);

        Assert.AreEqual("Langue changée en Français", result);
        Assert.AreEqual(Lang.FR, Language.GetInstance().GetCurrentLanguage());
    }

    [TestMethod]
    public void CreateWork_Max5_ReturnsError()
    {
        Controller controller = new();

        Trace.WriteLine("[TEST] Creation de 6 travaux - le 6eme doit etre refuse (FR)");
        Trace.WriteLine("");

        for (int i = 1; i <= 5; i++)
        {
            string res = controller.OptionExecuted(2, [$"fichier{i}", $"C:\\src{i}", $"C:\\dst{i}", "1"]);
            Trace.WriteLine($"  Travail {i} : {res}");
            Assert.AreEqual("Travaux sauvegardé", res);
        }

        string result = controller.OptionExecuted(2, ["fichier6", "C:\\src6", "C:\\dst6", "1"]);
        Trace.WriteLine($"  Travail 6 : {result}");
        DisplayResult(result);

        Assert.AreEqual("Maximum de 5 travaux atteint", result);
    }

    [TestMethod]
    public void ChangeLanguage_FR_To_EN_FullFlow()
    {
        Controller controller = new();

        Trace.WriteLine("[TEST] Scenario complet : changement FR -> EN puis affichage du menu");
        Trace.WriteLine("");

        // Menu en FR
        Trace.WriteLine("--- Avant changement ---");
        DisplayMenu(controller);

        // Changement de langue
        Trace.WriteLine("Utilisateur choisit : 4");
        Trace.WriteLine("");
        DisplayParameterPrompts(controller, 4);
        Trace.WriteLine("  -> 2");
        string result = controller.OptionExecuted(4, ["2"]);
        DisplayResult(result);

        Assert.AreEqual("Language changed to English", result);

        // Menu en EN
        Trace.WriteLine("--- Apres changement ---");
        DisplayMenu(controller);

        List<string> options = controller.GetOption();
        Assert.AreEqual("Display works", options[0]);
        Assert.AreEqual("Change language", options[3]);
    }
}

[TestClass]
[DoNotParallelize]
public sealed class TestLanguage
{
    [TestInitialize]
    public void Setup()
    {
        Language.Reset();
    }

    [TestMethod]
    public void DefaultLanguage_IsFrench()
    {
        Language lang = Language.GetInstance();

        Assert.AreEqual(Lang.FR, lang.GetCurrentLanguage());
    }

    [TestMethod]
    public void GetString_French_ReturnsCorrectValue()
    {
        Language lang = Language.GetInstance();

        Assert.AreEqual("Afficher les travaux", lang.GetString("option_display"));
        Assert.AreEqual("Travaux sauvegardé", lang.GetString("work_saved"));
        Assert.AreEqual("Choisissez une option :", lang.GetString("menu_title"));
    }

    [TestMethod]
    public void SetLanguage_ToEnglish_ReturnsEnglishStrings()
    {
        Language lang = Language.GetInstance();
        lang.SetLanguage(Lang.EN);

        Assert.AreEqual(Lang.EN, lang.GetCurrentLanguage());
        Assert.AreEqual("Display works", lang.GetString("option_display"));
        Assert.AreEqual("Work saved", lang.GetString("work_saved"));
        Assert.AreEqual("Choose an option:", lang.GetString("menu_title"));
    }

    [TestMethod]
    public void SwitchLanguage_BackAndForth()
    {
        Language lang = Language.GetInstance();

        lang.SetLanguage(Lang.EN);
        Assert.AreEqual("Display works", lang.GetString("option_display"));

        lang.SetLanguage(Lang.FR);
        Assert.AreEqual("Afficher les travaux", lang.GetString("option_display"));
    }

    [TestMethod]
    public void Singleton_ReturnsSameInstance()
    {
        Language lang1 = Language.GetInstance();
        Language lang2 = Language.GetInstance();

        Assert.AreSame(lang1, lang2);
    }

    [TestMethod]
    public void ChangeLanguage_ViaController_SwitchesToEnglish()
    {
        Controller controller = new();

        controller.OptionExecuted(4, ["2"]);

        Language lang = Language.GetInstance();
        Assert.AreEqual(Lang.EN, lang.GetCurrentLanguage());

        List<string> options = controller.GetOption();
        Assert.AreEqual("Display works", options[0]);
        Assert.AreEqual("Create a new work", options[1]);
    }

    [TestMethod]
    public void ChangeLanguage_ViaController_InvalidChoice_ReturnsError()
    {
        Controller controller = new();

        string result = controller.OptionExecuted(4, ["99"]);

        Assert.AreEqual("Option invalide", result);
    }

    [TestMethod]
    public void Reset_CreatesNewInstance()
    {
        Language lang1 = Language.GetInstance();
        lang1.SetLanguage(Lang.EN);

        Language.Reset();

        Language lang2 = Language.GetInstance();
        Assert.AreEqual(Lang.FR, lang2.GetCurrentLanguage());
    }

    [TestMethod]
    public void AllKeys_ExistInBothLanguages()
    {
        Language lang = Language.GetInstance();

        string[] keys = [
            "option_display", "option_create", "option_execute", "option_language",
            "create_name", "create_source", "create_destination", "create_type",
            "execute_input", "language_choice",
            "display_work_title", "display_file_name", "display_source",
            "display_destination", "display_type",
            "work_saved", "work_max_reached", "language_changed", "menu_title", "invalid_option"
        ];

        lang.SetLanguage(Lang.FR);
        foreach (string key in keys)
        {
            Assert.IsNotNull(lang.GetString(key), $"Clé manquante en FR : {key}");
        }

        lang.SetLanguage(Lang.EN);
        foreach (string key in keys)
        {
            Assert.IsNotNull(lang.GetString(key), $"Missing key in EN: {key}");
        }
    }
}
