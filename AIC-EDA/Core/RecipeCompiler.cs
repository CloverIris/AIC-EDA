using AIC_EDA.Models;
using AIC_EDA.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AIC_EDA.Core
{
    /// <summary>
    /// Recipe Compiler（配方编译器）- 将生产目标"综合"为设施网表
    /// 类比RTL到门级网表的逻辑综合
    /// </summary>
    public class RecipeCompiler
    {
        private readonly RecipeDatabaseService _db;

        public RecipeCompiler(RecipeDatabaseService? db = null)
        {
            _db = db ?? RecipeDatabaseService.Instance;
        }

        /// <summary>
        /// 从目标产物反向构建生产图
        /// </summary>
        /// <param name="targetItemId">目标产物ID</param>
        /// <param name="targetRatePerMinute">目标每分钟产量</param>
        /// <param name="preferredRecipes">优先使用的配方（可选）</param>
        /// <returns>生产依赖图</returns>
        public ProductionGraph Compile(string targetItemId, double targetRatePerMinute,
            Dictionary<string, string>? preferredRecipes = null)
        {
            var graph = new ProductionGraph
            {
                TargetItem = targetItemId,
                TargetRate = targetRatePerMinute
            };

            var nodeMap = new Dictionary<string, ProductionNode>(); // itemId -> 生产节点（主产出）
            var demandQueue = new Queue<(string itemId, double rate)>();
            var processed = new HashSet<string>();

            // 初始需求
            demandQueue.Enqueue((targetItemId, targetRatePerMinute));

            while (demandQueue.Count > 0)
            {
                var (itemId, requiredRate) = demandQueue.Dequeue();

                // 如果已经有节点生产此物品，增加其产能需求
                if (nodeMap.TryGetValue(itemId, out var existingNode))
                {
                    // 已在前面处理过，跳过（在平衡阶段统一计算数量）
                    continue;
                }

                // 查找生产此物品的配方
                var recipes = _db.FindRecipesByOutput(itemId);
                if (recipes.Count == 0)
                {
                    // 无配方 = 原始资源
                    graph.RawMaterialRequirements[itemId] = requiredRate;
                    continue;
                }

                // 选择配方（优先使用用户指定的，否则使用第一个主配方）
                Recipe selectedRecipe;
                if (preferredRecipes != null && preferredRecipes.TryGetValue(itemId, out var prefId))
                {
                    selectedRecipe = recipes.FirstOrDefault(r => r.Id == prefId) ?? recipes[0];
                }
                else
                {
                    selectedRecipe = recipes.FirstOrDefault(r => r.IsPrimary) ?? recipes[0];
                }

                // 创建生产节点
                var node = new ProductionNode
                {
                    Recipe = selectedRecipe,
                    Count = 1 // 先设为1，后续由FlowBalancer精确计算
                };

                graph.Nodes.Add(node);
                nodeMap[itemId] = node;

                // 递归处理所有输入需求
                foreach (var input in selectedRecipe.Inputs)
                {
                    var inputItemId = input.Key;
                    var inputAmountPerCraft = input.Value;
                    var inputRatePerMinute = selectedRecipe.GetInputRatePerMinute(inputItemId) * node.Count;

                    // 如果输入物品是可回收容器（如瓶子），特殊处理
                    if (selectedRecipe.Outputs.ContainsKey(inputItemId))
                    {
                        // 容器回收，不需要额外生产
                        continue;
                    }

                    demandQueue.Enqueue((inputItemId, inputRatePerMinute));
                }
            }

            // 建立边连接（根据物品流）
            BuildEdges(graph, nodeMap);

            // 拓扑分层
            AssignLayers(graph);

            return graph;
        }

        /// <summary>
        /// 为指定节点集合建立连接边
        /// </summary>
        private void BuildEdges(ProductionGraph graph, Dictionary<string, ProductionNode> nodeMap)
        {
            foreach (var node in graph.Nodes)
            {
                foreach (var input in node.Recipe.Inputs)
                {
                    var inputItemId = input.Key;

                    // 查找生产此输入物品的节点
                    if (nodeMap.TryGetValue(inputItemId, out var sourceNode))
                    {
                        var edge = new ProductionEdge
                        {
                            SourceId = sourceNode.Id,
                            TargetId = node.Id,
                            ItemId = inputItemId,
                            RatePerMinute = node.Recipe.GetInputRatePerMinute(inputItemId) * node.Count
                        };

                        graph.Edges.Add(edge);

                        // 更新节点连接字典
                        if (!sourceNode.OutputConnections.ContainsKey(node.Id))
                            sourceNode.OutputConnections[node.Id] = new List<string>();
                        sourceNode.OutputConnections[node.Id].Add(inputItemId);

                        if (!node.InputConnections.ContainsKey(sourceNode.Id))
                            node.InputConnections[sourceNode.Id] = new List<string>();
                        node.InputConnections[sourceNode.Id].Add(inputItemId);
                    }
                }
            }
        }

        /// <summary>
        /// 拓扑分层（0=最顶层原料，最大层=最终产物）
        /// </summary>
        private void AssignLayers(ProductionGraph graph)
        {
            // 计算每个节点的入度
            var inDegree = new Dictionary<Guid, int>();
            foreach (var node in graph.Nodes)
                inDegree[node.Id] = 0;

            foreach (var edge in graph.Edges)
            {
                if (inDegree.ContainsKey(edge.TargetId))
                    inDegree[edge.TargetId]++;
            }

            // Kahn算法拓扑排序并分层
            var queue = new Queue<(Guid id, int layer)>();
            foreach (var kvp in inDegree)
            {
                if (kvp.Value == 0)
                    queue.Enqueue((kvp.Key, 0));
            }

            var processed = new HashSet<Guid>();
            while (queue.Count > 0)
            {
                var (id, layer) = queue.Dequeue();
                processed.Add(id);

                var node = graph.FindNode(id);
                if (node != null)
                    node.Layer = layer;

                // 找到所有以id为源节点的边
                foreach (var edge in graph.Edges.Where(e => e.SourceId == id))
                {
                    var targetLayer = layer + 1;
                    var targetNode = graph.FindNode(edge.TargetId);
                    if (targetNode != null && targetNode.Layer < targetLayer)
                        targetNode.Layer = targetLayer;

                    inDegree[edge.TargetId]--;
                    if (inDegree[edge.TargetId] == 0)
                    {
                        queue.Enqueue((edge.TargetId, targetNode?.Layer ?? targetLayer));
                    }
                }
            }
        }

        /// <summary>
        /// 多目标编译 - 同时满足多个生产目标
        /// </summary>
        public ProductionGraph CompileMultiTarget(Dictionary<string, double> targets,
            Dictionary<string, string>? preferredRecipes = null)
        {
            // 为每个目标分别编译，然后合并
            var mergedGraph = new ProductionGraph
            {
                TargetItem = "[Multi-Target]",
                TargetRate = targets.Values.Sum()
            };

            foreach (var target in targets)
            {
                var subGraph = Compile(target.Key, target.Value, preferredRecipes);
                MergeGraph(mergedGraph, subGraph);
            }

            // 重新计算拓扑层
            var nodeMap = mergedGraph.Nodes.ToDictionary(
                n => n.Recipe.Outputs.Keys.First(), n => n);
            BuildEdges(mergedGraph, nodeMap);
            AssignLayers(mergedGraph);

            return mergedGraph;
        }

        private void MergeGraph(ProductionGraph target, ProductionGraph source)
        {
            // 合并节点（相同配方的节点需要合并）
            foreach (var srcNode in source.Nodes)
            {
                var existing = target.Nodes.FirstOrDefault(n =>
                    n.Recipe.Id == srcNode.Recipe.Id);

                if (existing != null)
                {
                    // 合并数量需求（后续由FlowBalancer精确计算）
                    existing.Count = Math.Max(existing.Count, srcNode.Count);
                }
                else
                {
                    target.Nodes.Add(srcNode);
                }
            }

            // 合并原始资源需求
            foreach (var res in source.RawMaterialRequirements)
            {
                if (target.RawMaterialRequirements.ContainsKey(res.Key))
                    target.RawMaterialRequirements[res.Key] += res.Value;
                else
                    target.RawMaterialRequirements[res.Key] = res.Value;
            }
        }
    }
}
