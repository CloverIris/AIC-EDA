# 终末地 AIC 资料源

检查日期：2026-04-28。以下来源用于建立《明日方舟：终末地》集成工业系统（AIC）的机制资料库。社区来源只作为候选事实，导入数据前必须记录来源和置信度。

## 高优先级机制来源

| 优先级 | 来源 | URL | 语言 | 覆盖内容 | 可信度 | AIC-EDA 提取目标 |
|---|---|---|---|---|---|---|
| P0 | 官方 Hypergryph / Endfield 站点 | https://endfield.hypergryph.com/ | 中文/英文 | 官方新闻、版本信息、工具入口 | 官方，但结构化数据少 | 版本追踪、官方命名、公告变更 |
| P0 | 官方 Game Tools Guide | https://endfield.gryphline.com/news/0755 | 英文 | 官方 Wiki、Map Tools、Protocol Terminal 入口 | 官方 | 确认官方工具存在，不假设公开 API |
| P0 | Game8 AIC Factory and Base Building Guide | https://game8.co/games/Arknights-Endfield/archives/537154 | 英文 | AIC 总览、PAC/Sub-PAC、矿机、电力、生产线 | 高质量社区来源 | AIC 分类体系、阶段目标、关键生产链 |
| P0 | Game8 List of All AIC Buildings | https://game8.co/games/Arknights-Endfield/archives/575474 | 英文 | 设施目录 | 高质量社区来源 | 机器列表、分类、解锁候选 |
| P0 | Game8 Processing Facilities | https://game8.co/games/Arknights-Endfield/archives/536215 | 英文 | 加工设施、面积、电力、产物 | 高质量社区来源 | `MachineSpec`、机器到配方映射 |
| P0 | Game8 Logistics Facilities | https://game8.co/games/Arknights-Endfield/archives/536163 | 英文 | 传送带、管道、分流/汇流、物流设施 | 高质量社区来源 | `RoutePlanner`、物流 DRC 规则 |
| P0 | Game8 Power Facilities | https://game8.co/games/Arknights-Endfield/archives/536217 | 英文 | 供电桩、中继器、热能站 | 高质量社区来源 | `PWROptimizer`、电力设施规格 |
| P0 | Endfield DB Calculator | https://endfielddb.com/calculator/ | 英文 | 产能规划、设备数、电力、循环依赖提示 | 高质量社区工具 | 配方图、产能、电力交叉验证 |
| P0 | EndfieldTools Recipes | https://endfieldtools.dev/factory-planner/recipes/ | 英文 | 配方和 planner 数据 | 高质量社区工具 | 结构化 recipe 候选 |
| P0 | Talos Hub Facilities | https://endfieldhub.org/database/facilities | 英文 | 设施、范围、描述 | 中高 | 设施和电力范围交叉验证 |
| P0 | Talos Hub AIC Factory Guide | https://endfieldhub.org/guides/factory | 英文 | 电力链、Thermal Bank、Depot Bus、Wuling 机制 | 中高 | 电力/物流机制说明 |
| P0 | Talos Hub AIC Throughput Guide | https://endfieldhub.org/guides/aic-throughput | 英文 | 端口/传送吞吐、比例、收益 | 中等，社区推导 | 吞吐公式候选，需实测验证 |
| P0 | Endfield Wiki / wiki.gg AIC | https://endfield.wiki.gg/wiki/Automated_Industry_Complex | 英文 | AIC 概念、PAC、仓储、流体模式 | 中高，CC BY-SA | 机制词表和概念说明 |

## 中文资料与交叉验证来源

| 优先级 | 来源 | URL | 覆盖内容 | 可信度 | 备注 |
|---|---|---|---|---|---|
| P1 | GameKee 供电桩 | https://www.gamekee.com/zmd/692037.html | 供电桩、协议容量、供电范围 | 高质量中文社区来源 | 中英文术语和电力数值交叉验证 |
| P1 | GamerSky 工业系统设备介绍 | https://www.gamersky.com/handbook/202601/2079903.shtml | 设备功能总览 | 中高 | 中文设施名称候选 |
| P2 | 17173 基建制造站配方大全 | https://news.17173.com/z/arknights2026/content/01242026/175606550.shtml | 配方、设施、港口、电力 | 中等 | SEO/攻略站，只做发现 |
| P2 | 17173 采集与制造系统解析 | http://news.17173.com/z/arknights2026/content/01272026/143047592.shtml | 采集、制造、吞吐公式候选 | 中等 | 所有数值需二次验证 |
| P2 | 17173 电力系统管理指南 | https://news.17173.com/z/arknights2026/content/01272026/191626207.shtml | 核心、中继器、供电桩 | 中等 | 电力叙述交叉验证 |
| P2 | 切游网 基建蓝图机制 | https://www.qieyou.com/content/150682 | 蓝图、流水线搭建 | 中等 | 蓝图流程和限制候选 |
| P2 | 切游网 基建机制介绍 | https://www.qieyou.com/content/152866 | PAC、供电桩、中继器、闭环生产 | 中等 | 中文教程性说明 |
| P2 | 游侠网 供电桩设备页 | https://gl.ali213.net/html/2026-1/1742509_57.html | 设备获取、面积、运输 | 中等 | 解锁/材料候选 |
| P2 | 游侠网 装备原件机任务 | https://gl.ali213.net/html/2026-1/1741331.html | 任务生产线示例 | 中等 | 任务驱动的生产链参考 |
| P3 | NGA / 贴吧 / Bilibili 帖子 | 手动搜索 | 蓝图、实测、补丁发现 | 不稳定 | 只做发现和人工验证，不直接入库 |

## 重点缺失字段

| 字段 | 难点 | 候选来源 |
|---|---|---|
| 设备占地 | 社区页有面积但不一定有精确占格和高度 | Game8 设施页、原始游戏表、游戏内测量 |
| 端口位置 | 通常攻略不记录具体端口坐标/方向 | 游戏内测量、原始 `Factory*` 表、截图 |
| 供电半径 / 中继距离 | 不同来源可能混用范围、线长、供电半径 | GameKee、Game8、Talos Hub、游戏内测试 |
| 传送带 / 管道吞吐 | 社区有 0.5 item/s 等说法，需版本验证 | Talos throughput guide、ENKA guide、游戏内测试 |
| 武陵流体规则 | 新地区机制更新快 | Game8 武陵页面、Talos Hub、原始表 |
| 原生蓝图格式 | 原生分享码未公开 | Talos Pioneers 不透明字符串模型、未来逆向研究备注 |

## 提取规则

任何导入到 `Data/` 或未来数据库的游戏事实都必须记录：`value`、`sourceUrl`、`checkedAt`、`gameVersion`、`confidence`、`licenseNote`。
