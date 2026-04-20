# 更新日志

所有重要的变更都将记录在此文件中。

格式基于 [Keep a Changelog](https://keepachangelog.com/zh-CN/1.0.0/)，版本号遵循 [语义化版本](https://semver.org/lang/zh-CN/)。

---

## [1.2.0] - 2026-04-20

### 新增

#### 合成链条 Flyout (RecipeBrowser)
- **Minecraft 风格合成网格** — 点击 RecipeBrowser 任意物品，弹出 3×3  crafting grid，展示配方输入/输出
- **配方选择器** — 支持多个配方时横向切换
- **递归上游链条树** — 展示原材料 → 中间产物 → 目标物品的完整链条，含速率与机器信息
- **TeachingTip 定位** — Flyout 精准定位到点击的 GridViewItem

#### Dashboard 工业指挥中心（替换 WelcomeWindow）
- **2×2 田字格布局** — 启动页完全重写为四象限工业仪表盘
- **Q1: 资产录入 & 均衡解算** — ComboBox+Slider 录入已有资产，实时雷达图显示五大方向权重，一键计算均衡布局建议
- **Q2: 发展优先级 & RoadMap** — 初级/中级/高级三级多选，自动生成原材料 CheckList（含进度条）和分阶段 RoadMap（含电力/占地预估）
- **Q3: 多目标优化/复用** — 多选最终产品+设定速率，自动分析共享中间产物，给出合并建线建议
- **Q4: 产线模拟/瓶颈热力图** — 简化产线节点动画，Play/Pause 控制，红黄绿实时显示瓶颈状态

#### 步骤指示器 (Step Indicator)
- MainWindow 顶部新增全局步骤条：Setup → Synthesize → Layout → Export
- 当前步骤黄色高亮，已完成绿色打勾，未开始灰色
- 随页面导航自动同步

### 改进

#### 3D 等距视图修复
- **网格范围限制** — 不再绘制无限延伸的 60×40 全范围网格，改为只在机器包围盒 + 8 格边距内绘制
- **视觉降级** — 网格线透明度从 0x30 降至 0x18，改为 2,4 虚线，颜色更暗
- **背景层次分离** — RedrawCanvas 先绘制 #0A0A0A 深色底板，再画网格/机器/连接

### 修复
- `EndfieldButtonStyle` 缺失导致的启动 XamlParseException
- `UpdateCanvasSize` 中 Width/Height 可能为 NaN/负值/无穷导致的 ArgumentException
- 画布渲染异常时增加 try-catch + 红色边缘闪烁非阻塞警告

---

## [1.1.0] - 2026-04-20

### 新增

#### 交互式布局设计器 (Layout Designer)
- **机器工具栏 (Machine Palette)** — 按分类分组（资源、加工、农业、电力、物流），支持中文显示名
- **点击放置 + 拖拽移动** — 从工具栏选择机器类型，点击画布放置；拖拽已放置机器自动网格对齐
- **碰撞检测与边界检查** — 实时检测机器间碰撞和画布边界，拒绝非法放置
- **属性面板** — 选中机器显示位置、尺寸、旋转、电力覆盖半径；支持旋转(键R)和方向键微调
- **键盘快捷键** — Delete 删除、R 旋转、方向键移动、Esc 取消选择
- **从编译图导入** — 一键将 RTL Synthesis 编译结果导入为可编辑布局
- **JSON 保存/加载** — 支持 .json 格式的布局文件保存与加载（Windows 文件选择器）

#### 等距 3D 预览
- **2D/等距视图切换** — 工具栏一键切换平面视图与伪 3D 等距视图
- **菱形网格** — 等距视角下的对角线网格线
- **Painter's Algorithm 深度排序** — 按 (gridX + gridY) 排序确保正确遮挡关系
- **三面体绘制** — 顶面（亮色）、左侧面（中色）、右侧面（暗色）营造立体感

#### 电力覆盖可视化
- 选中电力设备（协议锚点核心、供电桩、中继器等）时显示半透明虚线圆，标注供电半径

#### 传送带连接模式
- **Connect Mode** — 点击工具栏连接图标进入连接模式，先点击源机器，再点击目标机器建立连接
- **虚线箭头** — 连接以黄色虚线带箭头表示，支持 2D 平面和等距 3D 两种视图
- **一键清除** — 工具栏提供清除所有连接按钮
- **防重复** — 自动检测并阻止重复连接

#### 工作流连通
- **RecipeBrowser → RecipeCompiler** — 选中任意物品后点击 "Compile Target" 直接跳转并预填目标产物
- **RecipeCompiler → LayoutDesigner** — 编译完成后点击 "Layout Designer" 直接跳转

#### 复制设备
- **Duplicate (Ctrl+D)** — 选中机器后按 Ctrl+D 快速复制到相邻网格位置（优先右侧，若碰撞则下方）

### 改进
- RecipeBrowser 卡片分类着色 — FontIcon 和详情面板图标按物品分类显示对应颜色
- RecipeBrowser 详情面板 — 显示 Production Recipes 和 Used As Input In 两个列表，含配方输入/输出摘要
- RecipeCompiler NodeTemplate — 全部改为 x:Bind 编译时绑定，提升性能和类型安全
- MainWindow 右侧面板 — 新增 "Layout Designer" 快捷按钮和版本信息

### 修复
- RecipeBrowser SelectedItem 空状态绑定（FallbackValue 处理）
- 筛选芯片激活状态视觉反馈

---

## [1.0.0] - 2026-04-20

### 新增

#### 界面与交互
- **欢迎界面** — 1100×750 启动画面，包含硬件检测、新建项目、最近项目
- **主窗口** — 专业 IDE 布局：MenuBar + CommandBar + NavigationView + 右侧信息面板
- **终末地暗色主题** — 柠檬黄（#FFD600）主色调，深色表面（#0D0D0D、#141414）
- **自定义控件** — `[ BRACKET_TAG ]` 风格标签控件
- **布局预览交互** — 鼠标拖拽平移、滚轮缩放（以光标为中心）、缩放按钮

#### 页面
- **Recipe Browser** — 70+ 物品、80+ 配方的完整数据库浏览与搜索
- **RTL Synthesis** — 配方编译页面，支持目标产物选择、产能输入、一键编译
- **FloorPlan & Place** — 2D 网格化布局预览，俄罗斯方块式设备块显示
- **Tape Deploy** — 蓝图导出页面，支持蓝图字符串和 JSON 导出

#### 核心算法
- **Recipe Compiler** — 反向构建生产依赖树，拓扑层级分配
- **Flow Balancer** — 精确设备数量计算，产线流量平衡
- **Spatial Planner** — 2D/3D 网格布局，碰撞检测，力导向优化
- **Route Planner** — A* 曼哈顿路径规划，障碍物绕行
- **Throughput STA** — 产能静态时序分析引擎
- **DRC Validator** — 6 项设计规则检查
- **PWR Optimizer** — 供电树综合与优化
- **Blueprint Codec** — GZip+Base64 蓝图编码与 JSON 导出

#### 配方数据库
- 70+ 物品定义（原材料、中间产物、最终产品、流体、特殊）
- 80+ 配方覆盖完整生产链：采矿 → 精炼 → 零件 → 组件 → 最终产品
- 30+ 设备类型，含空间规格（尺寸、端口、电力半径）
- 最终产品：SC Wuling 电池、齿轮原件、药剂、饮料、工业炸药等

#### 布局系统
- **40×40 / 70×70 网格切换** — 支持两种网格精度
- **传送带 L 形路由** — 曼哈顿路径，转弯标记，方向箭头
- **设备分类着色** — 资源（蓝）、加工（橙）、物流（灰）、电力（金）、农业（绿）
- **网格铺满渲染** — 根据 Canvas 实际尺寸动态计算可见网格范围

### 技术栈
- WinUI 3 (Windows App SDK 1.8.260317003)
- .NET 8 (net8.0-windows10.0.19041.0)
- CommunityToolkit.Mvvm 8.4.0
- MVVM 架构 + 源生成器

### 修复
- 修复 XamlParseException：移除对不存在 `Default*Style` 资源的 `BasedOn` 引用
- 修复 RecipeBrowser 首次加载不自动选中第一项的问题
- 修复 MainWindow 内容区域宽度不足的问题（ContentFrame 现在正确填充可用空间）
- 修复 RoutePlanner 障碍物检测：从中心基改为角落基，与 SpatialPlanner 一致

### 已知问题
- MVVMTK0045 警告：21 个 `[ObservableProperty]` 字段的 AOT 兼容性警告（非阻塞）
- WinAppSDK 1.8 XamlCompiler 偶发性崩溃（建议使用 VS 构建而非命令行）
- 游戏原生蓝图格式尚未逆向，当前导出为自定义格式

---

## [0.9.0] - 2026-04-15

### 新增
- 初始项目架构搭建（Models, Core, Services, Views, ViewModels）
- 基础主题系统（深色模式）
- 配方数据库骨架（20+ 物品，30+ 配方）
- 空间布局规划器初版

### 修复
- 解决 WMC1013、CS1012、CS0236、CS0103 等编译错误
- 修复 MSB3073 MSIX 打包失败

---

## [0.1.0] - 2026-04-10

### 新增
- 项目初始化
- 基础 WinUI 3 应用框架
- 概念验证：配方编译与布局渲染

---

## 未来路线图

### [1.2.0] 计划
- [ ] 传送带连接可视化（手动拖拽连接输出口/输入口）
- [ ] 多产物联合优化
- [ ] 自动流水线节拍平衡
- [ ] 导入/导出配方数据库 JSON
- [ ] 布局方案保存与对比
- [ ] 复制/粘贴设备组
- [ ] 3D 预览模式（DirectX 硬件加速）

### [2.0.0] 计划
- [ ] 游戏内原生蓝图格式支持（需逆向工程）
- [ ] 在线配方数据库同步
- [ ] 云端布局分享
- [ ] 社区配方市场
