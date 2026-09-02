using System.Collections.Generic;
using System.Linq;
using Tsumiki.Core;
using UnityEngine;

namespace Tsumiki.Runtime
{
    public sealed class AppBootstrap : MonoBehaviour
    {
        private enum Page { Home, Levels, Count, Free, Compare, View, Settings, DressUp, ParentGate, Parent, Store }
        private Page page, requestedMode;
        private Page pageAfterParentGate = Page.Parent;
        private BlockRenderer left, right;
        private readonly PuzzleGenerator generator = new();
        private HeightMap a, b;
        private IReadOnlyList<int> choices;
        private readonly List<int[,]> viewChoices = new();
        private int viewAnswerIndex = -1, selectedViewIndex = -1;
        private int level = 1, question = 1, correct;
        private string message = "";
        private bool waitingForNext;
        private int pendingTotal;
        private float problemRotation;
        private readonly List<bool?> results = new();
        private GUIStyle title, button, label, progressStyle, feedbackStyle;
        private Texture2D kuro;
        private Texture2D[] kuroOutfits;
        private readonly string[] outfitNames =
        {
            "いつもの くろ", "きいろい おうかん", "みずいろ ベレーぼう", "むらさきの めがね", "おはなの かんむり",
            "みどりの たんけんぼう", "ピンクの おおきな リボン", "ほしぞらの まほうぼう", "コックさんの ぼうし",
            "ひまわりの かざり", "にじいろ パーティーぼう", "いちごの かざり", "よつばの クローバー",
            "かいがらの かざり", "もみじの かんむり", "サンタさんの ぼうし", "うさぎの みみ",
            "しょうぼうしの ヘルメット", "かんごしさんの ぼうし", "けいさつかんの ぼうし", "そつぎょうぼう",
            "うちゅうひこうしの ヘルメット", "パイロットの ぼうし", "かいぞくの ぼうし", "きしの かんむり",
            "ドラゴンの つの", "ようせいの かんむり", "おつきさまの ティアラ", "ほうせきの おうかん",
            "ぎんがの おうかん", "ダイヤと にじの おうかん"
        };
        private readonly int[] outfitUnlocks =
        {
            0, 30, 60, 90, 120, 150, 180, 210, 240, 270, 300,
            330, 360, 390, 420, 450, 480, 510, 540, 570, 600,
            630, 660, 690, 720, 750, 780, 810, 840, 870, 900
        };
        private int outfitPage;
        private Texture2D cream, pink, mint, sky, yellow, lavender, green, red, referenceOrange;
        private readonly List<GameObject> freeGrid = new();
        private readonly List<GameObject> modeGround = new();
        private readonly List<GameObject> viewReferenceTiles = new();
        private GameObject selectionMarker;
        private int selectedX = 2, selectedY = 2;
        private bool freeBlocksHidden;
        private int parentGateAnswer;
        private AudioSource sfxSource;
        private AudioClip correctSound;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureApp() { if (!FindAnyObjectByType<AppBootstrap>()) new GameObject("つみき なんこ？").AddComponent<AppBootstrap>(); }

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.autorotateToPortrait = Screen.autorotateToPortraitUpsideDown = false;
            var camera = Camera.main ? Camera.main : new GameObject("Main Camera") { tag = "MainCamera" }.AddComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 6.15f;
            camera.backgroundColor = new Color(.66f, .87f, .96f, 1f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.transform.position = new Vector3(8, 7, -8); camera.transform.LookAt(new Vector3(0, 1, 0));
            left = new GameObject("ひだり").AddComponent<BlockRenderer>(); right = new GameObject("みぎ").AddComponent<BlockRenderer>();
            left.transform.position = Vector3.left * 2.25f; right.transform.position = Vector3.right * 2.25f;
            var light = new GameObject("ひかり").AddComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.1f; light.transform.rotation = Quaternion.Euler(50, -30, 0);
            kuro = Resources.Load<Texture2D>("Characters/kuro_neutral");
            kuroOutfits = new[]
            {
                kuro,
                Resources.Load<Texture2D>("Characters/kuro_crown"),
                Resources.Load<Texture2D>("Characters/kuro_beret"),
                Resources.Load<Texture2D>("Characters/kuro_glasses"),
                Resources.Load<Texture2D>("Characters/kuro_flowers"),
                Resources.Load<Texture2D>("Characters/kuro_explorer"),
                Resources.Load<Texture2D>("Characters/kuro_headbow"),
                Resources.Load<Texture2D>("Characters/kuro_wizard"),
                Resources.Load<Texture2D>("Characters/kuro_chef"),
                Resources.Load<Texture2D>("Characters/kuro_sunflower"),
                Resources.Load<Texture2D>("Characters/kuro_party"),
                Resources.Load<Texture2D>("Characters/kuro_strawberry"),
                Resources.Load<Texture2D>("Characters/kuro_clover"),
                Resources.Load<Texture2D>("Characters/kuro_seashell"),
                Resources.Load<Texture2D>("Characters/kuro_autumn"),
                Resources.Load<Texture2D>("Characters/kuro_santa"),
                Resources.Load<Texture2D>("Characters/kuro_bunny"),
                Resources.Load<Texture2D>("Characters/kuro_firefighter"),
                Resources.Load<Texture2D>("Characters/kuro_nurse"),
                Resources.Load<Texture2D>("Characters/kuro_police"),
                Resources.Load<Texture2D>("Characters/kuro_graduation"),
                Resources.Load<Texture2D>("Characters/kuro_astronaut"),
                Resources.Load<Texture2D>("Characters/kuro_pilot"),
                Resources.Load<Texture2D>("Characters/kuro_pirate"),
                Resources.Load<Texture2D>("Characters/kuro_knight"),
                Resources.Load<Texture2D>("Characters/kuro_dragon"),
                Resources.Load<Texture2D>("Characters/kuro_fairy"),
                Resources.Load<Texture2D>("Characters/kuro_moon"),
                Resources.Load<Texture2D>("Characters/kuro_jewel"),
                Resources.Load<Texture2D>("Characters/kuro_galaxy"),
                Resources.Load<Texture2D>("Characters/kuro_diamond")
            };
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.spatialBlend = 0f;
            correctSound = CreateCorrectSound();
            _ = AppPurchaseManager.Instance;
        }

