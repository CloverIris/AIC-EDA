using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIC_EDA.Core
{
    /// <summary>
    /// Throughput-STA（产能静态分析）- 验证生产链时序收敛
    /// 类比IC设计中的Static Timing Analysis (STA)
    /// </summary>
    public class ThroughputSTA
    {
        /// <summary>
        /// 时序分析结果
        /// </summary>
        public class TimingResult
        {
            public Guid NodeId { get; set; }
            public string MachineName { get; set; } = string.Empty;
            public string ItemId { get; set; } = string.Empty;
            public double RequiredRate { get; set; }      // 需求速率
            public double ActualRate { get; set; }        // 实际产能
            public double Slack { get; set; }             // 松弛 = 实际 - 需求
            public bool IsCritical { get; set; }          // 是否关键路径
            public double Utilization { get; set; }       // 利用率
        }

        /// <summary>
        /// 执行产能静态分析
        /// </summary>
        public List<TimingResult> Analyze(ProductionGraph graph)
        {
            var results = new List<TimingResult>();

            foreach (var node in graph.Nodes)
            {
                foreach (var output in node.Recipe.Outputs)
                {
                    var itemId = output.Key;
                    var actualRate = node.GetActualOutputRatePerMinute(itemId);

                    // 计算下游总需求
                    var downstreamDemand = graph.Edges
                        .Where(e => e.SourceId == node.Id && e.ItemId == itemId)
                        .Sum(e => e.RatePerMinute);

                    // 如果没有下游连接（最终产物），使用目标速率
                    if (downstreamDemand == 0)
                    {
                        downstreamDemand = actualRate;
                    }

                    var slack = actualRate - downstreamDemand;
                    var utilization = actualRate > 0 ? downstreamDemand / actualRate : 0;

                    results.Add(new TimingResult
                    {
                        NodeId = node.Id,
                        MachineName = node.Recipe.Machine.GetDisplayName(),
                        ItemId = itemId,
                        RequiredRate = downstreamDemand,
                        ActualRate = actualRate,
                        Slack = slack,
                        IsCritical = Math.Abs(slack) < 0.01 || slack < 0,
                        Utilization = utilization
                    });
                }
            }

            return results.OrderBy(r => r.Slack).ToList();
        }

        /// <summary>
        /// 计算关键路径（产能最紧张的通路）
        /// </summary>
        public List<ProductionNode> FindCriticalPath(ProductionGraph graph)
        {
            var timing = Analyze(graph);
            var criticalNodes = timing
                .Where(t => t.IsCritical)
                .Select(t => graph.FindNode(t.NodeId))
                .Where(n => n != null)
                .Distinct()
                .ToList();

            // 按层级排序
            return criticalNodes.OrderBy(n => n!.Layer).ToList()!;
        }

        /// <summary>
        /// 计算生产链的理论最大产能
        /// </summary>
        public double CalculateMaxThroughput(ProductionGraph graph, string targetItemId)
        {
            var targetNodes = graph.Nodes.Where(n => n.Recipe.Outputs.ContainsKey(targetItemId)).ToList();
            if (targetNodes.Count == 0) return 0;

            // 对于每个目标节点，计算瓶颈限制
            double maxThroughput = double.MaxValue;

            foreach (var targetNode in targetNodes)
            {
                var nodeMax = CalculateNodeMaxThroughput(graph, targetNode, targetItemId);
                maxThroughput = Math.Min(maxThroughput, nodeMax);
            }

            return maxThroughput == double.MaxValue ? 0 : maxThroughput;
        }

        private double CalculateNodeMaxThroughput(ProductionGraph graph, ProductionNode node, string targetItemId)
        {
            double maxRate = node.GetActualOutputRatePerMinute(targetItemId);

            // 检查所有上游输入限制
            foreach (var input in node.Recipe.Inputs)
            {
                var inputItemId = input.Key;
                if (node.Recipe.Outputs.ContainsKey(inputItemId)) continue; // 可回收容器

                var inputEdges = graph.GetInputEdges(node.Id)
                    .Where(e => e.ItemId == inputItemId).ToList();

                foreach (var edge in inputEdges)
                {
                    var sourceNode = graph.FindNode(edge.SourceId);
                    if (sourceNode != null)
                    {
                        var sourceMax = CalculateNodeMaxThroughput(graph, sourceNode, inputItemId);
                        // 按比例缩放
                        var consumptionRate = node.GetActualInputRatePerMinute(inputItemId);
                        if (consumptionRate > 0)
                        {
                            maxRate = Math.Min(maxRate, sourceMax * (node.GetActualOutputRatePerMinute(targetItemId) / consumptionRate));
                        }
                    }
                }
            }

            return maxRate;
        }

        /// <summary>
        /// 生成时序报告摘要
        /// </summary>
        public string GenerateReport(ProductionGraph graph)
        {
            var timing = Analyze(graph);
            var criticalPath = FindCriticalPath(graph);

            var report = new System.Text.StringBuilder();
            report.AppendLine("=== AIC-EDA Throughput-STA Report ===");
            report.AppendLine($"分析时间: {DateTime.Now}");
            report.AppendLine($"总节点数: {graph.Nodes.Count}");
            report.AppendLine($"总边数: {graph.Edges.Count}");
            report.AppendLine();

            report.AppendLine("--- 关键路径 (Critical Path) ---");
            foreach (var node in criticalPath)
            {
                report.AppendLine($"  [{node.Layer}] {node.Recipe.Machine.GetDisplayName()} x{node.Count}");
            }
            report.AppendLine();

            report.AppendLine("--- 时序分析 (Timing Analysis) ---");
            report.AppendLine($"{"节点",-20} {"物品",-15} {"需求",-10} {"产能",-10} {"松弛",-10} {"利用率",-8}");
            foreach (var t in timing.Where(x => x.IsCritical))
            {
                report.AppendLine($"{t.MachineName,-20} {t.ItemId,-15} {t.RequiredRate,10:F1} {t.ActualRate,10:F1} {t.Slack,10:F1} {t.Utilization,8:P1}");
            }
            report.AppendLine();

            var violations = timing.Where(t => t.Slack < 0).ToList();
            report.AppendLine($"--- 违规数 (Violations): {violations.Count} ---");
            foreach (var v in violations)
            {
                report.AppendLine($"  [NEGATIVE SLACK] {v.MachineName} ({v.ItemId}): 松弛 = {v.Slack:F2}");
            }

            return report.ToString();
        }
    }
}
