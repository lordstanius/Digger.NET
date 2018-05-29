using System;
using System.IO;
using System.Text;

public static class Log
{
    public static void Write(string line)
    {
        using (var sw = new StreamWriter("digger.log", true, Encoding.UTF8))
        {
            sw.WriteLine(line);
        }
    }

    public static void Write(Exception ex)
    {
        Write(ex.ToString());
    }
}
