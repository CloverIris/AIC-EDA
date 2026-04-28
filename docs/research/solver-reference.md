# 求解器参考资料

复杂求解器资料先完整收集，实际实现按阶段逐步接入。AIC-EDA 的默认交互路径应先保持确定性、可解释；高级求解器用于精确小规模案例、优化模式、验证 oracle 和离线批处理。

## OR / 约束求解器

| 求解器 / 工具 | URL | 许可证 / 状态 | .NET 可用性 | 解决问题 | AIC-EDA 用途 | 阶段 |
|---|---|---|---|---|---|---|
| Google OR-Tools | https://github.com/google/or-tools | Apache-2.0 | 官方 C# / NuGet | CP-SAT、routing、assignment、min-cost flow、bin packing | 精确小布局、分配、供电覆盖、流量分配 | 第一集成候选 |
| OR-Tools CP-SAT | https://developers.google.com/optimization/cp/cp_solver | Apache-2.0 | 优秀 | 整数约束、可选区间、no-overlap | 机器放置、旋转、供电桩覆盖 | 优先原型 |
| OR-Tools 图/流模块 | https://developers.google.com/optimization/flow | Apache-2.0 | 良好 | 最小费用流、最大流、assignment | 供需匹配、通道分配 | 按需使用 |
| HiGHS | https://github.com/ERGO-Code/HiGHS | MIT | 存在 C# interface / native packages | LP、QP、MIP | 流量平衡、容量估算、LP/MIP 后端 | 评估 |
| SCIP | https://github.com/scipopt/scip | Apache-2.0 | 直接 .NET 支持弱 | MIP、MINLP、constraint integer programming | 困难精确实验 | 研究 / 可选后端 |
| CBC / COIN-OR | https://github.com/coin-or/Cbc | EPL-2.0 | wrapper / CLI | MILP | OSS MILP baseline | 备选 |
| Z3 | https://github.com/Z3Prover/z3 | MIT | `Microsoft.Z3` NuGet | SMT / SAT / 线性约束 | DRC 可满足性、小规模精确检查 | 规则研究 |
| MiniZinc | https://www.minizinc.org/doc-latest/en/ | MPL-2.0 | C# 可通过 CLI 调用 | 求解器无关建模 | 快速试验布局/覆盖模型 | 研究层 |
| Chuffed | https://github.com/chuffed/chuffed | MIT | 通过 MiniZinc 使用 | Lazy clause generation CP | no-overlap、调度、装箱 | MiniZinc 后端 |
| GLPK | https://www.gnu.org/software/glpk/ | GPL-3.0 | 存在 wrapper | LP/MIP | 教学参考 | 避免作为必要依赖 |
| Gurobi | https://docs.gurobi.com/ | 商业 | 官方 .NET | 高性能 LP/MIP/QP | 性能 baseline | 可选商业参考 |
| IBM CPLEX | https://www.ibm.com/products/ilog-cplex-optimization-studio | 商业 | 官方 .NET 生态 | LP/MIP/CP | 性能 baseline | 可选商业参考 |
| Timefold Solver | https://github.com/TimefoldAI/timefold-solver | Apache-2.0 CE | Java 服务/进程 | 约束规划 / 元启发式 | 大规模启发式分配/路由 | 外部服务参考 |

## 高级布局与 floorplanning

| 参考 | URL | 解决问题 | AIC-EDA 用途 | 难度 | 阶段 |
|---|---|---|---|---|---|
| MaxRects / Skyline / Guillotine | https://github.com/secnot/rectpack | 2D 矩形装箱 | 同层机器 footprint packing | 中 | 近期 |
| A Thousand Ways to Pack the Bin | https://github.com/secnot/rectpack#supported-algorithms | 矩形装箱综述 | 选择 packing heuristic | 中 | 参考 |
| CP-SAT NoOverlap2D | https://developers.google.com/optimization | 精确轴对齐矩形不重叠 | 小规模最优布局 / golden tests | 中高 | 高级 |
| Slicing floorplan / Polish expression | https://doi.org/10.1145/318013.318030 | guillotine/slicing floorplans | 区块级工厂布局 | 中 | 实验 |
| Sequence Pair | https://doi.org/10.1109/43.506438 | 非 slicing 矩形布局编码 | 机器相对顺序 | 高 | 研究 |
| B*-Tree | https://doi.org/10.1145/337292.337541 | 紧凑非 slicing floorplan | 紧凑生产单元 | 高 | 研究 |
| Fast-SA | https://doi.org/10.1109/TCAD.2006.870076 | 更快的 annealing floorplan | 固定蓝图范围优化 | 高 | 后期 |
| Abacus legalization | https://doi.org/10.1145/1353629.1353640 | 最小移动量的重叠合法化 | 修复 analytic/force placement | 中高 | 中期 |
| RePlAce | https://github.com/The-OpenROAD-Project/RePlAce | routability-aware analytic placement | 高级全局 placer 灵感 | 专家级 | 参考 |
| DREAMPlace | https://github.com/limbo018/DREAMPlace | GPU/analytic placement、legalization | density map 和 legalization 参考 | 专家级 | 参考 |
| Xplace | https://github.com/cuhk-eda/Xplace | 快速 GPU placement framework | 插件式 placer 架构 | 专家级 | 参考 |
| p-median / p-center | https://github.com/pysal/spopt | 设施选址 | 仓储、hub、供电桩、中继器 | 中 | 中期 |

