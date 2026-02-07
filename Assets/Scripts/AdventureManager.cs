using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class AdventureManager : MonoBehaviour
{
    public static AdventureManager Instance { get; private set; }

    [Header("Adventures")]
    [Tooltip("List of adventure filenames in StreamingAssets (e.g. adventure1.txt, adventure2.txt)")]
    public string[] adventureFiles = { "adventure1.txt" };

    [Header("UI References")]
    public TMP_Text bodyLabel;
    public Transform choicesContainer;
    public Button choiceButtonPrefab;

    [Header("Static Page Images (optional)")]
    public RawImage pageImage;
    public string imageFolder = "images";

    [Header("Splash Screen")]
    public GameObject splashScreen;
    [Tooltip("Subfolder in StreamingAssets for cover images (cover_001.jpg, cover_002.jpg, etc.)")]
    public string coverFolder = "images";
    [Tooltip("Total number of cover images available")]
    public int coverCount = 3;

    [Header("Hearts")]
    public TMP_Text heartsLabel;
    public int startingHearts = 3;

    [Header("Game Over / Pay")]
    [Tooltip("Image shown on Start button when hearts reach 0 (e.g. '$0.99 for 3 Hearts')")]
    public Sprite paySprite;
    [Tooltip("Original Start button image to restore after paying")]
    public Sprite startSprite;

    private AdventureBook _book;
    private readonly HashSet<int> _visited = new();
    private int _hearts;
    private int _currentAdventureIndex;
    private int _currentCoverIndex;
    private readonly List<string> _inventory = new();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private IEnumerator Start()
    {
        _hearts = startingHearts;
        _currentAdventureIndex = 0;
        UpdateHeartsDisplay();

        yield return LoadBook(adventureFiles[_currentAdventureIndex]);

        if (_book != null && _book.Pages.ContainsKey(1))
        {
            if (splashScreen == null || !splashScreen.activeSelf)
                ShowPage(1);
        }
        else
            bodyLabel.text = "Error: could not load adventure or missing PAGE 1.";
    }

    private IEnumerator LoadBook(string fileName)
    {
        string basePath = Application.streamingAssetsPath;
        string filePath = basePath + "/" + fileName;

        if (filePath.StartsWith("jar") || filePath.StartsWith("http"))
        {
            using var request = UnityWebRequest.Get(filePath);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                _book = AdventureBook.Parse(request.downloadHandler.text);
            else
                Debug.LogError($"Failed to load book: {request.error} (URL: {filePath})");
        }
        else
        {
            try
            {
                string text = System.IO.File.ReadAllText(filePath);
                _book = AdventureBook.Parse(text);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load book: {e.Message}");
            }
            yield break;
        }
    }

    /// <summary>
    /// Called when the player hits [Play again -> restart] (loses a heart).
    /// </summary>
    public void Restart()
    {
        _visited.Clear();

        _hearts--;
        UpdateHeartsDisplay();

        ClearUI();

        // Game over — no hearts left
        if (_hearts <= 0)
        {
            ShowGameOver();
            return;
        }

        // Re-show splash screen
        if (splashScreen != null)
            splashScreen.SetActive(true);
        else
            ShowPage(1);
    }

    /// <summary>
    /// Load the next adventure (or wrap around). Resets hearts to full.
    /// </summary>
    public void Reroll()
    {
        _visited.Clear();
        _currentCoverIndex = (_currentCoverIndex + 1) % coverCount;
        _currentAdventureIndex = _currentCoverIndex % adventureFiles.Length;

        StartCoroutine(RerollCoroutine());
    }

    private IEnumerator RerollCoroutine()
    {
        yield return LoadBook(adventureFiles[_currentAdventureIndex]);
        yield return LoadCoverImage(_currentCoverIndex);

        if (splashScreen != null)
            splashScreen.SetActive(true);
        else
            ShowPage(1);
    }

    private IEnumerator LoadCoverImage(int index)
    {
        if (splashScreen == null) yield break;

        var splash = splashScreen.GetComponent<SplashScreen>();
        if (splash == null || splash.coverImage == null)
        {
            Debug.LogWarning("SplashScreen or coverImage not assigned");
            yield break;
        }

        string basePath = Application.streamingAssetsPath;
        string fileName = "cover_" + (index + 1).ToString("D3") + ".jpg";
        string filePath = string.IsNullOrEmpty(coverFolder)
            ? basePath + "/" + fileName
            : basePath + "/" + coverFolder + "/" + fileName;

        if (!filePath.StartsWith("jar") && !filePath.StartsWith("http"))
            filePath = "file://" + filePath;

        Debug.Log($"Loading cover image: {filePath}");

        using var request = UnityWebRequestTexture.GetTexture(filePath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var texture = DownloadHandlerTexture.GetContent(request);
            var sprite = Sprite.Create(texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f));
            splash.coverImage.sprite = sprite;
            Debug.Log($"Cover image loaded successfully: {fileName}");
        }
        else
        {
            Debug.LogWarning($"Cover image not found: {filePath} — {request.error}");
        }
    }

    /// <summary>
    /// Called by the paid reroll button on the splash screen.
    /// Cycles to the next cover image.
    /// </summary>
    public void PaidReroll()
    {
        // TODO: Add IAP verification here
        _currentCoverIndex = (_currentCoverIndex + 1) % coverCount;
        _currentAdventureIndex = _currentCoverIndex % adventureFiles.Length;

        _hearts = startingHearts;
        UpdateHeartsDisplay();

        StartCoroutine(PaidRerollCoroutine());
    }

    private IEnumerator PaidRerollCoroutine()
    {
        yield return LoadBook(adventureFiles[_currentAdventureIndex]);
        yield return LoadCoverImage(_currentCoverIndex);
    }

    public void ShowPage(int pageId)
    {
        // Handle restart target
        if (pageId == AdventureBook.RESTART)
        {
            Restart();
            return;
        }

        if (_book == null || !_book.Pages.TryGetValue(pageId, out var page))
        {
            bodyLabel.text = $"Page {pageId} not found.";
            return;
        }

        _visited.Add(pageId);
        bodyLabel.text = page.bodyText;

        // Award item if this page has one
        if (!string.IsNullOrEmpty(page.item) && !_inventory.Contains(page.item))
        {
            _inventory.Add(page.item);
        }

        // Load page image
        if (pageImage != null)
            StartCoroutine(LoadPageImage(pageId));

        // Clear old buttons
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        // If this page awards an item, auto-reroll to next adventure instead of normal choices
        if (!string.IsNullOrEmpty(page.item))
        {
            var btn = Instantiate(choiceButtonPrefab, choicesContainer);
            btn.GetComponentInChildren<TMP_Text>().text = "Continue to next adventure";
            btn.gameObject.SetActive(true);
            btn.onClick.AddListener(() =>
            {
                _hearts--;
                UpdateHeartsDisplay();
                ClearUI();

                if (_hearts <= 0)
                {
                    ShowGameOver();
                    return;
                }

                Reroll();
            });
            return;
        }

        foreach (var choice in page.choices)
        {
            var btn = Instantiate(choiceButtonPrefab, choicesContainer);
            btn.GetComponentInChildren<TMP_Text>().text = choice.label;
            btn.gameObject.SetActive(true);

            int target = choice.targetPageId;
            btn.onClick.AddListener(() => ShowPage(target));
        }
    }

    private IEnumerator LoadPageImage(int pageId)
    {
        string basePath = Application.streamingAssetsPath;
        string adventureName = System.IO.Path.GetFileNameWithoutExtension(adventureFiles[_currentAdventureIndex]);
        string filePath = basePath + "/" + imageFolder + "/" + adventureName + "/" + pageId.ToString("D3") + ".jpg";

        if (!filePath.StartsWith("jar") && !filePath.StartsWith("http"))
            filePath = "file://" + filePath;

        Debug.Log($"Loading page image: {filePath}");

        using var request = UnityWebRequestTexture.GetTexture(filePath);
        yield return request.SendWebRequest();

        if (request.result == UnityWebRequest.Result.Success)
        {
            var texture = DownloadHandlerTexture.GetContent(request);
            pageImage.texture = texture;
            pageImage.color = Color.white;
        }
        else
        {
            Debug.LogWarning($"Page image not found: {filePath} — {request.error}");
            pageImage.texture = null;
            pageImage.color = new Color(1, 1, 1, 0);
        }
    }

    private void ClearUI()
    {
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        bodyLabel.text = "";

        if (pageImage != null)
        {
            pageImage.texture = null;
            pageImage.color = new Color(1, 1, 1, 0);
        }
    }

    private void ShowGameOver()
    {
        if (splashScreen != null)
        {
            splashScreen.SetActive(true);

            var splash = splashScreen.GetComponent<SplashScreen>();
            if (splash != null && splash.beginButton != null)
            {
                var btnImg = splash.beginButton.GetComponent<Image>();

                // Swap to pay image
                if (paySprite != null && btnImg != null)
                    btnImg.sprite = paySprite;

                // Replace click: pay → refill hearts → restore start button
                splash.beginButton.onClick.RemoveAllListeners();
                splash.beginButton.onClick.AddListener(() =>
                {
                    // TODO: Add IAP verification here
                    _hearts = startingHearts;
                    UpdateHeartsDisplay();

                    // Restore the start button image and normal behavior
                    if (startSprite != null && btnImg != null)
                        btnImg.sprite = startSprite;

                    splash.beginButton.onClick.RemoveAllListeners();
                    splash.beginButton.onClick.AddListener(() =>
                    {
                        splashScreen.SetActive(false);
                        ShowPage(1);
                    });
                });
            }
        }
        else
        {
            bodyLabel.text = "You have no hearts left. Game over!";
        }
    }

    private void UpdateHeartsDisplay()
    {
        if (heartsLabel != null)
            heartsLabel.text = _hearts.ToString();
    }
}
