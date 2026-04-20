using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AIC_EDA.Models
{
    /// <summary>
    /// 生产节点 - 设施实例化
    /// </summary>
    public class ProductionNode
    {
        /// <summary>节点唯一ID</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>使用的配方</summary>
        public Recipe Recipe { get; set; } = null!;

        /// <summary>并行设备数量（产线平衡后）</summary>
        public int Count { get; set; } = 1;

        /// <summary>3D位置（网格坐标）</summary>
        public Vector3? Position { get; set; }

        /// <summary>旋转角度（0/90/180/270）</summary>
        public int Rotation { get; set; } = 0;

        /// <summary>输入连接（源节点ID -> 物品ID列表）</summary>
        public Dictionary<Guid, List<string>> InputConnections { get; set; } = new();

        /// <summary>输出连接（目标节点ID -> 物品ID列表）</summary>
        public Dictionary<Guid, List<string>> OutputConnections { get; set; } = new();

        /// <summary>实际每分钟产出（已考虑设备数量）</summary>
        public double GetActualOutputRatePerMinute(string itemId)
        {
            return Recipe.GetOutputRatePerMinute(itemId) * Count;
        }

        /// <summary>实际每分钟消耗（已考虑设备数量）</summary>
        public double GetActualInputRatePerMinute(string itemId)
        {
            return Recipe.GetInputRatePerMinute(itemId) * Count;
        }

        /// <summary>总电力消耗</summary>
        public double TotalPowerConsumption => Recipe.PowerConsumption * Count;

        /// <summary>节点层级（拓扑排序结果，0=原料层）</summary>
        public int Layer { get; set; }

        /// <summary>节点显示名称</summary>
        public string DisplayName => $"{Recipe.Machine.GetDisplayName()} x{Count}";

        public override string ToString() => DisplayName;
    }

    /// <summary>
    /// 生产图边 - 表示资源流连接
    /// </summary>
    public class ProductionEdge
    {
        public Guid SourceId { get; set; }
        public Guid TargetId { get; set; }
        public string ItemId { get; set; } = string.Empty;
        public double RatePerMinute { get; set; }
        public string? BeltTier { get; set; }
    }
}