## 高级路由与网络流

| 参考 | URL | 解决问题 | AIC-EDA 用途 | 难度 | 阶段 |
|---|---|---|---|---|---|
| A* routing | https://en.wikipedia.org/wiki/A*_search_algorithm | 启发式最短路 | 单条传送带/管道路径 | 低中 | MVP |
| Lee maze routing | https://doi.org/10.1109/TEC.1961.5219222 | 完备网格路由 | fallback router | 中 | MVP+ |
| Hadlock routing | https://doi.org/10.1145/800139.804503 | minimum-detour grid routing | 稀疏障碍更快路由 | 中 | MVP+ |
| Soukup router | https://bibtex.github.io/DAC-1978-Soukup.html | 快速 maze routing | 稀疏工厂路由实验 | 中 | 实验 |
| Rip-up and reroute | https://en.wikipedia.org/wiki/Routing_%28electronic_design_automation%29 | 重布冲突网络 | 解决传送带/管道冲突 | 中 | 中期 |
| PathFinder negotiated congestion | https://doi.org/10.1145/201310.201328 | 基于历史成本的拥塞路由 | 多传送带共享网格 | 高 | 高级 |
| VTR / VPR router | https://docs.verilogtorouting.org/en/latest/vpr/ | FPGA 网格路由 | routing resource graph 和诊断 | 高 | 架构参考 |
| FastRoute / OpenROAD GRT | https://openroad.readthedocs.io/en/latest/main/src/grt/README.html | 全局 routing guide 和拥塞 | 粗粒度物流通道 | 中高 | 中期 |
| TritonRoute / OpenROAD DRT | https://openroad.readthedocs.io/en/latest/main/src/drt/README.html | DRC-aware detailed routing | 精确路径合法化 | 高 | 高级 |
| FLUTE / RSMT | https://doi.org/10.1109/TCAD.2007.905044 | 多端点 rectilinear Steiner tree | 电力主干和共享总线 | 高 | 高级 |
| Min-cost flow | https://developers.google.com/optimization/flow/mincostflow | 容量约束下的最小成本分配 | 通道分配、供需匹配 | 中 | 中期 |
| Multi-commodity flow | https://www.cs.jhu.edu/~mdinitz/classes/ApproxAlgorithms/Spring2019/Lectures/lecture12.pdf | 多商品共享容量 | 多物料物流优化 | 专家级 | 研究 |
| FreeRouting | https://github.com/freerouting/freerouting | PCB autorouting | 类电路板路由参考 | 中高 | GPL 概念参考 |
| KiCad push-and-shove | https://github.com/KiCad/kicad-source-mirror | 交互式 PCB routing | 手动碰撞感知传送带编辑 | 高 | GPL UX 参考 |

## 仿真与元启发式

| 参考 | URL | 许可证 / 状态 | 解决问题 | AIC-EDA 用途 | .NET 可用性 | 阶段 |
|---|---|---|---|---|---|---|
| SimSharp | https://github.com/heal-research/SimSharp | MIT | process-based DES | 机器、缓冲、传送带、队列、堵塞 | 高 | DES 首选候选 |
| O2DES.Net | https://github.com/li-haobin/O2DESNet | MIT | object-oriented DES | 模块化机器/运输状态 | 高 | DES 备选 |
| SimPy | https://simpy.readthedocs.io/ | MIT | canonical Python DES | API/语义参考 | 概念参考 | 参考 |
| FactorySimPy | https://github.com/FactorySimPy/FactorySimPy | MIT | manufacturing DES components | 机器/传送带抽象 | 概念参考 | 参考 |
| OpenFactoryTwin | https://github.com/OpenFactoryTwin/ofact | Apache-2.0 | production/logistics digital twin | 场景、KPI、物料流 | 概念参考 | 架构参考 |
| QueueSim | https://github.com/A-Herzog/QueueSim | Apache-2.0 | queueing networks | 缓冲近似验证 | 概念参考 | 参考 |
| GeneticSharp | https://github.com/giacomelli/GeneticSharp | MIT | C# genetic algorithms | 布局 genome / fitness 实验 | 高 | 实验 |
| cs-moea | https://github.com/chen0040/cs-moea | MIT | C# multi-objective evolutionary algorithms | 面积/产能/电力/长度 Pareto | 中 | 研究 |
| pymoo | https://github.com/anyoptimization/pymoo | Apache-2.0 | multi-objective optimization | benchmark 和算法比较 | 概念参考 | 研究 |
| jMetal | https://github.com/jMetal/jMetal | MIT | multi-objective metaheuristics | Pareto 指标和实验设计 | 概念参考 | 研究 |
| OR-Tools local search | https://developers.google.com/optimization/routing/routing_options | Apache-2.0 | guided local search、SA、tabu | 路由和布局 neighborhood 优化 | 高 | 中高级 |

## 建议分阶段接入

1. **MVP 核心**：确定性 graph + flow + placement + A* + DRC。
2. **第一批求解器集成**：OR-Tools CP-SAT 用于小规模 no-overlap/coverage；OR-Tools MinCostFlow 用于供需/通道分配。
3. **优化实验室**：HiGHS、MiniZinc、Z3、GeneticSharp 用于实验和验证。
4. **高级路由**：rip-up/reroute、PathFinder 历史成本、global/detailed routing split。
5. **仿真模式**：SimSharp DES，配合固定随机种子和 KPI 报告。
