using System;
using System.Collections.Generic;
using System.Linq;

namespace AIC_EDA.Models
{
    /// <summary>
    /// 生产依赖图 - 整个工厂的网络表示
    /// </summary>
    public class ProductionGraph
    {
        /// <summary>目标产物ID</summary>
        public string TargetItem { get; set; } = string.Empty;

        /// <summary>目标每分钟产量</summary>
        public double TargetRate { get; set; }

        /// <summary>所有生产节点</summary>
        public List<ProductionNode> Nodes { get; set; } = new();

        /// <summary>所有连接边</summary>
        public List<ProductionEdge> Edges { get; set; } = new();

        /// <summary>原始资源需求（矿石等）</summary>
        public Dictionary<string, double> RawMaterialRequirements { get; set; } = new();

        /// <summary>总电力消耗</summary>
        public double TotalPowerConsumption => Nodes.Sum(n => n.TotalPowerConsumption);

        /// <summary>总设备数量</summary>
        public int TotalMachineCount => Nodes.Sum(n => n.Count);

        /// <summary>按拓扑层获取节点</summary>
        public List<IGrouping<int, ProductionNode>> GetLayers()
        {
            return Nodes.GroupBy(n => n.Layer).OrderBy(g => g.Key).ToList();
        }

        /// <summary>获取节点的输入边</summary>
        public List<ProductionEdge> GetInputEdges(Guid nodeId)
        {
            return Edges.Where(e => e.TargetId == nodeId).ToList();
        }

        /// <summary>获取节点的输出边</summary>
        public List<ProductionEdge> GetOutputEdges(Guid nodeId)
        {
            return Edges.Where(e => e.SourceId == nodeId).ToList();
        }

        /// <summary>查找节点</summary>
        public ProductionNode? FindNode(Guid id)
        {
            return Nodes.FirstOrDefault(n => n.Id == id);
        }

        /// <summary>获取节点的所有前置节点（递归）</summary>
        public List<ProductionNode> GetPredecessors(Guid nodeId)
        {
            var result = new List<ProductionNode>();
            var visited = new HashSet<Guid>();
            var queue = new Queue<Guid>();

            foreach (var edge in GetInputEdges(nodeId))
            {
                queue.Enqueue(edge.SourceId);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (visited.Contains(current)) continue;
                visited.Add(current);

                var node = FindNode(current);
                if (node != null)
                {
                    result.Add(node);
                    foreach (var edge in GetInputEdges(current))
                    {
                        queue.Enqueue(edge.SourceId);
                    }
                }
            }

            return result;
        }
    }
}
