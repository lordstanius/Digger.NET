namespace Digger.Source
{
    class Program
    {
        public static void Main(string[] args)
        {
            var game = new Game();

            game.LoadSettings();
            game.ParseCmdLine(args);
            game.Init();
            game.Start();
            game.SaveSettings();
        }
    }
}
