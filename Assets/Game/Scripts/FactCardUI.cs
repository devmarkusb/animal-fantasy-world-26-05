using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Runtime-created UI card that displays an animal's name and fun fact.
/// Builds its own Canvas, EventSystem, and layout on first use.
/// One shared instance across all <see cref="ClickableAnimal"/> components.
/// </summary>
public class FactCardUI : MonoBehaviour
{
    static FactCardUI _instance;

    GameObject _card;
    Text _nameLabel;
    Text _factLabel;

    public bool IsVisible => _card != null && _card.activeSelf;

    public static FactCardUI Instance
    {
        get
        {
            if (_instance == null)
                CreateInstance();
            return _instance;
        }
    }

    public void Show(string animalName, string factText)
    {
        _nameLabel.text = animalName;
        _factLabel.text = factText;
        _card.SetActive(true);
    }

    public void Hide()
    {
        if (_card != null)
            _card.SetActive(false);
    }

    static void CreateInstance()
    {
        if (EventSystem.current == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<StandaloneInputModule>();
        }

        var canvasGO = new GameObject("FactCardCanvas");

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();

        _instance = canvasGO.AddComponent<FactCardUI>();
        _instance.BuildCard();
    }

    void BuildCard()
    {
        Font font = LoadFont();

        _card = CreateRect("FactCard", transform);
        var cardRect = Rect(_card);
        cardRect.anchorMin = new Vector2(0.5f, 0f);
        cardRect.anchorMax = new Vector2(0.5f, 0f);
        cardRect.pivot = new Vector2(0.5f, 0f);
        cardRect.anchoredPosition = new Vector2(0f, 32f);
        cardRect.sizeDelta = new Vector2(520f, 0f);

        var bg = _card.AddComponent<Image>();
        bg.color = new Color(0.99f, 0.97f, 0.93f, 0.97f);

        var layout = _card.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(28, 28, 22, 22);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var fitter = _card.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        _nameLabel = AddLabel("AnimalName", _card.transform, font, 28,
            FontStyle.Bold, new Color(0.17f, 0.14f, 0.11f), TextAnchor.MiddleCenter);

        AddDivider(_card.transform);

        _factLabel = AddLabel("FactText", _card.transform, font, 22,
            FontStyle.Normal, new Color(0.30f, 0.26f, 0.22f), TextAnchor.UpperCenter);

        BuildCloseButton(_card.transform, font);

        _card.SetActive(false);
    }

    void AddDivider(Transform parent)
    {
        var go = CreateRect("Divider", parent);
        go.AddComponent<Image>().color = new Color(0.85f, 0.80f, 0.72f);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 2f;
        le.flexibleWidth = 1f;
    }

    void BuildCloseButton(Transform parent, Font font)
    {
        var go = CreateRect("CloseButton", parent);

        var btnImage = go.AddComponent<Image>();
        btnImage.color = new Color(0.55f, 0.78f, 0.58f);

        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 40f;
        le.preferredWidth = 130f;
        le.flexibleWidth = 0f;

        var btn = go.AddComponent<Button>();
        btn.targetGraphic = btnImage;
        var colors = btn.colors;
        colors.highlightedColor = new Color(0.62f, 0.84f, 0.64f);
        colors.pressedColor = new Color(0.45f, 0.68f, 0.48f);
        btn.colors = colors;
        btn.onClick.AddListener(Hide);

        var label = AddLabel("Label", go.transform, font, 20,
            FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        label.text = "Close";
    }

    static Text AddLabel(string name, Transform parent, Font font, int size,
        FontStyle style, Color color, TextAnchor anchor)
    {
        var go = CreateRect(name, parent);
        var text = go.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.fontStyle = style;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    static GameObject CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }

    static RectTransform Rect(GameObject go) => (RectTransform)go.transform;

    static Font LoadFont()
    {
        var font = Resources.GetBuiltinResource<Font>("LegacySRuntime.ttf");
        if (font == null)
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return font;
    }
}
