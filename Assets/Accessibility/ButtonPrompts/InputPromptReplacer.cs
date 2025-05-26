using UnityEngine;
using UnityEngine.InputSystem;
using System.Text.RegularExpressions;
using Needle.Console;


// Apply component to a ButtonPromptZone (Game Object with a trigger collider)

public class InputPromptReplacer : MonoBehaviour
{
    public TMProSpriteAssetTextSetter TMProTextToReplace; // Requires a reference to a gameObject with a TMPro text component and SpriteAssetSelector component attached to it.
    [Tooltip("Set this so that the tooltip only shows up when the player is in the defined input mode. Leave as \"None\" to show always. WARNING: Not recommended for most use cases.")]
    public InputModeManager.InputMode explicitInputMode = InputModeManager.InputMode.None; // Leave as "None" to show always. WARNING: Not recommended.

    [TextArea(3, 5)]
    [SerializeField] private string originalInputText = "Press BUTTONPROMPT.Jump to jump!"; // example, to be replaced for whatever your uses are
    private string inputTextConverted = "Converted inputText will result here. Manually modifying in this value in the inspector or in code has no effect.";

    private static readonly Regex buttonPromptRegex = new Regex(@"BUTTONPROMPT\.(\w+)", RegexOptions.Compiled);
    // effectivePathRegex parses UnityEngine InputBinding's .effectivePath property that returns control path strings that look like, for example, \"<Gamepad>/buttonSouth\".")
    private static readonly Regex effectivePathRegex = new Regex(@"^<(?<device>[^>]+)>\/(?<input>.+)$");
    private InputModeManager inputManager;
    private InputSystem_Actions inputActions;

    private string[] validKeysInSpriteAsset1 = {"w", "a", "s", "d", "space", "e", "f", "t", "leftShift", "leftCtrl", "escape", "q"};
    private string[] validKeysInSpriteAsset2 = {"leftButton", "rightButton", "middleButton", "g", "r", "c", "1", "4"};

    void Awake()
    {
        inputManager = InputModeManager.Instance;
        inputActions = inputManager.inputActions;
    }

    // Takes the text in this component's text field in the inspector and converts it. Can handle a mix of strings and button prompts in the text.
    // Button prompts follow a naming convention of "BUTTONPROMPT" followed by a .bindingActionName. Example can be seen for the initial value of the variable originalInputText.
    private string DynamicConvert()
    {
        // The variable below, controlDeviceType, is an enum defined in InputModeManager. It contains all supported controller types.
        inputTextConverted = originalInputText;
        inputTextConverted = buttonPromptRegex.Replace(originalInputText, match =>
        {
            string actionName = match.Groups[1].Value; // ACTION name e.g., Jump, Sprint, etc. Automatically assumes actionMap context.

            return BriefConvert(actionName);
        });
        return inputTextConverted;
    }

    // Takes the name of an action (case sensitive, capilize first letter) and converts it into the correct name of an input SpriteAsset element.
    private string BriefConvert(string actionName)
    {
        InputModeManager.ControlDeviceType controlDeviceType = inputManager.GetCurrentDeviceType();
        return GetSpriteTag(actionName, controlDeviceType) ?? "UNBOUND";
    }

