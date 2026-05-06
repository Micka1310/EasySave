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
        string worksFile = Path.Combine(AppContext.BaseDirectory, "works.json");
        if (File.Exists(worksFile)) File.Delete(worksFile);
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
        Language lang = Language.GetInstance();

        Trace.WriteLine("[TEST] Affichage du menu principal (FR)");
        DisplayMenu(controller);

        List<string> options = controller.GetOption();

        Assert.HasCount(5, options);
        Assert.AreEqual("Afficher les travaux", options[0]);
        Assert.AreEqual("Créer un nouveau travaux", options[1]);
        Assert.AreEqual("Exécuter un travaux", options[2]);
        Assert.AreEqual(lang.GetString("option_delete"), options[3]);
        Assert.AreEqual(lang.GetString("option_language"), options[4]);
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
    public void GetParameter_Option5_Returns1Message_Language()
    {
        Controller controller = new();

        List<string> parameters = controller.GetParameterMessage(5);

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

        string createResult1 = controller.OptionExecuted(2, ["fichier1", "C:\\source1", "C:\\dest1", "Complet"]);
        DisplayResult(createResult1);

        string createResult2 = controller.OptionExecuted(2, ["fichier2", "C:\\source2", "C:\\dest2", "Différentielle"]);
        DisplayResult(createResult2);

        Assert.AreEqual("Travaux sauvegardé", createResult1);
        Assert.AreEqual("Travaux sauvegardé", createResult2);

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
        string result = controller.OptionExecuted(1, []);
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void GetOption_InEnglish_ReturnsEnglishOptions()
    {
        Language lang = Language.GetInstance();
        lang.SetLanguage(Lang.EN);
        Controller controller = new();

        List<string> options = controller.GetOption();

        Assert.HasCount(5, options);
        Assert.AreEqual("Display works", options[0]);
        Assert.AreEqual("Create a new work", options[1]);
        Assert.AreEqual("Execute a work", options[2]);
        Assert.AreEqual(lang.GetString("option_delete"), options[3]);
        Assert.AreEqual(lang.GetString("option_language"), options[4]);
    }

    [TestMethod]
    public void GetParameter_Option2_InEnglish_ReturnsEnglishMessages()
    {
        Language.GetInstance().SetLanguage(Lang.EN);
        Controller controller = new();

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

        string createResult = controller.OptionExecuted(2, ["myFile", "C:\\src", "C:\\dst", "Full"]);
        Assert.AreEqual("Work saved", createResult);

        string displayResult = controller.OptionExecuted(1, []);
        StringAssert.Contains(displayResult, "myFile");
        StringAssert.Contains(displayResult, "File name");
        StringAssert.Contains(displayResult, "Source directory");
    }

    [TestMethod]
    public void ExecuteOption3_ReturnsResult()
    {
        Controller controller = new();
        string id = Guid.NewGuid().ToString("N");
        string tempSrc = Path.Combine(Path.GetTempPath(), "EasySaveExecTest_src_" + id);
        string tempDst = Path.Combine(Path.GetTempPath(), "EasySaveExecTest_dst_" + id);
        Directory.CreateDirectory(tempSrc);
        File.WriteAllText(Path.Combine(tempSrc, "test.txt"), "easy");

        try
        {
            string create = controller.OptionExecuted(2, ["execTest", tempSrc, tempDst, "1"]);
            Assert.AreEqual("Travaux sauvegardé", create);

            string result = controller.OptionExecuted(3, ["1"]);
            Assert.AreEqual("true", result);
        }
        finally
        {
            try { if (Directory.Exists(tempDst)) Directory.Delete(tempDst, true); } catch { }
            try { if (Directory.Exists(tempSrc)) Directory.Delete(tempSrc, true); } catch { }
        }
    }

    [TestMethod]
    public void ChangeLanguage_ToFR_WhenAlreadyFR()
    {
        Controller controller = new();
        string result = controller.OptionExecuted(5, ["1"]);
        Assert.AreEqual(Language.GetInstance().GetString("language_changed_to_fr"), result);
        Assert.AreEqual(Lang.FR, Language.GetInstance().GetCurrentLanguage());
    }

    [TestMethod]
    public void ExecuteOption3_InvalidInput_ReturnsFormatError()
    {
        Controller controller = new();
        controller.OptionExecuted(2, ["w", "C:\\a", "C:\\b", "1"]);

        string bad = controller.OptionExecuted(3, ["abc"]);
        StringAssert.Contains(bad, "Format");

        string badFr = controller.OptionExecuted(3, ["1 abc"]);
        StringAssert.Contains(badFr, "Format");
    }

    [TestMethod]
    public void ExecuteOption3_NoWorks_ReturnsMessage()
    {
        Controller controller = new();
        string result = controller.OptionExecuted(3, ["1"]);
        StringAssert.Contains(result, "travail");
    }

    [TestMethod]
    public void CreateWork_InvalidType_ReturnsError()
    {
        Controller controller = new();
        string result = controller.OptionExecuted(2, ["n", "C:\\s", "C:\\d", "typo"]);
        StringAssert.Contains(result, "Type");
    }

    [TestMethod]
    public void CreateWork_EmptyName_ReturnsError()
    {
        Controller controller = new();
        string result = controller.OptionExecuted(2, ["   ", "C:\\s", "C:\\d", "1"]);
        StringAssert.Contains(result, "nom");
    }

    [TestMethod]
    public void CreateWork_Max5_ReturnsError()
    {
        Controller controller = new();

        for (int i = 1; i <= 5; i++)
        {
            string res = controller.OptionExecuted(2, [$"fichier{i}", $"C:\\src{i}", $"C:\\dst{i}", "1"]);
            Assert.AreEqual("Travaux sauvegardé", res);
        }

        string result = controller.OptionExecuted(2, ["fichier6", "C:\\src6", "C:\\dst6", "1"]);
        Assert.AreEqual("Maximum de 5 travaux atteint", result);
    }

    [TestMethod]
    public void ChangeLanguage_FR_To_EN_FullFlow()
    {
        Controller controller = new();

        string result = controller.OptionExecuted(5, ["2"]);
        Assert.AreEqual("Language changed to English", result);

        List<string> options = controller.GetOption();
        Assert.AreEqual("Display works", options[0]);
        Assert.AreEqual("Change language", options[4]);
    }

    [TestMethod]
    public void DeleteWork_RemovesWork()
    {
        Controller controller = new();
        controller.OptionExecuted(2, ["toDelete", "C:\\s", "C:\\d", "1"]);

        string display = controller.OptionExecuted(1, []);
        StringAssert.Contains(display, "toDelete");

        string del = controller.OptionExecuted(4, ["1"]);
        Assert.AreEqual(Language.GetInstance().GetString("delete_success"), del);

        string after = controller.OptionExecuted(1, []);
        Assert.AreEqual("", after);
    }

    [TestMethod]
    public void DeleteWork_InvalidIndex_ReturnsError()
    {
        Controller controller = new();
        controller.OptionExecuted(2, ["tmp", "C:\\a", "C:\\b", "1"]);
        string result = controller.OptionExecuted(4, ["99"]);
        Assert.AreEqual(Language.GetInstance().GetString("delete_invalid"), result);
    }

    [TestMethod]
    public void DeleteWork_NoWorks_ReturnsMessage()
    {
        Controller controller = new();
        string result = controller.OptionExecuted(4, ["1"]);
        Assert.AreEqual(Language.GetInstance().GetString("delete_no_jobs"), result);
    }

    [TestMethod]
    public void WorksPersistAfterNewController()
    {
        Controller controller1 = new();
        controller1.OptionExecuted(2, ["persist", "C:\\p1", "C:\\p2", "1"]);

        Controller controller2 = new();
        string display = controller2.OptionExecuted(1, []);
        StringAssert.Contains(display, "persist");
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
        string worksFile = Path.Combine(AppContext.BaseDirectory, "works.json");
        if (File.Exists(worksFile)) File.Delete(worksFile);

        Controller controller = new();
        controller.OptionExecuted(5, ["2"]);

        Language lang = Language.GetInstance();
        Assert.AreEqual(Lang.EN, lang.GetCurrentLanguage());

        List<string> options = controller.GetOption();
        Assert.AreEqual("Display works", options[0]);
        Assert.AreEqual("Create a new work", options[1]);
    }

    [TestMethod]
    public void ChangeLanguage_ViaController_InvalidChoice_ReturnsError()
    {
        string worksFile = Path.Combine(AppContext.BaseDirectory, "works.json");
        if (File.Exists(worksFile)) File.Delete(worksFile);

        Controller controller = new();
        string result = controller.OptionExecuted(5, ["99"]);
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
            "option_display", "option_create", "option_execute", "option_delete", "option_language",
            "create_name", "create_source", "create_destination", "create_type",
            "execute_input", "execute_jobs_header", "execute_no_jobs_yet",
            "backup_type_short_full", "backup_type_short_diff",
            "language_choice",
            "delete_input", "delete_no_jobs", "delete_success", "delete_invalid",
            "progress_job", "progress_status", "progress_done", "progress_error",
            "progress_files", "progress_size", "progress_remaining", "progress_bar", "progress_current_file",
            "display_work_title", "display_file_name", "display_source",
            "display_destination", "display_type",
            "work_saved", "work_max_reached", "language_changed_to_fr", "language_changed_to_en",
            "menu_title", "invalid_option", "prompt_retry_input",
            "error_empty_execute_input", "error_no_works_to_execute", "error_invalid_execute_format",
            "error_invalid_work_selection", "error_empty_work_name", "error_empty_source",
            "error_empty_destination",
            "error_invalid_backup_type", "error_missing_create_parameters"
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
