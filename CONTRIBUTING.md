# 欢迎贡献

本项目免费开源，作者为樊小胃，欢迎 Issue、PR 和改进。贡献的原创代码应能按本项目 MIT 许可证分发。

开始前请阅读 [架构](docs/ARCHITECTURE.md)、[构建](docs/BUILD.md) 和 [兼容范围](docs/COMPATIBILITY.md)。不要提交游戏、Steam、Euterpe 或加载器二进制文件、实际账号信息、运行日志、转储和个人配置。

修改后至少运行 `scripts/build.ps1 -TestsOnly`。涉及打包时再完整构建、打包并运行 `tests/Test-Release.ps1`。CI 不连接 Steam，不能替代实际好友视角与完整游戏流程的验证。

涉及 Steam 适配时，**不要只更改哈希或猜测接口槽位**。需要核对目标构建 ABI、失败路径、回调消费、清理行为和实际消息效果，保留旧适配器的拒绝逻辑。描述验证环境和未验证部分，不以 `SetRichPresence` 返回成功替代好友可见证据。

欢迎改进长曲名／作者显示；当前没有保证完整显示的办法。PR 说明请写清楚问题、最终行为与必要验证，保持改动聚焦。
