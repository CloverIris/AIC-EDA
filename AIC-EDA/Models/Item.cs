using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIC_EDA.Models
{
    /// <summary>
    /// 物品/资源类型
    /// </summary>
    public enum ItemCategory
    {
        RawMaterial,    // 原材料
        Intermediate,   // 中间产物
        FinalProduct,   // 最终产品
        Fluid,          // 液体
        Special,        // 特殊物品
    }

    /// <summary>
    /// 物品定义
    /// </summary>
    public class Item
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string NameEN { get; set; } = string.Empty;
        public ItemCategory Category { get; set; }
        public string? IconPath { get; set; }
        public string? Description { get; set; }

        public Item() { }

        public Item(string id, string name, string nameEN, ItemCategory category)
        {
            Id = id;
            Name = name;
            NameEN = nameEN;
            Category = category;
        }

        public override string ToString() => Name;
        public override bool Equals(object? obj) => obj is Item other && Id == other.Id;
        public override int GetHashCode() => Id.GetHashCode();
    }
}
