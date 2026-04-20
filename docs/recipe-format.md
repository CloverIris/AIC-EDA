# 配方数据格式

本文档描述 AIC-EDA 中配方和物品的数据模型，以及如何添加新的配方。

---

## 物品（Item）

### 数据模型

```csharp
public class Item
{
    public string Id { get; set; }           // 唯一标识符
    public string Name { get; set; }          // 显示名称
    public ItemCategory Category { get; set; } // 分类
    public string? Description { get; set; }  // 描述
}
```

### 物品分类

```csharp
public enum ItemCategory
{
    RawMaterial,    // 原材料（矿石、植物）
    Intermediate,   // 中间产物（钢板、齿轮）
    FinalProduct,   // 最终产物（电池、药品）
    Fluid,          // 流体（水、废水）
    Special         // 特殊物品
}
```

### 分类颜色

| 分类 | 颜色 | 用途 |
|------|------|------|
| RawMaterial | 灰色 | 布局预览中显示为灰色块 |
| Intermediate | 黄色 | 布局预览中显示为黄色块 |
| FinalProduct | 绿色 | 布局预览中显示为绿色块 |
| Fluid | 青色 | 布局预览中显示为青色块 |
| Special | 橙色 | 布局预览中显示为橙色块 |

---

## 配方（Recipe）

### 数据模型

```csharp
public class Recipe
{
    public string Id { get; set; }              // 唯一标识符
    public string Name { get; set; }            // 显示名称
    public MachineType Machine { get; set; }    // 加工设备类型
    public double ProcessingTime { get; set; }  // 加工时间（秒）
    public Dictionary<string, int> Inputs { get; set; }   // 输入: {物品ID: 数量}
    public Dictionary<string, int> Outputs { get; set; }  // 输出: {物品ID: 数量}
}
```

### 示例配方

```csharp
new Recipe
{
    Id = "refining_steel",
    Name = "精炼钢材",
    Machine = MachineType.RefiningUnit,
    ProcessingTime = 4.0,
    Inputs = new Dictionary<string, int>
    {
        ["iron_ore"] = 2,
        ["coal"] = 1
    },
    Outputs = new Dictionary<string, int>
    {
        ["steel"] = 1
    }
}
```

### 加工时间说明

- `ProcessingTime` 单位为**秒**
- 设备效率 = 60 / ProcessingTime（个/分钟）
- 例如：加工时间 4 秒 → 效率 = 15 个/分钟

---

## 设备类型（MachineType）

### 枚举定义

```csharp
public enum MachineType
{
    // 资源采集
    MiningRig,              // 电驱矿机
    MiningRigMk2,           // 二型电驱矿机
    HydraulicMiningRig,     // 液压采矿机 (Wuling)
    FluidPump,              // 水泵

    // 基础加工
    RefiningUnit,           // 精炼单元
    ShreddingUnit,          // 粉碎单元
    GrindingUnit,           // 研磨机
    MouldingUnit,           // 塑形机
    FittingUnit,            // 装配单元
    GearingUnit,            // 齿轮单元
    FillingUnit,            // 灌装机
    PackagingUnit,          // 包装单元
    SeparatingUnit,         // 分离单元

    // 高级加工
    ReactorCrucible,        // 反应池
    ExpandedCrucible,       // 反应池扩展
    PurificationUnit,       // 纯化单元

    // 农业
    PlantingUnit,           // 种植机
    SeedPickingUnit,        // 选种机

    // 物流
    ConveyorBelt,           // 传送带
    Splitter,               // 分流器
    Merger,                 // 汇流器
    Bridge,                 // 跨接器
    Pipe,                   // 管道
    ProtocolStash,          // 协议储存器

    // 电力
    ProtocolCore,           // 协议锚点核心 (PAC)
    SubPAC,                 // 次级核心
    PowerPylon,             // 供电桩
    RelayTower,             // 中继器
    ThermalBank,            // 热能银行
}
```

### 设备分类

```csharp
public enum MachineCategory
{
    Resource,       // 资源采集（蓝）
    Processing,     // 加工（橙）
    Logistics,      // 物流（灰）
    Power,          // 电力（金）
    Agriculture     // 农业（绿）
}
```

---

## 设备空间规格（MachineSpec）

