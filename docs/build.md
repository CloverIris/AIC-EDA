# 构建与开发指南

本文档说明如何在本地构建、运行和开发 AIC-EDA。

---

## 环境要求

### 必需

| 组件 | 最低版本 | 说明 |
|------|---------|------|
| Windows | 10 2004 (Build 19041) | WinUI 3 的最低系统要求 |
| .NET SDK | 8.0 | 项目目标框架 |
| Windows App SDK | 1.8.260317003 | 已作为 NuGet 包引用，无需单独安装 Runtime |

### 推荐开发环境

- **Visual Studio 2022** 17.9+，安装以下工作负载：
  - `.NET 桌面开发`
  - `通用 Windows 平台开发`
  - `Windows 应用 SDK C# 模板`（通过 VS Installer 的"单个组件"安装）

- 或 **VS Code** + C# Dev Kit + .NET 8 SDK

---

## 构建命令

### Debug（开发）

```powershell
cd AIC-EDA
dotnet build -c Debug -p:Platform=x64
```

### Release（发布）

```powershell
cd AIC-EDA
dotnet build -c Release -p:Platform=x64
```

### 运行

```powershell
cd AIC-EDA
dotnet run -c Debug -p:Platform=x64
```

### 打包 MSIX

```powershell
cd AIC-EDA
dotnet publish -c Release -p:Platform=x64 -p:SelfContained=true -r win-x64
```

---

## 平台注意事项

### ❌ 不支持 AnyCPU

WinUI 3 打包应用必须使用特定平台架构。项目已配置：

```xml
<Platforms>x86;x64;ARM64</Platforms>
```

使用 AnyCPU 将导致 MSIX 打包失败。

### ✅ 推荐 x64

x64 是开发和发布的首选平台。

---

## 已知构建问题

### 问题 1: XamlCompiler.exe 崩溃

**症状**
```
XamlCompiler.exe 返回 exit code 1，不生成 output.json
```

**根因**
Windows App SDK 1.8 的 XamlCompiler 在特定条件下崩溃（[GitHub Issue #10947](https://github.com/microsoft/WindowsAppSDK/issues/10947)）。

**解决方案**
1. **首选**: 在 Visual Studio 中按 F5 或 Ctrl+Shift+B 构建（而非命令行 `dotnet build`）
2. **降级 WinAppSDK**: 将 `.csproj` 中的版本改为 `1.6.240923002` 或 `1.7.x`
3. **更新 VS**: 确保 Visual Studio 2022 为 17.9+ 版本

### 问题 2: PRI/XBF 扁平化崩溃

**症状**
```
PRI175: 0x80073b0f - Processing Resources failed with error: 重复资源名称
```

**根因**
当 `Views/` 子文件夹中存在同名 XAML 文件时，MSBuild 的 `GenerateProjectPriFile` 目标会因资源名称冲突而崩溃（[WindowsAppSDK Issue #6299](https://github.com/microsoft/WindowsAppSDK/issues/6299)）。

**解决方案**
项目已内置 workaround：

```xml
<ItemGroup>
  <Page Update="Views\**\*.xaml">
    <Link>%(Filename)%(Extension)</Link>
  </Page>
</ItemGroup>
```

此配置将子文件夹中的 XAML 文件在构建时扁平化到根命名空间，避免名称冲突。**注意**：所有 XAML 文件名在项目中必须全局唯一。

### 问题 3: MVVMTK0045 警告

**症状**
构建时出现大量如下警告：
```
warning MVVMTK0045: The field ... using [ObservableProperty] will generate code 
that is not AOT compatible in WinRT scenarios
```

**说明**
这是 CommunityToolkit.Mvvm 8.4.0 的已知行为。在 WinUI 3 中，这些警告**不会导致运行时问题**，因为当前项目未启用 Native AOT 编译。

如需消除警告，可将字段改为部分属性（C# 12 特性）：
```csharp
// 旧写法（产生警告）
[ObservableProperty] private string _name;

// 新写法（无警告）
[ObservableProperty] public partial string Name { get; set; }
```

---

## 开发工作流

### 添加新页面

1. 在 `Views/` 目录下创建 `.xaml` 和 `.xaml.cs` 文件
2. **确保文件名全局唯一**（即使放在子文件夹中）
3. 在 `MainWindow.xaml.cs` 的 `NavigateToPage` 方法中添加路由
4. 在 `NavigationView.MenuItems` 中添加导航项

### 添加新配方

1. 编辑 `Services/RecipeDatabaseService.cs`
2. 在 `LoadDefaultData()` 方法中添加新的 `Item` 和 `Recipe` 定义
3. 重新编译运行

### 主题定制

全局主题定义在 `Themes/EndfieldTheme.xaml`：

| 资源键 | 用途 |
|--------|------|
| `EndfieldYellow` | 主色调（柠檬黄 #FFD600） |
| `SurfaceDarkColor` | 深色背景（#0D0D0D） |
| `SurfaceCardColor` | 卡片背景（#141414） |
| `AccentYellowBrush` | 强调色画刷 |

---

## 调试技巧

### 启用 XAML 热重载

在 Visual Studio 中：
- 确保 `工具 → 选项 → 调试 → 热重载` 已启用
- 使用 `"仅我的代码"` 模式启动调试

### 查看生成的 MVVM 代码

CommunityToolkit.Mvvm 在 `obj/` 目录下生成源生成器代码：
```
obj\Debug\net8.0-windows10.0.19041.0\CommunityToolkit.Mvvm.SourceGenerators\
```

### 日志输出

使用 `System.Diagnostics.Debug.WriteLine()` 输出调试信息，在 VS 的"输出"窗口中查看。

---

## 发布检查清单

- [ ] 版本号更新（`Package.appxmanifest` 或项目属性）
- [ ] 配方数据库已同步最新游戏版本
- [ ] `Release` 配置下 x64 构建通过
- [ ] 无运行时异常（空引用、XamlParseException 等）
- [ ] README.md 截图已更新（如有 UI 变更）
