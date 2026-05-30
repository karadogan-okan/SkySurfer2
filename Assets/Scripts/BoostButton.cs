using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Handles the boost button UI — cooldown fill, icon tint, and calling SpeedManager.ActivateBoost().
/// Attach to the boost button GameObject.
/// </summary>
public class BoostButton : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The radial fill image that drains to show cooldown progress.")]
    public Image cooldownFillImage;
    [Tooltip("The flame icon image — tinted gray on cooldown, white when ready.")]
    public Image iconImage;
    [Tooltip("The Button component on this GameObject.")]
    public Button button;

    [Header("Colors")]
    public Color readyColor = Color.white;
    public Color cooldownColor = new Color(0.4f, 0.4f, 0.4f, 1f);

    void Update()
    {
        if (SpeedManager.Instance == null) return;

        bool canBoost = SpeedManager.Instance.CanBoost;
        float progress = SpeedManager.Instance.BoostCooldownProgress;

        // Overlay is FULL when on cooldown, EMPTY when ready
        if (cooldownFillImage != null)
            cooldownFillImage.fillAmount = 1f - progress;

        // Tint icon
        if (iconImage != null)
            iconImage.color = canBoost ? readyColor : cooldownColor;

        // Enable/disable button interaction
        if (button != null)
            button.interactable = canBoost;
    }

    // Wire this to the Button's OnClick event in the Inspector
    public void OnBoostPressed()
    {
        if (SpeedManager.Instance != null)
            SpeedManager.Instance.ActivateBoost();
    }
}
