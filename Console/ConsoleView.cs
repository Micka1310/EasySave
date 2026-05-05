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

        /// <summary>
        /// Entry point : asks for language first, then starts the main loop.
        /// </summary>
        public void Run()
        {
            ChooseLanguage();

            while (true)
            {
                Menu();

                int choice = GetInput();
                if (choice == 0) break;
                SendInput(choice);
                Console.WriteLine();
            }
        }

        /// <summary>
        /// Displayed once at startup to let the user choose their preferred language.
        /// </summary>
        private void ChooseLanguage()
        {
            Console.WriteLine("1. Français");
            Console.WriteLine("2. English");
            Console.Write("> ");

            int choice = GetInput();

            switch (choice)
            {
                case 1:
                    _language.SetLanguage(Lang.FR);
                    break;
                case 2:
                    _language.SetLanguage(Lang.EN);
                    break;
                default:
                    // Default to Frech if invalid input
                    _language.SetLanguage(Lang.FR);
                    break;
            }
        }

        /// <summary>
        /// Fetches available options from the controller and displays the menu.
        /// </summary>
        public void Menu()
        {
            Console.WriteLine($"\n >>>>>>>>>> EasySave Menu <<<<<<<<<<");
            Console.WriteLine(_language.GetString("menu_title"));

            List<string> options = _controller.GetOption();

            for (int i = 0; i < options.Count; i++)
            {
                Console.WriteLine($" {i + 1}. {options[i]}");
            }

            Console.WriteLine("0. Exit");
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>");
            Console.Write("> ");
        }

        /// <summary>
        /// Reads and validates an integrer input from the user.
        /// </summary>
        public int GetInput()
        {
            string raw = Console.ReadLine() ?? string.Empty;

            if (int.TryParse(raw, out int result))
                return result;
            Console.WriteLine(_language.GetString("invalid_option"));
            return GetInput();
        }

        /// <summary>
        /// Reads a string input frop the user after displaying a prompt.
        /// </summary>
        private string GetInput(string prompt)
        {
            Console.Write($"{prompt} ");
            return Console.ReadLine() ?? string.Empty;
        }

        /// <summary>
        /// Processes the user;s menu choise
        /// Fetches required parameters, collects them, then calls the controller.
        /// </summary>
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
            Console.WriteLine(result);
        }
    }

