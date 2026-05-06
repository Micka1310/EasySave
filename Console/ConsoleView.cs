namespace ConsoleViewFile;

using ConsoleStrategyFile;
using LanguageFile;
using ControllerFile;
using StateFileLib;

public class ConsoleView
{
    private const int OptionDisplayWorks = 1;
    private const int OptionExecuteWork = 3;
    private const int OptionDeleteWork = 4;

    private readonly Controller _controller;
    private readonly Language _language;

    public ConsoleView(Controller controller)
    {
        _controller = controller;
        _language = Language.GetInstance();
    }

    public void Run()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        Console.CursorVisible = false;

        DisplayBanner();
        Thread.Sleep(1500);
        ChooseLanguage();

        while (true)
        {
            int choice = MenuSelect();
            if (choice == -1) break;

            Console.Clear();
            DisplayHeader();
            SendInput(choice + 1);
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  Appuyez sur une touche pour continuer...");
            Console.ResetColor();
            Console.ReadKey(true);
        }

        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("\n  Au revoir ! / Goodbye!\n");
        Console.ResetColor();
        Console.CursorVisible = true;
    }

    private void DisplayBanner()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(@"
    ███████╗ █████╗ ███████╗██╗   ██╗███████╗ █████╗ ██╗   ██╗███████╗
    ██╔════╝██╔══██╗██╔════╝╚██╗ ██╔╝██╔════╝██╔══██╗██║   ██║██╔════╝
    █████╗  ███████║███████╗ ╚████╔╝ ███████╗███████║██║   ██║█████╗  
    ██╔══╝  ██╔══██║╚════██║  ╚██╔╝  ╚════██║██╔══██║╚██╗ ██╔╝██╔══╝  
    ███████╗██║  ██║███████║   ██║   ███████║██║  ██║ ╚████╔╝ ███████╗
    ╚══════╝╚═╝  ╚═╝╚══════╝   ╚═╝   ╚══════╝╚═╝  ╚═╝  ╚═══╝ ╚══════╝
        ");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("    ─────────────────────────────────────────────────────────────────");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("                        Backup Management Tool v1.0");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("    ─────────────────────────────────────────────────────────────────\n");
        Console.ResetColor();
    }

    private void ChooseLanguage()
    {
        string[] langOptions = ["Français", "English"];
        int selected = ArrowSelect(langOptions, "Select your language / Choisissez votre langue", false);

        switch (selected)
        {
            case 0:
                _language.SetLanguage(Lang.FR);
                break;
            case 1:
                _language.SetLanguage(Lang.EN);
                break;
            default:
                _language.SetLanguage(Lang.FR);
                break;
        }
    }

    private void DisplayHeader()
    {
        string langLabel = _language.GetCurrentLanguage() == Lang.FR ? "FR" : "EN";

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────────────────────┐");
        Console.WriteLine("  │              E A S Y S A V E             │");
        Console.WriteLine("  └──────────────────────────────────────────┘");
        Console.ResetColor();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  Langue : ");
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(langLabel);
        Console.ResetColor();
        Console.WriteLine();
    }

    private int MenuSelect()
    {
        List<string> options = _controller.GetOption();

        string[] menuItems = new string[options.Count + 1];
        for (int i = 0; i < options.Count; i++)
        {
            menuItems[i] = options[i];
        }
        menuItems[options.Count] = "Exit / Quitter";

        string title = _language.GetString("menu_title");
        int selected = ArrowSelect(menuItems, title, true);

        if (selected == -1 || selected == options.Count) return -1;
        return selected;
    }

    private int ArrowSelect(string[] options, string title, bool allowEscape)
    {
        int selected = 0;

        while (true)
        {
            DrawScreen(options, selected, title, allowEscape);

            ConsoleKeyInfo key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selected = (selected - 1 + options.Length) % options.Length;
                    break;
                case ConsoleKey.DownArrow:
                case ConsoleKey.Tab:
                    selected = (selected + 1) % options.Length;
                    break;
                case ConsoleKey.Enter:
                    return selected;
                case ConsoleKey.Escape:
                    if (allowEscape) return -1;
                    break;
            }
        }
    }

    private void DrawScreen(string[] options, int selected, string title, bool allowEscape)
    {
        Console.Clear();
        DisplayHeader();

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"  {title}");
        Console.ResetColor();
        Console.WriteLine();

        for (int i = 0; i < options.Length; i++)
        {
            if (i == selected)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                string line = $"   ► {options[i]}";
                Console.Write(line);
                int pad = 50 - line.Length;
                if (pad > 0) Console.Write(new string(' ', pad));
                Console.ResetColor();
                Console.WriteLine();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"     {options[i]}");
                Console.ResetColor();
            }
        }

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ┌──────────────────────────────────────────┐");

        Console.Write("  │  ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("↑ ↓");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(" Naviguer    ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Tab");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(" Suivant        ");
        Console.WriteLine("│");

        Console.Write("  │  ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write("Enter");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(" Valider  ");
        if (allowEscape)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Esc");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" Retour         ");
        }
        else
        {
            Console.Write("                    ");
        }
        Console.WriteLine("│");

        Console.WriteLine("  └──────────────────────────────────────────┘");
        Console.ResetColor();
    }

    private string GetInput(string prompt)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.Write($"  {prompt} ");
        Console.ResetColor();
        Console.CursorVisible = true;
        string input = Console.ReadLine() ?? string.Empty;
        Console.CursorVisible = false;
        return input;
    }

    public void SendInput(int input)
    {
        List<string> parameterMessages = _controller.GetParameterMessage(input);
        string[] fieldValues = new string[parameterMessages.Count];
        int startFieldIndex = 0;

        bool showWorksList = input is OptionExecuteWork or OptionDeleteWork;

        if (input == OptionExecuteWork)
        {
            _controller.SetProgressCallback(DisplayLiveProgress);
        }

        try
        {
            while (true)
            {
                Console.Clear();
                DisplayHeader();

                if (showWorksList && parameterMessages.Count > 0)
                {
                    Console.ForegroundColor = ConsoleColor.White;
                    Console.WriteLine(_controller.GetNumberedWorksSummary());
                    Console.ResetColor();
                    Console.WriteLine();
                }

                for (int i = startFieldIndex; i < parameterMessages.Count; i++)
                {
                    fieldValues[i] = GetInput(parameterMessages[i]);
                }

                List<string> collectedParameters = fieldValues.Select(static s => s ?? string.Empty).ToList();
                string result = _controller.OptionExecuted(input, collectedParameters);
                WriteActionResult(result, input);

                if (!_language.ShouldPromptAgainForMessage(result))
                {
                    break;
                }

                startFieldIndex = _language.GetRetryFieldIndexForMessage(result) ?? 0;

                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($"  {_language.GetString("prompt_retry_input")}");
                Console.ResetColor();
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine("  Appuyez sur une touche pour corriger votre saisie...");
                Console.ResetColor();
                Console.ReadKey(true);
            }
        }
        finally
        {
            if (input == OptionExecuteWork)
            {
                _controller.SetProgressCallback(null);
            }
        }
    }

    private void DisplayLiveProgress(WorkState state)
    {
        Lang lang = _language.GetCurrentLanguage();

        int barWidth = 30;
        int filled = (int)(state.Progression / 100.0 * barWidth);
        string bar = new string('█', filled) + new string('░', barWidth - filled);

        string sizeTotal = BackupProgressHelper.FormatBytes(state.TotalSize, lang);
        string sizeRemaining = BackupProgressHelper.FormatBytes(state.RemainingSize, lang);

        bool done = state.RemainingFiles == 0;
        string statusLabel = done
            ? _language.GetString("progress_done")
            : _language.GetString("progress_status");

        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("  ────────────────────────────────────────────");
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"  {_language.GetString("progress_job")}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write(state.WorkName);
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write("  │  ");
        Console.ForegroundColor = done ? ConsoleColor.Green : ConsoleColor.Yellow;
        Console.WriteLine(statusLabel);
        Console.ResetColor();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  {_language.GetString("progress_bar")}: ");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write($"[{bar}]");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($" {state.Progression}%");

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  {_language.GetString("progress_files")}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{state.TotalFiles - state.RemainingFiles}/{state.TotalFiles}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  │  {_language.GetString("progress_size")}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(sizeTotal);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  {_language.GetString("progress_remaining")}: ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.Write($"{state.RemainingFiles} {_language.GetString("progress_files").ToLowerInvariant()}");
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  │  ");
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(sizeRemaining);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write($"  {_language.GetString("progress_current_file")}: ");
        Console.ForegroundColor = ConsoleColor.Gray;
        string fileName = Path.GetFileName(state.CurrentSourceFile);
        Console.WriteLine(fileName);

        Console.ResetColor();
    }

    private void WriteActionResult(string result, int menuOption)
    {
        if (string.IsNullOrEmpty(result))
        {
            return;
        }

        Console.WriteLine();

        if (menuOption == OptionDisplayWorks)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(result);
            Console.ResetColor();
            return;
        }

        bool okGreen =
            string.Equals(result, "true", StringComparison.OrdinalIgnoreCase)
            || result == _language.GetString("work_saved")
            || result == _language.GetString("language_changed_to_fr")
            || result == _language.GetString("language_changed_to_en")
            || result == _language.GetString("delete_success");

        if (string.Equals(result, "false", StringComparison.OrdinalIgnoreCase))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ {result}");
        }
        else if (!okGreen)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ! {result}");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {result}");
        }

        Console.ResetColor();
    }
}
