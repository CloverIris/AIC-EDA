using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AIC_EDA.Core
{
    /// <summary>
    /// Spatial Planner（空间布局规划器）- 2D/3D网格布局
    /// 类比IC设计中的 Floorplanning + Placement
    /// </summary>
    public class SpatialPlanner
    {
        public double GridSize { get; set; } = 1.0;
        public double LayerHeight { get; set; } = 5.0;

        private HashSet<(int x, int z)> _occupied2D = new();
        private Dictionary<Guid, (int x, int z)> _nodePositions = new();

        /// <summary>
        /// 自动2D布局（按拓扑层排列）
        /// </summary>
        public ProductionGraph AutoLayout2D(ProductionGraph graph, int maxWidth = 50)
        {
            _occupied2D.Clear();
            _nodePositions.Clear();

            var layers = graph.GetLayers();
            int currentX = 0;

            foreach (var layerGroup in layers)
            {
                int layerIndex = layerGroup.Key;
                var nodes = layerGroup.ToList();
                int currentZ = 0;
                int maxLayerWidth = 0;

                // 对每层内的节点进行排序：按设备类型和数量
                nodes = nodes.OrderBy(n => n.Recipe.Machine.GetCategory())
                             .ThenByDescending(n => n.Count)
                             .ToList();

                foreach (var node in nodes)
                {
                    var spec = MachineSpecDatabase.GetSpec(node.Recipe.Machine);
                    int w = (int)Math.Ceiling((spec?.Width ?? 2) / GridSize);
                    int d = (int)Math.Ceiling((spec?.Depth ?? 2) / GridSize);

                    // 寻找不重叠的位置
                    var pos = FindPlacement2D(currentX, currentZ, w, d, maxWidth);
                    if (pos == null)
                    {
                        // 如果当前行放不下，换到下一行
                        currentZ += 5;
                        pos = FindPlacement2D(currentX, currentZ, w, d, maxWidth);
                    }

                    if (pos != null)
                    {
                        node.Position = new Vector3(
                            (float)(pos.Value.x * GridSize),
                            0,
                            (float)(pos.Value.z * GridSize));

                        MarkOccupied2D(pos.Value.x, pos.Value.z, w, d);
                        _nodePositions[node.Id] = pos.Value;

                        currentZ = pos.Value.z + d + 1;
                        maxLayerWidth = Math.Max(maxLayerWidth, w);
                    }
                }

                currentX += maxLayerWidth + 3;
            }

            return graph;
        }

        /// <summary>
        /// 自动3D分层布局
        /// </summary>
        public ProductionGraph AutoLayout3D(ProductionGraph graph, int maxWidth = 40, int maxDepth = 40)
        {
            var occupied3D = new HashSet<(int x, int y, int z)>();
            _nodePositions.Clear();

            var layers = graph.GetLayers();

            foreach (var layerGroup in layers)
            {
                int layerIndex = layerGroup.Key;
                int yLevel = layerIndex * (int)(LayerHeight / GridSize);
                var nodes = layerGroup.ToList();

                // 按电力需求和设备类型分组
                nodes = nodes.OrderBy(n => n.Recipe.Machine.GetCategory())
                             .ThenByDescending(n => n.Count)
                             .ToList();

                // 使用简单的网格填充
                int cols = (int)Math.Ceiling(Math.Sqrt(nodes.Count));
                int col = 0, row = 0;

                foreach (var node in nodes)
                {
                    var spec = MachineSpecDatabase.GetSpec(node.Recipe.Machine);
                    int w = (int)Math.Ceiling((spec?.Width ?? 2) / GridSize);
                    int d = (int)Math.Ceiling((spec?.Depth ?? 2) / GridSize);
                    int h = (int)Math.Ceiling((spec?.Height ?? 2) / GridSize);

                    // 计算网格位置
                    int x = col * 4;
                    int z = row * 4;

                    // 检查碰撞并调整
                    while (IsOccupied3D(occupied3D, x, yLevel, z, w, d, h))
                    {
                        col++;
                        if (col >= cols)
                        {
                            col = 0;
                            row++;
                        }
                        x = col * 4;
                        z = row * 4;
                    }

                    node.Position = new Vector3(
                        (float)(x * GridSize),
                        (float)(yLevel * GridSize),
                        (float)(z * GridSize));

                    MarkOccupied3D(occupied3D, x, yLevel, z, w, d, h);
                    _nodePositions[node.Id] = (x, z);

                    col++;
                    if (col >= cols)
                    {
                        col = 0;
                        row++;
                    }
                }
            }

            return graph;
        }

        /// <summary>
        /// 优化布局：基于力导向模型减少传送带长度
        /// </summary>
        public ProductionGraph OptimizeLayout(ProductionGraph graph, int iterations = 50)
        {
            var random = new Random(42);

            for (int i = 0; i < iterations; i++)
            {
                foreach (var node in graph.Nodes)
                {
                    if (node.Position == null) continue;

                    var pos = node.Position.Value;
                    Vector3 force = Vector3.Zero;

                    // 吸引力：连接到同一目标的节点应该靠近
                    foreach (var edge in graph.GetOutputEdges(node.Id))
                    {
                        var target = graph.FindNode(edge.TargetId);
                        if (target?.Position != null)
                        {
                            var diff = target.Position.Value - pos;
                            var dist = diff.Length();
                            if (dist > 0.1f)
                            {
                                // 吸引力与距离成正比（弹簧模型）
                                force += diff / dist * (float)(dist * 0.01);
                            }
                        }
                    }

                    // 排斥力：节点之间不应该重叠
                    foreach (var other in graph.Nodes)
                    {
                        if (other.Id == node.Id || other.Position == null) continue;

                        var diff = pos - other.Position.Value;
                        var dist = diff.Length();
                        if (dist > 0.1f && dist < 10)
                        {
                            force += diff / dist * (float)(1.0 / (dist * dist));
                        }
                    }

                    // 应用力（小步长）
                    pos += force * 0.1f;
                    node.Position = pos;
                }
            }

            // 网格对齐
            foreach (var node in graph.Nodes)
            {
                if (node.Position != null)
                {
                    var pos = node.Position.Value;
                    node.Position = new Vector3(
                        (float)(Math.Round(pos.X / GridSize) * GridSize),
                        (float)(Math.Round(pos.Y / GridSize) * GridSize),
                        (float)(Math.Round(pos.Z / GridSize) * GridSize));
                }
            }

            return graph;
        }

        private (int x, int z)? FindPlacement2D(int startX, int startZ, int w, int d, int maxWidth)
        {
            for (int z = startZ; z < maxWidth; z++)
            {
                for (int x = startX; x < maxWidth; x++)
                {
                    if (!IsOccupied2D(x, z, w, d))
                        return (x, z);
                }
            }
            return null;
        }

        private bool IsOccupied2D(int x, int z, int w, int d)
        {
            for (int dx = 0; dx < w; dx++)
                for (int dz = 0; dz < d; dz++)
                    if (_occupied2D.Contains((x + dx, z + dz)))
                        return true;
            return false;
        }

        private void MarkOccupied2D(int x, int z, int w, int d)
        {
            for (int dx = 0; dx < w; dx++)
                for (int dz = 0; dz < d; dz++)
                    _occupied2D.Add((x + dx, z + dz));
        }

        private bool IsOccupied3D(HashSet<(int x, int y, int z)> occupied, int x, int y, int z, int w, int d, int h)
        {
            for (int dx = 0; dx < w; dx++)
                for (int dy = 0; dy < h; dy++)
                    for (int dz = 0; dz < d; dz++)
                        if (occupied.Contains((x + dx, y + dy, z + dz)))
                            return true;
            return false;
        }

        private void MarkOccupied3D(HashSet<(int x, int y, int z)> occupied, int x, int y, int z, int w, int d, int h)
        {
            for (int dx = 0; dx < w; dx++)
                for (int dy = 0; dy < h; dy++)
                    for (int dz = 0; dz < d; dz++)
                        occupied.Add((x + dx, y + dy, z + dz));
        }
    }
}
