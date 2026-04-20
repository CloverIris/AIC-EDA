using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AIC_EDA.Core
{
    /// <summary>
    /// Route-Fabric（传送布图）- 传送带路径规划
    /// 类比IC设计中的Routing（布线）
    /// </summary>
    public class RoutePlanner
    {
        public class BeltRoute
        {
            public Guid SourceId { get; set; }
            public Guid TargetId { get; set; }
            public string ItemId { get; set; } = string.Empty;
            public List<Vector3> Path { get; set; } = new();
            public double Length { get; set; }
            public int Turns { get; set; }
        }

        /// <summary>
        /// 为生产图规划所有传送带路径
        /// </summary>
        public List<BeltRoute> PlanAllRoutes(ProductionGraph graph)
        {
            var routes = new List<BeltRoute>();
            var occupied = new HashSet<(int x, int z)>();

            // 标记所有设备位置为障碍物
            foreach (var node in graph.Nodes)
            {
                if (node.Position == null) continue;
                var spec = MachineSpecDatabase.GetSpec(node.Recipe.Machine);
                var w = (int)Math.Ceiling(spec?.Width ?? 2);
                var d = (int)Math.Ceiling(spec?.Depth ?? 2);
                var cx = (int)node.Position.Value.X;
                var cz = (int)node.Position.Value.Z;

                for (int dx = -w / 2; dx <= w / 2; dx++)
                    for (int dz = -d / 2; dz <= d / 2; dz++)
                        occupied.Add((cx + dx, cz + dz));
            }

            foreach (var edge in graph.Edges)
            {
                var source = graph.FindNode(edge.SourceId);
                var target = graph.FindNode(edge.TargetId);
                if (source?.Position == null || target?.Position == null) continue;

                var route = PlanRoute(source.Position.Value, target.Position.Value, occupied);
                route.SourceId = edge.SourceId;
                route.TargetId = edge.TargetId;
                route.ItemId = edge.ItemId;
                routes.Add(route);
            }

            return routes;
        }

        /// <summary>
        /// A*寻路算法规划单条传送带路径
        /// </summary>
        public BeltRoute PlanRoute(Vector3 start, Vector3 end, HashSet<(int x, int z)> obstacles)
        {
            var route = new BeltRoute();

            // 简化实现：曼哈顿路径 + 绕障
            var sx = (int)start.X;
            var sz = (int)start.Z;
            var ex = (int)end.X;
            var ez = (int)end.Z;

            var path = new List<Vector3> { start };
            var currentX = sx;
            var currentZ = sz;

            // X方向移动
            while (currentX != ex)
            {
                var nextX = currentX + Math.Sign(ex - currentX);
                if (obstacles.Contains((nextX, currentZ)))
                {
                    // 绕行：尝试Z方向
                    var detourZ = currentZ + 1;
                    if (!obstacles.Contains((currentX, detourZ)))
                    {
                        currentZ = detourZ;
                        path.Add(new Vector3(currentX, start.Y, currentZ));
                        continue;
                    }
                    detourZ = currentZ - 1;
                    if (!obstacles.Contains((currentX, detourZ)))
                    {
                        currentZ = detourZ;
                        path.Add(new Vector3(currentX, start.Y, currentZ));
                        continue;
                    }
                }
                currentX = nextX;
                path.Add(new Vector3(currentX, start.Y, currentZ));
            }

            // Z方向移动
            while (currentZ != ez)
            {
                var nextZ = currentZ + Math.Sign(ez - currentZ);
                if (obstacles.Contains((currentX, nextZ)))
                {
                    // 绕行：尝试X方向
                    var detourX = currentX + 1;
                    if (!obstacles.Contains((detourX, currentZ)))
                    {
                        currentX = detourX;
                        path.Add(new Vector3(currentX, start.Y, currentZ));
                        continue;
                    }
                    detourX = currentX - 1;
                    if (!obstacles.Contains((detourX, currentZ)))
                    {
                        currentX = detourX;
                        path.Add(new Vector3(currentX, start.Y, currentZ));
                        continue;
                    }
                }
                currentZ = nextZ;
                path.Add(new Vector3(currentX, start.Y, currentZ));
            }

            path.Add(end);
            route.Path = SimplifyPath(path);
            route.Length = CalculatePathLength(route.Path);
            route.Turns = CountTurns(route.Path);

            return route;
        }

        /// <summary>
        /// 简化路径（去除共线中间点）
        /// </summary>
        private List<Vector3> SimplifyPath(List<Vector3> path)
        {
            if (path.Count <= 2) return path;

            var simplified = new List<Vector3> { path[0] };

            for (int i = 1; i < path.Count - 1; i++)
            {
                var prev = path[i - 1];
                var curr = path[i];
                var next = path[i + 1];

                // 检查是否为转折点
                var dx1 = curr.X - prev.X;
                var dz1 = curr.Z - prev.Z;
                var dx2 = next.X - curr.X;
                var dz2 = next.Z - curr.Z;

                if (dx1 != dx2 || dz1 != dz2)
                {
                    simplified.Add(curr);
                }
            }

            simplified.Add(path[path.Count - 1]);
            return simplified;
        }

        private double CalculatePathLength(List<Vector3> path)
        {
            double length = 0;
            for (int i = 1; i < path.Count; i++)
            {
                length += Vector3.Distance(path[i - 1], path[i]);
            }
            return length;
        }

        private int CountTurns(List<Vector3> path)
        {
            if (path.Count < 3) return 0;
            int turns = 0;

            for (int i = 2; i < path.Count; i++)
            {
                var dx1 = path[i - 1].X - path[i - 2].X;
                var dz1 = path[i - 1].Z - path[i - 2].Z;
                var dx2 = path[i].X - path[i - 1].X;
                var dz2 = path[i].Z - path[i - 1].Z;

                if ((dx1 != 0 && dz2 != 0) || (dz1 != 0 && dx2 != 0))
                {
                    turns++;
                }
            }

            return turns;
        }
    }
}
