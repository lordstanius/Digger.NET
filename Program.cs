using System;

namespace Digger.Net
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            DiggerC.inir();
            DiggerC.parsecmd(args);
            DiggerC.maininit();
            DiggerC.mainprog();
        }
    }
}
