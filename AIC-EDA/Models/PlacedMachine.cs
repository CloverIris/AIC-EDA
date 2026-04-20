using System;
using System.Numerics;

namespace AIC_EDA.Models
{
    /// <summary>
    /// 已放置的设备实例 - 用于交互式布局设计器
    /// </summary>
    public class PlacedMachine
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>设备类型</summary>
        public MachineType MachineType { get; set; }

        /// <summary>网格坐标 X</summary>
        public int GridX { get; set; }

        /// <summary>网格坐标 Y</summary>
        public int GridY { get; set; }

        /// <summary>旋转角度 (0, 90, 180, 270)</summary>
        public int Rotation { get; set; }

        /// <summary>自定义标签</summary>
        public string? Label { get; set; }

        /// <summary>所属配方ID（可选，关联到编译图）</summary>
        public string? RecipeId { get; set; }

        public string DisplayName => MachineType.GetDisplayName();

        public MachineCategory Category => MachineType.GetCategory();

        public MachineSpec? Spec => MachineSpecDatabase.GetSpec(MachineType);

        /// <summary>占据的网格宽度（考虑旋转）</summary>
        public int GridWidth
        {
            get
            {
                var spec = Spec;
                if (spec == null) return 2;
                int w = (int)Math.Ceiling(spec.Width);
                int d = (int)Math.Ceiling(spec.Depth);
                return (Rotation == 90 || Rotation == 270) ? d : w;
            }
        }

        /// <summary>占据的网格深度（考虑旋转）</summary>
        public int GridDepth
        {
            get
            {
                var spec = Spec;
                if (spec == null) return 2;
                int w = (int)Math.Ceiling(spec.Width);
                int d = (int)Math.Ceiling(spec.Depth);
                return (Rotation == 90 || Rotation == 270) ? w : d;
            }
        }

        /// <summary>获取该设备占据的所有网格单元</summary>
        public System.Collections.Generic.List<(int x, int y)> OccupiedCells()
        {
            var cells = new System.Collections.Generic.List<(int x, int y)>();
            for (int dx = 0; dx < GridWidth; dx++)
            {
                for (int dy = 0; dy < GridDepth; dy++)
                {
                    cells.Add((GridX + dx, GridY + dy));
                }
            }
            return cells;
        }

        /// <summary>检查是否与另一个设备碰撞</summary>
        public bool CollidesWith(PlacedMachine other)
        {
            if (Id == other.Id) return false;
            var myCells = OccupiedCells();
            var otherCells = other.OccupiedCells();
            foreach (var cell in myCells)
            {
                if (otherCells.Contains(cell))
                    return true;
            }
            return false;
        }
    }
}
