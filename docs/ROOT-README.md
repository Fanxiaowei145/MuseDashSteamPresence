# Muse Dash Steam 状态插件：快速使用

**免费开源，欢迎改进。**

也可用记事本打开同目录 MuseDashPresence-ReadMe.txt 查看纯文本说明。

开始前先判断：

- 从 Euterpe Hub 安装本插件：点击下载后照常启动一次游戏，配套文件自动从 DLL 内准备，不需要补装。第一次运行后可通过 MuseDash-Settings.cmd 独立配置；之后改设置不必先开游戏，也不必打开 Euterpe。

- 已为 Muse Dash 安装 MelonLoader 0.7.3 net6，且能正常进入游戏：直接按下面的解压步骤操作。
- 没装过、不确定、或只有下载的安装器：先看后面的“没有 MelonLoader”步骤，安装并完成首次初始化，再解压本插件。

1. 退出 Muse Dash 和插件设置窗口。
2. 将压缩包中的全部文件／文件夹解压到 Muse Dash 根目录，使 `MuseDash-Settings.cmd` 与 `musedash.exe` 同级。
3. **先设置：** 双击 `MuseDash-Settings.cmd` 修改显示，点击“保存并应用”。无需启动游戏，保存后可以关闭设置窗口。
4. **再启动游戏：** 从 Steam 正常启动 Muse Dash，插件会读取已保存的设置。

设置软件可独立运行，不要求游戏或 Steam 正在运行；游戏运行中也可以修改，保存后约 0.5–1 秒更新。

本插件只要求 MelonLoader net6，不要求安装其他模组，也不要求打开其他模组管理器。发行包不捆绑或覆盖加载器。先退出游戏，即可在本设置窗口独立“启用插件／停用插件”，同时处理两个组件。

没有 MelonLoader？

1. 从 [MelonLoader 官方下载页](https://github.com/LavaGang/MelonLoader.Installer/releases/latest) 下载 Windows 文件 `MelonLoader.Installer.exe`。
2. 关闭游戏，打开安装器，选中 Muse Dash；未列出时点击 `Add Game Manually` 选择游戏的 `musedash.exe`。
3. 选择加载器版本 `0.7.3`，点击 `Install`。从 Steam 启动游戏一次，等待初始化并确认能进入游戏，然后退出。这仅用于首次初始化加载器，与修改设置无关。
4. 再按上面的步骤解压本插件。加载器的版本与安装器自身的版本号是两回事。

另一种可选方式是在 Euterpe 软件里完成首次环境配置：选 Muse Dash 游戏目录、选玩家身份、执行安装。此流程会自动下载安装 MelonLoader，也会准备其他基础组件；仅下载 Euterpe 不代表已安装加载器。它不是游戏内的新手教程，已有加载器时无需重做。

Steam 建议保持最新正式版；2026-11-01 已核对构建 1792618285。未来最新版变化后仍需通过兼容检测，未适配时不会发送状态。

可以自定义不打歌时文字，分别开关曲名和作者，选择无／空格／下划线／自定义分隔符。

默认不显示非打歌文字：未打歌时只显示 Muse Dash，打歌时显示 Muse Dash : 曲名，不带作者。非打歌文字与作者均可自行开启；升级保留已有设置，点击“恢复默认”并保存才应用新默认值。

当前已验证客户端下，选歌、开曲、返回选歌只更新状态，不会反复触发“开始玩游戏”的新弹窗，不用担心频繁切换状态打扰到好朋友们～首次启动仍可能正常通知，未来客户端需重新验证。

**已知限制：同时显示曲名和作者可能超过 Steam 的文本长度或界面限制，导致信息显示不全，目前尚无保证完整显示的解决方案。** 可尝试关闭作者或缩短分隔符。

本项目采用 MIT 许可证，欢迎提交 Issue、PR 或自行改进。完整安装、停用、兼容范围和项目说明见 [配套说明](UserData/MuseDashSteamPresence/README.md)。

关闭游戏后，`MuseDash-Disable.cmd` 停用两个插件，`MuseDash-Enable.cmd` 恢复。解压更新不自动备份，覆盖旧版前请自行备份。
