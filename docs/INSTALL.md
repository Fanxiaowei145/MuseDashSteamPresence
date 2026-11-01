# 安装、更新与停用

## 第一步：先判断加载器状态

- 已为 Muse Dash 正常安装 MelonLoader 0.7.3 net6：直接看“下载并解压”。
- 没装过、不确定、或只有安装器下载文件：先看“没有 MelonLoader 的玩家”。

不要求安装或打开其他模组管理器。设置窗口会检查游戏目录、两个插件文件、辅助程序和加载器，仍可在游戏未运行时保存设置。

## 先确认环境

- Steam 版 Muse Dash，Windows 10／11 x64，建议使用最新正式版 Steam；已核对 2026-11-01 当日无更新的构建 1792618285。仍需通过文件指纹检查。
- MelonLoader **net6**。已验证版本 0.7.3，请先独立启动游戏一次，使加载器完成初始化。
- 不要求安装其他模组；游戏测试环境为 Muse Dash 6.7.0。
- 设置软件和辅助程序使用 Windows 的 .NET Framework，推荐已安装 4.8。
- 当前 Steam 构建必须在[兼容范围](COMPATIBILITY.md)内，否则插件不会发送。

本压缩包只提供本项目文件，不安装运行库或加载器。是否已经安装其他模组，不能替代 MelonLoader 安装检查。

## 没有 MelonLoader 的玩家

只需要先安装一次：

1. 关闭 Muse Dash，打开 [MelonLoader 官方安装器下载页](https://github.com/LavaGang/MelonLoader.Installer/releases/latest)。
2. 下载其中的 Windows 文件 **MelonLoader.Installer.exe** 并打开。
3. 在游戏列表选中 **Muse Dash**；没有找到时，点击 **Add Game Manually**，选择游戏安装目录里的 `musedash.exe`。游戏目录可从 Steam 库右键游戏 → 管理 → 浏览本地文件打开。
4. 选择 **MelonLoader 0.7.3**，点击 **Install**。这里是加载器版本，不是安装器自己的版本。
5. 从 Steam 正常启动游戏一次，等待首次初始化完成，确认能正常进入游戏，再退出。这只是首次初始化加载器所需，不是打开设置的前提。
6. 继续下面的插件解压步骤。

安装器使用方法来自 [MelonLoader 官方安装器文档](https://github.com/LavaGang/MelonLoader.Installer#melonloader-installation)。如果已确认 MelonLoader 正常工作，不需要重新安装或覆盖现有加载器。

### 可选：通过 Euterpe 初始化安装加载器

从 [Euterpe 官方 Releases](https://github.com/Euterpe-org/App/releases/latest) 下载桌面程序，在 Euterpe 首次环境配置中选择 Muse Dash 并完成安装步骤。其“玩家”预设包含默认选中且必选的 MelonLoader、运行库、基础模组与冲突处理，执行成功后加载器会安装到所选游戏目录。

这与“只下载 Euterpe 就自带加载器”不同：未完成向导或安装失败，不能算已安装。Euterpe 有自己的游戏／DLC 前置条件，且其向导不只是安装加载器；请按其[官方文档](https://euterpe-org.com/zh-CN/docs)使用。已有加载器的玩家无需为了本插件重新执行完整向导；也不要求安装 Euterpe。

### 从 Hub 安装本插件

本插件的 Hub 发布通过后，在客户端点击下载主 DLL，正常启动游戏即可。单 DLL 内部已带齐配套文件，首次加载自动准备，无需额外下载、手动补装或让 Hub 管理员改变安装机制。

首次运行后，游戏根目录会出现 `MuseDash-Settings.cmd`。打开它就能独立改文字、曲名／作者和分隔符；关闭 Euterpe 后照常可用。首次 Hub 安装先开一次游戏只是为了准备设置程序；以后改设置不需要先开游戏。GitHub 完整包则始终可先设置再启动。

配套文件有清单与 SHA256 校验，只写本项目的 `UserData` 子目录、早期组件和专用根目录入口，不改游戏／Steam 原文件或个人设置。升级替换自己的旧文件时保留备份；设置窗口占用文件等情况会回滚并给出 `deployment-warning.txt`，关闭设置后重开游戏重试。不会强杀进程或联网下载修复。

## 下载并解压

1. 打开 GitHub 仓库的 Releases，选择预发布版本下的 `*-win-x64.zip`。
2. 退出游戏、关闭本插件的设置窗口。
3. Steam → 库 → 右键 Muse Dash → 管理 → 浏览本地文件。
4. 打开压缩包，把里面的 `Mods`、`Plugins`、`UserData` 和几个 `MuseDash-*.cmd` 等文件全部合并到游戏目录。
5. 确认 `MuseDash-Settings.cmd` 和原有 `musedash.exe` 在同一级。
6. **保持游戏关闭，先设置：** 双击 `MuseDash-Settings.cmd`，调整后点击“保存并应用”，然后可以关闭设置窗口。
7. **再从 Steam 启动游戏。** 插件会读取刚保存的设置。

**不需要运行安装脚本。** 不能只把 ZIP 文件放进游戏目录，也不能只解压其中一个 DLL。

设置软件可独立运行，保存配置不要求游戏或 Steam 正在运行。游戏运行中也可修改设置，保存后约 0.5–1 秒更新，无需重启游戏。

根目录 README 采用 `MuseDashPresence-README.md` 这个专用名称，避免覆盖其他模组的 README。发行包没有 `settings.xml`，因此不会用默认设置覆盖已有配置。

## 更新

退出游戏和设置窗口，先将下面两个 DLL 和 `UserData/MuseDashSteamPresence` 复制到自己的备份位置，再将新版完整解压到同一游戏目录，确认覆盖本项目文件。

- `Mods/MuseDashSteamPresence.dll`
- `Plugins/MuseDashSteamPresence.Bootstrap.dll`

解压式升级**不自动创建备份**。不要覆盖自己已保存的设置文件；官方包不会携带此文件。更新后重开游戏；日常仅修改设置不需要重启游戏或 Steam。

## 停用／恢复

先关闭游戏：

- 打开 `MuseDash-Settings.cmd`，直接点击窗口内“停用插件”；恢复时点击“启用插件”。两者都会同时处理本项目两个组件，无需其他管理器。
- 双击根目录 `MuseDash-Disable.cmd`，将本项目两个 DLL 改名为 `.dll.disabled`，设置保留。
- 需要恢复时双击 `MuseDash-Enable.cmd`。

也可手动将上面两个 DLL 都改名为 `.dll.disabled`；恢复时去掉 `.disabled`。推荐使用本软件同时控制。新版早期组件还会检查主插件是否启用；即使其他工具只停用主插件，下次开游戏也不会由早期组件单独上报“选歌中……”。游戏运行中改文件不能热卸载已加载代码，请先退出游戏。

停用后没有本项目代码随游戏加载。辅助程序随所属游戏进程结束而退出。

## 完全移除

关闭游戏后，只移除本项目的两个 DLL（或其 `.disabled` 文件）、`UserData/MuseDashSteamPresence` 整个子目录，以及根目录中这几个专用文件：

```text
MuseDash-Settings.cmd
MuseDash-Disable.cmd
MuseDash-Enable.cmd
MuseDashPresence-README.md
MuseDashPresence-ReadMe.txt
MuseDashPresence-SHA256SUMS.txt
```

子目录中的 `settings.xml` 是你的设置；需要保留时先备份。不要删除整个 `Mods`、`Plugins` 或 `UserData`，其他模组也使用这些目录。
