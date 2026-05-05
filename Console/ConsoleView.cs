namespace ConsoleViewFile;

using LanguageFile;
using ControllerFile;

public class ConsoleView
{
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
        string[] langOptions = ["  Français", "  English"];
        int selected = ArrowSelect(langOptions, "  Select your language / Choisissez votre langue :");

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
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine("  ┌──────────────────────────────────────┐");
        Console.WriteLine("  │            E A S Y S A V E           │");
        Console.WriteLine("  └──────────────────────────────────────┘");
        Console.ResetColor();
        Console.WriteLine();
    }

    private int MenuSelect()
    {
        Console.Clear();
        DisplayHeader();

        List<string> options = _controller.GetOption();

        string[] menuItems = new string[options.Count + 1];
        for (int i = 0; i < options.Count; i++)
        {
            menuItems[i] = $"  {options[i]}";
        }
        menuItems[options.Count] = "  Exit / Quitter";

        string title = $"  {_language.GetString("menu_title")}";
        int selected = ArrowSelect(menuItems, title);

        if (selected == options.Count) return -1;
        return selected;
    }

    private int ArrowSelect(string[] options, string title)
    {
        int selected = 0;

        while (true)
        {
            RenderOptions(options, selected, title);

            ConsoleKeyInfo key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.UpArrow:
                    selected = (selected - 1 + options.Length) % options.Length;
                    break;
                case ConsoleKey.DownArrow:
                    selected = (selected + 1) % options.Length;
                    break;
                case ConsoleKey.Enter:
                    return selected;
            }
        }
    }

    private void RenderOptions(string[] options, int selected, string title)
    {
        int startLine = Console.CursorTop;

        // Clear previous render
        if (startLine > 0)
        {
            Console.SetCursorPosition(0, startLine - options.Length - 3);
        }

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine($"\n{title}\n");
        Console.ResetColor();

        for (int i = 0; i < options.Length; i++)
        {
            if (i == selected)
            {
                Console.ForegroundColor = ConsoleColor.Black;
                Console.BackgroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"  ► {options[i]}  ");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"    {options[i]}");
                Console.ResetColor();
            }
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine("\n  ↑↓ Naviguer  │  Enter Valider");
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
        List<string> collectedParameters = new List<string>();

        foreach (string message in parameterMessages)
        {
            string value = GetInput(message);
            collectedParameters.Add(value);
        }

        string result = _controller.OptionExecuted(input, collectedParameters);

        if (!string.IsNullOrEmpty(result))
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ {result}");
            Console.ResetColor();
        }
    }
}
