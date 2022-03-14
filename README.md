# Klonk
Displays Final Fantasy XIV data on Phalanx-based VFD and Nixie devices.

![Klonk](https://i.imgur.com/zcmj0vy.png)

## Displayed Data
On start of the game, the plugin will automatically attempt to establish a connection with the serial device, but can be manually toggled with /klonk.

### In Combat
Displays your own HP in normal circumstances, until the selected target (or focus target if set) is casting a skill - then it will display the remaining time until that cast timer runs out.

### Outside of Combat
Displays your own HP, or the name of the selected target if the serial device supports displaying text. This can be anything from a player character to an NPC or a destination point.

# Config options
- Define COM port
- Amount digits 0-10
- Whether serial device supports text

# Building & Testing
- Get XIVLauncher, boot the game and verify it works
- Compile with Visual Studio
- Boot up FFXIV through XIVLauncher, open dalamud settings, and add the output binary folder to dev plugin locations
- If the game runs, on recompile it will hot-reload
- Debug by attaching to the FFXIV process
