using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using StickmanFight.Art;
using StickmanFight.Characters;
using StickmanFight.Core;
using StickmanFight.Inputs;
using StickmanFight.UI;
using StickmanFight.Weapons;

namespace StickmanFight.EditorTools
{
    /// <summary>
    /// Собирает сцены кодом (ADR-3). Ручная правка сцен в редакторе не коммитится:
    /// единственный источник истины — этот файл плюс LevelConfig.
    /// </summary>
    public static class SceneBuilder
    {
        public const string ScenesDir = "Assets/_Project/Scenes";

        [MenuItem("Tools/Playground/2. Rebuild Scenes", priority = 2)]
        public static void RebuildMenu() => BuildAll();

        /// <summary>Возвращает список путей сцен для Build Settings.</summary>
        public static string[] BuildAll()
        {
            Directory.CreateDirectory(ScenesDir);
            AssetDatabase.Refresh();

            var scenes = new List<string>
            {
                BuildArena(),     // S2/S3: основная сцена (управление + бой)
                BuildTestBed(),   // S1: приёмка стойки и ragdoll
                BuildBoot()
            };

            EditorBuildSettings.scenes = scenes.ConvertAll(p => new EditorBuildSettingsScene(p, true)).ToArray();
            AssetDatabase.SaveAssets();
            Debug.Log($"[Scenes] собрано {scenes.Count}: {string.Join(", ", scenes)}");
            return scenes.ToArray();
        }

        // ---------------------------------------------------------- Arena (S2/S3)

        /// <summary>
        /// Арена: земля, стены, игрок, манекен, три оружия, HUD и сенсорное управление.
        /// Собирается кодом (ADR-3), автопрогон G2/G3 — компонентом ArenaTest.
        /// </summary>
        private static string BuildArena()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- камера ---
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex(Palette.BackgroundArena);
            camGo.transform.position = new Vector3(0f, 2.4f, -10f);
            camGo.AddComponent<AudioListener>();

            var follow = camGo.AddComponent<CameraFollow>();
            follow.offset = new Vector2(0f, 1.5f);
            follow.min = new Vector2(-9.5f, 1.8f);
            follow.max = new Vector2(9.5f, 3.2f);

            // --- уровень: пол 25 м, стены по краям ---
            CreateStrip("Ground", new Vector2(-12.5f, 0f), new Vector2(12.5f, 0f), 0.14f, Palette.Ground,
                new Vector2(0f, -0.15f), new Vector2(25f, 0.3f));
            CreateStrip("WallLeft", new Vector2(-12.2f, 0f), new Vector2(-12.2f, 9f), 0.18f, Palette.Ground,
                new Vector2(-12.2f, 4.5f), new Vector2(0.4f, 9f));
            CreateStrip("WallRight", new Vector2(12.2f, 0f), new Vector2(12.2f, 9f), 0.18f, Palette.Ground,
                new Vector2(12.2f, 4.5f), new Vector2(0.4f, 9f));

            // --- персонажи ---
            var player = CreateCharacter("Player", new Vector3(-0.7f, 0.05f), GameLayers.PlayerBody, Palette.Player);
            var enemy = CreateCharacter("Enemy", new Vector3(0.5f, 0.05f), GameLayers.EnemyBody, Palette.Enemy);
            follow.characterTarget = player;

            // --- оружие на земле ---
            var sword = CreateWeapon("Weapon_Sword", WeaponKind.Sword, new Vector3(-2.4f, 0.5f));
            var hammer = CreateWeapon("Weapon_Hammer", WeaponKind.Hammer, new Vector3(-3.4f, 0.5f));
            var pistol = CreateWeapon("Weapon_Pistol", WeaponKind.Pistol, new Vector3(-4.4f, 0.5f));

