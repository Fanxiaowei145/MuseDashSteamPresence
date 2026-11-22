# Conservative automatic compatibility detection

The 0.5.3 candidate first checks the two audited whole-file hashes. An unknown
file can reuse the November adapter only when its normalized file digest matches
the audited November DLL. This is not fuzzy matching or a general ABI detector.

The normalizer checks an x64 PE32+ layout and retains every byte except:

- COFF timestamp;
- PE checksum;
- security-directory entry and its structurally valid, trailing certificate table.

Code, constants, imports, exports, relocations, resources, section headers, layout,
and all remaining bytes must match. A changed implementation or rebuilt binary
still stops status publication. No unknown native function is called to test it.
The file is held read-only, denying writes/deletion during identification and
native initialization. Existing runtime interface-address checks remain active.

This can tolerate signing/timestamp-only changes. It does **not** promise that a
normal Steam code update is compatible, or eliminate future adapter maintenance.

## 验证范围

- 内存中改变真实 DLL 的时间戳：整文件指纹不同，等价识别通过。
- 内存中改变真实 DLL 的代码：拒绝；没有加载或执行修改后的文件。
- 合成 PE 全字节变异、证书边界、截断及畸形头部测试通过。
- 状态控制器、设置、部署等原有离线回归通过。
- 2026-11-23：本机日志确认选歌文字及曲名，用户确认本机可见且好友与聊天保持连接。本次没有好友端重新确认，也没有完成返回选歌等全部流程的实机回归。

当前只对已取得的 November DLL 建立等价指纹。旧版仍支持精确指纹，
但没有未经核对就扩大旧版的自动识别范围。
