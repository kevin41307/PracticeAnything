using UnityEngine;

namespace PracticeAnything.SmartPhone
{
    public sealed class SmartPhonePage : MonoBehaviour
    {
        [SerializeField] private string pageId;
        [SerializeField] private string displayName;

        public string PageId => pageId;
        public string DisplayName => string.IsNullOrWhiteSpace(displayName) ? name : displayName;

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }

        public void Configure(string id, string title)
        {
            pageId = id;
            displayName = title;
        }
    }
}
