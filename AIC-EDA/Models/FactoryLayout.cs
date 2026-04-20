using System;
using System.Collections.Generic;
using System.Linq;

namespace AIC_EDA.Models
{
    /// <summary>
    /// 工厂布局 - 交互式布局设计器的核心数据模型
    /// </summary>
    public class FactoryLayout
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "New Layout";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime ModifiedAt { get; set; } = DateTime.Now;

        /// <summary>网格大小（像素）</summary>
        public int GridSize { get; set; } = 40;

        /// <summary>画布宽度（网格单元）</summary>
        public int CanvasGridWidth { get; set; } = 60;

        /// <summary>画布高度（网格单元）</summary>
        public int CanvasGridHeight { get; set; } = 40;

        /// <summary>已放置的设备列表</summary>
        public List<PlacedMachine> Machines { get; set; } = new();

        /// <summary>添加设备，返回是否成功（检测碰撞）</summary>
        public bool AddMachine(PlacedMachine machine)
        {
            if (machine.GridX < 0 || machine.GridY < 0) return false;
            if (machine.GridX + machine.GridWidth > CanvasGridWidth) return false;
            if (machine.GridY + machine.GridDepth > CanvasGridHeight) return false;

            foreach (var existing in Machines)
            {
                if (machine.CollidesWith(existing))
                    return false;
            }

            Machines.Add(machine);
            ModifiedAt = DateTime.Now;
            return true;
        }

        /// <summary>移除设备</summary>
        public bool RemoveMachine(Guid id)
        {
            var machine = Machines.FirstOrDefault(m => m.Id == id);
            if (machine == null) return false;
            Machines.Remove(machine);
            ModifiedAt = DateTime.Now;
            return true;
        }

        /// <summary>移动设备到新位置（检测碰撞和边界）</summary>
        public bool MoveMachine(Guid id, int newGridX, int newGridY)
        {
            var machine = Machines.FirstOrDefault(m => m.Id == id);
            if (machine == null) return false;

            // 临时移除以进行碰撞检测
            Machines.Remove(machine);

            var oldX = machine.GridX;
            var oldY = machine.GridY;
            machine.GridX = newGridX;
            machine.GridY = newGridY;

            bool valid = machine.GridX >= 0 && machine.GridY >= 0
                      && machine.GridX + machine.GridWidth <= CanvasGridWidth
                      && machine.GridY + machine.GridDepth <= CanvasGridHeight;

            if (valid)
            {
                foreach (var existing in Machines)
                {
                    if (machine.CollidesWith(existing))
                    {
                        valid = false;
                        break;
                    }
                }
            }

            if (!valid)
            {
                machine.GridX = oldX;
                machine.GridY = oldY;
            }

            Machines.Add(machine);
            if (valid) ModifiedAt = DateTime.Now;
            return valid;
        }

        /// <summary>旋转设备</summary>
        public bool RotateMachine(Guid id)
        {
            var machine = Machines.FirstOrDefault(m => m.Id == id);
            if (machine == null) return false;

            var spec = machine.Spec;
            if (spec == null || !spec.AllowRotation) return false;

            Machines.Remove(machine);
            var oldRotation = machine.Rotation;
            machine.Rotation = (machine.Rotation + 90) % 360;

            bool valid = machine.GridX + machine.GridWidth <= CanvasGridWidth
                      && machine.GridY + machine.GridDepth <= CanvasGridHeight;

            if (valid)
            {
                foreach (var existing in Machines)
                {
                    if (machine.CollidesWith(existing))
                    {
                        valid = false;
                        break;
                    }
                }
            }

            if (!valid)
            {
                machine.Rotation = oldRotation;
            }

            Machines.Add(machine);
            if (valid) ModifiedAt = DateTime.Now;
            return valid;
        }

        /// <summary>清空画布</summary>
        public void Clear()
        {
            Machines.Clear();
            ModifiedAt = DateTime.Now;
        }

        /// <summary>获取指定网格位置的设备</summary>
        public PlacedMachine? GetMachineAt(int gridX, int gridY)
        {
            foreach (var machine in Machines)
            {
                var cells = machine.OccupiedCells();
                if (cells.Contains((gridX, gridY)))
                    return machine;
            }
            return null;
        }

        /// <summary>统计各分类设备数量</summary>
        public Dictionary<MachineCategory, int> GetCategoryCounts()
        {
            return Machines.GroupBy(m => m.Category)
                          .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}
