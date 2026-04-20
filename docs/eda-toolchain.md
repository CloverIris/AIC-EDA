# EDA 工具链详解

本文档深入说明 AIC-EDA 中每个 EDA 工具模块的算法原理、输入输出和使用方法。

---

## 工具链概览

```
RTL Design ──► Synthesis ──► Floorplanning ──► Placement ──► Routing ──► STA ──► DRC ──► Sign-off
   │              │                │                │             │         │       │          │
   ▼              ▼                ▼                ▼             ▼         ▼       ▼          ▼
Recipe        Recipe          Spatial         Spatial      Route     Throughput  DRC     Blueprint
Data          Compiler        Planner(2D)     Planner(3D)  Planner   STA         Validator  Export
```

---

## 1. Recipe Compiler（配方编译器）

### 功能
从目标产物反向递归构建完整的生产依赖树，确定每个中间产物所需的加工设备和数量。

### 输入
- `targetItemId`: 目标产物 ID（如 `"wuling_battery_sc"`）
- `targetRate`: 目标产能（个/分钟）

### 输出
- `ProductionGraph`: 包含所有节点和边的生产依赖图

### 算法

```
Compile(itemId, rate):
    1. 查找生产 itemId 的所有配方
    2. 选择最优配方（默认选择复杂度最低的）
    3. 计算所需设备数量 = rate / (配方产出率 × 设备效率)
    4. 对配方的每个输入原料：
         recursive Compile(inputItemId, inputRate)
    5. 分配拓扑层级（Layer）：
         - 原料层 = 0
         - 加工层 = max(前置节点层) + 1
```

### 示例

目标：`SC Wuling 电池` @ 10/min

```
Layer 4: [SC Wuling 电池] x2 (齿轮单元)
            │
Layer 3: [齿轮 T4] x3 ── [充能单元] x2
            │                  │
Layer 2: [齿轮 T3] x5      [高密度电池] x4
            │                  │
Layer 1: [齿轮 T2] x8      [电池组] x6
            │                  │
Layer 0: [齿轮 T1] x12     [钢板] x10 ── [雷石矿] x8
```

---

## 2. Flow Balancer（流量平衡器）

### 功能
根据目标产能，精确计算每个生产节点的设备数量，确保上下游流量匹配。

### 核心公式

**设备数量计算**:
```
设备数量 = ceil(需求流量 / 单设备产出流量)
```

**单设备产出流量**:
```
产出流量 = 配方产出量 / 配方加工时间 (分钟)
```

### 瓶颈检测

```
Slack = 实际产能 - 需求产能

if Slack < 0:
    报告瓶颈：节点 X 产能不足，需增加 Y 台设备
```

### 优化策略
- 合并相同配方的并行节点
- 优先使用高效率设备（如 MiningRigMk2 > MiningRig）
- 流体管线与固体传送带分离规划

---

## 3. Spatial Planner（空间布局规划器）

### 3.1 AutoLayout2D

**目标**: 将生产图中的所有节点排列在 2D 平面上，最小化传送带总长度，同时避免碰撞。

**算法**:
```
AutoLayout2D(graph, maxWidth=50):
    occupied = HashSet<(int, int)>()
    layers = graph.GetLayers()  // 按拓扑层分组
    currentX = 0
    
    for each layerGroup in layers:
        nodes = layerGroup.OrderBy(category).ThenByDescending(count)
        currentZ = 0
        maxLayerWidth = 0
        
        for each node in nodes:
            spec = MachineSpecDatabase.GetSpec(node.Recipe.Machine)
            w = ceil(spec.Width)
            d = ceil(spec.Depth)
            
            // 寻找不重叠的位置
            pos = FindPlacement2D(currentX, currentZ, w, d, maxWidth)
            
            if pos != null:
                node.Position = (pos.x, 0, pos.z)
                MarkOccupied(pos.x, pos.z, w, d)
                currentZ = pos.z + d + 1
                maxLayerWidth = max(maxLayerWidth, w)
        
        currentX += maxLayerWidth + 3  // 层间距
```

**放置策略**:
- 同层设备从上到下排列
- 不同层从左到右排列（资源采集在最左，最终产物在最右）
- 按设备类型分组（采矿在一起、加工在一起）

### 3.2 OptimizeLayout

**目标**: 进一步优化布局，减少传送带总长度。

**算法**: 力导向模型

```
for iteration in 1..50:
    for each node:
        force = Vector3.Zero
        
        // 吸引力：连接的节点应该靠近
        for each outputEdge:
            target = graph.FindNode(edge.TargetId)
            diff = target.Position - node.Position
            dist = diff.Length()
            force += diff / dist * dist * 0.01
        
        // 排斥力：节点不应该重叠
        for each otherNode:
            diff = node.Position - otherNode.Position
            dist = diff.Length()
            if dist < 10:
                force += diff / dist * (1 / dist²)
        
        node.Position += force * 0.1
    
    // 网格对齐
    for each node:
        node.Position = round(node.Position / GridSize) * GridSize
```

---

## 4. Route Planner（传送布图）

### 功能
为生产图中的每条边规划传送带路径，确保：
- 路径沿网格线走（曼哈顿路径）
- 不穿过其他设备
- 转弯次数最少

### 算法: A* 曼哈顿路径规划

