using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance { get; private set; }

    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup fadeCanvasGroup;
    private bool isLoading;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SetupFadeCanvas();
    }

    void SetupFadeCanvas()
    {
        var canvasGo = new GameObject("FadeCanvas");
        canvasGo.transform.SetParent(transform);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();

        var panel = new GameObject("FadePanel");
        panel.transform.SetParent(canvasGo.transform, false);

        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        var image = panel.AddComponent<UnityEngine.UI.Image>();
        image.color = Color.black;

        fadeCanvasGroup = panel.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.blocksRaycasts = false;
    }

    public void LoadScene(SceneName sceneName)
    {
        if (isLoading) return;
        StartCoroutine(LoadSceneRoutine(sceneName.ToString()));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        yield return StartCoroutine(Fade(0f, 1f));

        yield return SceneManager.LoadSceneAsync(sceneName);

        yield return StartCoroutine(Fade(1f, 0f));

        isLoading = false;
    }

    private IEnumerator Fade(float from, float to)
    {
        fadeCanvasGroup.blocksRaycasts = true;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            fadeCanvasGroup.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        fadeCanvasGroup.alpha = to;
        fadeCanvasGroup.blocksRaycasts = to > 0f;
    }
}

public enum SceneName
{
    TitleScene,
    BaseScene,
    FieldScene,
    ResultScene,
}
