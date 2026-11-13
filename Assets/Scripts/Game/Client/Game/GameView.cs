//---------------------------------------------------------------------------------------
// Copyright (c) rongaihua 2023-2030
// Author: rongaihua
// Date: 2026-11-13
// Desc: 球堆玩法的主界面，全部用 uGUI 代码搭
//---------------------------------------------------------------------------------------

using SeeWho.Common.Balls;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SeeWho.Client
{
    /// <summary>
    /// 游戏主界面：上面是堆满彩球的坑，下面是四列等着发车的巴士。
    /// 点每列最前面那辆车，它会把同色的球吸走，上面的球往下落。
    /// </summary>
    public sealed class GameView : MonoBehaviour
    {
        /// <summary>
        /// 球的颜色表
        /// </summary>
        private static readonly Color[] Palette =
        {
            new Color(0.89f, 0.34f, 0.18f),
            new Color(0.18f, 0.53f, 0.67f),
            new Color(0.23f, 0.70f, 0.45f),
            new Color(0.95f, 0.76f, 0.31f),
            new Color(0.61f, 0.36f, 0.90f),
            new Color(0.95f, 0.36f, 0.71f),
            new Color(0.35f, 0.42f, 0.85f),
            new Color(0.90f, 0.55f, 0.30f),
        };

        private static readonly Color TextColor = new Color(0.96f, 0.97f, 0.99f);
        private static readonly Color WeakText = new Color(0.96f, 0.97f, 0.99f, 0.55f);
        private static readonly Color PanelColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color ButtonColor = new Color(0.24f, 0.70f, 0.45f);
        private static readonly Color ButtonGray = new Color(1f, 1f, 1f, 0.14f);

        private Font mFont;
        private Sprite mCircleSprite;
        private Sprite mRoundSprite;
        private Sprite mBgSprite;

        private BallLevel mLevel;
        private BallGame mGame;
        private int mNumber = 1;

        private RectTransform mBoardRoot;
        private RectTransform mBusRoot;
        private Text mTitleText;
        private Text mRemainText;
        private Text mToastText;
        private Text mTipText;

        private GameObject mOverlay;
        private Text mOverlayTitle;
        private Text mOverlayBody;
        private Button mOverlayButton;
        private Text mOverlayButtonLabel;

        private float mToastTimer;
        private float mCellSize = 60f;

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
            Refresh();
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
        /// 每帧处理提示淡出
        /// </summary>
        private void Update()
        {
            if (mToastTimer <= 0f)
            {
                return;
            }

            mToastTimer -= Time.deltaTime;
            var color = mToastText.color;
            color.a = Mathf.Clamp01(mToastTimer / 0.5f);
            mToastText.color = color;
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
            mBgSprite = LoadSprite("SeeWho/UI/sky_gradient");
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

            var background = UiKit.CreateImage(root, "Background", mBgSprite, new Color(0.10f, 0.12f, 0.18f));
            UiKit.Stretch(background.rectTransform);

            mTitleText = UiKit.CreateText(root, "Title", "第 1 关", 50, TextColor, TextAnchor.MiddleLeft, mFont);
            UiKit.Place(mTitleText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(44f, -36f), new Vector2(460f, 86f));

            mRemainText = UiKit.CreateText(root, "Remain", "剩余 0", 38, WeakText, TextAnchor.MiddleRight, mFont);
            UiKit.Place(mRemainText.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-44f, -36f), new Vector2(400f, 86f));

            mBoardRoot = UiKit.CreateNode(root, "Board");
            UiKit.Place(mBoardRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -190f), new Vector2(1040f, 780f));

            mBusRoot = UiKit.CreateNode(root, "Buses");
            UiKit.Place(mBusRoot, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 430f), new Vector2(1060f, 420f));

            mTipText = UiKit.CreateText(root, "Tip", "点每列最前面的车，把同色的球吸走", 30,
                new Color(0.96f, 0.97f, 0.99f, 0.38f), TextAnchor.MiddleCenter, mFont);
            UiKit.Place(mTipText.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 370f), new Vector2(1000f, 60f));

            var hintButton = UiKit.CreateButton(root, "Hint", "提示", 40, ButtonGray, mRoundSprite, TextColor, mFont,
                OnHintClicked);
            UiKit.Place(hintButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-160f, 250f), new Vector2(240f, 96f));

            var restartButton = UiKit.CreateButton(root, "Restart", "重开", 40, ButtonGray, mRoundSprite, TextColor,
                mFont, OnRestartClicked);
            UiKit.Place(restartButton.transform as RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(160f, 250f), new Vector2(240f, 96f));

            mToastText = UiKit.CreateText(root, "Toast", string.Empty, 38, new Color(1f, 0.85f, 0.4f, 0f),
                TextAnchor.MiddleCenter, mFont);
            UiKit.Place(mToastText.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 60f), new Vector2(900f, 80f));

            BuildOverlay(root);
        }

        /// <summary>
        /// 通关 / 卡住的浮层
        /// </summary>
        private void BuildOverlay(RectTransform root)
        {
            var overlay = UiKit.CreateNode(root, "Overlay");
            UiKit.Stretch(overlay);
            mOverlay = overlay.gameObject;

            var dim = UiKit.CreateImage(overlay, "Dim", null, new Color(0f, 0f, 0f, 0.72f));
            UiKit.Stretch(dim.rectTransform);
            dim.raycastTarget = true;

            var panel = UiKit.CreateImage(overlay, "Panel", mRoundSprite, new Color(0.15f, 0.16f, 0.22f, 0.98f));
            UiKit.Place(panel.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(780f, 600f));

            mOverlayTitle = UiKit.CreateText(panel.transform, "Title", "通关", 64, TextColor, TextAnchor.MiddleCenter,
                mFont);
            UiKit.Place(mOverlayTitle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, 170f), new Vector2(700f, 110f));

            mOverlayBody = UiKit.CreateText(panel.transform, "Body", string.Empty, 40, WeakText,
                TextAnchor.MiddleCenter, mFont);
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
            mRemainText.text = "剩余 " + mGame.Remaining();

            RefreshBoard();
            RefreshBuses();

            if (mGame.IsWin)
            {
                ShowOverlay("通关！", "发了 " + mGame.Dispatched + " 辆车", "下一关");
            }
            else if (mGame.IsStuck)
            {
                ShowOverlay("卡住了", "没车了，还剩 " + mGame.Remaining() + " 个球", "重开");
            }
            else
            {
                mOverlay.SetActive(false);
            }
        }

        /// <summary>
        /// 画球坑
        /// </summary>
        private void RefreshBoard()
        {
            UiKit.Clear(mBoardRoot);

            mCellSize = Mathf.Min(74f, 1000f / mLevel.Width);
            var width = mCellSize * mLevel.Width;
            var height = mCellSize * mLevel.Height;

            var board = UiKit.CreateImage(mBoardRoot, "Pit", mRoundSprite, new Color(1f, 1f, 1f, 0.04f));
            UiKit.Place(board.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(width + 24f, height + 24f));

            var ballSize = mCellSize - 6f;

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

                    var ball = UiKit.CreateImage(mBoardRoot, "B" + x + "_" + y, mCircleSprite,
                        Palette[color % Palette.Length]);
                    UiKit.Place(ball.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                        new Vector2(posX, posY), new Vector2(ballSize, ballSize));
                }
            }
        }

        /// <summary>
        /// 画四列巴士
        /// </summary>
        private void RefreshBuses()
        {
            UiKit.Clear(mBusRoot);

            var columns = mLevel.Columns;
            var spacing = 1060f / columns;

            for (var column = 0; column < columns; column++)
            {
                var centerX = (column - (columns - 1) * 0.5f) * spacing;
                var stack = mGame.Stacks[column];

                for (var i = 0; i < stack.Count; i++)
                {
                    var card = stack[i];
                    var isFront = i == 0;
                    var index = column;

                    var color = Palette[card.Color % Palette.Length];
                    var bus = UiKit.CreateImage(mBusRoot, "Bus" + column + "_" + i, mRoundSprite,
                        WithAlpha(color, isFront ? 0.95f : 0.55f));
                    UiKit.Place(bus.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                        new Vector2(centerX, i * 132f), new Vector2(spacing - 24f, 118f));

                    if (isFront)
                    {
                        bus.raycastTarget = true;
                        var button = bus.gameObject.AddComponent<Button>();
                        button.targetGraphic = bus;
                        button.onClick.AddListener(() => OnBusClicked(index));
                    }

                    var label = UiKit.CreateText(bus.transform, "Label", card.Points.ToString(), 46,
                        new Color(1f, 1f, 1f, isFront ? 1f : 0.6f), TextAnchor.MiddleCenter, mFont);
                    UiKit.Stretch(label.rectTransform);
                }
            }
        }

        /// <summary>
        /// 点了一辆车
        /// </summary>
        private void OnBusClicked(int column)
        {
            if (mGame == null || mGame.IsWin)
            {
                return;
            }

            var absorbed = mGame.Dispatch(column);
            if (absorbed < 0)
            {
                return;
            }

            if (absorbed == 0)
            {
                ShowToast("这辆车没吸到球");
            }
            else
            {
                ShowToast("吸走 " + absorbed + " 个");
            }

            Refresh();
        }

        /// <summary>
        /// 提示：直接走贪心认为最好的一列
        /// </summary>
        private void OnHintClicked()
        {
            if (mGame == null || mGame.IsWin)
            {
                return;
            }

            var best = BestColumn();
            if (best < 0)
            {
                ShowToast("这局已经没救了，重开吧");
                return;
            }

            var absorbed = mGame.Dispatch(best);
            ShowToast("提示：第 " + (best + 1) + " 列，吸 " + absorbed + " 个");
            Refresh();
        }

        /// <summary>
        /// 重开
        /// </summary>
        private void OnRestartClicked()
        {
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

            StartGame(mGame.IsWin ? mNumber + 1 : mNumber);
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
        /// 按关号造一关
        /// </summary>
        private static BallLevel CreateLevel(int number)
        {
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
