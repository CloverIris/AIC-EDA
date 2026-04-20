# AIC-EDA 产品需求文档

> Product Requirements Document — Automated Industry Complex Electronic Design Automation

## 1. 项目概述

### 1.1 产品名称
**AIC-EDA**（Automated Industry Complex - Electronic Design Automation）

### 1.2 产品定位
为《明日方舟：终末地》集成工业系统（AIC）开发的 RTL 自动布局工具，将复杂的3D工厂布局问题转化为"芯片物理设计"问题，提供从配方综合到部署流片的全流程自动化。

### 1.3 核心 Slogan
**Integrated Industry, Integrated Circuit**

### 1.4 目标用户
- 《明日方舟：终末地》高级基建玩家
- 工业优化爱好者
- 工厂模拟/图论算法研究者

---

## 2. 命名体系（类比 EDA 工具链）

| 终末地工业概念 | 对应 IC 设计概念 | AIC-EDA 工具模块 | 功能描述 |
|---|---|---|---|
| 配方/资源流 | RTL Design | Recipe Compiler | 将生产目标"综合"为设施网表 |
| 设施布局规划 | Floorplanning | FloorPlan-EF | 宏观区域划分与通道预留 |
| 设备 3D 摆放 | Placement | Place & Belt | 设备位置优化与碰撞检测 |
| 传送带路由 | Routing | Route-Fabric | 物流路径规划，避免缠绕与时序违规 |
| 电力网络 | CTS + Power Grid | PWR-Tree | 供电桩最优覆盖与时序同步 |
| 产能验证 | STA | Throughput-STA | 检测生产链瓶颈（Setup/Hold 违规） |
| 物理验证 | Physical Verification | DRC-End | 检查设备间距、传送带坡度、电力覆盖 |
| 蓝图交付 | GDSII / Tape-out | Tape-Deploy | 生成游戏可导入的蓝图/GDS 格式 |

---

## 3. 核心功能模块

### 3.1 Recipe Compiler（配方编译器）— 前端综合

**输入**：
- 目标产物与产能需求（Throughput Constraints）
- 可用配方库（Standard Cell Library）
- 优化目标（PPA：Power / Pollution / Area）

**输出**：
- 设施网表（Machine Netlist）：设备类型、数量、连接关系
- 资源流图（Data Flow Graph）
- 初步时序报告（产能预估）

**关键算法**：
- 从目标产物反向构建生产树（拓扑排序）
- 线性规划求解最优设备配比（产线平衡）
- 多目标优化（面积 vs 速度 vs 功耗）

### 3.2 FloorPlan-EF（布图规划器）

**核心约束**：
- 供电域划分（Power Domain）：高耗能区与低耗能区分离
- I/O 规划：原料输入口与成品输出口位置
- 宏模块布局（Macro Placement）：大型设备预定位
- 通道预留（Channel Reservation）：为传送带预留布线通道

**输出**：粗粒度布局草图（Coarse Placement）

### 3.3 Place & Belt（布局与传送带）— 核心后端

**Placement 阶段**：
- 标准单元布局：将设施实例化到 3D 网格
- Legalization：确保设备对齐网格、无重叠
- 时序驱动布局：关键路径上的设备优先靠近放置

**Belt Routing 阶段**：
- 全局布线（Global Routing）：确定传送带大致走向
- 详细布线（Detailed Routing）：具体路径规划（A* 寻路）
- 支持爬坡/转弯限制（Slope/DRC 约束）
- 流量分级：低级/中级/高级传送带对应不同线宽

**优化目标**：
- 最小化总传送带长度（Wire Length Minimization）
- 最小化转弯次数
- 避免交叉（Crosstalk Avoidance）

### 3.4 PWR-Tree（供电树综合）

**核心问题**：
- 供能桩（Power Pylon）辐射供应，需最小化供电延迟与中继级数

**算法**：
- H-Tree 算法：对称布置供能桩，确保供电延迟一致
- 中继器插入（Repeater Insertion）：长距离电力传输自动插入中继器
- 贪心集合覆盖：最少供电桩覆盖所有设备

### 3.5 Throughput-STA（产能静态分析）

**关键概念映射**：
- Setup Time Violation：上游产能不足，下游设备等待
- Hold Time Violation：上游产能过剩，下游缓存溢出
- Critical Path：生产链中最长的加工路径，决定整体产能上限

