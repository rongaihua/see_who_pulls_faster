# 看谁先上车 — 微信小游戏缓存快照

- 游戏名：看谁先上车
- AppID：`wx641f825d5dce9ea9`
- 来源客户端：微信 4.x（Weixin，安装于 `C:\Program Files (x86)\Tencent\Weixin`）
- 快照时间：2026-11-13 11:29（Asia/Shanghai）
- 源目录：`C:\Users\30604\AppData\Roaming\Tencent\xwechat\radium\users\df6269b8ffc3697d0ddc1bc82f6dd76d\applet`
- 文件数：358，总计 21,275,342 字节（约 20.3 MB），已逐文件 SHA-256 校验一致

## 目录结构

```
packages\wx641f825d5dce9ea9\21\   代码包/资源包（11 个 .wxapkg，版本 21，8.9 MB）
local\wx641f825d5dce9ea9\         运行数据（346 个文件，11.4 MB）
  ├─ usr\gamecaches\              游戏资源缓存 common/language/level/pixelMap/prefabs/uiSprites + cacheList.json
  ├─ usr\config\wx_ksxsc\         zs_game_config.json、zs_mp_config.json
  ├─ usrmmkvstorage0/1\           MMKV 键值存档
  ├─ launch.config                窗口尺寸/启动配置
  └─ temp\ store\               临时文件与图片
icon\wx641f825d5dce9ea9_*.png     游戏图标（140×140）
```

## 未包含的全局共享缓存

以下为同一微信账号下所有小程序/小游戏共用，不属于本游戏专属，未复制：

- `applet\local\globalmmkvstorage\`
- `applet\local\shared\`（广告素材缓存）
- `applet\data\8\applet.db`（加密的小程序清单库）
- `applet\codecache\26134\`

## 说明

- 快照时游戏仍在运行（`WeChatAppEx` 进程活跃），`launch.config` 可能随窗口操作变化；本快照为该时刻的只读副本。
- 恢复时把 `packages` 与 `local` 下对应目录放回源目录即可；直接删除 `local` 会清空本地存档，请先备份。