            // --- UI ---
            var canvasGo = new GameObject("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safeGo = new GameObject("SafeRoot", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            var safeRt = (RectTransform)safeGo.transform;
            safeRt.anchorMin = Vector2.zero;
            safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;
            safeGo.AddComponent<SafeAreaFitter>();
            Transform uiRoot = safeGo.transform;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            // HUD
            var hudGo = new GameObject("HUD");
            var hud = hudGo.AddComponent<BattleHUD>();
            hud.player = player;
            hud.enemy = enemy;
            hud.playerFill = MakeBar(uiRoot, "PlayerHp", new Vector2(520f, 26f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -44f), Hex(Palette.Player));
            hud.enemyFill = MakeBar(uiRoot, "EnemyHp", new Vector2(520f, 26f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-48f, -44f), Hex(Palette.Enemy));

            hud.weaponText = MakeText(uiRoot, "Weapon", "без оружия", 34, Hex(Palette.Text));
            Anchor(hud.weaponText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            hud.weaponText.rectTransform.pivot = new Vector2(0.5f, 1f);
            hud.weaponText.rectTransform.sizeDelta = new Vector2(600f, 44f);
            hud.weaponText.rectTransform.anchoredPosition = new Vector2(0f, -44f);
            hud.weaponText.alignment = TextAnchor.UpperCenter;

            hud.ammoText = MakeText(uiRoot, "Ammo", "", 28, Hex(Palette.Accent));
            Anchor(hud.ammoText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f));
            hud.ammoText.rectTransform.pivot = new Vector2(0.5f, 1f);
            hud.ammoText.rectTransform.sizeDelta = new Vector2(600f, 36f);
            hud.ammoText.rectTransform.anchoredPosition = new Vector2(0f, -90f);
            hud.ammoText.alignment = TextAnchor.UpperCenter;

            // Управление
            var touchGo = new GameObject("TouchInput");
            var touch = touchGo.AddComponent<TouchInput>();

            var left = MakeHoldButton(uiRoot, "BtnLeft", "◀", new Vector2(150f, 150f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(44f, 44f), Hex("#2F5578"));
            var right = MakeHoldButton(uiRoot, "BtnRight", "▶", new Vector2(150f, 150f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(214f, 44f), Hex("#2F5578"));
            touch.left = left.GetComponent<HoldButton>();
            touch.right = right.GetComponent<HoldButton>();

            var jump = MakeTapButton(uiRoot, "BtnJump", "⤒", new Vector2(150f, 150f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 260f), Hex("#2F5578"), 56);
            UnityEventTools.AddVoidPersistentListener(jump.onClick, touch.OnJump);

            var attack = MakeTapButton(uiRoot, "BtnAttack", "УДАР", new Vector2(200f, 200f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-44f, 44f), Hex(Palette.Accent), 42);
            UnityEventTools.AddVoidPersistentListener(attack.onClick, touch.OnAttack);

            var interact = MakeTapButton(uiRoot, "BtnInteract", "ВЗЯТЬ", new Vector2(150f, 150f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-90f, 44f), Hex("#3E6B4A"), 30);
            UnityEventTools.AddVoidPersistentListener(interact.onClick, touch.OnInteract);

            var drop = MakeTapButton(uiRoot, "BtnDrop", "БРОС", new Vector2(130f, 120f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(90f, 44f), Hex("#6B3E3E"), 28);
            UnityEventTools.AddVoidPersistentListener(drop.onClick, touch.OnDrop);

            var reset = MakeTapButton(uiRoot, "BtnReset", "СБРОС", new Vector2(150f, 90f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -92f), Hex("#444B57"), 26);

            var router = player.gameObject.AddComponent<InputRouter>();
            router.target = player;
            router.source = touch;

            // Автопрогон G2/G3
            var testGo = new GameObject("ArenaTest");
            var test = testGo.AddComponent<ArenaTest>();
            test.player = player;
            test.enemy = enemy;
            test.sword = sword;
            test.hammer = hammer;
            test.pistol = pistol;

            // Слушатель должен указывать на метод компонента, лямбда не сериализуется.
            UnityEventTools.AddVoidPersistentListener(reset.onClick, test.ResetScene);

            string path = $"{ScenesDir}/20_Battle_Arena.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static void CreateStrip(string name, Vector2 from, Vector2 to, float width, string colorHex,
            Vector2 colliderOffset, Vector2 colliderSize)
        {
            var go = new GameObject(name);
            go.layer = GameLayers.Ground;
            var strip = go.AddComponent<LineStrip>();
            strip.from = from;
            strip.to = to;
            strip.width = width;
            strip.colorHex = colorHex;

            var col = go.AddComponent<BoxCollider2D>();
            col.offset = colliderOffset;
            col.size = colliderSize;
        }

        private static StickmanCharacter CreateCharacter(string name, Vector3 position, int layer, string colorHex)
        {
            var go = new GameObject(name);
            go.transform.position = position;
            var character = go.AddComponent<StickmanCharacter>();
            character.config = ConfigBuilder.EnsureConfig();
            character.tint = Hex(colorHex);
            character.bodyLayer = layer;
            return character;
        }

        private static Weapon CreateWeapon(string name, WeaponKind kind, Vector3 position)
        {
            var config = ConfigBuilder.EnsureWeapon(kind);

            var go = new GameObject(name) { layer = GameLayers.Weapon };
            go.transform.position = position;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.mass = config.mass;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = config.visualSize;

            var weapon = go.AddComponent<Weapon>();
            weapon.config = config;
            weapon.body = rb;
            weapon.hitbox = col;
            return weapon;
        }

        private static Image MakeBar(Transform parent, string name, Vector2 size, Vector2 anchor, Vector2 pivot,
            Vector2 position, Color fillColor)
        {
            var bgGo = new GameObject(name);
            bgGo.transform.SetParent(parent, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = anchor;
            bgRt.anchorMax = anchor;
            bgRt.pivot = pivot;
            bgRt.sizeDelta = size;
            bgRt.anchoredPosition = position;
            var bg = bgGo.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.15f, 0.19f, 0.95f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero;
            fillRt.anchorMax = Vector2.one;
            fillRt.offsetMin = Vector2.zero;
            fillRt.offsetMax = Vector2.zero;

            var fill = fillGo.AddComponent<Image>();
            fill.color = fillColor;
            fill.type = Image.Type.Filled;
            fill.fillMethod = Image.FillMethod.Horizontal;
            fill.fillAmount = 1f;
            return fill;
        }

        private static void AddLabel(Transform parent, string label, int fontSize, Color color)
        {
            var textGo = new GameObject("Label");
            textGo.transform.SetParent(parent, false);
            var text = textGo.AddComponent<Text>();
            text.font = BuiltinFont();
            text.text = label;
            text.fontSize = fontSize;
            text.fontStyle = FontStyle.Bold;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            var rt = text.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Button MakeTapButton(Transform parent, string name, string label, Vector2 size,
            Vector2 anchor, Vector2 pivot, Vector2 position, Color background, int fontSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            image.color = background;

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            var colors = button.colors;
            colors.pressedColor = new Color(background.r * 0.65f, background.g * 0.65f, background.b * 0.65f, 1f);
            colors.highlightedColor = new Color(
                Mathf.Min(1f, background.r * 1.15f),
                Mathf.Min(1f, background.g * 1.15f),
                Mathf.Min(1f, background.b * 1.15f), 1f);
            button.colors = colors;

            AddLabel(go.transform, label, fontSize, Hex("#10131A"));
            return button;
        }

        private static GameObject MakeHoldButton(Transform parent, string name, string label, Vector2 size,
            Vector2 anchor, Vector2 pivot, Vector2 position, Color background)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = position;

            var image = go.AddComponent<Image>();
            image.color = background;

            go.AddComponent<HoldButton>();
            AddLabel(go.transform, label, 56, Hex("#ECEFF1"));
            return go;
        }

        // ------------------------------------------------------------ TestBed (S1)

        /// <summary>Стенд проверки G1: стойка, падение от импульса, подъём. Собирается кодом.</summary>
        private static string BuildTestBed()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex(Palette.BackgroundArena);
            camGo.transform.position = new Vector3(0f, 2f, -10f);
            camGo.AddComponent<AudioListener>();

            var ground = new GameObject("Ground");
            var strip = ground.AddComponent<LineStrip>();
            strip.from = new Vector2(-9f, 0f);
            strip.to = new Vector2(9f, 0f);
            strip.width = 0.12f;
            strip.colorHex = Palette.Ground;

            var gcol = ground.AddComponent<BoxCollider2D>();
            gcol.size = new Vector2(18f, 0.2f);
            gcol.offset = new Vector2(0f, -0.1f);
            ground.layer = GameLayers.Ground;

            var playerGo = new GameObject("Player");
            playerGo.transform.position = new Vector3(0f, 0.05f, 0f);
            var character = playerGo.AddComponent<StickmanCharacter>();
            character.config = ConfigBuilder.EnsureConfig();
            character.tint = Hex(Palette.Player);
            character.bodyLayer = GameLayers.PlayerBody;

            // --- UI стенда ---
            var canvasGo = new GameObject("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safeGo = new GameObject("SafeRoot", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            var safeRt = (RectTransform)safeGo.transform;
            safeRt.anchorMin = Vector2.zero;
            safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;
            safeGo.AddComponent<StickmanFight.UI.SafeAreaFitter>();
            Transform uiRoot = safeGo.transform;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            var bedGo = new GameObject("TestBed");
            var bed = bedGo.AddComponent<TestBed>();
            bed.character = character;

            var state = MakeText(uiRoot, "State", "STATE: —", 54, Hex(Palette.Text));
            Anchor(state.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f));
            state.rectTransform.pivot = new Vector2(1f, 1f);
            state.rectTransform.sizeDelta = new Vector2(700f, 70f);
            state.rectTransform.anchoredPosition = new Vector2(-48f, -40f);
            state.alignment = TextAnchor.UpperRight;

            var angle = MakeText(uiRoot, "Angle", "ANGLE: —", 42, Hex(Palette.Accent));
            Anchor(angle.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f));
            angle.rectTransform.pivot = new Vector2(1f, 1f);
            angle.rectTransform.sizeDelta = new Vector2(700f, 60f);
            angle.rectTransform.anchoredPosition = new Vector2(-48f, -115f);
            angle.alignment = TextAnchor.UpperRight;

            var hp = MakeText(uiRoot, "Hp", "HP: —", 42, Hex(Palette.Player));
            Anchor(hp.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f));
            hp.rectTransform.pivot = new Vector2(1f, 1f);
            hp.rectTransform.sizeDelta = new Vector2(700f, 60f);
            hp.rectTransform.anchoredPosition = new Vector2(-48f, -180f);
            hp.alignment = TextAnchor.UpperRight;

            var fps = MakeText(uiRoot, "Fps", "— FPS", 34, Hex(Palette.Dummy));
            Anchor(fps.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f));
            fps.rectTransform.pivot = new Vector2(0f, 1f);
            fps.rectTransform.sizeDelta = new Vector2(400f, 50f);
            fps.rectTransform.anchoredPosition = new Vector2(48f, -40f);
            fps.alignment = TextAnchor.UpperLeft;

            var note = MakeText(uiRoot, "Note", "стенд S1", 36, Hex(Palette.Accent));
            Anchor(note.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f));
            note.rectTransform.pivot = new Vector2(0f, 1f);
            note.rectTransform.sizeDelta = new Vector2(900f, 50f);
            note.rectTransform.anchoredPosition = new Vector2(48f, -95f);
            note.alignment = TextAnchor.UpperLeft;