**分析维度**：
- 最大产能分析（Max Throughput Analysis）
- 瓶颈识别（Bottleneck Detection）
- 缓冲器尺寸优化（Buffer Sizing）

### 3.6 DRC-End（设计规则检查）

**检查项**：
- DRC-001 间距规则：设备间最小间距（防火/维护通道）
- DRC-002 传送带最大长度限制
- DRC-003 电力覆盖：无供电盲区
- DRC-004 碰撞检测：设备重叠检查
- DRC-005 网格对齐检查
- DRC-006 输入输出端口连通性检查

### 3.7 Tape-Deploy（部署流片）

**输出格式**：
- GDS-End：AIC-EDA 自定义 JSON-based 蓝图格式
- 游戏原生蓝图：直接生成游戏可分享的蓝图字符串（Base64 + GZip）
- 3D 预览：基于 Canvas 的 2D 硅虚拟原型预览

---

## 4. 技术架构

```
┌─────────────────────────────────────────────────────────────┐
│                    AIC-EDA RTL Layout Engine                 │
├──────────────┬──────────────┬──────────────┬─────────────────┤
│   Recipe     │   Flow       │   Spatial    │   Blueprint     │
│   Compiler   │   Balancer   │   Planner    │   Generator     │
├──────────────┼──────────────┼──────────────┼─────────────────┤
│ • 配方图构建  │ • 产线平衡    │ • 网格划分    │ • 游戏蓝图编码   │
│ • 依赖分析    │ • 瓶颈消除    │ • 碰撞检测    │ • 分享码生成    │
│ • 资源流图    │ • 并行优化    │ • 物流路径    │ • 可视化导出    │
└──────────────┴──────────────┴──────────────┴─────────────────┘
```

### 4.1 技术栈

| 层级 | 技术选型 |
|---|---|
| UI 框架 | WinUI 3 (Windows App SDK 1.8) |
| 目标框架 | .NET 8.0-windows10.0.19041.0 |
| MVVM 框架 | CommunityToolkit.Mvvm 8.4.0 |
| 数据序列化 | System.Text.Json |
| 核心算法 | 自定义实现（拓扑排序、A*、贪心覆盖、力导向） |

### 4.2 项目结构

```
AIC-EDA/
├── App.xaml / App.xaml.cs          # 应用入口
├── MainWindow.xaml / .xaml.cs      # 主窗口 (NavigationView)
├── Data/
│   └── Recipes.json                # 配方数据库（可热更新）
├── Models/                         # 数据模型层
│   ├── MachineType.cs              # 设备类型枚举（30+种）
│   ├── Item.cs                     # 物品/资源定义
│   ├── Recipe.cs                   # 配方模型
│   ├── MachineSpec.cs              # 设备空间规格数据库
│   ├── ProductionNode.cs           # 生产节点
│   └── ProductionGraph.cs          # 生产依赖图
├── Core/                           # 核心算法引擎
│   ├── RecipeCompiler.cs           # 配方综合器
│   ├── FlowBalancer.cs             # 流量平衡器
│   ├── SpatialPlanner.cs           # 空间布局规划器
│   ├── RoutePlanner.cs             # 传送布图（A*）
│   ├── ThroughputSTA.cs            # 产能静态分析
│   ├── DRCValidator.cs             # 设计规则检查
│   ├── PWROptimizer.cs             # 供电树优化
│   └── BlueprintCodec.cs           # 蓝图编解码器
├── Services/
│   └── RecipeDatabaseService.cs    # 配方数据库服务
├── Converters/
│   └── FormatConverters.cs         # 格式化转换器
├── ViewModels/                     # MVVM 视图模型
└── Views/                          # WinUI 3 页面
    ├── RecipeBrowserPage.xaml      # 配方浏览器
    ├── RecipeCompilerPage.xaml     # 配方综合器
    ├── LayoutPreviewPage.xaml      # 布局预览
    └── BlueprintExportPage.xaml    # 蓝图导出
```

---

## 5. UI/UX 设计

### 5.1 主界面布局

