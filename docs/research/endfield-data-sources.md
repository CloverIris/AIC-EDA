# 终末地数据源与开源工具

检查日期：2026-04-28。本文列出可用于 AIC-EDA 数据建模、交叉验证、算法参考的社区数据库和开源项目。许可为 `NOASSERTION`、`Other`、GPL/AGPL 的项目默认只做参考，不直接复用代码或数据。

## 结构化数据与数据库

| 优先级 | 来源 | URL | 结构化程度 | 覆盖内容 | 许可/条款备注 | AIC-EDA 用途 |
|---|---|---|---|---|---|---|
| P0 | `sssxks/end-cli` | https://github.com/sssxks/end-cli | TOML | 物品、配方、设施、电力配方 | Apache-2.0 | 结构化 baseline，适合交叉验证 recipe/facility/power |
| P0 | `555me/beyondGameData` | https://github.com/555me/beyondGameData | JSON / 原始表 | `FactoryMachineCraftTable`、`FactoryBuildingTable`、`FactoryPowerPoleTable`、`FactoryGridBeltTable` 等 | 未发现明确许可证 | 原始表高价值，但法律谨慎，只作验证候选 |
| P0 | EndfieldTools recipes | https://endfieldtools.dev/factory-planner/recipes/ | 可能是 Web app 数据 | 配方 / planner | 条款不明 | 配方候选、planner UX 对照 |
| P0 | Endfield DB calculator | https://endfielddb.com/calculator/ | 可能是 Web app 数据 | 物品、设施数量、电力、循环 | Fan site，all rights reserved | 产能和设备数交叉验证，不批量复制 |
| P0 | Talos Hub facilities | https://endfieldhub.org/database/facilities | HTML 卡片 | 设施、范围、描述 | Fan site | 设施目录和范围交叉验证 |
| P0 | wiki.gg AIC | https://endfield.wiki.gg/wiki/Automated_Industry_Complex | MediaWiki | 概念、设施分类、PAC/仓储机制 | CC BY-SA 4.0 | 机制说明和词表，注意署名/相同方式共享 |
| P1 | Game8 facility pages | https://game8.co/games/Arknights-Endfield/archives/536215 | HTML 表格 | 单设施配方、面积、等级、解锁 | 商业攻略站 | 补足机器面积、解锁、配方候选 |
| P1 | GamerGuides AIC products | https://www.gamerguides.com/arknights-endfield/database/items/aic-products | HTML 数据库 | 物品描述、来源、稀有度 | 商业攻略站 | 物品描述和分类补充 |
| P1 | Talos Pioneers | https://talospioneers.com/ | API / Web app | 蓝图元数据 | API 条款限制镜像 | 蓝图元数据参考，不镜像数据 |
| P2 | ENKA AIC tutorial | https://enka.network/endfield/aic/tutorial | HTML 教程 | 教程比例、传送带说明 | 未知 | 物流/教程交叉验证 |
| P2 | GameStratPort AIC guide | https://gamestratport.com/arknights-endfield/guides/endfield-aic-factory-guide/ | HTML | 配方、电力/物流总览 | 未知 | 聚合型资料，只做发现 |

## Endfield 开源项目

| 优先级 | 仓库 | 许可证 | 技术栈 | 内容 | 复用可行性 | 风险 |
|---|---|---|---|---|---|---|
| P0 | https://github.com/JamboChen/endfield-calc | MIT | TypeScript | 生产链计算器、配方依赖、循环处理 | 高价值参考 | 需验证数据新鲜度和来源 |
| P0 | https://github.com/endfield-calc/factoriolab | MIT | TypeScript / Angular | EndfieldLab fork of FactorioLab | 高价值架构/数据模型参考 | 大型 fork，需要小心隔离 Endfield 数据 |
| P0 | https://github.com/sssxks/end-cli | Apache-2.0 | Rust / WASM | MILP / HiGHS 生产优化器 | 高价值模型参考 | 参考思想，不直接照搬代码 |
| P0 | https://github.com/hsyhhssyy/IndustrialPlanner | Other | TypeScript | AIC 布局/物流模拟器、自定义蓝图 JSON | 高价值参考，不直接复用 | 许可证不清晰 |
| P0 | https://github.com/djkcyl/D.I.G.E. | MIT | TypeScript | 热能/电力计算器 | 电力子系统高价值参考 | 范围较窄 |
| P0 | https://github.com/fa93hws/endfield-industry-helper | NOASSERTION | TypeScript | 配方查看器 / 管理器 | 只做交叉验证 | 无许可证 |
| P0 | https://github.com/palmcivet/awesome-arknights-endfield | MIT | Awesome list | 工具/资源索引 | 发现入口 | 不是数据源 |
| P1 | https://github.com/DumzGW/ZMD-Endfield-calculator | MIT | Python | 产线、设备、电力、面积、利润规划 | 高价值算法交叉验证 | 中文语境假设需验证 |
| P1 | https://github.com/eddy3721/arknights-endfield-bp-tool | MIT | TypeScript | 蓝图工具 | 蓝图研究 | 需确认是否处理真实游戏蓝图字符串 |
| P1 | https://github.com/Talos-Pioneers/api | AGPL-3.0 | PHP | 蓝图后端 API/模型 | 元数据模型参考 | AGPL，不复制到 MIT 代码库 |
| P1 | https://github.com/Talos-Pioneers/ui | AGPL-3.0 | Vue | 蓝图分享 UI | UX 参考 | AGPL |
| P1 | https://github.com/XiaHouSheng/t2industry | MIT | Vue | 生产线模拟 | 物流/仿真参考 | 需检查正确性 |
| P1 | https://github.com/FrozenStream/endfield-sim | LGPL-3.0 | TypeScript | 早期基地/建筑模拟器 | 编辑器行为参考 | LGPL 义务 |
| P1 | https://github.com/dontless7/Arknights-Endfield-Simple-Modeler | NOASSERTION | C# / WinForms | 桌面生产线模型器 | 仅作为 C# 参考 | 无许可证 |
| P1 | https://github.com/NovaSagittarii/endfield-ilp | NOASSERTION | Python / Streamlit | 线性规划优化器 | 优化思想参考 | 无许可证 |
| P1 | https://github.com/LinTao1816/Endfield_AIC_Optimizer | NOASSERTION | MATLAB | AIC 布局优化求解器 | 算法参考 | 无许可证，MATLAB |
| P1 | https://github.com/hongshan-academy/endfield-AIC-sim | NOASSERTION | Python | 传送带仿真 | 物流仿真参考 | 无许可证 |
| P2 | https://github.com/yawarakatai/endfield-production-planner | MIT | Rust | 生产规划器 | 交叉验证 | 小型仓库 |
| P2 | https://github.com/Halasue-dev/endfield-aic-calculator | MIT | JavaScript | 早期 AIC 计算器 | 历史参考 | 可能已过时 |
| P2 | https://github.com/NagiYume/AKEDatabase | GPL-3.0 | HTML | 在线数据查询 | 数据交叉验证 | GPL / 数据来源风险 |
| P2 | https://github.com/7L-jingzhe/CycleFeed_Power | MIT | Rust | 循环供电仿真 | 电力子系统参考 | 范围较窄 |

## 安全复用策略

1. MIT / Apache-2.0 项目可作为实现参考；直接复用代码仍需署名和兼容性审查。
2. GPL / AGPL 项目只做概念参考，除非整体分发策略改变。
3. `NOASSERTION` / `Other` 项目在复制代码或数据前必须获得明确授权。
4. 游戏数据、图标、截图、原生蓝图码可能有独立 IP/条款限制，不自动随仓库许可证授权。
