# Atlyss Discord Rich Presence

Enables Discord Rich Presence support for ATLYSS.

![](https://i.imgur.com/eGlh4yG.png)

Shows various stats, such as:
- Your current status (in menu, exploring a zone, etc.)
- Your character
- Your character's current state
- Whenever you're in singleplayer or multiplayer
- Elapsed playtime

Also allows you to:
- Customize the texts displayed by the integration, and select between available presets
- Send invites for the ATLYSS server you're in to people who also have the mod (while the game is open)
- Have people who also have the mod be able to join your ATLYSS server (while the game is open)

# Configuration

You can configure the displayed strings via the configuration file in `BepInEx/config/Marioalexsan.AtlyssDiscordRichPresence.cfg`.

Within these strings, you can use various variables to display stats, such as `{PLAYERNAME} exploring {WORLDAREA}` => `Chip exploring Sanctum`. You can use this to display other stats, such as Health percentage instead of exact health numbers.

The available variables are as follows:
- `HP` - Player health
- `MAXHP` - Player maximum health
- `HPPCT` - Player health pecentage (0-100)
- `MP` - Player mana
- `MAXMP` - Player maximum mana
- `MPPCT` - Player mana pecentage (0-100)
- `SP` - Player stamina
- `MAXSP` - Player mximum stamina
- `SPPCT` - Player stamina pecentage (0-100)
- `LVL` - Player level
- `EXP` - Player experience
- `EXPNEXT` - Player max experience (i.e. the experience required to level up)
- `EXPPCT` - Player experience percentage (0-100)
- `PLAYERNAME` - Player display name
- `PLAYERRACE` - Player race ("Poon", etc.)
- `PLAYERCLASS` - Player class ("Novice", "Fighter", etc.)
- `PLAYERRACEANDCLASS` - Displays both race and class together ("Poon Novice", etc.)
- `WORLDAREA` - Current world area ("Sanctum", etc.)
- `SERVERNAME` - The server you're playing on
- `PLAYERS` - The number of players in the server you're on
- `MAXPLAYERS` - The maximum number of players in the server you're on

You can also further configure how player races (PLAYERRACE) and player classes (PLAYERCLASS) are displayed by configuring variables such as `RacePoon`

# Mod Compatibility

AtlyssDiscordRichPresence targets the following game versions and mods:

- ATLYSS v1.6.2b
- Nessie's EasySettings v1.1.3 (optional dependency used for configuration)

Compatibility with other game versions and mods is not guaranteed, especially for updates with major changes.

# Gallery

![](https://i.imgur.com/zHNFQb4.png)

![](https://i.imgur.com/G2VhZDa.png)

