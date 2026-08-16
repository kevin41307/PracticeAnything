using UnityEngine;
using UnityEngine.UI;

namespace PracticeAnything.InkLite
{
    public sealed class InkLiteChatView : MonoBehaviour
    {
        [SerializeField] private RectTransform messageRoot;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform choiceRoot;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private Text[] choiceTexts;
        [SerializeField] private GameObject typingIndicator;
        [SerializeField] private Text typingText;
        [SerializeField] private Text statusText;
        [SerializeField] private Font font;
        [SerializeField] private string[] imageIds;
        [SerializeField] private Sprite[] imageSprites;

        public void Clear()
        {
            foreach (Transform child in messageRoot)
            {
                Destroy(child.gameObject);
            }

            HideChoices();
            ShowTyping(false);
            SetStatus(string.Empty);
        }

        public void AddMessage(InkLiteSpeaker speaker, InkLiteMessageType type, string content)
        {
            GameObject rowObject = new($"{speaker}_{type}_Row", typeof(RectTransform));
            rowObject.transform.SetParent(messageRoot, false);
            HorizontalLayoutGroup rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childControlWidth = false;
            rowLayout.childControlHeight = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;
            rowLayout.padding = new RectOffset(10, 10, 4, 4);
            rowLayout.spacing = 8;
            rowLayout.childAlignment = speaker == InkLiteSpeaker.Player ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;

            LayoutElement spacer = CreateSpacer(rowObject.transform);
            Color bubbleColor = speaker == InkLiteSpeaker.Player ? new Color(0.22f, 0.67f, 0.32f, 1f) : Color.white;
            RectTransform bubble = CreateBubble(rowObject.transform, bubbleColor);

            if (speaker == InkLiteSpeaker.Player)
            {
                spacer.transform.SetAsFirstSibling();
            }
            else
            {
                spacer.transform.SetAsLastSibling();
            }

            if (type == InkLiteMessageType.Text)
            {
                Text text = CreateText("MessageText", bubble, content, 22, speaker == InkLiteSpeaker.Player ? Color.white : new Color(0.08f, 0.1f, 0.12f, 1f));
                text.alignment = TextAnchor.MiddleLeft;
                text.horizontalOverflow = HorizontalWrapMode.Wrap;
                text.verticalOverflow = VerticalWrapMode.Overflow;
                Stretch(text.rectTransform, new Vector2(18, 12), new Vector2(-18, -12));
                SetBubbleSize(bubble, new Vector2(560, Mathf.Max(64, EstimateTextHeight(content))));
            }
            else
            {
                Image image = bubble.GetComponent<Image>();
                image.color = speaker == InkLiteSpeaker.Player ? new Color(0.18f, 0.55f, 0.28f, 1f) : new Color(0.86f, 0.91f, 0.96f, 1f);
                if (TryGetImageSprite(content, out Sprite sprite))
                {
                    Image contentImage = CreateImage("ImageContent", bubble, sprite);
                    Stretch(contentImage.rectTransform, new Vector2(12, 12), new Vector2(-12, -12));
                }
                else
                {
                    Text imageLabel = CreateText("ImageLabel", bubble, $"[圖片]\n{content}", 22, speaker == InkLiteSpeaker.Player ? Color.white : new Color(0.08f, 0.1f, 0.12f, 1f));
                    imageLabel.alignment = TextAnchor.MiddleCenter;
                    Stretch(imageLabel.rectTransform, new Vector2(16, 16), new Vector2(-16, -16));
                }

                SetBubbleSize(bubble, new Vector2(420, 220));
            }

            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }

        public void ShowTyping(bool visible)
        {
            if (typingIndicator != null)
            {
                typingIndicator.SetActive(visible);
            }

            if (typingText != null)
            {
                typingText.text = "...";
            }
        }

        public void ShowChoices(System.Collections.Generic.IReadOnlyList<InkLiteChoiceOption> options, System.Action<int> onSelected)
        {
            choiceRoot.gameObject.SetActive(true);
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                bool visible = i < options.Count;
                choiceButtons[i].gameObject.SetActive(visible);
                choiceButtons[i].onClick.RemoveAllListeners();

                if (visible)
                {
                    int choiceIndex = i;
                    choiceTexts[i].text = options[i].Text;
                    choiceButtons[i].onClick.AddListener(() => onSelected(choiceIndex));
                }
            }
        }

        public void HideChoices()
        {
            if (choiceRoot == null)
            {
                return;
            }

            choiceRoot.gameObject.SetActive(false);
            foreach (Button button in choiceButtons)
            {
                button.onClick.RemoveAllListeners();
                button.gameObject.SetActive(false);
            }
        }

        public void SetStatus(string value)
        {
            if (statusText != null)
            {
                statusText.text = value;
            }
        }

        private RectTransform CreateBubble(Transform parent, Color color)
        {
            GameObject bubbleObject = new("Bubble", typeof(RectTransform));
            bubbleObject.transform.SetParent(parent, false);
            Image image = bubbleObject.AddComponent<Image>();
            image.color = color;
            return bubbleObject.GetComponent<RectTransform>();
        }

        private LayoutElement CreateSpacer(Transform parent)
        {
            GameObject spacerObject = new("Spacer", typeof(RectTransform));
            spacerObject.transform.SetParent(parent, false);
            LayoutElement spacer = spacerObject.AddComponent<LayoutElement>();
            spacer.flexibleWidth = 1f;
            return spacer;
        }

        private Text CreateText(string name, Transform parent, string value, int size, Color color)
        {
            GameObject textObject = new(name, typeof(RectTransform));
            textObject.transform.SetParent(parent, false);
            Text text = textObject.AddComponent<Text>();
            text.font = font != null ? font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.fontSize = size;
            text.color = color;
            text.text = value;
            return text;
        }

        private Image CreateImage(string name, Transform parent, Sprite sprite)
        {
            GameObject imageObject = new(name, typeof(RectTransform));
            imageObject.transform.SetParent(parent, false);
            Image image = imageObject.AddComponent<Image>();
            image.sprite = sprite;
            image.preserveAspect = true;
            image.color = Color.white;
            return image;
        }

        private bool TryGetImageSprite(string imageId, out Sprite sprite)
        {
            if (imageIds != null && imageSprites != null)
            {
                int count = Mathf.Min(imageIds.Length, imageSprites.Length);
                for (int i = 0; i < count; i++)
                {
                    if (imageIds[i] == imageId && imageSprites[i] != null)
                    {
                        sprite = imageSprites[i];
                        return true;
                    }
                }
            }

            sprite = null;
            return false;
        }

        private static void SetBubbleSize(RectTransform rect, Vector2 size)
        {
            rect.sizeDelta = size;
            LayoutElement layoutElement = rect.gameObject.AddComponent<LayoutElement>();
            layoutElement.preferredWidth = size.x;
            layoutElement.preferredHeight = size.y;
        }

        private static float EstimateTextHeight(string content)
        {
            int lineCount = Mathf.Max(1, Mathf.CeilToInt(content.Length / 18f));
            return 36 + lineCount * 30;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }
    }
}
