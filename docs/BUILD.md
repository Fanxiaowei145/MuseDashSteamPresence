# 构建与离线测试

## 环境

- Windows x64、.NET Framework（推荐 4.8）。
- PowerShell **7.2 或更高**，使用 `pwsh`，不是 Windows 自带的 PowerShell 5.1。
- 完整构建需要 .NET SDK **8 或更高**提供 Roslyn；测试和两个辅助程序不需要 SDK。
- 本机已安装 Muse Dash、MelonLoader net6，且启动过游戏以生成 `MelonLoader/Il2CppAssemblies`。
- .NET 6 运行时引用来自游戏的 `dotnet/shared/Microsoft.NETCore.App/6.0.*`，或系统安装目录；也可显式指定。

官方工具：[PowerShell](https://github.com/PowerShell/PowerShell#-powershell) · [.NET SDK](https://dotnet.microsoft.com/download) · [MelonLoader](https://github.com/LavaGang/MelonLoader)。

## 只跑离线检查

在仓库根目录：

```powershell
pwsh -File scripts/build.ps1 -TestsOnly
```

输出在 `artifacts/tests` 和 `artifacts/bin`。执行：

- 状态规则与去重；
- 原始 AppID 消息的固定字节样例、真实 PID 要求、UTF-8 限制；
- 自定义状态、独立曲名／作者开关、各分隔符和设置 XML 读写；
- 独立安装检查与双组件控制：缺少加载器、只下载过其他程序、未验证加载器、缺失文件、外部工具只停用主插件，以及冲突前置检查；
- 真实设置窗口的隔离环境测试：配置可在游戏未启动时保存，不误报环境就绪，预览采用指定本体曲目；
- **真实控制器循环 + 模拟传输**：启动补正、长曲名未完整确认、状态切换、失败后恢复、实时设置、两项关闭时的去重、损坏配置恢复。

测试不加载 Steam DLL，不提交真实状态，不需要启动 Steam。生产版 Steam 适配器只参与编译，不执行。

## 完整构建

```powershell
pwsh -File scripts/build.ps1 -GamePath "D:\Games\SteamLibrary\steamapps\common\Muse Dash"
```

`-GamePath` 可省略，此时从 Steam 注册表和库文件自动查找。示例路径只是示意，不是作者电脑要求。

可选显式引用：

```powershell
pwsh -File scripts/build.ps1 `
  -GamePath "游戏安装目录" `
  -CompilerDirectory "SDK目录\Roslyn\bincore" `
  -RuntimeDirectory "NET6运行时目录"
```

构建输出：

```text
artifacts/bin/MuseDashSteamPresence.dll
artifacts/bin/MuseDashSteamPresence.Bootstrap.dll
artifacts/bin/PresenceController.exe
artifacts/bin/MuseDashPresenceSettings.exe
```

不会复制到游戏安装中，不会运行控制器或改 Steam 状态。游戏／加载器引用仅供本机编译，不进入仓库和发行包。没有对 Euterpe DLL 的编译引用。

主 DLL 同时是 Hub 单 DLL 安装文件。构建顺序为辅助程序 → 早期组件 → 本地资源 ZIP → 内嵌资源的主 DLL。完整构建还会用纯 .NET 进程读取实际 DLL 的 PE 资源，在隔离目录模拟 Hub 只安装主 DLL 的情况，并用无 Steam 功能的旧控制器桩检查优雅更新；不会执行真实游戏、生产控制器或 Steam API。

### 导出 Hub 上传文件

```powershell
pwsh -File scripts/package-hub.ps1
```

生成的 `MuseDashSteamPresence.dll` 就是 Hub 发布上传的单个文件，旁边有校验值、`hub-upload-fields.json` 和审核说明。脚本实际读取 MelonInfo 元数据和资源表，拒绝导出没有配套资源的旧 DLL。平台版本填 DLL 的 `0.5.1`，描述中注明它属于 `0.5.1-beta.1` 测试发行，不冒充稳定版。

首次从 Hub 安装后照常启动游戏，会准备设置窗口；下次起可提前独立配置。GitHub ZIP 仍可直接先设置后启动。无需 Hub 平台改变安装方式。

## 生成可直接解压的发行包

```powershell
pwsh -File scripts/package.ps1
pwsh -File tests/Test-Release.ps1 -ZipPath "dist/MuseDashSteamPresence-0.5.1-beta.1-win-x64.zip"
```

ZIP 顶层直接是 `Mods`、`Plugins`、`UserData` 等，无额外套层。包内有 SHA256 清单，ZIP 旁有整个文件的 `.sha256`；校验值用于核对下载完整性，不等同于签名或安全审计。

打包脚本拒绝覆盖同名产物。需要重做时使用 `-OutputDirectory "新的输出目录"`，保留旧包以便比较。

## CI 的范围

GitHub Actions 的 Windows 任务执行离线测试和辅助程序编译。GitHub 云端没有合法安装的游戏与生成的游戏引用，因此**不构建两个游戏插件，也不自动发布 Release**。不要上传游戏 DLL 以凑齐 CI。
