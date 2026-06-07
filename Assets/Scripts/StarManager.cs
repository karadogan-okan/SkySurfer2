using UnityEngine;
using TMPro;

public class StarManager : MonoBehaviour
{
    public static StarManager Instance { get; private set; }

    [Header("UI")]
    [Tooltip("TextMeshPro text on the top-left showing the star count.")]
    public TextMeshProUGUI starCountText;

    private int sessionStars = 0;
    public int SessionStars => sessionStars;

    void Awake()
    {
        Instance = this;
        if (starCountText != null)
            starCountText.gameObject.SetActive(false);
    }

    public void ShowUI()
    {
        if (starCountText != null)
            starCountText.gameObject.SetActive(true);
        UpdateUI();
    }

    public void AddStar(int amount = 1)
    {
        sessionStars += amount;
        UpdateUI();
    }

    void UpdateUI()
    {
        if (starCountText != null)
            starCountText.text = "Stars: " + sessionStars;
    }
}
