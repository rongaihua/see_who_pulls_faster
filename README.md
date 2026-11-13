# see_who_pulls_faster

《看谁先上车》的复刻练习工程（Unity + uGUI）。

玩法照着原作第 22 关的样子做：上面是一个装满彩球的坑，下面是四列等着发车的巴士，
点每列最前面那辆车，它会把坑里同色的球吸走，上面的球往下落。

## 环境

- Unity **2022.3.62f3**（和 PixelCar 同一个版本）
- 依赖 [my-game-libra](https://github.com/rongaihua/my-game-libra)，以 git submodule 的形式放在 `Packages/my-game-libra`

## 拉取

```bash
git clone --recursive git@github.com:rongaihua/see_who_pulls_faster.git
```

已经 clone 过、但子模块是空的：

```bash
git submodule update --init --recursive
```

## 目录

```
Assets/
  Scenes/                入口场景（entry.unity，由编辑器菜单生成）
  Scripts/
    Editor/              编辑器工具（seewho_editor）：入口场景、界面截图
    Game/
      Client/            客户端逻辑（seewho_client）
        Entry/           入口 GameEntry
        Game/            界面 GameView + uGUI 小工具 UiKit
      Common/            纯 C# 公共逻辑，不依赖 Unity（seewho_common）
        Balls/           当前的球堆玩法：球盘、发车、吸球、下落、生成器
        Level/           第一版的排队上车规则（留着做备用玩法）
  Resources/SeeWho/      字体和 UI 图（从 PixelCar 借的）
Packages/
  manifest.json          Unity 包依赖
  my-game-libra/         libra 子模块，嵌入式包
ProjectSettings/
  ProjectVersion.txt     Unity 版本
Tools/
  LevelLab/              离线自检程序（dotnet，不依赖 Unity）
```

## libra 的引用方式

和 PixelCar 一样走「嵌入式包」：libra 直接放在 `Packages/my-game-libra`，Unity 会把它当本地包自动加载，**不需要**在 `manifest.json` 里写依赖。`Packages/packages-lock.json` 里会显示成 `"source": "embedded"`。

业务代码只要在自己的 asmdef 里引用它的程序集：

```json
"references": [
    "MyGame.Shared",
    "MyGame.Client"
]
```

子模块当前固定在 `4391b44`（添加音乐管理器），和 PixelCar 用的是同一个提交。

## 美术资源（从缓存拆的）

资源来自微信小游戏的缓存快照（`...\local\wx641f825d5dce9ea9\usr\gamecaches`），拆图脚本在 `Tools/ArtExtract/extract_art.py`：

```bash
python Tools/ArtExtract/extract_art.py <缓存 gamecaches 目录> Assets
```

拆出来的东西：

| 文件 | 来源 | 说明 |
|---|---|---|
| `Resources/SeeWho/Art/bg_level.png` | `prefabs/179444969224831.png` | 主背景：蓝天、草地、彩旗、木牌。**缓存里扩展名是 .png，内容其实是 webp**，脚本会转成真 PNG |
| `Resources/SeeWho/Art/People/person_00..17.png` | `prefabs/179428657819468.png` | 小人精灵图（8 列 × 2 行，列宽 48）切出来的 18 种颜色 |

切图要点：

- 精灵图里人物是紧密排列的，直接按格子切会带进隔壁的像素，所以每格用**连通域取最大一块**再裁包围盒；
- 颜色编号到精灵编号做了映射（`GameView.SpriteMap`），只挑鲜艳的 8 个：橙、黄、浅绿、青、蓝、粉、红、淡紫；
- 木牌上的关卡标题、沙地 + 草绿边框的球坑、四列车位，都是按原作的配色用 uGUI 画的。

> 这些素材版权属于原作者，只用于本地原型对照，别打包发布。

## 入口场景

打开工程后点菜单 **SeeWho/生成入口场景**，会生成 `Assets/Scenes/entry.unity` 并加进 Build Settings。场景里只有一个挂着 `GameEntry` 的 Entry 物体，`GameEntry` 负责初始化 libra 的 `MyGame.G`、准备相机，然后起一局。

命令行生成（CI / 无界面）：

```bash
Unity.exe -batchmode -quit -projectPath <工程路径> -executeMethod SeeWho.Editor.EntrySceneBuilder.Build
```

## 球堆玩法（`Common/Balls`）

规则（`BallGame`）：

1. 球盘按 `Width × Height` 存颜色，第 0 行在最下面；
2. 底部四列各摆 3 辆车，**只能发每列最前面那辆**；
3. 发出去的车把盘上同色的球吸走，从下往上吸，最多吸自己的容量；
4. 吸完每列的球往下落，上面的颜色才会露出来；
5. 球盘清空就通关；没车了还有球就卡住。

车身上的数字 = `容量 × 10`，和原作一样是 100 / 200 这种写法。

| 类 | 职责 |
|---|---|
| `BallLevel` | 静态数据：球盘尺寸、颜色、`Cells`、车队 `Deck` |
| `BusCard` | 一辆车：颜色 + 容量 |
| `BallGame` | 局面：发车、吸球、下落、胜负判定 |
| `BallGenerator` | 造关卡：先铺同色色块，再配够用的车队，最后用贪心验一遍能通关 |

生成器会保证车队总容量约是球数的 1.35 倍，并且至少能摆满所有车位；否则一步吸空就会过不去。

### 发车动画

点车之后不是瞬间刷新，而是先改模型再播 0.3 秒动画（`GameView.AnimateDispatch`）：

- 被吸掉的球**缩小消失**；
- 剩下的球按新位置**往下落**（缓动，带点重力感）；
- 被发的车**向下滑走并淡出**，后面的车在动画结束后整体上移。

动画期间 `mAnimating` 会挡住输入，避免连点把状态打乱。匹配“旧格子 → 新格子”用的是每列从下往上的双指针比对，因为下落只压紧、不改变相对顺序。

## 第一版排队规则（`Common/Level`）

最早的版本：队伍先进先出，队头能上就上、上不了就去候车区，车满发车。现在客户端没接它，但规则、求解器（BFS 最少步数）和生成器都还能用，离线自检里也还在跑。

## 离线自检

```bash
cd Tools/LevelLab
dotnet run -c Release
```

会跑两套规则的断言（吸球、下落、生成、可通关），再打印样例；全过时退出码为 0。

## 界面预览

没有显示器时可以让 Unity 直接渲染一张 PNG：

```bash
Unity.exe -batchmode -quit -projectPath <工程路径> -executeMethod SeeWho.Editor.UiPreview.Capture
```

输出到工程根目录的 `Preview/ui_easy.png`、`ui_hard.png`、`ui_mid.png`。

## 下一步

- 把缓存里那套**道路图集**（U 形桥、草地方块、箭头）铺到球坑和车位之间，做成原作那种环路
- 接上原作的关卡数据（`queue` / `bus` 里的 `颜色_数量`），替掉现在的程序生成
- 体力、广告加车位（`0/6` + 加号）、道具（消除 / 打乱 / 磁铁）
