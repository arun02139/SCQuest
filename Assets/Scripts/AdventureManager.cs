using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

public class AdventureManager : MonoBehaviour
{
    public static AdventureManager Instance { get; private set; }

    [Header("Settings")]
    [Tooltip("Filename inside StreamingAssets (e.g. adventure.txt)")]
    public string bookFileName = "adventure.txt";

    [Header("UI References")]
    public TMP_Text bodyLabel;
    public Transform choicesContainer;
    public Button choiceButtonPrefab;

    [Header("Static Page Images (optional)")]
    [Tooltip("Assign a RawImage to display pre-generated page images")]
    public RawImage pageImage;
    [Tooltip("Subfolder inside StreamingAssets for page images (e.g. 'images')")]
    public string imageFolder = "images";

    [Header("Splash Screen (optional)")]
    [Tooltip("Assign to re-show splash on restart")]
    public GameObject splashScreen;

    [Header("Gems")]
    [Tooltip("Text label to display gem count (e.g. '💎 3')")]
    public TMP_Text gemsLabel;

    private AdventureBook _book;
    private readonly HashSet<int> _visited = new();
    private int _gems;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private IEnumerator Start()
    {
        yield return LoadBook();

        if (_book != null && _book.Pages.ContainsKey(1))
        {
            // If there's a splash screen, wait for the player to click Begin
            if (splashScreen == null || !splashScreen.activeSelf)
                ShowPage(1);
        }
        else
            bodyLabel.text = "Error: could not load adventure or missing PAGE 1.";
    }

    private IEnumerator LoadBook()
    {
        // Build the path — use '/' separator since Path.Combine can break URLs on Web
        string basePath = Application.streamingAssetsPath;
        string filePath = basePath + "/" + bookFileName;

        if (filePath.StartsWith("jar") || filePath.StartsWith("http"))
        {
            // Web & Android: must use UnityWebRequest
            using var request = UnityWebRequest.Get(filePath);
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
                _book = AdventureBook.Parse(request.downloadHandler.text);
            else
                Debug.LogError($"Failed to load book: {request.error} (URL: {filePath})");
        }
        else
        {
            // Editor, Windows, Mac, Linux: can read directly
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

    public void Restart()
    {
        _visited.Clear();
        _gems = 0;
        UpdateGemsDisplay();

        // Clear choice buttons
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        bodyLabel.text = "";

        // Hide the image
        if (pageImage != null)
        {
            pageImage.texture = null;
            pageImage.color = new Color(1, 1, 1, 0);
        }

        // Re-show splash screen if assigned
        if (splashScreen != null)
            splashScreen.SetActive(true);
        else
            ShowPage(1);
    }

    private IEnumerator LoadPageImage(int pageId)
    {
        string basePath = Application.streamingAssetsPath;
        string filePath = basePath + "/" + imageFolder + "/" + pageId.ToString("D3") + ".jpg";

        // Local platforms need file:// prefix for UnityWebRequestTexture
        if (!filePath.StartsWith("jar") && !filePath.StartsWith("http"))
            filePath = "file://" + filePath;

        // Try loading static image
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
            // No image found — hide
            pageImage.texture = null;
            pageImage.color = new Color(1, 1, 1, 0);
        }
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

        // Award gems
        if (page.gems > 0)
        {
            _gems += page.gems;
            UpdateGemsDisplay();
        }

        // Load page image if we have a display for it
        if (pageImage != null)
            StartCoroutine(LoadPageImage(pageId));

        // Clear old buttons
        foreach (Transform child in choicesContainer)
            Destroy(child.gameObject);

        foreach (var choice in page.choices)
        {
            // Restart choices always show; others skip visited pages
            if (choice.targetPageId != AdventureBook.RESTART
                && _visited.Contains(choice.targetPageId))
                continue;

            var btn = Instantiate(choiceButtonPrefab, choicesContainer);
            btn.GetComponentInChildren<TMP_Text>().text = choice.label;
            btn.gameObject.SetActive(true);

            int target = choice.targetPageId;
            btn.onClick.AddListener(() => ShowPage(target));
        }
    }

    private void UpdateGemsDisplay()
    {
        if (gemsLabel != null)
            gemsLabel.text = _gems.ToString();
    }
}