            bed.hudState = state;
            bed.hudAngle = angle;
            bed.hudHp = hp;
            bed.hudFps = fps;
            bed.hudNote = note;

            var b20 = MakeButton(uiRoot, "Btn20", "20", new Vector2(240f, 150f), new Vector2(-48f, 48f));
            UnityEventTools.AddVoidPersistentListener(b20.onClick, bed.Impulse20);

            var b40 = MakeButton(uiRoot, "Btn40", "40", new Vector2(240f, 150f), new Vector2(-308f, 48f));
            UnityEventTools.AddVoidPersistentListener(b40.onClick, bed.Impulse40);

            var bReset = MakeButton(uiRoot, "BtnReset", "СБРОС", new Vector2(260f, 110f), new Vector2(-568f, 68f));
            UnityEventTools.AddVoidPersistentListener(bReset.onClick, bed.ManualReset);

            bed.manualButtons = new[] { b20, b40, bReset };

            string path = $"{ScenesDir}/10_TestBed.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        // ---------------------------------------------------------------- Boot

        private static string BuildBoot()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var cam = CreateCamera();
            CreateGround();
            CreateStickman();

            var hello = CreateUi(out var infoText, out var counterText, out var touchText);

            hello.infoText = infoText;
            hello.counterText = counterText;
            hello.touchText = touchText;

