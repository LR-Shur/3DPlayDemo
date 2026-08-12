using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Train.Presentation.UI.Views
{
    /// <summary>战斗 HUD 左下角的四个主动道具快捷槽。</summary>
    [DisallowMultipleComponent]
    public sealed class ActiveItemQuickBarView : MonoBehaviour
    {
        private readonly TMP_Text[] _labels = new TMP_Text[4];
        private readonly TMP_Text[] _quantities = new TMP_Text[4];
        private readonly TMP_Text[] _cooldowns = new TMP_Text[4];
        private readonly Image[] _slotImages = new Image[4];

        private void Awake()
        {
            Build();
        }

        /// <summary>刷新四个快捷槽的名称、数量和快捷键提示。</summary>
        public void Render(string[] itemNames, int[] quantities, float[] cooldowns = null)
        {
            Build();
            for (var index = 0; index < 4; index++)
            {
                var itemName = itemNames != null && index < itemNames.Length
                    ? itemNames[index]
                    : string.Empty;
                var quantity = quantities != null && index < quantities.Length
                    ? Mathf.Max(0, quantities[index])
                    : 0;
                _labels[index].text = $"{index + 1}\n" +
                    (string.IsNullOrWhiteSpace(itemName) ? "空" : itemName);
                _quantities[index].text = quantity > 0 ? $"×{quantity}" : "0";
                var cooldown = cooldowns != null && index < cooldowns.Length
                    ? Mathf.Max(0f, cooldowns[index])
                    : 0f;
                _cooldowns[index].text = cooldown > .05f ? $"{cooldown:0.0}s" : string.Empty;
                _cooldowns[index].color = new Color32(120, 225, 255, 255);
                _quantities[index].color = quantity > 0
                    ? new Color32(220, 235, 242, 255)
                    : new Color32(120, 135, 150, 220);
                _slotImages[index].color = cooldown > .05f
                    ? new Color32(20, 45, 66, 245)
                    : quantity > 0
                        ? new Color32(12, 48, 62, 245)
                        : new Color32(10, 26, 43, 235);
            }
        }

        private void Build()
        {
            if (_labels[0] != null)
            {
                return;
            }

            var rect = gameObject.GetComponent<RectTransform>() ??
                       gameObject.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(48f, 48f);
            rect.sizeDelta = new Vector2(520f, 92f);
            var layout = gameObject.GetComponent<HorizontalLayoutGroup>() ??
                         gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.spacing = 10f;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            for (var index = 0; index < 4; index++)
            {
                var slot = new GameObject(
                    $"ActiveItemSlot{index + 1}",
                    typeof(RectTransform),
                    typeof(Image));
                slot.transform.SetParent(transform, false);
                _slotImages[index] = slot.GetComponent<Image>();
                _slotImages[index].color = new Color32(10, 26, 43, 235);
                var label = CreateText(slot.transform, "Label");
                label.alignment = TextAlignmentOptions.Center;
                label.fontSize = 15f;
                label.color = new Color32(244, 247, 251, 255);
                var quantity = CreateText(slot.transform, "Quantity");
                quantity.alignment = TextAlignmentOptions.BottomRight;
                quantity.fontSize = 14f;
                quantity.color = new Color32(220, 235, 242, 255);
                var cooldown = CreateText(slot.transform, "Cooldown");
                cooldown.alignment = TextAlignmentOptions.BottomLeft;
                cooldown.fontSize = 13f;
                cooldown.color = new Color32(120, 225, 255, 255);
                _labels[index] = label;
                _quantities[index] = quantity;
                _cooldowns[index] = cooldown;
            }
        }

        private static TMP_Text CreateText(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            child.transform.SetParent(parent, false);
            var rect = child.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(4f, 4f);
            rect.offsetMax = new Vector2(-4f, -4f);
            var text = child.GetComponent<TextMeshProUGUI>();
            text.raycastTarget = false;
            return text;
        }
    }
}
