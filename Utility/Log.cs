using System;
using System.IO;
using System.Linq;
using System.Text;

public static class Log
{
    public static void Write(string line)
    {
        using (var sw = new StreamWriter("digger.log", true, Encoding.UTF8))
        {
            sw.WriteLine(line);
        }

        Console.WriteLine(line);
    }

    public static void Write(Exception ex)
    {
        Write($"{ex.Message} at {ex.StackTrace.Split(new[] { "at " }, StringSplitOptions.None).Last()}");
    }
}
