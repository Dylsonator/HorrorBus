using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Toggles a debug UI GameObject when ALT + NUMPAD 8
/// is held for a set duration.
/// </summary>
public sealed class DebugUI : MonoBehaviour
{
    [Header("Debug UI")]
    [SerializeField] private GameObject debugUIRoot;

    [Header("Input")]
    [SerializeField] private float holdTimeRequired = 2f;

    private float holdTimer;
    private bool isHolding;
    private bool isDebugVisible;

    private void Update()
    {
        if (Keyboard.current == null)
            return;

        bool altHeld = Keyboard.current.leftAltKey.isPressed;
        bool numpad8Held = Keyboard.current.numpad8Key.isPressed;

        if (altHeld && numpad8Held)
        {
            if (!isHolding)
            {
                isHolding = true;
                holdTimer = 0f;
            }

            holdTimer += Time.deltaTime;

            if (holdTimer >= holdTimeRequired)
            {
                ToggleDebugUI();
                ResetHold();
            }
        }
        else
        {
            ResetHold();
        }
    }

    private void ToggleDebugUI()
    {
        isDebugVisible = !isDebugVisible;

        if (debugUIRoot != null)
            debugUIRoot.SetActive(isDebugVisible);

        Debug.Log($"Debug UI {(isDebugVisible ? "ENABLED" : "DISABLED")}");
    }

    private void ResetHold()
    {
        isHolding = false;
        holdTimer = 0f;
    }
}
