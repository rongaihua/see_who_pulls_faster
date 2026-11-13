# see_who_pulls_faster

《看谁先上车》类休闲解谜小游戏的 Unity 工程，玩法是排队上车 / 挪车出库的顺序解谜。

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
    Editor/              编辑器工具（seewho_editor）
    Game/
      Client/            客户端逻辑（seewho_client）
        Entry/           入口 GameEntry
      Common/            纯 C# 公共逻辑，不依赖 Unity（seewho_common）
        Level/           关卡核心：数据、规则、求解器、生成器
  Res/                   美术、音频等资源
Packages/
  manifest.json          Unity 包依赖
  my-game-libra/         libra 子模块，嵌入式包
ProjectSettings/
  ProjectVersion.txt     Unity 版本
Tools/
  LevelLab/              关卡核心的离线自检程序（dotnet，不依赖 Unity）
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

## 入口场景

打开工程后点菜单 **SeeWho/生成入口场景**，会生成 `Assets/Scenes/entry.unity` 并加进 Build Settings。场景里只有一个挂着 `GameEntry` 的 Entry 物体，`GameEntry` 负责初始化 libra 的 `MyGame.G` 并准备正交相机。

命令行生成（CI / 无界面）：

```bash
Unity.exe -batchmode -quit -projectPath <工程路径> -executeMethod SeeWho.Editor.EntrySceneBuilder.Build
```

## 关卡核心

纯 C#，放在 `Assets/Scripts/Game/Common/Level`，不引用 UnityEngine，可以脱离 Unity 跑。

规则（`GameRules`）：

1. 队伍先进先出，只有队头能动；
2. 队头颜色对得上某个车位上还有空座的车，就能上车；
3. 上不了车就去候车区，候车区满还上不了车就卡死；
4. 候车区里的人随时可以补位上车；
5. 车坐满就发车，下一辆车立刻进站。

| 类 | 职责 |
|---|---|
| `LevelDefinition` | 关卡的静态数据：颜色数、座位数、车位、候车区、队伍、车队 |
| `GameState` | 局面：队头下标、候车区、车位上的车、车队进度 |
| `Move` | 一步操作：队头上车 / 队头候车 / 候车区上车 |
| `GameRules` | 合法操作、执行操作、胜负判定 |
| `LevelSolver` | 广度优先求最少步数的解，无解也能判定 |
| `LevelGenerator` | 随机造关卡 + 求解验证，按步数和候车压力卡难度 |
| `LevelStats` | 难度指标：步数、候车次数、候车区峰值 |

关卡生成时保证每种颜色的人数是座位数的整数倍，否则最后一辆车永远坐不满、会把车位一直占住。

### 离线自检

```bash
cd Tools/LevelLab
dotnet run -c Release
```

会跑规则/求解/生成的断言，再打印三档难度的样例关卡；全过时退出码为 0。

## 下一步

- `GameEntry` 还没接玩法模块，下一步把 `LevelDefinition` 喂给客户端，做队伍、车位、候车区的表现
- 关卡数据先由 `LevelGenerator` 生成成 JSON，编辑器里再手调
