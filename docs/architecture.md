# 架构设计文档

本文档描述 AIC-EDA 的整体架构、分层设计、数据流和关键设计决策。

---

## 架构总览

AIC-EDA 采用经典的 **MVVM（Model-View-ViewModel）** 架构，结合 **EDA 工具链模式** 组织核心逻辑。

```
┌─────────────────────────────────────────────────────────────┐
│                      表现层 (UI Layer)                       │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │
│  │ WelcomeWin  │ │  MainWindow │ │   Dialogs   │           │
│  └──────┬──────┘ └──────┬──────┘ └──────┬──────┘           │
│         │               │               │                   │
│  ┌──────▼───────────────▼───────────────▼──────┐           │
│  │              Views (XAML Pages)               │           │
│  │  RecipeBrowser │ RecipeCompiler │ LayoutPreview│           │
│  │  BlueprintExport                              │           │
│  └──────────────────┬────────────────────────────┘           │
└─────────────────────┼───────────────────────────────────────┘
                      │ Binding / Command
┌─────────────────────▼───────────────────────────────────────┐
│                   视图模型层 (ViewModel)                     │
│  RecipeBrowserVM    RecipeCompilerVM    LayoutPreviewVM     │
│  [ObservableProperty] + [RelayCommand] (CommunityToolkit)   │
└─────────────────────┬───────────────────────────────────────┘
                      │ Service Injection
┌─────────────────────▼───────────────────────────────────────┐
│                   服务层 (Service Layer)                     │
│  RecipeDatabaseService                                      │
└─────────────────────┬───────────────────────────────────────┘
                      │ Data Model
┌─────────────────────▼───────────────────────────────────────┐
│                    核心层 (Core Layer)                       │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │
│  │   Recipe    │ │    Flow     │ │   Spatial   │           │
│  │  Compiler   │ │  Balancer   │ │  Planner    │           │
│  └─────────────┘ └─────────────┘ └─────────────┘           │
│  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐           │
│  │   Route     │ │ Throughput  │ │    DRC      │           │
│  │  Planner    │ │    STA      │ │ Validator   │           │
│  └─────────────┘ └─────────────┘ └─────────────┘           │
│  ┌─────────────┐ ┌─────────────┐                           │
│  │    PWR      │ │  Blueprint  │                           │
│  │ Optimizer   │ │   Codec     │                           │
│  └─────────────┘ └─────────────┘                           │
└─────────────────────────────────────────────────────────────┘
                      │
┌─────────────────────▼───────────────────────────────────────┐
│                    数据层 (Data Layer)                       │
│  Item          Recipe         MachineSpec    ProductionNode │
│  MachineType   ProductionGraph                              │
└─────────────────────────────────────────────────────────────┘
```

---

## 分层职责

### 1. 表现层（Presentation Layer）

**XAML 页面**（`Views/`）负责：
- 用户界面布局和视觉呈现
- 用户输入捕获（点击、拖拽、滚轮）
- 数据绑定到 ViewModel

**窗口**（`MainWindow.xaml`, `WelcomeWindow.xaml`）负责：
- 应用级导航框架
- 全局命令栏和菜单
- 项目信息面板（右侧边栏）

**自定义控件**（`Controls/`）负责：
- 可复用的 UI 组件（如 `BracketTag`）

### 2. 视图模型层（ViewModel Layer）

使用 **CommunityToolkit.Mvvm** 源生成器自动生成 INotifyPropertyChanged 实现：

```csharp
public partial class RecipeCompilerViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<ProductionNodeViewModel> _nodes;
    [RelayCommand] private void Compile() { ... }
}
```

ViewModel 职责：
- 将 Model 数据转换为 View 友好的格式
- 处理用户命令（Compile、Zoom、Navigate）
- 维护可绑定的状态（SelectedItem、SearchText 等）
- **不包含任何 UI 操作或 Core 算法逻辑**

