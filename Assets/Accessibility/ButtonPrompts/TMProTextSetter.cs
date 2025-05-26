// Attach this script to same gameObject with TMPro text component on it: e.g. your tooltip component.

using UnityEngine;
using TMPro;
using Needle.Console;

[RequireComponent(typeof(TMP_Text))]
public class TMProSpriteAssetTextSetter : MonoBehaviour
{
    // References to TMP_SpriteAssets for gamepad and keyboard
    [SerializeField] private SpriteAssetReferenceHolder spriteAssetReferences;
    private TMP_SpriteAsset gamepadSpriteAsset;
    private TMP_SpriteAsset keyboardSpriteAsset1;
    private TMP_SpriteAsset keyboardSpriteAsset2;
    private TMP_Text textbox;

    private void Awake()
    {
        textbox = GetComponent<TMP_Text>();
        gamepadSpriteAsset = spriteAssetReferences.gamepadSpriteAsset;
        keyboardSpriteAsset1 = spriteAssetReferences.keyboardSpriteAsset1;
        keyboardSpriteAsset2 = spriteAssetReferences.keyboardSpriteAsset2;
    }

    public void SetText(string formattedText){
        textbox.text = formattedText;
    }

    public void SetSpriteAsset(InputModeManager.ControlDeviceType deviceType, int spriteAssetExtension)
    {
        TMP_SpriteAsset spriteAsset = null;

        switch (deviceType)
        {
            case InputModeManager.ControlDeviceType.Gamepad:
                switch (spriteAssetExtension)
                {
                    case 1:
                        spriteAsset = gamepadSpriteAsset;
                        break;
                    case 2:
                        // Add logic for another gamepad sprite asset if needed
                        break;
                    default:
                        D.LogError("Invalid sprite asset extension for Gamepad!", gameObject, "Able");
                        return;
                }
                break;
            case InputModeManager.ControlDeviceType.Keyboard:
                switch (spriteAssetExtension)
                {
                    case 1:
                        spriteAsset = keyboardSpriteAsset1;
                        break;
                    case 2:
                        spriteAsset = keyboardSpriteAsset2;
                        break;
                    default:
                        D.LogError("Invalid sprite asset extension for Keyboard!", gameObject, "Able");
                        return;
                }
                break;
            default:
                D.LogError("DeviceType doesn't exist in SetSpriteAsset()!", gameObject, "Able");
                return;
        }

        textbox.spriteAsset = spriteAsset;
    }
    private void UpdateSpriteAsset()
    {
        // Dynamically select the sprite asset based on the device
        TMP_SpriteAsset selectedSpriteAsset = SelectSpriteAssetBasedOnDevice();

        // Dynamically set the sprite asset for the TMP_Text
        textbox.spriteAsset = selectedSpriteAsset;
    }

    // Dynamically select which sprite asset to use based on the device
    private TMP_SpriteAsset SelectSpriteAssetBasedOnDevice()
    {
        InputModeManager.ControlDeviceType deviceType = InputModeManager.Instance.GetCurrentDeviceType();

        switch (deviceType)
        {
            case InputModeManager.ControlDeviceType.Gamepad:
                return gamepadSpriteAsset;
            case InputModeManager.ControlDeviceType.Keyboard:
                return keyboardSpriteAsset1;
            default:
                D.LogError("DeviceType doesn't exist in SelectSpriteAssetBasedOnDevice()!", gameObject, "Able");
                return null;
        }
    }

}