        private void Update()
        {
            if (page != Page.Free) return;

            // iOS uses touch input directly. GetMouseButtonDown is not reliable on
            // every Unity/iOS input configuration, so only use it as a fallback.
            if (Input.touchCount > 0)
            {
                var touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began) SelectFreeCell(touch.position);
            }
            else if (Input.GetMouseButtonDown(0)) SelectFreeCell(Input.mousePosition);
        }

        private void SelectFreeCell(Vector2 screenPosition)
        {
            var camera = Camera.main;
            if (!camera) return;
            var ray = camera.ScreenPointToRay(screenPosition);
            var floor = new Plane(Vector3.up, Vector3.zero);
            if (!floor.Raycast(ray, out var distance)) return;
            var worldPoint = ray.GetPoint(distance);
            var localPoint = Quaternion.Inverse(left.transform.rotation) * (worldPoint - left.transform.position);
            var x = Mathf.FloorToInt(localPoint.x + a.Width * .5f);
            var y = Mathf.FloorToInt(localPoint.z + a.Depth * .5f);
            if (!a.InBounds(x, y)) return;
            selectedX = x;
            selectedY = y;
            UpdateSelectionMarker();
        }

        private void OnGUI()
        {
            Styles();
            var scale = Mathf.Min(Screen.width / 1194f, Screen.height / 834f);
            var offsetX = (Screen.width - 1194f * scale) * .5f;
            var offsetY = (Screen.height - 834f * scale) * .5f;
            GUI.matrix = Matrix4x4.TRS(new Vector3(offsetX, offsetY, 0), Quaternion.identity, Vector3.one * scale);
            DrawWallpaper();
            // OnGUI is rendered in front of the 3D camera. Keep decoration at the
            // edges so the blocks in the center remain visible.
            GUI.DrawTexture(new Rect(0, 0, 1194, 18), lavender);
            GUI.DrawTexture(new Rect(0, 816, 1194, 18), lavender);
            GUI.DrawTexture(new Rect(0, 0, 18, 834), lavender);
            GUI.DrawTexture(new Rect(1176, 0, 18, 834), lavender);
            GUI.color = new Color(1f, 1f, 1f, .38f);
            GUI.DrawTexture(new Rect(18, 18, 1158, 125), sky);
            GUI.DrawTexture(new Rect(18, 650, 1158, 166), cream);
            GUI.color = Color.white;
            switch (page)
            {
                case Page.Home: Home(); break; case Page.Levels: Levels(); break; case Page.Count: Count(); break;
                case Page.Free: Free(); break; case Page.Compare: Compare(); break; case Page.View: View(); break;
                case Page.Settings: Settings(); break; case Page.DressUp: DressUp(); break;
                case Page.ParentGate: ParentGate(); break; case Page.Parent: Parent(); break; case Page.Store: Store(); break;
            }
        }

