using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIC_EDA.Core
{
    /// <summary>
    /// Flow Balancer（流量平衡器）- 计算最优产线配置
    /// 确保上下游流量匹配，最小化设备总数
    /// </summary>
    public class FlowBalancer
    {
        /// <summary>
        /// 平衡生产图：根据目标产量计算每个节点的精确设备数量
        /// </summary>
        /// <param name="graph">生产图</param>
        /// <param name="targetItemId">目标产物</param>
        /// <param name="targetRatePerMinute">目标每分钟产量</param>
        /// <returns>平衡后的图</returns>
        public ProductionGraph Balance(ProductionGraph graph, string targetItemId, double targetRatePerMinute)
        {
            // 从最终产物节点开始，反向计算每个节点的需求
            var demandMap = new Dictionary<Guid, Dictionary<string, double>>(); // nodeId -> {itemId -> requiredRate}

            // 找到生产目标产物的节点
            var targetNodes = graph.Nodes.Where(n => n.Recipe.Outputs.ContainsKey(targetItemId)).ToList();
            if (targetNodes.Count == 0)
                return graph;

            // 广度优先从目标节点反向传播需求
            var queue = new Queue<Guid>();
            foreach (var node in targetNodes)
            {
                if (!demandMap.ContainsKey(node.Id))
                    demandMap[node.Id] = new Dictionary<string, double>();
                demandMap[node.Id][targetItemId] = targetRatePerMinute;
                queue.Enqueue(node.Id);
            }

            var visited = new HashSet<Guid>();

            while (queue.Count > 0)
            {
                var nodeId = queue.Dequeue();
                if (visited.Contains(nodeId)) continue;
                visited.Add(nodeId);

                var node = graph.FindNode(nodeId);
                if (node == null) continue;

                // 计算此节点的总需求（所有需要此节点产出的物品速率之和）
                double totalRequiredOutput = 0;
                if (demandMap.TryGetValue(nodeId, out var demands))
                {
                    foreach (var itemDemand in demands)
                    {
                        var itemId = itemDemand.Key;
                        var rate = itemDemand.Value;
                        var outputRatePerMachine = node.Recipe.GetOutputRatePerMinute(itemId);
                        if (outputRatePerMachine > 0)
                        {
                            totalRequiredOutput = Math.Max(totalRequiredOutput, rate / outputRatePerMachine);
                        }
                    }
                }

                // 向上取整确定设备数量
                node.Count = Math.Max(1, (int)Math.Ceiling(totalRequiredOutput));

                // 计算此节点对所有输入物品的总需求
                var inputDemands = new Dictionary<string, double>();
                foreach (var input in node.Recipe.Inputs)
                {
                    var itemId = input.Key;
                    var ratePerMachine = node.Recipe.GetInputRatePerMinute(itemId);
                    var totalRate = ratePerMachine * node.Count;

                    // 如果此物品也是输出（可回收容器），跳过
                    if (node.Recipe.Outputs.ContainsKey(itemId))
                        continue;

                    inputDemands[itemId] = totalRate;
                }

                // 将需求传播到上游节点
                foreach (var edge in graph.GetInputEdges(nodeId))
                {
                    var sourceNode = graph.FindNode(edge.SourceId);
                    if (sourceNode == null) continue;

                    if (inputDemands.TryGetValue(edge.ItemId, out var requiredRate))
                    {
                        if (!demandMap.ContainsKey(sourceNode.Id))
                            demandMap[sourceNode.Id] = new Dictionary<string, double>();

                        if (demandMap[sourceNode.Id].ContainsKey(edge.ItemId))
                            demandMap[sourceNode.Id][edge.ItemId] += requiredRate;
                        else
                            demandMap[sourceNode.Id][edge.ItemId] = requiredRate;

                        queue.Enqueue(sourceNode.Id);
                    }
                }
            }

            // 更新边的速率
            foreach (var edge in graph.Edges)
            {
                var source = graph.FindNode(edge.SourceId);
                var target = graph.FindNode(edge.TargetId);
                if (source != null && target != null)
                {
                    edge.RatePerMinute = source.GetActualOutputRatePerMinute(edge.ItemId);
                    // 限制为下游实际需求
                    var targetNeed = target.GetActualInputRatePerMinute(edge.ItemId);
                    edge.RatePerMinute = Math.Min(edge.RatePerMinute, targetNeed);
                }
            }

            return graph;
        }

        /// <summary>
        /// 检测生产链中的瓶颈
        /// </summary>
        public List<BottleneckReport> DetectBottlenecks(ProductionGraph graph)
        {
            var bottlenecks = new List<BottleneckReport>();

            foreach (var node in graph.Nodes)
            {
                foreach (var output in node.Recipe.Outputs)
                {
                    var itemId = output.Key;
                    var theoreticalRate = node.GetActualOutputRatePerMinute(itemId);

                    // 计算下游总需求
                    var downstreamEdges = graph.Edges.Where(e => e.SourceId == node.Id && e.ItemId == itemId).ToList();
                    var downstreamDemand = downstreamEdges.Sum(e => e.RatePerMinute);

                    // 如果下游需求接近理论产能的95%，标记为潜在瓶颈
                    if (downstreamDemand > theoreticalRate * 0.95)
                    {
                        bottlenecks.Add(new BottleneckReport
                        {
                            NodeId = node.Id,
                            MachineName = node.Recipe.Machine.GetDisplayName(),
                            ItemId = itemId,
                            ItemName = itemId,
                            TheoreticalRate = theoreticalRate,
                            ActualDemand = downstreamDemand,
                            Utilization = downstreamDemand / theoreticalRate,
                            Severity = downstreamDemand > theoreticalRate ? BottleneckSeverity.Critical : BottleneckSeverity.Warning
                        });
                    }
                }
            }

            return bottlenecks.OrderByDescending(b => b.Utilization).ToList();
        }

        /// <summary>
        /// 计算每个节点的利用率
        /// </summary>
        public Dictionary<Guid, double> CalculateUtilization(ProductionGraph graph)
        {
            var utilization = new Dictionary<Guid, double>();

            foreach (var node in graph.Nodes)
            {
                double maxUtil = 0;
                foreach (var output in node.Recipe.Outputs)
                {
                    var itemId = output.Key;
                    var theoreticalRate = node.GetActualOutputRatePerMinute(itemId);
                    var downstreamDemand = graph.Edges
                        .Where(e => e.SourceId == node.Id && e.ItemId == itemId)
                        .Sum(e => e.RatePerMinute);

                    if (theoreticalRate > 0)
                    {
                        var util = downstreamDemand / theoreticalRate;
                        maxUtil = Math.Max(maxUtil, util);
                    }
                }
                utilization[node.Id] = Math.Min(1.0, maxUtil);
            }

            return utilization;
        }

        /// <summary>
        /// 重新计算并平衡整个图的所有节点数量
        /// </summary>
        public ProductionGraph RebalanceAll(ProductionGraph graph)
        {
            // 获取最终产物（最高层级的节点输出）
            var maxLayer = graph.Nodes.Max(n => n.Layer);
            var finalNodes = graph.Nodes.Where(n => n.Layer == maxLayer).ToList();

            foreach (var finalNode in finalNodes)
            {
                foreach (var output in finalNode.Recipe.Outputs)
                {
                    var targetRate = finalNode.GetActualOutputRatePerMinute(output.Key);
                    graph = Balance(graph, output.Key, targetRate);
                }
            }

            return graph;
        }
    }

    public enum BottleneckSeverity
    {
        Warning,    // 利用率>95%
        Critical    // 需求>产能
    }

    public class BottleneckReport
    {
        public Guid NodeId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string ItemId { get; set; } = string.Empty;
        public string ItemName { get; set; } = string.Empty;
        public double TheoreticalRate { get; set; }
        public double ActualDemand { get; set; }
        public double Utilization { get; set; }
        public BottleneckSeverity Severity { get; set; }

        public override string ToString()
        {
            return $"[{Severity}] {MachineName} 生产 {ItemName}: 利用率 {Utilization:P1} (产能{TheoreticalRate:F1}/需求{ActualDemand:F1})";
        }
    }
}
