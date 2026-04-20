using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AIC_EDA.Models
{
    /// <summary>
    /// 设备空间规格 - 用于布局规划
    /// </summary>
    public class MachineSpec
    {
        public MachineType MachineType { get; set; }

        /// <summary>宽度（X轴，网格单位）</summary>
        public double Width { get; set; }

        /// <summary>深度（Z轴，网格单位）</summary>
        public double Depth { get; set; }

        /// <summary>高度（Y轴，网格单位）</summary>
        public double Height { get; set; }

        /// <summary>输入端口相对位置列表</summary>
        public List<Vector3> InputPorts { get; set; } = new();

        /// <summary>输出端口相对位置列表</summary>
        public List<Vector3> OutputPorts { get; set; } = new();

        /// <summary>电力覆盖半径（供电桩等）</summary>
        public double PowerRadius { get; set; }

        /// <summary>最大传送带连接距离</summary>
        public double MaxBeltDistance { get; set; } = 50.0;

        /// <summary>是否可以对齐网格（大多数设备可以）</summary>
        public bool GridAligned { get; set; } = true;

        /// <summary>是否允许旋转（0/90/180/270度）</summary>
        public bool AllowRotation { get; set; } = true;

        /// <summary>设备建造材料成本</summary>
        public Dictionary<string, int>? BuildCost { get; set; }

        public MachineSpec() { }

        public MachineSpec(MachineType type, double width, double depth, double height)
        {
            MachineType = type;
            Width = width;
            Depth = depth;
            Height = height;
        }
    }

    /// <summary>
    /// 设备规格数据库
    /// </summary>
    public static class MachineSpecDatabase
    {
        private static readonly Dictionary<MachineType, MachineSpec> Specs = new()
        {
            // 资源采集
            [MachineType.MiningRig] = new MachineSpec(MachineType.MiningRig, 3, 3, 2)
            {
                OutputPorts = { new Vector3(0, 1, 1.5f) },
                PowerRadius = 20
            },
            [MachineType.MiningRigMk2] = new MachineSpec(MachineType.MiningRigMk2, 3, 3, 2)
            {
                OutputPorts = { new Vector3(0, 1, 1.5f) },
                PowerRadius = 20
            },
            [MachineType.HydraulicMiningRig] = new MachineSpec(MachineType.HydraulicMiningRig, 3, 3, 2)
            {
                OutputPorts = { new Vector3(0, 1, 1.5f) },
                PowerRadius = 20
            },
            [MachineType.FluidPump] = new MachineSpec(MachineType.FluidPump, 2, 2, 2)
            {
                OutputPorts = { new Vector3(0, 1, 1.0f) },
                PowerRadius = 15
            },

            // 基础加工 - 多数为2x2或3x3
            [MachineType.RefiningUnit] = new MachineSpec(MachineType.RefiningUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },
            [MachineType.ShreddingUnit] = new MachineSpec(MachineType.ShreddingUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },
            [MachineType.GrindingUnit] = new MachineSpec(MachineType.GrindingUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },
            [MachineType.MouldingUnit] = new MachineSpec(MachineType.MouldingUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },
            [MachineType.FittingUnit] = new MachineSpec(MachineType.FittingUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },
            [MachineType.GearingUnit] = new MachineSpec(MachineType.GearingUnit, 3, 3, 2)
            {
                InputPorts = { new Vector3(-1.0f, 0.5f, 1.5f), new Vector3(0, 0.5f, 1.5f) },
                OutputPorts = { new Vector3(1.0f, 0.5f, 1.5f) },
                PowerRadius = 15
            },
            [MachineType.FillingUnit] = new MachineSpec(MachineType.FillingUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f), new Vector3(0, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },
            [MachineType.PackagingUnit] = new MachineSpec(MachineType.PackagingUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f), new Vector3(0, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },
            [MachineType.SeparatingUnit] = new MachineSpec(MachineType.SeparatingUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f), new Vector3(0, 0.5f, 1.0f) },
                PowerRadius = 15
            },

            // 高级加工
            [MachineType.ReactorCrucible] = new MachineSpec(MachineType.ReactorCrucible, 3, 3, 3)
            {
                InputPorts = { new Vector3(-1.0f, 0.5f, 1.5f), new Vector3(0, 0.5f, 1.5f) },
                OutputPorts = { new Vector3(1.0f, 0.5f, 1.5f) },
                PowerRadius = 20
            },
            [MachineType.ExpandedCrucible] = new MachineSpec(MachineType.ExpandedCrucible, 3, 3, 3)
            {
                InputPorts = { new Vector3(-1.0f, 0.5f, 1.5f), new Vector3(0, 0.5f, 1.5f), new Vector3(1.0f, 0.5f, 1.5f) },
                OutputPorts = { new Vector3(0, 0.5f, -1.5f) },
                PowerRadius = 20
            },
            [MachineType.PurificationUnit] = new MachineSpec(MachineType.PurificationUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },

            // 农业
            [MachineType.PlantingUnit] = new MachineSpec(MachineType.PlantingUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0, 1.0f) },
                PowerRadius = 15
            },
            [MachineType.SeedPickingUnit] = new MachineSpec(MachineType.SeedPickingUnit, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },
            [MachineType.PlantWaterer] = new MachineSpec(MachineType.PlantWaterer, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },

            // 电力
            [MachineType.ProtocolCore] = new MachineSpec(MachineType.ProtocolCore, 4, 4, 3)
            {
                OutputPorts = { new Vector3(0, 1, 2.0f) },
                PowerRadius = 40,
                AllowRotation = false
            },
            [MachineType.SubPAC] = new MachineSpec(MachineType.SubPAC, 3, 3, 2)
            {
                OutputPorts = { new Vector3(0, 1, 1.5f) },
                PowerRadius = 30,
                AllowRotation = false
            },
            [MachineType.PowerPylon] = new MachineSpec(MachineType.PowerPylon, 1, 1, 3)
            {
                PowerRadius = 25,
                AllowRotation = false
            },
            [MachineType.RelayTower] = new MachineSpec(MachineType.RelayTower, 1, 1, 4)
            {
                PowerRadius = 80,
                AllowRotation = false
            },
            [MachineType.ThermalBank] = new MachineSpec(MachineType.ThermalBank, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                PowerRadius = 15
            },

            // 物流
            [MachineType.ProtocolStash] = new MachineSpec(MachineType.ProtocolStash, 2, 2, 2)
            {
                InputPorts = { new Vector3(-0.5f, 0.5f, 1.0f) },
                OutputPorts = { new Vector3(0.5f, 0.5f, 1.0f) },
                PowerRadius = 10
            },
        };

        public static MachineSpec? GetSpec(MachineType type)
        {
            return Specs.TryGetValue(type, out var spec) ? spec : null;
        }

        public static bool HasSpec(MachineType type) => Specs.ContainsKey(type);
    }
}
