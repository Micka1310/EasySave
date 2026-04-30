namespace ConsoleStrategyFile;

using WorkListFile;
using WorkFile;

// Interface for creating strategies
public interface IStrategy
{
    // Attributs
    string option { get; }
    List<string> parameterMessage { get; }

    // Methods
    public string Execution(List<string> parameters, WorkList workList);
}

// Option 1 : display all the works
public class DisplayWork1 : IStrategy
{
    // Attributes
    public string option { get; } = "Afficher les travaux";
    public List<string> parameterMessage { get; } = [];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        string displayString = "";
        int index = 0;

        List<Work> WorkList = workList.GetWork();

        foreach (Work elem in WorkList)
        {
            index = index + 1;
            displayString = displayString + $"Travaux n°{index} :\n" + "- Nom du fichier : " + elem.GetName() + "\n- Répertoire source : " + elem.GetSourceDirectory() + "\n- Répertoire destination : " + elem.GetDestinationDirectory() + "\n- Type de sauvegarde : " + elem.GetWorkType() + "\n\n ";
        }

        return displayString;
    }
}

// option 2 : create a new work
public class CreateWork2 : IStrategy
{
    // Attributes
    public string option { get; set; } = "Créer un nouveau travaux";
    public List<string> parameterMessage { get; set; } = ["Saisissez un nom de fichier :", "Saisissez le répertoire source :","Saisissez le répertoire de destination :","Choisissez le type du fichier : \n1. Complet\n2. Différentielle"];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        workList.AddWork(parameters);

        return "Travaux sauvegardé";
    }
}

// option 3 : execute a work
public class ExecuteWork3 : IStrategy
{
    // Attributes
    public string option { get; set; } = "Exécuter un travaux";
    public List<string> parameterMessage { get; set; } = ["Saisissez une ligne de commande pour exécuter le(les) travaux de votre(vos) choix :"];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        // Waiting for the others to commit their works...

        return "";
    }
}

// option 4 : change the language
public class ChangeLanguage4 : IStrategy
{
    // Attributes
    public string option { get; set; } = "Changer la langue";
    public List<string> parameterMessage { get; set; } = ["Choisissez la langue : \n1. FR\n2. EN"];

    // Methods
    public string Execution(List<string> parameters, WorkList workList)
    {
        // Waiting for the others to commit their works...

        return "";
    }
}