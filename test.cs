using System;
using System.IO;

class Program
{
    static void Main()
    {
        string rule = ""UserId="";
        string msg = ""Credited arrival deposit: ScoutId=503161, UserId=kfk68vu17xcl9vw, Amount=61.079062421635065"";
        
        bool result = msg.Contains(rule, StringComparison.OrdinalIgnoreCase);
        Console.WriteLine($""Match? {result}"");
    }
}
