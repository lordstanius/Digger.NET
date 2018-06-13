/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

namespace Digger.Source
{
    public static class Keyboard
    {
        private const int CHR_W = Const.CHR_W;
        private const int CHR_H = Const.CHR_H;
        private const int MAX_W = Const.MAX_W;
        private const int MAX_H = Const.MAX_H;
        private const int MAX_TEXT_LEN = Const.MAX_TEXT_LEN;

        public static readonly string[] KeyNames = {
            "Right","Up","Left","Down","Fire",
            "Right","Up","Left","Down","Fire",
            "God mode","Speed up","Speed down","Toggle music","Toggle sound","Exit","Pause",
            "Change game mode","Save DRF", "Switch to VGA graphics", "Switch to CGA graphics"};

        public static void Redefine(Game game, bool allKeys)
        {
            game.Init();

            game.drawing.TextOutCentered("D I G G E R", 2, 3);
            game.drawing.TextOutCentered("REDEFINE KEYBOARD", 3 * CHR_H, 1);

            int playerrow = 5 * CHR_H;
            int keyrow = 8 * CHR_H;
            int errorrow1 = 11 * CHR_H;
            int errorrow2 = 13 * CHR_H;
            int color = 3;

            for (int i = 0; i < game.input.KeyCount; i++)
            {
                game.drawing.EraseLine(playerrow);
                game.drawing.EraseLine(keyrow);

                if (i < 5)
                    game.drawing.TextOutCentered("PLAYER 1", playerrow, 2);
                else if (i < 10)
                    game.drawing.TextOutCentered("PLAYER 2", playerrow, 2);

                if (i >= 10 && !allKeys)
                    break;

                if (i >= 10 && allKeys)
                    game.drawing.TextOutCentered("MISELLANEOUS", playerrow, 2);

                game.drawing.TextOutCentered(KeyNames[i], keyrow, color);

                if (game.input.ProcessKey(i) == -1)
                    return;

                game.drawing.EraseLine(errorrow1);
                game.drawing.EraseLine(errorrow2);
                color = 3;

                for (int j = 0; j < i; j++)
                { /* Note: only check keys just pressed (I hate it when
                           this is done wrong, and it often is.) */
                    if (game.input.KeyCodes[i][0] == game.input.KeyCodes[j][0] && game.input.KeyCodes[i][0] != 0)
                    {
                        i--;
                        color = 2;
                        game.drawing.TextOutCentered("THIS KEY IS ALREADY USED", errorrow1, 2);
                        game.drawing.TextOutCentered("CHOOSE ANOTHER KEY", errorrow2, 2);
                        break;
                    }

                    for (int k = 2; k < 5; k++)
                    {
                        for (int l = 2; l < 5; l++)
                        {
                            if (game.input.KeyCodes[i][k] == game.input.KeyCodes[j][l] && game.input.KeyCodes[i][k] != -2)
                            {
                                j = i;
                                k = 5;
                                i--;
                                color = 2;
                                game.drawing.TextOutCentered("THIS KEY IS ALREADY USED", errorrow1, 2);
                                game.drawing.TextOutCentered("CHOOSE ANOTHER KEY", errorrow2, 2);
                                break; /* Try again if this key already used */
                            }
                        }
                    }
                }
            }

            game.WriteIniKeySettings();
        }
    }
}