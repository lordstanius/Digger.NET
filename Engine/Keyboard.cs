/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

namespace Digger.Net
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
            "Cheat","Accel","Brake","Music","Sound","Exit","Pause",
            "Mode Change","Save DRF"};

        public static void Redefine(Game game, Input input, Video video, bool allf)
        {
            int i, j, k, l, z, y = 0, x, savey;
            bool f;

            game.Initialize();

            video.TextOut("PRESS NEW KEY FOR", 0, y, 3);
            y += CHR_H;

            if (game.DiggerCount == 2)
            {
                video.TextOut("PLAYER 1:", 0, y, 3);
                y += CHR_H;
            }

            /* Step one: redefine keys that are always redefined. */

            savey = y;
            for (i = 0; i < 5; i++)
            {
                video.TextOut(KeyNames[i], 0, y, 2); /* Red first */
                if (input.ProcessKey(i) == -1) return;
                video.TextOut(KeyNames[i], 0, y, 1); /* Green once got */
                y += CHR_H;
                for (j = 0; j < i; j++)
                { /* Note: only check keys just pressed (I hate it when
                           this is done wrong, and it often is.) */
                    if (input.keyboard.keycodes[i][0] == input.keyboard.keycodes[j][0] && input.keyboard.keycodes[i][0] != 0)
                    {
                        i--;
                        y -= CHR_H;
                        break;
                    }
                    for (k = 2; k < 5; k++)
                        for (l = 2; l < 5; l++)
                            if (input.keyboard.keycodes[i][k] == input.keyboard.keycodes[j][l] && input.keyboard.keycodes[i][k] != -2)
                            {
                                j = i;
                                k = 5;
                                i--;
                                y -= CHR_H;
                                break; /* Try again if this key already used */
                            }
                }
            }

            if (game.DiggerCount == 2)
            {
                video.TextOut("PLAYER 2:", 0, y, 3);
                y += CHR_H;
                for (i = 5; i < 10; i++)
                {
                    video.TextOut(KeyNames[i], 0, y, 2); /* Red first */
                    if (input.ProcessKey(i) == -1) return;
                    video.TextOut(KeyNames[i], 0, y, 1); /* Green once got */
                    y += CHR_H;
                    for (j = 0; j < i; j++)
                    { /* Note: only check keys just pressed (I hate it when
                             this is done wrong, and it often is.) */
                        if (input.keyboard.keycodes[i][0] == input.keyboard.keycodes[j][0] && input.keyboard.keycodes[i][0] != 0)
                        {
                            i--;
                            y -= CHR_H;
                            break;
                        }
                        for (k = 2; k < 5; k++)
                            for (l = 2; l < 5; l++)
                                if (input.keyboard.keycodes[i][k] == input.keyboard.keycodes[j][l] && input.keyboard.keycodes[i][k] != -2)
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

            /* Step two: redefine other keys which step one has caused to conflict */

            if (allf)
            {
                video.TextOut("OTHER:", 0, y, 3);
                y += CHR_H;
            }

            z = 0;
            x = 0;
            y -= CHR_H;
            for (i = 10; i < Input.NKEYS; i++)
            {
                f = false;
                for (j = 0; j < 10; j++)
                    for (k = 0; k < 5; k++)
                        for (l = 2; l < 5; l++)
                            if (input.keyboard.keycodes[i][k] == input.keyboard.keycodes[j][l] && input.keyboard.keycodes[i][k] != -2)
                                f = true;
                for (j = 10; j < i; j++)
                    for (k = 0; k < 5; k++)
                        for (l = 0; l < 5; l++)
                            if (input.keyboard.keycodes[i][k] == input.keyboard.keycodes[j][l] && input.keyboard.keycodes[i][k] != -2)
                                f = true;
                if (f || (allf && i != z))
                {
                    if (i != z)
                        y += CHR_H;
                    if (y >= MAX_H - CHR_H)
                    {
                        y = savey;
                        x = (MAX_TEXT_LEN / 2) * CHR_W;
                    }
                    video.TextOut(KeyNames[i], x, y, 2); /* Red first */
                    if (input.ProcessKey(i) == -1) return;
                    video.TextOut(KeyNames[i], x, y, 1); /* Green once got */
                    z = i;
                    i--;
                }
            }
            
            /* Step three: save the INI file */
            game.WriteKeyboardSettings();
        }
    }
}