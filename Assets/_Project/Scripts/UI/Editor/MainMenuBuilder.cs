using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using DreamHacks.UI;
using DreamHacks.Battle;
using DreamHacks.World;
using DreamHacks.PlayerNS;

namespace DreamHacks.UI.Editor
{
    public static class MainMenuBuilder
    {
        [MenuItem("DreamHacks/Generar Maqueta Menu Principal")]
        public static void BuildMainMenuScene()
        {
            // Asegurar cámara principal limpia
            Camera cam = Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                cam = camGo.AddComponent<Camera>();
                camGo.AddComponent<AudioListener>();
            }
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.09f, 0.14f, 1f);
            cam.orthographic = true;

            // Asegurar EventSystem compatible con Input System
            EventSystem es = Object.FindFirstObjectByType<EventSystem>();
            if (es == null)
            {
                var esGo = new GameObject("EventSystem");
                es = esGo.AddComponent<EventSystem>();
                esGo.AddComponent<InputSystemUIInputModule>();
            }

            // Eliminar objetos de intro o cuarto si quedaron en MainMenu por error
            var oldCuarto = GameObject.Find("Contenedor_Cuarto");
            if (oldCuarto != null) Object.DestroyImmediate(oldCuarto);
            var oldIntroCanvas = GameObject.Find("Canvas_IntroCutscene");
            if (oldIntroCanvas != null) Object.DestroyImmediate(oldIntroCanvas);
            var oldShutter = GameObject.Find("Overlay_Cortina8Bit");
            if (oldShutter != null && oldShutter.transform.parent == null) Object.DestroyImmediate(oldShutter);

            // Eliminar maqueta previa si se vuelve a ejecutar
            var oldCanvas = GameObject.Find("Canvas_MenuPrincipal");
            if (oldCanvas != null)
            {
                Object.DestroyImmediate(oldCanvas);
            }

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Cargar Sprites Pixel Art del Menú
            var fondoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Menu/Spr_FondoMenu.png");
            var logoPrismaSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Menu/Spr_LogoPrisma.png");
            var tituloLogoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Menu/Spr_TituloShatteredPrism.png");
            var btnJugarSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Menu/Spr_BtnJugar.png");
            var btnCreditosSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Menu/Spr_BtnCreditos.png");
            var btnItchIoSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Menu/Spr_BtnItchIo.png");
            var menuMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/Mus_MenuPrincipal.mp3");

            // 1. Canvas raíz (1920x1080 encajado en el recuadro de la Main Camera)
            var canvasGo = new GameObject("Canvas_MenuPrincipal");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            var menuController = canvasGo.AddComponent<MainMenuUI>();
            canvasGo.AddComponent<MenuIntroShutter8Bit>();

            // 2. Fondo Pixel Art (Spr_FondoMenu)
            var bgGo = CreateUIObject("BG_ArteFondo", canvasGo.transform);
            StretchFull(bgGo.GetComponent<RectTransform>());
            var bgImage = bgGo.AddComponent<Image>();
            bgImage.sprite = fondoSprite;
            bgImage.color = Color.white;
            bgImage.preserveAspect = false;

            // 3. Panel Principal (Título/Logo + Botones)
            var mainPanelGo = CreateUIObject("Panel_MenuPrincipal", canvasGo.transform);
            StretchFull(mainPanelGo.GetComponent<RectTransform>());

            // 3a. Prisma Central de Shattered Prism
            var logoGo = CreateUIObject("Img_LogoJuego", mainPanelGo.transform);
            var logoRt = logoGo.GetComponent<RectTransform>();
            logoRt.anchorMin = new Vector2(0.5f, 0.5f);
            logoRt.anchorMax = new Vector2(0.5f, 0.5f);
            logoRt.pivot = new Vector2(0.5f, 0.5f);
            logoRt.anchoredPosition = new Vector2(0f, 280f);
            logoRt.sizeDelta = new Vector2(220f, 286f);
            var logoImage = logoGo.AddComponent<Image>();
            logoImage.sprite = logoPrismaSprite;
            logoImage.color = Color.white;
            logoImage.preserveAspect = true;

