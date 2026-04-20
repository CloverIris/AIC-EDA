using System;
using System.Collections.Generic;
using System.Linq;

namespace AIC_EDA.Models
{
    /// <summary>
    /// 配方定义 - 对应终末地加工设备的单条配方
    /// </summary>
    public class Recipe
    {
        /// <summary>配方唯一ID</summary>
        public string Id { get; set; } = string.Empty;

        /// <summary>配方显示名称</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>加工设备类型</summary>
        public MachineType Machine { get; set; }

        /// <summary>输入物品及数量</summary>
        public Dictionary<string, double> Inputs { get; set; } = new();

        /// <summary>输出物品及数量</summary>
        public Dictionary<string, double> Outputs { get; set; } = new();

        /// <summary>加工时间（秒）</summary>
        public double Duration { get; set; }

        /// <summary>电力消耗（kW）</summary>
        public double PowerConsumption { get; set; }

        /// <summary>是否为主要配方（非副产物）</summary>
        public bool IsPrimary { get; set; } = true;

        /// <summary>配方所属科技阶段</summary>
        public string? TechTier { get; set; }

        /// <summary>计算每分钟产出速率</summary>
        public double GetOutputRatePerMinute(string itemId)
        {
            if (Outputs.TryGetValue(itemId, out double amount))
                return amount * 60.0 / Duration;
            return 0;
        }

        /// <summary>计算每分钟消耗速率</summary>
        public double GetInputRatePerMinute(string itemId)
        {
            if (Inputs.TryGetValue(itemId, out double amount))
                return amount * 60.0 / Duration;
            return 0;
        }

        public override string ToString() => $"{Name} @ {Machine.GetDisplayName()}";
    }
}
