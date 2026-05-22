using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class SpeedManager : MonoBehaviour
{
    [Header("Fuel Settings")]
    public float maxFuel = 100f;
    public float fuelDrainRate = 10f; // empties in 10 seconds
    public float myGravityScale = 1f;

    [Header("Slowdown Settings")]
    public float slowDownRate = 1f; // how fast speed reduces when fuel is empty

    [Header("UI")]
    public Image fillImage;
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI unitText;
    public TextMeshProUGUI currentScoreText; // top right live score

    [Header("Game Over")]
    public GameObject gameOverPanel;
    public TextMeshProUGUI totalScoreText;  // final score
    public TextMeshProUGUI highestScoreText; // highest score

    [Header("References")]
    public Rigidbody2D playerRb;
    public PlayerController playerController;
    public CameraFollow cameraFollow;

    private float currentFuel;
    private bool isGameOver = false;
    private bool hasLaunched = false;
    private float totalDistance = 0f;
    private float freezeTimer = 0f;

    public bool HasLaunched => hasLaunched;
    public bool IsGameOver => isGameOver;

    void Awake()
    {
        SetGaugeVisible(false);
        if (currentScoreText) currentScoreText.gameObject.SetActive(false);
    }

    void Start()
    {
        currentFuel = maxFuel;
        if (gameOverPanel) gameOverPanel.SetActive(false);
    }

    void Update()
    {
        if (isGameOver) return;

        float currentSpeed = playerRb.linearVelocity.magnitude;

        // Show gauge only after first launch
        if (!hasLaunched && currentSpeed > 1f)
        {
            hasLaunched = true;
            SetGaugeVisible(true);
            if (currentScoreText) currentScoreText.gameObject.SetActive(true);
        }

        if (!hasLaunched) return;

        // Count down freeze timer
        if (freezeTimer > 0f)
            freezeTimer -= Time.deltaTime;

        // Drain fuel over time (skip while freeze is active)
        if (currentFuel > 0f)
        {
            if (freezeTimer <= 0f)
            {
                currentFuel -= fuelDrainRate * Time.deltaTime;
                currentFuel = Mathf.Clamp(currentFuel, 0f, maxFuel);
            }

            // No gravity while fuel is available or frozen
            playerRb.gravityScale = 0f;
        }
        else
        {
            // Fuel empty — gradually slow down the player
            playerRb.gravityScale = myGravityScale;
            playerRb.linearVelocity = Vector2.Lerp(
                playerRb.linearVelocity,
                Vector2.zero,
                slowDownRate * Time.deltaTime
            );
        }

        // Update fuel ring
        fillImage.fillAmount = currentFuel / maxFuel;

        // Update speed number
        speedText.text = Mathf.RoundToInt(currentSpeed * 10f).ToString();
        if (unitText) unitText.text = "km/h";

        // Fuel ring color
        float ratio = currentFuel / maxFuel;
        if (ratio > 0.6f)
            fillImage.color = new Color(0f, 0.82f, 1f);
        else if (ratio > 0.3f)
            fillImage.color = new Color(1f, 0.75f, 0f);
        else
            fillImage.color = new Color(1f, 0.2f, 0.2f);

        // Track upward distance
        if (playerRb.linearVelocity.y > 0)
            totalDistance += playerRb.linearVelocity.y * Time.deltaTime;

        // Update live score on top right
        if (currentScoreText)
            currentScoreText.text = "Score: " + Mathf.RoundToInt(totalDistance) * 10 + "m";

        // Game over only once the player has fully stopped AND fuel is empty.
        // This must come BEFORE the force-fall check below so both don't fire
        // in the same frame (currentSpeed is captured once at the top of Update).
        if (hasLaunched && currentFuel <= 0f && currentSpeed < 0.05f)
            GameOver();

        // If the player's speed drops to near-zero while fuel is still available
        // (e.g. hit by an obstacle), drain the fuel so gravity kicks in next frame
        // and the player visually falls before game over triggers.
        if (hasLaunched && currentFuel > 0f && currentSpeed < 0.1f)
            currentFuel = 0f;
    }

    public void AddFuel(float amount)
    {
        currentFuel = Mathf.Clamp(currentFuel + amount, 0f, maxFuel);
    }

    public void ActivateFuelFreeze(float duration)
    {
        // If a freeze is already active, extend it rather than restart it.
        freezeTimer = Mathf.Max(freezeTimer, duration);
    }

    void SetGaugeVisible(bool visible)
    {
        fillImage.transform.parent.gameObject.SetActive(visible);
    }

    void GameOver()
    {
        isGameOver = true;
        playerController.enabled = false;
        cameraFollow.enabled = false;

        int finalScore = Mathf.RoundToInt(totalDistance) * 10;

        // Save high score
        int highScore = PlayerPrefs.GetInt("HighScore", 0);
        if (finalScore > highScore)
        {
            highScore = finalScore;
            PlayerPrefs.SetInt("HighScore", highScore);
            PlayerPrefs.Save();
        }

        // Update game over UI
        if (totalScoreText) totalScoreText.text = "Total Score: " + finalScore + "m";
        if (highestScoreText) highestScoreText.text = "Best Score: " + highScore + "m";
        if (gameOverPanel) gameOverPanel.SetActive(true);
    }

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}