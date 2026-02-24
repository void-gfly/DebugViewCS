# DebugViewCS 

[![Version](https://img.shields.io/badge/Version-v1.0.1-blue.svg)]()
[![.NET](https://img.shields.io/badge/.NET-10.0-purple.svg)]()
[![WPF](https://img.shields.io/badge/WPF-Fluent%20Design-brightgreen.svg)]()

**DebugViewCS** 是一款基于 .NET 10 和 WPF 构建的现代化 Windows `OutputDebugString` (ODS) 日志查看器。本质上，它是对经典的 Sysinternals DebugView 的现代化重新实现。在完全保留强大的底层共享内存捕获机制的基础上，结合现代化软件设计理念、高并发消息处理与 Fluent Design 美学，带给开发者极佳的桌面端日志调试体验。

---

## ✨ 核心特性 

* **🚀 原生高性能 ODS 捕获**：基于 Windows 共享内存协议 (`DBWIN_BUFFER`) 与事件句柄 (`EventWaitHandle`)，无缝原生捕获全局或进程级的 `OutputDebugString` 输出。
* **🎨 纯粹的暗色美学**：全局深度定制的 Dark Theme。精选配色体系覆盖各级控件（下拉框、列表、滚动条），为你提供舒适的极简暗色视觉。
* **⚡ 极致虚拟化渲染**：专门为海量日志输出设计。依托 `VirtualizingStackPanel` 和后台异步缓冲刷新，在每秒数千条日志的冲击下依然保持 60 FPS 的如丝般顺滑滚动。
* **🔍 强大的过滤引擎 (Filter Engine)**：
  * 支持**普通文本**与**正则表达式 (Regex)** 匹配。
  * 顶部工具栏支持一键开启/关闭过滤模式状态。
  * 右键一键将目标 `Process Name` 加入至过滤列表。
* **🌈 智能高亮识别**：
  * **按进程分配颜色**：自动引入基于 HSL 色环算法，为不同进程智能分配不同的前景色，避免颜色冲突。
  * **自定义规则上色**：支持根据字符串关键字（或头尾空格匹配），自定义配置 Error / Warning / Info 等特殊级别的高亮底色。
* **🛠️ 高级日志管理体验**：
  * **操作扩展菜单**：单行日志支持右键菜单：“复制完整日志(包含PID等)”、“复制消息文本”、“将进程名增加到过滤器”。
  * **自动清理机制**：支持自定义设定清除策略。如 “Keep max 5000/10000 entries”，达到上限后自动淘汰最老数据，全天候挂机绝不溢出。
  * **智能悬浮窗**：超长日志被折叠截断时，提供智能计算尺寸的 Tooltip。
  * **状态监控追踪**：底部状态栏实时显示当前捕获总数、显示行数、程序内存占用，以及**最后一行捕获的日志正文**。

---

## 📦 技术栈 

* **运行环境 / Framework**: `.NET 10.0-windows`
* **架构模式 / Architecture**: 纯净前端 `MVVM`，核心功能全部封装在独立的 `DebugViewCS.Core`
* **底层库推荐**: 使用 `CommunityToolkit.Mvvm` 及高性能异步数据流 (`System.Threading.Channels` + `IAsyncEnumerable`) 处理高并发数据池。
* **UI 层**: 接入微软官方风格 `WPF-UI` 支持，实现 Windows 11 发光粒子及 Fluent 视觉特效。

---

## 🚀 快速走起

### 环境要求
* Windows 10 / 11 操作系统。
* 安装 [.NET 10.0 Desktop Runtime](https://dotnet.microsoft.com/download/dotnet/10.0)。

### 编译与运行
在项目根目录打开终端运行以下命令：

```pwsh
# 构建应用
dotnet build

# 运行 DebugViewCS WPF 客户端
dotnet run --project src/DebugViewCS/DebugViewCS.csproj
```

### 发送单条测试日志 (PowerShell)
可以利用下面这个脚本给当前的 DebugViewCS 灌入测试日志以验证功能：
```pwsh
Add-Type -TypeDefinition @"
using System.Runtime.InteropServices;
public class DebugHelper {
    [DllImport("kernel32.dll")]
    public static extern void OutputDebugStringA(string lpOutputString);
}
"@
# 发送 100 条循环测试文本
1..100 | ForEach-Object { [DebugHelper]::OutputDebugStringA("DebugViewCS Stress Test: Msg $_"); Start-Sleep -Milliseconds 10 }
```

---

## 🏗️ 解决方案内部结构 

```text
e:\dot_net\DebugViewCS\
├── src\
│   ├── DebugViewCS\             # WPF 前台可视化主机 (MVVM 模式, 提供 UI 互动)
│   └── DebugViewCS.Core\        # 低延迟内核收集器 (无 UI 依赖、基于内存共享的数据引擎)
└── tests\
    └── DebugViewCS.Core.Tests\  # 依托 xUnit 与 FluentAssertions 的可靠测试用例保障
```

## 📜 许可证 (License)

本项目基于 [MIT License](LICENSE) 许可协议开源。