```
PlanRoute(start, end, obstacles):
    sx, sz = (int)start.X, (int)start.Z
    ex, ez = (int)end.X, (int)end.Z
    path = [start]
    currentX, currentZ = sx, sz
    
    // X 方向移动
    while currentX != ex:
        nextX = currentX + sign(ex - currentX)
        if obstacles.Contains((nextX, currentZ)):
            // 绕行：尝试 Z 方向
            detourZ = currentZ + 1
            if !obstacles.Contains((currentX, detourZ)):
                currentZ = detourZ
                path.Add((currentX, currentZ))
                continue
            detourZ = currentZ - 1
            if !obstacles.Contains((currentX, detourZ)):
                currentZ = detourZ
                path.Add((currentX, currentZ))
                continue
        currentX = nextX
        path.Add((currentX, currentZ))
    
    // Z 方向移动（同理）
    while currentZ != ez:
        ...
    
    path = SimplifyPath(path)  // 去除共线点
    return path
```

### 路径简化

去除路径中的共线中间点，仅保留转折点：
```
Input:  [(0,0), (1,0), (2,0), (2,1), (2,2)]
Output: [(0,0), (2,0), (2,2)]
```

### 可视化

在布局预览中：
- 传送带为灰色虚线
- 转弯处显示黄色圆点标记
- 目标端显示黄色箭头

---

## 5. Throughput STA（产能静态时序分析）

### 类比 IC STA

| IC STA 概念 | AIC-EDA 对应概念 |
|------------|-----------------|
| 时钟周期 | 配方加工时间 |
| 关键路径 | 从原料到最终产物的最长加工链 |
| 建立时间 | 上游产出速率 ≥ 下游消耗速率 |
| 松弛 (Slack) | 实际产能 - 需求产能 |
| 时序违规 | 产能瓶颈 |

### 分析流程

```
1. 识别所有从原料到最终产物的完整路径
2. 计算每条路径的总加工时间
3. 找出关键路径（总时间最长的路径）
4. 计算每个节点的松弛值
5. 报告 Slack < 0 的瓶颈节点
```

### 输出示例

```
关键路径: 雷石矿 → 钢板 → 电池组 → 高密度电池 → SC Wuling 电池
总加工时间: 45.6 分钟

瓶颈报告:
- [齿轮单元] 在 Layer 4: Slack = -2.3/min (需增加 1 台设备)
- [灌装机] 在 Layer 2: Slack = -0.8/min (需增加 1 台设备)
```

---

## 6. DRC Validator（设计规则检查）

### DRC-001: 设备间距检查
```
for each pair of nodes:
    distance = ManhattanDistance(nodeA, nodeB)
    minDistance = max(nodeA.Width, nodeB.Width) / 2 + max(nodeA.Depth, nodeB.Depth) / 2 + 1
    if distance < minDistance:
        ReportViolation("DRC-001", nodeA, nodeB)
```

### DRC-002: 传送带最大长度
```
for each beltRoute:
    if beltRoute.Length > sourceMachine.MaxBeltDistance:
        ReportViolation("DRC-002", edge)
```

### DRC-003: 电力覆盖检查
```
powerNodes = graph.Nodes.Where(n => n.Category == Power)
for each processingNode:
    covered = powerNodes.Any(p => Distance(p, processingNode) <= p.PowerRadius)
    if !covered:
        ReportViolation("DRC-003", processingNode)
```

### DRC-004: 碰撞检测
```
occupied = HashSet<(int, int)>()
for each node:
    for dx in 0..Width:
        for dz in 0..Depth:
            if occupied.Contains((X+dx, Z+dz)):
                ReportViolation("DRC-004", node)
            occupied.Add((X+dx, Z+dz))
```

### DRC-005: 网格对齐检查
```
for each node:
    if node.Position.X != floor(node.Position.X) or
       node.Position.Z != floor(node.Position.Z):
        ReportViolation("DRC-005", node)
```

### DRC-006: 输入输出连接检查
```
for each edge:
    if graph.FindNode(edge.SourceId) == null or
       graph.FindNode(edge.TargetId) == null:
        ReportViolation("DRC-006", edge)
```

---

## 7. PWR Optimizer（供电树综合）

### 功能
优化供电设施（协议核心、供电桩）的布局，确保所有设备在供电范围内，同时最小化供电设施数量。

### 算法

**步骤 1: 贪婪覆盖**
```
uncovered = allProcessingNodes
while uncovered not empty:
    bestPosition = FindPositionCoveringMost(uncovered)
    PlacePowerPylon(bestPosition)
    uncovered.RemoveAll(n => Distance(bestPosition, n) <= PowerRadius)
```

**步骤 2: H-Tree 优化**
对于大规模布局，使用 H-Tree 结构部署中继塔，确保电力信号均匀分布。

---

## 8. Blueprint Codec（蓝图编解码器）

### 编码流程

```
ProductionGraph ──► JSON ──► GZip 压缩 ──► Base64 ──► 蓝图字符串
```

### 蓝图字符串格式

```
AICv1_<Base64EncodedGzippedJson>
```

### JSON 结构

```json
{
  "version": "1.0",
  "targetItem": "SC Wuling 电池",
  "targetRate": 10.0,
  "nodes": [
    {
      "id": "...",
      "recipeId": "gearing_sc_wuling",
      "machine": "GearingUnit",
      "count": 2,
      "layer": 4,
      "position": { "x": 45, "y": 0, "z": 12 },
      "rotation": 0
    }
  ],
  "edges": [
    {
      "sourceId": "...",
      "targetId": "...",
      "itemId": "gear_T4",
      "path": [{"x": 30, "z": 12}, {"x": 45, "z": 12}]
    }
  ],
  "power": {
    "cores": [{"x": 20, "z": 20}],
    "pylons": [{"x": 35, "z": 15}]
  }
}
```

### 导出格式

| 格式 | 用途 |
|------|------|
| 蓝图字符串 | 复制分享 |
| JSON 文件 | 备份、版本控制、第三方工具导入 |
| CSV | 设备清单统计 |
