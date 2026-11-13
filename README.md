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
  Res/                   美术、音频等资源
Packages/
  manifest.json          Unity 包依赖
  my-game-libra/         libra 子模块，嵌入式包
ProjectSettings/
  ProjectVersion.txt     Unity 版本
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

## 下一步

- `GameEntry.Main` 目前还没接玩法模块，关卡数据、车辆、车位的逻辑后面往 `Assets/Scripts/Game/Client` 里加
- 纯逻辑（关卡求解、规则判定）放 `Common`，方便脱离 Unity 单测
