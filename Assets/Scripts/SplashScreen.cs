using UnityEngine;
using UnityEngine.UI;

public class SplashScreen : MonoBehaviour
{
    [Tooltip("The Begin/Start button")]
    public Button beginButton;

    [Tooltip("The Reroll button ($0.99)")]
    public Button rerollButton;

    [Tooltip("The RawImage or Image that displays the cover art")]
    public Image coverImage;

    private void Start()
    {
        beginButton.onClick.AddListener(OnBegin);

        if (rerollButton != null)
            rerollButton.onClick.AddListener(OnReroll);
    }

    private void OnBegin()
    {
        gameObject.SetActive(false);

        if (AdventureManager.Instance != null)
            AdventureManager.Instance.ShowPage(1);
    }

    private void OnReroll()
    {
        // Disabled for now — button is visible but does nothing
    }
}
