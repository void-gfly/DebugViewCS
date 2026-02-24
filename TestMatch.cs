using System;
using System.Windows.Media;

class Program
{
    static void Main()
    {
        string message = ""Credited arrival deposit: ScoutId=503161, UserId=kfk68vu17xcl9vw, Amount=61.079062421635065"";
        string matchText = ""UserId=  "";
        matchText = matchText.Trim();
        bool contains = message.IndexOf(matchText, StringComparison.OrdinalIgnoreCase) >= 0;
        Console.WriteLine($""MatchText after trim: '{matchText}'"");
        Console.WriteLine($""Contains: {contains}"");
    }
}
