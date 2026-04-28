# 数据溯源规范

AIC-EDA 的配方、设备、物流、电力、蓝图数据必须可追溯。所有从外部来源导入或手工录入的游戏事实都应附带来源、版本、置信度和许可说明。

## 必填溯源字段

| 字段 | 类型 | 是否必填 | 含义 |
|---|---|---:|---|
| `value` | 任意 | 是 | 实际数据值，例如配方时间、设备面积、供电半径 |
| `sourceName` | 字符串 | 是 | 来源名称，例如 `Game8 Processing Facilities` |
| `sourceUrl` | 字符串 | 是 | 能定位到该数据的网页或仓库链接 |
| `checkedAt` | 字符串 | 是 | ISO 日期，例如 `2026-04-28` |
| `gameVersion` | 字符串 | 推荐 | 游戏版本或 patch，例如 `1.2.1`，未知则 `unknown` |
| `language` | 字符串 | 推荐 | `zh-CN`、`en-US` 等 |
| `confidence` | 枚举 | 是 | `official`、`high`、`medium`、`low`、`unverified` |
| `licenseNote` | 字符串 | 是 | 来源许可、条款或“不允许批量复制”等说明 |
| `extractedBy` | 字符串 | 可选 | 手工、脚本、社区贡献者等 |
| `notes` | 字符串 | 可选 | 争议、二次验证、转换说明 |

## 置信度等级

| 等级 | 含义 | 导入策略 |
|---|---|---|
| `official` | 官方公告、官方工具、游戏内实测截图/录屏 | 可作为最高优先级，但仍记录版本 |
| `high` | 多个高质量社区数据库一致，或开源结构化数据有许可 | 可导入为候选事实 |
| `medium` | 单一社区来源，或攻略站表格 | 需要交叉验证后再用于算法默认值 |
| `low` | SEO 攻略、帖子、截图、转述 | 只做发现，不直接入库 |
| `unverified` | 未确认来源或许可 | 不进入正式数据集 |

## JSON 示例结构

```json
{
  "id": "electric_pylon",
  "name": {
    "zhCN": "供电桩",
    "enUS": "Electric Pylon"
  },
  "category": "Power",
  "footprint": {
    "width": {
      "value": 2,
      "sourceName": "Game8 All Power Facilities",
      "sourceUrl": "https://game8.co/games/Arknights-Endfield/archives/536217",
      "checkedAt": "2026-04-28",
      "gameVersion": "unknown",
      "confidence": "medium",
      "licenseNote": "社区攻略；作为正式数据导入前需游戏内验证"
    },
    "depth": {
      "value": 2,
      "sourceName": "Game8 All Power Facilities",
      "sourceUrl": "https://game8.co/games/Arknights-Endfield/archives/536217",
      "checkedAt": "2026-04-28",
      "gameVersion": "unknown",
      "confidence": "medium",
      "licenseNote": "社区攻略；作为正式数据导入前需游戏内验证"
    }
  }
}
```

## 来源选择顺序

1. 官方游戏工具 / 游戏内验证。
2. 有明确许可和来源说明的结构化 OSS 数据，例如 MIT/Apache 来源。
3. 至少被另一个来源交叉验证的高质量社区数据库。
4. 用于描述和术语整理的 Wiki / 攻略页。
5. 论坛、视频、社交平台帖子只用于发现和人工验证。

## 数据导入审查清单

- [ ] 每个数值是否都有 `sourceUrl`？
- [ ] `checkedAt` 是否适用于目标游戏版本？
- [ ] 来源许可证是否允许再分发？
- [ ] 关键玩法数值是否有第二来源一致？
- [ ] 是否排除了未经授权的专有资产和图标？
- [ ] 原生蓝图码是否仅在有署名和许可说明时保存？
