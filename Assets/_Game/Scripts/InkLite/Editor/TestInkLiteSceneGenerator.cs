using PracticeAnything.InkLite;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PracticeAnything.Editor.InkLite
{
    public static class TestInkLiteSceneGenerator
    {
        private const string ScenePath = "Assets/_Game/Content/Scenes/TestInkLite.unity";
        private const string ScenarioPath = "Assets/_Game/Content/InkLite/TestInkLite.inklite.txt";
        private const string ExampleImagePath = "Assets/_Game/Content/example.png";

        [MenuItem("Tools/PracticeAnything/Generate Test InkLite Scene")]
        public static void Generate()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateCamera();
            Canvas canvas = CreateCanvas();
            EnsureEventSystem();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            RectTransform phone = CreateRect("LineLikePhone", canvas.transform, new Vector2(760, 1320), Vector2.zero);
            Image phoneImage = phone.gameObject.AddComponent<Image>();
            phoneImage.color = new Color(0.025f, 0.028f, 0.032f, 1f);
            phone.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.35f);

            RectTransform screen = CreateRect("ChatScreen", phone, new Vector2(700, 1240), Vector2.zero);
            Image screenImage = screen.gameObject.AddComponent<Image>();
            screenImage.color = new Color(0.68f, 0.78f, 0.87f, 1f);

            RectTransform header = CreateRect("Header", screen, new Vector2(700, 132), new Vector2(0, 554));
            Image headerImage = header.gameObject.AddComponent<Image>();
            headerImage.color = new Color(0.02f, 0.78f, 0.34f, 1f);
            Text back = CreateText("Back", header, "<", font, 38, TextAnchor.MiddleCenter, Color.white);
            back.rectTransform.sizeDelta = new Vector2(70, 92);
            back.rectTransform.anchoredPosition = new Vector2(-302, -18);
            Text title = CreateText("Title", header, "InkLite 測試帳號", font, 32, TextAnchor.MiddleLeft, Color.white);
            title.rectTransform.sizeDelta = new Vector2(420, 48);
            title.rectTransform.anchoredPosition = new Vector2(-74, -6);
            Text subtitle = CreateText("Subtitle", header, "LINE 風格聊天室", font, 20, TextAnchor.MiddleLeft, new Color(0.88f, 1f, 0.91f, 1f));
            subtitle.rectTransform.sizeDelta = new Vector2(420, 32);
            subtitle.rectTransform.anchoredPosition = new Vector2(-74, -40);
            Text menu = CreateText("Menu", header, "...", font, 34, TextAnchor.MiddleCenter, Color.white);
            menu.rectTransform.sizeDelta = new Vector2(86, 76);
            menu.rectTransform.anchoredPosition = new Vector2(292, -18);

            RectTransform statusBar = CreateRect("InputBar", screen, new Vector2(700, 76), new Vector2(0, -582));
            Image statusImage = statusBar.gameObject.AddComponent<Image>();
            statusImage.color = new Color(0.965f, 0.97f, 0.965f, 1f);
            RectTransform inputField = CreateRect("FakeInputField", statusBar, new Vector2(520, 46), new Vector2(-38, 0));
            Image inputImage = inputField.gameObject.AddComponent<Image>();
            inputImage.color = Color.white;
            Text statusText = CreateText("StatusText", inputField, string.Empty, font, 20, TextAnchor.MiddleLeft, new Color(0.42f, 0.47f, 0.43f, 1f));
            Stretch(statusText.rectTransform, new Vector2(22, 4), new Vector2(-22, -4));
            Text send = CreateText("SendLabel", statusBar, "送出", font, 22, TextAnchor.MiddleCenter, new Color(0.02f, 0.78f, 0.34f, 1f));
            send.rectTransform.sizeDelta = new Vector2(88, 52);
            send.rectTransform.anchoredPosition = new Vector2(288, 0);

            RectTransform choiceRoot = CreateRect("ChoiceRoot", screen, new Vector2(700, 230), new Vector2(0, -446));
            VerticalLayoutGroup choiceLayout = choiceRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            choiceLayout.padding = new RectOffset(28, 28, 16, 16);
            choiceLayout.spacing = 10;
            choiceLayout.childControlWidth = true;
            choiceLayout.childControlHeight = true;
            choiceLayout.childForceExpandWidth = true;
            choiceLayout.childForceExpandHeight = false;
            Image choiceBg = choiceRoot.gameObject.AddComponent<Image>();
            choiceBg.color = new Color(0.965f, 0.97f, 0.965f, 0.96f);

            Button[] choiceButtons = new Button[3];
            Text[] choiceTexts = new Text[3];
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                choiceButtons[i] = CreateButton($"ChoiceButton{i + 1}", choiceRoot, $"Choice {i + 1}", font, new Color(0.02f, 0.78f, 0.34f, 1f));
                choiceTexts[i] = choiceButtons[i].GetComponentInChildren<Text>();
            }

            RectTransform viewport = CreateRect("Viewport", screen, new Vector2(672, 860), new Vector2(0, 62));
            Image viewportMaskImage = viewport.gameObject.AddComponent<Image>();
            viewportMaskImage.color = new Color(0.68f, 0.78f, 0.87f, 1f);
            Mask mask = viewport.gameObject.AddComponent<Mask>();
            mask.showMaskGraphic = true;

            RectTransform content = CreateRect("MessageContent", viewport, new Vector2(672, 860), Vector2.zero);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = new Vector2(1, 1);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            VerticalLayoutGroup contentLayout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 18, 18);
            contentLayout.spacing = 10;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;
            ContentSizeFitter fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect scrollRect = screen.gameObject.AddComponent<ScrollRect>();
            scrollRect.viewport = viewport;
            scrollRect.content = content;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            RectTransform typing = CreateRect("TypingIndicator", screen, new Vector2(110, 54), new Vector2(-250, -340));
            Image typingImage = typing.gameObject.AddComponent<Image>();
            typingImage.color = Color.white;
            typing.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.08f);
            Text typingText = CreateText("TypingText", typing, "...", font, 28, TextAnchor.MiddleCenter, new Color(0.1f, 0.12f, 0.13f, 1f));
            Stretch(typingText.rectTransform, Vector2.zero, Vector2.zero);

            InkLiteChatView view = screen.gameObject.AddComponent<InkLiteChatView>();
            InkLiteChatRunner runner = screen.gameObject.AddComponent<InkLiteChatRunner>();
            TextAsset scenario = AssetDatabase.LoadAssetAtPath<TextAsset>(ScenarioPath);
            Sprite exampleSprite = LoadFirstSprite(ExampleImagePath);
            SetViewReferences(view, content, scrollRect, choiceRoot, choiceButtons, choiceTexts, typing.gameObject, typingText, statusText, font, exampleSprite);
            SetRunnerReferences(runner, scenario, view);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"Generated {ScenePath}");
        }

        public static void Verify()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            InkLiteChatRunner runner = Object.FindFirstObjectByType<InkLiteChatRunner>();
            InkLiteChatView view = Object.FindFirstObjectByType<InkLiteChatView>();
            if (!scene.IsValid() || runner == null || view == null)
            {
                throw new System.InvalidOperationException($"{ScenePath} is missing InkLite runner/view.");
            }

            InkLiteParser.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(ScenarioPath).text);
            Debug.Log($"Verified {ScenePath}");
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.18f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.AddComponent<AudioListener>();
        }

        private static Canvas CreateCanvas()
        {
            GameObject canvasObject = new("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
        }

        private static Button CreateButton(string name, Transform parent, string label, Font font, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(640, 62), Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            LayoutElement layout = rect.gameObject.AddComponent<LayoutElement>();
            layout.preferredHeight = 62;
            Text text = CreateText("Text", rect, label, font, 24, TextAnchor.MiddleCenter, Color.white);
            Stretch(text.rectTransform, new Vector2(16, 6), new Vector2(-16, -6));
            return button;
        }

        private static Text CreateText(string name, Transform parent, string value, Font font, int fontSize, TextAnchor anchor, Color color)
        {
            RectTransform rect = CreateRect(name, parent, Vector2.zero, Vector2.zero);
            Text text = rect.gameObject.AddComponent<Text>();
            text.text = value;
            text.font = font;
            text.fontSize = fontSize;
            text.alignment = anchor;
            text.color = color;
            return text;
        }

        private static RectTransform CreateRect(string name, Transform parent, Vector2 size, Vector2 position)
        {
            GameObject gameObject = new(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            RectTransform rect = gameObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static Sprite LoadFirstSprite(string path)
        {
            Sprite directSprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (directSprite != null)
            {
                return directSprite;
            }

            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    return sprite;
                }
            }

            return null;
        }

        private static void SetViewReferences(InkLiteChatView view, RectTransform messageRoot, ScrollRect scrollRect, RectTransform choiceRoot, Button[] choiceButtons, Text[] choiceTexts, GameObject typingIndicator, Text typingText, Text statusText, Font font, Sprite exampleSprite)
        {
            SerializedObject serializedObject = new(view);
            serializedObject.FindProperty("messageRoot").objectReferenceValue = messageRoot;
            serializedObject.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            serializedObject.FindProperty("choiceRoot").objectReferenceValue = choiceRoot;
            serializedObject.FindProperty("typingIndicator").objectReferenceValue = typingIndicator;
            serializedObject.FindProperty("typingText").objectReferenceValue = typingText;
            serializedObject.FindProperty("statusText").objectReferenceValue = statusText;
            serializedObject.FindProperty("font").objectReferenceValue = font;
            SerializedProperty imageIds = serializedObject.FindProperty("imageIds");
            imageIds.arraySize = 1;
            imageIds.GetArrayElementAtIndex(0).stringValue = "example.png";
            SerializedProperty imageSprites = serializedObject.FindProperty("imageSprites");
            imageSprites.arraySize = 1;
            imageSprites.GetArrayElementAtIndex(0).objectReferenceValue = exampleSprite;

            SerializedProperty buttons = serializedObject.FindProperty("choiceButtons");
            buttons.arraySize = choiceButtons.Length;
            SerializedProperty texts = serializedObject.FindProperty("choiceTexts");
            texts.arraySize = choiceTexts.Length;
            for (int i = 0; i < choiceButtons.Length; i++)
            {
                buttons.GetArrayElementAtIndex(i).objectReferenceValue = choiceButtons[i];
                texts.GetArrayElementAtIndex(i).objectReferenceValue = choiceTexts[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRunnerReferences(InkLiteChatRunner runner, TextAsset scenario, InkLiteChatView view)
        {
            SerializedObject serializedObject = new(runner);
            serializedObject.FindProperty("scenarioAsset").objectReferenceValue = scenario;
            serializedObject.FindProperty("view").objectReferenceValue = view;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
