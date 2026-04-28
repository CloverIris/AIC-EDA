# 蓝图研究

检查日期：2026-04-28。AIC-EDA 当前应把游戏原生 `EFO...` 蓝图分享码视为不透明字符串；在官方格式未公开或未明确可逆向前，不声明可解码或可直接导入。

## 来源地图

| 来源 | URL | 类型 | 内容 | 对 AIC-EDA 的启发 |
|---|---|---|---|---|
| 官方 Game Tools Guide | https://endfield.gryphline.com/news/0755 | 官方 | Wiki、Map Tools、Protocol Terminal 入口 | 不提供公开蓝图 schema，不能假设兼容 |
| 官方安全风险提醒 | https://endfield.gryphline.com/news/7026 | 官方 | 账号授权和第三方工具风险提醒 | AIC-EDA 应优先离线、本地、不要求账号 |
| Game8 Blueprints and Codes | https://game8.co/games/Arknights-Endfield/archives/534265 | 社区攻略 | 蓝图、代码、布局推荐 | 学习导入/导出 UX 和用户文案 |
| Game8 Create and Share Blueprints | https://game8.co/games/Arknights-Endfield/archives/575914 | 社区攻略 | 创建、保存、分享步骤 | UI 流程参考 |
| Talos Pioneers | https://talospioneers.com/en | 社区蓝图库 | 公开蓝图条目、区域/服务器/标签/尺寸 | 社区元数据模型参考 |
| Talos Pioneers collections | https://talospioneers.com/en/collections | 社区蓝图库 | 多部件蓝图集合 | 支持 layout bundle / blueprint pack |
| Talos Pioneers API terms | https://talospioneers.com/en/api-terms | 条款 | API 使用、署名、禁止镜像/批量再分发 | 不能批量镜像其数据 |
| Talos Pioneers API | https://github.com/Talos-Pioneers/api | AGPL 后端 | 蓝图模型字段：code、version、region、serverRegion、width、height | 原生 code 作为不透明元数据 |
| Mobalytics blueprint guide | https://mobalytics.gg/arknights-endfield/profile/mattjestic-multigaming/guides/arknights-endfield-blueprints-codes | 社区攻略 | Patch 1.1/1.2、Wuling/Valley 布局、区域变体 | 记录 patch/server 兼容性 |
| Talos Hub Endgame Blueprints | https://endfieldhub.org/guides/endgame-blueprints-part-5 | 社区攻略 | Wuling 1.2.1 大型基地、导入步骤、代码表 | 复制错误、O/0、版本不匹配提示 |
| Endfield Calculator Blueprints | https://endfieldcalculator.org/blueprint | 社区数据库 | 蓝图尺寸、作者、产出、代码 | 参考列表字段 |
| RosenBerryRooms blueprint guide | https://www.rosenberryrooms.com/arknights-endfield-blueprints/ | 社区攻略 | 创建/导入流程和常见问题 | 材料、解锁、地形、空间限制提醒 |
| Talos Hub Industrial Planner | https://endfieldhub.org/tools/industrial-planner | 工具目录 | 模拟器与蓝图工具链接 | simulation-first workflow 参考 |
| IndustrialPlanner | https://github.com/hsyhhssyy/IndustrialPlanner | 开源工具 | 自定义 JSON 蓝图 schema、公开示例 | AIC-EDA 自定义 JSON 格式参考 |

## 建议支持的元数据字段

| 字段 | 含义 |
|---|---|
| `format` | `AIC_EDA_JSON`、`GAME_NATIVE_OPAQUE`、`INDUSTRIAL_PLANNER_JSON` 等 |
| `code` | 如果存在原生分享码，则作为不透明字符串保存；不要假设可解码 |
| `gameVersion` | 蓝图测试时的游戏版本或 patch |
| `serverRegion` | America/Europe、Asia、CN、unknown |
| `region` | Valley IV、Wuling、any/unknown |
| `width` / `height` | 已知蓝图尺寸 |
| `requiredUnlocks` | 所需科技或设施解锁 |
| `requiredMaterials` | 可选材料估算 |
| `sourceUrl` | 原始社区或官方来源 |
| `permissionNote` | 署名和再分发说明 |
| `warnings` | 服务器不匹配、版本不匹配、地形要求、复制歧义等 |

## 实现立场

1. `BlueprintCodec` 应优先继续支持 **AIC-EDA 自定义 JSON** 格式。
2. 游戏原生分享码只应作为不透明字符串保存和复制。
3. UI 应提示服务器/版本不匹配，以及复制字符歧义（`O` vs `0`、额外空格）。
4. 社区蓝图库应以链接和署名为主，不批量镜像。
5. 只有当原生格式公开文档化，或确认存在合法逆向路径时，AIC-EDA 才考虑增加原生码解析器。
