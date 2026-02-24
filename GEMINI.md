# DebugViewCS 项目指南

## 项目概述
**DebugViewCS** 是一款基于 .NET 10 和 WPF 构建的现代化 Windows `OutputDebugString` (ODS) 日志查看器。它是对经典的 Sysinternals DebugView 的现代化重新实现，保留了底层的共享内存捕获机制，并结合了现代化软件设计理念、高并发消息处理与 Fluent Design 美学。

### 技术栈
*   **运行环境**: `.NET 10.0-windows`
*   **前端 UI**: WPF，采用 `WPF-UI` 实现 Windows 11 Fluent 视觉特效。
*   **架构模式**: 纯净前端 `MVVM`，依托 `CommunityToolkit.Mvvm` 实现。
*   **核心引擎**: `DebugViewCS.Core`（无 UI 依赖），使用高性能异步数据流（`System.Threading.Channels` + `IAsyncEnumerable`）处理高并发数据池。
*   **捕获机制**: 原生 Windows 共享内存协议（`DBWIN_BUFFER`）与事件句柄（`EventWaitHandle`）。

## 目录结构
*   `src/DebugViewCS/`: WPF 前端可视化应用程序项目（MVVM 模式，负责 UI 交互和渲染）。
*   `src/DebugViewCS.Core/`: 低延迟内核日志收集类库（独立于 UI，负责基于内存共享的数据抓取与过滤机制）。
*   `tests/DebugViewCS.Core.Tests/`: （参考）基于 xUnit 与 FluentAssertions 的测试用例项目。

## 构建与运行

### 环境要求
*   操作系统：Windows 10 / 11
*   SDK：.NET 10.0 SDK (或 Desktop Runtime)

### 常用命令
```pwsh
# 构建整个应用解决方案
dotnet build

# 运行 DebugViewCS WPF 客户端
dotnet run --project src/DebugViewCS/DebugViewCS.csproj

# 运行测试 (前提是 tests 目录包含测试用例)
dotnet test
```

### 测试建议
可以使用以下 PowerShell 脚本发送测试用的 `OutputDebugString` 消息，以验证应用程序是否正确捕获：
```pwsh
Add-Type -TypeDefinition @"
using System.Runtime.InteropServices;
public class DebugHelper {
    [DllImport("kernel32.dll")]
    public static extern void OutputDebugStringA(string lpOutputString);
}
"@
1..100 | ForEach-Object { [DebugHelper]::OutputDebugStringA("DebugViewCS Stress Test: Msg $_"); Start-Sleep -Milliseconds 10 }
```

## 开发规约与约定
*   **架构分层**: 严格遵循 UI 展现与核心逻辑解耦。任何底层捕获、过滤、存储机制必须编写在 `DebugViewCS.Core` 中，`DebugViewCS` 仅负责展示和 MVVM 绑定。
*   **MVVM 原则**: UI 交互与业务状态需通过 ViewModel 实现，严禁在 `MainWindow.xaml.cs` 等 Code-Behind 文件中堆砌业务逻辑。
*   **高性能处理**: 对于海量日志的处理，必须优先使用异步流机制（如 `System.Threading.Channels`）实现后台缓冲，并配合 UI 的虚拟化技术（如 `VirtualizingStackPanel`）确保 60FPS 的渲染丝滑度。
*   **暗色美学**: 全局 UI 设计以极简暗色（Dark Theme）为主，新增控件时需复用系统提供的精选配色体系以保持一致的美感体验。