using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI;

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

        /// <summary>
        /// UI category color for the Endfield theme
        /// Lemon yellow = primary accent, cyan = fluid/tech, green = final/success
        /// </summary>
        public Color CategoryColor => Category switch
        {
            ItemCategory.RawMaterial => Color.FromArgb(255, 158, 158, 158),      // Gray
            ItemCategory.Intermediate => Color.FromArgb(255, 255, 214, 0),       // Lemon Yellow
            ItemCategory.FinalProduct => Color.FromArgb(255, 57, 255, 20),       // Green (success)
            ItemCategory.Fluid => Color.FromArgb(255, 0, 229, 255),              // Cyan
            ItemCategory.Special => Color.FromArgb(255, 255, 109, 0),            // Orange
            _ => Color.FromArgb(255, 128, 128, 128),
        };

        /// <summary>
        /// Segoe Fluent Icons glyph for this item category
        /// </summary>
        public string IconGlyph => Category switch
        {
            ItemCategory.RawMaterial => "\uE7C1",    // World (ore/earth)
            ItemCategory.Intermediate => "\uE950",   // Repair (parts/components)
            ItemCategory.FinalProduct => "\uE74E",   // Package (finished goods)
            ItemCategory.Fluid => "\uE90B",          // Water (fluids)
            ItemCategory.Special => "\uE7C3",       // Admin (special/power)
            _ => "\uE71B",                           // Unknown
        };

        /// <summary>
        /// Short display label for category filter chips
        /// </summary>
        public string CategoryLabel => Category switch
        {
            ItemCategory.RawMaterial => "原料",
            ItemCategory.Intermediate => "中间",
            ItemCategory.FinalProduct => "成品",
            ItemCategory.Fluid => "流体",
            ItemCategory.Special => "特殊",
            _ => "其他",
        };

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
