using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AIC_EDA.Core
{
    /// <summary>
    /// PWR-Tree（供电树综合）- 优化供电桩和中继器布局
    /// 类比IC设计中的Clock Tree Synthesis (CTS) + Power Planning
    /// </summary>
    public class PWROptimizer
    {
        /// <summary>
        /// 供电方案结果
        /// </summary>
        public class PowerPlan
        {
            public List<Vector3> PylonPositions { get; set; } = new();
            public List<Vector3> RelayPositions { get; set; } = new();
            public double TotalCoverage { get; set; }
            public int PylonCount => PylonPositions.Count;
            public int RelayCount => RelayPositions.Count;
        }

        /// <summary>
        /// 使用贪心算法优化供电桩位置（集合覆盖问题）
        /// </summary>
        public PowerPlan OptimizePylons(ProductionGraph graph, double pylonRadius = 25.0)
        {
            var plan = new PowerPlan();
            var uncovered = graph.Nodes
                .Where(n => n.Position != null && n.Recipe.Machine.GetCategory() != MachineCategory.Power)
                .Select(n => n.Position!.Value)
                .ToList();

            var candidates = GenerateCandidatePositions(graph);

            while (uncovered.Count > 0)
            {
                Vector3? bestPylon = null;
                int bestCoverageCount = 0;
                List<Vector3> bestCoverage = new();

                foreach (var candidate in candidates)
                {
                    var coverage = uncovered
                        .Where(pos => Vector3.Distance(pos, candidate) <= pylonRadius)
                        .ToList();

                    if (coverage.Count > bestCoverageCount)
                    {
                        bestCoverageCount = coverage.Count;
                        bestPylon = candidate;
                        bestCoverage = coverage;
                    }
                }

                if (bestPylon == null || bestCoverageCount == 0)
                {
                    // 无法继续覆盖，在剩余未覆盖设备附近放置供电桩
                    if (uncovered.Count > 0)
                    {
                        bestPylon = uncovered[0];
                        bestCoverage = uncovered
                            .Where(pos => Vector3.Distance(pos, bestPylon.Value) <= pylonRadius)
                            .ToList();
                    }
                    else break;
                }

                plan.PylonPositions.Add(bestPylon.Value);
                foreach (var covered in bestCoverage)
                {
                    uncovered.Remove(covered);
                }
            }

            return plan;
        }

        /// <summary>
        /// H-Tree算法：对称布置供能桩，确保供电延迟一致
        /// </summary>
        public PowerPlan GenerateHTree(ProductionGraph graph, Vector3 center, int depth = 2)
        {
            var plan = new PowerPlan();
            GenerateHTreeRecursive(plan, center, 20.0f, depth);
            return plan;
        }

        private void GenerateHTreeRecursive(PowerPlan plan, Vector3 center, float size, int depth)
        {
            if (depth <= 0) return;

            // H树的四个端点作为供电桩位置
            var half = size / 2;
            var positions = new[]
            {
                new Vector3(center.X - half, center.Y, center.Z - half),
                new Vector3(center.X - half, center.Y, center.Z + half),
                new Vector3(center.X + half, center.Y, center.Z - half),
                new Vector3(center.X + half, center.Y, center.Z + half),
            };

            plan.PylonPositions.AddRange(positions);

            // 递归子H树
            foreach (var pos in positions)
            {
                GenerateHTreeRecursive(plan, pos, half, depth - 1);
            }
        }

        /// <summary>
        /// 中继器优化：长距离电力传输自动插入中继器
        /// </summary>
        public List<Vector3> OptimizeRelays(Vector3 source, Vector3 destination, double relayRange = 80.0)
        {
            var relays = new List<Vector3>();
            var totalDist = Vector3.Distance(source, destination);

            if (totalDist <= relayRange)
                return relays; // 不需要中继器

            var direction = Vector3.Normalize(destination - source);
            int relayCount = (int)Math.Ceiling(totalDist / relayRange) - 1;

            for (int i = 1; i <= relayCount; i++)
            {
                var pos = source + direction * (float)(relayRange * i);
                relays.Add(new Vector3(pos.X, Math.Max(source.Y, destination.Y), pos.Z));
            }

            return relays;
        }

        /// <summary>
        /// 计算总电力需求并检查是否超载
        /// </summary>
        public (double totalDemand, double totalSupply, bool isOverloaded) AnalyzePowerGrid(ProductionGraph graph)
        {
            double totalDemand = graph.TotalPowerConsumption;

            // 计算总供电（假设每个核心/发电机供电200kW）
            double totalSupply = graph.Nodes
                .Where(n => n.Recipe.Machine == MachineType.ProtocolCore)
                .Sum(n => 200.0 * n.Count);

            totalSupply += graph.Nodes
                .Where(n => n.Recipe.Machine == MachineType.ThermalBank)
                .Sum(n => 50.0 * n.Count);

            bool isOverloaded = totalDemand > totalSupply * 0.9; // 10%余量

            return (totalDemand, totalSupply, isOverloaded);
        }

        /// <summary>
        /// 生成候选供电桩位置（基于设备位置网格）
        /// </summary>
        private List<Vector3> GenerateCandidatePositions(ProductionGraph graph)
        {
            var positions = new List<Vector3>();

            foreach (var node in graph.Nodes)
            {
                if (node.Position == null) continue;
                positions.Add(node.Position.Value);

                // 在设备周围添加候选点
                for (int dx = -2; dx <= 2; dx++)
                {
                    for (int dz = -2; dz <= 2; dz++)
                    {
                        positions.Add(new Vector3(
                            node.Position.Value.X + dx * 5,
                            node.Position.Value.Y,
                            node.Position.Value.Z + dz * 5));
                    }
                }
            }

            return positions.Distinct().ToList();
        }
    }
}