            cam.transform.position = new Vector3(0f, 0f, -10f);

            string path = $"{ScenesDir}/00_Boot.unity";
            EditorSceneManager.SaveScene(scene, path);
            return path;
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera");
            go.tag = "MainCamera";
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Hex(Palette.BackgroundArena);
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 100f;
            go.AddComponent<AudioListener>();
            return cam;
        }

        /// <summary>Земля — линия на y = -3 (в S0 достаточно, полноценный уровень в S5).</summary>
        private static void CreateGround()
        {
            var go = new GameObject("Ground");
            go.transform.position = new Vector3(0f, -3f, 0f);

            var strip = go.AddComponent<LineStrip>();
            strip.from = new Vector2(-9f, 0f);
            strip.to = new Vector2(9f, 0f);
            strip.width = 0.12f;
            strip.colorHex = Palette.Ground;

            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(18f, 0.2f);
            collider.offset = new Vector2(0f, -0.1f);
            go.layer = GameLayers.Ground;
        }

        private static void CreateStickman()
        {
            var go = new GameObject("Stickman");
            go.transform.position = new Vector3(-3.2f, -3f, 0f);
            var fig = go.AddComponent<StickmanFigure>();
            fig.SetColor(Hex(Palette.Player));
        }

        // ------------------------------------------------------------------ UI