### 3. 服务层（Service Layer）

**RecipeDatabaseService**（单例模式）：
- 管理所有配方和物品数据的内存缓存
- 提供查询接口（按输出/输入查找配方）
- 懒加载默认数据（80+ 配方，70+ 物品）

### 4. 核心层（Core Layer）

EDA 工具链的实现，每个类对应 IC 设计流程中的一个环节：

#### RecipeCompiler — 配方编译器
- **输入**: 目标产物 ID、目标产能
- **输出**: `ProductionGraph`（生产依赖图）
- **算法**: 反向 DFS 遍历配方树，为每个产物选择最优配方，构建拓扑层级

#### FlowBalancer — 流量平衡器
- **输入**: `ProductionGraph`、目标产物、目标产能
- **输出**: 平衡后的 `ProductionGraph`（每个节点的设备数量精确计算）
- **算法**: 自底向上汇总需求，自顶向下分配产能

#### SpatialPlanner — 空间布局规划器
- **输入**: `ProductionGraph`
- **输出**: 带有 `Position` 的 `ProductionGraph`
- **算法**: 
  - 2D 布局：按拓扑层从左到右排列，同层内从上到下
  - 3D 布局：分层高度放置
  - 力导向优化：弹簧-斥力模型 + 网格对齐

#### RoutePlanner — 传送布图
- **输入**: `ProductionGraph`
- **输出**: `List<BeltRoute>`
- **算法**: A* 曼哈顿路径规划，障碍物绕行

#### ThroughputSTA — 产能静态时序分析
- **输入**: `ProductionGraph`
- **输出**: 瓶颈报告、松弛值
- **类比**: IC 设计中的 STA（Static Timing Analysis）

#### DRCValidator — 设计规则检查
- **输入**: `ProductionGraph`
- **输出**: DRC 违规列表
- **规则**: 间距、碰撞、电力覆盖、网格对齐、连接完整性

#### PWROptimizer — 供电树综合
- **输入**: `ProductionGraph`
- **输出**: 优化后的供电设施布局
- **算法**: 贪 Voronoi 覆盖 + H-Tree 优化

#### BlueprintCodec — 蓝图编解码器
- **输入**: `ProductionGraph`
- **输出**: GZip+Base64 编码字符串 或 JSON

### 5. 数据层（Data Layer）

纯数据模型，无业务逻辑：

| 模型 | 职责 |
|------|------|
| `Item` | 物品定义（ID、名称、分类、颜色） |
| `Recipe` | 配方定义（输入、输出、加工设备、耗时） |
| `MachineType` | 设备类型枚举 + 分类扩展方法 |
| `MachineSpec` | 设备空间规格（尺寸、端口、电力半径） |
| `ProductionNode` | 生产节点（实例化的配方 + 数量 + 位置） |
| `ProductionEdge` | 生产边（源节点 → 目标节点，传输物品） |
| `ProductionGraph` | 生产依赖图（节点列表 + 边列表） |

---

## 数据流

### 编译流程（Compile Flow）

```
用户选择目标产物 + 产能
        │
        ▼
┌───────────────┐
│ RecipeCompiler│ ──► 反向构建生产树
└───────┬───────┘
        │ ProductionGraph (无位置)
        ▼
┌───────────────┐
│ FlowBalancer  │ ──► 计算设备数量、平衡流量
└───────┬───────┘
        │ ProductionGraph (有数量，无位置)
        ▼
┌───────────────┐
│ SpatialPlanner│ ──► 2D/3D 网格布局
└───────┬───────┘
        │ ProductionGraph (有数量，有位置)
        ▼
┌───────────────┐
│ RoutePlanner  │ ──► 传送带路径规划
└───────┬───────┘
        │ ProductionGraph + BeltRoutes
        ▼
┌───────────────┐
│   App.Current │ ──► 保存到全局，供布局预览使用
│     Graph     │
└───────────────┘
```

