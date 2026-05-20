using UnityEngine;
using UnityEngine.EventSystems;

public class RestartButton : MonoBehaviour, IPointerClickHandler
{
    public SpeedManager speedManager;

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("BUTTON PHYSICALLY CLICKED");
        speedManager.RestartGame();
    }
}