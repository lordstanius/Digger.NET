# Digger game in C# .NET
This is C# port of Windmill Software's classic game from 1983, remastered by Andrew Jenner. 
Port is based on Maxim Sobolyev's SDL version of Digger (<https://github.com/sobomax/digger>) and includes some bugfixes that were revealed during the transfer of C code to C# as well as CGA display mode. It builds in Visual Studio 2017 with .NET 4.6 and SDL library v2.0.5 on Windows.
CGA display is default, press V on main screen in order to switch to VGA.

<img src="https://github.com/lordstanius/WinDig/blob/master/digger.png" width="320"></a>

Input keys:
```
Player 1    Arrow keys (left, right, up, down) to move, F1 to fire
Player 2    WASD to move, Tab to fire
T           Cheat mode (unlimited lives)
N           Change game mode (one/two players (and simultaneous mode too), gauntlet)
C           Switch to CGA display
V           Switch to VGA display
Numpad+     Increase game speed
Numpad-     Decrease game speed
F7          Toggle sound
F8          Save recorded game (every game is being recorded from the start)
F9          Toggle music
Alt+Enter   Toggle full screen/window
F10         Exit
```
Command line syntax:
```
  DIGGER [[/S:]speed] [[/L:]level file] [/C] [/Q] [/M]
         [/P:playback file]
         [/E:playback file] [/R:record file] [/O] [/K[A]]
         [/G[:time]] [/2]
         [/U] [/I:level]
         [/F]
  /C = Use CGA graphics
  /Q = Quiet mode (no sound at all)
  /M = No music
  /R = Record graphics to file
  /P = Playback and restart program
  /E = Playback and exit program
  /O = Playback and loop to beginning of command line
  /K = Redefine keyboard (/KA to redefine all keys)
  /G = Gauntlet mode
  /2 = Two player simultaneous mode
  /F = Full-Screen
  /U = Allow unlimited lives
  /I = Start on a level other than 1
```
