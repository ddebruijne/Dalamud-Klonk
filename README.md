# Klonk
Displays Final Fantasy XIV data on Phalanx-based VFD and Nixie devices.

![Klonk](https://i.imgur.com/zcmj0vy.png)

### Building & Testing
- Get XIVLauncher, boot the game and verify it works
- Compile with Visual Studio
- Copy RJCP.SerialPortStream.dll to output binary folder or it won't work.
- Boot up FFXIV through XIVLauncher, open dalamud settings, and add the output binary folder to dev plugin locations
- If the game runs, on recompile it will hot-reload
- Debug by attaching to the FFXIV process
