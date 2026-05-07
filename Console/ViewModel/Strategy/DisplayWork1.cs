using System;
using System.Globalization;
using ConsoleStrategyFile
using WorkListFile;
using WorkFile;
using LanguageFile;

namespace DisplayWork1File;

// Option 1 : display all the works
public class DisplayWork1 : IStrategy
{
    public string option => Language.GetInstance().GetString("option_display");
    public List<string> parameterMessage => [];

    public string Execution(List<string> parameters, WorkList workList)
    {
        Language lang = Language.GetInstance();
        string displayString = "";
        int index = 0;

        List<Work> WorkList = workList.GetWork();

        foreach (Work elem in WorkList)
        {
            index++;
            string typeLabel = elem.GetWorkType() == "1"
                ? lang.GetString("backup_type_short_full")
                : lang.GetString("backup_type_short_diff");

            displayString += lang.GetString("display_work_title") + $"{index} :\n"
                + lang.GetString("display_file_name") + elem.GetName() + "\n"
                + lang.GetString("display_source") + elem.GetSourceDirectory() + "\n"
                + lang.GetString("display_destination") + elem.GetDestinationDirectory() + "\n"
                + lang.GetString("display_type") + typeLabel + "\n\n ";
        }

        return displayString;
    }
}