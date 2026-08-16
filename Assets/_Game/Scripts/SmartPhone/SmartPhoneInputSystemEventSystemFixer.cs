using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace PracticeAnything.SmartPhone
{
    [DefaultExecutionOrder(-10000)]
    public sealed class SmartPhoneInputSystemEventSystemFixer : MonoBehaviour
    {
        private void Awake()
        {
            EnsureInputSystemModule();
        }

        private void EnsureInputSystemModule()
        {
            if (GetComponent<InputSystemUIInputModule>() == null)
            {
                gameObject.AddComponent<InputSystemUIInputModule>();
            }

            BaseInputModule[] inputModules = GetComponents<BaseInputModule>();
            foreach (BaseInputModule inputModule in inputModules)
            {
                if (inputModule is InputSystemUIInputModule)
                {
                    continue;
                }

                inputModule.enabled = false;
                Destroy(inputModule);
            }
        }
    }
}
