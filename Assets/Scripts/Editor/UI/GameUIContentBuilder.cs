#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using Train.Presentation.UI.Data;
using Train.Presentation.UI.Views;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Train.EditorTools.UI
{
    /// <summary>
    /// 以可重复执行的方式生成项目统一字体、主题和商业化游戏 UI 预制体。
    /// </summary>
    public static class GameUIContentBuilder
    {
        private const string FontSourcePath =
            "Assets/Arts/UI/Fonts/NotoSansCJKsc-Regular.otf";
        private const string FontAssetPath =
            "Assets/Arts/UI/Fonts/NotoSansCJKsc-Regular SDF.asset";
        private const string ThemeFolder = "Assets/Data/UI";
        private const string ThemePath =
            ThemeFolder + "/DefaultUITheme.asset";
        private const string PrefabFolder = "Assets/Prefabs/UI/Core";
        private const string PrefabPath =
            PrefabFolder + "/GameUIRoot.prefab";
        private const string OnlineArtRoot =
            "Assets/Arts/UI/KenneySciFi/PNG";
        private const string ButtonSpritePath =
            OnlineArtRoot +
            "/Blue/Default/button_square_header_notch_rectangle.png";
        private const string SlotSpritePath =
            OnlineArtRoot +
            "/Blue/Default/button_square_header_notch_square.png";
        private const string CrosshairSpritePath =
            OnlineArtRoot + "/Blue/Default/crosshair_color_a.png";

        private static Sprite _buttonSprite;
        private static Sprite _slotSprite;
        private static Sprite _crosshairSprite;

        private static readonly Color Background =
            new Color32(11, 17, 32, 246);
        private static readonly Color Panel =
            new Color32(17, 28, 44, 232);
        private static readonly Color Raised =
            new Color32(23, 40, 61, 250);
        private static readonly Color Outline =
            new Color32(50, 70, 92, 255);
        private static readonly Color Cyan =
            new Color32(88, 224, 231, 255);
        private static readonly Color Gold =
            new Color32(242, 201, 107, 255);
        private static readonly Color Primary =
            new Color32(244, 247, 251, 255);
        private static readonly Color Secondary =
            new Color32(170, 184, 200, 255);
        private static readonly Color Muted =
            new Color32(112, 132, 154, 255);

        [MenuItem("Tools/Train/Content/Build Game UI")]
        public static void Build()
        {
            EnsureFolder(ThemeFolder);
            EnsureFolder(PrefabFolder);
            EnsureTmpEssentialResources();

            var font = CreateOrLoadFontAsset();
            _buttonSprite = LoadOnlineSprite(
                ButtonSpritePath,
                new Vector4(24f, 20f, 24f, 20f));
            _slotSprite = LoadOnlineSprite(
                SlotSpritePath,
                new Vector4(18f, 18f, 18f, 18f));
            _crosshairSprite = LoadOnlineSprite(
                CrosshairSpritePath,
                Vector4.zero);
            CreateOrUpdateTheme();
            CreateOrUpdatePrefab(font);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                $"Game UI built: '{PrefabPath}'. " +
                "Canvas 1920x1080, HUD, inventory, equipment, navigation " +
                "and EventSystem ready.");
        }

        private static Sprite LoadOnlineSprite(
            string assetPath,
            Vector4 border)
        {
            var importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
            {
                throw new InvalidOperationException(
                    $"Online UI art is missing or not a texture: {assetPath}");
            }

            var needsReimport =
                importer.textureType != TextureImporterType.Sprite ||
                importer.spriteImportMode != SpriteImportMode.Single ||
                importer.spriteBorder != border ||
                importer.mipmapEnabled;
            if (needsReimport)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100f;
                importer.spriteBorder = border;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.SaveAndReimport();
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                throw new InvalidOperationException(
                    $"Failed to import online UI sprite: {assetPath}");
            }

            return sprite;
        }

        private static void EnsureTmpEssentialResources()
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null)
            {
                return;
            }

            var package =
                UnityEditor.PackageManager.PackageInfo.FindForAssembly(
                    typeof(TMP_Text).Assembly);
            var packagePath = package != null
                ? package.resolvedPath
                : null;
            var essentialsPath = string.IsNullOrWhiteSpace(packagePath)
                ? null
                : Path.Combine(
                    packagePath,
                    "Package Resources",
                    "TMP Essential Resources.unitypackage");
            if (string.IsNullOrWhiteSpace(essentialsPath) ||
                !File.Exists(essentialsPath))
            {
                throw new InvalidOperationException(
                    "TextMesh Pro Essential Resources are missing and their " +
                    "package archive could not be located.");
            }

            AssetDatabase.ImportPackage(essentialsPath, false);
            AssetDatabase.Refresh();
            if (Resources.Load<TMP_Settings>("TMP Settings") == null)
            {
                throw new InvalidOperationException(
                    "TMP Essential Resources import completed without creating " +
                    "Assets/TextMesh Pro/Resources/TMP Settings.asset.");
            }
        }

        private static TMP_FontAsset CreateOrLoadFontAsset()
        {
            var existing =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (existing != null)
            {
                AssignDefaultFont(existing);
                return existing;
            }

            AssetDatabase.ImportAsset(
                FontSourcePath,
                ImportAssetOptions.ForceSynchronousImport);
            var sourceFont =
                AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            if (sourceFont == null)
            {
                throw new InvalidOperationException(
                    $"Chinese font source is missing: {FontSourcePath}");
            }

            var fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                72,
                9,
                GlyphRenderMode.SDFAA,
                2048,
                2048,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null)
            {
                throw new InvalidOperationException(
                    $"Failed to create a TMP font asset from {FontSourcePath}.");
            }

            fontAsset.name = "NotoSansCJKsc-Regular SDF";
            var atlas = fontAsset.atlasTexture;
            var material = fontAsset.material;
            atlas.name = fontAsset.name + " Atlas";
            material.name = fontAsset.name + " Material";
            atlas.hideFlags = HideFlags.None;
            material.hideFlags = HideFlags.None;

            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
            AssetDatabase.AddObjectToAsset(atlas, fontAsset);
            AssetDatabase.AddObjectToAsset(material, fontAsset);
            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();

            AssignDefaultFont(fontAsset);
            return fontAsset;
        }

        private static void AssignDefaultFont(TMP_FontAsset fontAsset)
        {
            var settings = Resources.Load<TMP_Settings>("TMP Settings");
            if (settings == null)
            {
                return;
            }

            TMP_Settings.defaultFontAsset = fontAsset;
            EditorUtility.SetDirty(settings);
        }

        private static void CreateOrUpdateTheme()
        {
            var theme = AssetDatabase.LoadAssetAtPath<UITheme>(ThemePath);
            if (theme == null)
            {
                theme = ScriptableObject.CreateInstance<UITheme>();
                AssetDatabase.CreateAsset(theme, ThemePath);
            }

            EditorUtility.SetDirty(theme);
        }

        private static void CreateOrUpdatePrefab(TMP_FontAsset font)
        {
            var root = new GameObject(
                "[GameUIRoot]",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster),
                typeof(GameUIRootView),
                typeof(UIHotkeyDriver));
            try
            {
                var rootRect = root.GetComponent<RectTransform>();
                SetStretch(rootRect);

                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                canvas.additionalShaderChannels =
                    AdditionalCanvasShaderChannels.TexCoord1 |
                    AdditionalCanvasShaderChannels.Normal |
                    AdditionalCanvasShaderChannels.Tangent;

                var scaler = root.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode =
                    CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                CreateEventSystem(root.transform);
                var hud = CreateHud(
                    rootRect,
                    font,
                    out var inventoryButton,
                    out var equipmentButton);
                var inventory = CreateInventory(rootRect, font);
                var equipment = CreateEquipment(rootRect, font);
                var quests = CreateQuests(rootRect, font);
                var characters = CreateCharacters(rootRect, font);
                var dialogue = CreateDialogue(rootRect, font);
                var placeholder = CreateMenuPlaceholder(rootRect, font);
                CreateMenuNavigation(
                    rootRect,
                    font,
                    out var menuNavigationRoot,
                    out var inventoryTabButton,
                    out var equipmentTabButton,
                    out var questTabButton,
                    out var charactersTabButton,
                    out var archiveTabButton,
                    out var menuCloseButton);

                root.GetComponent<GameUIRootView>().Configure(
                    canvas,
                    hud,
                    inventory,
                    equipment,
                    quests,
                    characters,
                    dialogue,
                    placeholder,
                    inventoryButton,
                    equipmentButton,
                    menuNavigationRoot,
                    inventoryTabButton,
                    equipmentTabButton,
                    questTabButton,
                    charactersTabButton,
                    archiveTabButton,
                    menuCloseButton);

                inventory.SetVisible(false);
                equipment.SetVisible(false);
                quests.SetVisible(false);
                characters.SetVisible(false);
                dialogue.SetVisible(false);
                placeholder.Hide();
                menuNavigationRoot.SetActive(false);

                var saved =
                    PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                var savedView =
                    saved != null
                        ? saved.GetComponent<GameUIRootView>()
                        : null;
                if (savedView == null || !savedView.IsValid)
                {
                    throw new InvalidOperationException(
                        "Generated GameUIRoot prefab is missing required " +
                        "runtime view references.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateEventSystem(Transform parent)
        {
            var eventSystemObject = new GameObject(
                "EventSystem",
                typeof(EventSystem),
                typeof(InputSystemUIInputModule));
            eventSystemObject.transform.SetParent(parent, false);
            var module =
                eventSystemObject.GetComponent<InputSystemUIInputModule>();
            module.AssignDefaultActions();
        }

        private static HudView CreateHud(
            RectTransform parent,
            TMP_FontAsset font,
            out Button inventoryButton,
            out Button equipmentButton)
        {
            var hudObject = CreateRect("HUD", parent);
            SetStretch(hudObject);
            var hud = hudObject.gameObject.AddComponent<HudView>();

            var playerPanel = CreateAnchored(
                "PlayerStatus",
                hudObject,
                new Vector2(0f, 1f),
                new Vector2(460f, 118f),
                new Vector2(48f, -48f),
                new Vector2(0f, 1f));
            AddImage(playerPanel.gameObject, Panel, false);
            CreateAccentBars(playerPanel);
            CreateText(
                "Agent",
                playerPanel,
                "ELLEN // LV.01",
                font,
                21f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(24f, -18f),
                new Vector2(330f, 30f),
                new Vector2(0f, 1f));
            CreateText(
                "HpLabel",
                playerPanel,
                "HP",
                font,
                16f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(24f, -57f),
                new Vector2(48f, 24f),
                new Vector2(0f, 1f));

            var barBackground = CreateAnchored(
                "HealthBar",
                playerPanel,
                new Vector2(0f, 1f),
                new Vector2(322f, 12f),
                new Vector2(78f, -64f),
                new Vector2(0f, 1f));
            AddImage(
                barBackground.gameObject,
                new Color32(8, 13, 24, 230),
                false);
            var healthFillRect = CreateRect("Fill", barBackground);
            SetStretch(healthFillRect, 2f);
            var healthFill = AddImage(
                healthFillRect.gameObject,
                Cyan,
                false);
            healthFill.type = Image.Type.Filled;
            healthFill.fillMethod = Image.FillMethod.Horizontal;
            healthFill.fillOrigin = 0;
            healthFill.fillAmount = 1f;

            var healthValue = CreateText(
                "HealthValue",
                playerPanel,
                "HP 100 / 100",
                font,
                18f,
                Secondary,
                TextAlignmentOptions.Right,
                new Vector2(-24f, -88f),
                new Vector2(220f, 24f),
                new Vector2(1f, 1f));

            var objectivePanel = CreateAnchored(
                "Objective",
                hudObject,
                new Vector2(0.5f, 1f),
                new Vector2(580f, 92f),
                new Vector2(0f, -48f),
                new Vector2(0.5f, 1f));
            AddImage(objectivePanel.gameObject, Panel, false);
            var objectiveAccent = CreateAnchored(
                "Accent",
                objectivePanel,
                new Vector2(0.5f, 1f),
                new Vector2(120f, 4f),
                Vector2.zero,
                new Vector2(0.5f, 1f));
            AddImage(objectiveAccent.gameObject, Gold, false);
            var levelName = CreateText(
                "LevelName",
                objectivePanel,
                "第1训练区",
                font,
                24f,
                Primary,
                TextAlignmentOptions.Center,
                new Vector2(0f, -14f),
                new Vector2(380f, 34f),
                new Vector2(0.5f, 1f));
            var phase = CreateText(
                "Phase",
                objectivePanel,
                "STANDBY",
                font,
                14f,
                Cyan,
                TextAlignmentOptions.Center,
                new Vector2(0f, -45f),
                new Vector2(220f, 24f),
                new Vector2(0.5f, 1f));
            var objective = CreateText(
                "ObjectiveText",
                objectivePanel,
                "击败敌人  0/3",
                font,
                17f,
                Secondary,
                TextAlignmentOptions.Center,
                new Vector2(0f, -70f),
                new Vector2(360f, 26f),
                new Vector2(0.5f, 1f));

            inventoryButton = CreateButton(
                "InventoryButton",
                hudObject,
                "仓库  [B]",
                font,
                new Vector2(1f, 1f),
                new Vector2(188f, 58f),
                new Vector2(-48f, -48f),
                Cyan);

            equipmentButton = CreateButton(
                "EquipmentButton",
                hudObject,
                "装备  [C]",
                font,
                new Vector2(1f, 1f),
                new Vector2(188f, 58f),
                new Vector2(-48f, -116f),
                Gold);

            var tracker = CreateAnchored(
                "QuestTracker",
                hudObject,
                new Vector2(1f, 1f),
                new Vector2(390f, 104f),
                new Vector2(-48f, -194f),
                new Vector2(1f, 1f));
            AddImage(tracker.gameObject, Panel, false);
            CreateText(
                "QuestLabel",
                tracker,
                "当前委托",
                font,
                13f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(20f, -16f),
                new Vector2(300f, 22f),
                new Vector2(0f, 1f));
            CreateText(
                "QuestPlaceholder",
                tracker,
                "训练委托 // 清除威胁",
                font,
                20f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(20f, -48f),
                new Vector2(340f, 30f),
                new Vector2(0f, 1f));

            var bannerRect = CreateAnchored(
                "Announcement",
                hudObject,
                new Vector2(0.5f, 0.5f),
                new Vector2(760f, 164f),
                new Vector2(0f, 180f),
                new Vector2(0.5f, 0.5f));
            AddImage(bannerRect.gameObject, Background, false);
            var bannerGroup =
                bannerRect.gameObject.AddComponent<CanvasGroup>();
            bannerGroup.alpha = 0f;
            var bannerAccentRect = CreateAnchored(
                "BannerAccent",
                bannerRect,
                new Vector2(0f, 0.5f),
                new Vector2(8f, 116f),
                new Vector2(0f, 0f),
                new Vector2(0f, 0.5f));
            var bannerAccent = AddImage(
                bannerAccentRect.gameObject,
                Cyan,
                false);
            CreateDecorativeDiamond(
                bannerRect,
                new Vector2(-318f, 50f),
                Gold);
            var bannerTitle = CreateText(
                "BannerTitle",
                bannerRect,
                "战斗开始",
                font,
                38f,
                Primary,
                TextAlignmentOptions.Center,
                new Vector2(0f, 18f),
                new Vector2(600f, 54f),
                new Vector2(0.5f, 0.5f));
            var bannerSubtitle = CreateText(
                "BannerSubtitle",
                bannerRect,
                "清除区域内全部敌人",
                font,
                18f,
                Secondary,
                TextAlignmentOptions.Center,
                new Vector2(0f, -35f),
                new Vector2(560f, 30f),
                new Vector2(0.5f, 0.5f));

            var interactionRect = CreateAnchored(
                "InteractionPrompt",
                hudObject,
                new Vector2(0.5f, 0f),
                new Vector2(560f, 88f),
                new Vector2(0f, 72f),
                new Vector2(0.5f, 0f));
            AddImage(interactionRect.gameObject, Background, false);
            var interactionGroup =
                interactionRect.gameObject.AddComponent<CanvasGroup>();
            interactionGroup.alpha = 0f;
            interactionGroup.blocksRaycasts = false;
            var interactionAccentRect = CreateAnchored(
                "Accent",
                interactionRect,
                new Vector2(0f, 0.5f),
                new Vector2(7f, 58f),
                Vector2.zero,
                new Vector2(0f, 0.5f));
            var interactionAccent = AddImage(
                interactionAccentRect.gameObject,
                Cyan,
                false);
            var interactionName = CreateText(
                "ItemName",
                interactionRect,
                "战术补给",
                font,
                23f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(26f, 15f),
                new Vector2(320f, 34f),
                new Vector2(0f, 0.5f));
            var interactionHint = CreateText(
                "InteractionHint",
                interactionRect,
                "[ E ]  拾取    ×1",
                font,
                17f,
                Cyan,
                TextAlignmentOptions.Right,
                new Vector2(-24f, -19f),
                new Vector2(260f, 30f),
                new Vector2(1f, 0.5f));

            var pickupToastRect = CreateAnchored(
                "PickupToast",
                hudObject,
                new Vector2(1f, 1f),
                new Vector2(390f, 88f),
                new Vector2(-48f, -320f),
                new Vector2(1f, 1f));
            AddImage(pickupToastRect.gameObject, Background, false);
            var pickupToastGroup =
                pickupToastRect.gameObject.AddComponent<CanvasGroup>();
            pickupToastGroup.alpha = 0f;
            pickupToastGroup.blocksRaycasts = false;
            var pickupToastAccentRect = CreateAnchored(
                "Accent",
                pickupToastRect,
                new Vector2(0f, 0.5f),
                new Vector2(6f, 60f),
                Vector2.zero,
                new Vector2(0f, 0.5f));
            var pickupToastAccent = AddImage(
                pickupToastAccentRect.gameObject,
                Gold,
                false);
            var pickupToastTitle = CreateText(
                "Title",
                pickupToastRect,
                "获得战术补给 ×1",
                font,
                20f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(22f, -15f),
                new Vector2(340f, 30f),
                new Vector2(0f, 1f));
            var pickupToastSubtitle = CreateText(
                "Subtitle",
                pickupToastRect,
                "已存入仓库",
                font,
                14f,
                Secondary,
                TextAlignmentOptions.Left,
                new Vector2(22f, -48f),
                new Vector2(340f, 24f),
                new Vector2(0f, 1f));

            hud.Configure(
                levelName,
                phase,
                objective,
                healthValue,
                healthFill,
                bannerGroup,
                bannerAccent,
                bannerTitle,
                bannerSubtitle,
                interactionGroup,
                interactionAccent,
                interactionName,
                interactionHint,
                pickupToastGroup,
                pickupToastAccent,
                pickupToastTitle,
                pickupToastSubtitle);
            return hud;
        }

        private static InventoryScreenView CreateInventory(
            RectTransform parent,
            TMP_FontAsset font)
        {
            var screen = CreateRect("InventoryScreen", parent);
            SetStretch(screen);
            AddImage(screen.gameObject, Background, true);
            var view =
                screen.gameObject.AddComponent<InventoryScreenView>();

            var body = CreateAnchored(
                "Body",
                screen,
                new Vector2(0.5f, 0.5f),
                new Vector2(1760f, 900f),
                new Vector2(0f, -8f),
                new Vector2(0.5f, 0.5f));
            AddImage(body.gameObject, Panel, false);
            CreateAccentBars(body);

            CreateText(
                "HeaderEnglish",
                body,
                "仓库 · 物品管理",
                font,
                16f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(48f, -38f),
                new Vector2(420f, 26f),
                new Vector2(0f, 1f));
            CreateText(
                "Header",
                body,
                "仓库",
                font,
                43f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(48f, -74f),
                new Vector2(280f, 60f),
                new Vector2(0f, 1f));
            var capacity = CreateText(
                "Capacity",
                body,
                "容量  04 / 24",
                font,
                19f,
                Secondary,
                TextAlignmentOptions.Right,
                new Vector2(-164f, -70f),
                new Vector2(260f, 34f),
                new Vector2(1f, 1f));
            var closeButton = CreateButton(
                "Close",
                body,
                "关闭  [ESC]",
                font,
                new Vector2(1f, 1f),
                new Vector2(142f, 48f),
                new Vector2(-32f, -32f),
                new Color32(255, 99, 121, 255));

            var categoryRail = CreateAnchored(
                "CategoryRail",
                body,
                new Vector2(0f, 0.5f),
                new Vector2(116f, 682f),
                new Vector2(48f, -20f),
                new Vector2(0f, 0.5f));
            AddImage(categoryRail.gameObject, Raised, false);
            var categoryLabels = new[]
            {
                "全部", "素材", "消耗", "货币"
            };
            for (var index = 0; index < categoryLabels.Length; index++)
            {
                var label = CreateAnchored(
                    $"Category_{index:00}",
                    categoryRail,
                    new Vector2(0.5f, 1f),
                    new Vector2(84f, 72f),
                    new Vector2(0f, -40f - index * 84f),
                    new Vector2(0.5f, 1f));
                AddImage(
                    label.gameObject,
                    index == 0
                        ? new Color32(29, 83, 96, 255)
                        : new Color32(31, 49, 70, 255),
                    false);
                CreateText(
                    "Text",
                    label,
                    categoryLabels[index],
                    font,
                    18f,
                    index == 0 ? Cyan : Secondary,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    new Vector2(80f, 40f),
                    new Vector2(0.5f, 0.5f));
            }

            var gridArea = CreateAnchored(
                "ItemGrid",
                body,
                new Vector2(0f, 0.5f),
                new Vector2(1000f, 682f),
                new Vector2(184f, -20f),
                new Vector2(0f, 0.5f));
            var gridBackground = AddImage(
                gridArea.gameObject,
                new Color32(8, 15, 28, 205),
                false);
            _ = gridBackground;
            var grid = gridArea.gameObject.AddComponent<GridLayoutGroup>();
            grid.padding = new RectOffset(22, 22, 26, 24);
            grid.cellSize = new Vector2(146f, 142f);
            grid.spacing = new Vector2(16f, 16f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 6;
            grid.childAlignment = TextAnchor.UpperLeft;

            var slotButtons = new Button[24];
            var slotBackgrounds = new Image[24];
            var slotAccents = new Image[24];
            var slotIcons = new Image[24];
            var slotNames = new TMP_Text[24];
            var slotQuantities = new TMP_Text[24];
            for (var index = 0; index < 24; index++)
            {
                CreateInventorySlot(
                    gridArea,
                    font,
                    index,
                    out slotButtons[index],
                    out slotBackgrounds[index],
                    out slotAccents[index],
                    out slotIcons[index],
                    out slotNames[index],
                    out slotQuantities[index]);
            }

            var detail = CreateAnchored(
                "Detail",
                body,
                new Vector2(1f, 0.5f),
                new Vector2(478f, 682f),
                new Vector2(-48f, -20f),
                new Vector2(1f, 0.5f));
            AddImage(detail.gameObject, Raised, false);
            var detailAccent = CreateAnchored(
                "TopAccent",
                detail,
                new Vector2(0.5f, 1f),
                new Vector2(430f, 5f),
                new Vector2(0f, -1f),
                new Vector2(0.5f, 1f));
            AddImage(detailAccent.gameObject, Gold, false);
            var detailCategory = CreateText(
                "Category",
                detail,
                "养成素材",
                font,
                15f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(32f, -36f),
                new Vector2(390f, 28f),
                new Vector2(0f, 1f));
            var detailName = CreateText(
                "Name",
                detail,
                "训练芯片",
                font,
                34f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(32f, -78f),
                new Vector2(390f, 48f),
                new Vector2(0f, 1f));
            var detailRarity = CreateText(
                "Rarity",
                detail,
                "普通",
                font,
                18f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(32f, -128f),
                new Vector2(180f, 30f),
                new Vector2(0f, 1f));

            var preview = CreateAnchored(
                "Preview",
                detail,
                new Vector2(0.5f, 1f),
                new Vector2(410f, 250f),
                new Vector2(0f, -286f),
                new Vector2(0.5f, 1f));
            AddImage(
                preview.gameObject,
                new Color32(12, 24, 39, 255),
                false);
            var onlineIcon = CreateAnchored(
                "OnlineCrosshair",
                preview,
                new Vector2(0.5f, 0.5f),
                new Vector2(136f, 136f),
                Vector2.zero,
                new Vector2(0.5f, 0.5f));
            var onlineIconImage = AddImage(
                onlineIcon.gameObject,
                new Color32(88, 224, 231, 118),
                false);
            onlineIconImage.sprite = _crosshairSprite;
            onlineIconImage.preserveAspect = true;
            var detailItemIcon = CreateAnchored(
                "ItemIcon",
                preview,
                new Vector2(0.5f, 0.5f),
                new Vector2(108f, 108f),
                new Vector2(0f, 12f),
                new Vector2(0.5f, 0.5f));
            var detailItemIconImage = AddImage(
                detailItemIcon.gameObject,
                Color.white,
                false);
            detailItemIconImage.preserveAspect = true;
            detailItemIconImage.raycastTarget = false;
            CreateDecorativeDiamond(preview, Vector2.zero, Cyan, 96f);
            CreateText(
                "IconLetters",
                preview,
                "数据",
                font,
                20f,
                Primary,
                TextAlignmentOptions.Center,
                Vector2.zero,
                new Vector2(120f, 36f),
                new Vector2(0.5f, 0.5f));

            var detailDescription = CreateText(
                "Description",
                detail,
                "用于基础能力训练的通用数据芯片。",
                font,
                20f,
                Secondary,
                TextAlignmentOptions.TopLeft,
                new Vector2(32f, -430f),
                new Vector2(414f, 118f),
                new Vector2(0f, 1f));
            detailDescription.textWrappingMode = TextWrappingModes.Normal;
            var detailQuantity = CreateText(
                "Quantity",
                detail,
                "持有  8    堆叠上限  99",
                font,
                18f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(32f, -578f),
                new Vector2(400f, 34f),
                new Vector2(0f, 1f));

            view.Configure(
                screen.gameObject,
                closeButton,
                capacity,
                slotButtons,
                slotBackgrounds,
                slotAccents,
                slotIcons,
                slotNames,
                slotQuantities,
                detailCategory,
                detailName,
                detailRarity,
                detailDescription,
                detailQuantity,
                detailItemIconImage);
            return view;
        }

        /// <summary>
        /// 创建包含十个穿戴槽、十八张候选卡片和属性对照的装备整备页。
        /// </summary>
        private static EquipmentScreenView CreateEquipment(
            RectTransform parent,
            TMP_FontAsset font)
        {
            var screen = CreateRect("EquipmentScreen", parent);
            SetStretch(screen);
            AddImage(screen.gameObject, Background, true);
            var view =
                screen.gameObject.AddComponent<EquipmentScreenView>();

            var body = CreateAnchored(
                "Body",
                screen,
                new Vector2(0.5f, 0.5f),
                new Vector2(1760f, 900f),
                new Vector2(0f, -8f),
                new Vector2(0.5f, 0.5f));
            AddImage(body.gameObject, Panel, false);
            CreateAccentBars(body);

            CreateText(
                "HeaderEnglish",
                body,
                "代理人整备 · 装备",
                font,
                16f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(48f, -38f),
                new Vector2(520f, 26f),
                new Vector2(0f, 1f));
            CreateText(
                "Header",
                body,
                "代理人整备",
                font,
                43f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(48f, -74f),
                new Vector2(360f, 60f),
                new Vector2(0f, 1f));
            var revisionText = CreateText(
                "Revision",
                body,
                "整备版本 0000",
                font,
                16f,
                Secondary,
                TextAlignmentOptions.Right,
                new Vector2(-176f, -74f),
                new Vector2(280f, 30f),
                new Vector2(1f, 1f));
            var closeButton = CreateButton(
                "Close",
                body,
                "关闭  [ESC]",
                font,
                new Vector2(1f, 1f),
                new Vector2(142f, 48f),
                new Vector2(-32f, -32f),
                new Color32(255, 99, 121, 255));

            var loadoutPanel = CreateAnchored(
                "LoadoutSlots",
                body,
                new Vector2(0f, 0.5f),
                new Vector2(338f, 680f),
                new Vector2(48f, -18f),
                new Vector2(0f, 0.5f));
            AddImage(loadoutPanel.gameObject, Raised, false);
            CreateText(
                "SectionEyebrow",
                loadoutPanel,
                "当前装备",
                font,
                13f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(20f, -18f),
                new Vector2(220f, 22f),
                new Vector2(0f, 1f));
            CreateText(
                "SectionTitle",
                loadoutPanel,
                "装备槽位",
                font,
                26f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(20f, -42f),
                new Vector2(240f, 38f),
                new Vector2(0f, 1f));
            var loadoutGrid = CreateAnchored(
                "SlotGrid",
                loadoutPanel,
                new Vector2(0.5f, 1f),
                new Vector2(310f, 578f),
                new Vector2(0f, -88f),
                new Vector2(0.5f, 1f));
            var loadoutLayout =
                loadoutGrid.gameObject.AddComponent<GridLayoutGroup>();
            loadoutLayout.padding = new RectOffset(3, 3, 4, 4);
            loadoutLayout.cellSize = new Vector2(147f, 105f);
            loadoutLayout.spacing = new Vector2(10f, 10f);
            loadoutLayout.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;
            loadoutLayout.constraintCount = 2;
            loadoutLayout.childAlignment = TextAnchor.UpperLeft;

            var slotButtons = new Button[10];
            var slotBackgrounds = new Image[10];
            var slotIcons = new Image[10];
            var slotLabels = new TMP_Text[10];
            var slotItemNames = new TMP_Text[10];
            for (var index = 0; index < slotButtons.Length; index++)
            {
                CreateEquipmentSlot(
                    loadoutGrid,
                    font,
                    index,
                    out slotButtons[index],
                    out slotBackgrounds[index],
                    out slotIcons[index],
                    out slotLabels[index],
                    out slotItemNames[index]);
            }

            var catalogPanel = CreateAnchored(
                "EquipmentCatalog",
                body,
                new Vector2(0f, 0.5f),
                new Vector2(590f, 680f),
                new Vector2(406f, -18f),
                new Vector2(0f, 0.5f));
            AddImage(
                catalogPanel.gameObject,
                new Color32(12, 22, 36, 244),
                false);
            CreateText(
                "SectionEyebrow",
                catalogPanel,
                "可用装备",
                font,
                13f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(20f, -18f),
                new Vector2(250f, 22f),
                new Vector2(0f, 1f));
            CreateText(
                "SectionTitle",
                catalogPanel,
                "候选装备",
                font,
                26f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(20f, -42f),
                new Vector2(240f, 38f),
                new Vector2(0f, 1f));
            CreateText(
                "CatalogHint",
                catalogPanel,
                "选择卡片后可装备至兼容槽位",
                font,
                14f,
                Muted,
                TextAlignmentOptions.Right,
                new Vector2(-20f, -48f),
                new Vector2(300f, 26f),
                new Vector2(1f, 1f));
            var catalogGrid = CreateAnchored(
                "ItemGrid",
                catalogPanel,
                new Vector2(0.5f, 1f),
                new Vector2(558f, 578f),
                new Vector2(0f, -88f),
                new Vector2(0.5f, 1f));
            var catalogLayout =
                catalogGrid.gameObject.AddComponent<GridLayoutGroup>();
            catalogLayout.padding = new RectOffset(4, 4, 5, 5);
            catalogLayout.cellSize = new Vector2(174f, 84f);
            catalogLayout.spacing = new Vector2(12f, 10f);
            catalogLayout.constraint =
                GridLayoutGroup.Constraint.FixedColumnCount;
            catalogLayout.constraintCount = 3;
            catalogLayout.childAlignment = TextAnchor.UpperLeft;

            var itemButtons = new Button[18];
            var itemBackgrounds = new Image[18];
            var itemIcons = new Image[18];
            var itemNames = new TMP_Text[18];
            var itemStates = new TMP_Text[18];
            for (var index = 0; index < itemButtons.Length; index++)
            {
                CreateEquipmentItemCard(
                    catalogGrid,
                    font,
                    index,
                    out itemButtons[index],
                    out itemBackgrounds[index],
                    out itemIcons[index],
                    out itemNames[index],
                    out itemStates[index]);
            }

            var detailPanel = CreateAnchored(
                "EquipmentDetail",
                body,
                new Vector2(1f, 0.5f),
                new Vector2(704f, 680f),
                new Vector2(-48f, -18f),
                new Vector2(1f, 0.5f));
            AddImage(detailPanel.gameObject, Raised, false);
            var detailTopAccent = CreateAnchored(
                "TopAccent",
                detailPanel,
                new Vector2(0.5f, 1f),
                new Vector2(656f, 5f),
                new Vector2(0f, -1f),
                new Vector2(0.5f, 1f));
            AddImage(detailTopAccent.gameObject, Gold, false);
            CreateText(
                "DetailEyebrow",
                detailPanel,
                "装备详情",
                font,
                13f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(28f, -22f),
                new Vector2(260f, 22f),
                new Vector2(0f, 1f));

            var detailIconFrame = CreateAnchored(
                "SelectedIconFrame",
                detailPanel,
                new Vector2(0f, 1f),
                new Vector2(118f, 118f),
                new Vector2(28f, -58f),
                new Vector2(0f, 1f));
            var detailIconFrameImage = AddImage(
                detailIconFrame.gameObject,
                new Color32(9, 19, 33, 255),
                false);
            detailIconFrameImage.sprite = _slotSprite;
            detailIconFrameImage.type = Image.Type.Sliced;
            var detailIconRect = CreateRect(
                "SelectedIcon",
                detailIconFrame);
            SetStretch(detailIconRect, 17f);
            var detailIcon = AddImage(
                detailIconRect.gameObject,
                new Color32(88, 224, 231, 210),
                false);
            detailIcon.sprite = _crosshairSprite;
            detailIcon.preserveAspect = true;

            var detailRarity = CreateText(
                "SelectedRarity",
                detailPanel,
                "传奇 // S",
                font,
                15f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(168f, -62f),
                new Vector2(210f, 26f),
                new Vector2(0f, 1f));
            var detailName = CreateText(
                "SelectedName",
                detailPanel,
                "雷鸣刃",
                font,
                33f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(168f, -91f),
                new Vector2(498f, 46f),
                new Vector2(0f, 1f));
            var detailDescription = CreateText(
                "SelectedDescription",
                detailPanel,
                "高频电流沿刃身循环，为攻击附加雷属性强化。",
                font,
                16f,
                Secondary,
                TextAlignmentOptions.TopLeft,
                new Vector2(168f, -139f),
                new Vector2(500f, 56f),
                new Vector2(0f, 1f));
            detailDescription.textWrappingMode =
                TextWrappingModes.Normal;

            CreateText(
                "ModifierLabel",
                detailPanel,
                "装备词条",
                font,
                13f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(28f, -201f),
                new Vector2(340f, 24f),
                new Vector2(0f, 1f));
            var detailModifiers = CreateText(
                "Modifiers",
                detailPanel,
                "攻击力 +18%    雷属性伤害 +12%\n暴击率 +6%",
                font,
                16f,
                Primary,
                TextAlignmentOptions.TopLeft,
                new Vector2(28f, -229f),
                new Vector2(648f, 68f),
                new Vector2(0f, 1f));
            detailModifiers.textWrappingMode =
                TextWrappingModes.Normal;

            var setPanel = CreateAnchored(
                "ActiveSet",
                detailPanel,
                new Vector2(0.5f, 1f),
                new Vector2(648f, 86f),
                new Vector2(0f, -314f),
                new Vector2(0.5f, 1f));
            AddImage(
                setPanel.gameObject,
                new Color32(40, 37, 33, 245),
                false);
            var setAccent = CreateAnchored(
                "Accent",
                setPanel,
                new Vector2(0f, 0.5f),
                new Vector2(6f, 66f),
                Vector2.zero,
                new Vector2(0f, 0.5f));
            AddImage(setAccent.gameObject, Gold, false);
            CreateText(
                "SetLabel",
                setPanel,
                "套装效果",
                font,
                12f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(20f, -12f),
                new Vector2(260f, 22f),
                new Vector2(0f, 1f));
            var activeSets = CreateText(
                "SetDescription",
                setPanel,
                "雷鸣共振 2/2：雷属性伤害 +15%，易伤叠层效率提升。",
                font,
                15f,
                Primary,
                TextAlignmentOptions.TopLeft,
                new Vector2(20f, -38f),
                new Vector2(606f, 42f),
                new Vector2(0f, 1f));
            activeSets.textWrappingMode = TextWrappingModes.Normal;

            CreateText(
                "StatSection",
                detailPanel,
                "属性对照",
                font,
                13f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(28f, -418f),
                new Vector2(360f, 22f),
                new Vector2(0f, 1f));
            CreateText(
                "BaseHeader",
                detailPanel,
                "基础",
                font,
                13f,
                Muted,
                TextAlignmentOptions.Right,
                new Vector2(-192f, -418f),
                new Vector2(100f, 22f),
                new Vector2(1f, 1f));
            CreateText(
                "FinalHeader",
                detailPanel,
                "最终",
                font,
                13f,
                Gold,
                TextAlignmentOptions.Right,
                new Vector2(-28f, -418f),
                new Vector2(110f, 22f),
                new Vector2(1f, 1f));

            var statNames = new TMP_Text[6];
            var statBaseValues = new TMP_Text[6];
            var statFinalValues = new TMP_Text[6];
            var statLabels = new[]
            {
                "生命值", "攻击力", "防御力",
                "暴击率", "暴击伤害", "雷属性伤害"
            };
            var statBaseDefaults = new[]
            {
                "100", "24", "8", "5.0%", "150.0%", "0.0%"
            };
            var statFinalDefaults = new[]
            {
                "112", "31", "11", "11.0%", "162.0%", "27.0%"
            };
            for (var index = 0; index < statNames.Length; index++)
            {
                var rowY = -451f - index * 27f;
                var row = CreateAnchored(
                    $"StatRow_{index:00}",
                    detailPanel,
                    new Vector2(0.5f, 1f),
                    new Vector2(648f, 27f),
                    new Vector2(0f, rowY),
                    new Vector2(0.5f, 1f));
                if (index % 2 == 0)
                {
                    AddImage(
                        row.gameObject,
                        new Color32(14, 27, 43, 210),
                        false);
                }

                statNames[index] = CreateText(
                    "Name",
                    row,
                    statLabels[index],
                    font,
                    14f,
                    Secondary,
                    TextAlignmentOptions.Left,
                    new Vector2(10f, 0f),
                    new Vector2(240f, 26f),
                    new Vector2(0f, 1f));
                statBaseValues[index] = CreateText(
                    "BaseValue",
                    row,
                    statBaseDefaults[index],
                    font,
                    14f,
                    Secondary,
                    TextAlignmentOptions.Right,
                    new Vector2(-164f, 0f),
                    new Vector2(120f, 26f),
                    new Vector2(1f, 1f));
                statFinalValues[index] = CreateText(
                    "FinalValue",
                    row,
                    statFinalDefaults[index],
                    font,
                    14f,
                    Cyan,
                    TextAlignmentOptions.Right,
                    new Vector2(-10f, 0f),
                    new Vector2(130f, 26f),
                    new Vector2(1f, 1f));
            }

            var equipButton = CreateButton(
                "Equip",
                detailPanel,
                "装备",
                font,
                new Vector2(1f, 0f),
                new Vector2(176f, 48f),
                new Vector2(-22f, 18f),
                Cyan);
            var unequipButton = CreateButton(
                "Unequip",
                detailPanel,
                "卸下",
                font,
                new Vector2(1f, 0f),
                new Vector2(176f, 48f),
                new Vector2(-210f, 18f),
                Gold);

            view.Configure(
                screen.gameObject,
                closeButton,
                equipButton,
                unequipButton,
                slotButtons,
                slotBackgrounds,
                slotIcons,
                slotLabels,
                slotItemNames,
                itemButtons,
                itemBackgrounds,
                itemIcons,
                itemNames,
                itemStates,
                revisionText,
                detailIcon,
                detailRarity,
                detailName,
                detailDescription,
                detailModifiers,
                activeSets,
                statNames,
                statBaseValues,
                statFinalValues);
            return view;
        }

        /// <summary>创建一个穿戴槽位卡片并返回 View 需要的引用。</summary>
        private static void CreateEquipmentSlot(
            RectTransform parent,
            TMP_FontAsset font,
            int index,
            out Button button,
            out Image background,
            out Image icon,
            out TMP_Text label,
            out TMP_Text itemName)
        {
            var slot = CreateRect($"EquipmentSlot_{index:00}", parent);
            slot.sizeDelta = new Vector2(147f, 105f);
            background = AddImage(slot.gameObject, Raised, true);
            background.sprite = _slotSprite;
            background.type = Image.Type.Sliced;

            button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color32(142, 244, 250, 255);
            colors.pressedColor =
                new Color32(78, 165, 175, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var accentRect = CreateAnchored(
                "Accent",
                slot,
                new Vector2(0.5f, 1f),
                new Vector2(147f, 4f),
                Vector2.zero,
                new Vector2(0.5f, 1f));
            AddImage(
                accentRect.gameObject,
                index == 0 ? Gold : Cyan,
                false);

            var iconFrame = CreateAnchored(
                "Icon",
                slot,
                new Vector2(0f, 0.5f),
                new Vector2(43f, 43f),
                new Vector2(10f, 8f),
                new Vector2(0f, 0.5f));
            icon = AddImage(
                iconFrame.gameObject,
                new Color32(88, 224, 231, 178),
                false);
            icon.sprite = _crosshairSprite;
            icon.preserveAspect = true;

            var slotNames = new[]
            {
                "武器", "头部", "护甲", "手套", "鞋履",
                "饰品Ⅰ", "饰品Ⅱ", "饰品Ⅲ", "饰品Ⅳ", "饰品Ⅴ"
            };
            label = CreateText(
                "SlotLabel",
                slot,
                slotNames[index],
                font,
                13f,
                index == 0 ? Gold : Cyan,
                TextAlignmentOptions.Left,
                new Vector2(61f, -18f),
                new Vector2(78f, 22f),
                new Vector2(0f, 1f));
            itemName = CreateText(
                "ItemName",
                slot,
                index == 0 ? "雷鸣刃" : "未装备",
                font,
                14f,
                index == 0 ? Primary : Muted,
                TextAlignmentOptions.Left,
                new Vector2(61f, -50f),
                new Vector2(78f, 40f),
                new Vector2(0f, 1f));
            itemName.textWrappingMode = TextWrappingModes.Normal;
        }

        /// <summary>创建一张候选装备卡片并返回 View 需要的引用。</summary>
        private static void CreateEquipmentItemCard(
            RectTransform parent,
            TMP_FontAsset font,
            int index,
            out Button button,
            out Image background,
            out Image icon,
            out TMP_Text itemName,
            out TMP_Text state)
        {
            var card = CreateRect($"EquipmentItem_{index:00}", parent);
            card.sizeDelta = new Vector2(174f, 84f);
            background = AddImage(
                card.gameObject,
                new Color32(20, 35, 54, 245),
                true);
            background.sprite = _slotSprite;
            background.type = Image.Type.Sliced;

            button = card.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color32(132, 237, 243, 255);
            colors.pressedColor =
                new Color32(76, 155, 166, 255);
            colors.selectedColor = Gold;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var rarity = CreateAnchored(
                "Rarity",
                card,
                new Vector2(0.5f, 1f),
                new Vector2(174f, 4f),
                Vector2.zero,
                new Vector2(0.5f, 1f));
            AddImage(
                rarity.gameObject,
                index % 6 == 0
                    ? Gold
                    : index % 3 == 0
                        ? Cyan
                        : Outline,
                false);

            var iconRect = CreateAnchored(
                "Icon",
                card,
                new Vector2(0f, 0.5f),
                new Vector2(46f, 46f),
                new Vector2(10f, 4f),
                new Vector2(0f, 0.5f));
            icon = AddImage(
                iconRect.gameObject,
                new Color32(88, 224, 231, 166),
                false);
            icon.sprite = _crosshairSprite;
            icon.preserveAspect = true;

            var defaultNames = new[]
            {
                "雷鸣刃", "电磁核心", "精密模组",
                "都市护符", "战术目镜", "轻量护甲"
            };
            itemName = CreateText(
                "Name",
                card,
                defaultNames[index % defaultNames.Length],
                font,
                14f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(64f, -16f),
                new Vector2(102f, 24f),
                new Vector2(0f, 1f));
            state = CreateText(
                "State",
                card,
                index == 0 ? "已装备" : "持有 ×1",
                font,
                11f,
                index == 0 ? Gold : Secondary,
                TextAlignmentOptions.Left,
                new Vector2(64f, -48f),
                new Vector2(102f, 22f),
                new Vector2(0f, 1f));
        }

        /// <summary>
        /// 创建商业化任务委托页面，包含任务列表、详情、追踪和领取。
        /// </summary>
        private static QuestScreenView CreateQuests(
            RectTransform parent,
            TMP_FontAsset font)
        {
            var screen = CreateRect("QuestScreen", parent);
            SetStretch(screen);
            AddImage(screen.gameObject, Background, true);
            var view =
                screen.gameObject.AddComponent<QuestScreenView>();

            var body = CreateAnchored(
                "Body",
                screen,
                new Vector2(0.5f, 0.5f),
                new Vector2(1760f, 900f),
                new Vector2(0f, -8f),
                new Vector2(0.5f, 0.5f));
            AddImage(body.gameObject, Panel, false);
            CreateAccentBars(body);

            CreateText(
                "HeaderEnglish",
                body,
                "任务委托",
                font,
                16f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(48f, -38f),
                new Vector2(480f, 26f),
                new Vector2(0f, 1f));
            CreateText(
                "Header",
                body,
                "任务委托",
                font,
                43f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(48f, -74f),
                new Vector2(300f, 60f),
                new Vector2(0f, 1f));
            var closeButton = CreateButton(
                "Close",
                body,
                "关闭  [ESC]",
                font,
                new Vector2(1f, 1f),
                new Vector2(142f, 48f),
                new Vector2(-32f, -32f),
                new Color32(255, 99, 121, 255));

            var listPanel = CreateAnchored(
                "QuestList",
                body,
                new Vector2(0f, 0.5f),
                new Vector2(720f, 700f),
                new Vector2(48f, -18f),
                new Vector2(0f, 0.5f));
            AddImage(listPanel.gameObject, Raised, false);
            CreateText(
                "SectionEyebrow",
                listPanel,
                "进行中的委托",
                font,
                13f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(24f, -20f),
                new Vector2(320f, 22f),
                new Vector2(0f, 1f));
            CreateText(
                "SectionTitle",
                listPanel,
                "委托列表",
                font,
                26f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(24f, -44f),
                new Vector2(240f, 38f),
                new Vector2(0f, 1f));

            var entryButtons = new Button[8];
            var entryBackgrounds = new Image[8];
            var entryTitles = new TMP_Text[8];
            var entryStatuses = new TMP_Text[8];
            var entryProgresses = new TMP_Text[8];
            for (var index = 0; index < entryButtons.Length; index++)
            {
                var card = CreateAnchored(
                    $"QuestEntry_{index:00}",
                    listPanel,
                    new Vector2(0.5f, 1f),
                    new Vector2(676f, 74f),
                    new Vector2(0f, -96f - index * 82f),
                    new Vector2(0.5f, 1f));
                entryBackgrounds[index] = AddImage(
                    card.gameObject,
                    new Color32(20, 35, 54, 245),
                    true);
                entryButtons[index] =
                    card.gameObject.AddComponent<Button>();
                entryButtons[index].targetGraphic =
                    entryBackgrounds[index];
                var colors = entryButtons[index].colors;
                colors.highlightedColor =
                    new Color32(142, 244, 250, 255);
                colors.pressedColor =
                    new Color32(78, 165, 175, 255);
                entryButtons[index].colors = colors;

                entryTitles[index] = CreateText(
                    "Title",
                    card,
                    index == 0 ? "初次巡防" : "空委托位",
                    font,
                    20f,
                    Primary,
                    TextAlignmentOptions.Left,
                    new Vector2(20f, -18f),
                    new Vector2(420f, 28f),
                    new Vector2(0f, 1f));
                entryStatuses[index] = CreateText(
                    "Status",
                    card,
                    index == 0 ? "进行中" : "未接取",
                    font,
                    14f,
                    index == 0 ? Cyan : Muted,
                    TextAlignmentOptions.Right,
                    new Vector2(-18f, -18f),
                    new Vector2(140f, 26f),
                    new Vector2(1f, 1f));
                entryProgresses[index] = CreateText(
                    "Progress",
                    card,
                    "0/1",
                    font,
                    14f,
                    Secondary,
                    TextAlignmentOptions.Left,
                    new Vector2(20f, -52f),
                    new Vector2(180f, 24f),
                    new Vector2(0f, 1f));
            }

            var detailPanel = CreateAnchored(
                "QuestDetail",
                body,
                new Vector2(1f, 0.5f),
                new Vector2(928f, 700f),
                new Vector2(-48f, -18f),
                new Vector2(1f, 0.5f));
            AddImage(detailPanel.gameObject, Raised, false);
            var detailAccent = CreateAnchored(
                "TopAccent",
                detailPanel,
                new Vector2(0.5f, 1f),
                new Vector2(880f, 5f),
                new Vector2(0f, -1f),
                new Vector2(0.5f, 1f));
            AddImage(detailAccent.gameObject, Gold, false);

            var detailStatus = CreateText(
                "Status",
                detailPanel,
                "进行中",
                font,
                15f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(32f, -30f),
                new Vector2(240f, 26f),
                new Vector2(0f, 1f));
            var detailTitle = CreateText(
                "Title",
                detailPanel,
                "初次巡防",
                font,
                36f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(32f, -68f),
                new Vector2(680f, 52f),
                new Vector2(0f, 1f));
            var detailDescription = CreateText(
                "Description",
                detailPanel,
                "清理训练城区中的模拟敌人。",
                font,
                17f,
                Secondary,
                TextAlignmentOptions.TopLeft,
                new Vector2(32f, -128f),
                new Vector2(860f, 70f),
                new Vector2(0f, 1f));
            detailDescription.textWrappingMode =
                TextWrappingModes.Normal;

            CreateText(
                "ObjectiveLabel",
                detailPanel,
                "目标进度",
                font,
                13f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(32f, -216f),
                new Vector2(320f, 22f),
                new Vector2(0f, 1f));
            var detailObjectives = CreateText(
                "Objectives",
                detailPanel,
                "• 击败训练骑士  0/3",
                font,
                17f,
                Primary,
                TextAlignmentOptions.TopLeft,
                new Vector2(32f, -244f),
                new Vector2(860f, 120f),
                new Vector2(0f, 1f));
            detailObjectives.textWrappingMode =
                TextWrappingModes.Normal;

            CreateText(
                "RewardLabel",
                detailPanel,
                "奖励",
                font,
                13f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(32f, -386f),
                new Vector2(280f, 22f),
                new Vector2(0f, 1f));
            var detailRewards = CreateText(
                "Rewards",
                detailPanel,
                "• 都市代币 ×30",
                font,
                16f,
                Primary,
                TextAlignmentOptions.TopLeft,
                new Vector2(32f, -414f),
                new Vector2(860f, 90f),
                new Vector2(0f, 1f));
            detailRewards.textWrappingMode =
                TextWrappingModes.Normal;

            var trackButton = CreateButton(
                "Track",
                detailPanel,
                "设为追踪",
                font,
                new Vector2(1f, 0f),
                new Vector2(220f, 52f),
                new Vector2(-232f, 34f),
                Cyan);
            var claimButton = CreateButton(
                "Claim",
                detailPanel,
                "领取奖励",
                font,
                new Vector2(1f, 0f),
                new Vector2(220f, 52f),
                new Vector2(-24f, 34f),
                Gold);
            var feedback = CreateText(
                "Feedback",
                detailPanel,
                string.Empty,
                font,
                15f,
                Gold,
                TextAlignmentOptions.Right,
                new Vector2(-264f, 18f),
                new Vector2(600f, 30f),
                new Vector2(1f, 0f));

            view.Configure(
                screen.gameObject,
                closeButton,
                entryButtons,
                entryBackgrounds,
                entryTitles,
                entryStatuses,
                entryProgresses,
                detailTitle,
                detailDescription,
                detailObjectives,
                detailRewards,
                detailStatus,
                trackButton,
                claimButton,
                feedback);
            return view;
        }

        /// <summary>
        /// 创建商业化角色名册页面，包含角色卡片、详情、切换和解锁。
        /// </summary>
        private static CharacterScreenView CreateCharacters(
            RectTransform parent,
            TMP_FontAsset font)
        {
            var screen = CreateRect("CharacterScreen", parent);
            SetStretch(screen);
            AddImage(screen.gameObject, Background, true);
            var view =
                screen.gameObject.AddComponent<CharacterScreenView>();

            var body = CreateAnchored(
                "Body",
                screen,
                new Vector2(0.5f, 0.5f),
                new Vector2(1760f, 900f),
                new Vector2(0f, -8f),
                new Vector2(0.5f, 0.5f));
            AddImage(body.gameObject, Panel, false);
            CreateAccentBars(body);

            CreateText(
                "HeaderEnglish",
                body,
                "代理人名册",
                font,
                16f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(48f, -38f),
                new Vector2(360f, 26f),
                new Vector2(0f, 1f));
            CreateText(
                "Header",
                body,
                "代理人名册",
                font,
                43f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(48f, -74f),
                new Vector2(360f, 60f),
                new Vector2(0f, 1f));
            var closeButton = CreateButton(
                "Close",
                body,
                "关闭  [ESC]",
                font,
                new Vector2(1f, 1f),
                new Vector2(142f, 48f),
                new Vector2(-32f, -32f),
                new Color32(255, 99, 121, 255));

            var listPanel = CreateAnchored(
                "CharacterList",
                body,
                new Vector2(0f, 0.5f),
                new Vector2(560f, 700f),
                new Vector2(48f, -18f),
                new Vector2(0f, 0.5f));
            AddImage(listPanel.gameObject, Raised, false);
            CreateText(
                "SectionEyebrow",
                listPanel,
                "已解锁代理人",
                font,
                13f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(24f, -20f),
                new Vector2(280f, 22f),
                new Vector2(0f, 1f));
            CreateText(
                "SectionTitle",
                listPanel,
                "角色列表",
                font,
                26f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(24f, -44f),
                new Vector2(240f, 38f),
                new Vector2(0f, 1f));

            var entryButtons = new Button[8];
            var entryBackgrounds = new Image[8];
            var entryNames = new TMP_Text[8];
            var entryRoles = new TMP_Text[8];
            for (var index = 0; index < entryButtons.Length; index++)
            {
                var card = CreateAnchored(
                    $"CharacterEntry_{index:00}",
                    listPanel,
                    new Vector2(0.5f, 1f),
                    new Vector2(516f, 76f),
                    new Vector2(0f, -96f - index * 84f),
                    new Vector2(0.5f, 1f));
                entryBackgrounds[index] = AddImage(
                    card.gameObject,
                    new Color32(20, 35, 54, 245),
                    true);
                entryButtons[index] =
                    card.gameObject.AddComponent<Button>();
                entryButtons[index].targetGraphic =
                    entryBackgrounds[index];
                var colors = entryButtons[index].colors;
                colors.highlightedColor =
                    new Color32(142, 244, 250, 255);
                colors.pressedColor =
                    new Color32(78, 165, 175, 255);
                entryButtons[index].colors = colors;

                entryNames[index] = CreateText(
                    "Name",
                    card,
                    index == 0 ? "艾莲·乔" : "待登记代理人",
                    font,
                    22f,
                    Primary,
                    TextAlignmentOptions.Left,
                    new Vector2(22f, -18f),
                    new Vector2(280f, 30f),
                    new Vector2(0f, 1f));
                entryRoles[index] = CreateText(
                    "Role",
                    card,
                index == 0 ? "强攻 / 冰属性" : "未解锁",
                    font,
                    14f,
                    index == 0 ? Cyan : Muted,
                    TextAlignmentOptions.Left,
                    new Vector2(22f, -54f),
                    new Vector2(320f, 24f),
                    new Vector2(0f, 1f));
            }

            var detailPanel = CreateAnchored(
                "CharacterDetail",
                body,
                new Vector2(1f, 0.5f),
                new Vector2(1120f, 700f),
                new Vector2(-48f, -18f),
                new Vector2(1f, 0.5f));
            AddImage(detailPanel.gameObject, Raised, false);
            var detailAccent = CreateAnchored(
                "TopAccent",
                detailPanel,
                new Vector2(0.5f, 1f),
                new Vector2(1072f, 5f),
                new Vector2(0f, -1f),
                new Vector2(0.5f, 1f));
            AddImage(detailAccent.gameObject, Cyan, false);

            var preview = CreateAnchored(
                "Preview",
                detailPanel,
                new Vector2(0f, 1f),
                new Vector2(440f, 560f),
                new Vector2(34f, -62f),
                new Vector2(0f, 1f));
            AddImage(
                preview.gameObject,
                new Color32(12, 24, 39, 255),
                false);
            var icon = CreateAnchored(
                "AgentIcon",
                preview,
                new Vector2(0.5f, 0.5f),
                new Vector2(180f, 180f),
                new Vector2(0f, 40f),
                new Vector2(0.5f, 0.5f));
            var iconImage = AddImage(
                icon.gameObject,
                new Color32(88, 224, 231, 120),
                false);
            iconImage.sprite = _crosshairSprite;
            iconImage.preserveAspect = true;
            CreateDecorativeDiamond(preview, Vector2.zero, Cyan, 120f);
            CreateText(
                "PreviewLetters",
                preview,
                "代理人",
                font,
                20f,
                Primary,
                TextAlignmentOptions.Center,
                Vector2.zero,
                new Vector2(160f, 34f),
                new Vector2(0.5f, 0.5f));

            var info = CreateAnchored(
                "Info",
                detailPanel,
                new Vector2(1f, 1f),
                new Vector2(600f, 560f),
                new Vector2(-34f, -62f),
                new Vector2(1f, 1f));
            var detailStatus = CreateText(
                "Status",
                info,
                "当前代理人",
                font,
                15f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(0f, -4f),
                new Vector2(240f, 26f),
                new Vector2(0f, 1f));
            var detailName = CreateText(
                "Name",
                info,
                "艾莲·乔",
                font,
                40f,
                Primary,
                TextAlignmentOptions.Left,
                new Vector2(0f, -42f),
                new Vector2(560f, 56f),
                new Vector2(0f, 1f));
            var detailFaction = CreateText(
                "Faction",
                info,
                "阵营  维多利亚家政",
                font,
                17f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(0f, -108f),
                new Vector2(560f, 30f),
                new Vector2(0f, 1f));
            var detailRole = CreateText(
                "Role",
                info,
                "定位  强攻 / 冰属性",
                font,
                17f,
                Secondary,
                TextAlignmentOptions.Left,
                new Vector2(0f, -146f),
                new Vector2(560f, 30f),
                new Vector2(0f, 1f));
            var detailDescription = CreateText(
                "Description",
                info,
                "维多利亚家政的鲨鱼希人女仆。",
                font,
                18f,
                Secondary,
                TextAlignmentOptions.TopLeft,
                new Vector2(0f, -196f),
                new Vector2(560f, 160f),
                new Vector2(0f, 1f));
            detailDescription.textWrappingMode =
                TextWrappingModes.Normal;

            var selectButton = CreateButton(
                "Select",
                info,
                "设为当前代理人",
                font,
                new Vector2(1f, 0f),
                new Vector2(250f, 52f),
                new Vector2(-260f, 12f),
                Cyan);
            var unlockButton = CreateButton(
                "Unlock",
                info,
                "解锁",
                font,
                new Vector2(1f, 0f),
                new Vector2(250f, 52f),
                new Vector2(0f, 12f),
                Gold);
            var feedback = CreateText(
                "Feedback",
                info,
                string.Empty,
                font,
                15f,
                Gold,
                TextAlignmentOptions.Right,
                new Vector2(0f, -48f),
                new Vector2(560f, 30f),
                new Vector2(1f, 0f));

            view.Configure(
                screen.gameObject,
                closeButton,
                entryButtons,
                entryBackgrounds,
                entryNames,
                entryRoles,
                detailName,
                detailDescription,
                detailFaction,
                detailRole,
                detailStatus,
                selectButton,
                unlockButton,
                feedback);
            return view;
        }

        /// <summary>
        /// 创建常驻对话浮层，显示说话人、台词、继续与选项按钮。
        /// </summary>
        private static DialogueScreenView CreateDialogue(
            RectTransform parent,
            TMP_FontAsset font)
        {
            var screen = CreateRect("DialogueOverlay", parent);
            SetStretch(screen);
            AddImage(
                screen.gameObject,
                new Color32(4, 8, 16, 220),
                true);
            var view =
                screen.gameObject.AddComponent<DialogueScreenView>();

            var panel = CreateAnchored(
                "DialoguePanel",
                screen,
                new Vector2(0.5f, 0f),
                new Vector2(1500f, 300f),
                new Vector2(0f, 44f),
                new Vector2(0.5f, 0f));
            AddImage(panel.gameObject, Panel, false);
            CreateAccentBars(panel);

            var title = CreateText(
                "Title",
                panel,
                "与 Rusk 的初次会面",
                font,
                18f,
                Gold,
                TextAlignmentOptions.Left,
                new Vector2(30f, -20f),
                new Vector2(620f, 28f),
                new Vector2(0f, 1f));
            var closeButton = CreateButton(
                "Close",
                panel,
                "取消",
                font,
                new Vector2(1f, 1f),
                new Vector2(118f, 42f),
                new Vector2(-24f, -22f),
                new Color32(255, 99, 121, 255));
            var speaker = CreateText(
                "Speaker",
                panel,
                "character.rusk",
                font,
                16f,
                Cyan,
                TextAlignmentOptions.Left,
                new Vector2(30f, -66f),
                new Vector2(320f, 26f),
                new Vector2(0f, 1f));
            var text = CreateText(
                "Line",
                panel,
                "醒了？这里并不安全。",
                font,
                26f,
                Primary,
                TextAlignmentOptions.TopLeft,
                new Vector2(30f, -104f),
                new Vector2(1440f, 100f),
                new Vector2(0f, 1f));
            text.textWrappingMode = TextWrappingModes.Normal;

            var continueButton = CreateButton(
                "Continue",
                panel,
                "继续  [空格]",
                font,
                new Vector2(1f, 0f),
                new Vector2(210f, 48f),
                new Vector2(-28f, 20f),
                Cyan);

            var choiceButtons = new Button[4];
            var choiceTexts = new TMP_Text[4];
            for (var index = 0; index < choiceButtons.Length; index++)
            {
                var choice = CreateAnchored(
                    $"Choice_{index:00}",
                    panel,
                    new Vector2(0f, 0f),
                    new Vector2(680f, 48f),
                    new Vector2(30f + (index % 2) * 710f,
                        24f + (index / 2) * 58f),
                    new Vector2(0f, 0f));
                var background = AddImage(
                    choice.gameObject,
                    new Color32(24, 44, 66, 250),
                    true);
                choiceButtons[index] =
                    choice.gameObject.AddComponent<Button>();
                choiceButtons[index].targetGraphic = background;
                choiceTexts[index] = CreateText(
                    "Text",
                    choice,
                    $"选项 {index + 1:00}",
                    font,
                    18f,
                    Primary,
                    TextAlignmentOptions.Center,
                    Vector2.zero,
                    new Vector2(650f, 42f),
                    new Vector2(0.5f, 0.5f));
            }

            view.Configure(
                screen.gameObject,
                title,
                speaker,
                text,
                continueButton,
                closeButton,
                choiceButtons,
                choiceTexts);
            return view;
        }

        /// <summary>
        /// 创建任务、角色和档案接入真实 Presenter 前使用的通用页面骨架。
        /// </summary>
        private static MenuPlaceholderScreenView CreateMenuPlaceholder(
            RectTransform parent,
            TMP_FontAsset font)
        {
            var screen = CreateRect("MenuPlaceholderScreen", parent);
            SetStretch(screen);
            AddImage(screen.gameObject, Background, true);
            var view =
                screen.gameObject.AddComponent<MenuPlaceholderScreenView>();

            var body = CreateAnchored(
                "Body",
                screen,
                new Vector2(0.5f, 0.5f),
                new Vector2(1760f, 900f),
                new Vector2(0f, -8f),
                new Vector2(0.5f, 0.5f));
            AddImage(body.gameObject, Panel, false);
            CreateAccentBars(body);

            var focusPanel = CreateAnchored(
                "FeaturePreview",
                body,
                new Vector2(0.5f, 0.5f),
                new Vector2(1280f, 590f),
                new Vector2(0f, -18f),
                new Vector2(0.5f, 0.5f));
            AddImage(
                focusPanel.gameObject,
                new Color32(13, 25, 41, 248),
                false);
            var focusAccent = CreateAnchored(
                "FocusAccent",
                focusPanel,
                new Vector2(0.5f, 1f),
                new Vector2(310f, 5f),
                new Vector2(0f, -1f),
                new Vector2(0.5f, 1f));
            AddImage(focusAccent.gameObject, Cyan, false);
            CreateDecorativeDiamond(
                focusPanel,
                new Vector2(0f, 104f),
                Cyan,
                132f);
            var iconRect = CreateAnchored(
                "PreviewIcon",
                focusPanel,
                new Vector2(0.5f, 0.5f),
                new Vector2(104f, 104f),
                new Vector2(0f, 104f),
                new Vector2(0.5f, 0.5f));
            var icon = AddImage(
                iconRect.gameObject,
                new Color32(88, 224, 231, 150),
                false);
            icon.sprite = _crosshairSprite;
            icon.preserveAspect = true;

            var eyebrow = CreateText(
                "Eyebrow",
                focusPanel,
                "任务",
                font,
                15f,
                Gold,
                TextAlignmentOptions.Center,
                new Vector2(0f, -16f),
                new Vector2(640f, 28f),
                new Vector2(0.5f, 0.5f));
            var title = CreateText(
                "Title",
                focusPanel,
                "委托档案",
                font,
                48f,
                Primary,
                TextAlignmentOptions.Center,
                new Vector2(0f, -72f),
                new Vector2(800f, 70f),
                new Vector2(0.5f, 0.5f));
            var description = CreateText(
                "Description",
                focusPanel,
                "页面框架已经准备完成，后续 Presenter 将在这里接入真实数据。",
                font,
                20f,
                Secondary,
                TextAlignmentOptions.Center,
                new Vector2(0f, -140f),
                new Vector2(850f, 76f),
                new Vector2(0.5f, 0.5f));
            description.textWrappingMode = TextWrappingModes.Normal;

            CreateText(
                "Footer",
                focusPanel,
                "DATA CHANNEL READY // 等待业务模块接入",
                font,
                13f,
                Muted,
                TextAlignmentOptions.Center,
                new Vector2(0f, -238f),
                new Vector2(620f, 26f),
                new Vector2(0.5f, 0.5f));

            view.Configure(
                screen.gameObject,
                eyebrow,
                title,
                description);
            return view;
        }

        /// <summary>
        /// 创建位于所有主菜单页面之上的六按钮导航覆盖层。
        /// </summary>
        private static void CreateMenuNavigation(
            RectTransform parent,
            TMP_FontAsset font,
            out GameObject navigationRoot,
            out Button inventoryButton,
            out Button equipmentButton,
            out Button questButton,
            out Button charactersButton,
            out Button archiveButton,
            out Button closeButton)
        {
            var navigation = CreateRect("MenuNavigationOverlay", parent);
            SetStretch(navigation);
            navigationRoot = navigation.gameObject;

            var bar = CreateAnchored(
                "NavigationBar",
                navigation,
                new Vector2(0.5f, 1f),
                new Vector2(1280f, 76f),
                new Vector2(0f, -14f),
                new Vector2(0.5f, 1f));
            AddImage(
                bar.gameObject,
                new Color32(8, 15, 28, 252),
                true);
            var topLine = CreateAnchored(
                "TopLine",
                bar,
                new Vector2(0.5f, 1f),
                new Vector2(1216f, 3f),
                new Vector2(0f, -1f),
                new Vector2(0.5f, 1f));
            AddImage(topLine.gameObject, Outline, false);

            inventoryButton = CreateButton(
                "InventoryTab",
                bar,
                "仓库",
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(184f, 50f),
                new Vector2(-515f, 0f),
                Cyan);
            equipmentButton = CreateButton(
                "EquipmentTab",
                bar,
                "装备",
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(184f, 50f),
                new Vector2(-309f, 0f),
                Gold);
            questButton = CreateButton(
                "QuestTab",
                bar,
                "任务",
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(184f, 50f),
                new Vector2(-103f, 0f),
                Cyan);
            charactersButton = CreateButton(
                "CharactersTab",
                bar,
                "角色",
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(184f, 50f),
                new Vector2(103f, 0f),
                Cyan);
            archiveButton = CreateButton(
                "ArchiveTab",
                bar,
                "档案",
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(184f, 50f),
                new Vector2(309f, 0f),
                Cyan);
            closeButton = CreateButton(
                "CloseMenu",
                bar,
                "关闭  [ESC]",
                font,
                new Vector2(0.5f, 0.5f),
                new Vector2(184f, 50f),
                new Vector2(515f, 0f),
                new Color32(255, 99, 121, 255));
        }

        private static void CreateInventorySlot(
            RectTransform parent,
            TMP_FontAsset font,
            int index,
            out Button button,
            out Image background,
            out Image accent,
            out Image icon,
            out TMP_Text name,
            out TMP_Text quantity)
        {
            var slot = CreateRect($"Slot_{index:00}", parent);
            slot.sizeDelta = new Vector2(146f, 142f);
            background = AddImage(slot.gameObject, Raised, true);
            background.sprite = _slotSprite;
            background.type = Image.Type.Sliced;
            button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = background;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color32(151, 249, 255, 255);
            colors.pressedColor = new Color32(85, 177, 186, 255);
            colors.selectedColor = colors.highlightedColor;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var accentRect = CreateAnchored(
                "Rarity",
                slot,
                new Vector2(0.5f, 1f),
                new Vector2(146f, 5f),
                Vector2.zero,
                new Vector2(0.5f, 1f));
            accent = AddImage(
                accentRect.gameObject,
                index < 4 ? Cyan : Outline,
                false);
            CreateText(
                "Index",
                slot,
                $"{index + 1:00}",
                font,
                13f,
                Muted,
                TextAlignmentOptions.Left,
                new Vector2(12f, -12f),
                new Vector2(42f, 22f),
                new Vector2(0f, 1f));
            CreateDecorativeDiamond(
                slot,
                new Vector2(0f, 26f),
                index < 4 ? Cyan : Outline,
                42f);
            var iconRect = CreateAnchored(
                "Icon",
                slot,
                new Vector2(0.5f, 1f),
                new Vector2(58f, 58f),
                new Vector2(0f, -22f),
                new Vector2(0.5f, 1f));
            icon = AddImage(iconRect.gameObject, Color.white, false);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            icon.gameObject.SetActive(false);
            name = CreateText(
                "Name",
                slot,
                index switch
                {
                    0 => "训练芯片",
                    1 => "治疗罐",
                    2 => "都市代币",
                    3 => "升级模组",
                    _ => "空槽位"
                },
                font,
                15f,
                index < 4 ? Primary : Muted,
                TextAlignmentOptions.Center,
                new Vector2(10f, -98f),
                new Vector2(126f, 28f),
                new Vector2(0f, 1f));
            quantity = CreateText(
                "Quantity",
                slot,
                index switch
                {
                    0 => "×8",
                    1 => "×3",
                    2 => "×120",
                    3 => "×1",
                    _ => $"{index + 1:00}"
                },
                font,
                13f,
                Secondary,
                TextAlignmentOptions.Center,
                new Vector2(10f, -124f),
                new Vector2(126f, 22f),
                new Vector2(0f, 1f));
        }

        private static void CreateAccentBars(RectTransform parent)
        {
            var top = CreateAnchored(
                "TopLine",
                parent,
                new Vector2(0.5f, 1f),
                new Vector2(parent.sizeDelta.x - 32f, 2f),
                new Vector2(0f, -1f),
                new Vector2(0.5f, 1f));
            AddImage(top.gameObject, Outline, false);

            var cyan = CreateAnchored(
                "CyanLine",
                parent,
                new Vector2(0f, 1f),
                new Vector2(84f, 3f),
                new Vector2(24f, -1f),
                new Vector2(0f, 1f));
            AddImage(cyan.gameObject, Cyan, false);
        }

        private static void CreateDecorativeDiamond(
            RectTransform parent,
            Vector2 position,
            Color color,
            float size = 22f)
        {
            var diamond = CreateAnchored(
                "Diamond",
                parent,
                new Vector2(0.5f, 0.5f),
                new Vector2(size, size),
                position,
                new Vector2(0.5f, 0.5f));
            diamond.localRotation = Quaternion.Euler(0f, 0f, 45f);
            AddImage(
                diamond.gameObject,
                new Color(color.r, color.g, color.b, 0.34f),
                false);
        }

        private static Button CreateButton(
            string name,
            RectTransform parent,
            string label,
            TMP_FontAsset font,
            Vector2 anchor,
            Vector2 size,
            Vector2 position,
            Color accent)
        {
            var rect = CreateAnchored(
                name,
                parent,
                anchor,
                size,
                position,
                anchor);
            var image = AddImage(rect.gameObject, Raised, true);
            image.sprite = _buttonSprite;
            image.type = Image.Type.Sliced;
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = accent;
            colors.pressedColor = new Color(
                accent.r * 0.72f,
                accent.g * 0.72f,
                accent.b * 0.72f,
                1f);
            colors.selectedColor = colors.highlightedColor;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            var accentRect = CreateAnchored(
                "Accent",
                rect,
                new Vector2(0f, 0.5f),
                new Vector2(5f, size.y - 12f),
                new Vector2(3f, 0f),
                new Vector2(0f, 0.5f));
            AddImage(accentRect.gameObject, accent, false);
            CreateText(
                "Label",
                rect,
                label,
                font,
                18f,
                Primary,
                TextAlignmentOptions.Center,
                Vector2.zero,
                size - new Vector2(14f, 8f),
                new Vector2(0.5f, 0.5f));
            return button;
        }

        private static TMP_Text CreateText(
            string name,
            RectTransform parent,
            string value,
            TMP_FontAsset font,
            float fontSize,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 position,
            Vector2 size,
            Vector2 pivot)
        {
            var rect = CreateAnchored(
                name,
                parent,
                pivot,
                size,
                position,
                pivot);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSize = fontSize;
            text.color = color;
            text.alignment = alignment;
            text.text = value;
            text.raycastTarget = false;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            return text;
        }

        private static Image AddImage(
            GameObject target,
            Color color,
            bool raycastTarget)
        {
            var image = target.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycastTarget;
            return image;
        }

        private static RectTransform CreateRect(
            string name,
            Transform parent)
        {
            var target = new GameObject(name, typeof(RectTransform));
            var rect = target.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform CreateAnchored(
            string name,
            Transform parent,
            Vector2 anchor,
            Vector2 size,
            Vector2 position,
            Vector2 pivot)
        {
            var rect = CreateRect(name, parent);
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.sizeDelta = size;
            rect.anchoredPosition = position;
            return rect;
        }

        private static void SetStretch(
            RectTransform rect,
            float inset = 0f)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(inset, inset);
            rect.offsetMax = new Vector2(-inset, -inset);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var slash = path.LastIndexOf('/');
            var parent = path.Substring(0, slash);
            var name = path.Substring(slash + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }
    }
}
#endif
