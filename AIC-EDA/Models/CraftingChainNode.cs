using System.Collections.Generic;

namespace AIC_EDA.Models
{
    /// <summary>
    /// Recursive crafting chain node for flyout display.
    /// Represents one item and the recipe used to produce it, plus all upstream inputs.
    /// </summary>
    public class CraftingChainNode
    {
        public Item Item { get; set; } = new();
        public Recipe? Recipe { get; set; }
        public double RequiredAmount { get; set; }
        public double RequiredRatePerMinute { get; set; }
        public List<CraftingChainNode> Inputs { get; set; } = new();
        public int Depth { get; set; }

        /// <summary>True if this item is a raw material (no recipe produces it).</summary>
        public bool IsRawMaterial => Recipe == null;
    }
}
