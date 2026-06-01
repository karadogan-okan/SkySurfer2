using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SpeedManager : MonoBehaviour
{
    public static SpeedManager Instance { get; private set; }

    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float fuelDrainRate = 10f;

    [Header("Speed Settings")]
    [Tooltip("How fast scroll speed increases per second while fuel is available.")]
    public float acceleration = 0.5f;
    [Tooltip("Maximum scroll speed the world can reach. Acts as a hard cap — scroll speed never exceeds this.")]
    public float maxScrollSpeed = 20f;
    [Tooltip("How fast scroll speed drops per second when fuel is empty.")]
    public float decelerationRate = 3f;

    [Header("UI")]
    public Image fillImage;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI unitText;
    public TextMeshProUGUI currentScoreText;
    public GameObject boostButtonObject;

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI totalScoreText;
    public TextMeshProUGUI highestScoreText;

    [Header("References")]
    public PlayerController playerController;

    [Header("Boost Settings")]
    [Tooltip("Speed added when boost is activated. Increase this value for upgrades.")]
    public float boostAmount = 5f;
    [Tooltip("How fast the boost ramps up to its target speed (units per second).")]
    public float boostAccelerationRate = 8f;
    [Tooltip("How fast speed decays back to maxScrollSpeed after an overdrive boost.")]
    public float overdriveDragRate = 2f;
    [Tooltip("Cooldown in seconds before the player can boost again.")]
    public float boostCooldown = 45f;

    [Header("Freeze Frost Effect")]
    [Tooltip("Optional frost image (e.g. Assets/Frost/Ice.tga). Leave empty to use the built-in procedural frost.")]
    public Texture2D frostTexture;
    [Tooltip("Tint applied to the frost. Use white to keep the procedural colors; an icy blue works nicely with Ice.tga.")]
    public Color frostTint = Color.white;
    [Range(0f, 1f)]
    [Tooltip("How opaque the frost gets at full strength.")]
    public float frostMaxAlpha = 0.9f;
    [Tooltip("Frame the fuel tank, or cover the whole screen.")]
    public FuelTankFrost.Placement frostPlacement = FuelTankFrost.Placement.FuelTank;

    [Header("Debug")]
    [Tooltip("When ON, fuel never drains. Use in Editor to test gameplay without time pressure.")]
    public bool infiniteFuel = false;
    [Tooltip("Debug: show the frost effect constantly (no pickup needed) to preview the look.")]
    public bool debugAlwaysShowFrost = false;

    private float currentFuel;
    private bool isGameOver = false;
    private bool hasLaunched = false;
    private bool isFalling = false;
    private float totalDistance = 0f;
    private float freezeTimer = 0f;
    private float scrollSpeed = 0f;
    private float boostCooldownTimer = 0f;
    private float boostTargetSpeed = 0f;
    private bool isBoosting = false;

    public bool HasLaunched => hasLaunched;
    public bool IsGameOver => isGameOver;
    public float ScrollSpeed => scrollSpeed;
    public bool CanBoost => boostCooldownTimer <= 0f && hasLaunched && !isGameOver;
    // 0 = cooldown just started, 1 = ready to boost
    public float BoostCooldownProgress => boostCooldown > 0f ? Mathf.Clamp01(1f - boostCooldownTimer / boostCooldown) : 1f;
    public bool IsFrozen => freezeTimer > 0f;

    void Awake()
    {
        Instance = this;
        SetGaugeVisible(false);
        if (currentScoreText) currentScoreText.gameObject.SetActive(false);
    }

    void Start()
    {
        currentFuel = maxFuel;
        if (gameOverPanel) gameOverPanel.SetActive(false);

        // Frost overlay shown during a freeze (frames the fuel ring by default).
        if (fillImage != null)
        {
            Canvas canvas = fillImage.GetComponentInParent<Canvas>(true);
            FuelTankFrost.Create(fillImage, canvas != null ? canvas.transform : null,
                frostTexture, frostTint, frostMaxAlpha, frostPlacement, debugAlwaysShowFrost);
        }
    }

    // Called by PlayerController.Launch() when the slingshot releases.
    public void SetScrollSpeed(float speed)
    {
        scrollSpeed = Mathf.Min(speed, maxScrollSpeed);
        hasLaunched = true;
        SetGaugeVisible(true);
        if (currentScoreText) currentScoreText.gameObject.SetActive(true);
    }

    // Called by Obstacle when the player is hit.
    public void ReduceScrollSpeed(float amount)
    {
        if (isBoosting)
        {
            // Hit during boost — stop the boost at current speed, ignore penalty
            isBoosting = false;
            return;
        }
        scrollSpeed = Mathf.Max(0f, scrollSpeed - amount);
    }

    void Update()
    {
        if (isGameOver) return;
        if (!hasLaunched) return;

        // Count down timers
        if (freezeTimer > 0f)
            freezeTimer -= Time.deltaTime;
        if (boostCooldownTimer > 0f)
            boostCooldownTimer -= Time.deltaTime;

        if (currentFuel > 0f)
        {
            // Drain fuel (skip while frozen or infinite fuel debug is on)
            if (freezeTimer <= 0f && !infiniteFuel)
            {
                currentFuel -= fuelDrainRate * Time.deltaTime;
                currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);
            }

            if (isBoosting)
            {
                // Gradually ramp up toward boost target
                scrollSpeed = Mathf.MoveTowards(scrollSpeed, boostTargetSpeed, boostAccelerationRate * Time.deltaTime);
                if (Mathf.Approximately(scrollSpeed, boostTargetSpeed))
                    isBoosting = false;
            }
            else if (scrollSpeed < maxScrollSpeed)
            {
                // Accelerate toward max speed normally
                scrollSpeed = Mathf.Min(scrollSpeed + acceleration * Time.deltaTime, maxScrollSpeed);
            }
            else if (scrollSpeed > maxScrollSpeed)
            {
                // Overdrive — decay back to max speed
                scrollSpeed = Mathf.Max(maxScrollSpeed, scrollSpeed - overdriveDragRate * Time.deltaTime);
            }
        }
        else
        {
            // Fuel empty — decelerate scroll speed naturally
            scrollSpeed = Mathf.Max(0f, scrollSpeed - decelerationRate * Time.deltaTime);
        }

        // Accumulate score distance
        totalDistance += scrollSpeed * Time.deltaTime;

        // Update fuel ring
        fillImage.fillAmount = currentFuel / maxFuel;

        // Update speed display
        speedText.text = Mathf.RoundToInt(scrollSpeed * 30f).ToString();
        if (unitText) unitText.text = "km/h";

        // Fuel ring color
        float ratio = currentFuel / maxFuel;
        if (ratio > 0.6f)
            fillImage.color = new Color(0f, 0.82f, 1f);
        else if (ratio > 0.3f)
            fillImage.color = new Color(1f, 0.75f, 0f);
        else
            fillImage.color = new Color(1f, 0.2f, 0.2f);

        // Update live score
        if (currentScoreText)
            currentScoreText.text = "Score: " + Mathf.RoundToInt(totalDistance) * 100 + "m";

        // If an obstacle hit drops speed near zero while fuel remains,
        // drain fuel so deceleration kicks in next frame.
        if (currentFuel > 0f && scrollSpeed < 0.1f)
            currentFuel = 0f;

        // When scroll speed hits zero, start the fall phase instead of
        // immediately showing game over — gives the player a visual fall.
        if (scrollSpeed <= 0f && !isFalling)
        {
            isFalling = true;
            if (playerController) playerController.StartFalling();
        }

        // Game over once the falling player exits the bottom of the screen
        if (isFalling)
        {
            Camera cam = Camera.main;
            if (cam != null && playerController != null)
            {
                float camBottom = cam.transform.position.y - cam.orthographicSize;
                if (playerController.transform.position.y < camBottom)
                    GameOver();
            }
        }
    }

    public void ActivateBoost()
    {
        if (!CanBoost) return;
        boostTargetSpeed = scrollSpeed + boostAmount;
        isBoosting = true;
        boostCooldownTimer = boostCooldown;
    }

    public void AddFuel(float amount)
    {
        currentFuel = Mathf.Clamp(currentFuel + amount, 0f, maxFuel);
    }

    public void ActivateFuelFreeze(float duration)
    {
        freezeTimer = Mathf.Max(freezeTimer, duration);
    }

    void SetGaugeVisible(bool visible)
    {
        fillImage.transform.parent.gameObject.SetActive(visible);
        if (boostButtonObject) boostButtonObject.SetActive(visible);
    }

    void GameOver()
    {
        if (isGameOver) return;
        isGameOver = true;
        scrollSpeed = 0f;
        if (playerController) playerController.enabled = false;

        int finalScore = Mathf.RoundToInt(totalDistance) * 100;

        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        if (totalScoreText) totalScoreText.text = "Total Score: " + finalScore + "m";
        if (highestScoreText) highestScoreText.text = "Best Score: " + highScore + "m";
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
