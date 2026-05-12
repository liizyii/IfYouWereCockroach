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
        private float survivalTime;
        private float eventMessageTimer;
        private float suspicion;
        private int seed;
        private int familyCount;
        private int targetFoodCount;
        private int eggsLaid;
        private bool alive;
        private bool hasBeenDetected;
        private bool escapedAfterDetection;
        private System.Random random;

        public static CockroachGameManager Instance { get; private set; }

        public bool Alive => alive;
        public CockroachPlayerController Player => player;
        public float Suspicion => suspicion;
        public bool HasBeenDetected => hasBeenDetected;

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

            UpdateUi();
        }

        public void BeginNewRun()
        {
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
            alive = true;
            hasBeenDetected = false;
            escapedAfterDetection = false;
            familyCount = random.Next(1, 5);
            targetFoodCount = random.Next(10, 21);
            foodItems.Clear();
            humans.Clear();
            hideSpots.Clear();

            runRoot = new GameObject("Generated Apartment Run").transform;
            BuildApartment();
            BuildPlayer();
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

            if (foodItems.Count(food => food.Eaten) < 5)
            {
                ShowEvent("至少吃到 5 种食物后才能产卵");
                return;
            }

            if (!player.IsHidden)
            {
                ShowEvent("需要躲在家具底下或阴影处才敢产卵");
                return;
            }

            eggsLaid += 1;
            player.AddNoise(0.08f);
            ShowEvent($"产卵成功：{eggsLaid} 次");
        }

        private void BuildApartment()
        {
            CreatePrimitive("Floor", PrimitiveType.Cube, new Vector3(0f, -0.06f, 0f), new Vector3(18f, 0.12f, 14f), new Color(0.55f, 0.52f, 0.47f));
            CreatePrimitive("Back Wall", PrimitiveType.Cube, new Vector3(0f, 1.2f, 7f), new Vector3(18f, 2.4f, 0.18f), new Color(0.78f, 0.77f, 0.72f));
            CreatePrimitive("Front Wall", PrimitiveType.Cube, new Vector3(0f, 1.2f, -7f), new Vector3(18f, 2.4f, 0.18f), new Color(0.78f, 0.77f, 0.72f));
            CreatePrimitive("Left Wall", PrimitiveType.Cube, new Vector3(-9f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 14f), new Color(0.78f, 0.77f, 0.72f));
            CreatePrimitive("Right Wall", PrimitiveType.Cube, new Vector3(9f, 1.2f, 0f), new Vector3(0.18f, 2.4f, 14f), new Color(0.78f, 0.77f, 0.72f));

            CreateZone("Kitchen", new Vector3(-5.8f, 0.01f, 3.7f), new Vector3(5.5f, 0.02f, 5.3f), new Color(0.62f, 0.68f, 0.63f, 0.45f));
            CreateZone("Living Room", new Vector3(3.3f, 0.012f, 2.6f), new Vector3(9.7f, 0.02f, 6.8f), new Color(0.58f, 0.56f, 0.62f, 0.45f));
            CreateZone("Bedroom", new Vector3(2.8f, 0.014f, -4.3f), new Vector3(8.8f, 0.02f, 4.7f), new Color(0.66f, 0.57f, 0.53f, 0.45f));
            CreateZone("Bathroom", new Vector3(-5.8f, 0.016f, -4.4f), new Vector3(5.5f, 0.02f, 4.5f), new Color(0.55f, 0.66f, 0.72f, 0.45f));

            AddFurniture("冰箱", new Vector3(-7.1f, 0.7f, 5.5f), new Vector3(1.1f, 1.4f, 0.9f), new Color(0.82f, 0.86f, 0.85f), true);
            AddFurniture("灶台", new Vector3(-4.7f, 0.45f, 5.8f), new Vector3(2.2f, 0.9f, 0.8f), new Color(0.28f, 0.28f, 0.29f), true);
            AddFurniture("餐桌", new Vector3(-2.6f, 0.45f, 2.4f), new Vector3(2.2f, 0.28f, 1.4f), new Color(0.43f, 0.28f, 0.18f), true);
            AddFurniture("沙发", new Vector3(5.2f, 0.42f, 4.5f), new Vector3(3.2f, 0.84f, 1.2f), new Color(0.27f, 0.39f, 0.48f), true);
            AddFurniture("茶几", new Vector3(4.7f, 0.28f, 2.2f), new Vector3(1.9f, 0.26f, 1.1f), new Color(0.36f, 0.25f, 0.17f), true);
            AddFurniture("床", new Vector3(4.9f, 0.36f, -4.7f), new Vector3(3.2f, 0.7f, 2.2f), new Color(0.35f, 0.42f, 0.58f), true);
            AddFurniture("洗手台", new Vector3(-7.1f, 0.4f, -5.6f), new Vector3(1.2f, 0.8f, 0.8f), new Color(0.88f, 0.9f, 0.88f), true);

            int decorationCount = random.Next(5, 10);
            for (int i = 0; i < decorationCount; i++)
            {
                var position = RandomFloorPosition();
                var scale = new Vector3(UnityEngine.Random.Range(0.5f, 1.3f), UnityEngine.Random.Range(0.25f, 0.8f), UnityEngine.Random.Range(0.4f, 1.2f));
                AddFurniture("随机杂物", position + Vector3.up * (scale.y * 0.5f), scale, new Color(UnityEngine.Random.Range(0.25f, 0.75f), UnityEngine.Random.Range(0.25f, 0.75f), UnityEngine.Random.Range(0.25f, 0.75f)), UnityEngine.Random.value > 0.35f);
            }

            int foodCount = random.Next(targetFoodCount + 2, targetFoodCount + 8);
            for (int i = 0; i < foodCount; i++)
            {
                var name = foodNames[i % foodNames.Length];
                var food = CreatePrimitive($"Food - {name}", PrimitiveType.Sphere, RandomFloorPosition() + Vector3.up * 0.08f, Vector3.one * UnityEngine.Random.Range(0.14f, 0.24f), new Color(0.88f, UnityEngine.Random.Range(0.45f, 0.85f), 0.22f));
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
                light.intensity = 1f;
                light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            }
        }

        private void BuildPlayer()
        {
            var spawnChoices = new[]
            {
                new Vector3(-7.3f, 0.1f, 5.4f),
                new Vector3(-2.4f, 0.1f, 2.3f),
                new Vector3(5.2f, 0.1f, 4.2f),
                new Vector3(4.7f, 0.1f, -4.7f),
                RandomFloorPosition() + Vector3.up * 0.1f
            };

            var playerObject = new GameObject("Player Cockroach");
            playerObject.transform.SetParent(runRoot);
            playerObject.transform.position = spawnChoices[random.Next(spawnChoices.Length)];
            player = playerObject.AddComponent<CockroachPlayerController>();

            var cockroachModel = Resources.Load<GameObject>("Models/Cockroach/Cockroach_LowPoly");
            if (cockroachModel != null)
            {
                var visual = Instantiate(cockroachModel, playerObject.transform);
                visual.name = "Cockroach Model";
                visual.transform.localPosition = Vector3.zero;
                visual.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                visual.transform.localScale = Vector3.one * 0.75f;
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
        }

        private void BuildHumans()
        {
            string[] names = { "爸爸", "妈妈", "孩子", "老人" };
            for (int i = 0; i < familyCount; i++)
            {
                var humanObject = new GameObject($"Human - {names[i]}");
                humanObject.transform.SetParent(runRoot);
                humanObject.transform.position = RandomFloorPosition();

                var visual = CreatePrimitive("Human Visual", PrimitiveType.Capsule, humanObject.transform.position + Vector3.up * 0.9f, new Vector3(0.55f, 0.9f, 0.55f), new Color(0.72f, 0.54f, 0.42f));
                visual.transform.SetParent(humanObject.transform);
                visual.transform.localPosition = Vector3.up * 0.9f;
                Destroy(visual.GetComponent<Collider>());

                var controller = humanObject.AddComponent<HumanController>();
                controller.DisplayName = names[i];
                controller.SetHome(RandomFloorPosition());
                RegisterHuman(controller);
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
            camera.fieldOfView = 60f;

            var follow = camera.GetComponent<SimpleCameraFollow>();
            if (follow == null)
            {
                follow = camera.gameObject.AddComponent<SimpleCameraFollow>();
            }

            follow.Target = player.transform;
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
            canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasObject.AddComponent<GraphicRaycaster>();

            statusText = CreateText(canvasObject.transform, "Status", new Vector2(18f, -18f), TextAnchor.UpperLeft, 18, new Vector2(520f, 116f));
            tasksText = CreateText(canvasObject.transform, "Tasks", new Vector2(18f, -132f), TextAnchor.UpperLeft, 17, new Vector2(560f, 180f));
            leaderboardText = CreateText(canvasObject.transform, "Leaderboard", new Vector2(-18f, -18f), TextAnchor.UpperRight, 17, new Vector2(360f, 180f));
            eventText = CreateText(canvasObject.transform, "Event", new Vector2(0f, 52f), TextAnchor.LowerCenter, 19, new Vector2(900f, 72f));
        }

        private Text CreateText(Transform parent, string name, Vector2 anchoredPosition, TextAnchor anchor, int fontSize, Vector2 size)
        {
            var textObject = new GameObject(name);
            textObject.transform.SetParent(parent);
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
            text.alignment = anchor;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
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
            return gameObject;
        }

        private void CreateZone(string name, Vector3 position, Vector3 scale, Color color)
        {
            var zone = CreatePrimitive(name, PrimitiveType.Cube, position, scale, color);
            Destroy(zone.GetComponent<Collider>());
        }

        private void AddFurniture(string name, Vector3 position, Vector3 scale, Color color, bool createsHideSpot)
        {
            var furniture = CreatePrimitive(name, PrimitiveType.Cube, position, scale, color);
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

        private Vector3 RandomFloorPosition()
        {
            return new Vector3(UnityEngine.Random.Range(-7.5f, 7.5f), 0f, UnityEngine.Random.Range(-5.7f, 5.7f));
        }

        private void ShowEvent(string message)
        {
            eventMessageTimer = 4f;
            if (eventText != null)
            {
                eventText.text = message;
            }
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
            if (statusText != null)
            {
                string state = alive ? "存活中" : "已死亡";
                string hidden = player != null && player.IsHidden ? "隐藏" : "暴露";
                statusText.text =
                    $"状态：{state}\n" +
                    $"存活时间：{FormatTime(survivalTime)}\n" +
                    $"声音：{Percent(player != null ? player.NoiseLevel : 0f)}  警觉：{Percent(suspicion)}\n" +
                    $"位置状态：{hidden}  种子：{seed}\n" +
                    "WASD 移动 / Shift 疾跑 / E 产卵 / R 重开";
            }

            if (tasksText != null)
            {
                tasksText.text =
                    "本局小任务\n" +
                    TaskLine(eaten >= targetFoodCount, $"吃到 {targetFoodCount} 种食物：{eaten}/{targetFoodCount}") +
                    TaskLine(eggsLaid >= 1, $"产卵一次：{eggsLaid}/1") +
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
        private float verticalVelocity;
        private float hideTimer;
        private int hideContacts;

        public float NoiseLevel { get; private set; }
        public bool IsHidden => hideContacts > 0;

        private void Awake()
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.18f;
            characterController.height = 0.22f;
            characterController.center = new Vector3(0f, 0.11f, 0f);
            characterController.stepOffset = 0.05f;
        }

        private void Update()
        {
            if (CockroachGameManager.Instance == null || !CockroachGameManager.Instance.Alive)
            {
                return;
            }

            var input = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));
            input = Vector3.ClampMagnitude(input, 1f);
            bool sprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            float speed = sprinting ? 3.2f : 1.65f;

            var camera = Camera.main;
            Vector3 forward = camera != null ? camera.transform.forward : Vector3.forward;
            Vector3 right = camera != null ? camera.transform.right : Vector3.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 move = (forward * input.z + right * input.x) * speed;
            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -0.2f;
            }

            verticalVelocity += Physics.gravity.y * Time.deltaTime;
            move.y = verticalVelocity;
            characterController.Move(move * Time.deltaTime);

            if (input.sqrMagnitude > 0.01f)
            {
                var look = new Vector3(move.x, 0f, move.z);
                if (look.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), Time.deltaTime * 14f);
                }
            }

            float targetNoise = input.magnitude * (sprinting ? 0.6f : 0.28f);
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
        }

        public void AddNoise(float amount)
        {
            NoiseLevel = Mathf.Clamp01(NoiseLevel + amount);
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
        public string DisplayName { get; set; }
        public bool Eaten { get; private set; }

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

    public sealed class HumanController : MonoBehaviour
    {
        private CharacterController characterController;
        private Vector3 home;
        private Vector3 destination;
        private float waitTimer;
        private float detectionCooldown;
        private bool chasing;

        public string DisplayName { get; set; }

        private void Awake()
        {
            characterController = gameObject.AddComponent<CharacterController>();
            characterController.radius = 0.35f;
            characterController.height = 1.8f;
            characterController.center = Vector3.up * 0.9f;
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
            bool canSee = CanSeePlayer(game.Player);
            bool canHear = CanHearPlayer(game.Player);
            chasing = canSee || canHear || (chasing && Vector3.Distance(transform.position, game.Player.transform.position) < 5.5f && !game.Player.IsHidden);

            if (chasing)
            {
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

        private void Patrol()
        {
            if (waitTimer > 0f)
            {
                waitTimer -= Time.deltaTime;
                return;
            }

            MoveToward(destination, 1.05f);
            if (Vector3.Distance(new Vector3(transform.position.x, 0f, transform.position.z), destination) < 0.4f)
            {
                waitTimer = UnityEngine.Random.Range(0.8f, 2.6f);
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
        }

        private bool CanSeePlayer(CockroachPlayerController player)
        {
            if (player.IsHidden)
            {
                return false;
            }

            var toPlayer = player.transform.position - transform.position;
            var flat = new Vector3(toPlayer.x, 0f, toPlayer.z);
            if (flat.magnitude > 5.2f)
            {
                return false;
            }

            if (Vector3.Angle(transform.forward, flat.normalized) > 58f)
            {
                return false;
            }

            return HasLineOfSight(player);
        }

        private bool CanHearPlayer(CockroachPlayerController player)
        {
            float distance = Vector3.Distance(transform.position, player.transform.position);
            float hearing = Mathf.Lerp(1.4f, 6.5f, player.NoiseLevel);
            if (player.IsHidden)
            {
                hearing *= 0.45f;
            }

            bool heard = distance < hearing && player.NoiseLevel > 0.2f;
            if (heard)
            {
                CockroachGameManager.Instance.AddSuspicion(Time.deltaTime * 0.28f);
            }

            return heard && CockroachGameManager.Instance.Suspicion > 0.35f;
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
            destination.x = Mathf.Clamp(destination.x, -7.6f, 7.6f);
            destination.z = Mathf.Clamp(destination.z, -5.8f, 5.8f);
        }
    }

    public sealed class SimpleCameraFollow : MonoBehaviour
    {
        public Transform Target { get; set; }

        private Vector3 velocity;

        private void LateUpdate()
        {
            if (Target == null)
            {
                return;
            }

            var desired = Target.position + new Vector3(0f, 6.5f, -5.7f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.16f);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(54f, 0f, 0f), Time.deltaTime * 8f);
        }
    }
}
