using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIC_EDA.Models
{
    /// <summary>
    /// AIC设备类型枚举 - 对应终末地所有工业设施
    /// </summary>
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
        GearingUnit,            // 齿轮单元 (装备原件)
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
        Bridge,                 // 桥梁 (跨接)
        Pipe,                   // 管道
        PipeSplitter,           // 管道分流器
        PipeMerger,             // 管道汇流器
        ConduitInlet,           // 管道准入口
        ConduitOutlet,          // 管道输出口
        ProtocolStash,          // 协议储存器 / 总线卸载器

        // 电力
        ProtocolCore,           // 协议锚点核心 (PAC)
        SubPAC,                 // 次级核心
        PowerPylon,             // 供电桩
        RelayTower,             // 中继器
        ThermalBank,            // 热能银行 (发电机)

        // 其他
        FluidSupplyUnit,        // 流体供应单元
        WaterTreatmentUnit,     // 污水处理单元
        PlantWaterer,           // 给水器
    }

    /// <summary>
    /// 设备分类
    /// </summary>
    public enum MachineCategory
    {
        Resource,       // 资源采集
        Processing,     // 加工
        Logistics,      // 物流
        Power,          // 电力
        Agriculture,    // 农业
        Combat,         // 战斗
    }

    public static class MachineTypeExtensions
    {
        public static string GetDisplayName(this MachineType type)
        {
            return type switch
            {
                MachineType.MiningRig => "电驱矿机",
                MachineType.MiningRigMk2 => "二型电驱矿机",
                MachineType.HydraulicMiningRig => "液压采矿机",
                MachineType.FluidPump => "水泵",
                MachineType.RefiningUnit => "精炼单元",
                MachineType.ShreddingUnit => "粉碎单元",
                MachineType.GrindingUnit => "研磨机",
                MachineType.MouldingUnit => "塑形机",
                MachineType.FittingUnit => "装配单元",
                MachineType.GearingUnit => "齿轮单元",
                MachineType.FillingUnit => "灌装机",
                MachineType.PackagingUnit => "包装单元",
                MachineType.SeparatingUnit => "分离单元",
                MachineType.ReactorCrucible => "反应池",
                MachineType.ExpandedCrucible => "反应池扩展",
                MachineType.PurificationUnit => "纯化单元",
                MachineType.PlantingUnit => "种植机",
                MachineType.SeedPickingUnit => "选种机",
                MachineType.ConveyorBelt => "传送带",
                MachineType.Splitter => "分流器",
                MachineType.Merger => "汇流器",
                MachineType.Bridge => "跨接器",
                MachineType.Pipe => "管道",
                MachineType.PipeSplitter => "管道分流器",
                MachineType.PipeMerger => "管道汇流器",
                MachineType.ConduitInlet => "管道准入口",
                MachineType.ConduitOutlet => "管道输出口",
                MachineType.ProtocolStash => "协议储存器",
                MachineType.ProtocolCore => "协议锚点核心",
                MachineType.SubPAC => "次级核心",
                MachineType.PowerPylon => "供电桩",
                MachineType.RelayTower => "中继器",
                MachineType.ThermalBank => "热能银行",
                MachineType.FluidSupplyUnit => "流体供应单元",
                MachineType.WaterTreatmentUnit => "污水处理单元",
                MachineType.PlantWaterer => "给水器",
                _ => type.ToString()
            };
        }

        public static MachineCategory GetCategory(this MachineType type)
        {
            return type switch
            {
                MachineType.MiningRig or MachineType.MiningRigMk2 or MachineType.HydraulicMiningRig or MachineType.FluidPump
                    => MachineCategory.Resource,
                MachineType.RefiningUnit or MachineType.ShreddingUnit or MachineType.GrindingUnit or
                MachineType.MouldingUnit or MachineType.FittingUnit or MachineType.GearingUnit or
                MachineType.FillingUnit or MachineType.PackagingUnit or MachineType.SeparatingUnit or
                MachineType.ReactorCrucible or MachineType.ExpandedCrucible or MachineType.PurificationUnit
                    => MachineCategory.Processing,
                MachineType.ConveyorBelt or MachineType.Splitter or MachineType.Merger or MachineType.Bridge or
                MachineType.Pipe or MachineType.PipeSplitter or MachineType.PipeMerger or
                MachineType.ConduitInlet or MachineType.ConduitOutlet or MachineType.ProtocolStash
                    => MachineCategory.Logistics,
                MachineType.ProtocolCore or MachineType.SubPAC or MachineType.PowerPylon or
                MachineType.RelayTower or MachineType.ThermalBank
                    => MachineCategory.Power,
                MachineType.PlantingUnit or MachineType.SeedPickingUnit or MachineType.PlantWaterer
                    => MachineCategory.Agriculture,
                _ => MachineCategory.Processing
            };
        }
    }
}
