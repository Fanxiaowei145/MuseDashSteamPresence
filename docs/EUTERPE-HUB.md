# Euterpe Hub 发布资料

当前提供 **0.5.1-beta.1** 单 DLL 预发布构建，主 DLL 的 `MelonInfo.Version` 为 **0.5.1**。它已内嵌配套文件；不要求 Hub 增加 ZIP 安装功能，不新增 MuseDashMirror 依赖。**准备好文件不等于 Hub 已审核通过。**

## 创建模组页

| 字段 | 填写内容 |
| --- | --- |
| 模组名称 | `Muse Dash Steam Song Presence`，必须与 MelonInfo.Name 完全一致，区分大小写 |
| 开发者名称 | `樊小胃` |
| 文件名 | `MuseDashSteamPresence.dll`，请明确填写，以匹配早期组件和独立启停的路径 |
| GitHub 仓库 | 公开源码后填写实际 `owner/repo`，目前没有远端地址时留空，不填示例地址 |
| 配置文件 | 留空，使用本项目独立设置窗口，不把嵌套 XML 当成 UserData 根目录文件 |
| 游戏版本 | `6.7.0`，当前测试环境；尚未验证所有版本，不填 `*` |
| MelonLoader 版本 | `0.7.3`，不要沿用表单示例 0.6.1 |
| 模组依赖 | 留空，本项目不需要其他 Hub 模组 |
| 库依赖 | 留空，不把本项目辅助 EXE、Bootstrap 或 MelonLoader 自带组件冒充 Hub 库依赖 |
| 不兼容模组 | 目前无确认名单，留空；不要把未验证的环境填成已知冲突 |

上传发布时选择 `artifacts/bin/MuseDashSteamPresence.dll`，不要传 ZIP、Bootstrap DLL、设置 EXE 或构建探针。文件旁的 SHA256 可核对具体构建。版本字段按实际 DLL 元数据填写 `0.5.1`，在描述和更新日志中明确“测试版”。

## 可复制的中文描述

免费开源的 Muse Dash Steam 好友状态插件，作者：樊小胃。保留正版 Muse Dash AppID 774171，显示当前曲名与歌曲作者；可自定义非打歌状态，独立开关曲名／作者并选择分隔符。

新装默认：不打歌只显示 Muse Dash，打歌只显示曲名，不显示作者。非打歌自定义文字与作者均可在独立设置窗口自行开启，升级保留已有设置。

安装后照常启动一次游戏，插件会从 DLL 内部准备自己的配套文件；不联网下载组件，不需要另装 EXE 或解压第二个包。之后双击游戏根目录 MuseDash-Settings.cmd 即可独立配置，不要求 Euterpe 客户端保持打开。默认状态可直接用，全部自定义功能保留。

当前已验证 Steam 客户端下，选歌／开曲／返回选歌只更新状态，不会反复触发“开始玩游戏”通知，不用担心打扰好朋友们～首次启动仍可能正常通知，未来客户端行为变化需重新验证。

已知限制：曲名与作者过长可能显示不全，目前没有保证完整显示的方案。建议最新正式版 Steam，但仅已核对的文件指纹可发送状态；不匹配会停止功能。当前为测试版，完整新构建游戏流程仍待实机回归。

## 给审核员的说明

- 参考 MuseDashMirror 的单 DLL 分发与 MelonInfo 元数据形式；没有复制其代码，也未添加该库依赖。它公开的 GitHub Release 有独立 DLL 附件，但无法据此证明其私有 Hub 提交流程。
- 主 DLL 有一个 `MuseDashSteamPresence.Payload.zip` 资源，包含自己的控制器、独立设置程序、早期插件、启停脚本和说明。清单列出全部路径及 SHA256。通过 PE 元数据读取资源，不在早期加载阶段提前加载主 DLL 的游戏类型。
- 只写 `UserData/MuseDashSteamPresence`、`Plugins/MuseDashSteamPresence.Bootstrap.dll` 及本项目专用根目录入口。拒绝越界路径、额外可执行文件、个人 settings.xml、state 和备份目录覆盖；校验全部内容后才写文件。已有配套文件哈希相同则不重写。
- 升级保留备份，失败回滚；只有本项目旧控制器身份（PID、启动时间、完整路径）一致时才通过自己的 stop 文件请求优雅退出，不强杀、不操纵 Steam 进程。设置程序被占用时提示关闭后重开游戏。
- 正式插件读取游戏／已加载自制曲元数据，通过本机文件 IPC 交给自己的外部控制器。控制器使用私有 Steam 客户端通信，保留 DLL 指纹和接口地址校验；不额外登录、不调快捷方式 setter、不请求自身好友资料刷新。
- 不改游戏、Steam 或其他模组的原文件；不改存档，不做数据分析上报。设置窗口“获取 MelonLoader”只有用户点击后才打开官方 GitHub 下载页。
- 主 DLL 被 Hub 停用或卸载后，下次启动时早期组件检查不到启用的主 DLL，不启动控制器。自己的配套目录和用户设置保留，可按移除说明清理；运行中不能热卸载已加载代码。
- 首次 Hub 安装的设置程序由首次启动生成；当次可按默认／已有设置显示。新写入早期插件从下一次启动起更早加载，首次状态仍会在主 DLL 注册时提交。

审核重点应包含私有 Steam 通信的使用范围、实际稳定性和本地配套文件部署。提交时应公开对应源码与二进制校验值，由平台人工审核判断，不能保证通过。

## 依据与操作顺序

1. [Hub 官方模组作者指南](https://euterpe-org.com/zh-CN/docs/ecosystem/modder)：先建元数据页，再上传单个 DLL、版本号与更新日志，人工审核通过后可见。
2. [客户端单文件模组下载实现](https://github.com/Euterpe-org/App/blob/main/src/Euterpe.Core/Services/DownloadManager/GameDownloadManager.cs)：下载文件到 Mods，不负责安装我们的多文件 ZIP。
3. [MuseDashMirror AssemblyInfo](https://github.com/MDMods/MuseDashMirror/blob/main/src/MuseDashMirror/Properties/AssemblyInfo.cs) 和 [GitHub Releases](https://github.com/MDMods/MuseDashMirror/releases)：参考单 DLL 元数据与附件方式。
4. [Euterpe 的 Muse Dash 初始化选项](https://github.com/Euterpe-org/App/blob/main/src/Euterpe.Models/Games/MuseDashConfig.cs)：MelonLoader 默认选中且必选，完成 Euterpe 首次环境配置会安装，单纯下载程序不会。

先完成对应源码公开与本地 Hub 模式回归，再填写模组页和上传测试版。不要上传之前没有嵌入配套资源的 0.4.x 主 DLL。
