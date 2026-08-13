using TMPro;
using Train.Architecture.Bootstrap;
using Train.Gameplay.Combat.Buffs;
using Train.Gameplay.Player.Input;
using Train.Presentation.UI.Views;
using UnityEngine;
using UnityEngine.UI;

namespace Train.Composition
{
    /// <summary>战斗右下角 Buff 栏和 I 键详情面板。</summary>
    [DefaultExecutionOrder(-7180)]
    [DisallowMultipleComponent]
    public sealed class BuffBarRuntimeController : MonoBehaviour
    {
        private BuffHandleComponent _source;
        private PlayerInputReader _input;
        private GameObject _bar;
        private GameObject _detail;
        private Transform _barContent;
        private Transform _detailContent;
        private bool _detailOpen;

        private void Awake() => DontDestroyOnLoad(gameObject);

        private void Update()
        {
            BindSource();
            if (_source != null) Render(_source.Snapshot.Buffs);
        }

        private void BindSource()
        {
            var source = FindFirstObjectByType<PlayerInputReader>()?.GetComponent<BuffHandleComponent>();
            var input = source != null ? source.GetComponent<PlayerInputReader>() : null;
            if (input != _input)
            {
                if (_input != null) _input.BuffPanelPerformed -= ToggleDetail;
                _input = input;
                if (_input != null) _input.BuffPanelPerformed += ToggleDetail;
            }
            if (source == _source) return;
            _source = source;
            if (_bar == null && FindFirstObjectByType<GameUIRootView>() is { Canvas: not null } root)
            {
                Build(root.Canvas.transform);
            }
        }

        private void ToggleDetail() => SetDetailVisible(!_detailOpen);

        private void Build(Transform canvas)
        {
            _bar = CreatePanel(canvas, "BuffBar", new Vector2(1f, 0f), new Vector2(-32f, 34f), new Vector2(420f, 74f));
            _barContent = CreateContent(_bar.transform, false);
            _detail = CreatePanel(canvas, "BuffDetails", new Vector2(.5f, .5f), Vector2.zero, new Vector2(900f, 560f));
            _detailContent = CreateContent(_detail.transform, true);
            SetDetailVisible(false);
        }

        private void Render(System.Collections.Generic.IReadOnlyList<Train.Buffs.Core.BuffSnapshot> buffs)
        {
            if (_barContent == null) return;
            RenderCards(_barContent, buffs, false);
            if (_detailOpen) RenderCards(_detailContent, buffs, true);
        }

        private static void RenderCards(Transform content, System.Collections.Generic.IReadOnlyList<Train.Buffs.Core.BuffSnapshot> buffs, bool detail)
        {
            for (var i = content.childCount - 1; i >= 0; i--) Destroy(content.GetChild(i).gameObject);
            for (var i = 0; i < buffs.Count; i++)
            {
                var buff = buffs[i];
                var card = new GameObject("BuffCard", typeof(RectTransform), typeof(Image));
                card.transform.SetParent(content, false);
                var image = card.GetComponent<Image>();
                image.color = BuffColor(buff.BuffId);
                var text = card.AddComponent<TextMeshProUGUI>();
                text.alignment = TextAlignmentOptions.Center;
                text.fontSize = detail ? 20f : 12f;
                text.text = $"{BuffName(buff.BuffId)}\n{buff.StackCount}/{buff.MaxStacks}\n{buff.RemainingDuration:0.0}s";
                text.color = Color.white;
                text.raycastTarget = false;
                var rect = card.GetComponent<RectTransform>();
                rect.sizeDelta = detail ? new Vector2(150f, 150f) : new Vector2(66f, 66f);
            }
        }

        private void SetDetailVisible(bool visible)
        {
            _detailOpen = visible;
            if (_detail != null) _detail.SetActive(visible);
            var look = FindFirstObjectByType<Train.Gameplay.Camera.CinemachineLookInputAdapter>();
            look?.SetExternalLookBlocked(visible);
            if (visible)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Application.isPlaying)
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchor, Vector2 position, Vector2 size)
        {
            var panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            var rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = anchor;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
            panel.GetComponent<Image>().color = new Color32(8, 18, 31, 235);
            return panel;
        }

        private static Transform CreateContent(Transform parent, bool detail)
        {
            var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
            viewport.transform.SetParent(parent, false);
            var rect = viewport.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(12f, 12f); rect.offsetMax = new Vector2(-12f, -12f);
            var content = new GameObject("Content", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            var layout = content.GetComponent<HorizontalLayoutGroup>();
            layout.spacing = 8f; layout.childForceExpandHeight = false; layout.childForceExpandWidth = false;
            content.GetComponent<ContentSizeFitter>().horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            return content.transform;
        }

        private static string BuffName(string id) => id switch
        {
            "electric_charge" => "电荷",
            "lightning_vulnerability" => "雷易伤",
            "burning" => "燃烧",
            "wet" => "潮湿",
            "poison" => "中毒",
            "bleed" => "流血",
            _ => id
        };

        private static Color BuffColor(string id) => id == "electric_charge"
            ? new Color32(116, 40, 190, 255)
            : new Color32(36, 86, 112, 255);
    }
}
