# MuseDashSteamPresence

[简体中文](README.md) | [English](docs/README.en.md)

让 Steam 好友看到你正在 Muse Dash（喵斯快跑）里演奏的歌曲，附带中文设置窗口。

Free and open-source Muse Dash Steam status mod with song titles, optional artist information, customizable idle text, and a standalone settings app. Friends do not need to install anything. [English installation and usage guide](docs/README.en.md).

作者：**樊小胃**。

```text
不打歌时  Muse Dash
打歌时    Muse Dash : ものふぉびあ
```

**免费开源 · Windows x64 · 独立 MelonLoader 插件 · MIT · 实验性预发布**

> **Steam 要求：** 建议保持最新正式版。2026-11-01 已核对的客户端构建为 **1792618285**，当日本机更新检查显示无新版本，且与已验证 DLL 指纹匹配。以后“最新版”可能变化，仍以设置软件的兼容检测为准；不匹配时会停止发送，不要删除校验。[兼容范围](docs/COMPATIBILITY.md)

## 实际显示效果

以下为作者于 2026-11-01 提供的实际使用截图，分别展示自己和好友视角。

这些截图展示了手动开启非打歌文字与作者后的可选效果。**新装默认关闭这两项：不打歌只显示 Muse Dash，打歌只显示曲名。** 需要时可在设置里重新开启。

| 状态 | 自己视角 | 好友视角 |
| --- | --- | --- |
| 未打歌／选歌中 | ![自己视角：选歌中](docs/images/self-selecting.png) | ![好友视角：选歌中](docs/images/friend-selecting.png) |
| 正在演奏 | ![自己视角：正在演奏](docs/images/self-playing.png) | ![好友视角：正在演奏](docs/images/friend-playing.png) |

**当前已验证客户端下，选歌、开曲和返回选歌只会更新显示状态，不会反复触发“开始玩游戏”的新弹窗，不用担心频繁切换状态打扰到好朋友们～** 首次启动游戏仍可能按 Steam 自身设置正常通知；未来客户端行为变化需重新验证。

截图用于展示实际显示效果，不替代本次公开构建尚待完成的完整生命周期回归。

## 第一步：是否已安装 MelonLoader？

| 你的情况 | 应该怎么做 |
| --- | --- |
| 已在 Muse Dash 安装 MelonLoader 0.7.3 net6，并能正常进入游戏 | 直接进入下方“下载安装” |
| 没装过／不确定／只下载过安装器 | 先按下方“没装过 MelonLoader”安装，再下载本插件 |

不需要安装或打开其他模组管理器。仅下载过安装器或某个模组，不等于已安装加载器。

## 从 Euterpe Hub 一键安装

Hub 发布完成后，在客户端找到 **Muse Dash Steam Song Presence**，点击下载，照常启动游戏即可。Hub 只安装一个主 DLL；主 DLL 已内嵌自己的控制器、独立设置软件、早期组件和说明，首次加载时从本地释放，不联网补下载，不需要再解压另一个包。

第一次运行后，双击游戏根目录 **`MuseDash-Settings.cmd`**，或打开 `UserData/MuseDashSteamPresence/MuseDashPresenceSettings.exe`，即可独立修改全部显示选项；之后不需要打开 Euterpe 或先启动游戏。若不改设置，不打歌时只显示游戏名，打歌时只显示曲名。

Hub 首次安装和 GitHub 完整 ZIP 有一点区别：仅有主 DLL 时，需要先照常启动一次游戏来生成设置程序，首次启动使用默认或已有配置；GitHub ZIP 已有全部文件，可直接先设置再开游戏。新释放的早期组件从下次启动加载，首次仍会在主 DLL 注册时请求状态，不能保证和后续启动一样早。

本仓库提供可上传的单 DLL，但**尚未向 Hub 提交或通过人工审核**。[Hub 发布资料与审核说明](docs/EUTERPE-HUB.md)

## 下载安装：解压到游戏根目录

