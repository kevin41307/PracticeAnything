using System.Collections.Generic;
using UnityEngine.InputSystem;
using UnityEngine;
using UnityEngine.UI;

namespace PracticeAnything.SmartPhone
{
    public sealed class SmartPhoneNavigation : MonoBehaviour
    {
        [SerializeField] private SmartPhonePage homePage;
        [SerializeField] private SmartPhonePage[] pages;
        [SerializeField] private Button backButton;
        [SerializeField] private Text titleText;
        [SerializeField] private Text stackText;

        private readonly Stack<SmartPhonePage> pageStack = new();
        private readonly Dictionary<string, SmartPhonePage> pagesById = new();

        private void Awake()
        {
            pagesById.Clear();

            foreach (SmartPhonePage page in pages)
            {
                if (page == null)
                {
                    continue;
                }

                page.SetVisible(false);

                if (!string.IsNullOrWhiteSpace(page.PageId))
                {
                    pagesById[page.PageId] = page;
                }
            }

            if (homePage != null)
            {
                OpenRoot(homePage);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame))
            {
                Back();
            }
        }

        public void OpenRoot(SmartPhonePage page)
        {
            if (page == null)
            {
                return;
            }

            foreach (SmartPhonePage knownPage in pages)
            {
                if (knownPage != null)
                {
                    knownPage.SetVisible(false);
                }
            }

            pageStack.Clear();
            page.SetVisible(true);
            pageStack.Push(page);
            RefreshChrome();
        }

        public void OpenPage(SmartPhonePage page)
        {
            if (page == null)
            {
                return;
            }

            if (pageStack.Count > 0)
            {
                pageStack.Peek().SetVisible(false);
            }

            page.SetVisible(true);
            pageStack.Push(page);
            RefreshChrome();
        }

        public void OpenPageById(string pageId)
        {
            if (pagesById.TryGetValue(pageId, out SmartPhonePage page))
            {
                OpenPage(page);
            }
            else
            {
                Debug.LogWarning($"Smart phone page not found: {pageId}", this);
            }
        }

        public void Back()
        {
            if (pageStack.Count <= 1)
            {
                RefreshChrome();
                return;
            }

            SmartPhonePage current = pageStack.Pop();
            current.SetVisible(false);

            SmartPhonePage previous = pageStack.Peek();
            previous.SetVisible(true);
            RefreshChrome();
        }

        public void GoHome()
        {
            OpenRoot(homePage);
        }

        private void RefreshChrome()
        {
            SmartPhonePage currentPage = pageStack.Count > 0 ? pageStack.Peek() : null;

            if (titleText != null)
            {
                titleText.text = currentPage == null ? string.Empty : currentPage.DisplayName;
            }

            if (backButton != null)
            {
                backButton.interactable = pageStack.Count > 1;
            }

            if (stackText != null)
            {
                stackText.text = $"Stack depth: {pageStack.Count} | Esc/Backspace = Back";
            }
        }
    }
}
