# 发布到 GitHub

这个目录是独立项目；不要把之前包含各种本机调试工具的上层工作区上传。

## 建立仓库

1. 在自己的 GitHub 账号下新建仓库，建议名称 **MuseDashSteamPresence**，描述可用：

   > 免费开源的 Muse Dash Steam 好友曲名状态插件，支持曲名、作者与自定义显示。

2. 上传本目录的源码、文档和 `.github` 配置。不要上传 `artifacts`、`dist`、`.local`、游戏 DLL 或本机日志；`.gitignore` 已设置。
3. 如使用网页上传，记得包含 `.github`、`.gitignore`、`.gitattributes`；不要再让源码套一层 `MuseDashSteamPresence-GitHub` 目录。

也可使用 Git，在本目录执行（URL 替换成自己创建的仓库地址）：

```powershell
git init
git add .
git commit -m "Prepare public beta project"
git branch -M main
git remote add origin 你的仓库地址
git push -u origin main
```

首次提交需要你自己的 Git 用户名与邮箱；本项目不会替你写全局 Git 身份，也不会自动创建仓库或推送。

## 发布玩家下载包

打开仓库 Releases → Draft a new release：

- Tag：`v0.5.1-beta.1`
- Title：`0.5.1-beta.1 — 解压即用的 Steam 状态插件`
- 勾选 **Set as a pre-release**。
- 附件上传本地 `dist` 下的 `MuseDashSteamPresence-0.5.1-beta.1-win-x64.zip` 和对应 `.zip.sha256`。
- 同一构建可另附 `scripts/package-hub.ps1` 导出的 `MuseDashSteamPresence.dll`；Hub 平台上传的是该 DLL，不是 ZIP。字段与审核说明见 [Hub 指南](EUTERPE-HUB.md)。
- 发布说明可复制下方内容；源码仓库与发行包是两份不同用途的内容。

## 可复制的发布说明

免费开源的 Muse Dash Steam 好友状态插件，作者：樊小胃。可自定义非打歌状态、独立开关曲名／作者、选择分隔符。

**先判断前置：** 已为 Muse Dash 安装 MelonLoader 0.7.3 net6 的玩家直接解压；没有或不确定的玩家先按官方步骤安装加载器。仅下载过安装器不算安装完成。

**安装：** 关闭游戏和设置窗口，将 `win-x64.zip` 中的全部内容解压到 `musedash.exe` 同级；先双击 `MuseDash-Settings.cmd` 调整并保存，再从 Steam 启动游戏。设置软件可独立运行，不需要先开游戏；游戏运行中也可修改，保存后约 0.5–1 秒更新。已有 MelonLoader net6 环境是前置，包内不捆绑加载器。不要下载 Source code 包安装。

没有 MelonLoader 的玩家：从 [官方安装器下载页](https://github.com/LavaGang/MelonLoader.Installer/releases/latest) 下载 Windows 的 `MelonLoader.Installer.exe`，选中 Muse Dash，选择加载器版本 0.7.3 并安装。启动游戏完成首次初始化并退出后，再解压本插件。不要求安装其他模组。

如果本来就使用 Euterpe，也可在 Euterpe 中为 Muse Dash 完成首次环境配置（选择游戏目录、玩家身份并执行安装）；该流程会下载安装 MelonLoader 等基础组件，单纯下载 Euterpe 不算安装。之后本设置程序独立运行。

从 Hub 下载本插件时只需点击下载并正常启动游戏，单 DLL 会从内部准备配套软件，不需要额外下载或手工补文件。首次运行后打开根目录 MuseDash-Settings.cmd，即可独立修改全部自定义显示选项。

**兼容限制：** 当前仅适配文档列出的 Steam DLL 指纹，不匹配会停止发送；完整公开版游戏生命周期实机回归尚待完成。此版本为实验性预发布。

**长文字限制：** 同时显示曲名和作者可能超过文本长度或界面限制，造成信息显示不全。目前尚无保证完整显示的解决方案。

**不会因切歌反复弹窗：** 当前已验证客户端下，选歌、开曲和返回选歌只更新状态，不会反复触发“开始玩游戏”的新弹窗，不用担心打扰好朋友们～首次启动仍可能正常通知，未来客户端行为变化需重新验证。自己／好友视角的实际显示截图见项目首页。

**独立操作：** 无需其他模组管理器，设置窗口可在游戏未启动时修改和保存，并在退出游戏后独立启用／停用两个组件。随包有简短的纯文本说明。

建议保持最新正式版 Steam；2026-11-01 已核对构建 1792618285，当日更新检查未发现新版本。未来版本仍需通过兼容检测。

项目采用 MIT，免费开源，欢迎提交 Issue、PR 和改进。不需要好友安装，不额外登录 Steam，不修改游戏或其他模组原文件。