### 布局预览流程（Preview Flow）

```
LayoutPreviewPage.Loaded
        │
        ▼
   App.CurrentGraph?
   ├─ null ──► 显示占位提示
   │
   └─ exists ──► DrawLayout()
                    ├─ DrawGrid()      ──► 铺满的网格线
                    ├─ DrawConveyorBelts() ──► L 形传送带
                    └─ DrawMachines()  ──► 俄罗斯方块式设备块
```

---

## 关键设计决策

### 决策 1: WinUI 3 而非 WPF

**原因**: WinUI 3 是微软推荐的现代 Windows 原生 UI 框架，支持：
- Mica/Acrylic 系统背景材质
- 现代控件和 Fluent Design
- Windows App SDK 的 MSIX 打包

**权衡**: 仅支持 Windows 10 2004+，无法跨平台。

### 决策 2: 网格化 Placement（整数坐标）

**原因**: 终末地的工厂建造基于网格系统，设备只能放在整数网格坐标上。

**实现**: 
- 世界坐标 = 网格单元索引（整数）
- 设备尺寸 = 占用的网格单元数（如 MiningRig = 3×3）
- 碰撞检测基于 `HashSet<(int x, int z)>`

**视觉**: 布局预览中的设备像俄罗斯方块一样严格对齐到网格线。

### 决策 3: 内置配方数据库（而非外部 API）

**原因**: 
- 游戏无官方 API
- 避免网络依赖
- 启动速度快

**权衡**: 配方更新需要重新编译应用。

### 决策 4: EDA 命名体系

**原因**: 终末地工业系统与 IC 设计存在天然的类比关系：
- 配方 → RTL
- 设备 → 标准单元
- 传送带 → 布线
- 电力 → 时钟/供电树

这种命名不仅增加了专业感，也使得有芯片设计背景的用户能快速理解系统。

### 决策 5: 自定义蓝图格式（而非游戏原生）

**原因**: 游戏蓝图格式未公开/未逆向。

**当前方案**: GZip+Base64 编码的自定义 JSON 格式，包含：
- 所有节点位置、旋转
- 传送带路径点
- 电力设施位置

**未来**: 一旦游戏蓝图格式被逆向，可添加新的 Codec 实现。

---

## 扩展点

### 添加新的 EDA 工具

1. 在 `Core/` 目录下创建新类
2. 在 `ViewModels/` 中添加对应的 ViewModel 属性
3. 在 `Views/` 中添加新的 XAML 页面（如果需要 UI）
4. 在 `MainWindow.xaml` 的 NavigationView 中添加导航项

### 添加新的设备类型

1. 在 `Models/MachineType.cs` 中添加枚举值
2. 在 `MachineTypeExtensions.GetDisplayName()` 中添加显示名称
3. 在 `MachineTypeExtensions.GetCategory()` 中添加分类
4. 在 `Models/MachineSpec.cs` 的 `MachineSpecDatabase` 中添加规格

### 添加新的配方

1. 在 `Services/RecipeDatabaseService.cs` 的 `LoadDefaultData()` 中添加
2. 确保所有输入/输出物品已定义

---

## 性能考量

| 场景 | 预期性能 | 优化策略 |
|------|---------|---------|
| 配方编译（<50 节点） | <100ms | 缓存中间结果 |
| 2D 布局（<100 节点） | <200ms | 贪心 + 力导向迭代 |
| A* 路由（<200 条 belt） | <500ms | 简化路径、HashSet 障碍检测 |
| 布局渲染（Canvas） | 60fps | 仅渲染可见区域，复用 UI 元素 |

---

## 安全与异常处理

- **空引用**: 所有可空类型使用 C# 8 nullable reference types
- **除零**: 流量平衡中检查零值分母
- **循环依赖**: RecipeCompiler 检测并打断循环配方引用
- **越界**: DRCValidator 检查所有坐标在有效范围内