    private string GetSpriteTag(string actionName, InputModeManager.ControlDeviceType deviceType)
    {
        InputBinding dynamicBinding = inputManager.GetBinding(actionName, deviceType); // Finds the binding using only a name and current device type

        (string device, string input) effectivePath = parseEffectivePath(dynamicBinding.effectivePath); // Grabs the effective path using Unity's API, then parses it into two separate strings, excluding all the weird symbols by using Regex.

        D.Log($"GetSpriteTag() - Device: {effectivePath.device} and Input: {effectivePath.input}", gameObject, "Able");

        string spriteTagName = $"<sprite name=\"{effectivePath.device}_{effectivePath.input}\">"; // Sprite tag follow a naming convention outlined in this expression

        if (effectivePath.device == "Keyboard")
        {
            int spriteAsset = DetermineSpriteAssetKeyboard(effectivePath.input);

            if (spriteAsset == 1)
            {
                TMProTextToReplace.SetSpriteAsset(InputModeManager.ControlDeviceType.Keyboard, 1); // Set sprite asset to Keyboard SpriteAsset 1
                spriteTagName = $"<sprite name=\"keyboard_{effectivePath.input}\">";
            }
            else if (spriteAsset == 2)
            {
                TMProTextToReplace.SetSpriteAsset(InputModeManager.ControlDeviceType.Keyboard, 2); // Set sprite asset to Keyboard SpriteAsset 2
                spriteTagName = $"<sprite name=\"keyboard_{effectivePath.input}\">"; // SpriteAsset 2
            }
            else
            {
                spriteTagName = MakeFallbackInputString(effectivePath.input);
                D.Log($"Non-default keybind sprite detected, displaying: {spriteTagName}, {effectivePath.input}", this, "Able");
            }
        }
        else if (effectivePath.device == "Gamepad")
        {
            int spriteAsset = DetermineSpriteAssetGamepad(effectivePath.input);
            if (spriteAsset == -1)
            {
                spriteTagName = MakeFallbackInputString(effectivePath.input);
                D.Log($"Non-default gamepad bind sprite detected, displaying: {spriteTagName}", this, "Able");
            }
        }
        else if (effectivePath.device == "Mouse")
        {
            int spriteAsset = DetermineSpriteAssetKeyboard(effectivePath.input);

            if (spriteAsset == 1)
            {
                TMProTextToReplace.SetSpriteAsset(InputModeManager.ControlDeviceType.Keyboard, 1); // Set sprite asset to Keyboard SpriteAsset 1
                spriteTagName = $"<sprite name=\"mouse_{effectivePath.input}\">";
            }
            else if (spriteAsset == 2)
            {
                TMProTextToReplace.SetSpriteAsset(InputModeManager.ControlDeviceType.Keyboard, 2); // Set sprite asset to Keyboard SpriteAsset 2
                spriteTagName = $"<sprite name=\"mouse_{effectivePath.input}\">"; // SpriteAsset 2
            }
            else
            {
                spriteTagName = MakeFallbackInputString(effectivePath.input);
                D.Log($"Non-default keybind sprite detected, displaying: {spriteTagName}, {effectivePath.input}", this, "Able");
            }
        }

        return spriteTagName;
    }

    private int DetermineSpriteAssetKeyboard(string inputKey)
    {
        foreach (string key in validKeysInSpriteAsset1)
        {
            if (inputKey == key)
            {
                return 1; // SpriteAsset 1
            }
        }
        foreach (string key in validKeysInSpriteAsset2)
        {
            if (inputKey == key)
            {
                return 2; // SpriteAsset 2
            }
        }

        return -1; // Not found in either sprite asset
    }

    private int DetermineSpriteAssetGamepad(string inputKey)
    {
        // Gamepad keys are not supported in the current sprite assets, so we return -1 to indicate that.
        // This is a placeholder for future expansion if gamepad sprite assets are added.
        return -1; // Not found in any sprite asset
    }

    private string MakeFallbackInputString(string inputKey)
    {
        string str = inputKey;

        str = "[" + str + "]";

        return str;
    }

    private (string device, string input) parseEffectivePath(string effectivePath)
    {

        Match match = effectivePathRegex.Match(effectivePath);

        if (match.Success)
        {
            string device = match.Groups["device"].Value;
            string input = match.Groups["input"].Value;
            return (device, input);
        }
        else
        {
            D.LogError("Invalid input path in parseEffectivePath()", gameObject, "Any");
            return ("", "");
        }

    }

    // SetConvertedText_SlowPerformance() calls DynamicConvert() to get a potentially new text content depending on if the player's input device changed.
    // Should only be called outside of loops, since DynamicConvert() employs lots of string operations and regex matching.
    // USAGE: Sets a tmpro textbox's content with proper SpriteAsset key/button icons associated with the current control device (which can change during runtime)
    // Performance is slow because it performs an update to check if the player switched devices in the middle of the game (e.g. keyboard -> controller)
    // DO_NOT_OPTIMIZE: Do not attempt to optimize, SlowPerformance tag only exists to discourage use of this method in loops, not as a flag for needing improvement.
    // Optimizing for loops may result in losing the dynamic feature of correctly showing the player's current control device. Correct usage of this method is in class ButtonPromptTriggerZone.cs .
    public string SetConvertedText_SlowPerformance()
    {
        D.Log("SetConvertedText_SlowPerformance() called, converting text...", gameObject, "Able");
        string convertedText = DynamicConvert();
        TMProTextToReplace.SetText(convertedText); // Updates the asset according to the current device type.
        return convertedText;
    }

    // Getter Setter

    public void SetOriginalInputText(string text){
        originalInputText = text;
    }
}