1. 在本仓库 **Releases** 下载 `MuseDashSteamPresence-版本号-win-x64.zip`。不要下载 GitHub 自动生成的 **Source code** 压缩包。
2. **关闭 Muse Dash 和状态设置窗口**。Steam 库右键 Muse Dash → 管理 → 浏览本地文件。
3. 将压缩包里的**全部文件和文件夹**解压／合并到刚打开的目录，与 `musedash.exe` 放在同一级。不要再套一层压缩包同名文件夹。
4. **先设置：** 双击游戏根目录的 **`MuseDash-Settings.cmd`**，按喜好调整，点击“保存并应用”。此时不需要启动游戏。
5. **再玩游戏：** 正常从 Steam 启动 Muse Dash，插件会读取已保存的设置。设置窗口可以提前关闭。

无需运行安装器、登录额外账号、手动移动每个 DLL，好友也不需要安装。

**唯一的模组加载前置是 MelonLoader net6，不要求安装其他模组。** 本项目已验证的加载器版本为 **0.7.3**，发行包不捆绑或覆盖加载器。

### 没装过 MelonLoader？先做这一步

1. 关闭游戏，从 [MelonLoader 官方下载页](https://github.com/LavaGang/MelonLoader.Installer/releases/latest) 下载 Windows 文件 **`MelonLoader.Installer.exe`**。
2. 打开安装器，选中 **Muse Dash**；如果没有列出，点 **Add Game Manually**，手动选择游戏根目录里的 `musedash.exe`。
3. 在加载器版本选项中选择 **0.7.3**，点击 **Install**。安装器自身的版本号与加载器版本号不同，不要混淆。
4. 从 Steam 启动游戏一次，等待加载器完成初始化；能正常进入游戏后退出，再按上方步骤解压本插件。这次启动只用于首次初始化加载器，不是修改设置的要求。

已经能正常使用 MelonLoader 的玩家跳过这一步即可。官方操作说明见 [MelonLoader 安装器文档](https://github.com/LavaGang/MelonLoader.Installer#melonloader-installation)。不要根据是否安装了某个其他模组来推断加载器是否已就绪。

如果也希望使用谱面与模组管理，可选 [Euterpe 官方下载](https://github.com/Euterpe-org/App/releases/latest)：在 Euterpe 首次环境配置中选择 Muse Dash 并完成安装步骤时，MelonLoader 是默认选中且必选的安装步骤，会自动下载并安装。**只下载／打开 Euterpe 不等于完成安装。** 向导还会准备运行库、基础模组并处理自身的冲突项，使用前需满足 Euterpe 自己的游戏／DLC 条件；仅需要本插件时可继续用上面的官方加载器安装器。完成后，设置软件仍独立运行。

解压后目录应当是：

```text
Muse Dash/
├─ musedash.exe                          ← 原有游戏文件
├─ MuseDash-Settings.cmd                 ← 双击设置
├─ MuseDash-Disable.cmd                  ← 关闭游戏后停用
├─ MuseDash-Enable.cmd                   ← 关闭游戏后恢复
├─ MuseDashPresence-README.md
├─ MuseDashPresence-ReadMe.txt            ← 简短纯文本说明
├─ Mods/
│  └─ MuseDashSteamPresence.dll
├─ Plugins/
│  └─ MuseDashSteamPresence.Bootstrap.dll
└─ UserData/
   └─ MuseDashSteamPresence/
      ├─ PresenceController.exe
      ├─ MuseDashPresenceSettings.exe
      └─ …                               ← 配套说明和停用脚本
```

详细步骤、更新与手动停用见 [安装说明](docs/INSTALL.md)。

## 使用设置

| 设置 | 效果 |
| --- | --- |
| 显示自定义状态 | 默认关闭，不打歌时只显示 `Muse Dash`；开启后可自定义文字，例如“选歌中……” |
| 显示歌曲名 | 独立控制曲名；关闭后仍可单独显示作者 |
| 显示歌曲作者 | 默认关闭；可独立开启歌曲艺人／作者，不是谱面作者 |
| 曲名作者分隔方式 | 无、空格、`_` 或自定义，只有两项都显示且都有内容时才添加 |
| 保存并应用 | 未开游戏时仅保存，下次启动读取；游戏运行时约 0.5–1 秒更新。预览／输入过程中不会向 Steam 提交 |

菜单、选歌、加载、结算和暂停均属于“不打歌”。真正开始／恢复演奏才显示曲目信息。两项都关闭时只显示 `Muse Dash`。关闭设置窗口后继续生效，无需常驻设置软件。

**设置软件可独立运行，不要求游戏或 Steam 正在运行。** 推荐先设置、再开游戏；游戏运行中也可以修改并保存，不必重开游戏。

窗口内的“启用插件／停用插件”可独立控制本项目两个组件，需先退出游戏，不依赖其他管理器。根目录的启用／停用入口仍可使用。关闭设置窗口只关闭编辑窗口，不等于停用插件。

环境提示会区分：目录不正确、缺少文件、缺少加载器、未验证的加载器版本、插件已停用、等待游戏，以及状态提交／缓存确认。“文件检查通过”不等于好友已经看到状态。没有加载器或 Steam 版本未适配时仍可预先保存设置。

设置保存在 `UserData/MuseDashSteamPresence/settings.xml`，首次保存生成。最终文字会按 UTF-8 字节数安全截短；Steam 界面本身也可能裁切长文字。完整说明见 [使用与排错](docs/USAGE.md)。

新默认值适用于首次安装、没有配置文件或点击“恢复默认”再保存；升级会保留已有设置，不会强制关闭其他用户已开启的文字／作者选项。

### 已知限制：曲名和作者可能显示不全

**同时显示歌曲名和作者时，较长内容可能超过 Steam 的文本长度或界面显示限制，导致信息显示不全。目前尚无保证完整显示的解决方案。** 插件只会做安全截短，不会绕过该限制。可以按需关闭作者、只显示曲名，或缩短分隔符，以减少被截短的情况。

**本项目免费开源，欢迎大家提交 Issue、Pull Request，或在遵守 MIT 许可证的前提下修改和改进。** 如果你找到更好的长文本显示方式，也欢迎一起完善。

## 功能与边界

- 保留正版 Muse Dash 的 **AppID 774171**，不建立非 Steam 游戏，不修改库里的游戏名称。
- 独立的 MelonLoader 插件，本体曲目直接读取游戏数据库；不要求安装其他模组。
- 同一个会话中更新游戏显示文字，相同最终文字不重复提交，无三分钟限制，也没有延迟补发歌曲队列。
- 已验证客户端的通知逻辑测试和好友反馈表明：同 AppID 切换选歌／曲名状态不会反复触发“开始玩游戏”弹窗，不用担心切歌打扰好友。首次启动仍可能正常通知，未来客户端需重新验证。
- 不是游戏名下面的第二行富状态；本项目采用游戏名称文本路径，不依赖官方 Rich Presence 本地化模板。
- 早期加载插件会尽早提交非打歌状态，仍需等待加载器和 Steam 同步，无法保证游戏进程出现的同一瞬间更新。
- 当前代码经过离线测试，早期版本曾完成实际曲名显示测试。**本次公开预发布的完整启动、曲终、暂停流程尚待实机回归，不应标为稳定版。**

## 开发与贡献

[构建和测试](docs/BUILD.md) · [发布到 GitHub](docs/PUBLISH.md) · [实现与隐私](docs/ARCHITECTURE.md) · [贡献说明](CONTRIBUTING.md) · [English quick start](docs/README.en.md)

```powershell
# Windows + PowerShell 7，执行不连接 Steam 的测试
pwsh -File scripts/build.ps1 -TestsOnly

# 完整构建需要本机的游戏和加载器引用，不会修改游戏安装
pwsh -File scripts/build.ps1 -GamePath "游戏目录"
pwsh -File scripts/package.ps1
```

本仓库不包含游戏、Steam、加载器或其他模组的二进制文件。完整游戏插件不能在缺少本地游戏引用的普通 GitHub 云端环境直接构建；CI 只执行离线测试和辅助程序编译。

## 许可证与归属

本项目原创代码采用 [MIT](LICENSE)，第三方产品与依赖不因此重新授权，见 [NOTICE](NOTICE.md)。本项目与游戏、Steam 或加载器的开发团队无官方隶属关系。

已有 Muse Dash Discord 状态项目及其他 Steam 富状态尝试，本项目不宣称“全球首创”。
