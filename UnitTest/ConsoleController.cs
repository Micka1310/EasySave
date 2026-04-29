/* 
 * Command line for testing :
 * dotnet test --logger "console;verbosity=detailed"
 * dotnet test
*/

using ControllerFile;
using LanguageFile;

[TestClass]
[DoNotParallelize]
public sealed class TestConsoleStrategy
{
    [TestInitialize]
    public void Setup()
    {
        Language.Reset();
    }

    [TestMethod]
    public void GetOption_ReturnsAllOptions()
    {
        Controller controller = new();
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
        List<string> parameters = controller.GetParameterMessage(4);

        Assert.HasCount(1, parameters);
        StringAssert.Contains(parameters[0], "FR");
        StringAssert.Contains(parameters[0], "EN");
    }

    [TestMethod]
    public void ExecuteOption_CreateAndDisplay()
    {
        Controller controller = new();

        List<string> param1 = ["fichier1", "C:\\source1", "C:\\dest1", "Complet"];
        List<string> param2 = ["fichier2", "C:\\source2", "C:\\dest2", "Différentielle"];

        string createResult1 = controller.OptionExecuted(2, param1);
        string createResult2 = controller.OptionExecuted(2, param2);

        Assert.AreEqual("Travaux sauvegardé", createResult1);
        Assert.AreEqual("Travaux sauvegardé", createResult2);

        string displayResult = controller.OptionExecuted(1, []);

        StringAssert.Contains(displayResult, "fichier1");
        StringAssert.Contains(displayResult, "fichier2");
        StringAssert.Contains(displayResult, "C:\\source1");
        StringAssert.Contains(displayResult, "C:\\dest2");
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
            "work_saved", "language_changed", "menu_title", "invalid_option"
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
