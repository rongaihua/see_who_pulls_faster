//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 球堆玩法的主界面，全部用 uGUI 代码搭
//---------------------------------------------------------------------------------------

using System.Collections;
using System.Collections.Generic;
using SeeWho.Common.Balls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SeeWho.Client
{
    /// <summary>
    /// 游戏主界面：上面是堆满彩球的坑，下面是四列等着发车的巴士。
    /// 点每列最前面那辆车，它会把同色的球吸走，上面的球往下落。
    /// 美术取自小游戏缓存：背景、小人精灵、配色都按原作来。
    /// </summary>
    public sealed class GameView : MonoBehaviour
    {
        /// <summary>
        /// 小人颜色表，取的是缓存精灵里的鲜艳色
        /// </summary>
        private static readonly Color[] Palette =
        {
            new Color(0.967f, 0.393f, 0.019f),
            new Color(0.960f, 0.764f, 0.013f),
            new Color(0.398f, 0.791f, 0.197f),
            new Color(0.042f, 0.720f, 0.872f),
            new Color(0.011f, 0.427f, 0.947f),
            new Color(0.925f, 0.269f, 0.855f),
            new Color(0.899f, 0.208f, 0.246f),
            new Color(0.624f, 0.615f, 0.877f),
        };

        /// <summary>
        /// 颜色编号到精灵编号的映射，挑的都是鲜艳的那几个
        /// </summary>
        private static readonly int[] SpriteMap = { 10, 17, 6, 4, 1, 11, 14, 9 };

        private static readonly Color TextColor = new Color(0.96f, 0.97f, 0.99f);
        private static readonly Color WeakText = new Color(0.96f, 0.97f, 0.99f, 0.7f);
        private static readonly Color SandColor = new Color(0.96f, 0.84f, 0.62f);
        private static readonly Color GrassColor = new Color(0.42f, 0.74f, 0.25f);
        private static readonly Color WaterColor = new Color(0.62f, 0.83f, 0.92f, 0.5f);
        private static readonly Color ButtonColor = new Color(0.24f, 0.70f, 0.45f);
        private static readonly Color ButtonGray = new Color(0.25f, 0.28f, 0.34f, 0.9f);

        private Font mFont;
        private Sprite[] mPeople = new Sprite[0];
        private Sprite mCircleSprite;
        private Sprite mRoundSprite;
        private Sprite mSmallRoundSprite;
        private Sprite mBgSprite;
        private Sprite mHomeBgSprite;
        private Sprite mHomeBusSprite;

        private BallLevel mLevel;
        private BallGame mGame;
        private int mNumber = 1;

        private RectTransform mBoardRoot;
        private RectTransform mBusRoot;
        private Text mTitleText;
        private Text mRemainText;
        private Text mToastText;

        private GameObject mOverlay;
        private Text mOverlayTitle;
        private Text mOverlayBody;
        private Button mOverlayButton;
        private Text mOverlayButtonLabel;

        private GameObject mHome;
        private Text mHomeLevelText;
        private Text mHomeButtonLabel;
        private RectTransform mRoot;
        private RectTransform mRoadRoot;
        private Text mRoadBadgeText;
        private int mStartLevel = 1;

        private float mToastTimer;
        private float mCellSize = 60f;
        private Vector2 mBoardOrigin;
        private GameObject[,] mBallObjects = new GameObject[0, 0];
        private List<GameObject>[] mBusCards = new List<GameObject>[0];
        private readonly List<RectTransform> mColumnHits = new List<RectTransform>();
        private bool mAnimating;
        private float mAnimatingSince;

        /// <summary>
        /// 加载资源并搭界面
        /// </summary>
        private void Awake()
        {
            LoadAssets();
            BuildUi();
        }

        /// <summary>
        /// 开一关
        /// </summary>
        /// <param name="number">第几关</param>
        public void StartGame(int number)
        {
            mNumber = number;
            mLevel = CreateLevel(number);
            mGame = BallGame.Create(mLevel);

            if (mHome != null)
            {
                mHome.SetActive(false);
            }

            Refresh();
        }

        /// <summary>
        /// 回到开始界面：背景 + 游戏名 + 开始按钮
        /// </summary>
        public void ShowHome()
        {
            mStartLevel = Mathf.Max(1, PlayerPrefs.GetInt("seewho.level", 1));

            if (mHome == null)
            {
                BuildHome(mRoot);
            }

            mGame = null;
            mLevel = null;

            UiKit.Clear(mBoardRoot);
            UiKit.Clear(mBusRoot);
            mColumnHits.Clear();

            mTitleText.text = string.Empty;
            mTitleText.fontSize = 52;
            mRemainText.text = string.Empty;

            mHomeButtonLabel.text = "第 " + mStartLevel + " 关";
            mHomeLevelText.text = mStartLevel > 1 ? "继续上次的进度" : "点一下开始游戏";
            mHome.SetActive(true);
            mOverlay.SetActive(false);
        }

        /// <summary>
        /// 预览工具用：按“吸得最多”的贪心走几步
        /// </summary>
        /// <param name="steps">步数</param>
        public void DebugAutoPlay(int steps)
        {
            for (var i = 0; i < steps && mGame != null && !mGame.IsWin; i++)
            {
                var best = BestColumn();
                if (best < 0)
                {
                    return;
                }

                mGame.Dispatch(best);
            }

            Refresh();
        }

        /// <summary>
        /// 每帧处理提示淡出和点车。点车用屏幕矩形判定，不走 Button，避免射线被挡住时点不动
        /// </summary>
        private void Update()
        {
            if (mToastTimer > 0f)
            {
                mToastTimer -= Time.deltaTime;
                var color = mToastText.color;
                color.a = Mathf.Clamp01(mToastTimer / 0.5f);
                mToastText.color = color;
            }

            if (mGame == null || mGame.IsWin)
            {
                return;
            }

            // 动画万一被打断，别把输入一直锁着
            if (mAnimating && Time.time - mAnimatingSince > 2f)
            {
                mAnimating = false;
                Refresh();
            }

            if (mAnimating)
            {
                return;
            }

            if (mOverlay != null && mOverlay.activeSelf)
            {
                return;
            }

            if (!Input.GetMouseButtonDown(0))
            {
                return;
            }

            var pointer = (Vector2)Input.mousePosition;
            for (var column = 0; column < mColumnHits.Count; column++)
            {
                if (mColumnHits[column] != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(mColumnHits[column], pointer, null))
                {
                    Debug.Log("发车：第 " + (column + 1) + " 列");
                    Dispatch(column, false);
                    return;
                }
            }
        }

        /// <summary>
        /// 读字体和图片
        /// </summary>
        private void LoadAssets()
        {
            mFont = Resources.Load<Font>("SeeWho/Fonts/AlimamaFangYuanTiVF_Thin");
            if (mFont == null)
            {
                mFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            }

            mCircleSprite = LoadSprite("SeeWho/UI/circle");
            mRoundSprite = UiKit.CreateRoundedSprite(96, 24);
            mSmallRoundSprite = UiKit.CreateRoundedSprite(32, 9);
            mBgSprite = LoadSprite("SeeWho/Art/bg_level");
            mHomeBgSprite = LoadSprite("SeeWho/Art/bg_home");
            mHomeBusSprite = LoadSprite("SeeWho/Art/bus_home");

            mPeople = new Sprite[18];
            for (var i = 0; i < mPeople.Length; i++)
            {
                mPeople[i] = LoadSprite("SeeWho/Art/People/person_" + i.ToString("00"));
            }
        }

        /// <summary>
        /// 读一张图
        /// </summary>
        private static Sprite LoadSprite(string path)
        {
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }

            var texture = Resources.Load<Texture2D>(path);
            if (texture == null)
            {
                return null;
            }

            return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f), 100f);
        }

        /// <summary>
        /// 搭界面骨架
        /// </summary>
        private void BuildUi()
        {
            EnsureEventSystem();

            var canvasGo = new GameObject("GameCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvasGo.transform.SetParent(transform, false);

            var canvas = canvasGo.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            var root = (RectTransform)canvasGo.transform;
            mRoot = root;

            var background = UiKit.CreateImage(root, "Background", mBgSprite, Color.white);
            UiKit.Stretch(background.rectTransform);
            if (mBgSprite == null)
            {
                background.color = new Color(0.55f, 0.78f, 0.94f);
            }

            // 标题压在背景的蓝色木牌上
            mTitleText = UiKit.CreateText(root, "Title", "第 1 关", 52, TextColor, TextAnchor.MiddleCenter, mFont);
            UiKit.Place(mTitleText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -105f), new Vector2(520f, 90f));

            mRemainText = UiKit.CreateText(root, "Remain", "剩余 0", 34, new Color(0.28f, 0.22f, 0.12f, 0.85f),
                TextAnchor.MiddleCenter, mFont);
            UiKit.Place(mRemainText.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -200f), new Vector2(600f, 60f));

            mBoardRoot = UiKit.CreateNode(root, "Board");
            UiKit.Place(mBoardRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -260f), new Vector2(1040f, 780f));

            BuildRoad(root);

            mRoadRoot = UiKit.CreateNode(root, "RoadRoot");
            UiKit.Stretch(mRoadRoot);

            // 上路车辆数：N/6
            var badge = UiKit.CreateImage(root, "RoadBadge", mRoundSprite, new Color(1f, 0.86f, 0.48f, 1f));
            UiKit.Place(badge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-30f, 770f), new Vector2(260f, 62f));

            mRoadBadgeText = UiKit.CreateText(badge.transform, "Label", "0/6", 34,
                new Color(0.35f, 0.22f, 0.10f, 1f), TextAnchor.MiddleCenter, mFont);
            UiKit.Stretch(mRoadBadgeText.rectTransform);

            var plus = UiKit.CreateButton(root, "Plus", "+", 40, new Color(0.36f, 0.76f, 0.20f, 1f),
                mRoundSprite, TextColor, mFont, OnPlusClicked);
            UiKit.Place(plus.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(140f, 770f), new Vector2(64f, 64f));

            mBusRoot = UiKit.CreateNode(root, "Buses");
            UiKit.Place(mBusRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 250f), new Vector2(1060f, 420f));

            var hintButton = UiKit.CreateButton(root, "Hint", "提示", 40, new Color(0.28f, 0.66f, 0.36f, 1f),
                mRoundSprite, TextColor, mFont, OnHintClicked);
            UiKit.Place(hintButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-160f, 95f), new Vector2(240f, 96f));

            var restartButton = UiKit.CreateButton(root, "Restart", "重开", 40, new Color(0.55f, 0.40f, 0.22f, 1f),
                mRoundSprite, TextColor, mFont, OnRestartClicked);
            UiKit.Place(restartButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(160f, 95f), new Vector2(240f, 96f));

            mToastText = UiKit.CreateText(root, "Toast", string.Empty, 38, new Color(1f, 0.95f, 0.6f, 0f),
                TextAnchor.MiddleCenter, mFont);
            UiKit.Place(mToastText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 60f), new Vector2(900f, 80f));

            BuildOverlay(root);
            BuildHome(root);
        }

        /// <summary>
        /// 底部横向车道，车从车位开上来先走这条
        /// </summary>
        private void BuildRoad(RectTransform root)
        {
            var roadFill = new Color(0.74f, 0.90f, 0.98f, 1f);
            var roadEdge = new Color(0.40f, 0.72f, 0.88f, 1f);

            var laneEdge = UiKit.CreateImage(root, "LaneEdge", mRoundSprite, roadEdge);
            UiKit.Place(laneEdge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 670f), new Vector2(1060f, 90f));

            var lane = UiKit.CreateImage(laneEdge.transform, "LaneFill", mRoundSprite, roadFill);
            UiKit.Place(lane.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1042f, 78f));
        }

        /// <summary>
        /// 竖着的跑道，一直穿进球坑中间。宽度按球坑挖掉的车道列数算。
        /// </summary>
        private void RefreshRoad()
        {
            UiKit.Clear(mRoadRoot);

            var roadFill = new Color(0.74f, 0.90f, 0.98f, 1f);
            var roadEdge = new Color(0.40f, 0.72f, 0.88f, 1f);

            var lanes = Mathf.Max(2, mLevel.Width / 8);
            var roadWidth = mCellSize * lanes + 18f;
            const float roadHeight = 880f;

            var edge = UiKit.CreateImage(mRoadRoot, "RoadEdge", mRoundSprite, roadEdge);
            UiKit.Place(edge.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 760f), new Vector2(roadWidth, roadHeight));

            var fill = UiKit.CreateImage(edge.transform, "RoadFill", mRoundSprite, roadFill);
            UiKit.Place(fill.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(roadWidth - 18f, roadHeight - 18f));

            for (var i = 0; i < 5; i++)
            {
                var dash = UiKit.CreateImage(fill.transform, "Dash" + i, mSmallRoundSprite,
                    new Color(1f, 1f, 1f, 0.9f));
                UiKit.Place(dash.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(0f, -300f + i * 130f), new Vector2(22f, 46f));
            }
        }

        /// <summary>
        /// 开始界面：照原作主页 —— 公交站背景 + 大巴士 + 底部绿色关卡按钮
        /// </summary>
        private void BuildHome(RectTransform root)
        {
            var home = UiKit.CreateNode(root, "Home");
            UiKit.Stretch(home);
            mHome = home.gameObject;

            var bg = UiKit.CreateImage(home, "Bg", mHomeBgSprite, Color.white);
            UiKit.Stretch(bg.rectTransform);
            if (mHomeBgSprite == null)
            {
                bg.color = new Color(0.55f, 0.82f, 0.96f);
            }

            var bus = UiKit.CreateImage(home, "Bus", mHomeBusSprite, Color.white);
            UiKit.Place(bus.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 150f), new Vector2(700f, 550f));
            bus.preserveAspect = true;

            var ring = UiKit.CreateImage(home, "StartRing", mRoundSprite, new Color(1f, 1f, 1f, 0.96f));
            UiKit.Place(ring.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -500f), new Vector2(660f, 178f));

            var start = UiKit.CreateButton(home, "Start", "第 1 关", 62, new Color(0.36f, 0.76f, 0.20f, 1f),
                mRoundSprite, TextColor, mFont, OnStartClicked);
            UiKit.Place(start.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -500f), new Vector2(640f, 158f));
            mHomeButtonLabel = start.GetComponentInChildren<Text>();

            mHomeLevelText = UiKit.CreateText(home, "Tip", "点一下开始游戏", 30,
                new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleCenter, mFont);
            UiKit.Place(mHomeLevelText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -640f), new Vector2(800f, 60f));
        }

        /// <summary>
        /// 开始 / 继续
        /// </summary>
        private void OnStartClicked()
        {
            if (mHome != null)
            {
                mHome.SetActive(false);
            }

            StartGame(mStartLevel);
        }

        /// <summary>
        /// 通关 / 卡住的浮层
        /// </summary>
        private void BuildOverlay(RectTransform root)
        {
            var overlay = UiKit.CreateNode(root, "Overlay");
            UiKit.Stretch(overlay);
            mOverlay = overlay.gameObject;

            var dim = UiKit.CreateImage(overlay, "Dim", null, new Color(0f, 0f, 0f, 0.55f));
            UiKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var panel = UiKit.CreateImage(overlay, "Panel", mRoundSprite, SandColor);
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(780f, 600f));

            var frame = UiKit.CreateImage(panel.transform, "Frame", mRoundSprite, GrassColor);
            UiKit.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(796f, 616f));
            frame.transform.SetAsFirstSibling();

            mOverlayTitle = UiKit.CreateText(panel.transform, "Title", "通关", 64,
                new Color(0.35f, 0.22f, 0.10f, 1f), TextAnchor.MiddleCenter, mFont);
            UiKit.Place(mOverlayTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 170f), new Vector2(700f, 110f));

            mOverlayBody = UiKit.CreateText(panel.transform, "Body", string.Empty, 40,
                new Color(0.42f, 0.30f, 0.16f, 1f), TextAnchor.MiddleCenter, mFont);
            UiKit.Place(mOverlayBody.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 50f), new Vector2(700f, 90f));

            mOverlayButton = UiKit.CreateButton(panel.transform, "Action", "下一关", 44, ButtonColor, mRoundSprite,
                TextColor, mFont, OnOverlayClicked);
            UiKit.Place(mOverlayButton.transform as RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -170f), new Vector2(400f, 120f));
            mOverlayButtonLabel = mOverlayButton.GetComponentInChildren<Text>();

            mOverlay.SetActive(false);
        }

        /// <summary>
        /// 没有 EventSystem 就建一个
        /// </summary>
        private static void EnsureEventSystem()
        {
            if (FindObjectOfType<EventSystem>() != null)
            {
                return;
            }

            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        /// <summary>
        /// 刷新整个界面
        /// </summary>
        private void Refresh()
        {
            if (mGame == null)
            {
                return;
            }

            mTitleText.text = "第 " + mNumber + " 关";
            mTitleText.fontSize = 52;
            mRemainText.text = "剩余 " + mGame.Remaining() + " 个";
            if (mRoadBadgeText != null)
            {
                mRoadBadgeText.text = (mAnimating ? 1 : 0) + "/6";
            }

            RefreshBoard();
            RefreshBuses();

            if (mGame.IsWin)
            {
                ShowOverlay("通关！", "发了 " + mGame.Dispatched + " 辆车", "下一关");
            }
            else if (mGame.IsStuck)
            {
                ShowOverlay("卡住了", "没车了，还剩 " + mGame.Remaining() + " 个", "重开");
            }
            else
            {
                mOverlay.SetActive(false);
            }
        }

        /// <summary>
        /// 画球坑：外面一圈草绿边框，里面是沙地
        /// </summary>
        private void RefreshBoard()
        {
            UiKit.Clear(mBoardRoot);

            mCellSize = Mathf.Min(130f, 1000f / mLevel.Width, 600f / mLevel.Height);
            var width = mCellSize * mLevel.Width;
            var height = mCellSize * mLevel.Height;

            // 球坑贴着板子的顶边放，下面留给道路
            var baseY = (mBoardRoot.rect.height - (height + 32f)) * 0.5f;
            var origin = new Vector2(0f, baseY);
            mBoardOrigin = origin;

            var frame = UiKit.CreateImage(mBoardRoot, "PitFrame", mRoundSprite, GrassColor);
            UiKit.Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                origin, new Vector2(width + 32f, height + 32f));

            var sand = UiKit.CreateImage(mBoardRoot, "PitSand", mRoundSprite, SandColor);
            UiKit.Place(sand.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                origin, new Vector2(width + 16f, height + 16f));

            mBallObjects = new GameObject[mLevel.Width, mLevel.Height];

            for (var y = 0; y < mLevel.Height; y++)
            {
                for (var x = 0; x < mLevel.Width; x++)
                {
                    var color = mGame.Cells[mLevel.Index(x, y)];
                    if (color < 0)
                    {
                        continue;
                    }

                    var posX = (x - (mLevel.Width - 1) * 0.5f) * mCellSize;
                    var posY = (y - (mLevel.Height - 1) * 0.5f) * mCellSize;

                    var person = PersonSprite(color);
                    var ball = person != null
                        ? UiKit.CreateImage(mBoardRoot, "P" + x + "_" + y, person, Color.white)
                        : UiKit.CreateImage(mBoardRoot, "P" + x + "_" + y, mCircleSprite,
                            Palette[color % Palette.Length]);

                    UiKit.Place(ball.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        origin + new Vector2(posX, posY), new Vector2(mCellSize * 0.94f, mCellSize * 1.12f));
                    ball.preserveAspect = true;
                    mBallObjects[x, y] = ball.gameObject;
                }
            }

            RefreshRoad();
        }

        /// <summary>
        /// 画四列巴士
        /// </summary>
        private void RefreshBuses()
        {
            UiKit.Clear(mBusRoot);

            var columns = mLevel.Columns;
            var spacing = 1060f / columns;
            mBusCards = new List<GameObject>[columns];
            mColumnHits.Clear();

            // 车位区铺一块浅蓝底，边框照缓存里的道路图块
            var floor = UiKit.CreateImage(mBusRoot, "Floor", mRoundSprite, new Color(0.58f, 0.83f, 0.96f, 1f));
            UiKit.Place(floor.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 18f), new Vector2(1040f, 430f));

            var floorInner = UiKit.CreateImage(floor.transform, "Inner", mRoundSprite,
                new Color(0.84f, 0.95f, 1f, 1f));
            UiKit.Place(floorInner.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(1020f, 410f));

            for (var column = 0; column < columns; column++)
            {
                var centerX = (column - (columns - 1) * 0.5f) * spacing;

                // 整列的点击判定区，点到列里任何地方都算发这列的车
                var hit = UiKit.CreateNode(mBusRoot, "Hit" + column);
                UiKit.Place(hit, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    new Vector2(centerX, 0f), new Vector2(spacing, 430f));
                mColumnHits.Add(hit);

                var stack = mGame.Stacks[column];
                mBusCards[column] = new List<GameObject>(stack.Count);

                for (var i = 0; i < stack.Count; i++)
                {
                    var card = stack[i];
                    var isFront = i == 0;

                    var color = Palette[card.Color % Palette.Length];

                    if (isFront)
                    {
                        var ring = UiKit.CreateImage(mBusRoot, "Ring" + column, mRoundSprite,
                            new Color(1f, 1f, 1f, 0.95f));
                        UiKit.Place(ring.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(centerX, i * 128f + 4f), new Vector2(spacing - 12f, 126f));
                    }

                    var bus = UiKit.CreateImage(mBusRoot, "Bus" + column + "_" + i, mRoundSprite,
                        WithAlpha(color, isFront ? 1f : 0.85f));
                    UiKit.Place(bus.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(centerX, i * 128f + 8f), new Vector2(spacing - 26f, 114f));
                    mBusCards[column].Add(bus.gameObject);

                    var label = UiKit.CreateText(bus.transform, "Label", card.Points.ToString(), 48,
                        new Color(1f, 1f, 1f, isFront ? 1f : 0.75f), TextAnchor.MiddleCenter, mFont);
                    UiKit.Place(label.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(0f, 8f), new Vector2(160f, 80f));

                    // 车头两扇小窗
                    for (var w = 0; w < 2; w++)
                    {
                        var window = UiKit.CreateImage(bus.transform, "Window" + w, mSmallRoundSprite,
                            new Color(1f, 1f, 1f, isFront ? 0.92f : 0.5f));
                        UiKit.Place(window.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                            new Vector2(w == 0 ? -32f : 32f, 12f), new Vector2(46f, 20f));
                    }
                }
            }
        }

        /// <summary>
        /// 点了一辆车（Button 走的入口，保留给以后的卡片点击用）
        /// </summary>
        private void OnBusClicked(int column)
        {
            Dispatch(column, false);
        }

        /// <summary>
        /// 发车：先让模型走一步，再播动画，动画结束再整体刷新
        /// </summary>
        /// <param name="column">列</param>
        /// <param name="fromHint">是不是提示按钮触发的</param>
        private void Dispatch(int column, bool fromHint)
        {
            if (mGame == null || mGame.IsWin || mAnimating)
            {
                return;
            }

            var oldCells = (int[])mGame.Cells.Clone();
            var absorbed = mGame.Dispatch(column);
            if (absorbed < 0)
            {
                return;
            }

            ShowToast(fromHint
                ? "提示：第 " + (column + 1) + " 列，吸 " + absorbed + " 个"
                : absorbed == 0 ? "这辆车没吸到球" : "吸走 " + absorbed + " 个");

            mAnimating = true;
            mAnimatingSince = Time.time;
            StartCoroutine(AnimateDispatch(column, oldCells, absorbed));
        }

        /// <summary>
        /// 发车动画：车滑走淡出，吸掉的球缩小消失，剩下的球往下落
        /// </summary>
        /// <param name="column">发的哪一列</param>
        /// <param name="oldCells">发车前的盘面</param>
        /// <param name="absorbed">吸走几个</param>
        private IEnumerator AnimateDispatch(int column, int[] oldCells, int absorbed)
        {
            var moves = new List<RectTransform>();
            var from = new List<Vector2>();
            var to = new List<Vector2>();
            var removed = new List<GameObject>();

            var width = mLevel.Width;
            var height = mLevel.Height;

            for (var x = 0; x < width; x++)
            {
                var oldYs = new List<int>();
                for (var y = 0; y < height; y++)
                {
                    if (oldCells[mLevel.Index(x, y)] >= 0)
                    {
                        oldYs.Add(y);
                    }
                }

                var newYs = new List<int>();
                for (var y = 0; y < height; y++)
                {
                    if (mGame.Cells[mLevel.Index(x, y)] >= 0)
                    {
                        newYs.Add(y);
                    }
                }

                var i = 0;
                var j = 0;
                while (i < oldYs.Count)
                {
                    var oldY = oldYs[i];
                    var kept = false;

                    if (j < newYs.Count && mGame.Cells[mLevel.Index(x, newYs[j])] == oldCells[mLevel.Index(x, oldY)])
                    {
                        var obj = mBallObjects[x, oldY];
                        if (obj != null)
                        {
                            var rect = (RectTransform)obj.transform;
                            moves.Add(rect);
                            from.Add(BallPosition(x, oldY));
                            to.Add(BallPosition(x, newYs[j]));
                        }

                        kept = true;
                        j++;
                    }

                    if (!kept)
                    {
                        removed.Add(mBallObjects[x, oldY]);
                    }

                    i++;
                }
            }

            var busCard = column < mBusCards.Length && mBusCards[column].Count > 0 ? mBusCards[column][0] : null;
            var busRect = busCard != null ? (RectTransform)busCard.transform : null;
            var busFrom = busRect != null ? busRect.anchoredPosition : Vector2.zero;

            // 先开到底部车道，再顺着路往上开进球坑
            var lanePoint = new Vector2(0f, 465f);
            var topPoint = new Vector2(0f, 1150f);

            // 车位卡片比路宽，开上路时缩到路宽
            var roadWidth = mCellSize * Mathf.Max(2, mLevel.Width / 8) + 18f;
            var driveScale = Mathf.Clamp(roadWidth / Mathf.Max(1f, busRect != null ? busRect.rect.width : 1f),
                0.4f, 1f);

            yield return MoveBus(busRect, busFrom, lanePoint, 0.22f, false, driveScale);
            yield return MoveBus(busRect, lanePoint, topPoint, 0.35f, false, driveScale);

            // 被吸走的人飞进车里
            var fliers = new List<RectTransform>();
            var flierFrom = new List<Vector2>();
            var flierImages = new List<Image>();
            var flierColors = new List<Color>();

            for (var k = 0; k < removed.Count; k++)
            {
                if (removed[k] == null)
                {
                    continue;
                }

                var rect = (RectTransform)removed[k].transform;
                var image = removed[k].GetComponent<Image>();
                fliers.Add(rect);
                flierFrom.Add(rect.anchoredPosition);
                flierImages.Add(image);
                flierColors.Add(image != null ? image.color : Color.white);
            }

            var target = busRect != null
                ? (Vector2)mBoardRoot.InverseTransformPoint(busRect.position)
                : Vector2.zero;

            // 车身上的数字随吸人递减
            var busLabel = busCard != null ? busCard.GetComponentInChildren<Text>() : null;
            var startPoints = 0;
            if (busLabel != null)
            {
                int.TryParse(busLabel.text, out startPoints);
            }

            var endPoints = Mathf.Max(0, startPoints - absorbed * 10);

            const float flyTime = 0.42f;
            const float fallTime = 0.52f;
            var clock = 0f;

            while (clock < flyTime + 0.18f)
            {
                clock += Time.deltaTime;

                ApplyFall(moves, from, to, Mathf.Clamp01(clock / fallTime));

                if (busLabel != null)
                {
                    var progress = Mathf.Clamp01(clock / flyTime);
                    busLabel.text = Mathf.RoundToInt(Mathf.Lerp(startPoints, endPoints, progress)).ToString();
                }

                for (var k = 0; k < fliers.Count; k++)
                {
                    var delay = k * 0.018f;
                    var local = Mathf.Clamp01((clock - delay) / Mathf.Max(0.18f, flyTime - delay));
                    var ease = local * local * (3f - 2f * local);
                    var arc = Mathf.Sin(ease * Mathf.PI) * 70f;

                    fliers[k].anchoredPosition = Vector2.Lerp(flierFrom[k], target, ease) + new Vector2(0f, arc);
                    var scale = Mathf.Lerp(1f, 0.35f, ease);
                    fliers[k].localScale = new Vector3(scale, scale, 1f);

                    if (flierImages[k] != null)
                    {
                        var color = flierColors[k];
                        color.a = 1f - ease;
                        flierImages[k].color = color;
                    }
                }

                yield return null;
            }

            // 开回车道，再沿车道驶出
            yield return MoveBus(busRect, topPoint, lanePoint, 0.25f, false, driveScale);
            yield return MoveBus(busRect, lanePoint, lanePoint + new Vector2(640f, 0f), 0.3f, true, driveScale);

            mAnimating = false;
            Refresh();
        }

        /// <summary>
        /// 让一辆车从 from 开到 to
        /// </summary>
        /// <param name="rect">车的矩形</param>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <param name="duration">时长</param>
        /// <param name="fade">是否顺便淡出</param>
        /// <returns>协程</returns>
        private static IEnumerator MoveBus(RectTransform rect, Vector2 from, Vector2 to, float duration,
            bool fade = false, float endScale = 1f)
        {
            if (rect == null)
            {
                yield break;
            }

            var image = rect.GetComponent<Image>();
            var color = image != null ? image.color : Color.white;
            var startScale = rect.localScale.x;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var ease = t * t * (3f - 2f * t);

                rect.anchoredPosition = Vector2.Lerp(from, to, ease);
                var scale = Mathf.Lerp(startScale, endScale, ease);
                rect.localScale = new Vector3(scale, scale, 1f);

                if (fade && image != null)
                {
                    image.color = WithAlpha(color, 1f - ease);
                }

                yield return null;
            }

            rect.anchoredPosition = to;
            rect.localScale = new Vector3(endScale, endScale, 1f);
        }

        /// <summary>
        /// 把一批矩形从旧位置缓动到新位置
        /// </summary>
        /// <param name="rects">矩形</param>
        /// <param name="from">起点</param>
        /// <param name="to">终点</param>
        /// <param name="t">进度 0~1</param>
        private static void ApplyFall(List<RectTransform> rects, List<Vector2> from, List<Vector2> to, float t)
        {
            var ease = t * t * (3f - 2f * t);
            for (var i = 0; i < rects.Count; i++)
            {
                rects[i].anchoredPosition = Vector2.Lerp(from[i], to[i], ease);
            }
        }

        /// <summary>
        /// 某个格子的局部坐标
        /// </summary>
        /// <param name="x">列</param>
        /// <param name="y">行</param>
        /// <returns>坐标</returns>
        private Vector2 BallPosition(int x, int y)
        {
            return mBoardOrigin + new Vector2(
                (x - (mLevel.Width - 1) * 0.5f) * mCellSize,
                (y - (mLevel.Height - 1) * 0.5f) * mCellSize);
        }

        /// <summary>
        /// 点加号：原作里是看广告多放两辆车上路
        /// </summary>
        private void OnPlusClicked()
        {
            ShowToast("看广告加车（广告还没接）");
        }

        /// <summary>
        /// 提示：直接走贪心认为最好的一列
        /// </summary>
        private void OnHintClicked()
        {
            if (mGame == null || mGame.IsWin || mAnimating)
            {
                return;
            }

            var best = BestColumn();
            if (best < 0)
            {
                ShowToast("这局已经没救了，重开吧");
                return;
            }

            Dispatch(best, true);
        }

        /// <summary>
        /// 重开
        /// </summary>
        private void OnRestartClicked()
        {
            if (mAnimating)
            {
                return;
            }

            mGame = BallGame.Create(mLevel);
            Refresh();
        }

        /// <summary>
        /// 浮层按钮
        /// </summary>
        private void OnOverlayClicked()
        {
            if (mGame == null)
            {
                return;
            }

            if (mGame.IsWin)
            {
                PlayerPrefs.SetInt("seewho.level", mNumber + 1);
                PlayerPrefs.Save();
                StartGame(mNumber + 1);
                return;
            }

            StartGame(mNumber);
        }

        /// <summary>
        /// 哪一列的车能吸最多
        /// </summary>
        private int BestColumn()
        {
            var best = -1;
            var bestAbsorb = 0;

            for (var column = 0; column < mLevel.Columns; column++)
            {
                var absorb = mGame.PreviewAbsorb(column);
                if (absorb > bestAbsorb)
                {
                    bestAbsorb = absorb;
                    best = column;
                }
            }

            return best;
        }

        /// <summary>
        /// 显示浮层
        /// </summary>
        private void ShowOverlay(string title, string body, string action)
        {
            mOverlayTitle.text = title;
            mOverlayBody.text = body;
            mOverlayButtonLabel.text = action;
            mOverlay.SetActive(true);
        }

        /// <summary>
        /// 弹一行提示
        /// </summary>
        private void ShowToast(string message)
        {
            mToastText.text = message;
            mToastTimer = 1.4f;
            var color = mToastText.color;
            color.a = 1f;
            mToastText.color = color;
        }

        /// <summary>
        /// 换透明度
        /// </summary>
        private static Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        /// <summary>
        /// 颜色编号对应的小人精灵
        /// </summary>
        /// <param name="color">颜色编号</param>
        /// <returns>精灵，没加载到返回空</returns>
        private Sprite PersonSprite(int color)
        {
            if (mPeople.Length == 0)
            {
                return null;
            }

            var index = SpriteMap[color % SpriteMap.Length] % mPeople.Length;
            return mPeople[index];
        }

        /// <summary>
        /// 按关号造一关：优先用原作导出的数据，没有再程序生成
        /// </summary>
        private static BallLevel CreateLevel(int number)
        {
            if (LevelLoader.TryLoad(number, out var real))
            {
                return real;
            }

            var difficulty = number <= 5
                ? BallDifficulty.Easy
                : number <= 12
                    ? BallDifficulty.Medium
                    : BallDifficulty.Hard;

            for (var attempt = 0; attempt < 6; attempt++)
            {
                var options = BallGeneratorOptions.For(difficulty, number * 97 + attempt * 31);
                var level = BallGenerator.Generate(options);
                if (level != null)
                {
                    return level;
                }
            }

            return BallGenerator.Build(BallGeneratorOptions.For(BallDifficulty.Easy, number), number);
        }
    }
}
