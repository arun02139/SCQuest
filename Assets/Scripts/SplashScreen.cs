using UnityEngine;
using UnityEngine.UI;

public class SplashScreen : MonoBehaviour
{
    [Tooltip("The Begin/Start button")]
    public Button beginButton;

    private void Start()
    {
        beginButton.onClick.AddListener(OnBegin);
    }

    private void OnBegin()
    {
        gameObject.SetActive(false);

        if (AdventureManager.Instance != null)
            AdventureManager.Instance.ShowPage(1);
    }
}
