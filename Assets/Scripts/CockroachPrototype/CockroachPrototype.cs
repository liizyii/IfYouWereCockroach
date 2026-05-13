using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace IfYouWereCockroach.Prototype
{
    public sealed class CockroachBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void StartPrototype()
        {
            if (UnityEngine.Object.FindObjectOfType<CockroachGameManager>() != null)
            {
                return;
            }

            var gameObject = new GameObject("Cockroach Prototype Game");
            gameObject.AddComponent<CockroachGameManager>();
        }
    }

    public sealed class CockroachGameManager : MonoBehaviour
    {
        private const string LeaderboardKey = "IfYouWereCockroach.LocalLeaderboard";

        private readonly List<FoodItem> foodItems = new List<FoodItem>();
        private readonly List<HumanController> humans = new List<HumanController>();
        private readonly List<HideSpot> hideSpots = new List<HideSpot>();
        private readonly List<Rect> blockedFloorAreas = new List<Rect>();
        private readonly string[] foodNames =
        {
            "面包屑", "米饭粒", "苹果核", "糖渍", "肉渣", "饼干屑", "奶酪碎", "面条", "薯片", "果皮",
            "菜叶", "鱼刺边", "蛋糕屑", "汤渍", "花生碎", "酱汁"
        };

        private Transform runRoot;
        private CockroachPlayerController player;
        private Text statusText;
        private Text tasksText;
        private Text leaderboardText;
        private Text eventText;
        private Text challengeText;
        private GameObject challengePanel;
        private GameObject eggHintObject;
        private float survivalTime;
        private float eventMessageTimer;
        private float suspicion;
        private AudioSource ambientAudioSource;
        private AudioSource musicAudioSource;
        private int seed;
        private int familyCount;
        private int targetFoodCount;
        private int targetEggCount;
        private int eggsLaid;
        private int challengeLevel;
        private bool alive;
        private bool hasBeenDetected;
        private bool escapedAfterDetection;
        private bool challengePromptActive;
        private bool challengeOfferShown;
        private System.Random random;

        public static CockroachGameManager Instance { get; private set; }

        public bool Alive => alive;
        public CockroachPlayerController Player => player;
        public float Suspicion => suspicion;
        public bool HasBeenDetected => hasBeenDetected;
        public bool ChallengePromptActive => challengePromptActive;

        private void Awake()
        {
            Instance = this;
        }

        private void Start()
        {
            BeginNewRun();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                BeginNewRun();
            }

            if (challengePromptActive)
            {
                HandleChallengePromptInput();
                UpdateUi();
                return;
            }

            if (!alive)
            {
                return;
            }

            survivalTime += Time.deltaTime;
            suspicion = Mathf.Clamp01(suspicion - Time.deltaTime * 0.08f);

            if (Input.GetKeyDown(KeyCode.E))
            {
                TryLayEgg();
            }

            UpdateEggHint();
            UpdateUi();
            TryShowChallengePrompt();
        }

        public void BeginNewRun()
        {
            Time.timeScale = 1f;
            challengePromptActive = false;

            var sceneMarker = GameObject.Find("Press Play To Generate Prototype");
            if (sceneMarker != null)
            {
                Destroy(sceneMarker);
            }

            if (runRoot != null)
            {
                Destroy(runRoot.gameObject);
            }

            random = new System.Random(Environment.TickCount);
            seed = random.Next(10000, 99999);
            UnityEngine.Random.InitState(seed);
            survivalTime = 0f;
            suspicion = 0f;
            eggsLaid = 0;
            challengeLevel = 0;
            alive = true;
            hasBeenDetected = false;
            escapedAfterDetection = false;
            challengeOfferShown = false;
            familyCount = random.Next(1, 5);
            targetFoodCount = random.Next(10, 21);
            targetEggCount = random.Next(0, 4);
            foodItems.Clear();
            humans.Clear();
            hideSpots.Clear();
            blockedFloorAreas.Clear();

            runRoot = new GameObject("Generated Apartment Run").transform;
            BuildApartment();
            BuildPlayer();
            BuildEggHint();
            BuildHumans();
            BuildCamera();
            BuildUi();
            ShowEvent($"出生点随机完成：本局家庭成员 {familyCount} 人，目标食物 {targetFoodCount} 种");
            UpdateUi();
        }

        public void RegisterFood(FoodItem food)
        {
            if (!foodItems.Contains(food))
            {
                foodItems.Add(food);
            }
        }

        public void RegisterHuman(HumanController human)
        {
            if (!humans.Contains(human))
            {
                humans.Add(human);
            }
        }

        public void RegisterHideSpot(HideSpot hideSpot)
        {
            if (!hideSpots.Contains(hideSpot))
            {
                hideSpots.Add(hideSpot);
            }
        }

        public void EatFood(FoodItem food)
        {
            if (!alive || food == null || food.Eaten)
            {
                return;
            }

            food.MarkEaten();
            player.AddNoise(0.18f);
            player.PlayEatSound();
            ShowEvent($"吃到了：{food.DisplayName}");
            UpdateUi();
        }

        public void AddSuspicion(float amount)
        {
            suspicion = Mathf.Clamp01(suspicion + amount);
        }

        public void MarkDetected(HumanController human)
        {
            if (!alive)
            {
                return;
            }

            hasBeenDetected = true;
            suspicion = 1f;
            if (player != null)
            {
                player.PlayDetectedSound();
            }

            ShowEvent($"{human.DisplayName} 发现了你，快钻到家具下面或逃远！");
        }

        public void MarkEscaped()
        {
            if (!alive || !hasBeenDetected || escapedAfterDetection)
            {
                return;
            }

            escapedAfterDetection = true;
            ShowEvent("你成功甩开了一次追捕");
        }

        public void KillPlayer(string reason)
        {
            if (!alive)
            {
                return;
            }

            alive = false;
            CloseChallengePrompt();
            if (player != null)
            {
                player.PlayDeathSound();
            }

            SaveScore(survivalTime);
            ShowEvent($"本局结束：{reason}。按 R 重新开始");
            UpdateUi();
        }

        private void TryLayEgg()
        {
            if (player == null)
            {
                return;
            }

            int availableEggs = foodItems.Count(food => food.Eaten) / 5 - eggsLaid;
            if (availableEggs <= 0)
            {
                ShowEvent("每吃够 5 种食物才获得 1 次产卵机会");
                return;
            }

            if (!player.IsHidden)
            {
                ShowEvent("需要躲在家具底下或阴影处才敢产卵");
                return;
            }

            eggsLaid += 1;
            CreateEggCluster(player.transform.position);
            player.AddNoise(0.08f);
            player.PlayEggSound();
            ShowEvent($"产卵成功：{eggsLaid} 次");
        }

        private void BuildApartment()
        {
            CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -0.06f, 0f), new Vector3(18f, 0.12f, 14f), new Color(0.55f, 0.52f, 0.47f));
            CreatePrimitive("Back Wall", PrimitiveType.Cube, new Vector3(0f, 1.2f, 7f), new Vector3(18f, 2.4f, 0.18f), new Color(0.78f, 0.77f, 0.72f));
            CreatePrimitive("Front Wall", PrimitiveType.Cube, new Vector3(0f, 1.2f, -7f), new Vector3(18f, 2.4f, 0.18f), new Color(0.78f, 0.77f, 0.72f));
            CreatePrimitive("Left Wall", PrimitiveType.Cube, new Vector3(-9f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 14f), new Color(0.78f, 0.77f, 0.72f));
            CreatePrimitive("Right Wall", PrimitiveType.Cube, new Vector3(9f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 14f), new Color(0.78f, 0.77f, 0.72f));
            CreateWorldVisual("Ceiling", PrimitiveType.Cube, new Vector3(0f, 2.42f, 0f), new Vector3(18f, 0.12f, 14f), new Color(0.7f, 0.69f, 0.64f));
            BuildInteriorRooms();
            BuildRoomDetails();

            CreateZone("Kitchen", new Vector3(-5.8f, 0.01f, 3.7f), new Vector3(5.5f, 0.02f, 5.3f), new Color(0.62f, 0.68f, 0.63f, 0.45f));
            CreateZone("Living Room", new Vector3(3.3f, 0.012f, 2.6f), new Vector3(9.7f, 0.02f, 6.8f), new Color(0.58f, 0.56f, 0.62f, 0.45f));
            CreateZone("Bedroom", new Vector3(2.8f, 0.014f, -4.3f), new Vector3(8.8f, 0.02f, 4.7f), new Color(0.66f, 0.57f, 0.53f, 0.45f));
            CreateZone("Bathroom", new Vector3(-5.8f, 0.016f, -4.4f), new Vector3(5.5f, 0.02f, 4.5f), new Color(0.55f, 0.66f, 0.72f, 0.45f));

            AddFurniture("冰箱", new Vector3(-7.1f, 0.7f, 5.5f), new Vector3(1.1f, 1.4f, 0.9f), new Color(0.82f, 0.86f, 0.85f), true, "Models/Environment/Fridge_LowPoly");
            AddFurniture("灶台", new Vector3(-4.7f, 0.45f, 5.8f), new Vector3(2.2f, 0.9f, 0.8f), new Color(0.28f, 0.28f, 0.29f), true, "Models/Environment/Stove_LowPoly");
            AddFurniture("餐桌", new Vector3(-2.6f, 0.45f, 2.4f), new Vector3(2.2f, 0.28f, 1.4f), new Color(0.43f, 0.28f, 0.18f), true, "Models/Environment/DiningTable_LowPoly");
            AddFurniture("沙发", new Vector3(5.2f, 0.42f, 4.5f), new Vector3(3.2f, 0.84f, 1.2f), new Color(0.27f, 0.39f, 0.48f), true, "Models/Environment/Sofa_LowPoly");
            AddFurniture("茶几", new Vector3(4.7f, 0.28f, 2.2f), new Vector3(1.9f, 0.26f, 1.1f), new Color(0.36f, 0.25f, 0.17f), true, "Models/Environment/CoffeeTable_LowPoly");
            AddFurniture("床", new Vector3(4.9f, 0.36f, -4.7f), new Vector3(3.2f, 0.7f, 2.2f), new Color(0.35f, 0.42f, 0.58f), true, "Models/Environment/Bed_LowPoly");
            AddFurniture("洗手台", new Vector3(-7.1f, 0.4f, -5.6f), new Vector3(1.2f, 0.8f, 0.8f), new Color(0.88f, 0.9f, 0.88f), true, "Models/Environment/Sink_LowPoly");

            int decorationCount = random.Next(5, 10);
            for (int i = 0; i < decorationCount; i++)
            {
                var position = RandomOpenFloorPosition(0.6f);
                var scale = new Vector3(UnityEngine.Random.Range(0.5f, 1.3f), UnityEngine.Random.Range(0.25f, 0.8f), UnityEngine.Random.Range(0.4f, 1.2f));
                AddFurniture("随机杂物", position + Vector3.up * (scale.y * 0.5f), scale, new Color(UnityEngine.Random.Range(0.25f, 0.75f), UnityEngine.Random.Range(0.25f, 0.75f), UnityEngine.Random.Range(0.25f, 0.75f)), UnityEngine.Random.value > 0.35f, "Models/Environment/Clutter_LowPoly");
            }

            int foodCount = random.Next(targetFoodCount + 2, targetFoodCount + 8);
            for (int i = 0; i < foodCount; i++)
            {
                var name = foodNames[i % foodNames.Length];
                var food = CreateFood(name, RandomOpenFloorPosition(0.42f));
                var collider = food.GetComponent<Collider>();
                collider.isTrigger = true;
                var item = food.AddComponent<FoodItem>();
                item.DisplayName = name;
                RegisterFood(item);
            }

            var light = FindObjectOfType<Light>();
            if (light == null)
            {
                var lightObject = new GameObject("Main Light");
                light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
            }

            light.intensity = 1.18f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.68f;
            light.transform.rotation = Quaternion.Euler(54f, -34f, 0f);
            QualitySettings.shadows = ShadowQuality.All;
            QualitySettings.shadowDistance = 24f;
            RenderSettings.ambientIntensity = 0.72f;

            BuildAmbientAudio();
        }

        private void BuildPlayer()
        {
            var spawnChoices = new[]
            {
                new Vector3(-7.3f, 0.1f, 5.4f),
                new Vector3(-2.4f, 0.1f, 2.3f),
                new Vector3(5.2f, 0.1f, 4.2f),
                new Vector3(4.7f, 0.1f, -4.7f),
                RandomOpenFloorPosition(0.75f) + Vector3.up * 0.1f
            };

            var playerObject = new GameObject("Player Cockroach");
            playerObject.transform.SetParent(runRoot);
            playerObject.transform.position = ChooseSafeSpawn(spawnChoices);
            player = playerObject.AddComponent<CockroachPlayerController>();

            var cockroachModel = Resources.Load<GameObject>("Models/Cockroach/Cockroach_LowPoly");
            if (cockroachModel != null)
            {
                var visual = Instantiate(cockroachModel, playerObject.transform);
                visual.name = "Cockroach Model";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one * 0.75f;
                visual.AddComponent<CockroachVisualAnimator>();
                return;
            }

            var body = CreatePrimitive("Cockroach Body", PrimitiveType.Capsule, playerObject.transform.position, new Vector3(0.28f, 0.08f, 0.42f), new Color(0.12f, 0.07f, 0.04f));
            body.transform.SetParent(playerObject.transform);
            body.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            body.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Destroy(body.GetComponent<Collider>());

            for (int i = 0; i < 6; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float row = -0.16f + (i / 2) * 0.16f;
                var leg = CreatePrimitive("Leg", PrimitiveType.Cube, playerObject.transform.position, new Vector3(0.24f, 0.018f, 0.025f), new Color(0.08f, 0.045f, 0.03f));
                leg.transform.SetParent(playerObject.transform);
                leg.transform.localPosition = new Vector3(0.16f * side, 0.055f, row);
                leg.transform.localRotation = Quaternion.Euler(0f, side * 25f, 0f);
                Destroy(leg.GetComponent<Collider>());
            }

            playerObject.AddComponent<CockroachVisualAnimator>();
        }

        private void BuildHumans()
        {
            var people = new[]
            {
                new PersonSpec("男人", HumanArchetype.Man),
                new PersonSpec("女人", HumanArchetype.Woman),
                new PersonSpec("小孩", HumanArchetype.Child),
                new PersonSpec("老人", HumanArchetype.Elder)
            };

            for (int i = 0; i < familyCount; i++)
            {
                var person = people[i % people.Length];
                var humanObject = new GameObject($"Human - {person.DisplayName}");
                humanObject.transform.SetParent(runRoot);
                humanObject.transform.position = RandomOpenFloorPosition(0.9f);

                var visualRoot = AddHumanVisual(humanObject.transform, person.Archetype, i);

                var controller = humanObject.AddComponent<HumanController>();
                controller.DisplayName = person.DisplayName;
                controller.Configure(person.Archetype, visualRoot);
                controller.SetHome(RandomOpenFloorPosition(0.9f));
                RegisterHuman(controller);
            }

            if (UnityEngine.Random.value < 0.65f)
            {
                BuildPet(UnityEngine.Random.value < 0.5f ? PetKind.Cat : PetKind.Dog);
            }
        }

        private void BuildCamera()
        {
            var camera = Camera.main;
            if (camera == null)
            {
                var cameraObject = new GameObject("Main Camera");
                camera = cameraObject.AddComponent<Camera>();
                camera.tag = "MainCamera";
            }

            camera.transform.SetParent(null);
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.fieldOfView = 74f;
            camera.nearClipPlane = 0.02f;

            var follow = camera.GetComponent<SimpleCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<SimpleCameraFollow>();
            }

            follow.Target = player.transform;
        }

        private void BuildEggHint()
        {
            eggHintObject = CreateWorldVisual("Egg Ready Indicator", PrimitiveType.Cylinder, Vector3.zero, new Vector3(0.7f, 0.018f, 0.7f), new Color(0.24f, 0.95f, 0.42f));
            eggHintObject.SetActive(false);
        }

        private void BuildUi()
        {
            var existingCanvas = FindObjectOfType<Canvas>();
            if (existingCanvas != null)
            {
                Destroy(existingCanvas.gameObject);
            }

            var canvasObject = new GameObject("Prototype HUD");
            var canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1600f, 900f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();

            var statusPanel = CreatePanel(canvasObject.transform, "Status Panel", new Vector2(16f, -16f), TextAnchor.UpperLeft, new Vector2(540f, 162f), new Color(0f, 0f, 0f, 0.62f));
            var tasksPanel = CreatePanel(canvasObject.transform, "Tasks Panel", new Vector2(16f, -190f), TextAnchor.UpperLeft, new Vector2(540f, 254f), new Color(0f, 0f, 0f, 0.58f));
            var boardPanel = CreatePanel(canvasObject.transform, "Leaderboard Panel", new Vector2(-16f, -16f), TextAnchor.UpperRight, new Vector2(330f, 164f), new Color(0f, 0f, 0f, 0.46f));

            statusText = CreateText(statusPanel.transform, "Status", new Vector2(14f, -12f), TextAnchor.UpperLeft, 19, new Vector2(512f, 136f));
            tasksText = CreateText(tasksPanel.transform, "Tasks", new Vector2(14f, -12f), TextAnchor.UpperLeft, 18, new Vector2(512f, 228f));
            leaderboardText = CreateText(boardPanel.transform, "Leaderboard", new Vector2(-14f, -12f), TextAnchor.UpperRight, 16, new Vector2(302f, 138f));
            eventText = CreateText(canvasObject.transform, "Event", new Vector2(0f, 54f), TextAnchor.LowerCenter, 22, new Vector2(980f, 78f));
            challengePanel = CreatePanel(canvasObject.transform, "Challenge Panel", Vector2.zero, TextAnchor.MiddleCenter, new Vector2(620f, 300f), new Color(0f, 0f, 0f, 0.8f)).gameObject;
            challengeText = CreateText(challengePanel.transform, "Challenge Text", new Vector2(0f, 0f), TextAnchor.MiddleCenter, 23, new Vector2(580f, 256f));
            challengePanel.SetActive(false);
        }

        private Image CreatePanel(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, Vector2 size, Color color)
        {
            var panelObject = new GameObject(name);
            panelObject.transform.SetParent(parent, false);
            var rect = panelObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            if (anchor == TextAnchor.UpperRight)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }
            else if (anchor == TextAnchor.MiddleCenter)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }

            rect.anchoredPosition = anchoredPosition;
            var image = panelObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text CreateText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, int fontSize, Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent, false);
            var rect = textObject.AddComponent<RectTransform>();
            rect.sizeDelta = size;

            if (anchor == TextAnchor.UpperRight)
            {
                rect.anchorMin = new Vector2(1f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(1f, 1f);
            }
            else if (anchor == TextAnchor.LowerCenter)
            {
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
            }
            else if (anchor == TextAnchor.MiddleCenter)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
            }
            else
            {
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
            }

            rect.anchoredPosition = anchoredPosition;
            var text = textObject.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf") ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = fontSize;
            text.lineSpacing = 0.92f;
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }

        private GameObject CreatePrimitive(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(runRoot);
            gameObject.transform.position = position;
            gameObject.transform.localScale = scale;

            var renderer = gameObject.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return gameObject;
        }

        private void CreateZone(string name, Vector3 position, Vector3 scale, Color color)
        {
            var zone = CreatePrimitive(name, PrimitiveType.Cube, position, scale, color);
            Destroy(zone.GetComponent<Collider>());
        }

        private void BuildInteriorRooms()
        {
            var wallColor = new Color(0.72f, 0.71f, 0.66f);
            CreatePrimitive("Interior Wall Kitchen Bath Lower", PrimitiveType.Cube, new Vector3(-2f, 1.05f, -5.2f), new Vector3(0.14f, 2.1f, 3.6f), wallColor);
            CreatePrimitive("Interior Wall Kitchen Bath Upper", PrimitiveType.Cube, new Vector3(-2f, 1.05f, 4.45f), new Vector3(0.14f, 2.1f, 5.1f), wallColor);
            CreatePrimitive("Interior Wall Bedroom Living Left", PrimitiveType.Cube, new Vector3(-6.1f, 1.05f, -1f), new Vector3(5.8f, 2.1f, 0.14f), wallColor);
            CreatePrimitive("Interior Wall Bedroom Living Right", PrimitiveType.Cube, new Vector3(4.3f, 1.05f, -1f), new Vector3(9.4f, 2.1f, 0.14f), wallColor);

            AddDoorFrame(new Vector3(-2f, 1.1f, -1f), true);
            AddDoorFrame(new Vector3(-2f, 1.1f, 2.1f), true);
            AddDoorFrame(new Vector3(-1f, 1.1f, -1f), false);
            AddDoorFrame(new Vector3(0.2f, 1.1f, -1f), false);
        }

        private void AddDoorFrame(Vector3 position, bool vertical)
        {
            var color = new Color(0.38f, 0.23f, 0.12f);
            if (vertical)
            {
                CreatePrimitive("Door Frame Left", PrimitiveType.Cube, position + new Vector3(0f, 0f, -0.68f), new Vector3(0.18f, 2.2f, 0.08f), color);
                CreatePrimitive("Door Frame Right", PrimitiveType.Cube, position + new Vector3(0f, 0f, 0.68f), new Vector3(0.18f, 2.2f, 0.08f), color);
                CreateWorldVisual("Open Door Panel", PrimitiveType.Cube, position + new Vector3(0.42f, -0.1f, 0.28f), new Vector3(0.08f, 1.75f, 0.82f), new Color(0.42f, 0.25f, 0.13f), Quaternion.Euler(0f, 32f, 0f));
                CreateWorldVisual("Door Handle", PrimitiveType.Sphere, position + new Vector3(0.47f, 0.04f, -0.12f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.75f, 0.62f, 0.35f));
            }
            else
            {
                CreatePrimitive("Door Frame Left", PrimitiveType.Cube, position + new Vector3(-0.68f, 0f, 0f), new Vector3(0.08f, 2.2f, 0.18f), color);
                CreatePrimitive("Door Frame Right", PrimitiveType.Cube, position + new Vector3(0.68f, 0f, 0f), new Vector3(0.08f, 2.2f, 0.18f), color);
                CreateWorldVisual("Open Door Panel", PrimitiveType.Cube, position + new Vector3(0.28f, -0.1f, 0.42f), new Vector3(0.82f, 1.75f, 0.08f), new Color(0.42f, 0.25f, 0.13f), Quaternion.Euler(0f, -32f, 0f));
                CreateWorldVisual("Door Handle", PrimitiveType.Sphere, position + new Vector3(-0.12f, 0.04f, 0.47f), new Vector3(0.08f, 0.08f, 0.08f), new Color(0.75f, 0.62f, 0.35f));
            }
        }

        private void BuildRoomDetails()
        {
            var trim = new Color(0.42f, 0.32f, 0.22f);
            CreateWorldVisual("Back Baseboard", PrimitiveType.Cube, new Vector3(0f, 0.1f, 6.88f), new Vector3(17.6f, 0.12f, 0.08f), trim);
            CreateWorldVisual("Front Baseboard", PrimitiveType.Cube, new Vector3(0f, 0.1f, -6.88f), new Vector3(17.6f, 0.12f, 0.08f), trim);
            CreateWorldVisual("Left Baseboard", PrimitiveType.Cube, new Vector3(-8.88f, 0.1f, 0f), new Vector3(0.08f, 0.12f, 13.6f), trim);
            CreateWorldVisual("Right Baseboard", PrimitiveType.Cube, new Vector3(8.88f, 0.1f, 0f), new Vector3(0.08f, 0.12f, 13.6f), trim);

            for (int i = 0; i < 7; i++)
            {
                float x = -7.5f + i * 2.5f;
                CreateWorldVisual("Floor Seam X", PrimitiveType.Cube, new Vector3(x, 0.012f, 0f), new Vector3(0.025f, 0.012f, 13.4f), new Color(0.42f, 0.4f, 0.36f));
            }

            for (int i = 0; i < 5; i++)
            {
                float z = -5.4f + i * 2.7f;
                CreateWorldVisual("Floor Seam Z", PrimitiveType.Cube, new Vector3(0f, 0.014f, z), new Vector3(17.4f, 0.012f, 0.025f), new Color(0.42f, 0.4f, 0.36f));
            }

            AddWindow(new Vector3(-8.88f, 1.35f, 4.1f), false);
            AddWindow(new Vector3(8.88f, 1.35f, 2.8f), false);
            AddWindow(new Vector3(3.8f, 1.35f, 6.88f), true);
            AddRug(new Vector3(4.5f, 0.025f, 2.6f), new Vector3(3.3f, 0.03f, 2f), new Color(0.48f, 0.16f, 0.18f));
            AddRug(new Vector3(4.7f, 0.026f, -4.9f), new Vector3(2.8f, 0.03f, 2.4f), new Color(0.24f, 0.34f, 0.52f));

            AddKitchenDetails();
            AddBathroomDetails();
            AddBedroomDetails();
        }

        private void AddWindow(Vector3 position, bool horizontal)
        {
            var glass = new Color(0.45f, 0.68f, 0.82f, 1f);
            var frame = new Color(0.85f, 0.86f, 0.82f);
            if (horizontal)
            {
                CreateWorldVisual("Window Glass", PrimitiveType.Cube, position, new Vector3(1.6f, 0.82f, 0.035f), glass);
                CreateWorldVisual("Window Cross", PrimitiveType.Cube, position + Vector3.up * 0.02f, new Vector3(0.05f, 0.86f, 0.045f), frame);
                CreateWorldVisual("Window Sill", PrimitiveType.Cube, position + Vector3.down * 0.48f, new Vector3(1.9f, 0.08f, 0.18f), frame);
            }
            else
            {
                CreateWorldVisual("Window Glass", PrimitiveType.Cube, position, new Vector3(0.035f, 0.82f, 1.6f), glass);
                CreateWorldVisual("Window Cross", PrimitiveType.Cube, position + Vector3.up * 0.02f, new Vector3(0.045f, 0.86f, 0.05f), frame);
                CreateWorldVisual("Window Sill", PrimitiveType.Cube, position + Vector3.down * 0.48f, new Vector3(0.18f, 0.08f, 1.9f), frame);
            }
        }

        private void AddRug(Vector3 position, Vector3 scale, Color color)
        {
            CreateWorldVisual("Rug", PrimitiveType.Cube, position, scale, color);
            CreateWorldVisual("Rug Stripe", PrimitiveType.Cube, position + new Vector3(0f, 0.02f, 0f), new Vector3(scale.x * 0.9f, 0.012f, 0.05f), color * 1.25f);
        }

        private void AddKitchenDetails()
        {
            var cabinet = new Color(0.48f, 0.36f, 0.22f);
            var counter = new Color(0.62f, 0.62f, 0.58f);
            CreateWorldVisual("Kitchen Counter", PrimitiveType.Cube, new Vector3(-6.4f, 0.46f, 3.35f), new Vector3(2.6f, 0.68f, 0.65f), cabinet);
            CreateWorldVisual("Kitchen Counter Top", PrimitiveType.Cube, new Vector3(-6.4f, 0.84f, 3.35f), new Vector3(2.75f, 0.08f, 0.78f), counter);
            CreateWorldVisual("Upper Cabinet", PrimitiveType.Cube, new Vector3(-6.4f, 1.65f, 6.7f), new Vector3(2.5f, 0.5f, 0.18f), cabinet * 0.9f);
            CreateWorldVisual("Trash Bin", PrimitiveType.Cylinder, new Vector3(-3.4f, 0.32f, 5.6f), new Vector3(0.38f, 0.58f, 0.38f), new Color(0.18f, 0.22f, 0.22f));
            CreateWorldVisual("Kitchen Mat", PrimitiveType.Cube, new Vector3(-5.2f, 0.026f, 4.65f), new Vector3(2.2f, 0.03f, 0.75f), new Color(0.2f, 0.42f, 0.35f));
        }

        private void AddBathroomDetails()
        {
            CreateWorldVisual("Toilet Base", PrimitiveType.Cylinder, new Vector3(-4.2f, 0.22f, -5.2f), new Vector3(0.34f, 0.34f, 0.34f), new Color(0.9f, 0.92f, 0.9f));
            CreateWorldVisual("Toilet Tank", PrimitiveType.Cube, new Vector3(-4.2f, 0.66f, -5.58f), new Vector3(0.72f, 0.42f, 0.18f), new Color(0.9f, 0.92f, 0.9f));
            CreateWorldVisual("Bathtub", PrimitiveType.Cube, new Vector3(-6.15f, 0.32f, -3.05f), new Vector3(1.9f, 0.55f, 0.82f), new Color(0.86f, 0.9f, 0.92f));
            CreateWorldVisual("Bath Inner", PrimitiveType.Cube, new Vector3(-6.15f, 0.62f, -3.05f), new Vector3(1.5f, 0.08f, 0.52f), new Color(0.56f, 0.72f, 0.8f));
            CreateWorldVisual("Mirror", PrimitiveType.Cube, new Vector3(-8.88f, 1.35f, -5.55f), new Vector3(0.035f, 0.78f, 0.7f), new Color(0.58f, 0.72f, 0.78f));
        }

        private void AddBedroomDetails()
        {
            CreateWorldVisual("Wardrobe", PrimitiveType.Cube, new Vector3(8.35f, 0.95f, -3.7f), new Vector3(0.82f, 1.9f, 1.55f), new Color(0.34f, 0.2f, 0.11f));
            CreateWorldVisual("Wardrobe Handle", PrimitiveType.Cube, new Vector3(7.92f, 0.98f, -3.7f), new Vector3(0.05f, 0.55f, 0.05f), new Color(0.65f, 0.55f, 0.35f));
            CreateWorldVisual("Night Stand", PrimitiveType.Cube, new Vector3(2.85f, 0.35f, -5.55f), new Vector3(0.72f, 0.7f, 0.62f), new Color(0.32f, 0.18f, 0.1f));
            CreateWorldVisual("Lamp Shade", PrimitiveType.Cylinder, new Vector3(2.85f, 0.9f, -5.55f), new Vector3(0.25f, 0.28f, 0.25f), new Color(0.88f, 0.78f, 0.48f));
            CreateWorldVisual("Wall Picture", PrimitiveType.Cube, new Vector3(4.9f, 1.45f, -6.88f), new Vector3(1.2f, 0.72f, 0.035f), new Color(0.62f, 0.52f, 0.38f));
        }

        private GameObject CreateWorldVisual(string name, PrimitiveType type, Vector3 position, Vector3 scale, Color color, Quaternion? rotation = null)
        {
            var gameObject = CreatePrimitive(name, type, position, scale, color);
            gameObject.transform.localRotation = rotation ?? Quaternion.identity;
            if (gameObject.TryGetComponent<Collider>(out var collider))
            {
                Destroy(collider);
            }

            return gameObject;
        }

        private GameObject CreateFood(string displayName, Vector3 floorPosition)
        {
            var root = CreatePrimitive($"Food - {displayName}", PrimitiveType.Sphere, floorPosition + Vector3.up * 0.12f, Vector3.one * 0.34f, Color.white);
            if (root.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.enabled = false;
            }
            if (root.TryGetComponent<SphereCollider>(out var foodCollider))
            {
                foodCollider.radius = 2.2f;
                foodCollider.isTrigger = true;
            }

            Color color = FoodColor(displayName);
            CreateVisualPrimitive(root.transform, "food_shadow", PrimitiveType.Cylinder, new Vector3(0f, -0.28f, 0f), new Vector3(0.7f, 0.025f, 0.7f), new Color(0.18f, 0.15f, 0.1f, 1f));

            if (displayName.Contains("面条"))
            {
                for (int i = 0; i < 5; i++)
                {
                    CreateVisualPrimitive(root.transform, "noodle_strand", PrimitiveType.Cube, new Vector3(UnityEngine.Random.Range(-0.1f, 0.1f), i * 0.035f - 0.05f, UnityEngine.Random.Range(-0.08f, 0.08f)), new Vector3(UnityEngine.Random.Range(0.75f, 1.25f), 0.055f, 0.055f), color, Quaternion.Euler(0f, UnityEngine.Random.Range(-70f, 70f), UnityEngine.Random.Range(-8f, 8f)));
                }
            }
            else if (displayName.Contains("鱼刺"))
            {
                CreateVisualPrimitive(root.transform, "fish_bone_spine", PrimitiveType.Cube, Vector3.zero, new Vector3(1.15f, 0.055f, 0.055f), new Color(0.88f, 0.84f, 0.68f), Quaternion.Euler(0f, 25f, 0f));
                for (int i = -2; i <= 2; i++)
                {
                    CreateVisualPrimitive(root.transform, "fish_bone_rib", PrimitiveType.Cube, new Vector3(i * 0.16f, 0.02f, 0f), new Vector3(0.055f, 0.045f, 0.35f), new Color(0.86f, 0.82f, 0.66f), Quaternion.Euler(0f, 25f + i * 8f, 35f));
                }
            }
            else if (displayName.Contains("米饭") || displayName.Contains("花生"))
            {
                int grains = displayName.Contains("米饭") ? 9 : 5;
                for (int i = 0; i < grains; i++)
                {
                    CreateVisualPrimitive(root.transform, "food_grain", PrimitiveType.Sphere, new Vector3(UnityEngine.Random.Range(-0.35f, 0.35f), UnityEngine.Random.Range(-0.03f, 0.08f), UnityEngine.Random.Range(-0.24f, 0.24f)), new Vector3(0.18f, 0.11f, 0.14f), color);
                }
            }
            else if (displayName.Contains("苹果") || displayName.Contains("果皮"))
            {
                CreateVisualPrimitive(root.transform, "food_core", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.55f, 0.42f, 0.45f), color);
                CreateVisualPrimitive(root.transform, "food_peel", PrimitiveType.Cube, new Vector3(0.12f, 0.12f, 0f), new Vector3(0.12f, 0.06f, 0.75f), new Color(0.55f, 0.08f, 0.05f), Quaternion.Euler(0f, 18f, 35f));
            }
            else if (displayName.Contains("奶酪"))
            {
                CreateVisualPrimitive(root.transform, "cheese_wedge", PrimitiveType.Cube, Vector3.zero, new Vector3(0.72f, 0.32f, 0.48f), color, Quaternion.Euler(0f, 18f, 0f));
                for (int i = 0; i < 3; i++)
                {
                    CreateVisualPrimitive(root.transform, "cheese_hole", PrimitiveType.Sphere, new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), 0.08f, -0.25f), new Vector3(0.11f, 0.06f, 0.035f), new Color(0.62f, 0.42f, 0.08f));
                }
            }
            else if (displayName.Contains("薯片"))
            {
                for (int i = 0; i < 4; i++)
                {
                    CreateVisualPrimitive(root.transform, "chip", PrimitiveType.Cube, new Vector3(UnityEngine.Random.Range(-0.24f, 0.24f), UnityEngine.Random.Range(-0.02f, 0.08f), UnityEngine.Random.Range(-0.18f, 0.18f)), new Vector3(0.42f, 0.035f, 0.28f), color, Quaternion.Euler(UnityEngine.Random.Range(-12f, 12f), UnityEngine.Random.Range(0f, 180f), UnityEngine.Random.Range(-10f, 10f)));
                }
            }
            else if (displayName.Contains("蛋糕") || displayName.Contains("饼干") || displayName.Contains("面包"))
            {
                CreateVisualPrimitive(root.transform, "baked_base", PrimitiveType.Cube, Vector3.zero, new Vector3(0.58f, 0.28f, 0.48f), color, Quaternion.Euler(0f, UnityEngine.Random.Range(-35f, 35f), 0f));
                CreateVisualPrimitive(root.transform, "cream_or_crust", PrimitiveType.Cube, new Vector3(0f, 0.18f, 0f), new Vector3(0.54f, 0.06f, 0.44f), displayName.Contains("蛋糕") ? new Color(0.92f, 0.86f, 0.74f) : color * 0.72f);
                for (int i = 0; i < 4; i++)
                {
                    CreateVisualPrimitive(root.transform, "crumb_dot", PrimitiveType.Sphere, new Vector3(UnityEngine.Random.Range(-0.34f, 0.34f), UnityEngine.Random.Range(0f, 0.12f), UnityEngine.Random.Range(-0.28f, 0.28f)), new Vector3(0.08f, 0.05f, 0.07f), color * UnityEngine.Random.Range(0.75f, 1.15f));
                }
            }
            else if (displayName.Contains("汤") || displayName.Contains("糖") || displayName.Contains("酱"))
            {
                CreateVisualPrimitive(root.transform, "sticky_puddle", PrimitiveType.Cylinder, new Vector3(0f, -0.08f, 0f), new Vector3(0.58f, 0.05f, 0.48f), color);
                CreateVisualPrimitive(root.transform, "wet_highlight", PrimitiveType.Cylinder, new Vector3(0.1f, -0.035f, -0.06f), new Vector3(0.18f, 0.018f, 0.13f), new Color(1f, 0.82f, 0.45f));
            }
            else if (displayName.Contains("肉"))
            {
                CreateVisualPrimitive(root.transform, "meat_chunk", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.56f, 0.3f, 0.38f), color);
                CreateVisualPrimitive(root.transform, "meat_fat", PrimitiveType.Cube, new Vector3(0.04f, 0.08f, -0.22f), new Vector3(0.34f, 0.055f, 0.08f), new Color(0.93f, 0.72f, 0.54f), Quaternion.Euler(0f, 18f, 0f));
            }
            else if (displayName.Contains("菜叶"))
            {
                CreateVisualPrimitive(root.transform, "leaf_main", PrimitiveType.Cube, Vector3.zero, new Vector3(0.72f, 0.035f, 0.42f), color, Quaternion.Euler(5f, 22f, 8f));
                CreateVisualPrimitive(root.transform, "leaf_vein", PrimitiveType.Cube, new Vector3(0f, 0.035f, 0f), new Vector3(0.62f, 0.025f, 0.035f), color * 0.65f, Quaternion.Euler(5f, 22f, 8f));
            }
            else
            {
                CreateVisualPrimitive(root.transform, "food_chunk", PrimitiveType.Sphere, Vector3.zero, new Vector3(0.5f, 0.32f, 0.42f), color);
                CreateVisualPrimitive(root.transform, "food_crumb", PrimitiveType.Sphere, new Vector3(0.22f, -0.02f, 0.16f), new Vector3(0.22f, 0.14f, 0.18f), color * 0.85f);
            }

            return root;
        }

        private Color FoodColor(string displayName)
        {
            if (displayName.Contains("苹果") || displayName.Contains("果皮")) return new Color(0.8f, 0.18f, 0.08f);
            if (displayName.Contains("菜")) return new Color(0.18f, 0.55f, 0.16f);
            if (displayName.Contains("肉") || displayName.Contains("鱼")) return new Color(0.72f, 0.36f, 0.27f);
            if (displayName.Contains("奶酪")) return new Color(0.95f, 0.72f, 0.18f);
            if (displayName.Contains("糖") || displayName.Contains("酱")) return new Color(0.65f, 0.22f, 0.09f);
            return new Color(0.84f, 0.62f, 0.28f);
        }

        private GameObject CreateVisualPrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPosition, Vector3 localScale, Color color, Quaternion? localRotation = null)
        {
            var gameObject = GameObject.CreatePrimitive(type);
            gameObject.name = name;
            gameObject.transform.SetParent(parent, false);
            gameObject.transform.localPosition = localPosition;
            gameObject.transform.localRotation = localRotation ?? Quaternion.identity;
            gameObject.transform.localScale = localScale;

            if (gameObject.TryGetComponent<Collider>(out var collider))
            {
                Destroy(collider);
            }

            var renderer = gameObject.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = color;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
            renderer.receiveShadows = true;
            return gameObject;
        }

        private void AddFurniture(string name, Vector3 position, Vector3 scale, Color color, bool createsHideSpot, string modelResourcePath = null)
        {
            var furniture = CreatePrimitive(name, PrimitiveType.Cube, position, scale, color);
            RegisterBlockedArea(position, scale, 0.35f);
            AddFurnitureFloorShadow(name, position, scale);
            if (!TryAttachFurnitureModel(furniture.transform, modelResourcePath))
            {
                AddFurnitureVisual(furniture.transform, name, color);
            }

            if (!createsHideSpot)
            {
                return;
            }

            if (furniture.TryGetComponent<BoxCollider>(out var furnitureCollider))
            {
                furnitureCollider.center = new Vector3(0f, 0.35f, 0f);
                furnitureCollider.size = new Vector3(1f, 0.3f, 1f);
            }

            var hideObject = new GameObject($"Hide Spot - {name}");
            hideObject.transform.SetParent(runRoot);
            hideObject.transform.position = new Vector3(position.x, 0.12f, position.z);
            hideObject.transform.localScale = new Vector3(Mathf.Max(0.7f, scale.x * 0.9f), 0.22f, Mathf.Max(0.7f, scale.z * 0.9f));
            var box = hideObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            var hideSpot = hideObject.AddComponent<HideSpot>();
            RegisterHideSpot(hideSpot);
        }

        private bool TryAttachFurnitureModel(Transform parent, string modelResourcePath)
        {
            if (string.IsNullOrWhiteSpace(modelResourcePath))
            {
                return false;
            }

            var model = Resources.Load<GameObject>(modelResourcePath);
            if (model == null)
            {
                return false;
            }

            if (parent.TryGetComponent<Renderer>(out var parentRenderer))
            {
                parentRenderer.enabled = false;
            }

            var visual = Instantiate(model, parent);
            visual.name = $"{parent.name} Model";
            visual.transform.localPosition = new Vector3(0f, -0.5f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            PrepareImportedModel(visual);
            return true;
        }

        private void PrepareImportedModel(GameObject modelRoot)
        {
            foreach (var collider in modelRoot.GetComponentsInChildren<Collider>())
            {
                Destroy(collider);
            }

            foreach (var renderer in modelRoot.GetComponentsInChildren<Renderer>())
            {
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                renderer.receiveShadows = true;
            }
        }

        private void AddFurnitureFloorShadow(string name, Vector3 position, Vector3 scale)
        {
            var shadowScale = new Vector3(Mathf.Max(0.7f, scale.x * 1.05f), 0.018f, Mathf.Max(0.55f, scale.z * 1.05f));
            CreateWorldVisual($"{name} Ground Shadow", PrimitiveType.Cylinder, new Vector3(position.x, 0.018f, position.z), shadowScale, new Color(0.08f, 0.07f, 0.055f));
        }

        private void AddFurnitureVisual(Transform parent, string name, Color baseColor)
        {
            if (parent.TryGetComponent<Renderer>(out var renderer))
            {
                renderer.enabled = false;
            }

            var wood = new Color(0.38f, 0.23f, 0.12f);
            var darkWood = new Color(0.2f, 0.11f, 0.055f);
            var metal = new Color(0.56f, 0.58f, 0.57f);
            var black = new Color(0.04f, 0.04f, 0.045f);
            var fabric = new Color(0.22f, 0.34f, 0.43f);
            var fabricLight = new Color(0.34f, 0.48f, 0.55f);
            var white = new Color(0.86f, 0.9f, 0.88f);

            if (name.Contains("冰箱"))
            {
                CreateVisualPrimitive(parent, "fridge_body", PrimitiveType.Cube, Vector3.zero, Vector3.one, white);
                CreateVisualPrimitive(parent, "freezer_door", PrimitiveType.Cube, new Vector3(0f, 0.18f, -0.52f), new Vector3(0.92f, 0.34f, 0.04f), new Color(0.92f, 0.96f, 0.95f));
                CreateVisualPrimitive(parent, "fridge_door", PrimitiveType.Cube, new Vector3(0f, -0.18f, -0.52f), new Vector3(0.92f, 0.52f, 0.04f), new Color(0.82f, 0.88f, 0.87f));
                CreateVisualPrimitive(parent, "fridge_handle_top", PrimitiveType.Cube, new Vector3(0.32f, 0.2f, -0.57f), new Vector3(0.04f, 0.22f, 0.05f), metal);
                CreateVisualPrimitive(parent, "fridge_handle_bottom", PrimitiveType.Cube, new Vector3(0.32f, -0.2f, -0.57f), new Vector3(0.04f, 0.32f, 0.05f), metal);
                CreateVisualPrimitive(parent, "fridge_vent", PrimitiveType.Cube, new Vector3(-0.18f, -0.43f, -0.57f), new Vector3(0.38f, 0.045f, 0.035f), new Color(0.62f, 0.68f, 0.68f));
                CreateVisualPrimitive(parent, "fridge_magnet_red", PrimitiveType.Cube, new Vector3(-0.28f, 0.05f, -0.575f), new Vector3(0.11f, 0.08f, 0.025f), new Color(0.7f, 0.08f, 0.08f));
                CreateVisualPrimitive(parent, "fridge_magnet_note", PrimitiveType.Cube, new Vector3(-0.12f, -0.1f, -0.575f), new Vector3(0.16f, 0.12f, 0.025f), new Color(0.95f, 0.86f, 0.46f));
            }
            else if (name.Contains("灶台"))
            {
                CreateVisualPrimitive(parent, "stove_base", PrimitiveType.Cube, Vector3.zero, Vector3.one, black);
                CreateVisualPrimitive(parent, "stove_top", PrimitiveType.Cube, new Vector3(0f, 0.47f, 0f), new Vector3(1.04f, 0.08f, 1.04f), metal);
                CreateVisualPrimitive(parent, "oven_window", PrimitiveType.Cube, new Vector3(0f, -0.1f, -0.52f), new Vector3(0.5f, 0.3f, 0.04f), new Color(0.05f, 0.08f, 0.09f));
                CreateVisualPrimitive(parent, "left_burner", PrimitiveType.Cylinder, new Vector3(-0.23f, 0.54f, -0.18f), new Vector3(0.16f, 0.025f, 0.16f), black);
                CreateVisualPrimitive(parent, "right_burner", PrimitiveType.Cylinder, new Vector3(0.23f, 0.54f, 0.18f), new Vector3(0.16f, 0.025f, 0.16f), black);
                CreateVisualPrimitive(parent, "front_left_burner", PrimitiveType.Cylinder, new Vector3(-0.28f, 0.55f, 0.24f), new Vector3(0.13f, 0.02f, 0.13f), black * 1.6f);
                CreateVisualPrimitive(parent, "front_right_burner", PrimitiveType.Cylinder, new Vector3(0.28f, 0.55f, -0.24f), new Vector3(0.13f, 0.02f, 0.13f), black * 1.6f);
                for (int i = 0; i < 4; i++)
                {
                    CreateVisualPrimitive(parent, "stove_knob", PrimitiveType.Cylinder, new Vector3(-0.3f + i * 0.2f, 0.22f, -0.56f), new Vector3(0.055f, 0.035f, 0.055f), metal, Quaternion.Euler(90f, 0f, 0f));
                }
            }
            else if (name.Contains("餐桌"))
            {
                CreateVisualPrimitive(parent, "table_top", PrimitiveType.Cube, new Vector3(0f, 0.35f, 0f), new Vector3(1f, 0.12f, 1f), wood);
                AddTableLegs(parent, darkWood, 0.33f, 0.27f, 0.06f, 0.72f);
                CreateVisualPrimitive(parent, "table_runner", PrimitiveType.Cube, new Vector3(0f, 0.43f, 0f), new Vector3(0.18f, 0.025f, 0.9f), new Color(0.74f, 0.55f, 0.36f));
                CreateVisualPrimitive(parent, "table_plate", PrimitiveType.Cylinder, new Vector3(0.18f, 0.48f, -0.05f), new Vector3(0.18f, 0.025f, 0.18f), new Color(0.9f, 0.9f, 0.84f));
                AddChair(parent, new Vector3(-0.7f, -0.06f, 0f), Quaternion.Euler(0f, 90f, 0f), wood);
                AddChair(parent, new Vector3(0.7f, -0.06f, 0f), Quaternion.Euler(0f, -90f, 0f), wood);
                AddChair(parent, new Vector3(0f, -0.06f, -0.72f), Quaternion.identity, wood);
            }
            else if (name.Contains("沙发"))
            {
                CreateVisualPrimitive(parent, "sofa_seat", PrimitiveType.Cube, new Vector3(0f, -0.15f, -0.05f), new Vector3(1f, 0.42f, 0.72f), fabric);
                CreateVisualPrimitive(parent, "sofa_back", PrimitiveType.Cube, new Vector3(0f, 0.18f, 0.42f), new Vector3(1f, 0.7f, 0.18f), fabric);
                CreateVisualPrimitive(parent, "sofa_left_arm", PrimitiveType.Cube, new Vector3(-0.48f, 0f, -0.02f), new Vector3(0.12f, 0.68f, 0.8f), fabricLight);
                CreateVisualPrimitive(parent, "sofa_right_arm", PrimitiveType.Cube, new Vector3(0.48f, 0f, -0.02f), new Vector3(0.12f, 0.68f, 0.8f), fabricLight);
                CreateVisualPrimitive(parent, "sofa_left_cushion", PrimitiveType.Cube, new Vector3(-0.22f, 0.09f, -0.18f), new Vector3(0.38f, 0.08f, 0.42f), fabricLight * 1.1f);
                CreateVisualPrimitive(parent, "sofa_right_cushion", PrimitiveType.Cube, new Vector3(0.22f, 0.09f, -0.18f), new Vector3(0.38f, 0.08f, 0.42f), fabricLight * 1.1f);
                CreateVisualPrimitive(parent, "sofa_pillow", PrimitiveType.Cube, new Vector3(-0.28f, 0.24f, 0.23f), new Vector3(0.2f, 0.18f, 0.08f), new Color(0.62f, 0.37f, 0.27f), Quaternion.Euler(0f, 0f, -8f));
                AddTableLegs(parent, darkWood, 0.38f, 0.28f, 0.04f, 0.28f);
            }
            else if (name.Contains("茶几"))
            {
                CreateVisualPrimitive(parent, "coffee_table_top", PrimitiveType.Cube, new Vector3(0f, 0.24f, 0f), new Vector3(1f, 0.1f, 1f), wood);
                CreateVisualPrimitive(parent, "coffee_table_shelf", PrimitiveType.Cube, new Vector3(0f, -0.08f, 0f), new Vector3(0.82f, 0.07f, 0.82f), darkWood);
                AddTableLegs(parent, darkWood, 0.42f, 0.33f, 0.05f, 0.62f);
                CreateVisualPrimitive(parent, "book_stack", PrimitiveType.Cube, new Vector3(-0.2f, 0.33f, 0.12f), new Vector3(0.3f, 0.055f, 0.2f), new Color(0.12f, 0.2f, 0.42f), Quaternion.Euler(0f, -12f, 0f));
                CreateVisualPrimitive(parent, "remote_control", PrimitiveType.Cube, new Vector3(0.22f, 0.33f, -0.16f), new Vector3(0.09f, 0.035f, 0.34f), black, Quaternion.Euler(0f, 25f, 0f));
                CreateVisualPrimitive(parent, "mug", PrimitiveType.Cylinder, new Vector3(0.22f, 0.38f, 0.18f), new Vector3(0.095f, 0.13f, 0.095f), new Color(0.82f, 0.82f, 0.74f));
            }
            else if (name.Contains("床"))
            {
                CreateVisualPrimitive(parent, "bed_frame", PrimitiveType.Cube, new Vector3(0f, -0.25f, 0f), new Vector3(1f, 0.25f, 1f), darkWood);
                CreateVisualPrimitive(parent, "mattress", PrimitiveType.Cube, new Vector3(0f, 0.02f, 0f), new Vector3(0.92f, 0.28f, 0.92f), new Color(0.78f, 0.78f, 0.72f));
                CreateVisualPrimitive(parent, "blanket", PrimitiveType.Cube, new Vector3(0f, 0.22f, -0.12f), new Vector3(0.92f, 0.13f, 0.55f), new Color(0.26f, 0.35f, 0.52f));
                CreateVisualPrimitive(parent, "pillow_left", PrimitiveType.Cube, new Vector3(-0.22f, 0.26f, 0.32f), new Vector3(0.28f, 0.12f, 0.22f), new Color(0.88f, 0.86f, 0.78f));
                CreateVisualPrimitive(parent, "pillow_right", PrimitiveType.Cube, new Vector3(0.22f, 0.26f, 0.32f), new Vector3(0.28f, 0.12f, 0.22f), new Color(0.88f, 0.86f, 0.78f));
                CreateVisualPrimitive(parent, "headboard", PrimitiveType.Cube, new Vector3(0f, 0.08f, 0.52f), new Vector3(1f, 0.62f, 0.1f), darkWood);
                CreateVisualPrimitive(parent, "blanket_fold", PrimitiveType.Cube, new Vector3(0f, 0.31f, 0.16f), new Vector3(0.92f, 0.055f, 0.08f), new Color(0.18f, 0.26f, 0.42f));
            }
            else if (name.Contains("洗手台"))
            {
                CreateVisualPrimitive(parent, "sink_cabinet", PrimitiveType.Cube, new Vector3(0f, -0.08f, 0f), new Vector3(1f, 0.82f, 1f), white);
                CreateVisualPrimitive(parent, "sink_basin", PrimitiveType.Cube, new Vector3(0f, 0.38f, 0f), new Vector3(0.84f, 0.12f, 0.78f), new Color(0.92f, 0.94f, 0.91f));
                CreateVisualPrimitive(parent, "faucet", PrimitiveType.Cube, new Vector3(0f, 0.55f, -0.22f), new Vector3(0.08f, 0.26f, 0.08f), metal);
                CreateVisualPrimitive(parent, "sink_drain", PrimitiveType.Cylinder, new Vector3(0f, 0.46f, 0.08f), new Vector3(0.09f, 0.018f, 0.09f), metal);
                CreateVisualPrimitive(parent, "tap_left", PrimitiveType.Cylinder, new Vector3(-0.16f, 0.52f, -0.24f), new Vector3(0.055f, 0.045f, 0.055f), metal);
                CreateVisualPrimitive(parent, "tap_right", PrimitiveType.Cylinder, new Vector3(0.16f, 0.52f, -0.24f), new Vector3(0.055f, 0.045f, 0.055f), metal);
                CreateVisualPrimitive(parent, "soap_bottle", PrimitiveType.Cube, new Vector3(0.34f, 0.56f, 0.24f), new Vector3(0.12f, 0.18f, 0.1f), new Color(0.36f, 0.75f, 0.68f));
            }
            else
            {
                CreateVisualPrimitive(parent, "clutter_box", PrimitiveType.Cube, new Vector3(-0.16f, -0.05f, 0.04f), new Vector3(0.52f, 0.7f, 0.52f), baseColor);
                CreateVisualPrimitive(parent, "clutter_can", PrimitiveType.Cylinder, new Vector3(0.26f, 0.02f, -0.18f), new Vector3(0.18f, 0.34f, 0.18f), metal);
                CreateVisualPrimitive(parent, "folded_cloth", PrimitiveType.Cube, new Vector3(0.1f, 0.26f, 0.18f), new Vector3(0.36f, 0.08f, 0.28f), new Color(0.48f, 0.18f, 0.2f), Quaternion.Euler(0f, 18f, 0f));
                CreateVisualPrimitive(parent, "paper_label", PrimitiveType.Cube, new Vector3(-0.16f, 0.16f, -0.23f), new Vector3(0.28f, 0.18f, 0.025f), new Color(0.88f, 0.8f, 0.58f));
            }
        }

        private void AddChair(Transform parent, Vector3 localPosition, Quaternion localRotation, Color color)
        {
            var root = new GameObject("dining_chair").transform;
            root.SetParent(parent, false);
            root.localPosition = localPosition;
            root.localRotation = localRotation;

            var cushion = new Color(0.38f, 0.26f, 0.18f);
            CreateVisualPrimitive(root, "chair_seat", PrimitiveType.Cube, new Vector3(0f, 0.1f, 0f), new Vector3(0.34f, 0.08f, 0.34f), cushion);
            CreateVisualPrimitive(root, "chair_back", PrimitiveType.Cube, new Vector3(0f, 0.36f, 0.16f), new Vector3(0.34f, 0.42f, 0.07f), color);
            CreateVisualPrimitive(root, "chair_left_leg", PrimitiveType.Cube, new Vector3(-0.13f, -0.08f, -0.12f), new Vector3(0.045f, 0.34f, 0.045f), color);
            CreateVisualPrimitive(root, "chair_right_leg", PrimitiveType.Cube, new Vector3(0.13f, -0.08f, -0.12f), new Vector3(0.045f, 0.34f, 0.045f), color);
            CreateVisualPrimitive(root, "chair_back_left_leg", PrimitiveType.Cube, new Vector3(-0.13f, -0.08f, 0.13f), new Vector3(0.045f, 0.34f, 0.045f), color);
            CreateVisualPrimitive(root, "chair_back_right_leg", PrimitiveType.Cube, new Vector3(0.13f, -0.08f, 0.13f), new Vector3(0.045f, 0.34f, 0.045f), color);
        }

        private void AddTableLegs(Transform parent, Color color, float x, float z, float thickness, float height)
        {
            float y = -0.15f;
            CreateVisualPrimitive(parent, "leg_front_left", PrimitiveType.Cube, new Vector3(-x, y, -z), new Vector3(thickness, height, thickness), color);
            CreateVisualPrimitive(parent, "leg_front_right", PrimitiveType.Cube, new Vector3(x, y, -z), new Vector3(thickness, height, thickness), color);
            CreateVisualPrimitive(parent, "leg_back_left", PrimitiveType.Cube, new Vector3(-x, y, z), new Vector3(thickness, height, thickness), color);
            CreateVisualPrimitive(parent, "leg_back_right", PrimitiveType.Cube, new Vector3(x, y, z), new Vector3(thickness, height, thickness), color);
        }

        private Transform AddHumanVisual(Transform parent, HumanArchetype archetype, int variant)
        {
            var root = new GameObject("Human Visual Root").transform;
            root.SetParent(parent, false);

            var humanModel = Resources.Load<GameObject>("Models/Human/Human_LowPoly");
            if (humanModel != null)
            {
                float importedHeight = archetype == HumanArchetype.Child ? 0.72f : archetype == HumanArchetype.Elder ? 0.92f : 1f;
                float importedWidth = archetype == HumanArchetype.Man ? 1.08f : archetype == HumanArchetype.Child ? 0.78f : 0.95f;
                root.localScale = new Vector3(importedWidth, importedHeight, importedWidth);
                var visual = Instantiate(humanModel, root);
                visual.name = $"{archetype} Model";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.identity;
                visual.transform.localScale = Vector3.one;
                PrepareImportedModel(visual);

                if (archetype == HumanArchetype.Woman)
                {
                    CreateVisualPrimitive(root, "hair_back_extra", PrimitiveType.Capsule, new Vector3(0f, 1.52f, -0.1f), new Vector3(0.16f, 0.18f, 0.1f), new Color(0.07f, 0.045f, 0.03f));
                }
                else if (archetype == HumanArchetype.Child)
                {
                    CreateVisualPrimitive(root, "small_backpack", PrimitiveType.Cube, new Vector3(0f, 1.0f, -0.18f), new Vector3(0.24f, 0.32f, 0.08f), new Color(0.12f, 0.22f, 0.48f));
                }
                else if (archetype == HumanArchetype.Elder)
                {
                    CreateVisualPrimitive(root, "walking_cane", PrimitiveType.Cube, new Vector3(0.42f, 0.52f, 0.1f), new Vector3(0.035f, 0.88f, 0.035f), new Color(0.24f, 0.14f, 0.07f), Quaternion.Euler(0f, 0f, -8f));
                }

                return root;
            }

            var skin = new Color(0.72f, 0.52f, 0.39f);
            var shirtColors = new[] { new Color(0.32f, 0.4f, 0.5f), new Color(0.48f, 0.28f, 0.26f), new Color(0.34f, 0.44f, 0.31f), new Color(0.42f, 0.34f, 0.5f) };
            var shirt = shirtColors[variant % shirtColors.Length];
            var pants = new Color(0.11f, 0.14f, 0.18f);
            float height = archetype == HumanArchetype.Child ? 0.72f : archetype == HumanArchetype.Elder ? 0.92f : 1f;
            float width = archetype == HumanArchetype.Man ? 1.08f : archetype == HumanArchetype.Child ? 0.78f : 0.95f;
            root.localScale = new Vector3(width, height, width);

            CreateVisualPrimitive(root, "torso", PrimitiveType.Capsule, new Vector3(0f, 1.05f, 0f), new Vector3(0.28f, 0.44f, 0.22f), shirt);
            CreateVisualPrimitive(root, "shirt_front", PrimitiveType.Cube, new Vector3(0f, 1.08f, 0.19f), new Vector3(0.34f, 0.48f, 0.035f), shirt * 1.12f);
            CreateVisualPrimitive(root, "left_shoulder", PrimitiveType.Sphere, new Vector3(-0.24f, 1.25f, 0f), new Vector3(0.11f, 0.1f, 0.1f), shirt);
            CreateVisualPrimitive(root, "right_shoulder", PrimitiveType.Sphere, new Vector3(0.24f, 1.25f, 0f), new Vector3(0.11f, 0.1f, 0.1f), shirt);
            CreateVisualPrimitive(root, "head", PrimitiveType.Sphere, new Vector3(0f, 1.65f, 0f), new Vector3(0.24f, 0.24f, 0.24f), skin);
            CreateVisualPrimitive(root, "hair", PrimitiveType.Sphere, new Vector3(0f, 1.78f, -0.03f), new Vector3(0.25f, 0.08f, 0.22f), archetype == HumanArchetype.Elder ? Color.gray : new Color(0.06f, 0.04f, 0.03f));
            if (archetype == HumanArchetype.Woman)
            {
                CreateVisualPrimitive(root, "back_hair", PrimitiveType.Capsule, new Vector3(0f, 1.57f, -0.12f), new Vector3(0.18f, 0.18f, 0.12f), new Color(0.07f, 0.045f, 0.03f));
            }

            CreateVisualPrimitive(root, "left_ear", PrimitiveType.Sphere, new Vector3(-0.22f, 1.64f, 0f), new Vector3(0.04f, 0.06f, 0.035f), skin);
            CreateVisualPrimitive(root, "right_ear", PrimitiveType.Sphere, new Vector3(0.22f, 1.64f, 0f), new Vector3(0.04f, 0.06f, 0.035f), skin);
            CreateVisualPrimitive(root, "left_eye", PrimitiveType.Sphere, new Vector3(-0.055f, 1.68f, 0.2f), new Vector3(0.028f, 0.028f, 0.018f), Color.black);
            CreateVisualPrimitive(root, "right_eye", PrimitiveType.Sphere, new Vector3(0.055f, 1.68f, 0.2f), new Vector3(0.028f, 0.028f, 0.018f), Color.black);
            CreateVisualPrimitive(root, "nose", PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0.23f), new Vector3(0.035f, 0.045f, 0.04f), skin * 0.95f);
            CreateVisualPrimitive(root, "mouth", PrimitiveType.Cube, new Vector3(0f, 1.55f, 0.235f), new Vector3(0.1f, 0.015f, 0.012f), new Color(0.32f, 0.08f, 0.07f));
            CreateVisualPrimitive(root, "left_arm", PrimitiveType.Cube, new Vector3(-0.32f, 1f, 0f), new Vector3(0.08f, 0.72f, 0.08f), skin, Quaternion.Euler(0f, 0f, -8f));
            CreateVisualPrimitive(root, "right_arm", PrimitiveType.Cube, new Vector3(0.32f, 1f, 0f), new Vector3(0.08f, 0.72f, 0.08f), skin, Quaternion.Euler(0f, 0f, 8f));
            CreateVisualPrimitive(root, "left_hand", PrimitiveType.Sphere, new Vector3(-0.38f, 0.62f, 0f), new Vector3(0.07f, 0.06f, 0.06f), skin);
            CreateVisualPrimitive(root, "right_hand", PrimitiveType.Sphere, new Vector3(0.38f, 0.62f, 0f), new Vector3(0.07f, 0.06f, 0.06f), skin);
            CreateVisualPrimitive(root, "left_leg", PrimitiveType.Cube, new Vector3(-0.1f, 0.45f, 0f), new Vector3(0.1f, 0.82f, 0.1f), pants);
            CreateVisualPrimitive(root, "right_leg", PrimitiveType.Cube, new Vector3(0.1f, 0.45f, 0f), new Vector3(0.1f, 0.82f, 0.1f), pants);
            CreateVisualPrimitive(root, "left_knee", PrimitiveType.Sphere, new Vector3(-0.1f, 0.44f, 0.06f), new Vector3(0.07f, 0.055f, 0.055f), pants * 1.2f);
            CreateVisualPrimitive(root, "right_knee", PrimitiveType.Sphere, new Vector3(0.1f, 0.44f, 0.06f), new Vector3(0.07f, 0.055f, 0.055f), pants * 1.2f);
            CreateVisualPrimitive(root, "left_foot", PrimitiveType.Cube, new Vector3(-0.1f, 0.07f, 0.13f), new Vector3(0.14f, 0.08f, 0.26f), Color.black);
            CreateVisualPrimitive(root, "right_foot", PrimitiveType.Cube, new Vector3(0.1f, 0.07f, 0.13f), new Vector3(0.14f, 0.08f, 0.26f), Color.black);
            if (archetype == HumanArchetype.Woman)
            {
                CreateVisualPrimitive(root, "skirt_hint", PrimitiveType.Cube, new Vector3(0f, 0.74f, 0f), new Vector3(0.42f, 0.18f, 0.34f), shirt * 0.85f);
            }
            else if (archetype == HumanArchetype.Child)
            {
                CreateVisualPrimitive(root, "backpack", PrimitiveType.Cube, new Vector3(0f, 1.06f, -0.2f), new Vector3(0.28f, 0.36f, 0.08f), new Color(0.12f, 0.22f, 0.48f));
            }
            else if (archetype == HumanArchetype.Elder)
            {
                CreateVisualPrimitive(root, "cane", PrimitiveType.Cube, new Vector3(0.45f, 0.56f, 0.12f), new Vector3(0.035f, 0.95f, 0.035f), new Color(0.24f, 0.14f, 0.07f), Quaternion.Euler(0f, 0f, -8f));
            }

            return root;
        }

        private void BuildPet(PetKind kind)
        {
            var petObject = new GameObject($"Pet - {kind}");
            petObject.transform.SetParent(runRoot);
            petObject.transform.position = RandomOpenFloorPosition(0.7f);
            AddPetVisual(petObject.transform, kind);
            var controller = petObject.AddComponent<PetController>();
            controller.Configure(kind);
        }

        private void AddPetVisual(Transform parent, PetKind kind)
        {
            var fur = kind == PetKind.Cat ? new Color(0.58f, 0.54f, 0.48f) : new Color(0.46f, 0.28f, 0.16f);
            CreateVisualPrimitive(parent, "pet_body", PrimitiveType.Capsule, new Vector3(0f, 0.22f, 0f), new Vector3(0.22f, 0.34f, 0.18f), fur, Quaternion.Euler(90f, 0f, 0f));
            CreateVisualPrimitive(parent, "pet_head", PrimitiveType.Sphere, new Vector3(0f, 0.32f, 0.34f), new Vector3(0.16f, 0.14f, 0.14f), fur);
            CreateVisualPrimitive(parent, "pet_tail", PrimitiveType.Cube, new Vector3(0f, 0.32f, -0.36f), new Vector3(0.055f, 0.055f, 0.36f), fur, Quaternion.Euler(kind == PetKind.Cat ? -28f : 18f, 0f, 0f));
            CreateVisualPrimitive(parent, "pet_left_ear", PrimitiveType.Cube, new Vector3(-0.08f, 0.44f, 0.38f), new Vector3(0.07f, 0.08f, 0.035f), fur, Quaternion.Euler(0f, 0f, 28f));
            CreateVisualPrimitive(parent, "pet_right_ear", PrimitiveType.Cube, new Vector3(0.08f, 0.44f, 0.38f), new Vector3(0.07f, 0.08f, 0.035f), fur, Quaternion.Euler(0f, 0f, -28f));
            for (int i = 0; i < 4; i++)
            {
                float x = i % 2 == 0 ? -0.11f : 0.11f;
                float z = i < 2 ? 0.17f : -0.18f;
                CreateVisualPrimitive(parent, "pet_leg", PrimitiveType.Cube, new Vector3(x, 0.08f, z), new Vector3(0.045f, 0.16f, 0.045f), fur * 0.8f);
            }
        }

        private void BuildAmbientAudio()
        {
            var audioObject = new GameObject("Apartment Ambience");
            audioObject.transform.SetParent(runRoot);
            audioObject.transform.position = Vector3.zero;
            ambientAudioSource = audioObject.AddComponent<AudioSource>();
            ambientAudioSource.spatialBlend = 0f;
            ambientAudioSource.loop = true;
            ambientAudioSource.volume = 0.12f;
            ambientAudioSource.clip = ProceduralAudio.CreateHouseAmbience();
            ambientAudioSource.Play();

            musicAudioSource = audioObject.AddComponent<AudioSource>();
            musicAudioSource.spatialBlend = 0f;
            musicAudioSource.loop = true;
            musicAudioSource.volume = 0.035f;
            musicAudioSource.clip = ProceduralAudio.CreateSoftMusicLoop();
            musicAudioSource.Play();
        }

        private Vector3 RandomFloorPosition()
        {
            return new Vector3(UnityEngine.Random.Range(-7.5f, 7.5f), 0f, UnityEngine.Random.Range(-5.7f, 5.7f));
        }

        private Vector3 RandomOpenFloorPosition(float clearance)
        {
            for (int attempt = 0; attempt < 80; attempt++)
            {
                var candidate = RandomFloorPosition();
                if (IsFloorAreaClear(candidate, clearance))
                {
                    return candidate;
                }
            }

            var fallbacks = new[]
            {
                new Vector3(-7.2f, 0f, -0.2f),
                new Vector3(-3.4f, 0f, -2.6f),
                new Vector3(0.8f, 0f, 0.2f),
                new Vector3(2.2f, 0f, -5.8f),
                new Vector3(7.1f, 0f, 0.1f)
            };

            foreach (var point in fallbacks)
            {
                if (IsFloorAreaClear(point, clearance * 0.8f))
                {
                    return point;
                }
            }

            return Vector3.zero;
        }

        private Vector3 ChooseSafeSpawn(Vector3[] choices)
        {
            foreach (var choice in choices.OrderBy(_ => UnityEngine.Random.value))
            {
                var floor = new Vector3(choice.x, 0f, choice.z);
                if (IsFloorAreaClear(floor, 0.75f))
                {
                    return floor + Vector3.up * 0.1f;
                }
            }

            return RandomOpenFloorPosition(0.75f) + Vector3.up * 0.1f;
        }

        private bool IsFloorAreaClear(Vector3 position, float clearance)
        {
            if (position.x < -8.1f || position.x > 8.1f || position.z < -6.2f || position.z > 6.2f)
            {
                return false;
            }

            var probe = new Rect(position.x - clearance, position.z - clearance, clearance * 2f, clearance * 2f);
            return blockedFloorAreas.All(area => !area.Overlaps(probe));
        }

        private void RegisterBlockedArea(Vector3 position, Vector3 scale, float margin)
        {
            float width = Mathf.Max(0.5f, scale.x + margin * 2f);
            float depth = Mathf.Max(0.5f, scale.z + margin * 2f);
            blockedFloorAreas.Add(new Rect(position.x - width * 0.5f, position.z - depth * 0.5f, width, depth));
        }

        private void ShowEvent(string message)
        {
            eventMessageTimer = 4f;
            if (eventText != null)
            {
                eventText.text = message;
            }
        }

        private void UpdateEggHint()
        {
            if (eggHintObject == null || player == null)
            {
                return;
            }

            int availableEggs = foodItems.Count(food => food.Eaten) / 5 - eggsLaid;
            bool show = alive && player.IsHidden && availableEggs > 0;
            eggHintObject.SetActive(show);
            if (show)
            {
                eggHintObject.transform.position = player.transform.position + Vector3.up * 0.018f;
                float pulse = 0.72f + Mathf.Sin(Time.time * 5f) * 0.08f;
                eggHintObject.transform.localScale = new Vector3(pulse, 0.018f, pulse);
            }
        }

        private void CreateEggCluster(Vector3 position)
        {
            var root = new GameObject("Egg Cluster").transform;
            root.SetParent(runRoot);
            root.position = position + Vector3.up * 0.045f;

            CreateVisualPrimitive(root, "egg_glow", PrimitiveType.Cylinder, Vector3.down * 0.035f, new Vector3(0.42f, 0.012f, 0.42f), new Color(0.32f, 0.85f, 0.38f));
            for (int i = 0; i < 5; i++)
            {
                float angle = i * 72f * Mathf.Deg2Rad;
                var offset = new Vector3(Mathf.Cos(angle) * 0.09f, 0f, Mathf.Sin(angle) * 0.06f);
                CreateVisualPrimitive(root, "egg", PrimitiveType.Sphere, offset + Vector3.up * 0.025f, new Vector3(0.07f, 0.095f, 0.07f), new Color(0.94f, 0.88f, 0.68f));
            }
        }

        private void TryShowChallengePrompt()
        {
            if (challengeOfferShown || challengePromptActive || !alive || !AllTasksComplete())
            {
                return;
            }

            challengeOfferShown = true;
            challengePromptActive = true;
            Time.timeScale = 0f;
            if (challengePanel != null)
            {
                challengePanel.SetActive(true);
            }

            if (challengeText != null)
            {
                challengeText.text =
                    "本轮目标已完成\n\n" +
                    $"你已经存活 {FormatTime(survivalTime)}\n" +
                    "是否挑战更难任务？\n\n" +
                    "Enter / 空格：提高难度继续\n" +
                    "Backspace：只继续存活\n" +
                    "R：重新开局";
            }
        }

        private void HandleChallengePromptInput()
        {
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Space))
            {
                AcceptHarderChallenge();
            }
            else if (Input.GetKeyDown(KeyCode.Backspace) || Input.GetKeyDown(KeyCode.Escape))
            {
                CloseChallengePrompt();
                ShowEvent("继续存活挑战，当前任务不再弹窗");
            }
        }

        private void AcceptHarderChallenge()
        {
            int eaten = foodItems.Count(food => food.Eaten);
            challengeLevel += 1;
            targetFoodCount = Mathf.Max(targetFoodCount + 5 + challengeLevel * 2, eaten + 4);
            targetEggCount = Mathf.Max(targetEggCount + 1, eggsLaid + 1);
            escapedAfterDetection = false;
            hasBeenDetected = false;
            challengeOfferShown = false;
            CloseChallengePrompt();
            ShowEvent($"难度提升：第 {challengeLevel + 1} 阶段，目标食物 {targetFoodCount} 种，产卵 {targetEggCount} 次");
        }

        private void CloseChallengePrompt()
        {
            challengePromptActive = false;
            Time.timeScale = 1f;
            if (challengePanel != null)
            {
                challengePanel.SetActive(false);
            }
        }

        private bool AllTasksComplete()
        {
            int eaten = foodItems.Count(food => food.Eaten);
            bool eggComplete = targetEggCount <= 0 || eggsLaid >= targetEggCount;
            return eaten >= targetFoodCount && eggComplete && escapedAfterDetection && alive;
        }

        private void UpdateUi()
        {
            if (eventText != null && eventMessageTimer > 0f)
            {
                eventMessageTimer -= Time.deltaTime;
                if (eventMessageTimer <= 0f)
                {
                    eventText.text = string.Empty;
                }
            }

            int eaten = foodItems.Count(food => food.Eaten);
            int availableEggs = foodItems.Count(food => food.Eaten) / 5 - eggsLaid;
            if (statusText != null)
            {
                string state = alive ? "存活中" : "已死亡";
                string hidden = player != null && player.IsHidden ? "隐藏" : "暴露";
                string eggState = availableEggs > 0
                    ? (player != null && player.IsHidden ? "可按 E" : "找家具阴影")
                    : "需再吃食物";
                statusText.text =
                    $"状态：{state}\n" +
                    $"存活时间：{FormatTime(survivalTime)}\n" +
                    $"声音：{Percent(player != null ? player.NoiseLevel : 0f)}  警觉：{Percent(suspicion)}\n" +
                    $"位置：{hidden}  产卵：{eggState}\n" +
                    "操作：WASD / 鼠标 / E产卵 / R重开";
            }

            if (tasksText != null)
            {
                string eggTask = targetEggCount <= 0
                    ? TaskLine(true, "本局没有强制产卵目标")
                    : TaskLine(eggsLaid >= targetEggCount, $"产卵目标：{eggsLaid}/{targetEggCount}");

                tasksText.text =
                    $"本局小任务  阶段 {challengeLevel + 1}\n" +
                    TaskLine(eaten >= targetFoodCount, $"吃到 {targetFoodCount} 种食物：{eaten}/{targetFoodCount}") +
                    eggTask +
                    $"可产卵机会：{Mathf.Max(0, availableEggs)} 次\n" +
                    "提示：隐藏时绿色圈=可产卵\n" +
                    TaskLine(escapedAfterDetection, "被发现后成功逃脱一次") +
                    TaskLine(alive, "核心目标：尽可能活得更久");
            }

            if (leaderboardText != null)
            {
                var scores = LoadScores();
                leaderboardText.text = "本地生存榜\n" +
                    (scores.Count == 0
                        ? "暂无记录"
                        : string.Join("\n", scores.Take(5).Select((score, index) => $"{index + 1}. {FormatTime(score)}")));
            }
        }

        private static string TaskLine(bool complete, string text)
        {
            return $"{(complete ? "[完成]" : "[ ]")} {text}\n";
        }

        private static string Percent(float value)
        {
            return Mathf.RoundToInt(Mathf.Clamp01(value) * 100f) + "%";
        }

        private static string FormatTime(float seconds)
        {
            var time = TimeSpan.FromSeconds(Mathf.Max(0f, seconds));
            return $"{(int)time.TotalMinutes:00}:{time.Seconds:00}";
        }

        private void SaveScore(float score)
        {
            var scores = LoadScores();
            scores.Add(score);
            scores = scores.OrderByDescending(item => item).Take(10).ToList();
            PlayerPrefs.SetString(LeaderboardKey, string.Join("|", scores.Select(item => item.ToString(CultureInfo.InvariantCulture))));
            PlayerPrefs.Save();
        }

        private static List<float> LoadScores()
        {
            var raw = PlayerPrefs.GetString(LeaderboardKey, string.Empty);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return new List<float>();
            }

            return raw.Split('|')
                .Select(item => float.TryParse(item, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) ? value : 0f)
                .Where(item => item > 0f)
                .OrderByDescending(item => item)
                .ToList();
        }
    }

    public sealed class CockroachPlayerController : MonoBehaviour
    {
        private CharacterController characterController;
        private AudioSource audioSource;
        private AudioClip crawlLoopClip;
        private AudioClip eatClip;
        private AudioClip eggClip;
        private AudioClip detectedClip;
        private AudioClip deathClip;
        private Vector3 currentPlanarVelocity;
        private float verticalVelocity;
        private float hideTimer;
        private float detectedSoundCooldown;
        private float mouseSensitivity = 3.2f;
        private int hideContacts;

        public float NoiseLevel { get; private set; }
        public float MoveIntensity { get; private set; }
        public bool IsSprinting { get; private set; }
        public bool IsHidden => hideContacts > 0;

        private void Awake()
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.18f;
            characterController.height = 0.22f;
            characterController.center = new Vector3(0f, 0.11f, 0f);
            characterController.stepOffset = 0.05f;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1.5f;
            audioSource.maxDistance = 18f;
            audioSource.volume = 0.18f;

            crawlLoopClip = ProceduralAudio.CreateCrawlLoop();
            eatClip = ProceduralAudio.CreateClickBurst("Eat Crunch", 0.28f, 760f, 9);
            eggClip = ProceduralAudio.CreateClickBurst("Lay Egg", 0.34f, 420f, 5);
            detectedClip = ProceduralAudio.CreateTone("Detected Sting", 0.36f, 930f, 0.22f);
            deathClip = ProceduralAudio.CreateTone("Death Thud", 0.52f, 120f, 0.5f);

            audioSource.clip = crawlLoopClip;
            audioSource.loop = true;
            audioSource.Play();
            LockCursor();
        }

        private void Update()
        {
            detectedSoundCooldown -= Time.deltaTime;
            if (CockroachGameManager.Instance == null || !CockroachGameManager.Instance.Alive)
            {
                MoveIntensity = 0f;
                IsSprinting = false;
                UpdateCrawlAudio(0f, false);
                return;
            }

            if (CockroachGameManager.Instance.ChallengePromptActive)
            {
                MoveIntensity = 0f;
                IsSprinting = false;
                UpdateCrawlAudio(0f, false);
                return;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (Input.GetMouseButtonDown(0))
            {
                LockCursor();
            }

            float strafeInput = Input.GetAxisRaw("Horizontal");
            float forwardInput = Input.GetAxisRaw("Vertical");
            float mouseX = Input.GetAxisRaw("Mouse X");
            IsSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float forwardSpeed = IsSprinting ? 2.15f : 1.05f;
            float backwardSpeed = 0.58f;
            float strafeSpeed = IsSprinting ? 1.35f : 0.72f;
            float targetForwardSpeed = forwardInput >= 0f ? forwardInput * forwardSpeed : forwardInput * backwardSpeed;
            Vector3 targetPlanarVelocity = transform.forward * targetForwardSpeed + transform.right * (strafeInput * strafeSpeed);
            targetPlanarVelocity = Vector3.ClampMagnitude(targetPlanarVelocity, forwardSpeed);

            float acceleration = targetPlanarVelocity.sqrMagnitude > currentPlanarVelocity.sqrMagnitude ? 3.2f : 4.8f;
            currentPlanarVelocity = Vector3.MoveTowards(currentPlanarVelocity, targetPlanarVelocity, acceleration * Time.deltaTime);
            transform.Rotate(0f, mouseX * mouseSensitivity, 0f);
            MoveIntensity = Mathf.Clamp01(currentPlanarVelocity.magnitude / forwardSpeed);

            Vector3 move = currentPlanarVelocity;
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -0.2f;
            }

            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            move.y = verticalVelocity;
            characterController.Move(move * Time.deltaTime);

            float targetNoise = MoveIntensity * (IsSprinting ? 0.54f : 0.2f);
            if (IsHidden)
            {
                targetNoise *= 0.35f;
                hideTimer += Time.deltaTime;
                if (hideTimer > 3f && CockroachGameManager.Instance.HasBeenDetected)
                {
                    CockroachGameManager.Instance.MarkEscaped();
                }
            }
            else
            {
                hideTimer = 0f;
            }

            NoiseLevel = Mathf.MoveTowards(NoiseLevel, targetNoise, Time.deltaTime * 1.3f);
            UpdateCrawlAudio(MoveIntensity, IsSprinting);
        }

        public void AddNoise(float amount)
        {
            NoiseLevel = Mathf.Clamp01(NoiseLevel + amount);
        }

        public void PlayEatSound()
        {
            PlayOneShot(eatClip, 0.55f);
        }

        public void PlayEggSound()
        {
            PlayOneShot(eggClip, 0.38f);
        }

        public void PlayDetectedSound()
        {
            if (detectedSoundCooldown > 0f)
            {
                return;
            }

            detectedSoundCooldown = 2.5f;
            PlayOneShot(detectedClip, 0.42f);
        }

        public void PlayDeathSound()
        {
            PlayOneShot(deathClip, 0.5f);
        }

        private void UpdateCrawlAudio(float movement, bool sprinting)
        {
            if (audioSource == null)
            {
                return;
            }

            float hiddenMultiplier = IsHidden ? 0.35f : 1f;
            audioSource.volume = movement * hiddenMultiplier * (sprinting ? 0.075f : 0.032f);
            audioSource.pitch = sprinting ? 1.35f : 0.9f;
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (audioSource != null && clip != null)
            {
                audioSource.PlayOneShot(clip, volume);
            }
        }

        private void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.TryGetComponent<HideSpot>(out _))
            {
                hideContacts += 1;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.TryGetComponent<HideSpot>(out _))
            {
                hideContacts = Mathf.Max(0, hideContacts - 1);
            }
        }
    }

    public sealed class FoodItem : MonoBehaviour
    {
        private const float CaptureRadius = 0.95f;

        public string DisplayName { get; set; }
        public bool Eaten { get; private set; }

        private void Update()
        {
            var game = CockroachGameManager.Instance;
            if (Eaten || game == null || !game.Alive || game.Player == null)
            {
                return;
            }

            if (Vector3.Distance(transform.position, game.Player.transform.position) <= CaptureRadius)
            {
                game.EatFood(this);
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (Eaten || other.GetComponentInParent<CockroachPlayerController>() == null)
            {
                return;
            }

            CockroachGameManager.Instance.EatFood(this);
        }

        public void MarkEaten()
        {
            Eaten = true;
            gameObject.SetActive(false);
        }
    }

    public sealed class HideSpot : MonoBehaviour
    {
    }

    public enum HumanArchetype
    {
        Man,
        Woman,
        Child,
        Elder
    }

    public enum HumanActivity
    {
        Standing,
        Sitting,
        Lying,
        Eating
    }

    public enum PetKind
    {
        Cat,
        Dog
    }

    public readonly struct PersonSpec
    {
        public PersonSpec(string displayName, HumanArchetype archetype)
        {
            DisplayName = displayName;
            Archetype = archetype;
        }

        public string DisplayName { get; }
        public HumanArchetype Archetype { get; }
    }

    public static class ProceduralAudio
    {
        private const int SampleRate = 22050;

        public static AudioClip CreateCrawlLoop()
        {
            float duration = 0.75f;
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            var random = new System.Random(17);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float tick = 0f;
                for (float start = 0f; start < duration; start += 0.085f)
                {
                    float local = t - start;
                    if (local >= 0f && local < 0.026f)
                    {
                        float envelope = 1f - local / 0.026f;
                        float noise = (float)(random.NextDouble() * 2.0 - 1.0);
                        tick += noise * envelope * 0.07f;
                    }
                }

                data[i] = Mathf.Clamp(tick, -0.45f, 0.45f);
            }

            var clip = AudioClip.Create("Procedural Crawl Loop", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateClickBurst(string name, float duration, float pitch, int bursts)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            var random = new System.Random(name.GetHashCode());
            var starts = new float[bursts];

            for (int burst = 0; burst < bursts; burst++)
            {
                starts[burst] = burst * duration / bursts + (float)random.NextDouble() * 0.018f;
            }

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float value = 0f;
                for (int burst = 0; burst < bursts; burst++)
                {
                    float start = starts[burst];
                    float local = t - start;
                    if (local >= 0f && local < 0.04f)
                    {
                        float envelope = Mathf.Exp(-local * 55f);
                        float grit = (float)(random.NextDouble() * 2.0 - 1.0);
                        value += (Mathf.Sin(2f * Mathf.PI * pitch * local) * 0.35f + grit * 0.65f) * envelope;
                    }
                }

                data[i] = Mathf.Clamp(value * 0.38f, -0.8f, 0.8f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateTone(string name, float duration, float frequency, float noiseAmount)
        {
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            var random = new System.Random(name.GetHashCode());

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float envelope = Mathf.Exp(-t * 5f) * Mathf.Clamp01(1f - t / duration);
                float tone = Mathf.Sin(2f * Mathf.PI * frequency * t);
                float noise = (float)(random.NextDouble() * 2.0 - 1.0) * noiseAmount;
                data[i] = Mathf.Clamp((tone + noise) * envelope * 0.45f, -0.8f, 0.8f);
            }

            var clip = AudioClip.Create(name, sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateHouseAmbience()
        {
            float duration = 3.2f;
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            var random = new System.Random(51);

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float refrigerator = Mathf.Sin(2f * Mathf.PI * 54f * t) * 0.02f;
                float roomTone = Mathf.Sin(2f * Mathf.PI * 116f * t) * 0.012f;
                float distant = (float)(random.NextDouble() * 2.0 - 1.0) * 0.006f;
                float occasional = 0f;
                for (float start = 0.6f; start < duration; start += 1.15f)
                {
                    float local = t - start;
                    if (local >= 0f && local < 0.08f)
                    {
                        occasional += Mathf.Exp(-local * 28f) * Mathf.Sin(2f * Mathf.PI * 250f * local) * 0.012f;
                    }
                }

                data[i] = Mathf.Clamp(refrigerator + roomTone + distant + occasional, -0.25f, 0.25f);
            }

            var clip = AudioClip.Create("House Ambience", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        public static AudioClip CreateSoftMusicLoop()
        {
            float duration = 8f;
            int sampleCount = Mathf.CeilToInt(SampleRate * duration);
            var data = new float[sampleCount];
            float[] notes = { 220f, 261.63f, 329.63f, 392f };

            for (int i = 0; i < sampleCount; i++)
            {
                float t = i / (float)SampleRate;
                float value = 0f;
                for (int n = 0; n < notes.Length; n++)
                {
                    float local = Mathf.Repeat(t - n * 1.6f, duration);
                    float envelope = Mathf.Clamp01(1f - local / 2.4f) * Mathf.Clamp01(local / 0.35f);
                    value += Mathf.Sin(2f * Mathf.PI * notes[n] * t) * envelope * 0.08f;
                    value += Mathf.Sin(2f * Mathf.PI * notes[n] * 2f * t) * envelope * 0.018f;
                }

                float slowPulse = Mathf.Sin(2f * Mathf.PI * 0.18f * t) * 0.015f;
                data[i] = Mathf.Clamp(value + slowPulse, -0.25f, 0.25f);
            }

            var clip = AudioClip.Create("Soft Ambient Music", sampleCount, 1, SampleRate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }

    public sealed class CockroachVisualAnimator : MonoBehaviour
    {
        private readonly List<AnimatedPart> legs = new List<AnimatedPart>();
        private readonly List<AnimatedPart> antennas = new List<AnimatedPart>();
        private readonly List<AnimatedPart> bodyParts = new List<AnimatedPart>();
        private CockroachPlayerController player;

        private void Awake()
        {
            player = GetComponentInParent<CockroachPlayerController>();
            CacheParts();
        }

        private void CacheParts()
        {
            legs.Clear();
            antennas.Clear();
            bodyParts.Clear();

            foreach (var part in GetComponentsInChildren<Transform>(true))
            {
                if (part == transform)
                {
                    continue;
                }

                string lowerName = part.name.ToLowerInvariant();
                var animated = new AnimatedPart(part);
                if (lowerName.Contains("antenna"))
                {
                    antennas.Add(animated);
                }
                else if (lowerName.Contains("leg"))
                {
                    legs.Add(animated);
                }
                else if (lowerName.Contains("abdomen") || lowerName.Contains("thorax") || lowerName.Contains("head") || lowerName.Contains("cockroach body"))
                {
                    bodyParts.Add(animated);
                }
            }
        }

        private void Update()
        {
            if (player == null)
            {
                player = GetComponentInParent<CockroachPlayerController>();
                if (player == null)
                {
                    return;
                }
            }

            float movement = player.MoveIntensity;
            float gaitSpeed = Mathf.Lerp(5.5f, player.IsSprinting ? 18f : 11f, movement);
            float phase = Time.time * gaitSpeed;
            float activity = Mathf.Lerp(0.18f, 1f, movement);

            for (int i = 0; i < legs.Count; i++)
            {
                var leg = legs[i];
                string name = leg.Transform.name.ToLowerInvariant();
                float side = name.Contains("left") ? -1f : 1f;
                float rowOffset = i * 0.85f;
                float swing = Mathf.Sin(phase + rowOffset) * activity;
                float lift = Mathf.Cos(phase + rowOffset) * activity;
                leg.Transform.localRotation = leg.BaseRotation * Quaternion.Euler(lift * 10f, side * swing * 18f, swing * 8f);
            }

            for (int i = 0; i < antennas.Count; i++)
            {
                var antenna = antennas[i];
                float side = antenna.Transform.name.ToLowerInvariant().Contains("left") ? -1f : 1f;
                float sway = Mathf.Sin(Time.time * 3.2f + i * 1.7f) * (4f + activity * 8f);
                antenna.Transform.localRotation = antenna.BaseRotation * Quaternion.Euler(0f, side * sway, sway * 0.35f);
            }

            for (int i = 0; i < bodyParts.Count; i++)
            {
                var body = bodyParts[i];
                float bob = Mathf.Sin(phase * 0.5f + i * 0.7f) * movement * 0.012f;
                body.Transform.localPosition = body.BasePosition + new Vector3(0f, 0f, bob);
            }
        }

        private readonly struct AnimatedPart
        {
            public AnimatedPart(Transform transform)
            {
                Transform = transform;
                BaseRotation = transform.localRotation;
                BasePosition = transform.localPosition;
            }

            public Transform Transform { get; }
            public Quaternion BaseRotation { get; }
            public Vector3 BasePosition { get; }
        }
    }

    public sealed class HumanController : MonoBehaviour
    {
        private CharacterController characterController;
        private Transform visualRoot;
        private AudioSource audioSource;
        private AudioClip stepClip;
        private AudioClip sitClip;
        private AudioClip eatClip;
        private HumanArchetype archetype;
        private HumanActivity activity;
        private Vector3 home;
        private Vector3 destination;
        private float waitTimer;
        private float detectionCooldown;
        private float activitySoundTimer;
        private float idleLookTimer;
        private Quaternion idleLookRotation = Quaternion.identity;
        private bool chasing;

        public string DisplayName { get; set; }

        private void Awake()
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.35f;
            characterController.height = 1.8f;
            characterController.center = Vector3.up * 0.9f;
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1.2f;
            audioSource.maxDistance = 12f;
            audioSource.volume = 0.08f;
            stepClip = ProceduralAudio.CreateClickBurst("Human Footstep", 0.18f, 170f, 2);
            sitClip = ProceduralAudio.CreateTone("Human Sit Fabric", 0.26f, 95f, 0.35f);
            eatClip = ProceduralAudio.CreateClickBurst("Human Eating", 0.38f, 520f, 6);
            idleLookRotation = transform.rotation;
            PickDestination();
        }

        private void Update()
        {
            var game = CockroachGameManager.Instance;
            if (game == null || !game.Alive || game.Player == null)
            {
                return;
            }

            detectionCooldown -= Time.deltaTime;
            float playerDistance = Vector3.Distance(transform.position, game.Player.transform.position);
            bool closeContact = !game.Player.IsHidden && playerDistance < 1.35f;
            bool canSee = closeContact || CanSeePlayer(game.Player);
            bool canHear = closeContact || CanHearPlayer(game.Player);
            if (closeContact)
            {
                game.AddSuspicion(Time.deltaTime * 0.85f);
            }

            chasing = canSee || canHear || (chasing && playerDistance < 6.4f && !game.Player.IsHidden);

            if (chasing)
            {
                SetActivity(HumanActivity.Standing);
                if (detectionCooldown <= 0f)
                {
                    game.MarkDetected(this);
                    detectionCooldown = 2.2f;
                }

                MoveToward(game.Player.transform.position, 2.25f);
                if (Vector3.Distance(transform.position, game.Player.transform.position) < 0.65f && !game.Player.IsHidden)
                {
                    game.KillPlayer($"{DisplayName} 一脚踩中了你");
                }
            }
            else
            {
                Patrol();
            }
        }

        public void SetHome(Vector3 value)
        {
            home = value;
            PickDestination();
        }

        public void Configure(HumanArchetype value, Transform visual)
        {
            archetype = value;
            visualRoot = visual;
            if (characterController != null)
            {
                float height = archetype == HumanArchetype.Child ? 1.35f : archetype == HumanArchetype.Elder ? 1.65f : 1.8f;
                characterController.height = height;
                characterController.center = Vector3.up * (height * 0.5f);
                characterController.radius = archetype == HumanArchetype.Child ? 0.26f : 0.35f;
            }

            SetActivity(RandomIdleActivity());
        }

        private void Patrol()
        {
            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                LookAroundWhileIdle();
                UpdateActivitySounds();
                return;
            }

            SetActivity(HumanActivity.Standing);
            MoveToward(destination, 1.05f);
            if (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), destination) < 0.4f)
            {
                waitTimer = UnityEngine.Random.Range(1.2f, 4.5f);
                SetActivity(RandomIdleActivity());
                PickDestination();
            }
        }

        private void MoveToward(Vector3 target, float speed)
        {
            var current = new Vector3(transform.position.x, 0f, transform.position.z);
            var flatTarget = new Vector3(target.x, 0f, target.z);
            var direction = flatTarget - current;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            direction.Normalize();
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 8f);
            var move = direction * speed;
            move.y = Physics.gravity.y;
            characterController.Move(move * Time.deltaTime);
            activitySoundTimer -= Time.deltaTime;
            if (activitySoundTimer <= 0f)
            {
                activitySoundTimer = speed > 1.5f ? 0.48f : 0.82f;
                audioSource.PlayOneShot(stepClip, speed > 1.5f ? 0.12f : 0.055f);
            }
        }

        private void LookAroundWhileIdle()
        {
            idleLookTimer -= Time.deltaTime;
            if (idleLookTimer <= 0f)
            {
                Vector3 lookTarget = Vector3.zero;
                var game = CockroachGameManager.Instance;
                if (game != null && game.Player != null && !game.Player.IsHidden && Vector3.Distance(transform.position, game.Player.transform.position) < 4.2f)
                {
                    lookTarget = game.Player.transform.position;
                }
                else
                {
                    lookTarget = home + new Vector3(UnityEngine.Random.Range(-2.5f, 2.5f), 0f, UnityEngine.Random.Range(-2.2f, 2.2f));
                    lookTarget.x = Mathf.Clamp(lookTarget.x, -6.8f, 6.8f);
                    lookTarget.z = Mathf.Clamp(lookTarget.z, -5.0f, 5.0f);
                }

                var flat = new Vector3(lookTarget.x - transform.position.x, 0f, lookTarget.z - transform.position.z);
                if (flat.sqrMagnitude < 0.04f)
                {
                    flat = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0f, UnityEngine.Random.Range(-1f, 1f));
                    if (flat.sqrMagnitude < 0.04f)
                    {
                        flat = transform.forward;
                    }
                }

                idleLookRotation = Quaternion.LookRotation(flat.normalized);
                idleLookTimer = UnityEngine.Random.Range(0.8f, 1.8f);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, idleLookRotation, Time.deltaTime * 2.2f);
        }

        private HumanActivity RandomIdleActivity()
        {
            float roll = UnityEngine.Random.value;
            if (roll < 0.28f) return HumanActivity.Sitting;
            if (roll < 0.48f) return HumanActivity.Eating;
            if (roll < 0.62f && archetype != HumanArchetype.Child) return HumanActivity.Lying;
            return HumanActivity.Standing;
        }

        private void SetActivity(HumanActivity next)
        {
            if (activity == next && visualRoot != null)
            {
                return;
            }

            activity = next;
            if (visualRoot == null)
            {
                return;
            }

            switch (activity)
            {
                case HumanActivity.Sitting:
                    visualRoot.localPosition = new Vector3(0f, -0.38f, 0f);
                    visualRoot.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                    audioSource.PlayOneShot(sitClip, 0.22f);
                    break;
                case HumanActivity.Lying:
                    visualRoot.localPosition = new Vector3(0f, -0.73f, 0.2f);
                    visualRoot.localRotation = Quaternion.Euler(82f, 0f, 0f);
                    audioSource.PlayOneShot(sitClip, 0.18f);
                    break;
                case HumanActivity.Eating:
                    visualRoot.localPosition = Vector3.zero;
                    visualRoot.localRotation = Quaternion.Euler(6f, 0f, 0f);
                    audioSource.PlayOneShot(eatClip, 0.22f);
                    break;
                default:
                    visualRoot.localPosition = Vector3.zero;
                    visualRoot.localRotation = Quaternion.identity;
                    break;
            }
        }

        private void UpdateActivitySounds()
        {
            activitySoundTimer -= Time.deltaTime;
            if (activitySoundTimer > 0f)
            {
                return;
            }

            if (activity == HumanActivity.Eating)
            {
                activitySoundTimer = UnityEngine.Random.Range(0.7f, 1.4f);
                audioSource.PlayOneShot(eatClip, 0.18f);
            }
            else if (activity == HumanActivity.Sitting && UnityEngine.Random.value < 0.35f)
            {
                activitySoundTimer = UnityEngine.Random.Range(1.5f, 3.2f);
                audioSource.PlayOneShot(sitClip, 0.08f);
            }
            else
            {
                activitySoundTimer = 0.8f;
            }
        }

        private bool CanSeePlayer(CockroachPlayerController player)
        {
            if (player.IsHidden)
            {
                return false;
            }

            var toPlayer = player.transform.position - transform.position;
            var flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            float distance = flat.magnitude;
            if (distance < 1.6f)
            {
                return true;
            }

            if (distance > 7.2f)
            {
                return false;
            }

            if (Vector3.Angle(transform.forward, flat.normalized) > 82f)
            {
                return false;
            }

            return HasLineOfSight(player);
        }

        private bool CanHearPlayer(CockroachPlayerController player)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            float hearing = Mathf.Lerp(1.8f, 8.2f, player.NoiseLevel);
            if (player.IsHidden)
            {
                hearing *= 0.45f;
            }

            bool heard = distance < hearing && player.NoiseLevel > 0.12f;
            if (heard)
            {
                CockroachGameManager.Instance.AddSuspicion(Time.deltaTime * 0.42f);
            }

            return heard && (distance < 2.4f || CockroachGameManager.Instance.Suspicion > 0.22f);
        }

        private bool HasLineOfSight(CockroachPlayerController player)
        {
            Vector3 origin = transform.position + Vector3.up * 1.2f;
            Vector3 target = player.transform.position + Vector3.up * 0.12f;
            Vector3 direction = target - origin;
            if (!Physics.Raycast(origin, direction.normalized, out var hit, direction.magnitude + 0.1f))
            {
                return true;
            }

            return hit.collider.GetComponentInParent<CockroachPlayerController>() != null;
        }

        private void PickDestination()
        {
            var offset = new Vector3(UnityEngine.Random.Range(-4.5f, 4.5f), 0f, UnityEngine.Random.Range(-3.5f, 3.5f));
            destination = home + offset;
            destination.x = Mathf.Clamp(destination.x, -6.8f, 6.8f);
            destination.z = Mathf.Clamp(destination.z, -5.0f, 5.0f);
        }
    }

    public sealed class PetController : MonoBehaviour
    {
        private CharacterController characterController;
        private AudioSource audioSource;
        private AudioClip stepClip;
        private AudioClip callClip;
        private PetKind kind;
        private Vector3 destination;
        private float waitTimer;
        private float soundTimer;

        private void Awake()
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.22f;
            characterController.height = 0.48f;
            characterController.center = Vector3.up * 0.24f;

            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.minDistance = 1f;
            audioSource.maxDistance = 10f;
            audioSource.volume = 0.08f;
            stepClip = ProceduralAudio.CreateClickBurst("Pet Paws", 0.14f, 240f, 3);
            callClip = ProceduralAudio.CreateTone("Pet Call", 0.32f, 360f, 0.15f);
            PickDestination();
        }

        public void Configure(PetKind value)
        {
            kind = value;
        }

        private void Update()
        {
            var game = CockroachGameManager.Instance;
            if (game == null || !game.Alive || game.Player == null)
            {
                return;
            }

            soundTimer -= Time.deltaTime;
            float distanceToPlayer = Vector3.Distance(transform.position, game.Player.transform.position);
            bool curious = distanceToPlayer < (kind == PetKind.Cat ? 4.2f : 3.2f) && !game.Player.IsHidden;
            if (curious)
            {
                MoveToward(game.Player.transform.position, kind == PetKind.Cat ? 1.7f : 1.35f);
                game.AddSuspicion(Time.deltaTime * 0.12f);
                if (distanceToPlayer < 0.45f)
                {
                    game.KillPlayer(kind == PetKind.Cat ? "猫把你按住了" : "狗发现了你");
                }
                return;
            }

            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                if (soundTimer <= 0f && UnityEngine.Random.value < 0.02f)
                {
                    soundTimer = UnityEngine.Random.Range(3f, 6f);
                    audioSource.PlayOneShot(callClip, kind == PetKind.Cat ? 0.045f : 0.065f);
                }
                return;
            }

            MoveToward(destination, kind == PetKind.Cat ? 1.1f : 0.9f);
            if (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), destination) < 0.35f)
            {
                waitTimer = UnityEngine.Random.Range(1.5f, 4.2f);
                PickDestination();
            }
        }

        private void MoveToward(Vector3 target, float speed)
        {
            var current = new Vector3(transform.position.x, 0f, transform.position.z);
            var flatTarget = new Vector3(target.x, 0f, target.z);
            var direction = flatTarget - current;
            if (direction.sqrMagnitude < 0.01f)
            {
                return;
            }

            direction.Normalize();
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), Time.deltaTime * 10f);
            var move = direction * speed;
            move.y = Physics.gravity.y;
            characterController.Move(move * Time.deltaTime);

            if (soundTimer <= 0f)
            {
                soundTimer = 0.36f;
                audioSource.PlayOneShot(stepClip, 0.045f);
            }
        }

        private void PickDestination()
        {
            destination = new Vector3(UnityEngine.Random.Range(-7.4f, 7.4f), 0f, UnityEngine.Random.Range(-5.6f, 5.6f));
        }
    }

    public sealed class SimpleCameraFollow : MonoBehaviour
    {
        public Transform Target { get; set; }

        private Vector3 velocity;
        private float bob;

        private void LateUpdate()
        {
            if (Target == null)
            {
                return;
            }

            var player = Target.GetComponent<CockroachPlayerController>();
            float movement = player != null ? player.MoveIntensity : 0f;
            float bobSpeed = player != null && player.IsSprinting ? 18f : 10f;
            bob += Time.deltaTime * bobSpeed * movement;

            Vector3 eyeOffset = Vector3.up * (0.145f + Mathf.Sin(bob) * movement * 0.012f) + Target.forward * 0.42f;
            var desired = Target.position + eyeOffset;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.035f);

            Vector3 lookDirection = (Target.forward + Vector3.down * 0.025f).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection, Vector3.up), Time.deltaTime * 22f);
        }
    }
}
