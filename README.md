# MuseDashSteamPresence

[简体中文](README.md) | [English](docs/README.en.md)

在 Steam 好友列表显示你正在 Muse Dash 中游玩的曲名，支持本体曲目和 Euterpe 自制曲。可自定义状态、歌曲作者和分隔符，好友无需安装。

**[下载模组 DLL](https://github.com/Fanxiaowei145/MuseDashSteamPresence/releases/download/v0.5.1-beta.1/MuseDashSteamPresence.dll)** · **[下载完整安装包](https://github.com/Fanxiaowei145/MuseDashSteamPresence/releases/download/v0.5.1-beta.1/MuseDashSteamPresence-0.5.1-beta.1-win-x64.zip)** · [版本页面](https://github.com/Fanxiaowei145/MuseDashSteamPresence/releases/tag/v0.5.1-beta.1)

当前版本：**0.5.1 Beta**。上面的链接可直接下载；也可进入版本页面，在 **Assets** 中选择 DLL 或 `win-x64.zip`。两种安装方式任选一种，`Source code` 是源码。

## 效果

| 未打歌（开启自定义文字） | 打歌（开启作者显示） |
| --- | --- |
| ![选歌状态](docs/images/friend-selecting.png) | ![歌曲状态](docs/images/friend-playing.png) |

新装默认：未打歌显示 `Muse Dash`，打歌显示 `Muse Dash : 曲名`，不显示作者。截图中的自定义文字和作者均可自行开启。

## 安装

需要 **Windows x64、Steam 正版 Muse Dash、MelonLoader 0.7.3 net6**。

1. 关闭游戏，下载上方的 **模组 DLL**。
2. Steam 库右键 Muse Dash → 管理 → 浏览本地文件，将 `MuseDashSteamPresence.dll` 放进 **`Mods`** 文件夹。
3. 从 Steam 正常启动游戏一次，配套设置工具会自动生成，开始打歌即可显示曲名。

没有 MelonLoader？先用[官方安装器](https://github.com/LavaGang/MelonLoader.Installer/releases/latest)为 Muse Dash 安装 **0.7.3**，启动游戏初始化一次。已通过 Euterpe 完成游戏环境安装的玩家可跳过；仅下载 Euterpe 软件不算完成。

希望首次开游戏前就修改设置，可改用 **完整安装包**：关闭游戏，将 ZIP 内全部内容解压到 `musedash.exe` 同级，再打开设置工具。完整步骤见[安装说明](docs/INSTALL.md)。

Euterpe Hub 上架后，也可通过客户端下载本模组；具体以平台审核状态为准。

## 自定义

双击游戏根目录的 **`MuseDash-Settings.cmd`**，调整后点击 **“保存并应用”**。

- 自定义非打歌时的文字，或只显示游戏名。
- 独立开关曲名和歌曲作者。
- 选择无、空格、`_` 或自定义分隔符。

设置工具独立运行，无需启动游戏或保持 Euterpe 打开；游戏运行中保存也会更新状态。升级保留已有设置。退出游戏后运行 `MuseDash-Disable.cmd` 可停用，`MuseDash-Enable.cmd` 可恢复。

## 兼容与限制

- 已验证跨一次游戏版本更新正常使用，其他游戏版本不保证兼容。
- 使用私有 Steam 客户端接口，**仅已适配的 Steam DLL 指纹可发送状态**，不匹配时停止发送。[兼容详情](docs/COMPATIBILITY.md)
- 当前已验证客户端下，切歌不会反复触发“开始玩游戏”通知；首次启动仍可能正常通知。
- 过长的曲名与作者可能显示不全，目前没有保证完整显示的解决方案。

主 DLL 内嵌自己的辅助程序、设置工具和早期插件，首次运行在本地释放，无需额外下载；不修改游戏、Steam 或其他模组原文件。

作者：**樊小胃 / Fanxiaowei145**。免费开源，采用 [MIT 许可证](LICENSE)，欢迎提交 [Issue](https://github.com/Fanxiaowei145/MuseDashSteamPresence/issues) 或改进。

[使用说明](docs/USAGE.md) · [构建说明](docs/BUILD.md) · [更新记录](CHANGELOG.md)
