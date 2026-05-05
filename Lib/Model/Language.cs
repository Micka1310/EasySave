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
                { "option_delete", "Supprimer un travail" },
                { "option_exit", "Quitter" },

                { "create_name", "Saisissez un nom de fichier :" },
                { "create_source", "Saisissez le répertoire source :" },
                { "create_destination", "Saisissez le répertoire de destination :" },
                { "create_type", "Choisissez le type du fichier : \n1. Complet\n2. Différentielle" },

                { "execute_input", "Saisissez une ligne de commande pour exécuter le(les) travaux de votre(vos) choix :" },

                { "language_choice", "Choisissez la langue : \n1. FR\n2. EN" },

                { "work_to_delete", "Saisissez le nom du travail à supprimer :" },

                { "display_work_title", "Travaux n°" },
                { "display_file_name", "- Nom du fichier : " },
                { "display_source", "- Répertoire source : " },
                { "display_destination", "- Répertoire destination : " },
                { "display_type", "- Type de sauvegarde : " },

                { "work_saved", "Travaux sauvegardé" },
                { "work_transfered", "Travail(aux) sauvegardé" },
                { "work_not_transfered", "Impossible de transféré" },
                { "language_changed", "Langue changée en Français" },
                { "work_deleted", "Travail supprimer" },
                { "work_not_deleted", "Aucun travail ne porte le nom que vous avez saisie" },

                { "menu_title", "Choisissez une option :" },

                { "invalid_option", "Option invalide" },
                { "nonexistent_option", "Le chiffre saisit ne correspond à aucune options" },

                { "5_works", "Vous ne pouvez sauvegarder que 5 travaux maximum" },
                { "same_work_name", "Le nom choisi a déjà été attribué à un autre travail" },
                { "wrong_type_transfer", "La saisie pour le type du fichier est invalide" },

                { "invalid_input", "Saisie invalide" },

                { "wrong_num_work", "Le travail spécifié n'existe pas" }
            }
        },
        {
            Lang.EN, new Dictionary<string, string>
            {
                { "option_display", "Display works" },
                { "option_create", "Create a new work" },
                { "option_execute", "Execute a work" },
                { "option_language", "Change language" },
                { "option_delete", "Delete a work" },
                { "option_exit", "Exit" },

                { "create_name", "Enter a file name:" },
                { "create_source", "Enter the source directory:" },
                { "create_destination", "Enter the destination directory:" },
                { "create_type", "Choose the file type: \n1. Full\n2. Differential" },

                { "execute_input", "Enter a command line to execute the work(s) of your choice:" },

                { "language_choice", "Choose the language: \n1. FR\n2. EN" },

                { "work_to_delete", "Enter the name of the work you want to delete :" },

                { "display_work_title", "Work n°" },
                { "display_file_name", "- File name: " },
                { "display_source", "- Source directory: " },
                { "display_destination", "- Destination directory: " },
                { "display_type", "- Backup type: " },

                { "work_saved", "Work saved" },
                { "work_transfered", "work(s) transferred" },
                { "work_not_transfered", "Impossible to transfer" },
                { "language_changed", "Language changed to English" },
                { "work_deleted", "Work deleted" },
                { "work_not_deleted", "No work have the same name that you entered" },

                { "menu_title", "Choose an option:" },
                { "invalid_option", "Invalid option" },
                { "nonexistent_option", "The number inputed does not match any options" },

                { "5_works", "You can only save a maximum of 5 works" },
                { "same_work_name", "The name choosed is already given to another work" },
                { "wrong_type_transfer", "The input for the transfer type is invalid" },

                { "invalid_input", "Invalid input" },

                { "wrong_num_work", "The specified work does not exist" }
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
