namespace LanguageFile;

public enum Lang
{
    FR,
    EN
}

public class Language
{
    private static Language? instance;
    private Lang currentLanguage;

    private static readonly Dictionary<Lang, Dictionary<string, string>> translations = new()
    {
        {
            Lang.FR, new Dictionary<string, string>
            {
                { "option_display", "Afficher les travaux" },
                { "option_create", "Créer un nouveau travaux" },
                { "option_execute", "Exécuter un travaux" },
                { "option_language", "Changer la langue" },

                { "create_name", "Saisissez un nom de fichier :" },
                { "create_source", "Saisissez le répertoire source :" },
                { "create_destination", "Saisissez le répertoire de destination :" },
                { "create_type", "Choisissez le type du fichier : \n1. Complet\n2. Différentielle" },

                { "execute_input", "Saisissez une ligne de commande pour exécuter le(les) travaux de votre(vos) choix :" },

                { "language_choice", "Choisissez la langue : \n1. FR\n2. EN" },

                { "display_work_title", "Travaux n°" },
                { "display_file_name", "- Nom du fichier : " },
                { "display_source", "- Répertoire source : " },
                { "display_destination", "- Répertoire destination : " },
                { "display_type", "- Type de sauvegarde : " },

                { "work_saved", "Travaux sauvegardé" },
                { "language_changed", "Langue changée en Français" },

                { "menu_title", "Choisissez une option :" },
                { "invalid_option", "Option invalide" }
            }
        },
        {
            Lang.EN, new Dictionary<string, string>
            {
                { "option_display", "Display works" },
                { "option_create", "Create a new work" },
                { "option_execute", "Execute a work" },
                { "option_language", "Change language" },

                { "create_name", "Enter a file name:" },
                { "create_source", "Enter the source directory:" },
                { "create_destination", "Enter the destination directory:" },
                { "create_type", "Choose the file type: \n1. Full\n2. Differential" },

                { "execute_input", "Enter a command line to execute the work(s) of your choice:" },

                { "language_choice", "Choose the language: \n1. FR\n2. EN" },

                { "display_work_title", "Work n°" },
                { "display_file_name", "- File name: " },
                { "display_source", "- Source directory: " },
                { "display_destination", "- Destination directory: " },
                { "display_type", "- Backup type: " },

                { "work_saved", "Work saved" },
                { "language_changed", "Language changed to English" },

                { "menu_title", "Choose an option:" },
                { "invalid_option", "Invalid option" }
            }
        }
    };

    private Language()
    {
        currentLanguage = Lang.FR;
    }

    public static Language GetInstance()
    {
        if (instance == null)
        {
            instance = new Language();
        }
        return instance;
    }

    public static void Reset()
    {
        instance = null;
    }

    public string GetString(string key)
    {
        return translations[currentLanguage][key];
    }

    public void SetLanguage(Lang language)
    {
        currentLanguage = language;
    }

    public Lang GetCurrentLanguage()
    {
        return currentLanguage;
    }
}
