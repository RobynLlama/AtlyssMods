using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace Marioalexsan.AllowAnyNames.HarmonyPatches;

[HarmonyPatch(typeof(CharacterSelectManager), nameof(CharacterSelectManager.Init_DeleteCharacterPrompt))]
static class CharacterSelectManager_Init_DeleteCharacterPrompt
{
    static void Prefix()
    {
        var labelTextComponent = GameObject.Find("_text_characterDeletePrompt").GetComponent<Text>();

        var confirmText = CharacterSelectManager_Handle_ButtonControl.ReplacementDeleteString;
        var characterName = ProfileDataManager._current._characterFile._nickName;

        labelTextComponent.text = $"Type in \"{confirmText}\" to confirm deleting \n\"{characterName}\"";

        var placeholderTextComponent = GameObject.Find("_input_characterDeleteConfirm").transform.Find("Placeholder").GetComponent<Text>();

        placeholderTextComponent.text = confirmText;
    }
}
