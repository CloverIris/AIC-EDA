# AIC-EDA Agent Guidelines

> 本项目为《明日方舟：终末地》集成工业系统（AIC）开发的 RTL 自动布局工具，使用 WinUI 3 (Windows App SDK 1.8) + .NET 8 构建。

---

## 🔍 开发前必做：研究《终末地》集成工业系统

**在任何代码修改或功能设计之前，必须先联网搜索以下内容的最新信息：**

### 核心搜索关键词（必搜）
- `明日方舟终末地 集成工业系统 机制 配方 设备 传送带`
- `明日方舟终末地 协议核心 PAC 电力系统 供电桩 中继器`
- `Arknights Endfield integrated industrial system AIC mechanics recipes`
- `Arknights Endfield blueprint system layout factory`
- `终末地 集成工业 配方数据库 加工设备 矿机 精炼`
- `终末地 蓝图 分享码 导入导出 布局`
- `Arknights Endfield conveyor belt pipe splitter merger logistics`

### 需要持续跟踪的游戏机制
1. **配方数据**：新配方、新设备、加工时间、输入输出比例变更
2. **设备规格**：设备尺寸、端口位置、电力消耗变更
3. **物流系统**：传送带速度、管道系统、分流器/汇流器机制
4. **电力系统**：协议核心供电范围、供电桩覆盖半径、中继器机制
5. **蓝图系统**：游戏内蓝图分享码格式、导入导出机制
6. **新地区机制**：如武陵地区的液体/管道系统、息壤科技

---

## 🛠 技术栈与约束

### 强制使用 WinUI 3
- **Windows App SDK 版本**: 1.8.x
- **目标框架**: `net8.0-windows10.0.19041.0`
- **最低平台版本**: `10.0.17763.0`
- **MVVM 框架**: CommunityToolkit.Mvvm 8.4.0

### WinUI 3 兼容性红线
- ❌ **禁止使用 `Binding.StringFormat`** — WinUI 3 不支持。改用 `IValueConverter`、代码格式化属性，或 `StackPanel`+`TextBlock` 组合
- ❌ **禁止在 XAML 字符串插值中使用单引号字符字面量** — C# 字符串插值中 `{'文本'}` 会被解析为字符字面量，必须使用 `{"文本"}`
- ❌ **XAML 文件在子目录中时，禁止直接复制到根目录编译** — 会导致 WMC1013 重复路径错误。正确做法：使用 `<Link>` metadata
- ⚠️ **WinUI 3 的 `Colors` 预定义颜色在 `Microsoft.UI` 命名空间**，`Color` 结构体在 `Windows.UI` 命名空间
- ⚠️ **Windows Runtime 异步操作**（如 `FileSavePicker.PickSaveFileAsync()`）需调用 `.AsTask()` 后再 `await`

### XAML 子目录处理（已配置）
项目 `.csproj` 中已包含 workaround：
```xml
<Page Update="Views\**\*.xaml">
  <Link>%(Filename)%(Extension)</Link>
</Page>
```
**注意：所有子目录中的 XAML 文件名必须全局唯一。**

---

## 📁 项目结构规范

```
AIC-EDA/
├── Data/Recipes.json              # 配方数据库（唯一允许手动编辑的数据文件）
├── Models/                        # 纯数据模型，无业务逻辑
├── Core/                          # 核心算法引擎，无 UI 依赖
├── Services/                      # 数据加载/持久化服务
├── Converters/                    # XAML 值转换器
├── ViewModels/                    # MVVM 视图模型
├── Views/                         # XAML 页面（仅存放界面代码）
└── PRD.md / AGENTS.md / README.md # 文档
```

### 命名规范
- 核心算法类命名遵循 EDA 工具链隐喻：`RecipeCompiler`、`FlowBalancer`、`SpatialPlanner`、`ThroughputSTA`、`DRCValidator`、`PWROptimizer`、`BlueprintCodec`
- ViewModel 使用 `CommunityToolkit.Mvvm` 的 `[ObservableProperty]` 源生成器
- 数据模型属性使用 `{ get; set; } = ...` 初始化，避免字段初始化器引用非静态成员

---

## 🧪 构建与验证

### 构建命令
```powershell
# 正确方式（指定平台）
msbuild AIC-EDA.csproj -t:Rebuild -p:Configuration=Debug -p:Platform=x64

# 或 Visual Studio 中直接生成（推荐）
```

### 常见陷阱
- `dotnet build` 在当前环境中可能对 WinAppSDK 1.8 的 XamlCompiler 错误报告不完整，优先使用 VS 的 MSBuild
- WinUI 3 打包应用不支持 AnyCPU，必须在 VS 中选择 x64/x86/ARM64 平台
- 清理解决方案时务必同时删除 `bin/` 和 `obj/` 目录

---

## 📋 修改检查清单

修改代码前确认：
- [ ] 已搜索《终末地》最新集成工业机制（如果修改涉及配方/设备/物流逻辑）
- [ ] XAML 中未使用 `StringFormat`
- [ ] C# 字符串插值中使用 `"` 而非 `'` 包裹文本
- [ ] 新增 XAML 文件名在所有子目录中唯一
- [ ] WinUI 3 颜色使用正确的命名空间（`Microsoft.UI.Colors` / `Windows.UI.Color`）
- [ ] 异步 WinRT API 调用已加 `.AsTask()`

---

*本文件由 Kimi Code 生成，适用于 AIC-EDA 项目及其所有子目录。*
