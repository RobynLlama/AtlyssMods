# AutoSaver

Automatically backups your character save and Spike's inventory on game load, quit and every 4 minutes.

Up to 15 saves are stored. You can configure both the number of saves stored and the automatic save interval in `BepInEx/config/Marioalexsan.AutoSaver.cfg`.

To fix save corruptions:
- go to `ATLYSS_Data/profileCollections/Marioalexsan_AutoSaver/Characters/{CHARACTER_NAME}`, where CHARACTER_NAME is your character's name in ATLYSS
- grab the latest backup, or any previous backup based on the time it was created on
- copy the file to `ATLYSS_Data/profileCollections` and replace `atl_characterProfile_{X}` with that file, where X is the slot you want it to go in, from 0 to 5 (`atl_characterProfile_0` is the first slot)
- example: `ATLYSS_Data/profileCollections/Marioalexsan_AutoSaver/Characters/Chip/_latest` -> `ATLYSS_Data/profileCollections/atl_characterProfile_0` (overwriting previous corrupt file if needed)

To fix item bank corruption:
- go to `ATLYSS_Data/profileCollections/Marioalexsan_AutoSaver/ItemBank/`
- grab the latest backup, or any previous backup based on the time it was created on
- copy the files from within to `ATLYSS_Data/profileCollections`
- replace `atl_itemBank` with `itemBank_0`, `atl_itemBank_01` with `itemBank_1`, `atl_itemBank_02` with `itemBank_2`, etc.
- example: 
  - `ATLYSS_Data/profileCollections/Marioalexsan_AutoSaver/ItemBank/_latest/itemBank_0` -> `ATLYSS_Data/profileCollections/atl_itemBank` (overwriting previous corrupt file if needed)
  - `ATLYSS_Data/profileCollections/Marioalexsan_AutoSaver/ItemBank/_latest/itemBank_1` -> `ATLYSS_Data/profileCollections/atl_itemBank_01` (overwriting previous corrupt file if needed)
  - `ATLYSS_Data/profileCollections/Marioalexsan_AutoSaver/ItemBank/_latest/itemBank_2` -> `ATLYSS_Data/profileCollections/atl_itemBank_02` (overwriting previous corrupt file if needed)

# Notes

This mod assumes your character's names are unique.

If they're not, you might run into trouble as playing on one of the characters will overwrite the autosaves of the other.