        private void DrawWallpaper()
        {
            var wallpaperStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 27,
                alignment = TextAnchor.MiddleCenter,
                fontStyle = FontStyle.Bold
            };
            wallpaperStyle.normal.textColor = new Color(.30f, .48f, .62f, .10f);
            var scaledMatrix = GUI.matrix;
            for (var row = 0; row < 7; row++)
            {
                for (var column = -1; column < 6; column++)
                {
                    var x = column * 245f + (row % 2) * 110f;
                    var y = row * 132f - 25f;
                    var rect = new Rect(x, y, 245f, 48f);
                    GUIUtility.RotateAroundPivot(-14f, rect.center);
                    GUI.Label(rect, "つみき なんこ？", wallpaperStyle);
                    GUI.matrix = scaledMatrix;
                }
            }
        }

        private void Styles()
        {
            if (button != null) return;
            title = new GUIStyle(GUI.skin.label) { fontSize = 54, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            title.normal.textColor = TsumikiPalette.Outline;
            label = new GUIStyle(title) { fontSize = 36, fontStyle = FontStyle.Normal, wordWrap = true };
            button = new GUIStyle(GUI.skin.button) { fontSize = 36, alignment = TextAnchor.MiddleCenter, wordWrap = true };
            progressStyle = new GUIStyle(label) { fontSize = 40, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            feedbackStyle = new GUIStyle(progressStyle) { fontSize = 43 };
            cream = Solid(new Color32(255, 250, 239, 255)); pink = Solid(new Color32(255, 205, 216, 255));
            mint = Solid(new Color32(192, 235, 220, 255)); sky = Solid(new Color32(190, 224, 255, 255));
            yellow = Solid(new Color32(255, 231, 156, 255)); lavender = Solid(new Color32(220, 205, 246, 255));
            green = Solid(new Color32(165, 225, 174, 255)); red = Solid(new Color32(255, 181, 181, 255));
            referenceOrange = Solid(new Color32(242, 122, 38, 255));
        }

        private static Texture2D Solid(Color color)
        {
            var texture = new Texture2D(1, 1); texture.SetPixel(0, 0, color); texture.Apply(); return texture;
        }

        private GUIStyle ColorButton(Texture2D color)
        {
            var style = new GUIStyle(button); style.normal.background = color; style.hover.background = color;
            style.active.background = yellow; style.normal.textColor = TsumikiPalette.Outline;
            style.border = new RectOffset(12, 12, 12, 12); return style;
        }

        private bool ChoiceButton(Rect rect, string text, Texture2D color)
        {
            GUI.DrawTexture(new Rect(rect.x-9,rect.y-9,rect.width+18,rect.height+18),lavender);
            GUI.DrawTexture(new Rect(rect.x-4,rect.y-4,rect.width+8,rect.height+8),cream);
            var starStyle = new GUIStyle(progressStyle) { fontSize = 28 };
            starStyle.normal.textColor = new Color(.92f,.48f,.12f);
            GUI.Label(new Rect(rect.x-18,rect.y-20,38,38),"★",starStyle);
            GUI.Label(new Rect(rect.x+rect.width-20,rect.y-20,38,38),"★",starStyle);
            GUI.Label(new Rect(rect.x-18,rect.y+rect.height-18,38,38),"✦",starStyle);
            GUI.Label(new Rect(rect.x+rect.width-20,rect.y+rect.height-18,38,38),"✦",starStyle);
            return GUI.Button(rect,text,ColorButton(color));
        }

        private void Home()
        {
            Clear();
            GUI.color = new Color(1f,1f,1f,.82f); GUI.DrawTexture(new Rect(220,32,754,92),lavender);
            GUI.DrawTexture(new Rect(232,42,730,72),cream); GUI.color = Color.white;
            GUI.Label(new Rect(250,42,694,58),"つみき なんこ？",title);
            var subtitle = new GUIStyle(label) { fontSize=26,alignment=TextAnchor.MiddleCenter };
            subtitle.normal.textColor=new Color(.42f,.34f,.38f); GUI.Label(new Rect(350,94,494,28),"みて・かぞえて・かんがえよう",subtitle);
            var ornament = new GUIStyle(progressStyle) { fontSize=30 }; ornament.normal.textColor=new Color(.76f,.58f,.22f);
            GUI.Label(new Rect(198,55,45,45),"✦",ornament); GUI.Label(new Rect(950,55,45,45),"✦",ornament);
            GUI.color=new Color(1f,1f,1f,.72f);GUI.DrawTexture(new Rect(315,135,800,294),cream);GUI.color=Color.white;
            var currentKuro = CurrentKuro();
            if (currentKuro && PlayerPrefs.GetInt("kuro", 1) == 1) GUI.DrawTexture(new Rect(45, 145, 250, 250), currentKuro, ScaleMode.ScaleToFit, true);
            Mode(new Rect(350,155,345,95), "① ブロックは\nいくつ？", Page.Count, pink); Mode(new Rect(735,155,345,95), "② じゆうに\nつんでみよう", Page.Free, mint);
            Mode(new Rect(350,295,345,95), "③ どっちが\nおおい？", Page.Compare, sky); Mode(new Rect(735,295,345,95), "④ どこから\nみてる？", Page.View, yellow);
            if (GUI.Button(new Rect(380,465,230,70), "⚙ せってい", ColorButton(lavender))) page = Page.Settings;
            var parentButton = new GUIStyle(ColorButton(pink)) { fontSize = 30, wordWrap = false };
            if (GUI.Button(new Rect(635,465,320,70), "⌂ おうちの かたへ", parentButton)) OpenParentGate(Page.Parent);
        }

        private void Mode(Rect r, string text, Page mode, Texture2D color)
        {
            GUI.DrawTexture(new Rect(r.x-7,r.y+7,r.width+14,r.height+8),lavender);
            GUI.DrawTexture(new Rect(r.x-3,r.y-3,r.width+6,r.height+6),cream);
            if (!GUI.Button(r, text, ColorButton(color))) return; requestedMode = mode;
            if (mode == Page.Free) Start(mode); else page = Page.Levels;
        }

        private void Levels()
        {
            Header("どの レベルで あそぶ？");
            for (var i = 1; i <= 5; i++)
            {
                var name = i <= 2 ? "★☆☆ やさしい" : i <= 4 ? "★★☆ ふつう" : "★★★ むずかしい";
                var unlocked = i <= 2 || (i <= 4 ? AppPurchaseManager.IntermediateUnlocked : AppPurchaseManager.AdvancedUnlocked);
                var levelText = unlocked ? $"{name}　{i}" : $"🔒 {name}　{i}";
                if (ChoiceButton(new Rect(330,120+(i-1)*105,535,78), levelText, new[]{mint,sky,yellow,pink,lavender}[i-1]))
                {
                    if (unlocked) { level = i; Start(requestedMode); }
                    else OpenParentGate(Page.Store);
                }
            }
            GUI.Label(new Rect(280,655,635,55), message, label);
        }

        private void Start(Page mode)
        {
            CancelInvoke(nameof(AdvanceAfterCorrect));
            page = mode; question = 1; correct = 0; message = ""; waitingForNext = false; problemRotation = 0f; results.Clear();
            var count = mode == Page.Count ? 10 : 8; for (var i = 0; i < count; i++) results.Add(null);
            if (mode == Page.Free)
            {
                SetViewCamera(false);
                freeBlocksHidden = false;
                a = new HeightMap(6,6,6); a[2,2] = 1; selectedX = selectedY = 2;
                left.transform.position = Vector3.zero; left.Show(a); CreateFreeGrid(); UpdateSelectionMarker();
            }
            else Next();
        }

        private void Next()
        {
            message = ""; waitingForNext = false; problemRotation = 0f;
            left.transform.rotation = Quaternion.identity; right.transform.rotation = Quaternion.identity; Clear();
            SetViewCamera(page == Page.View);
            if (page == Page.Count || page == Page.View)
            {
                var singleCenter = Vector3.zero;
                a = generator.Generate(level); left.transform.position = singleCenter; left.Show(a); choices = generator.CountChoices(a, level);
                if (page == Page.View) BuildViewChoices();
                CreateModeGround(singleCenter, 8f);
            }
            else
            {
                left.transform.position = Vector3.left * 2.25f; right.transform.position = Vector3.right * 2.25f;
                a = generator.Generate(level); b = generator.Generate(level); left.Show(a); right.Show(b);
                CreateModeGround(Vector3.zero, 8f);
            }
        }

        private void Count()
        {
            Header("ブロックは いくつ？"); Progress(10);
            RotationButtons();
            GUI.enabled = !waitingForNext;
            for (var i=0;i<choices.Count;i++) if (ChoiceButton(new Rect(115+i*195,680,155,82), choices[i].ToString(), new[]{pink,yellow,mint,sky,lavender}[i])) Answer(choices[i] == a.TotalCount, 10);
            GUI.enabled = true; Feedback(10);
        }

        private void Compare()
        {
            Header("どっちが おおい？"); Progress(8); RotationButtons();
            GUI.enabled = !waitingForNext;
            if (ChoiceButton(new Rect(210,670,220,82),"ひだり",pink)) Answer(a.TotalCount>b.TotalCount,8);
            if (ChoiceButton(new Rect(487,670,220,82),"おなじ",yellow)) Answer(a.TotalCount==b.TotalCount,8);
            if (ChoiceButton(new Rect(764,670,220,82),"みぎ",sky)) Answer(a.TotalCount<b.TotalCount,8); GUI.enabled = true; Feedback(8);
        }

        private void RotationButtons()
        {
            if (GUI.Button(new Rect(35,500,155,68), "↶ 90°", ColorButton(sky))) RotateProblems(-90f);
            if (GUI.Button(new Rect(1004,500,155,68), "90° ↷", ColorButton(sky))) RotateProblems(90f);
        }

        private void RotateProblems(float degrees)
        {
            if (waitingForNext) return;
            problemRotation = Mathf.Repeat(problemRotation + degrees, 360f);
            left.transform.rotation = Quaternion.Euler(0f, problemRotation, 0f);
            right.transform.rotation = Quaternion.Euler(0f, problemRotation, 0f);
            if (page == Page.Free)
            {
                var step = Quaternion.Euler(0f, degrees, 0f);
                foreach (var tile in freeGrid) if (tile) { tile.transform.position = step * tile.transform.position; tile.transform.rotation = step * tile.transform.rotation; }
                UpdateSelectionMarker();
            }
            if (page == Page.View)
            {
                var step = Quaternion.Euler(0f,degrees,0f);
                foreach(var marker in viewReferenceTiles) if(marker)
                {
                    marker.transform.position=step*marker.transform.position;
                    marker.transform.rotation=step*marker.transform.rotation;
                }
            }
        }

        private void View()
        {
            Header("うえから みると どれ？"); Progress(8); RotationButtons();
            GUI.enabled = !waitingForNext;
            for (var i=0;i<viewChoices.Count;i++)
            {
                var rect = new Rect(105+i*265,610,220,135);
                if (ChoiceButton(rect,"",new[]{pink,yellow,mint,sky}[i])) { selectedViewIndex=i; Answer(i==viewAnswerIndex,8); }
                DrawGridWithReference(viewChoices[i], new Rect(rect.x+28,rect.y+8,164,118));
            }
            GUI.enabled = true;
            if (waitingForNext && selectedViewIndex >= 0) ViewFeedback(8);
        }

        private void Free()
        {
            Header("じゆうに つんでみよう");
            GUI.Label(new Rect(420,98,355,48),$"ブロック: {a.TotalCount}こ",label);
            RotationButtons();
            GUI.Label(new Rect(280,575,640,75),$"えらんだ ばしょ\nよこ {selectedX+1}・おく {selectedY+1}",label);
            if(GUI.Button(new Rect(210,675,220,85),"＋ ひとつ\nつむ",ColorButton(mint))) ChangeFree(1);
            if(GUI.Button(new Rect(487,675,220,85),"− ひとつ\nへらす",ColorButton(pink))) ChangeFree(-1);
            var visibilityLabel = freeBlocksHidden ? "ぜんぶ\nみせる" : "ブロックを\nかくす";
            if(GUI.Button(new Rect(764,675,220,85),visibilityLabel,ColorButton(sky)))
            {
                freeBlocksHidden = !freeBlocksHidden;
                left.Show(a,freeBlocksHidden);
            }
        }

        private void ChangeFree(int delta)
        {
            var next = Mathf.Clamp(a[selectedX, selectedY] + delta, 0, 6);
            if (next == a[selectedX, selectedY]) { message = delta > 0 ? "これいじょう つめないよ" : "ここには ないよ"; return; }
            a[selectedX, selectedY] = next; left.Show(a,freeBlocksHidden); UpdateSelectionMarker();
        }

        private void CreateFreeGrid()
        {
            ClearFreeGrid();
            var material = TransparentMaterial(new Color(.36f,.19f,.07f,.55f));
            for (var gx = 0; gx < 8; gx++) for (var gy = 0; gy < 8; gy++)
            {
                var tile = GameObject.CreatePrimitive(PrimitiveType.Cube); tile.name = $"ゆか ({gx},{gy})";
                tile.transform.position = new Vector3(gx - 3.5f, -.08f, gy - 3.5f); tile.transform.localScale = new Vector3(.96f,.10f,.96f);
                tile.GetComponent<Renderer>().sharedMaterial = material;
                if (gx >= 1 && gx <= 6 && gy >= 1 && gy <= 6) tile.AddComponent<BlockCell>().Set(gx - 1, gy - 1);
                freeGrid.Add(tile);
            }
            selectionMarker = null;
        }

        private void CreateModeGround(Vector3 center, float size)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "はんとうめいの つち";
            floor.transform.position = center + Vector3.down * .11f; floor.transform.localScale = new Vector3(size,.12f,size);
            floor.GetComponent<Renderer>().sharedMaterial = TransparentMaterial(new Color(.46f,.28f,.13f,.42f));
            modeGround.Add(floor);
            AddGroundGrid(center, size);
            if (page == Page.View) AddViewReferenceStrip(center, size);
        }

        private void AddViewReferenceStrip(Vector3 center, float size)
        {
            var material=OpaqueMaterial(new Color(.95f,.42f,.06f,1f));
            var cell=size/8f; var frontZ=center.z-size*.5f+cell*.5f;
            for(var i=-2;i<=2;i++)
            {
                var marker=GameObject.CreatePrimitive(PrimitiveType.Cube);marker.name="てまえの きじゅん";
                marker.transform.position=new Vector3(center.x+i*cell,.055f,frontZ);
                marker.transform.localScale=new Vector3(cell*.92f,.16f,cell*.92f);
                marker.GetComponent<Renderer>().sharedMaterial=material;marker.GetComponent<Collider>().enabled=false;
                modeGround.Add(marker);viewReferenceTiles.Add(marker);
            }
        }

        private void AddGroundGrid(Vector3 center, float size)
        {
            var lineMaterial = new Material(Shader.Find("Sprites/Default")) { color = new Color(.16f,.09f,.04f,.72f) };
            var half = size * .5f;
            for (var i = 0; i <= 8; i++)
            {
                var offset = -half + size * i / 8f;
                AddGroundLine(new Vector3(center.x + offset,-.035f,center.z-half), new Vector3(center.x + offset,-.035f,center.z+half), lineMaterial);
                AddGroundLine(new Vector3(center.x-half,-.035f,center.z+offset), new Vector3(center.x+half,-.035f,center.z+offset), lineMaterial);
            }
        }

        private void AddGroundLine(Vector3 from, Vector3 to, Material material)
        {
            var line = new GameObject("ゆかの グリッド").AddComponent<LineRenderer>();
            line.positionCount = 2; line.SetPositions(new[]{from,to}); line.startWidth = line.endWidth = .028f;
            line.sharedMaterial = material; line.startColor = line.endColor = material.color; modeGround.Add(line.gameObject);
        }

        private static Material TransparentMaterial(Color color)
        {
            var shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color, renderQueue = 3000 };
            return material;
        }

        private static Material OpaqueMaterial(Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (!shader) shader = Shader.Find("Unlit/Color");
            if (!shader) shader = Shader.Find("Sprites/Default");
            var material = new Material(shader) { color = color };
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            return material;
        }

        private void UpdateSelectionMarker()
        {
            foreach (var tile in freeGrid)
            {
                if (!tile) continue;
                var cell = tile.GetComponent<BlockCell>();
                var selected = cell && cell.X == selectedX && cell.Y == selectedY;
                tile.GetComponent<Renderer>().material.color = selected
                    ? new Color(1f,.68f,.08f,.78f)
                    : new Color(.36f,.19f,.07f,.55f);
            }
        }

        private void ClearFreeGrid()
        {
            // Disable immediately so no selected tile can remain visible for a
            // frame (or receive another touch) after the Back button is pressed.
            foreach (var tile in freeGrid) if (tile) { tile.SetActive(false); Destroy(tile); }
            freeGrid.Clear();
            if (selectionMarker) { selectionMarker.SetActive(false); Destroy(selectionMarker); }
            selectionMarker = null;
        }

        private void ClearModeGround()
        {
            foreach (var floor in modeGround) if (floor) { floor.SetActive(false); Destroy(floor); }
            modeGround.Clear();viewReferenceTiles.Clear();
        }

        private void Answer(bool ok, int total)
        {
            if (waitingForNext) return;
            results[question - 1] = ok;
            message = ok ? "○ せいかい！\nすごい！" : page == Page.Count ? $"× おしいね\nこたえは {a.TotalCount}こ" : "× おしいね\nもういちど みてみよう";
            if(ok) correct++; waitingForNext = true;
            if (ok && PlayerPrefs.GetInt("sfx", 1) == 1 && sfxSource && correctSound)
                sfxSource.PlayOneShot(correctSound, .8f);
            PlayerPrefs.SetInt("solvedCount",PlayerPrefs.GetInt("solvedCount")+1);
            if (ok) PlayerPrefs.SetInt("correctCount",PlayerPrefs.GetInt("correctCount")+1);
            if (ok && page == Page.Count)
            {
                pendingTotal = total;
                Invoke(nameof(AdvanceAfterCorrect), 1.0f);
            }
        }

        private static AudioClip CreateCorrectSound()
        {
            const int sampleRate = 44100;
            const float duration = .46f;
            var samples = new float[Mathf.CeilToInt(sampleRate * duration)];
            for (var i = 0; i < samples.Length; i++)
            {
                var time = i / (float)sampleRate;
                var secondTone = time >= .23f;
                var toneTime = secondTone ? time - .23f : time;
                var frequency = secondTone ? 784f : 659.25f;
                var envelope = Mathf.Clamp01(toneTime / .012f) * Mathf.Clamp01((.21f - toneTime) / .07f);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * toneTime) * envelope * .32f;
            }
            var clip = AudioClip.Create("せいかい ピンポン", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private void BuildViewChoices()
        {
            viewChoices.Clear(); selectedViewIndex = -1;
            var correct = a.TopView(); var keys = new HashSet<string>();
            AddViewChoice(correct, keys); AddViewChoice(Mirror(correct), keys); AddViewChoice(RotateGrid(correct), keys);
            for (var x=0;x<correct.GetLength(0) && viewChoices.Count<4;x++) for (var y=0;y<correct.GetLength(1) && viewChoices.Count<4;y++)
            {
                var changed=(int[,])correct.Clone(); changed[x,y]=changed[x,y]==0?1:0; AddViewChoice(changed,keys);
            }
            while(viewChoices.Count<4)
            {
                var changed=(int[,])correct.Clone(); changed[0,0]=viewChoices.Count%2; AddViewChoice(changed,keys);
                if(viewChoices.Count<4){ var larger=new int[correct.GetLength(0)+viewChoices.Count,correct.GetLength(1)]; AddViewChoice(larger,keys); }
            }
            for(var i=0;i<viewChoices.Count;i++){var j=UnityEngine.Random.Range(i,viewChoices.Count);(viewChoices[i],viewChoices[j])=(viewChoices[j],viewChoices[i]);}
            viewAnswerIndex=viewChoices.FindIndex(grid=>GridKey(grid)==GridKey(correct));
        }

        private void AddViewChoice(int[,] grid, HashSet<string> keys) { if(keys.Add(GridKey(grid))) viewChoices.Add(grid); }
        private static string GridKey(int[,] grid)
        {
            var key=$"{grid.GetLength(0)}x{grid.GetLength(1)}:";
            foreach(var value in grid) key+=value; return key;
        }
        private static int[,] Mirror(int[,] source)
        {
            var w=source.GetLength(0);var h=source.GetLength(1);var result=new int[w,h];
            for(var x=0;x<w;x++)for(var y=0;y<h;y++)result[w-1-x,y]=source[x,y];return result;
        }
        private static int[,] RotateGrid(int[,] source)
        {
            var w=source.GetLength(0);var h=source.GetLength(1);var result=new int[h,w];
            for(var x=0;x<w;x++)for(var y=0;y<h;y++)result[h-1-y,x]=source[x,y];return result;
        }

        private void DrawGrid(int[,] grid, Rect rect)
        {
            var w=grid.GetLength(0);var h=grid.GetLength(1);var cell=Mathf.Min(rect.width/w,rect.height/h);
            var ox=rect.x+(rect.width-cell*w)*.5f;var oy=rect.y+(rect.height-cell*h)*.5f;
            for(var x=0;x<w;x++)for(var y=0;y<h;y++)
            {
                var r=new Rect(ox+x*cell,oy+(h-1-y)*cell,cell-2,cell-2);
                GUI.DrawTexture(r,grid[x,y]==1?TsumikiPaletteTexture():cream);
            }
        }

        private void DrawGridWithReference(int[,] grid, Rect rect)
        {
            const float strip=18f;const float gap=4f;
            var inner=new Rect(rect.x+strip+gap,rect.y+strip+gap,rect.width-(strip+gap)*2,rect.height-(strip+gap)*2);
            DrawGrid(grid,inner);
            // Answer cards keep their reference edge fixed at the bottom. Rotating
            // the 3D question must not rotate the answer's baseline or its meaning.
            var cell=inner.width/5f;var y=inner.yMax+gap;
            for(var i=0;i<5;i++)GUI.DrawTexture(new Rect(inner.x+i*cell+1,y,cell-2,strip),referenceOrange);
        }

        private Texture2D TsumikiPaletteTexture()
        {
            return sky;
        }

        private void ViewFeedback(int total)
        {
            GUI.DrawTexture(new Rect(310,380,574,225),results[question-1]==true?green:red);
            GUI.Label(new Rect(325,380,250,65),"えらんだ\nかたち",label); GUI.Label(new Rect(620,380,240,65),"せいかい",label);
            DrawGridWithReference(viewChoices[selectedViewIndex],new Rect(350,430,180,150)); DrawGridWithReference(viewChoices[viewAnswerIndex],new Rect(650,430,180,150));
            if(GUI.Button(new Rect(915,565,190,65),question>=total?"おしまい":"つぎへ ▶",ColorButton(yellow))) Advance(total);
        }

        private void AdvanceAfterCorrect()
        {
            if (page == Page.Count && waitingForNext) Advance(pendingTotal);
        }

        private void Advance(int total)
        {
            if (question >= total) { Clear(); page = Page.Home; return; }
            question++; Next();
        }

        private void Progress(int total)
        {
            for (var i = 0; i < total; i++)
            {
                var value = results[i]; var text = value == true ? "○" : value == false ? "×" : "・";
                progressStyle.normal.textColor = value == true ? new Color(.12f,.62f,.25f) : value == false ? new Color(.9f,.2f,.22f) : new Color(.55f,.48f,.44f);
                GUI.Label(new Rect(315 + i * 58, 92, 50, 48), text, progressStyle);
            }
            GUI.Label(new Rect(930, 92, 130, 48), $"{question}/{total}", label);
        }

        private void Settings()
        {
            Header("せってい"); Toggle(150,"こえ","voice",true); Toggle(250,"おと","sfx",true); Toggle(350,"おんがく","bgm",true); Toggle(450,"くろ","kuro",true);
            if (ChoiceButton(new Rect(350,550,500,75),"くろの きせかえ",pink)){outfitPage=0;page=Page.DressUp;}
        }

        private Texture2D CurrentKuro()
        {
            if (kuroOutfits == null || kuroOutfits.Length == 0) return kuro;
            var selected=Mathf.Clamp(PlayerPrefs.GetInt("kuroOutfit",0),0,kuroOutfits.Length-1);
            if(PlayerPrefs.GetInt("correctCount",0)<outfitUnlocks[selected]) selected=0;
            return kuroOutfits[selected] ? kuroOutfits[selected] : kuro;
        }

        private void DressUp()
        {
            Header("くろの きせかえ");
            var correctAnswers=PlayerPrefs.GetInt("correctCount",0);
            GUI.Label(new Rect(45,115,390,55),$"せいかい　{correctAnswers}もん",label);
            var preview=CurrentKuro();
            if(preview) GUI.DrawTexture(new Rect(55,180,365,365),preview,ScaleMode.ScaleToFit,true);
            GUI.Label(new Rect(55,555,365,80),"30もん せいかいするたびに\n900もんまで グッズが ふえるよ",new GUIStyle(label){fontSize=27,alignment=TextAnchor.MiddleCenter});
            var selected=Mathf.Clamp(PlayerPrefs.GetInt("kuroOutfit",0),0,outfitNames.Length-1);
            const int outfitsPerPage=6;
            var pageCount=Mathf.CeilToInt(outfitNames.Length/(float)outfitsPerPage);
            outfitPage=Mathf.Clamp(outfitPage,0,pageCount-1);
            var start=outfitPage*outfitsPerPage;
            var end=Mathf.Min(start+outfitsPerPage,outfitNames.Length);
            for(var i=start;i<end;i++)
            {
                var row=i-start;
                var unlocked=correctAnswers>=outfitUnlocks[i];
                var remaining=Mathf.Max(0,outfitUnlocks[i]-correctAnswers);
                var text=unlocked?$"{(selected==i?"✓ ":"")}{outfitNames[i]}":$"まだ ひみつ　あと {remaining}もん";
                GUI.enabled=unlocked;
                if(ChoiceButton(new Rect(485,105+row*88,610,68),text,new[]{cream,yellow,sky,lavender,mint,pink}[row%6]))
                {
                    PlayerPrefs.SetInt("kuroOutfit",i);PlayerPrefs.Save();
                }
                GUI.enabled=true;
            }
            if(outfitPage>0&&GUI.Button(new Rect(500,650,180,65),"◀ まえ",ColorButton(sky)))outfitPage--;
            GUI.Label(new Rect(700,650,180,65),$"{outfitPage+1}/{pageCount}",progressStyle);
            if(outfitPage<pageCount-1&&GUI.Button(new Rect(900,650,180,65),"つぎ ▶",ColorButton(sky)))outfitPage++;
        }

        private static void SetViewCamera(bool raiseProblem)
        {
            var camera = Camera.main;
            if (!camera) return;
            var shift = raiseProblem ? -1.25f : 0f;
            camera.transform.position = new Vector3(8,7+shift,-8);
            camera.transform.LookAt(new Vector3(0,1+shift,0));
        }

        private void Toggle(float y,string text,string key,bool initial)
        {
            var on=PlayerPrefs.GetInt(key,initial?1:0)==1; if(GUI.Button(new Rect(350,y,500,75),$"{text}　{(on?"オン":"オフ")}",button)) PlayerPrefs.SetInt(key,on?0:1);
        }

        private void Parent()
        {
            Header("おうちの かたへ"); GUI.Label(new Rect(280,150,650,100),$"これまでに\n{PlayerPrefs.GetInt("solvedCount")}もん ときました",label);
            GUI.Label(new Rect(280,250,650,80),$"せいかいは {PlayerPrefs.GetInt("correctCount")}もんです。",label);
            GUI.Label(new Rect(280,330,650,100),"初級と「じゆうにつんでみよう」は無料です。\n中級・上級は一度の購入でずっと遊べます。",label);
            if(ChoiceButton(new Rect(345,465,505,80),"レベルの こうにゅう・ふくげん",yellow)) page=Page.Store;
        }

        private void OpenParentGate(Page destination)
        {
            pageAfterParentGate=destination;
            parentGateAnswer=25;
            page=Page.ParentGate;
        }

        private void ParentGate()
        {
            Header("おうちの かたへ");
            GUI.Label(new Rect(250,145,700,115),"ここから先は保護者の方が操作してください。\n17 ＋ 8 の答えを選んでください。",label);
            var answers=new[]{24,25,26};
            for(var i=0;i<answers.Length;i++)
            {
                if(!ChoiceButton(new Rect(285+i*220,315,185,90),answers[i].ToString(),new[]{sky,yellow,pink}[i])) continue;
                if(answers[i]==parentGateAnswer) page=pageAfterParentGate;
                else message="答えが違います。保護者の方と確認してください。";
            }
            GUI.Label(new Rect(245,450,710,80),message,label);
        }

        private void Store()
        {
            Header("レベルを ひらく");
            var purchases=AppPurchaseManager.Instance;
            var intermediate=AppPurchaseManager.IntermediateUnlocked;
            var advanced=AppPurchaseManager.AdvancedUnlocked;
            GUI.Label(new Rect(220,105,760,65),"買い切りです。毎月の支払いはありません。",label);
            var y=190f;
            if(!intermediate && !advanced)
            {
                if(ChoiceButton(new Rect(260,y,675,78),$"中級をひらく　{purchases.Price(AppPurchaseManager.IntermediateProductId,"¥300")}",mint)) purchases.Buy(AppPurchaseManager.IntermediateProductId); y+=95;
                if(ChoiceButton(new Rect(260,y,675,78),$"上級をひらく　{purchases.Price(AppPurchaseManager.AdvancedProductId,"¥300")}",sky)) purchases.Buy(AppPurchaseManager.AdvancedProductId); y+=95;
                if(ChoiceButton(new Rect(260,y,675,90),$"中級・上級をまとめてひらく　{purchases.Price(AppPurchaseManager.AllLevelsProductId,"¥500")}",yellow)) purchases.Buy(AppPurchaseManager.AllLevelsProductId); y+=110;
            }
            else
            {
                GUI.Label(new Rect(260,y,675,70),$"中級　{(intermediate?"購入済み":"未購入")}",label); y+=80;
                GUI.Label(new Rect(260,y,675,70),$"上級　{(advanced?"購入済み":"未購入")}",label); y+=80;
                if(!intermediate && ChoiceButton(new Rect(260,y,675,78),$"中級をひらく　{purchases.Price(AppPurchaseManager.IntermediateProductId,"¥300")}",mint)) purchases.Buy(AppPurchaseManager.IntermediateProductId);
                if(!advanced && ChoiceButton(new Rect(260,y,675,78),$"上級をひらく　{purchases.Price(AppPurchaseManager.AdvancedProductId,"¥300")}",sky)) purchases.Buy(AppPurchaseManager.AdvancedProductId);
                y+=95;
            }
            if(ChoiceButton(new Rect(385,570,425,70),"以前の購入を復元",lavender)) purchases.RestorePurchases();
            GUI.Label(new Rect(170,650,855,75),purchases.Status,new GUIStyle(label){fontSize=25,alignment=TextAnchor.MiddleCenter});
        }

        private void Header(string text)
        {
            if(GUI.Button(new Rect(35,30,150,65),"もどる",button)){CancelInvoke(nameof(AdvanceAfterCorrect));ClearFreeGrid();page=Page.Home;Clear();} GUI.Label(new Rect(210,25,900,75),text,title);
        }
        private void Feedback(int total)
        {
            if(message.Length==0) return;
            GUI.DrawTexture(new Rect(300,535,594,115), results[question-1] == true ? green : red);
            feedbackStyle.normal.textColor = TsumikiPalette.Outline; GUI.Label(new Rect(310,540,574,105),message,feedbackStyle);
            var autoAdvancing = page == Page.Count && results[question - 1] == true;
            if(waitingForNext && !autoAdvancing && GUI.Button(new Rect(915,555,190,82), question >= total ? "おしまい" : "つぎへ ▶", ColorButton(yellow))) Advance(total);
        }
        private void Clear(){if(left)left.Clear();if(right)right.Clear();ClearModeGround();}
    }
}
