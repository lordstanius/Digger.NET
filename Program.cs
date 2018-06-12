namespace Digger.Net
{
    class Program
    {
        public static void Main(string[] args)
        {
            var game = new Game();

            game.LoadSettings();
            game.ParseCmdLine(args);
            game.Initialize();
            game.Start();
            game.SaveSettings();
        }
    }
}
