using System;
using System.IO;
using System.Text;

public static class DebugLog
{
    public static void Write(string line)
    {
#if DEBUG
        using (var sw = new StreamWriter("digger.log", true, Encoding.UTF8))
        {
            sw.WriteLine(line);
        }
#endif
    }

    public static void Write(Exception ex)
    {
        Write(ex.ToString());
    }
}