        private static HelloStickman CreateUi(out Text infoText, out Text counterText, out Text touchText)
        {
            var canvasGo = new GameObject("UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();

            // Всё содержимое живёт внутри SafeRoot: он подгоняется под safe area (FR-UI-07).
            var safeGo = new GameObject("SafeRoot", typeof(RectTransform));
            safeGo.transform.SetParent(canvasGo.transform, false);
            var safeRt = (RectTransform)safeGo.transform;
            safeRt.anchorMin = Vector2.zero;
            safeRt.anchorMax = Vector2.one;
            safeRt.offsetMin = Vector2.zero;
            safeRt.offsetMax = Vector2.zero;
            safeGo.AddComponent<StickmanFight.UI.SafeAreaFitter>();

            Transform uiRoot = safeGo.transform;

            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<StandaloneInputModule>();

            var gameGo = new GameObject("Game");
            var hello = gameGo.AddComponent<HelloStickman>();

            // Заголовок
            var title = MakeText(uiRoot, "Title", "STICKMAN FIGHT", 96, Hex(Palette.Text));
            Anchor(title.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            title.rectTransform.offsetMin = new Vector2(40f, -190f);
            title.rectTransform.offsetMax = new Vector2(-40f, -60f);
            title.alignment = TextAnchor.MiddleCenter;
            title.fontStyle = FontStyle.Bold;

            var subtitle = MakeText(uiRoot, "Subtitle", "сборка S0 · проверка пайплайна", 34, Hex(Palette.Accent));
            Anchor(subtitle.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f));
            subtitle.rectTransform.offsetMin = new Vector2(40f, -250f);
            subtitle.rectTransform.offsetMax = new Vector2(-40f, -195f);
            subtitle.alignment = TextAnchor.MiddleCenter;

            // Инфо-панель справа
            infoText = MakeText(uiRoot, "Info", "…", 30, Hex(Palette.Text));
            Anchor(infoText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f));
            infoText.rectTransform.pivot = new Vector2(1f, 1f);
            infoText.rectTransform.sizeDelta = new Vector2(360f, 200f);
            infoText.rectTransform.anchoredPosition = new Vector2(-40f, -60f);
            infoText.alignment = TextAnchor.UpperRight;
            infoText.lineSpacing = 1.15f;

            // Счётчик нажатий
            counterText = MakeText(uiRoot, "Counter", "НАЖАТИЙ: 0", 48, Hex(Palette.Accent));
            Anchor(counterText.rectTransform, new Vector2(0f, 0f), new Vector2(0.6f, 0f));
            counterText.rectTransform.pivot = new Vector2(0f, 0f);
            counterText.rectTransform.sizeDelta = new Vector2(700f, 70f);
            counterText.rectTransform.anchoredPosition = new Vector2(56f, 290f);
            counterText.alignment = TextAnchor.LowerLeft;
            counterText.fontStyle = FontStyle.Bold;

            // Кнопка
            var button = MakeButton(uiRoot, "TapButton", "НАЖМИ МЕНЯ", new Vector2(520f, 170f), new Vector2(-56f, 56f));
            UnityEventTools.AddVoidPersistentListener(button.onClick, hello.OnTap);

            // Тач-индикатор
            touchText = MakeText(uiRoot, "Touch", "TOUCH: 0", 26, Hex(Palette.Dummy));
            Anchor(touchText.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f));
            touchText.rectTransform.pivot = new Vector2(0f, 0f);
            touchText.rectTransform.sizeDelta = new Vector2(400f, 40f);
            touchText.rectTransform.anchoredPosition = new Vector2(56f, 40f);
            touchText.alignment = TextAnchor.LowerLeft;

            return hello;
        }

        private static Text MakeText(Transform parent, string name, string content, int size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = BuiltinFont();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static Button MakeButton(Transform parent, string name, string label, Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var rt = go.AddComponent<RectTransform>();
            Anchor(rt, new Vector2(1f, 0f), new Vector2(1f, 0f));
            rt.pivot = new Vector2(1f, 0f);
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            var image = go.AddComponent<Image>();
            image.color = Hex(Palette.Accent);

            var button = go.AddComponent<Button>();
            button.targetGraphic = image;

            var colors = button.colors;
            colors.pressedColor = new Color(0.75f, 0.55f, 0f, 1f);
            colors.highlightedColor = new Color(1f, 0.85f, 0.4f, 1f);
            button.colors = colors;

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = BuiltinFont();
            text.text = label;
            text.fontSize = 46;
            text.fontStyle = FontStyle.Bold;
            text.color = Hex("#10131A");
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            var textRt = text.rectTransform;
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;

            return button;
        }

        private static void Anchor(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = min;
            rt.anchorMax = max;
        }

        private static Font _font;

        private static Font BuiltinFont()
        {
            if (_font != null) return _font;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (_font == null) _font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            return _font;
        }

        private static Color Hex(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }
    }
}
