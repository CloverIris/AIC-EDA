# AIC-EDA 工业自动化布局系统

> Automated Industry Complex - Electronic Design Automation

## 项目概述

AIC-EDA 是为《明日方舟：终末地》集成工业系统（AIC）开发的RTL自动布局工具，将复杂的3D工厂布局问题转化为"芯片物理设计"问题。

## 命名体系（类比EDA工具链）

| 终末地工业概念 | 对应IC设计概念 | TALOS-II 工具模块 |
|---|---|---|
| 配方/资源流 | RTL Design | Recipe Compiler (配方编译器) |
| 设施布局规划 | Floorplanning | FloorPlan-EF (布图规划器) |
| 设备3D摆放 | Placement | Place & Belt (布局与传送带) |
| 传送带路由 | Routing | Route-Fabric (传送布图) |
| 电力网络 | CTS + Power Grid | PWR-Tree (供电树综合) |
| 产能验证 | STA (静态时序分析) | Throughput-STA (产能静态分析) |
| 物理验证 | Physical Verification | DRC-End (设计规则检查) |
| 蓝图交付 | GDSII / Tape-out | Tape-Deploy (部署流片) |

## 系统架构

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

## 技术栈

- **UI框架**: WinUI 3 (Windows App SDK 1.8)
- **目标框架**: .NET 8.0-windows10.0.19041.0
- **MVVM**: CommunityToolkit.Mvvm 8.4.0
- **数据**: System.Text.Json

## 项目结构

```
AIC-EDA/
├── App.xaml                 # 应用入口
├── MainWindow.xaml          # 主窗口 (NavigationView导航)
├── Data/
│   └── Recipes.json         # 配方数据库 (可外部更新)
├── Models/
│   ├── MachineType.cs       # 设备类型枚举
│   ├── Item.cs              # 物品定义
│   ├── Recipe.cs            # 配方模型
│   ├── MachineSpec.cs       # 设备空间规格
│   ├── ProductionNode.cs    # 生产节点
│   └── ProductionGraph.cs   # 生产依赖图
├── Core/
│   ├── RecipeCompiler.cs    # 配方综合器 (RTL->Netlist)
│   ├── FlowBalancer.cs      # 流量平衡器
│   ├── SpatialPlanner.cs    # 空间布局规划器
│   ├── RoutePlanner.cs      # 传送带路径规划
│   ├── ThroughputSTA.cs     # 产能静态分析
│   ├── DRCValidator.cs      # 设计规则检查
│   ├── PWROptimizer.cs      # 供电树优化
│   └── BlueprintCodec.cs    # 蓝图编解码器
├── Services/
│   └── RecipeDatabaseService.cs  # 配方数据库服务
├── ViewModels/
│   ├── MainViewModel.cs
│   ├── RecipeBrowserViewModel.cs
│   └── RecipeCompilerViewModel.cs
└── Views/
    ├── RecipeBrowserPage.xaml      # 配方浏览器
    ├── RecipeCompilerPage.xaml     # 配方综合器
    ├── LayoutPreviewPage.xaml      # 布局预览
    └── BlueprintExportPage.xaml    # 蓝图导出
```

## 核心算法

### 1. Recipe Compiler (配方编译器)
从目标产物反向构建生产树，使用拓扑排序确定设备层级关系。

### 2. Flow Balancer (流量平衡器)
根据目标产量计算每个节点的精确设备数量，确保上下游流量匹配。

### 3. Spatial Planner (空间布局规划器)
- 2D/3D网格布局
- 碰撞检测与避免
- 力导向优化减少传送带长度

### 4. Route Planner (传送布图)
基于A*算法的传送带路径规划，支持绕障和路径简化。

### 5. Throughput-STA (产能静态分析)
- 关键路径分析
- 瓶颈检测
- 松弛计算 (Slack = 实际产能 - 需求)

### 6. DRC-End (设计规则检查)
- DRC-001: 设备间距检查
- DRC-002: 传送带最大长度检查
- DRC-003: 电力覆盖检查
- DRC-004: 碰撞检测
- DRC-005: 网格对齐检查
- DRC-006: 输入输出连接检查

### 7. PWR-Tree (供电树综合)
- 贪心集合覆盖算法优化供电桩位置
- H-Tree对称布置
- 中继器自动插入

## 已知兼容性问题与解决方案

### 问题: WinAppSDK 1.8 XamlCompiler 崩溃
**症状**: `XamlCompiler.exe` 返回 exit code 1，不生成 output.json
**根因**: Windows App SDK 1.8 的 XamlCompiler 在特定条件下崩溃（GitHub Issue #10947）

**解决方案（推荐）**:
1. **降级 Windows App SDK 至 1.6.x 或 1.7.x**
   ```xml
   <PackageReference Include="Microsoft.WindowsAppSDK" Version="1.6.240923002" />
   ```

2. **或在 Visual Studio 中构建**（而非命令行 `dotnet build`）

3. **确保安装 .NET Framework 4.7.2+**（XamlCompiler.exe 依赖）

4. **检查 Visual Studio 2022 版本**: 建议 17.9+

项目已包含 MSBuild Workaround（`.csproj` 中的 `_FlattenXamlSubfolders` target），可在编译前将 Views/ 子目录中的 XAML 文件扁平化。

## 开发路线图

### Phase 1: 前端综合 (Recipe Compiler) ✅
- [x] 配方数据库
- [x] 产线平衡算法
- [x] Machine Netlist 输出

### Phase 2: 后端布局布线 (Place & Belt) ✅
- [x] 3D网格系统与碰撞检测
- [x] 基础Placement算法
- [x] Belt Routing (A*寻路)

### Phase 3: 物理实现 (PWR-Tree & DRC) ✅
- [x] 供能桩优化算法
- [x] DRC规则引擎
- [x] 2D可视化预览

### Phase 4: 签核与流片 (Sign-off & Tape-Deploy) ✅
- [x] Throughput-STA引擎
- [x] 蓝图编码器
- [x] JSON导出

## License

MIT License - 社区驱动项目，与鹰角网络/GRYPHLINE 无任何官方关联。
游戏资产归 GRYPHLINE 所有。