### 数据模型

```csharp
public class MachineSpec
{
    public MachineType MachineType { get; set; }
    public double Width { get; set; }           // X 轴尺寸（网格单位）
    public double Depth { get; set; }           // Z 轴尺寸（网格单位）
    public double Height { get; set; }          // Y 轴尺寸（网格单位）
    public List<Vector3> InputPorts { get; set; }   // 输入端口相对位置
    public List<Vector3> OutputPorts { get; set; }  // 输出端口相对位置
    public double PowerRadius { get; set; }     // 电力覆盖半径
    public double MaxBeltDistance { get; set; } // 最大传送带连接距离
    public bool GridAligned { get; set; }       // 是否网格对齐
    public bool AllowRotation { get; set; }     // 是否允许旋转
}
```

### 常见设备尺寸

| 设备 | 宽×深×高 | 电力半径 | 可旋转 |
|------|---------|---------|--------|
| 电驱矿机 | 3×3×2 | 20 | ✅ |
| 精炼单元 | 2×2×2 | 15 | ✅ |
| 齿轮单元 | 3×3×2 | 15 | ✅ |
| 反应池 | 3×3×3 | 20 | ✅ |
| 协议核心 | 4×4×3 | 40 | ❌ |
| 供电桩 | 1×1×3 | 25 | ❌ |
| 协议储存器 | 2×2×2 | 10 | ✅ |

---

## 如何添加新配方

### 步骤 1: 确认物品已定义

在 `RecipeDatabaseService.LoadDefaultData()` 的 `_items` 列表中检查物品是否存在：

```csharp
_items.Add(new Item { Id = "new_item", Name = "新物品", Category = ItemCategory.Intermediate });
```

### 步骤 2: 添加配方

在同一文件的 `_recipes` 列表中添加：

```csharp
_recipes.Add(new Recipe
{
    Id = "new_recipe_id",           // 唯一 ID
    Name = "新配方名称",             // 显示名称
    Machine = MachineType.RefiningUnit,  // 加工设备
    ProcessingTime = 3.0,           // 加工时间（秒）
    Inputs = new Dictionary<string, int>
    {
        ["input_item_1"] = 2,
        ["input_item_2"] = 1
    },
    Outputs = new Dictionary<string, int>
    {
        ["new_item"] = 1
    }
});
```

### 步骤 3: 添加设备规格（如果是新设备类型）

在 `Models/MachineSpec.cs` 的 `MachineSpecDatabase` 中添加：

```csharp
[MachineType.NewMachine] = new MachineSpec(MachineType.NewMachine, 2, 2, 2)
{
    InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
    OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
    PowerRadius = 15
}
```

### 步骤 4: 添加显示名称和分类（如果是新设备类型）

在 `Models/MachineType.cs` 中：

1. 在枚举中添加新值
2. 在 `GetDisplayName()` 中添加名称映射
3. 在 `GetCategory()` 中添加分类映射

### 步骤 5: 重新编译测试

```bash
dotnet build -c Debug -p:Platform=x64
dotnet run -c Debug -p:Platform=x64
```

---

## 配方数据来源

当前配方数据基于以下公开资料整理：

- [game8.co - Arknights: Endfield](https://game8.co)
- [endfielddb.com](https://endfielddb.com)
- [endfield.wiki.gg](https://endfield.wiki.gg)

**免责声明**: 配方数据可能与游戏实际版本存在差异。如发现错误，欢迎提交 Issue 或 PR 修正。

---

## 配方数据库统计

| 类别 | 数量 |
|------|------|
| 物品 | 70+ |
| 配方 | 80+ |
| 设备类型 | 30+ |
| 生产链深度 | 最多 6 层 |

### 覆盖的生产链

```
采矿 → 精炼 → 零件加工 → 组件装配 → 最终产品
  │        │          │           │
  ├─ 铁矿 → 钢材 → 钢板/齿轮 → 装备原件
  ├─ 雷石 → 雷石矿 → 电池材料 → SC Wuling 电池
  ├─ 紫萤 → 纤维 → 纺织品 → 药品/饮料
  ├─ 红晶 → 晶石 → 晶体元件 → 电子器件
  └─ 流体 → 纯化 → 灌装 → 药剂/饮料
```
