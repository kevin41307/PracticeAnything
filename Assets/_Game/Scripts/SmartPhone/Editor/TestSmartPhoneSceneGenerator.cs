using System.Collections.Generic;
using PracticeAnything.SmartPhone;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace PracticeAnything.Editor.SmartPhone
{
    public static class TestSmartPhoneSceneGenerator
    {
        private const string ScenePath = "Assets/_Game/Content/Scenes/TestSmartPhone.unity";

        [MenuItem("Tools/PracticeAnything/Generate Test Smart Phone Scene")]
        public static void Generate()
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            Camera camera = CreateCamera();
            Canvas canvas = CreateCanvas(camera);
            EnsureEventSystem();

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
            {
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            }

            RectTransform phone = CreateRect("SmartPhone", canvas.transform, new Vector2(420, 760), Vector2.zero);
            Image phoneFrame = phone.gameObject.AddComponent<Image>();
            phoneFrame.color = new Color(0.035f, 0.039f, 0.055f, 1f);

            RectTransform screen = CreateRect("Screen", phone, new Vector2(380, 690), new Vector2(0, 5));
            Image screenImage = screen.gameObject.AddComponent<Image>();
            screenImage.color = new Color(0.08f, 0.095f, 0.14f, 1f);

            RectTransform topBar = CreateRect("TopBar", screen, new Vector2(380, 72), new Vector2(0, 309));
            Image topBarImage = topBar.gameObject.AddComponent<Image>();
            topBarImage.color = new Color(0.02f, 0.024f, 0.035f, 1f);

            Text titleText = CreateText("Title", topBar, "Home", font, 24, TextAnchor.MiddleCenter, Color.white);
            Stretch(titleText.rectTransform, new Vector2(74, 0), new Vector2(-74, 0));

            Button backButton = CreateButton("BackButton", topBar, "< Back", font, new Vector2(66, 44), new Vector2(-147, 0), new Color(0.18f, 0.22f, 0.32f, 1f));
            Button homeButton = CreateButton("HomeButton", topBar, "Home", font, new Vector2(66, 44), new Vector2(147, 0), new Color(0.18f, 0.22f, 0.32f, 1f));

            RectTransform pageRoot = CreateRect("PageRoot", screen, new Vector2(380, 556), new Vector2(0, -5));
            RectTransform statusBar = CreateRect("StatusBar", screen, new Vector2(380, 44), new Vector2(0, -323));
            Image statusImage = statusBar.gameObject.AddComponent<Image>();
            statusImage.color = new Color(0.02f, 0.024f, 0.035f, 1f);
            Text stackText = CreateText("StackHint", statusBar, "Stack depth: 1 | Esc/Backspace = Back", font, 14, TextAnchor.MiddleCenter, new Color(0.8f, 0.86f, 1f, 1f));
            Stretch(stackText.rectTransform, Vector2.zero, Vector2.zero);

            SmartPhoneNavigation navigation = phone.gameObject.AddComponent<SmartPhoneNavigation>();
            List<SmartPhonePage> pages = new();

            SmartPhonePage home = CreatePage(pageRoot, "HomePage", "home", "Home", new Color(0.11f, 0.13f, 0.2f, 1f));
            pages.Add(home);
            CreateHomeContent(home.transform, navigation, font);

            SmartPhonePage messages = CreatePage(pageRoot, "MessagesPage", "messages", "Messages", new Color(0.07f, 0.15f, 0.13f, 1f));
            pages.Add(messages);
            CreateListPage(messages.transform, navigation, font, "Messages", new[]
            {
                ("Alex", "Lunch plan and quest clue", "message-detail"),
                ("Guild", "Tonight's dungeon briefing", "message-detail"),
                ("System", "Welcome reward received", "message-detail")
            });

            SmartPhonePage messageDetail = CreatePage(pageRoot, "MessageDetailPage", "message-detail", "Chat Detail", new Color(0.06f, 0.12f, 0.18f, 1f));
            pages.Add(messageDetail);
            CreateDetailPage(messageDetail.transform, navigation, font, "Alex", "Can you check the map app after this? I marked a new location.", "Open Attachment", "message-attachment");

            SmartPhonePage attachment = CreatePage(pageRoot, "MessageAttachmentPage", "message-attachment", "Attachment", new Color(0.12f, 0.1f, 0.17f, 1f));
            pages.Add(attachment);
            CreateSimplePage(attachment.transform, font, "Attachment Preview", "This is a third-level page. Press Back twice to return to Messages, or Home to reset.");

            SmartPhonePage mail = CreatePage(pageRoot, "MailPage", "mail", "Mail", new Color(0.17f, 0.12f, 0.07f, 1f));
            pages.Add(mail);
            CreateListPage(mail.transform, navigation, font, "Inbox", new[]
            {
                ("Quest Board", "New request available", "mail-detail"),
                ("Shop", "Receipt #2048", "mail-detail"),
                ("Arena", "Weekly ranking", "mail-detail")
            });

            SmartPhonePage mailDetail = CreatePage(pageRoot, "MailDetailPage", "mail-detail", "Mail Detail", new Color(0.19f, 0.14f, 0.08f, 1f));
            pages.Add(mailDetail);
            CreateSimplePage(mailDetail.transform, font, "Quest Board", "Nested pages are app-independent. The same Back button unwinds one page at a time.");

            SmartPhonePage map = CreatePage(pageRoot, "MapPage", "map", "Map", new Color(0.08f, 0.15f, 0.21f, 1f));
            pages.Add(map);
            CreateDetailPage(map.transform, navigation, font, "City Map", "Tap the marker to open a location detail page.", "Open Marker", "map-marker");

            SmartPhonePage marker = CreatePage(pageRoot, "MapMarkerPage", "map-marker", "Marker Detail", new Color(0.07f, 0.19f, 0.16f, 1f));
            pages.Add(marker);
            CreateSimplePage(marker.transform, font, "Old Tower", "Marker detail page inside the Map app.");

            SmartPhonePage settings = CreatePage(pageRoot, "SettingsPage", "settings", "Settings", new Color(0.13f, 0.13f, 0.15f, 1f));
            pages.Add(settings);
            CreateDetailPage(settings.transform, navigation, font, "Settings", "This app has its own child page too.", "Open Controls", "controls");

            SmartPhonePage controls = CreatePage(pageRoot, "ControlsPage", "controls", "Controls", new Color(0.15f, 0.15f, 0.19f, 1f));
            pages.Add(controls);
            CreateSimplePage(controls.transform, font, "Controls", "Keyboard Backspace and Escape also call SmartPhoneNavigation.Back().");

            SetNavigationReferences(navigation, home, pages.ToArray(), backButton, titleText, stackText);
            UnityEventTools.AddPersistentListener(backButton.onClick, navigation.Back);
            UnityEventTools.AddPersistentListener(homeButton.onClick, navigation.GoHome);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log($"Generated {ScenePath}");
        }

        public static void Verify()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            SmartPhoneNavigation navigation = Object.FindFirstObjectByType<SmartPhoneNavigation>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();

            if (!scene.IsValid() || navigation == null || canvas == null)
            {
                throw new System.InvalidOperationException($"{ScenePath} is missing the required smart phone UI objects.");
            }

            SerializedObject serializedObject = new(navigation);
            SerializedProperty pagesProperty = serializedObject.FindProperty("pages");
            SerializedProperty homeProperty = serializedObject.FindProperty("homePage");

            if (homeProperty.objectReferenceValue == null || pagesProperty.arraySize < 9)
            {
                throw new System.InvalidOperationException($"{ScenePath} has incomplete navigation references.");
            }

            Debug.Log($"Verified {ScenePath} with {pagesProperty.arraySize} smart phone pages.");
        }

        private static Camera CreateCamera()
        {
            GameObject cameraObject = new("Main Camera");
            cameraObject.tag = "MainCamera";
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.16f, 0.24f, 1f);
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            cameraObject.AddComponent<AudioListener>();
            return camera;
        }

        private static Canvas CreateCanvas(Camera camera)
        {
            GameObject canvasObject = new("Canvas");
            Canvas canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
            _ = camera;
            return canvas;
        }

        private static void EnsureEventSystem()
        {
            GameObject eventSystemObject = new("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            eventSystemObject.AddComponent<InputSystemUIInputModule>();
            eventSystemObject.AddComponent<SmartPhoneInputSystemEventSystemFixer>();
        }

        private static SmartPhonePage CreatePage(Transform parent, string name, string id, string title, Color color)
        {
            RectTransform rect = CreateRect(name, parent, new Vector2(380, 556), Vector2.zero);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            SmartPhonePage page = rect.gameObject.AddComponent<SmartPhonePage>();
            page.Configure(id, title);
            rect.gameObject.SetActive(false);
            return page;
        }

        private static void CreateHomeContent(Transform parent, SmartPhoneNavigation navigation, Font font)
        {
            Text heading = CreateText("Heading", parent, "Smart Phone Demo", font, 30, TextAnchor.MiddleCenter, Color.white);
            SetRect(heading.rectTransform, new Vector2(340, 60), new Vector2(0, 210));

            CreateText("Hint", parent, "Open apps, drill into pages, then use Back to unwind the stack.", font, 16, TextAnchor.MiddleCenter, new Color(0.8f, 0.86f, 1f, 1f));
            parent.Find("Hint").GetComponent<RectTransform>().sizeDelta = new Vector2(330, 56);
            parent.Find("Hint").GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 160);

            CreateAppButton(parent, navigation, font, "Messages", "messages", new Vector2(-95, 60), new Color(0.1f, 0.58f, 0.43f, 1f));
            CreateAppButton(parent, navigation, font, "Mail", "mail", new Vector2(95, 60), new Color(0.88f, 0.45f, 0.18f, 1f));
            CreateAppButton(parent, navigation, font, "Map", "map", new Vector2(-95, -120), new Color(0.13f, 0.52f, 0.82f, 1f));
            CreateAppButton(parent, navigation, font, "Settings", "settings", new Vector2(95, -120), new Color(0.46f, 0.48f, 0.56f, 1f));
        }

        private static void CreateListPage(Transform parent, SmartPhoneNavigation navigation, Font font, string heading, (string title, string subtitle, string pageId)[] rows)
        {
            Text title = CreateText("Heading", parent, heading, font, 28, TextAnchor.MiddleLeft, Color.white);
            SetRect(title.rectTransform, new Vector2(320, 56), new Vector2(0, 220));

            for (int i = 0; i < rows.Length; i++)
            {
                Button rowButton = CreateButton($"Row{i + 1}", parent, $"{rows[i].title}\n{rows[i].subtitle}", font, new Vector2(320, 82), new Vector2(0, 115 - i * 100), new Color(1f, 1f, 1f, 0.12f));
                AddOpenPageListener(rowButton, navigation, rows[i].pageId);
            }
        }

        private static void CreateDetailPage(Transform parent, SmartPhoneNavigation navigation, Font font, string heading, string body, string buttonText, string nextPageId)
        {
            CreateSimplePage(parent, font, heading, body);
            Button button = CreateButton("OpenNextButton", parent, buttonText, font, new Vector2(260, 58), new Vector2(0, -170), new Color(0.24f, 0.38f, 0.78f, 1f));
            AddOpenPageListener(button, navigation, nextPageId);
        }

        private static void CreateSimplePage(Transform parent, Font font, string heading, string body)
        {
            Text title = CreateText("Heading", parent, heading, font, 30, TextAnchor.MiddleCenter, Color.white);
            SetRect(title.rectTransform, new Vector2(330, 72), new Vector2(0, 150));

            Text bodyText = CreateText("Body", parent, body, font, 18, TextAnchor.UpperCenter, new Color(0.86f, 0.9f, 1f, 1f));
            bodyText.horizontalOverflow = HorizontalWrapMode.Wrap;
            bodyText.verticalOverflow = VerticalWrapMode.Overflow;
            SetRect(bodyText.rectTransform, new Vector2(310, 160), new Vector2(0, 35));
        }

        private static void CreateAppButton(Transform parent, SmartPhoneNavigation navigation, Font font, string label, string pageId, Vector2 position, Color color)
        {
            Button button = CreateButton(label + "AppButton", parent, label, font, new Vector2(130, 130), position, color);
            AddOpenPageListener(button, navigation, pageId);
        }

        private static Button CreateButton(string name, Transform parent, string label, Font font, Vector2 size, Vector2 position, Color color)
        {
            RectTransform rect = CreateRect(name, parent, size, position);
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;

            Text text = CreateText("Text", rect, label, font, 18, TextAnchor.MiddleCenter, Color.white);
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            Stretch(text.rectTransform, new Vector2(8, 6), new Vector2(-8, -6));
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
            SetRect(rect, size, position);
            return rect;
        }

        private static void SetRect(RectTransform rect, Vector2 size, Vector2 position)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
        }

        private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        private static void AddOpenPageListener(Button button, SmartPhoneNavigation navigation, string pageId)
        {
            UnityEventTools.AddStringPersistentListener(button.onClick, navigation.OpenPageById, pageId);
        }

        private static void SetNavigationReferences(SmartPhoneNavigation navigation, SmartPhonePage homePage, SmartPhonePage[] pages, Button backButton, Text titleText, Text stackText)
        {
            SerializedObject serializedObject = new(navigation);
            serializedObject.FindProperty("homePage").objectReferenceValue = homePage;
            serializedObject.FindProperty("backButton").objectReferenceValue = backButton;
            serializedObject.FindProperty("titleText").objectReferenceValue = titleText;
            serializedObject.FindProperty("stackText").objectReferenceValue = stackText;

            SerializedProperty pagesProperty = serializedObject.FindProperty("pages");
            pagesProperty.arraySize = pages.Length;
            for (int i = 0; i < pages.Length; i++)
            {
                pagesProperty.GetArrayElementAtIndex(i).objectReferenceValue = pages[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
