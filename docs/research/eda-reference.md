# EDA 参考资料

本文记录 EDA 工具链、开源项目和理论基础如何映射到《终末地》AIC 工业布局问题。

## 流程映射

| EDA 概念 | IC 设计含义 | AIC-EDA 对应概念 | 推荐术语 |
|---|---|---|---|
| Specification / RTL | 行为/逻辑意图 | 目标产物、产能、可用配方、区域限制 | Factory Spec / Recipe Spec |
| Synthesis | RTL 到网表 | 目标产物到生产依赖图 | Recipe Synthesis |
| Netlist | 由连线连接的单元图 | 由物料流连接的机器/工序图 | Production Netlist |
| Library | 标准单元、宏、时序/功耗数据 | 配方、机器、端口、电力、占地 | Process Library / Machine Library |
| Constraints | 时序、功耗、面积、I/O 约束 | 产能、区域、电力、传送带/管道容量约束 | Factory Constraints |
| Floorplanning | 芯片区域、宏单元、电源域规划 | 采矿、加工、仓储、电力、物流通道分区 | Site Floorplan |
| Placement | 合法物理位置 | 设备位置、旋转、端口朝向 | Machine Placement |
| Routing | 金属线和布线轨道 | 传送带、管道、电力路径 | Logistics Routing |
| STA | 时序收敛、松弛分析 | 产能松弛、关键生产链 | Throughput STA |
| DRC | 物理规则检查 | 碰撞、间距、网格、端口、路径、电力检查 | Factory DRC / DRC-End |
| LVS | 版图与原理图一致性 | 已放置/已布线布局与生产图一致性 | Flow-LVS / Netlist-vs-Layout |
| Signoff | GDS 前最终验证 | 蓝图导出门禁 | Blueprint Signoff |

## 开源 EDA 项目

| 项目 | URL | 领域 | 可复用思想 | 优先级 |
|---|---|---|---|---|
| OpenROAD | https://github.com/The-OpenROAD-Project/OpenROAD | ASIC 布局布线 | 分阶段流程、工件、QoR 报告、signoff | P0 |
| OpenROAD-flow-scripts | https://github.com/The-OpenROAD-Project/OpenROAD-flow-scripts | 流程编排 | checkpoint、可复现实验配置、metrics | P1 |
| Yosys | https://github.com/YosysHQ/yosys | RTL 综合 | pass-based compiler 和 canonical IR | P0 |
| OpenSTA | https://github.com/The-OpenROAD-Project/OpenSTA | STA | 路径、松弛、关键路径报告术语 | P0 概念参考，GPL |
| OpenTimer | https://github.com/OpenTimer/OpenTimer | STA | 增量时序图 | P2 |
| VTR / VPR | https://github.com/verilog-to-routing/vtr-verilog-to-routing | FPGA 布局布线 | 网格资源图、布局/路由诊断 | P0 |
| nextpnr | https://github.com/YosysHQ/nextpnr | FPGA 布局布线 | device-agnostic target architecture model | P0 |
| FastRoute | https://github.com/The-OpenROAD-Project-Attic/FastRoute | 全局布线 | routing guide 和拥塞图 | P1 |
| TritonRoute | https://github.com/The-OpenROAD-Project/TritonRoute | 详细布线 | pin access、DRC-aware search-and-repair | P2 |
| KLayout | https://github.com/KLayout/klayout | 版图查看 / DRC / LVS | 分层几何 UI 和规则检查 | P2 概念参考，GPL |
| Magic VLSI | https://github.com/RTimothyEdwards/magic | 版图编辑 / DRC | 网格编辑和即时 DRC 反馈 | P2 |
| ELK | https://github.com/eclipse-elk/elk | 图布局 | 端口感知的分层图布局 | P0 概念参考 |
| NetworkX | https://github.com/networkx/networkx | 图算法 | DAG、流、最短路、Steiner 参考 | P1 算法参考 |

## 理论阅读清单

| 主题 | 来源 | URL / DOI | AIC-EDA 映射 | 优先级 |
|---|---|---|---|---|
| 物理设计算法 | Algorithms for VLSI Physical Design Automation | https://link.springer.com/book/10.1007/978-1-4615-2351-2 | 布局、路由、分区 | P0 |
| EDA 流程 | Electronic Design Automation for IC Implementation | https://link.springer.com/book/10.1007/978-0-387-71829-2 | 端到端工具链 | P0 |
| 图分区 | Fiduccia-Mattheyses | https://doi.org/10.1145/800263.809204 | 将大型工厂拆分成生产区块 | P1 |
| Floorplanning | Sequence Pair | https://doi.org/10.1109/43.506438 | 非 slicing 矩形布局 | P0 高级 |
| Floorplanning | B*-Tree | https://doi.org/10.1145/337292.337541 | 紧凑区块布局 | P0 高级 |
| Slicing floorplan | Wong and Liu / normalized Polish expression | https://doi.org/10.1109/TCAD.1986.1270212 | 更简单的高级 floorplanner | P1 |
| Placement | RePlAce | https://doi.org/10.1109/TCAD.2018.2859220 | routability-aware global placement | P3 |
| Routing | Lee maze routing | https://doi.org/10.1109/TEC.1961.5219222 | 网格传送带/管道寻路 | P0 |
| Routing | Hadlock minimum detour | https://doi.org/10.1145/800139.804503 | 更快的网格路由变体 | P1 |
| Congestion | PathFinder | https://doi.org/10.1145/201310.201328 | 多网络协商拥塞路由 | P0 高级 |
| Graph drawing | Sugiyama layered drawing | https://doi.org/10.1109/TSMC.1981.4308636 | 配方图布局 | P0 |
| Graph drawing | Gansner DOT | https://doi.org/10.1109/32.221135 | 分层图绘制 | P0 |
| Facility layout | Quadratic Assignment Problem | https://doi.org/10.2307/1907530 | flow-distance placement cost | P1 |
| Manufacturing | Factory Physics | https://www.factoryphysics.com/ | 产能/瓶颈理论 | P0 |
| Queueing | Little's Law | https://doi.org/10.1287/opre.9.3.383 | 缓冲、延迟、速率关系 | P0 |
| Simulation | Simulation Modeling and Analysis | https://www.mheducation.com/highered/product/simulation-modeling-analysis-law/M9780073401324.html | 离散事件仿真验证方法 | P1 |
| Metaheuristic | Simulated Annealing | https://doi.org/10.1126/science.220.4598.671 | 布局优化 | P0 高级 |
| Multi-objective | NSGA-II | https://doi.org/10.1109/4235.996017 | Pareto 布局权衡 | P1 |

## 推荐实现映射

| 阶段 | 实用技术 | 高级参考 |
|---|---|---|
| 配方综合 | typed DAG + topological sort | Yosys pass pipeline |
| 流量平衡 | 守恒方程 + slack | OpenSTA/OpenTimer report style |
| 示意图布局 | Sugiyama 分层图 | ELK / Dagre |
| MVP 布局 | 拓扑列 + 矩形装箱 + legalizer | MaxRects / Abacus |
| 高级布局 | CP-SAT no-overlap、sequence pair、B*-tree、SA | OR-Tools、floorplanning papers |
| MVP 路由 | A* + Lee fallback | Lee/Hadlock |
| 高级路由 | rip-up/reroute、PathFinder、global/detailed split | VTR、FastRoute、TritonRoute |
| 验证 | DRC rules + Flow-LVS + Blueprint Signoff | KLayout、Magic、OpenROAD signoff |
