/* Digger Remastered
   Copyright (c) Andrew Jenner 1998-2004 */

namespace Digger.Net
{
    public static class Keyboard
    {
        public static readonly string[] KeyNames = {
            "Right","Up","Left","Down","Fire",
            "Right","Up","Left","Down","Fire",
            "Cheat","Accel","Brake","Music","Sound","Exit","Pause",
            "Mode Change","Save DRF"};

        public static void Redefine(Input input, DrawApi drawApi, bool allf)
        {
            int i, j, k, l, z, y = 0, x, savey;
            bool f;

            DiggerC.maininit();

            drawApi.TextOut("PRESS NEW KEY FOR", 0, y, 3);
            y += DiggerC.CHR_H;

            if (DiggerC.g_Diggers == 2)
            {
                drawApi.TextOut("PLAYER 1:", 0, y, 3);
                y += DiggerC.CHR_H;
            }

            /* Step one: redefine keys that are always redefined. */

            savey = y;
            for (i = 0; i < 5; i++)
            {
                drawApi.TextOut(KeyNames[i], 0, y, 2); /* Red first */
                if (input.prockey(i) == -1) return;
                drawApi.TextOut(KeyNames[i], 0, y, 1); /* Green once got */
                y += DiggerC.CHR_H;
                for (j = 0; j < i; j++)
                { /* Note: only check keys just pressed (I hate it when
                           this is done wrong, and it often is.) */
                    if (input.keyboard.keycodes[i][0] == input.keyboard.keycodes[j][0] && input.keyboard.keycodes[i][0] != 0)
                    {
                        i--;
                        y -= DiggerC.CHR_H;
                        break;
                    }
                    for (k = 2; k < 5; k++)
                        for (l = 2; l < 5; l++)
                            if (input.keyboard.keycodes[i][k] == input.keyboard.keycodes[j][l] && input.keyboard.keycodes[i][k] != -2)
                            {
                                j = i;
                                k = 5;
                                i--;
                                y -= DiggerC.CHR_H;
                                break; /* Try again if this key already used */
                            }
                }
            }

            if (DiggerC.g_Diggers == 2)
            {
                drawApi.TextOut("PLAYER 2:", 0, y, 3);
                y += DiggerC.CHR_H;
                for (i = 5; i < 10; i++)
                {
                    drawApi.TextOut(KeyNames[i], 0, y, 2); /* Red first */
                    if (input.prockey(i) == -1) return;
                    drawApi.TextOut(KeyNames[i], 0, y, 1); /* Green once got */
                    y += DiggerC.CHR_H;
                    for (j = 0; j < i; j++)
                    { /* Note: only check keys just pressed (I hate it when
                             this is done wrong, and it often is.) */
                        if (input.keyboard.keycodes[i][0] == input.keyboard.keycodes[j][0] && input.keyboard.keycodes[i][0] != 0)
                        {
                            i--;
                            y -= DiggerC.CHR_H;
                            break;
                        }
                        for (k = 2; k < 5; k++)
                            for (l = 2; l < 5; l++)
                                if (input.keyboard.keycodes[i][k] == input.keyboard.keycodes[j][l] && input.keyboard.keycodes[i][k] != -2)
                                {
                                    j = i;
                                    k = 5;
                                    i--;
                                    y -= DiggerC.CHR_H;
                                    break; /* Try again if this key already used */
                                }
                    }
                }
            }

            /* Step two: redefine other keys which step one has caused to conflict */

            if (allf)
            {
                drawApi.TextOut("OTHER:", 0, y, 3);
                y += DiggerC.CHR_H;
            }

            z = 0;
            x = 0;
            y -= DiggerC.CHR_H;
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
                        y += DiggerC.CHR_H;
                    if (y >= DiggerC.MAX_H - DiggerC.CHR_H)
                    {
                        y = savey;
                        x = (DiggerC.MAX_TEXT_LEN / 2) * DiggerC.CHR_W;
                    }
                    drawApi.TextOut(KeyNames[i], x, y, 2); /* Red first */
                    if (input.prockey(i) == -1) return;
                    drawApi.TextOut(KeyNames[i], x, y, 1); /* Green once got */
                    z = i;
                    i--;
                }
            }

            /* Step three: save the INI file */

            for (i = 0; i < Input.NKEYS; i++)
            {
                if (input.krdf[i])
                {
                    string kbuf = string.Format("{0}{1}", KeyNames[i], (i >= 5 && i < 10) ? '2' : 0);
                    string vbuf = string.Format("{0}/{1}/{2}/{3}/{4}", input.keyboard.keycodes[i][0], input.keyboard.keycodes[i][1],
                            input.keyboard.keycodes[i][2], input.keyboard.keycodes[i][3], input.keyboard.keycodes[i][4]);
                    Ini.WriteINIString(DiggerC.INI_KEY_SETTINGS, kbuf, vbuf, DiggerC.ININAME);
                }
            }
        }
    }
}