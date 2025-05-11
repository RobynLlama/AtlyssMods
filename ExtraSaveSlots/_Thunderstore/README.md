# ExtraSaveSlots

Adds support for having more than 7 save slots (up to 128) by modifying the character select UI.

A new empty slot is created at the end of the scroll list if there isn't one already.

The save files follow the same format as the vanilla game's save files, being stored under `ATLYSS\ATLYSS_Data\profileCollections\atl_characterProfile_{SLOT_INDEX}`.

The mod will also load save slots 6 through 127 if dropped directly in the `profileCollections` folder.

![](https://github.com/Marioalexsan/AtlyssMods/blob/main/_Assets/ExtraSaveSlotsUI.png?raw=true)

# Configuration

If for some reason you need more than 128 save slots, you can increase it up to 65536 with the `MaxSupportedSaves` configuration option in `BepInEx/config/Marioalexsan.ExtraSaveSlots.cfg`.

*Please don't change MaxSupportedSaves unless you actually need to have that many saves at once.*

# Mod Compatibility

This mod version targets ATLYSS v1.6.2b. It also has experimental support for the public test branch (version 2.0.5d).

Compatibility with other game versions is not guaranteed, especially for updates with major changes.