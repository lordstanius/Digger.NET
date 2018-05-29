/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */
// C# port 2018 Mladen Stanisic <lordstanius@gmail.com>

namespace Digger.Net
{
    public static class Keys
    {
        private const int CHR_W = Const.CHR_W;
        private const int CHR_H = Const.CHR_H;
        private const int MAX_W = Const.MAX_W;
        private const int MAX_H = Const.MAX_H;
        private const int MAX_TEXT_LEN = Const.MAX_TEXT_LEN;

        public static readonly string[] KeyNames = {
            "Right","Up","Left","Down","Fire",
            "Right","Up","Left","Down","Fire",
            "Cheat","Accel","Brake","Music","Sound","Exit","Pause",
            "Mode Change","Save DRF", "Set VGA", "Set CGA"};

        // TODO: Test this!
        public static void Redefine(Game game, bool allKeys)
        {
            int j, k, l, z, y = 0, x, savey;
            bool f;

            game.Initialize();

            game.video.TextOut("PRESS NEW KEY FOR", 0, y, 3);
            y += CHR_H;

            if (game.diggerCount == 2)
            {
                game.video.TextOut("PLAYER 1:", 0, y, 3);
                y += CHR_H;
            }

            /* Step one: redefine keys that are always redefined. */

            savey = y;
            for (int i = 0; i < 5; i++)
            {
                game.video.TextOut(KeyNames[i], 0, y, 2); /* Red first */
                if (game.input.ProcessKey(i) == -1)
                    return;

                game.video.TextOut(KeyNames[i], 0, y, 1); /* Green once got */
                y += CHR_H;
                for (j = 0; j < i; j++)
                { /* Note: only check keys just pressed (I hate it when
                           this is done wrong, and it often is.) */
                    if (game.input.KeyCodes[i][0] == game.input.KeyCodes[j][0] && game.input.KeyCodes[i][0] != 0)
                    {
                        i--;
                        y -= CHR_H;
                        break;
                    }
                    for (k = 2; k < 5; k++)
                    {
                        for (l = 2; l < 5; l++)
                        {
                            if (game.input.KeyCodes[i][k] == game.input.KeyCodes[j][l] && game.input.KeyCodes[i][k] != -2)
                            {
                                j = i;
                                k = 5;
                                i--;
                                y -= CHR_H;
                                break; /* Try again if this key already used */
                            }
                        }
                    }
                }
            }

            if (game.diggerCount == 2)
            {
                game.video.TextOut("PLAYER 2:", 0, y, 3);
                y += CHR_H;
                for (int i = 5; i < 10; i++)
                {
                    game.video.TextOut(KeyNames[i], 0, y, 2); /* Red first */
                    if (game.input.ProcessKey(i) == -1) return;
                    game.video.TextOut(KeyNames[i], 0, y, 1); /* Green once got */
                    y += CHR_H;
                    for (j = 0; j < i; j++)
                    { /* Note: only check keys just pressed (I hate it when
                             this is done wrong, and it often is.) */
                        if (game.input.KeyCodes[i][0] == game.input.KeyCodes[j][0] && game.input.KeyCodes[i][0] != 0)
                        {
                            i--;
                            y -= CHR_H;
                            break;
                        }
                        for (k = 2; k < 5; k++)
                        {
                            for (l = 2; l < 5; l++)
                            {
                                if (game.input.KeyCodes[i][k] == game.input.KeyCodes[j][l] && game.input.KeyCodes[i][k] != -2)
                                {
                                    j = i;
                                    k = 5;
                                    i--;
                                    y -= CHR_H;
                                    break; /* Try again if this key already used */
                                }
                            }
                        }
                    }
                }
            }

            /* Step two: redefine other keys which step one has caused to conflict */

            if (allKeys)
            {
                game.video.TextOut("OTHER:", 0, y, 3);
                y += CHR_H;
            }

            z = 0;
            x = 0;
            y -= CHR_H;
            for (int i = 10; i < game.input.KeyCount; i++)
            {
                f = false;
                for (j = 0; j < 10; j++)
                    for (k = 0; k < 5; k++)
                        for (l = 2; l < 5; l++)
                            if (game.input.KeyCodes[i][k] == game.input.KeyCodes[j][l] && game.input.KeyCodes[i][k] != -2)
                                f = true;
                for (j = 10; j < i; j++)
                    for (k = 0; k < 5; k++)
                        for (l = 0; l < 5; l++)
                            if (game.input.KeyCodes[i][k] == game.input.KeyCodes[j][l] && game.input.KeyCodes[i][k] != -2)
                                f = true;
                if (f || (allKeys && i != z))
                {
                    if (i != z)
                        y += CHR_H;
                    if (y >= MAX_H - CHR_H)
                    {
                        y = savey;
                        x = (MAX_TEXT_LEN / 2) * CHR_W;
                    }
                    game.video.TextOut(KeyNames[i], x, y, 2); /* Red first */
                    if (game.input.ProcessKey(i) == -1) return;
                    game.video.TextOut(KeyNames[i], x, y, 1); /* Green once got */
                    z = i;
                    i--;
                }
            }

            /* Step three: save the INI file */
            game.WriteIniKeySettings();
        }
    }
}