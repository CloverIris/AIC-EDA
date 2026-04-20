using AIC_EDA.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AIC_EDA.Core
{
    /// <summary>
    /// DRC-End（设计规则检查）- 验证布局是否符合游戏物理约束
    /// 类比IC设计中的Design Rule Checking (DRC)
    /// </summary>
    public class DRCValidator
    {
        public class DRCViolation
        {
            public string RuleId { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public Guid? NodeId { get; set; }
            public string? NodeName { get; set; }
            public ViolationSeverity Severity { get; set; }
            public string? SuggestedFix { get; set; }
        }

        public enum ViolationSeverity
        {
            Info,
            Warning,
            Error,
            Critical
        }

        private readonly List<DRCRule> _rules = new();

        public DRCValidator()
        {
            InitializeRules();
        }

        private void InitializeRules()
        {
            _rules.Add(new DRCRule("DRC-001", "设备间距检查", CheckSpacing));
            _rules.Add(new DRCRule("DRC-002", "传送带最大长度检查", CheckBeltLength));
            _rules.Add(new DRCRule("DRC-003", "电力覆盖检查", CheckPowerCoverage));
            _rules.Add(new DRCRule("DRC-004", "碰撞检测", CheckCollision));
            _rules.Add(new DRCRule("DRC-005", "网格对齐检查", CheckGridAlignment));
            _rules.Add(new DRCRule("DRC-006", "输入输出连接检查", CheckPortConnectivity));
        }

        /// <summary>
        /// 执行完整DRC检查
        /// </summary>
        public List<DRCViolation> Validate(ProductionGraph graph)
        {
            var violations = new List<DRCViolation>();

            foreach (var rule in _rules)
            {
                var ruleViolations = rule.Check(graph);
                violations.AddRange(ruleViolations);
            }

            return violations.OrderByDescending(v => v.Severity).ToList();
        }

        /// <summary>
        /// DRC-001: 设备间距检查（最小间距1格）
        /// </summary>
        private List<DRCViolation> CheckSpacing(ProductionGraph graph)
        {
            var violations = new List<DRCViolation>();
            var nodes = graph.Nodes.Where(n => n.Position != null).ToList();

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    var a = nodes[i];
                    var b = nodes[j];
                    var specA = MachineSpecDatabase.GetSpec(a.Recipe.Machine);
                    var specB = MachineSpecDatabase.GetSpec(b.Recipe.Machine);

                    var dist = Vector3.Distance(a.Position!.Value, b.Position!.Value);
                    var minDist = (specA?.Width ?? 2) / 2 + (specB?.Width ?? 2) / 2 + 1.0;

                    if (dist < minDist)
                    {
                        violations.Add(new DRCViolation
                        {
                            RuleId = "DRC-001",
                            Description = $"设备间距不足: {a.Recipe.Machine.GetDisplayName()} 与 {b.Recipe.Machine.GetDisplayName()} 距离 {dist:F1} < 最小 {minDist:F1}",
                            NodeId = a.Id,
                            NodeName = a.DisplayName,
                            Severity = ViolationSeverity.Error,
                            SuggestedFix = "移动设备以增加间距"
                        });
                    }
                }
            }

            return violations;
        }

        /// <summary>
        /// DRC-002: 传送带最大长度检查（假设50格）
        /// </summary>
        private List<DRCViolation> CheckBeltLength(ProductionGraph graph)
        {
            var violations = new List<DRCViolation>();
            const double MaxBeltLength = 50.0;

            foreach (var edge in graph.Edges)
            {
                var source = graph.FindNode(edge.SourceId);
                var target = graph.FindNode(edge.TargetId);
                if (source?.Position == null || target?.Position == null) continue;

                var dist = Vector3.Distance(source.Position.Value, target.Position.Value);
                if (dist > MaxBeltLength)
                {
                    violations.Add(new DRCViolation
                    {
                        RuleId = "DRC-002",
                        Description = $"传送带过长: {source.Recipe.Machine.GetDisplayName()} -> {target.Recipe.Machine.GetDisplayName()} 距离 {dist:F1} > 最大 {MaxBeltLength}",
                        NodeId = source.Id,
                        NodeName = source.DisplayName,
                        Severity = ViolationSeverity.Warning,
                        SuggestedFix = "添加中继物流节点或调整布局"
                    });
                }
            }

            return violations;
        }

        /// <summary>
        /// DRC-003: 电力覆盖检查
        /// </summary>
        private List<DRCViolation> CheckPowerCoverage(ProductionGraph graph)
        {
            var violations = new List<DRCViolation>();
            var powerNodes = graph.Nodes.Where(n =>
                n.Recipe.Machine == MachineType.PowerPylon ||
                n.Recipe.Machine == MachineType.ProtocolCore ||
                n.Recipe.Machine == MachineType.RelayTower).ToList();

            foreach (var node in graph.Nodes)
            {
                if (node.Recipe.Machine.GetCategory() == MachineCategory.Power) continue;
                if (node.Position == null) continue;

                bool hasPower = false;
                var spec = MachineSpecDatabase.GetSpec(node.Recipe.Machine);
                var powerNeed = spec?.PowerRadius ?? 15;

                foreach (var power in powerNodes)
                {
                    if (power.Position == null) continue;
                    var powerSpec = MachineSpecDatabase.GetSpec(power.Recipe.Machine);
                    var powerRadius = powerSpec?.PowerRadius ?? 20;

                    var dist = Vector3.Distance(node.Position.Value, power.Position.Value);
                    if (dist <= powerRadius)
                    {
                        hasPower = true;
                        break;
                    }
                }

                if (!hasPower)
                {
                    violations.Add(new DRCViolation
                    {
                        RuleId = "DRC-003",
                        Description = $"设备无电力覆盖: {node.Recipe.Machine.GetDisplayName()} @ {node.Position}",
                        NodeId = node.Id,
                        NodeName = node.DisplayName,
                        Severity = ViolationSeverity.Critical,
                        SuggestedFix = "在设备附近添加供电桩或中继器"
                    });
                }
            }

            return violations;
        }

        /// <summary>
        /// DRC-004: 碰撞检测（设备重叠）
        /// </summary>
        private List<DRCViolation> CheckCollision(ProductionGraph graph)
        {
            var violations = new List<DRCViolation>();
            var nodes = graph.Nodes.Where(n => n.Position != null).ToList();

            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    var a = nodes[i];
                    var b = nodes[j];
                    var specA = MachineSpecDatabase.GetSpec(a.Recipe.Machine);
                    var specB = MachineSpecDatabase.GetSpec(b.Recipe.Machine);

                    var dx = Math.Abs(a.Position!.Value.X - b.Position!.Value.X);
                    var dz = Math.Abs(a.Position!.Value.Z - b.Position!.Value.Z);
                    var minDx = (float)((specA?.Width ?? 2) / 2 + (specB?.Width ?? 2) / 2);
                    var minDz = (float)((specA?.Depth ?? 2) / 2 + (specB?.Depth ?? 2) / 2);

                    if (dx < minDx && dz < minDz)
                    {
                        violations.Add(new DRCViolation
                        {
                            RuleId = "DRC-004",
                            Description = $"设备碰撞: {a.Recipe.Machine.GetDisplayName()} 与 {b.Recipe.Machine.GetDisplayName()} 重叠",
                            NodeId = a.Id,
                            NodeName = a.DisplayName,
                            Severity = ViolationSeverity.Critical,
                            SuggestedFix = "立即分离重叠设备"
                        });
                    }
                }
            }

            return violations;
        }

        /// <summary>
        /// DRC-005: 网格对齐检查
        /// </summary>
        private List<DRCViolation> CheckGridAlignment(ProductionGraph graph)
        {
            var violations = new List<DRCViolation>();

            foreach (var node in graph.Nodes)
            {
                if (node.Position == null) continue;
                var spec = MachineSpecDatabase.GetSpec(node.Recipe.Machine);
                if (spec?.GridAligned != true) continue;

                var x = node.Position.Value.X;
                var z = node.Position.Value.Z;
                var gridSize = 1.0f;

                if (Math.Abs(x % gridSize) > 0.01 || Math.Abs(z % gridSize) > 0.01)
                {
                    violations.Add(new DRCViolation
                    {
                        RuleId = "DRC-005",
                        Description = $"设备未对齐网格: {node.Recipe.Machine.GetDisplayName()} @ ({x:F2}, {z:F2})",
                        NodeId = node.Id,
                        NodeName = node.DisplayName,
                        Severity = ViolationSeverity.Info,
                        SuggestedFix = $"对齐到 ({Math.Round(x):F0}, {Math.Round(z):F0})"
                    });
                }
            }

            return violations;
        }

        /// <summary>
        /// DRC-006: 输入输出连接检查
        /// </summary>
        private List<DRCViolation> CheckPortConnectivity(ProductionGraph graph)
        {
            var violations = new List<DRCViolation>();

            foreach (var node in graph.Nodes)
            {
                // 检查输入是否都有来源
                foreach (var input in node.Recipe.Inputs)
                {
                    var itemId = input.Key;
                    if (node.Recipe.Outputs.ContainsKey(itemId)) continue; // 可回收

                    var hasSource = graph.Edges.Any(e => e.TargetId == node.Id && e.ItemId == itemId);
                    if (!hasSource)
                    {
                        violations.Add(new DRCViolation
                        {
                            RuleId = "DRC-006",
                            Description = $"输入无来源: {node.Recipe.Machine.GetDisplayName()} 缺少 {itemId}",
                            NodeId = node.Id,
                            NodeName = node.DisplayName,
                            Severity = ViolationSeverity.Warning,
                            SuggestedFix = "添加上游生产节点或连接总线"
                        });
                    }
                }
            }

            return violations;
        }

        private record DRCRule(string Id, string Name, Func<ProductionGraph, List<DRCViolation>> Check);
    }
}