            // Título Shattered Prism (multicolor pixel art)
            var titleImgGo = CreateUIObject("Img_TituloTexto", logoGo.transform);
            var titleRt = titleImgGo.GetComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0.5f, 0f);
            titleRt.anchorMax = new Vector2(0.5f, 0f);
            titleRt.pivot = new Vector2(0.5f, 0.5f);
            titleRt.anchoredPosition = new Vector2(0f, -35f);
            titleRt.sizeDelta = new Vector2(640f, 105f);
            var titleImg = titleImgGo.AddComponent<Image>();
            titleImg.sprite = tituloLogoSprite;
            titleImg.color = Color.white;
            titleImg.preserveAspect = true;

            // 3b. Contenedor de Botones (Jugar, Créditos, Salir)
            var buttonsContainerGo = CreateUIObject("Contenedor_Botones", mainPanelGo.transform);
            var buttonsRt = buttonsContainerGo.GetComponent<RectTransform>();
            buttonsRt.anchorMin = new Vector2(0.5f, 0.5f);
            buttonsRt.anchorMax = new Vector2(0.5f, 0.5f);
            buttonsRt.pivot = new Vector2(0.5f, 0.5f);
            buttonsRt.anchoredPosition = new Vector2(0f, -150f);
            buttonsRt.sizeDelta = new Vector2(380f, 330f);

            var vlg = buttonsContainerGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 20f;
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            Button playBtn = CreatePixelMenuButton(
                "Btn_Jugar",
                btnJugarSprite,
                buttonsContainerGo.transform,
                78f
            );

            Button creditsBtn = CreatePixelMenuButton(
                "Btn_Creditos",
                btnCreditosSprite,
                buttonsContainerGo.transform,
                78f
            );

            Button exitBtn = CreatePixelMenuButton(
                "Btn_Salir",
                btnItchIoSprite,
                buttonsContainerGo.transform,
                78f
            );

            // 4. Panel de Créditos (Modal oculto al inicio)
            var creditsPanelGo = CreateUIObject("Panel_Creditos", canvasGo.transform);
            StretchFull(creditsPanelGo.GetComponent<RectTransform>());
            var creditsOverlayImg = creditsPanelGo.AddComponent<Image>();
            creditsOverlayImg.color = new Color(0.04f, 0.05f, 0.09f, 0.88f);

            var creditsCardGo = CreateUIObject("Ventana_Creditos", creditsPanelGo.transform);
            var creditsCardRt = creditsCardGo.GetComponent<RectTransform>();
            creditsCardRt.anchorMin = new Vector2(0.5f, 0.5f);
            creditsCardRt.anchorMax = new Vector2(0.5f, 0.5f);
            creditsCardRt.pivot = new Vector2(0.5f, 0.5f);
            creditsCardRt.anchoredPosition = Vector2.zero;
            creditsCardRt.sizeDelta = new Vector2(960f, 680f);
            var creditsCardImg = creditsCardGo.AddComponent<Image>();
            creditsCardImg.color = new Color(0.13f, 0.16f, 0.25f, 1f);

            var creditsHeaderGo = CreateUIObject("Txt_TituloCreditos", creditsCardGo.transform);
            var creditsHeaderRt = creditsHeaderGo.GetComponent<RectTransform>();
            creditsHeaderRt.anchorMin = new Vector2(0.5f, 1f);
            creditsHeaderRt.anchorMax = new Vector2(0.5f, 1f);
            creditsHeaderRt.pivot = new Vector2(0.5f, 1f);
            creditsHeaderRt.anchoredPosition = new Vector2(0f, -32f);
            creditsHeaderRt.sizeDelta = new Vector2(800f, 60f);
            var creditsHeaderTxt = creditsHeaderGo.AddComponent<Text>();
            creditsHeaderTxt.font = defaultFont;
            creditsHeaderTxt.text = "CRÉDITOS — SHATTERED PRISM";
            creditsHeaderTxt.fontSize = 34;
            creditsHeaderTxt.fontStyle = FontStyle.Bold;
            creditsHeaderTxt.alignment = TextAnchor.MiddleCenter;
            creditsHeaderTxt.color = Color.white;

            var creditsBodyGo = CreateUIObject("Txt_NombresEquipo", creditsCardGo.transform);
            var creditsBodyRt = creditsBodyGo.GetComponent<RectTransform>();
            creditsBodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            creditsBodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            creditsBodyRt.pivot = new Vector2(0.5f, 0.5f);
            creditsBodyRt.anchoredPosition = new Vector2(0f, 12f);
            creditsBodyRt.sizeDelta = new Vector2(900f, 520f);
            var creditsBodyTxt = creditsBodyGo.AddComponent<Text>();
            creditsBodyTxt.font = defaultFont;
            creditsBodyTxt.supportRichText = true;
            creditsBodyTxt.verticalOverflow = VerticalWrapMode.Overflow;
            creditsBodyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;
            creditsBodyTxt.lineSpacing = 1.05f;
            creditsBodyTxt.text =
                "<b>PROGRAMACIÓN</b>\n" +
                "Gino Viloria : Líder de programación\n" +
                "Ramiro Martínez : Soporte de programación\n" +
                "Isaac Uribe : Soporte de programación\n" +
                "Duvan Altamar : Soporte de programación\n\n" +
                "<b>ARTE Y DIRECCIÓN VISUAL</b>\n" +
                "Lahuris Vivero : Diseñadora de personajes principales y sprites\n" +
                "Isaac Uribe : Diseñador de enemigos y sprites\n\n" +
                "<b>DISEÑO DE NIVELES Y NARRATIVA</b>\n" +
                "Ramiro Martínez : Historia, diálogos y estadísticas de personajes\n" +
                "Samuel Barrios : Diseño de niveles y fondos\n" +
                "Duvan Altamar : Escritor de diálogos e historia del final bueno\n\n" +
                "<b>MÚSICA Y SFX</b>\n" +
                "Emmanuel Pardey\n\n" +
                "<i>Shattered Prism — Desarrollado durante DreamHacks</i>";
            creditsBodyTxt.fontSize = 18;
            creditsBodyTxt.alignment = TextAnchor.MiddleCenter;
            creditsBodyTxt.color = new Color(0.88f, 0.90f, 0.96f, 1f);

            Button closeCreditsBtn = CreateMenuButton(
                "Btn_CerrarCreditos",
                "VOLVER",
                creditsCardGo.transform,
                defaultFont,
                new Color(0.22f, 0.51f, 0.96f, 1f),
                64f,
                26
            );
            var closeRt = closeCreditsBtn.GetComponent<RectTransform>();
            closeRt.anchorMin = new Vector2(0.5f, 0f);
            closeRt.anchorMax = new Vector2(0.5f, 0f);
            closeRt.pivot = new Vector2(0.5f, 0f);
            closeRt.anchoredPosition = new Vector2(0f, 28f);
            closeRt.sizeDelta = new Vector2(280f, 64f);

            creditsPanelGo.SetActive(false);

            // 5. Conectar todas las referencias en MainMenuUI
            var so = new SerializedObject(menuController);
            // Conectar música del menú
            var menuAudioSource = canvasGo.AddComponent<AudioSource>();
            menuAudioSource.clip = menuMusicClip;
            menuAudioSource.loop = true;
            menuAudioSource.playOnAwake = true;
            menuAudioSource.volume = 0.8f;

            so.FindProperty("musicAudioSource").objectReferenceValue = menuAudioSource;
            so.FindProperty("menuMusicClip").objectReferenceValue = menuMusicClip;
            so.FindProperty("musicVolume").floatValue = 0.8f;
            so.FindProperty("musicFadeOutDuration").floatValue = 1.5f;
            so.FindProperty("gameSceneName").stringValue = "IntroCutscene";
            so.FindProperty("mainPanel").objectReferenceValue = mainPanelGo;
            so.FindProperty("creditsPanel").objectReferenceValue = creditsPanelGo;
            so.FindProperty("playButton").objectReferenceValue = playBtn;
            so.FindProperty("creditsButton").objectReferenceValue = creditsBtn;
            so.FindProperty("exitButton").objectReferenceValue = exitBtn;
            so.FindProperty("closeCreditsButton").objectReferenceValue = closeCreditsBtn;
            so.FindProperty("backgroundImage").objectReferenceValue = bgImage;
            so.FindProperty("logoImage").objectReferenceValue = logoImage;
            so.ApplyModifiedPropertiesWithoutUndo();

            // 6. Guardar la escena en Assets/_Project/Scenes/MainMenu.unity
            string scenePath = "Assets/_Project/Scenes/MainMenu.unity";
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene(), scenePath);

            // Asegurar que MainMenu esté en Build Settings
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!buildScenes.Exists(s => s.path == scenePath))
            {
                buildScenes.Insert(0, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            Debug.Log("[MainMenuBuilder] Maqueta de Shattered Prism creada y guardada en " + scenePath);
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void StretchFull(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static Button CreatePixelMenuButton(
            string objectName,
            Sprite sprite,
            Transform parent,
            float height)
        {
            var btnGo = CreateUIObject(objectName, parent);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(380f, height);

            var img = btnGo.AddComponent<Image>();
            img.sprite = sprite;
            img.color = Color.white;
            img.preserveAspect = true;

            var layoutElem = btnGo.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = height;
            layoutElem.minHeight = height;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            btn.colors = colors;

            // Animación 8-bit de vaivén sincronizado y elevación al hover
            btnGo.AddComponent<MenuButton8BitAnim>();

            return btn;
        }

        private static Button CreateMenuButton(
            string objectName,
            string label,
            Transform parent,
            Font font,
            Color baseColor,
            float height,
            int fontSize)
        {
            var btnGo = CreateUIObject(objectName, parent);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(460f, height);

            var img = btnGo.AddComponent<Image>();
            img.color = baseColor;

            var layoutElem = btnGo.AddComponent<LayoutElement>();
            layoutElem.preferredHeight = height;
            layoutElem.minHeight = height;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;

            ColorBlock colors = btn.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = new Color(1.1f, 1.1f, 1.1f, 1f);
            btn.colors = colors;

            var txtGo = CreateUIObject("Txt_Label", btnGo.transform);
            StretchFull(txtGo.GetComponent<RectTransform>());
            var txt = txtGo.AddComponent<Text>();
            txt.font = font;
            txt.text = label;
            txt.fontSize = fontSize;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;

            return btn;
        }

        [MenuItem("DreamHacks/Generar Escena Intro Cutscene")]
        public static void BuildIntroCutsceneScene()
        {
            string scenePath = "Assets/_Project/Scenes/IntroCutscene.unity";

            var newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // 1. Cámara principal
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 0f, -10f);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.04f, 0.05f, 0.08f, 1f);
            cam.orthographic = true;
            cam.orthographicSize = 5.15f;
            camGo.AddComponent<AudioListener>();

            // 2. EventSystem
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();

            Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // Cargar Sprites de las dos habitaciones, retratos y transición binaria
            var roomSleepingSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Intro/Spr_CuartoProtagonista.jpg");
            var roomMessyBedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Intro/Spr_CuartoCamaDesordenada.jpg");
            var roomMadeBedSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Intro/Spr_CuartoCamaOrdenada.png");
            var livingRoomNormalSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Intro/Spr_SalaCocina.png");
            var livingRoomGlitchSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Intro/Spr_SalaCocina_PuertaGlitch.png");
            var binaryTransitionSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Intro/Spr_TransicionBinaria.png");
            var phoneSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Intro/Spr_TelefonoMano.png");
            var ikerSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Intro/Spr_RetratoIker.png");

            // Cargar Clips de Audio
            var vibrationClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/Sfx_CelularVibrando.wav");
            var apartmentMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/Mus_Apartamento.mp3");
            var makeBedClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/Sfx_HacerCama.wav");
            var makeBreakfastClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/Sfx_PrepararDesayuno.wav");
            var waterPlantClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/SFX/Sfx_RegarPlanta.wav");
            var glitchDoorMusicClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/_Project/Audio/Music/Mus_PuertaGlitch.wav");

            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");

            // 3A. Habitación 1: Cuarto (Dormitorio) en el Mundo 2D
            var roomWorldGo = new GameObject("Cuarto_Mundo");
            roomWorldGo.transform.position = Vector3.zero;
            var roomSr = roomWorldGo.AddComponent<SpriteRenderer>();
            roomSr.sprite = roomSleepingSprite;
            roomSr.sortingOrder = 0;

            CreateRoomCollider("Pared_Derecha", roomWorldGo.transform, new Vector2(3.05f, 0f), new Vector2(0.4f, 10.8f));
            CreateRoomCollider("Pared_Superior", roomWorldGo.transform, new Vector2(0f, 4.8f), new Vector2(7.2f, 1.2f));
            CreateRoomCollider("Pared_Inferior", roomWorldGo.transform, new Vector2(0f, -5.2f), new Vector2(7.2f, 1.2f));
            CreateRoomCollider("Pared_Izquierda_Arriba", roomWorldGo.transform, new Vector2(-3.05f, 2.0f), new Vector2(0.4f, 4.8f));
            CreateRoomCollider("Pared_Izquierda_Abajo", roomWorldGo.transform, new Vector2(-3.05f, -4.2f), new Vector2(0.4f, 2.4f));
            CreateRoomCollider("Cama_Colision", roomWorldGo.transform, new Vector2(-1.4f, 2.4f), new Vector2(2.1f, 3.0f));
            CreateRoomCollider("Armario_Colision", roomWorldGo.transform, new Vector2(1.9f, 2.8f), new Vector2(1.6f, 2.4f));
            CreateRoomCollider("Mesita_Colision", roomWorldGo.transform, new Vector2(0.7f, 2.6f), new Vector2(0.8f, 1.4f));

            // 3B. Habitación 2: Sala / Cocina en el Mundo 2D (inactiva al inicio)
            var livingWorldGo = new GameObject("SalaCocina_Mundo");
            livingWorldGo.transform.position = Vector3.zero;
            var livingSr = livingWorldGo.AddComponent<SpriteRenderer>();
            livingSr.sprite = livingRoomNormalSprite;
            livingSr.sortingOrder = 0;

            // Paredes exteriores de la Sala/Cocina (dejando libre la puerta derecha hacia el cuarto en y: -2.65..-0.85)
            CreateRoomCollider("Sala_Pared_Izquierda", livingWorldGo.transform, new Vector2(-4.85f, 0f), new Vector2(0.55f, 9.2f));
            CreateRoomCollider("Sala_Pared_Superior", livingWorldGo.transform, new Vector2(0f, 3.65f), new Vector2(10.5f, 1.4f));
            CreateRoomCollider("Sala_Pared_Inferior", livingWorldGo.transform, new Vector2(0f, -4.15f), new Vector2(10.5f, 0.55f));
            CreateRoomCollider("Sala_Pared_Derecha_Arriba", livingWorldGo.transform, new Vector2(4.85f, 1.85f), new Vector2(0.55f, 5.4f));
            CreateRoomCollider("Sala_Pared_Derecha_Abajo", livingWorldGo.transform, new Vector2(4.85f, -3.55f), new Vector2(0.55f, 1.8f));

            // Muebles de la Sala/Cocina
            CreateRoomCollider("Cocina_Meson_Nevera_Estufa", livingWorldGo.transform, new Vector2(-2.2f, 2.85f), new Vector2(3.9f, 1.55f));
            CreateRoomCollider("Ventana_Planta_Pared", livingWorldGo.transform, new Vector2(1.35f, 3.35f), new Vector2(2.2f, 0.85f));
            CreateRoomCollider("Mueble_TV", livingWorldGo.transform, new Vector2(3.29f, 2.85f), new Vector2(1.82f, 1.35f));
            CreateRoomCollider("Mesa_Centro_Alfombra", livingWorldGo.transform, new Vector2(3.17f, 0.85f), new Vector2(1.1f, 0.7f));
            CreateRoomCollider("Sofa_Rosado", livingWorldGo.transform, new Vector2(3.16f, -0.81f), new Vector2(2.05f, 1.05f));
            CreateRoomCollider("Lampara_Sala", livingWorldGo.transform, new Vector2(1.72f, -0.59f), new Vector2(0.5f, 0.5f));
            CreateRoomCollider("Mesa_Comedor", livingWorldGo.transform, new Vector2(-2.69f, -1.41f), new Vector2(1.42f, 2.0f));

            livingWorldGo.SetActive(false);

            // 3C. Mapa 3: Bosque Colorido en el Mundo 2D (inactivo al inicio)
            var forestMapSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Forest/Spr_BosqueColorido.png");
            var slimeIdle0 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Forest/Spr_Slime_0.png");
            var slimeIdle1 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Forest/Spr_Slime_1.png");
            var slimeIdle2 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Forest/Spr_Slime_2.png");
            var slimeAtk5 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Forest/Spr_Slime_5.png");
            var slimeAtk6 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Forest/Spr_Slime_6.png");
            var slimeAtk7 = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Forest/Spr_Slime_7.png");
            var swordSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Forest/Spr_Espada.png");
            var fullHeartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/Hearts/corazon_lleno.png");
            var emptyHeartSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/UI/Hearts/corazon_vacio.png");

            // Sprites de Hannah
            var hannahPortrait = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Characters/Hannah/hannah_portrait.png");
            var hannahFrontal = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Characters/Hannah/HANNAH_FRONTAL.png");
            var hannahDetras = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Characters/Hannah/HANNAH_DETRAS.png");
            var hannahIzq = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Characters/Hannah/HANNAH_IZQUIERDA.png");
            var hannahDer = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Characters/Hannah/HANNAH_DERECHA.png");
            var hannahAfk = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Characters/Hannah/HANNAH_AFK.png");

            // Sprites de Enemigos
            var goblinIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Enemies/Goblin/Goblin_idle_v2.png");
            var skelIdle = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Enemies/Skel/Skel_idle_v2.png");

            // Sprites de Puzzles
            var rockSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Props/Puzzles/rock_forest.png");
            var switchOffSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Props/Puzzles/switch_off_forest.png");
            var switchOnSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/_Project/Art/Props/Puzzles/switch_on_forest.png");

            var forestWorldGo = new GameObject("Bosque_Mundo");
            forestWorldGo.transform.position = Vector3.zero;
            forestWorldGo.transform.localScale = new Vector3(2f, 2f, 1f);
            var forestSr = forestWorldGo.AddComponent<SpriteRenderer>();
            forestSr.sprite = forestMapSprite;
            forestSr.sortingOrder = 0;

            // 1. Límites perimetrales del bosque
            CreateRoomCollider("Bosque_Borde_Superior", forestWorldGo.transform, new Vector2(0f, 2.40f), new Vector2(10.24f, 0.35f));
            CreateRoomCollider("Bosque_Borde_Izquierdo", forestWorldGo.transform, new Vector2(-4.96f, -0.16f), new Vector2(0.35f, 3.20f));
            CreateRoomCollider("Bosque_Borde_Derecho", forestWorldGo.transform, new Vector2(4.80f, -0.80f), new Vector2(0.65f, 3.60f));
            CreateRoomCollider("Bosque_Borde_Inferior_Izq", forestWorldGo.transform, new Vector2(-3.28f, -2.40f), new Vector2(3.68f, 0.35f));
            CreateRoomCollider("Bosque_Borde_Inferior_Der", forestWorldGo.transform, new Vector2(1.92f, -2.40f), new Vector2(2.56f, 0.35f));

            // 2. Nicho superior derecho donde aparece Iker
            CreateRoomCollider("Bosque_Nicho_Techo", forestWorldGo.transform, new Vector2(3.06f, 1.95f), new Vector2(1.10f, 0.30f));
            CreateRoomCollider("Bosque_Nicho_Pared_Izq", forestWorldGo.transform, new Vector2(2.42f, 1.35f), new Vector2(0.85f, 0.70f));
            CreateRoomCollider("Bosque_Nicho_Pared_Der", forestWorldGo.transform, new Vector2(3.78f, 1.35f), new Vector2(0.85f, 0.70f));

            // 3. Fila superior de islas de árboles
            CreateRoomCollider("Bosque_Isla_Sup_Izq_Borde", forestWorldGo.transform, new Vector2(-4.72f, 1.28f), new Vector2(0.80f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Sup_Izq_Larga", forestWorldGo.transform, new Vector2(-2.92f, 1.30f), new Vector2(1.92f, 0.52f));
            CreateRoomCollider("Bosque_Isla_Sup_Centro_Larga", forestWorldGo.transform, new Vector2(-0.16f, 1.30f), new Vector2(2.56f, 0.52f));
            CreateRoomCollider("Bosque_Isla_Sup_Der_Borde", forestWorldGo.transform, new Vector2(4.24f, 1.28f), new Vector2(1.76f, 0.64f));

            // 4. Fila media de islas de árboles
            CreateRoomCollider("Bosque_Isla_Med_Izq_Borde", forestWorldGo.transform, new Vector2(-4.56f, 0.00f), new Vector2(0.48f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Med_Izq_Larga", forestWorldGo.transform, new Vector2(-3.20f, 0.00f), new Vector2(1.28f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Med_Centro_Larga", forestWorldGo.transform, new Vector2(0.32f, 0.00f), new Vector2(1.92f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Med_Der_Larga", forestWorldGo.transform, new Vector2(2.64f, 0.00f), new Vector2(1.76f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Med_Der_Borde", forestWorldGo.transform, new Vector2(4.24f, 0.00f), new Vector2(0.48f, 0.64f));

            // 5. Arco / Herradura inferior central (arbustos que enmarcan la entrada inferior)
            CreateRoomCollider("Bosque_Arco_Inf_Izq", forestWorldGo.transform, new Vector2(-1.25f, -1.25f), new Vector2(0.65f, 2.30f));
            CreateRoomCollider("Bosque_Arco_Inf_Techo", forestWorldGo.transform, new Vector2(-0.60f, -0.16f), new Vector2(1.40f, 0.32f));
            CreateRoomCollider("Bosque_Arco_Inf_Der", forestWorldGo.transform, new Vector2(0.00f, -1.44f), new Vector2(1.28f, 2.24f));

            // 6. Fila inferior de islas de árboles
            CreateRoomCollider("Bosque_Isla_Inf_Izq_Borde", forestWorldGo.transform, new Vector2(-4.56f, -1.28f), new Vector2(0.48f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Inf_Izq_Larga", forestWorldGo.transform, new Vector2(-2.96f, -1.28f), new Vector2(1.76f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Inf_Centro_Izq", forestWorldGo.transform, new Vector2(0.96f, -1.28f), new Vector2(0.64f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Inf_Centro_Der", forestWorldGo.transform, new Vector2(2.16f, -1.28f), new Vector2(0.80f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Inf_Der_Larga", forestWorldGo.transform, new Vector2(3.76f, -1.28f), new Vector2(1.44f, 0.64f));
            CreateRoomCollider("Bosque_Isla_Inf_Der_Baja", forestWorldGo.transform, new Vector2(3.84f, -2.08f), new Vector2(1.28f, 0.96f));

            // Slime frente al nicho de salida
            var slimeGo = new GameObject("Slime_Bosque");
            slimeGo.transform.SetParent(forestWorldGo.transform, false);
            slimeGo.transform.position = new Vector3(6.12f, 1.15f, 0f);
            slimeGo.transform.localScale = new Vector3(0.68f, 0.68f, 1f);
            var slimeSr = slimeGo.AddComponent<SpriteRenderer>();
            slimeSr.sprite = slimeIdle0;
            slimeSr.sortingOrder = 4;

            // Trigger para iniciar combate por turnos (escena Battle) al colisionar con el Slime
            var slimeTriggerGo = new GameObject("Trigger");
            slimeTriggerGo.transform.SetParent(slimeGo.transform, false);
            var slimeTriggerCol = slimeTriggerGo.AddComponent<CircleCollider2D>();
            slimeTriggerCol.isTrigger = true;
            slimeTriggerCol.radius = 0.65f;
            var encTrigger = slimeTriggerGo.AddComponent<EncounterTrigger>();
            var encSo = new SerializedObject(encTrigger);
            var enemyIdProp = encSo.FindProperty("enemyId");
            if (enemyIdProp != null) enemyIdProp.stringValue = "Slime";
            var battleSceneProp = encSo.FindProperty("battleScene");
            if (battleSceneProp != null) battleSceneProp.stringValue = "Battle";
            encSo.ApplyModifiedPropertiesWithoutUndo();

            // Espada Flotante (inicialmente inactiva; aparece arriba de Iker y luego Iker la absorbe)
            var swordGo = new GameObject("Espada_Flotante");
            swordGo.transform.SetParent(forestWorldGo.transform, false);
            swordGo.transform.position = new Vector3(6.12f, 4.05f, 0f);
            var swordSr = swordGo.AddComponent<SpriteRenderer>();
            swordSr.sprite = swordSprite;
            swordSr.sortingOrder = 8;
            swordGo.SetActive(false);

            // Hannah (compañera que aparece tras derrotar al Slime inicial)
            var hannahGo = new GameObject("Hannah");
            hannahGo.transform.SetParent(forestWorldGo.transform, false);
            hannahGo.transform.position = new Vector3(6.85f, 1.15f, 0f);
            hannahGo.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
            var hannahSr = hannahGo.AddComponent<SpriteRenderer>();
            hannahSr.sprite = hannahFrontal;
            hannahSr.sortingOrder = 4;
            var hannahFollower = hannahGo.AddComponent<HannahFollower>();
            hannahFollower.SetSprites(hannahFrontal, hannahDetras, hannahIzq, hannahDer, hannahAfk);
            hannahGo.SetActive(false);

            // Contenedor de 8 enemigos distribuidos equitativamente
            var enemiesContainer = new GameObject("Enemigos_Bosque");
            enemiesContainer.transform.SetParent(forestWorldGo.transform, false);

            System.Action<string, string, Sprite, Vector3> createEnemy = (eName, enemyId, spr, worldPos) =>
            {
                var enGo = new GameObject(eName);
                enGo.transform.SetParent(enemiesContainer.transform, false);
                enGo.transform.position = worldPos;
                enGo.transform.localScale = new Vector3(0.65f, 0.65f, 1f);
                var sr = enGo.AddComponent<SpriteRenderer>();
                sr.sprite = spr;
                sr.sortingOrder = 4;

                var trGo = new GameObject("Trigger");
                trGo.transform.SetParent(enGo.transform, false);
                var col = trGo.AddComponent<CircleCollider2D>();
                col.isTrigger = true;
                col.radius = 0.55f;

                var trigger = trGo.AddComponent<EncounterTrigger>();
                var s = new SerializedObject(trigger);
                var eProp = s.FindProperty("enemyId");
                if (eProp != null) eProp.stringValue = enemyId;
                var bProp = s.FindProperty("battleScene");
                if (bProp != null) bProp.stringValue = "Battle";
                s.ApplyModifiedPropertiesWithoutUndo();
            };

            // 8 Enemigos distribuidos por los senderos del bosque
            createEnemy("Enemigo_Goblin_1", "Goblin", goblinIdle, new Vector3(-6.5f, 1.3f, 0f));
            createEnemy("Enemigo_Goblin_2", "Goblin", goblinIdle, new Vector3(2.6f, 1.3f, 0f));
            createEnemy("Enemigo_Goblin_3", "Goblin", goblinIdle, new Vector3(-7.2f, -1.3f, 0f));
            createEnemy("Enemigo_Skel_1", "Skel", skelIdle, new Vector3(-3.2f, 3.6f, 0f));
            createEnemy("Enemigo_Skel_2", "Skel", skelIdle, new Vector3(7.2f, -1.3f, 0f));
            createEnemy("Enemigo_Skel_3", "Skel", skelIdle, new Vector3(-4.0f, -3.6f, 0f));
            createEnemy("Enemigo_Slime_1", "Slime", slimeIdle0, new Vector3(0.0f, 3.6f, 0f));
            createEnemy("Enemigo_Slime_2", "Slime", slimeIdle0, new Vector3(3.6f, -3.6f, 0f));

            // Puzzle 1: Roca Empujable
            var rockGo = new GameObject("Roca_Bosque");
            rockGo.transform.SetParent(forestWorldGo.transform, false);
            rockGo.transform.position = new Vector3(-3.2f, 1.3f, 0f);
            rockGo.transform.localScale = new Vector3(0.6f, 0.6f, 1f);
            var rockSr = rockGo.AddComponent<SpriteRenderer>();
            rockSr.sprite = rockSprite;
            rockSr.sortingOrder = 5;
            var rockCol = rockGo.AddComponent<BoxCollider2D>();
            rockCol.size = new Vector2(1.4f, 0.9f);
            var pushableRock = rockGo.AddComponent<PushableRock>();

            // Salida del bosque (barrera que se abre con el interruptor)
            var exitBarrierGo = new GameObject("Barrera_Salida_Bosque");
            exitBarrierGo.transform.SetParent(forestWorldGo.transform, false);
            exitBarrierGo.transform.position = new Vector3(-1.2f, -4.8f, 0f);
            var exitCol = exitBarrierGo.AddComponent<BoxCollider2D>();
            exitCol.size = new Vector2(2.5f, 0.8f);

            // Puzzle 2: Interruptor del Bosque
            var switchGo = new GameObject("Interruptor_Bosque");
            switchGo.transform.SetParent(forestWorldGo.transform, false);
            switchGo.transform.position = new Vector3(-7.2f, -3.6f, 0f);
            switchGo.transform.localScale = new Vector3(0.7f, 0.7f, 1f);
            var switchSr = switchGo.AddComponent<SpriteRenderer>();
            switchSr.sprite = switchOffSprite;
            switchSr.sortingOrder = 3;
            var switchCol = switchGo.AddComponent<CircleCollider2D>();
            switchCol.isTrigger = true;
            switchCol.radius = 0.65f;
            var forestSwitch = switchGo.AddComponent<ForestSwitch>();
            var switchSo = new SerializedObject(forestSwitch);
            var sOffProp = switchSo.FindProperty("switchOffSprite");
            if (sOffProp != null) sOffProp.objectReferenceValue = switchOffSprite;
            var sOnProp = switchSo.FindProperty("switchOnSprite");
            if (sOnProp != null) sOnProp.objectReferenceValue = switchOnSprite;
            var barrierProp = switchSo.FindProperty("exitBarrierObject");
            if (barrierProp != null) barrierProp.objectReferenceValue = exitBarrierGo;
            switchSo.ApplyModifiedPropertiesWithoutUndo();

            forestWorldGo.SetActive(false);

            // 4. Instanciar Jugador en el Apartamento (inicialmente inactivo)
            GameObject playerGo = null;
            if (playerPrefab != null)
            {
                playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
                playerGo.name = "Player";
                playerGo.transform.position = new Vector3(-0.8f, 0.4f, 0f);
                var psr = playerGo.GetComponent<SpriteRenderer>();
                if (psr != null) psr.sortingOrder = 5;
                playerGo.SetActive(false);
            }

            // 5. Canvas principal
            var canvasGo = new GameObject("Canvas_IntroCutscene");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 5f;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
            var cutsceneCtrl = canvasGo.AddComponent<IntroCutsceneController>();

            // Fuentes de Audio
            var sfxAudioSource = canvasGo.AddComponent<AudioSource>();
            sfxAudioSource.playOnAwake = false;
            sfxAudioSource.loop = false;
            sfxAudioSource.spatialBlend = 0f;
            sfxAudioSource.volume = 1f;

            var musicAudioSource = canvasGo.AddComponent<AudioSource>();
            musicAudioSource.playOnAwake = false;
            musicAudioSource.loop = true;
            musicAudioSource.spatialBlend = 0f;
            musicAudioSource.volume = 0.85f;

            // 6A. Panel de Tareas (Esquina Superior Derecha)
            var tasksPanelGo = CreateUIObject("Panel_Tareas", canvasGo.transform);
            var tasksPanelRt = tasksPanelGo.GetComponent<RectTransform>();
            tasksPanelRt.anchorMin = new Vector2(1f, 1f);
            tasksPanelRt.anchorMax = new Vector2(1f, 1f);
            tasksPanelRt.pivot = new Vector2(1f, 1f);
            tasksPanelRt.anchoredPosition = new Vector2(-28f, -24f);
            tasksPanelRt.sizeDelta = new Vector2(390f, 178f);

            var tasksBgImg = tasksPanelGo.AddComponent<Image>();
            tasksBgImg.color = new Color(0.06f, 0.08f, 0.14f, 0.90f);
            var tasksOutline = tasksPanelGo.AddComponent<Outline>();
            tasksOutline.effectColor = new Color(0.32f, 0.42f, 0.65f, 0.85f);
            tasksOutline.effectDistance = new Vector2(2f, -2f);

            var tasksVlg = tasksPanelGo.AddComponent<VerticalLayoutGroup>();
            tasksVlg.padding = new RectOffset(18, 18, 14, 14);
            tasksVlg.spacing = 8f;
            tasksVlg.childAlignment = TextAnchor.UpperLeft;
            tasksVlg.childControlWidth = true;
            tasksVlg.childControlHeight = false;
            tasksVlg.childForceExpandWidth = true;
            tasksVlg.childForceExpandHeight = false;

            var tasksHeaderGo = CreateUIObject("Txt_TituloTareas", tasksPanelGo.transform);
            tasksHeaderGo.GetComponent<RectTransform>().sizeDelta = new Vector2(350f, 28f);
            var tasksHeaderTxt = tasksHeaderGo.AddComponent<Text>();
            tasksHeaderTxt.font = defaultFont;
            tasksHeaderTxt.text = "TAREAS";
            tasksHeaderTxt.fontSize = 20;
            tasksHeaderTxt.fontStyle = FontStyle.Bold;
            tasksHeaderTxt.color = new Color(1f, 0.88f, 0.36f, 1f);
            tasksHeaderTxt.alignment = TextAnchor.MiddleLeft;

            string[] initialTasks = new string[]
            {
                "1. Hacer la cama",
                "2. Preparar el desayuno",
                "3. Regar la planta."
            };

            GameObject[] taskRowObjects = new GameObject[3];
            Text[] taskItemTexts = new Text[3];
            GameObject[] taskStrikeBars = new GameObject[3];

            for (int i = 0; i < 3; i++)
            {
                var rowGo = CreateUIObject($"Tarea_Item_{i + 1}", tasksPanelGo.transform);
                var rowRt = rowGo.GetComponent<RectTransform>();
                rowRt.sizeDelta = new Vector2(350f, 30f);
                rowGo.AddComponent<CanvasGroup>();

                var itemTxt = rowGo.AddComponent<Text>();
                itemTxt.font = defaultFont;
                itemTxt.text = initialTasks[i];
                itemTxt.fontSize = 21;
                itemTxt.color = new Color(0.92f, 0.95f, 1f, 1f);
                itemTxt.alignment = TextAnchor.MiddleLeft;
                itemTxt.verticalOverflow = VerticalWrapMode.Overflow;
                itemTxt.horizontalOverflow = HorizontalWrapMode.Overflow;

                var strikeGo = CreateUIObject("Linea_Tachado", rowGo.transform);
                var strikeRt = strikeGo.GetComponent<RectTransform>();
                strikeRt.anchorMin = new Vector2(0f, 0.5f);
                strikeRt.anchorMax = new Vector2(0.88f, 0.5f);
                strikeRt.pivot = new Vector2(0f, 0.5f);
                strikeRt.anchoredPosition = Vector2.zero;
                strikeRt.sizeDelta = new Vector2(0f, 3f);
                var strikeImg = strikeGo.AddComponent<Image>();
                strikeImg.color = new Color(0.55f, 0.95f, 0.65f, 0.95f);
                strikeGo.SetActive(false);

                taskRowObjects[i] = rowGo;
                taskItemTexts[i] = itemTxt;
                taskStrikeBars[i] = strikeGo;
            }

            tasksPanelGo.SetActive(false);

            // 6B. Panel de 5 Corazones en la Esquina Superior Derecha (Vida de Iker = 5)
            var heartsPanelGo = CreateUIObject("Panel_Corazones", canvasGo.transform);
            var heartsPanelRt = heartsPanelGo.GetComponent<RectTransform>();
            heartsPanelRt.anchorMin = new Vector2(1f, 1f);
            heartsPanelRt.anchorMax = new Vector2(1f, 1f);
            heartsPanelRt.pivot = new Vector2(1f, 1f);
            heartsPanelRt.anchoredPosition = new Vector2(-28f, -24f);
            heartsPanelRt.sizeDelta = new Vector2(340f, 68f);

            var heartsHlg = heartsPanelGo.AddComponent<HorizontalLayoutGroup>();
            heartsHlg.spacing = 12f;
            heartsHlg.childAlignment = TextAnchor.MiddleRight;
            heartsHlg.childControlWidth = false;
            heartsHlg.childControlHeight = false;
            heartsHlg.childForceExpandWidth = false;
            heartsHlg.childForceExpandHeight = false;

            Image[] heartImages = new Image[5];
            for (int i = 0; i < 5; i++)
            {
                var hGo = CreateUIObject($"Corazon_{i + 1}", heartsPanelGo.transform);
                var hRt = hGo.GetComponent<RectTransform>();
                hRt.sizeDelta = new Vector2(54f, 54f);
                var hImg = hGo.AddComponent<Image>();
                hImg.sprite = fullHeartSprite;
                hImg.preserveAspect = true;
                heartImages[i] = hImg;
            }

            heartsPanelGo.SetActive(false);

            // 6C. Panel de Mensaje del Juego ("¡Recibiste una espada!")
            var sysMsgPanelGo = CreateUIObject("Panel_MensajeSistema", canvasGo.transform);
            var sysMsgRt = sysMsgPanelGo.GetComponent<RectTransform>();
            sysMsgRt.anchorMin = new Vector2(0.5f, 0.5f);
            sysMsgRt.anchorMax = new Vector2(0.5f, 0.5f);
            sysMsgRt.pivot = new Vector2(0.5f, 0.5f);
            sysMsgRt.anchoredPosition = new Vector2(0f, 120f);
            sysMsgRt.sizeDelta = new Vector2(580f, 96f);

            var sysMsgBg = sysMsgPanelGo.AddComponent<Image>();
            sysMsgBg.color = new Color(0.07f, 0.09f, 0.16f, 0.96f);
            var sysMsgOutline = sysMsgPanelGo.AddComponent<Outline>();
            sysMsgOutline.effectColor = new Color(1f, 0.86f, 0.32f, 0.95f);
            sysMsgOutline.effectDistance = new Vector2(3f, -3f);

            var sysMsgTxtGo = CreateUIObject("Txt_MensajeSistema", sysMsgPanelGo.transform);
            StretchFull(sysMsgTxtGo.GetComponent<RectTransform>());
            var sysMsgTxt = sysMsgTxtGo.AddComponent<Text>();
            sysMsgTxt.font = defaultFont;
            sysMsgTxt.text = "¡Recibiste una espada!";
            sysMsgTxt.fontSize = 30;
            sysMsgTxt.fontStyle = FontStyle.Bold;
            sysMsgTxt.alignment = TextAnchor.MiddleCenter;
            sysMsgTxt.color = new Color(1f, 0.94f, 0.48f, 1f);

            sysMsgPanelGo.SetActive(false);

            // 7. Indicador Contextual de Interacción
            var hintPanelGo = CreateUIObject("Panel_HintInteraccion", canvasGo.transform);
            var hintRt = hintPanelGo.GetComponent<RectTransform>();
            hintRt.anchorMin = new Vector2(0.5f, 0f);
            hintRt.anchorMax = new Vector2(0.5f, 0f);
            hintRt.pivot = new Vector2(0.5f, 0f);
            hintRt.anchoredPosition = new Vector2(0f, 28f);
            hintRt.sizeDelta = new Vector2(420f, 46f);

            var hintBg = hintPanelGo.AddComponent<Image>();
            hintBg.color = new Color(0.06f, 0.08f, 0.14f, 0.88f);
            var hintOutline = hintPanelGo.AddComponent<Outline>();
            hintOutline.effectColor = new Color(0.32f, 0.42f, 0.65f, 0.8f);
            hintOutline.effectDistance = new Vector2(2f, -2f);

            var hintTxtGo = CreateUIObject("Txt_Hint", hintPanelGo.transform);
            StretchFull(hintTxtGo.GetComponent<RectTransform>());
            var hintTxt = hintTxtGo.AddComponent<Text>();
            hintTxt.font = defaultFont;
            hintTxt.text = "[E] Interactuar";
            hintTxt.fontSize = 20;
            hintTxt.fontStyle = FontStyle.Bold;
            hintTxt.alignment = TextAnchor.MiddleCenter;
            hintTxt.color = new Color(0.95f, 0.96f, 1f, 1f);

            hintPanelGo.SetActive(false);

            // 8. Cortina 8-Bit
            var shutterContainerGo = CreateUIObject("Overlay_Cortina8Bit", canvasGo.transform);
            StretchFull(shutterContainerGo.GetComponent<RectTransform>());

            var topBarRt = CreateShutterBar("Cortina_Superior", shutterContainerGo.transform,
                new Vector2(0f, 0.5f), new Vector2(1f, 1f), new Vector2(0.5f, 1f));
            var bottomBarRt = CreateShutterBar("Cortina_Inferior", shutterContainerGo.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0.5f), new Vector2(0.5f, 0f));

            topBarRt.localScale = Vector3.one;
            bottomBarRt.localScale = Vector3.one;

            // 9. Overlay de Transición Binaria 0101
            var binaryOverlayGo = CreateUIObject("Overlay_Binario0101", canvasGo.transform);
            StretchFull(binaryOverlayGo.GetComponent<RectTransform>());
            var binaryOverlayImg = binaryOverlayGo.AddComponent<Image>();
            binaryOverlayImg.sprite = binaryTransitionSprite;
            binaryOverlayImg.color = new Color(1f, 1f, 1f, 0f);
            binaryOverlayImg.raycastTarget = false;
            binaryOverlayGo.SetActive(false);

            // 10. Overlay Frase Intro
            var phraseGo = CreateUIObject("Overlay_FraseIntro", canvasGo.transform);
            StretchFull(phraseGo.GetComponent<RectTransform>());
            var phraseCg = phraseGo.AddComponent<CanvasGroup>();
            phraseCg.alpha = 0f;
            phraseCg.blocksRaycasts = false;

            var phraseTextGo = CreateUIObject("Txt_FrasePrisma", phraseGo.transform);
            var phraseTextRt = phraseTextGo.GetComponent<RectTransform>();
            phraseTextRt.anchorMin = new Vector2(0.5f, 0.5f);
            phraseTextRt.anchorMax = new Vector2(0.5f, 0.5f);
            phraseTextRt.pivot = new Vector2(0.5f, 0.5f);
            phraseTextRt.anchoredPosition = Vector2.zero;
            phraseTextRt.sizeDelta = new Vector2(1400f, 320f);

            var phraseTxt = phraseTextGo.AddComponent<Text>();
            phraseTxt.font = defaultFont;
            phraseTxt.text = "“Los traumas de la persona se ven reflejados en el mundo,\ncomo si fuera un prisma que le da color”";
            phraseTxt.fontSize = 34;
            phraseTxt.fontStyle = FontStyle.Italic;
            phraseTxt.alignment = TextAnchor.MiddleCenter;
            phraseTxt.color = new Color(0.95f, 0.95f, 1f, 1f);
            phraseTxt.lineSpacing = 1.3f;
            phraseTxt.verticalOverflow = VerticalWrapMode.Overflow;
            phraseTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            // 11. Caja de Diálogo
            var dialoguePanelGo = CreateUIObject("Panel_Dialogo", canvasGo.transform);
            var dlgRt = dialoguePanelGo.GetComponent<RectTransform>();
            dlgRt.anchorMin = new Vector2(0.5f, 0f);
            dlgRt.anchorMax = new Vector2(0.5f, 0f);
            dlgRt.pivot = new Vector2(0.5f, 0f);
            dlgRt.anchoredPosition = new Vector2(0f, 38f);
            dlgRt.sizeDelta = new Vector2(1200f, 240f);

            var dlgBgImg = dialoguePanelGo.AddComponent<Image>();
            dlgBgImg.color = new Color(0.08f, 0.10f, 0.16f, 0.96f);

            var dlgOutline = dialoguePanelGo.AddComponent<Outline>();
            dlgOutline.effectColor = new Color(0.28f, 0.35f, 0.52f, 0.85f);
            dlgOutline.effectDistance = new Vector2(3f, -3f);

            var portraitGo = CreateUIObject("Img_Retrato", dialoguePanelGo.transform);
            var portraitRt = portraitGo.GetComponent<RectTransform>();
            portraitRt.anchorMin = new Vector2(0f, 0.5f);
            portraitRt.anchorMax = new Vector2(0f, 0.5f);
            portraitRt.pivot = new Vector2(0.5f, 0.5f);
            portraitRt.anchoredPosition = new Vector2(115f, 0f);
            portraitRt.sizeDelta = new Vector2(150f, 210f);

            var portraitImg = portraitGo.AddComponent<Image>();
            if (phoneSprite != null)
            {
                portraitImg.sprite = phoneSprite;
                portraitImg.preserveAspect = true;
            }

            var speakerGo = CreateUIObject("Txt_NombreEmisor", dialoguePanelGo.transform);
            var speakerRt = speakerGo.GetComponent<RectTransform>();
            speakerRt.anchorMin = new Vector2(0f, 1f);
            speakerRt.anchorMax = new Vector2(1f, 1f);
            speakerRt.pivot = new Vector2(0f, 1f);
            speakerRt.anchoredPosition = new Vector2(230f, -22f);
            speakerRt.sizeDelta = new Vector2(-260f, 40f);

            var speakerTxt = speakerGo.AddComponent<Text>();
            speakerTxt.font = defaultFont;
            speakerTxt.text = "Mamá";
            speakerTxt.fontSize = 26;
            speakerTxt.fontStyle = FontStyle.Bold;
            speakerTxt.color = new Color(1f, 0.90f, 0.40f, 1f);

            var bodyGo = CreateUIObject("Txt_Mensaje", dialoguePanelGo.transform);
            var bodyRt = bodyGo.GetComponent<RectTransform>();
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.pivot = new Vector2(0f, 1f);
            bodyRt.anchoredPosition = new Vector2(230f, -68f);
            bodyRt.sizeDelta = new Vector2(-260f, -100f);

            var bodyTxt = bodyGo.AddComponent<Text>();
            bodyTxt.font = defaultFont;
            bodyTxt.text = "";
            bodyTxt.fontSize = 24;
            bodyTxt.color = Color.white;
            bodyTxt.lineSpacing = 1.2f;
            bodyTxt.verticalOverflow = VerticalWrapMode.Overflow;
            bodyTxt.horizontalOverflow = HorizontalWrapMode.Wrap;

            var promptGo = CreateUIObject("Txt_PromptContinuar", dialoguePanelGo.transform);
            var promptRt = promptGo.GetComponent<RectTransform>();
            promptRt.anchorMin = new Vector2(1f, 0f);
            promptRt.anchorMax = new Vector2(1f, 0f);
            promptRt.pivot = new Vector2(1f, 0f);
            promptRt.anchoredPosition = new Vector2(-26f, 14f);
            promptRt.sizeDelta = new Vector2(400f, 32f);

            var promptTxt = promptGo.AddComponent<Text>();
            promptTxt.font = defaultFont;
            promptTxt.text = "▶ Presiona [E] o [Espacio] para continuar";
            promptTxt.fontSize = 17;
            promptTxt.fontStyle = FontStyle.Italic;
            promptTxt.alignment = TextAnchor.LowerRight;
            promptTxt.color = new Color(1f, 1f, 1f, 0.6f);

            dialoguePanelGo.SetActive(false);

            // 12. Caja de Decisión (SÍ / NO)
            var decisionPanelGo = CreateUIObject("Panel_Decision", canvasGo.transform);
            var decRt = decisionPanelGo.GetComponent<RectTransform>();
            decRt.anchorMin = new Vector2(0.5f, 0.5f);
            decRt.anchorMax = new Vector2(0.5f, 0.5f);
            decRt.pivot = new Vector2(0.5f, 0.5f);
            decRt.anchoredPosition = new Vector2(0f, -60f);
            decRt.sizeDelta = new Vector2(560f, 210f);

            var decBg = decisionPanelGo.AddComponent<Image>();
            decBg.color = new Color(0.07f, 0.09f, 0.15f, 0.97f);
            var decOutline = decisionPanelGo.AddComponent<Outline>();
            decOutline.effectColor = new Color(0.38f, 0.48f, 0.72f, 0.92f);
            decOutline.effectDistance = new Vector2(3f, -3f);

            var qGo = CreateUIObject("Txt_PreguntaDecision", decisionPanelGo.transform);
            var qRt = qGo.GetComponent<RectTransform>();
            qRt.anchorMin = new Vector2(0f, 1f);
            qRt.anchorMax = new Vector2(1f, 1f);
            qRt.pivot = new Vector2(0.5f, 1f);
            qRt.anchoredPosition = new Vector2(0f, -26f);
            qRt.sizeDelta = new Vector2(-40f, 64f);

            var qTxt = qGo.AddComponent<Text>();
            qTxt.font = defaultFont;
            qTxt.text = "¿Ordenar la cama?";
            qTxt.fontSize = 28;
            qTxt.fontStyle = FontStyle.Bold;
            qTxt.alignment = TextAnchor.MiddleCenter;
            qTxt.color = Color.white;

            var yesBtnGo = CreateUIObject("Btn_Decision_Si", decisionPanelGo.transform);
            var yesRt = yesBtnGo.GetComponent<RectTransform>();
            yesRt.anchorMin = new Vector2(0.5f, 0f);
            yesRt.anchorMax = new Vector2(0.5f, 0f);
            yesRt.pivot = new Vector2(0.5f, 0f);
            yesRt.anchoredPosition = new Vector2(-110f, 28f);
            yesRt.sizeDelta = new Vector2(170f, 64f);
            var yesImg = yesBtnGo.AddComponent<Image>();
            yesImg.color = new Color(0.14f, 0.22f, 0.36f, 1f);
            var yesBtn = yesBtnGo.AddComponent<Button>();
            yesBtn.targetGraphic = yesImg;

            var yesTxtGo = CreateUIObject("Txt_Si", yesBtnGo.transform);
            StretchFull(yesTxtGo.GetComponent<RectTransform>());
            var yesTxt = yesTxtGo.AddComponent<Text>();
            yesTxt.font = defaultFont;
            yesTxt.text = "> SÍ <";
            yesTxt.fontSize = 26;
            yesTxt.fontStyle = FontStyle.Bold;
            yesTxt.alignment = TextAnchor.MiddleCenter;
            yesTxt.color = new Color(1f, 0.92f, 0.38f, 1f);

            var noBtnGo = CreateUIObject("Btn_Decision_No", decisionPanelGo.transform);
            var noRt = noBtnGo.GetComponent<RectTransform>();
            noRt.anchorMin = new Vector2(0.5f, 0f);
            noRt.anchorMax = new Vector2(0.5f, 0f);
            noRt.pivot = new Vector2(0.5f, 0f);
            noRt.anchoredPosition = new Vector2(110f, 28f);
            noRt.sizeDelta = new Vector2(170f, 64f);
            var noImg = noBtnGo.AddComponent<Image>();
            noImg.color = new Color(0.14f, 0.18f, 0.28f, 1f);
            var noBtn = noBtnGo.AddComponent<Button>();
            noBtn.targetGraphic = noImg;

            var noTxtGo = CreateUIObject("Txt_No", noBtnGo.transform);
            StretchFull(noTxtGo.GetComponent<RectTransform>());
            var noTxt = noTxtGo.AddComponent<Text>();
            noTxt.font = defaultFont;
            noTxt.text = "NO";
            noTxt.fontSize = 26;
            noTxt.fontStyle = FontStyle.Bold;
            noTxt.alignment = TextAnchor.MiddleCenter;
            noTxt.color = new Color(0.75f, 0.80f, 0.88f, 1f);

            decisionPanelGo.SetActive(false);

            // 13. Conectar todo en IntroCutsceneController
            var so = new SerializedObject(cutsceneCtrl);
            so.FindProperty("nextSceneName").stringValue = "Sandbox_Gino";
            so.FindProperty("transitionToForestAfterDoor").boolValue = true;
            so.FindProperty("phraseCanvasGroup").objectReferenceValue = phraseCg;
            so.FindProperty("phraseText").objectReferenceValue = phraseTxt;

            so.FindProperty("bedroomRoot").objectReferenceValue = roomWorldGo;
            so.FindProperty("roomSpriteRenderer").objectReferenceValue = roomSr;
            so.FindProperty("roomSleepingSprite").objectReferenceValue = roomSleepingSprite;
            so.FindProperty("roomMessyBedSprite").objectReferenceValue = roomMessyBedSprite;
            so.FindProperty("roomMadeBedSprite").objectReferenceValue = roomMadeBedSprite;

            so.FindProperty("livingRoomRoot").objectReferenceValue = livingWorldGo;
            so.FindProperty("livingRoomSpriteRenderer").objectReferenceValue = livingSr;
            so.FindProperty("livingRoomNormalSprite").objectReferenceValue = livingRoomNormalSprite;
            so.FindProperty("livingRoomGlitchDoorSprite").objectReferenceValue = livingRoomGlitchSprite;

            so.FindProperty("forestRoot").objectReferenceValue = forestWorldGo;
            so.FindProperty("forestSpriteRenderer").objectReferenceValue = forestSr;
            so.FindProperty("forestMapSprite").objectReferenceValue = forestMapSprite;
            so.FindProperty("slimeObject").objectReferenceValue = slimeGo;
            so.FindProperty("slimeSpriteRenderer").objectReferenceValue = slimeSr;

            var slimeIdleProp = so.FindProperty("slimeIdleSprites");
            slimeIdleProp.arraySize = 3;
            slimeIdleProp.GetArrayElementAtIndex(0).objectReferenceValue = slimeIdle0;
            slimeIdleProp.GetArrayElementAtIndex(1).objectReferenceValue = slimeIdle1;
            slimeIdleProp.GetArrayElementAtIndex(2).objectReferenceValue = slimeIdle2;

            var slimeAtkProp = so.FindProperty("slimeAttackSprites");
            slimeAtkProp.arraySize = 3;
            slimeAtkProp.GetArrayElementAtIndex(0).objectReferenceValue = slimeAtk5;
            slimeAtkProp.GetArrayElementAtIndex(1).objectReferenceValue = slimeAtk6;
            slimeAtkProp.GetArrayElementAtIndex(2).objectReferenceValue = slimeAtk7;

            so.FindProperty("floatingSwordObject").objectReferenceValue = swordGo;
            so.FindProperty("swordSprite").objectReferenceValue = swordSprite;

            so.FindProperty("playerObject").objectReferenceValue = playerGo;
            so.FindProperty("playerSpawnPos").vector3Value = new Vector3(-0.8f, 0.4f, 0f);
            so.FindProperty("bedroomDoorSpawnPos").vector3Value = new Vector3(-2.15f, -1.8f, 0f);
            so.FindProperty("livingRoomDoorSpawnPos").vector3Value = new Vector3(3.75f, -1.75f, 0f);
            so.FindProperty("forestPlayerSpawnPos").vector3Value = new Vector3(6.12f, 2.88f, 0f);
            so.FindProperty("forestPlayerScale").vector3Value = new Vector3(0.5f, 0.5f, 1f);
            so.FindProperty("forestSlimeSpawnPos").vector3Value = new Vector3(6.12f, 1.15f, 0f);

            so.FindProperty("shutterContainer").objectReferenceValue = shutterContainerGo;
            so.FindProperty("topShutterBar").objectReferenceValue = topBarRt;
            so.FindProperty("bottomShutterBar").objectReferenceValue = bottomBarRt;
            so.FindProperty("binaryTransitionSprite").objectReferenceValue = binaryTransitionSprite;
            so.FindProperty("binaryOverlayImage").objectReferenceValue = binaryOverlayImg;
            so.FindProperty("retroSteps").intValue = 14;

            so.FindProperty("sfxAudioSource").objectReferenceValue = sfxAudioSource;
            so.FindProperty("phoneVibrationClip").objectReferenceValue = vibrationClip;
            so.FindProperty("apartmentMusicSource").objectReferenceValue = musicAudioSource;
            so.FindProperty("apartmentMusicClip").objectReferenceValue = apartmentMusicClip;
            so.FindProperty("musicVolume").floatValue = 0.85f;
            so.FindProperty("makeBedSfxClip").objectReferenceValue = makeBedClip;
            so.FindProperty("makeBreakfastSfxClip").objectReferenceValue = makeBreakfastClip;
            so.FindProperty("waterPlantSfxClip").objectReferenceValue = waterPlantClip;
            so.FindProperty("glitchDoorMusicClip").objectReferenceValue = glitchDoorMusicClip;

            so.FindProperty("dialoguePanel").objectReferenceValue = dialoguePanelGo;
            so.FindProperty("portraitImage").objectReferenceValue = portraitImg;
            so.FindProperty("phonePortraitSprite").objectReferenceValue = phoneSprite;
            so.FindProperty("ikerPortraitSprite").objectReferenceValue = ikerSprite;
            so.FindProperty("hannahPortraitSprite").objectReferenceValue = hannahPortrait;
            so.FindProperty("hannahObject").objectReferenceValue = hannahGo;
            so.FindProperty("hannahFollower").objectReferenceValue = hannahFollower;
            so.FindProperty("pushableRock").objectReferenceValue = pushableRock;
            so.FindProperty("forestSwitch").objectReferenceValue = forestSwitch;
            so.FindProperty("speakerText").objectReferenceValue = speakerTxt;
            so.FindProperty("bodyText").objectReferenceValue = bodyTxt;
            so.FindProperty("continuePrompt").objectReferenceValue = promptGo;
            so.FindProperty("charsPerSecond").floatValue = 32f;

            so.FindProperty("tasksPanel").objectReferenceValue = tasksPanelGo;
            so.FindProperty("tasksHeaderText").objectReferenceValue = tasksHeaderTxt;

            var rowObjsProp = so.FindProperty("taskRowObjects");
            rowObjsProp.arraySize = 3;
            var itemTxtsProp = so.FindProperty("taskItemTexts");
            itemTxtsProp.arraySize = 3;
            var strikeBarsProp = so.FindProperty("taskStrikeBars");
            strikeBarsProp.arraySize = 3;
            for (int i = 0; i < 3; i++)
            {
                rowObjsProp.GetArrayElementAtIndex(i).objectReferenceValue = taskRowObjects[i];
                itemTxtsProp.GetArrayElementAtIndex(i).objectReferenceValue = taskItemTexts[i];
                strikeBarsProp.GetArrayElementAtIndex(i).objectReferenceValue = taskStrikeBars[i];
            }

            so.FindProperty("heartsPanel").objectReferenceValue = heartsPanelGo;
            so.FindProperty("fullHeartSprite").objectReferenceValue = fullHeartSprite;
            so.FindProperty("emptyHeartSprite").objectReferenceValue = emptyHeartSprite;
            var heartImgsProp = so.FindProperty("heartImages");
            heartImgsProp.arraySize = 5;
            for (int i = 0; i < 5; i++)
            {
                heartImgsProp.GetArrayElementAtIndex(i).objectReferenceValue = heartImages[i];
            }

            so.FindProperty("systemMessagePanel").objectReferenceValue = sysMsgPanelGo;
            so.FindProperty("systemMessageText").objectReferenceValue = sysMsgTxt;

            so.FindProperty("decisionPanel").objectReferenceValue = decisionPanelGo;
            so.FindProperty("decisionQuestionText").objectReferenceValue = qTxt;
            so.FindProperty("decisionYesButton").objectReferenceValue = yesBtn;
            so.FindProperty("decisionNoButton").objectReferenceValue = noBtn;
            so.FindProperty("decisionYesText").objectReferenceValue = yesTxt;
            so.FindProperty("decisionNoText").objectReferenceValue = noTxt;

            so.FindProperty("interactionHintPanel").objectReferenceValue = hintPanelGo;
            so.FindProperty("interactionHintText").objectReferenceValue = hintTxt;

            so.FindProperty("introPhrase").stringValue = "Los traumas de la persona se ven reflejados en el mundo,\ncomo si fuera un prisma que le da color";

            var linesProp = so.FindProperty("dialogueLines");
            linesProp.arraySize = 3;
            var l0 = linesProp.GetArrayElementAtIndex(0);
            l0.FindPropertyRelative("speaker").stringValue = "Mamá";
            l0.FindPropertyRelative("portrait").objectReferenceValue = phoneSprite;
            l0.FindPropertyRelative("text").stringValue = "Iker, ¿cómo estás, hijo? Ha pasado mucho tiempo desde que escribiste o llamaste. Estoy muy preocupada, te extraño.";

            var l1 = linesProp.GetArrayElementAtIndex(1);
            l1.FindPropertyRelative("speaker").stringValue = "Iker";
            l1.FindPropertyRelative("portrait").objectReferenceValue = ikerSprite;
            l1.FindPropertyRelative("text").stringValue = "*Bostezo*";

            var l2 = linesProp.GetArrayElementAtIndex(2);
            l2.FindPropertyRelative("speaker").stringValue = "Iker";
            l2.FindPropertyRelative("portrait").objectReferenceValue = ikerSprite;
            l2.FindPropertyRelative("text").stringValue = "Aghh, otra vez lo mismo. No puedo lidiar con esto ahora mismo, mejor empiezo el día.";

            so.ApplyModifiedPropertiesWithoutUndo();

            // 14. Guardar la escena
            EditorSceneManager.MarkSceneDirty(newScene);
            EditorSceneManager.SaveScene(newScene, scenePath, false);

            // 15. Añadir a Build Settings
            var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int existingIdx = buildScenes.FindIndex(s => s.path == scenePath);
            if (existingIdx >= 0)
            {
                buildScenes[existingIdx].enabled = true;
            }
            else
            {
                int menuIdx = buildScenes.FindIndex(s => s.path.Contains("MainMenu"));
                int insertPos = (menuIdx >= 0) ? menuIdx + 1 : 0;
                buildScenes.Insert(insertPos, new EditorBuildSettingsScene(scenePath, true));
                EditorBuildSettings.scenes = buildScenes.ToArray();
            }

            // 16. Volver a dejar MainMenu.unity como escena abierta en el Editor para que el juego siempre inicie desde el Menú
            if (System.IO.File.Exists("Assets/_Project/Scenes/MainMenu.unity"))
            {
                EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
            }

            Debug.Log($"[MainMenuBuilder] Escena '{scenePath}' generada y guardada con éxito.");
        }

        private static void CreateRoomCollider(string name, Transform parent, Vector2 pos, Vector2 size)
        {
            var colGo = new GameObject(name);
            colGo.transform.SetParent(parent, false);
            colGo.transform.localPosition = new Vector3(pos.x, pos.y, 0f);
            var col = colGo.AddComponent<BoxCollider2D>();
            col.size = size;
        }

        private static RectTransform CreateShutterBar(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot)
        {
            var go = CreateUIObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = true;
            return rt;
        }
    }
}
