# 研究资料库

本目录沉淀 AIC-EDA 重写前需要长期维护的资料库。目标不是把外部资料原样搬进仓库，而是建立可追溯、可验证、可复用的参考索引。

## 文档索引

| 文档 | 内容 |
|---|---|
| [endfield-aic-sources.md](endfield-aic-sources.md) | 《明日方舟：终末地》AIC 机制、设施、物流、电力、配方资料源 |
| [endfield-data-sources.md](endfield-data-sources.md) | 社区数据库、结构化数据、Endfield 相关 OSS 工具与许可风险 |
| [blueprint-research.md](blueprint-research.md) | 蓝图、分享码、社区布局库、导入导出机制研究 |
| [eda-reference.md](eda-reference.md) | EDA 工具链、开源项目、理论基础与 AIC-EDA 映射 |
| [solver-reference.md](solver-reference.md) | OR-Tools、MILP、CP-SAT、布局、路由、仿真与高级求解器资料 |
| [data-provenance-schema.md](data-provenance-schema.md) | 未来数据导入的来源、版本、置信度、许可字段规范 |

## 资料使用原则

1. **官方优先，社区交叉验证**：官方工具/公告用于版本和命名；社区数据库用于候选数值，必须交叉验证。
2. **代码和数据分开看待**：开源代码许可证不自动授权复制游戏数据或资产。
3. **所有游戏数值必须带来源**：配方、设备尺寸、端口、电力、物流速度等字段都需要 `sourceUrl` 和 `checkedAt`。
4. **蓝图分享码先当不透明字符串**：原生 `EFO...` 格式未公开前，不声明可解码或兼容。
5. **复杂求解器先作为资料库**：实现时分阶段接入，但资料先完整收集。