```
┌─────────────────────────────────────────────────────────────┐
│ AIC-EDA                              [Sign-off]            │
├──────────┬──────────────────────────────────────────────────┤
│          │                                                  │
│  Recipe  │              3D Layout View                    │
│  Browser │        (Canvas 2D 渲染)                        │
│          │                                                  │
├──────────┤         ┌─────────┐     ┌─────────┐           │
│  Cell    │         │ Floor   │     │ Place & │           │
│  Library │         │ Plan    │ --> │ Route   │           │
│          │         └─────────┘     └─────────┘           │
├──────────┤              ↓              ↓                  │
│  Flow    │         ┌─────────┐     ┌─────────┐           │
│  Manager │         │ PWR-Tree│     │  STA    │           │
│          │         └─────────┘     └─────────┘           │
├──────────┤              ↓              ↓                  │
│  Reports │         ┌─────────┐     ┌─────────┐           │
│          │         │ DRC-End │ --> │ Tape-   │           │
│          │         │         │     │ Deploy  │           │
│          │         └─────────┘     └─────────┘           │
└──────────┴──────────────────────────────────────────────────┘
```

### 5.2 导航结构

| 页面 | 功能 | 图标 |
|---|---|---|
| 配方浏览器 | 搜索/浏览配方和物品，查看上下游关系 | Library |
| Recipe Compiler | 选择目标产物，编译生产树，显示统计 | Repair |
| 布局预览 | 2D Canvas 渲染设备、传送带，支持平移缩放 | MapPin |
| 蓝图导出 | 编码预览、JSON 导出、剪贴板复制 | Share |

---

## 6. 数据模型

### 6.1 配方数据库（Recipes.json）

配方数据外置为 JSON，便于游戏版本更新时热替换。当前内置 50+ 物品、30+ 配方，覆盖：
- 原材料：源石矿、铁矿石、紫水晶矿、赤铜矿等
- 基础加工：精炼、粉碎、研磨、塑形、装配
- 高级加工：齿轮单元（组件制造）、反应池、纯化
- 农业：种植机、选种机
- 最终产品：荞愈胶囊、工业炸药、电池、装备原件

### 6.2 设备空间规格

每种设备定义：
- 尺寸（宽/深/高，网格单位）
- 输入/输出端口相对位置
- 电力覆盖半径
- 是否允许旋转

---

## 7. 核心算法规格

### 7.1 配方编译（反向构建）

从目标产物出发，递归查找生产配方，构建有向无环图（DAG）。使用 Kahn 算法进行拓扑分层（Layer 0 = 原料层）。

### 7.2 流量平衡

基于目标速率，反向传播需求，计算每个节点的设备数量。公式：
```
node.Count = ceil(下游需求 / 单设备产能)
```

### 7.3 空间布局

- 2D 分层布局：按拓扑层沿 X 轴排列，同层内按设备类型分组
- 碰撞检测：网格占用标记 + 简单推移（Legalization）
- 力导向优化：弹簧吸引力 + 排斥力，减少传送带长度

### 7.4 传送带路由

简化版 A* 寻路：曼哈顿路径 + 障碍物绕行。路径简化去除共线中间点。

### 7.5 供电优化

贪心集合覆盖：每次选择覆盖最多未覆盖设备的候选供电桩位置。

---

## 8. 开发路线图

### Phase 1: 前端综合 ✅
- [x] 配方数据库（Standard Cell Library）
- [x] 产线平衡算法
- [x] Machine Netlist 输出

### Phase 2: 后端布局布线 ✅
- [x] 3D 网格系统与碰撞检测
- [x] 基础 Placement 算法
- [x] Belt Routing（A* 寻路）

### Phase 3: 物理实现 ✅
- [x] 供能桩优化算法
- [x] DRC 规则引擎
- [x] 2D 可视化预览

### Phase 4: 签核与流片 ✅
- [x] Throughput-STA 引擎
- [x] 蓝图编码器
- [x] JSON 导出

### Phase 5: 高级特性（未来）
- [ ] 3D 立体预览（支持层切换）
- [ ] 动态重布局（产线扩容时自动调整）
- [ ] 多目标 PPA 优化（美观度 + 效率 + 电力消耗）
- [ ] 社区蓝图分享平台集成
- [ ] 游戏内存读取/蓝图直接导入（需逆向研究）

---

## 9. 法律声明

- 本项目为社区驱动的开源工具，与鹰角网络 / GRYPHLINE 无任何官方关联
- 游戏资产（配方、设备名称等）归 GRYPHLINE 所有
- 项目采用 MIT License

---

*文档版本: v1.0*
*最后更新: 2026-04-20*
