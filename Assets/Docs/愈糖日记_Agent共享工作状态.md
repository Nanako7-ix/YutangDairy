# 《愈糖日记》Unity 原型 Agent 共享工作状态

> 最后核对日期：2026-06-13  
> 用途：供后续 Agent 直接接手开发。本文同时记录当前真实进度、技术决策、剩余任务和验收要求。  
> 事实来源：Unity MCP 对当前工程的实时检查，不以旧交付说明或工作区脚本副本为准。

## 1. 权威位置与连接信息

- Unity 工程：`D:/unity_/GlucoseDiary`
- Unity 版本：`2022.3.62f3`
- 当前平台：`StandaloneWindows64`
- MCP 地址：`http://127.0.0.1:8080/mcp`
- 当前实例：`GlucoseDiary@d309e89f2d55c2fe`
- 原始任务书：`C:/Users/Thephix/Documents/Codex/2026-06-08/files-mentioned-by-the-user-pdf/outputs/愈糖日记_Agent一口气开发任务书.md`
- 本文档：`C:/Users/Thephix/Documents/Codex/2026-06-10/8080-unity-mcp-codex-output-demo-2/outputs/愈糖日记_Agent共享工作状态.md`

MCP session ID 会随重连变化，不要写死 session ID。每次新会话应执行：

1. 读取 `mcpforunity://custom-tools`。
2. 读取 `mcpforunity://instances`。
3. 用 `set_active_instance` 锁定 `GlucoseDiary@d309e89f2d55c2fe`。
4. 读取 `mcpforunity://project/info` 和 `mcpforunity://editor/state`。

## 2. 重要交接结论

当前不是完整 Day 1 到 Day 5 Demo。

真实状态是：

- `MainMenu`：已完成序列化欢迎界面。
- `Day1_Hospital`：已完成可玩的第三人称医院探索原型。
- `Day2_Home` 到 `Result`：场景文件存在并加入 Build Settings，但当前均为 0 个根对象的空场景。
- 旧的运行时生成 Demo 已删除/禁用，不应恢复。
- `outputs/愈糖日记_UnityDemo_交付说明.md` 描述的是旧运行时生成版本，内容已经过时，不能作为当前完成度依据。
- `work/day1/*.cs` 是历史工作副本，可能落后于 Unity 工程。Unity `Assets` 内脚本才是唯一事实来源。

## 3. 原型总任务

制作一个电脑端 Unity 灰盒原型，最终流程为：

```text
MainMenu
  -> Day1_Hospital
  -> Day2_Home
  -> Day3_Home
  -> Day4_Home
  -> Day5_Rhythm
  -> Result
```

最终必须包含：

1. 主菜单和完整场景流程。
2. 本局健康值、健康积分、失败/重试次数和通关状态。
3. `PlayerPrefs` 本地最高健康积分。
4. Day 1 医院第三人称探索。
5. Day 2 血糖监测游戏化挑战和早餐卡牌。
6. Day 3 午餐/晚餐卡牌和血糖平衡跑酷。
7. Day 4 一日饮食规划、监测进阶，可选短版跑酷。
8. Day 5 四轨下落式音游。
9. Result 结算页。
10. 失败可重试，流程无阻断。

这是健康科普休闲游戏，不是医学诊疗模拟器。健康值和健康积分只能作为游戏反馈，避免使用“治好糖尿病”“控制真实血糖”“血糖恶化”等误导表达。

## 4. Build Settings 实际状态

| Build Index | 场景 | 根对象数 | 当前状态 |
| --- | --- | ---: | --- |
| 0 | `Assets/Scenes/MainMenu.unity` | 6 | 已完成欢迎界面 |
| 1 | `Assets/Scenes/Day1_Hospital.unity` | 8 | 已完成可玩原型 |
| 2 | `Assets/Scenes/Day2_Home.unity` | 0 | 空场景，待开发 |
| 3 | `Assets/Scenes/Day3_Home.unity` | 0 | 空场景，待开发 |
| 4 | `Assets/Scenes/Day4_Home.unity` | 0 | 空场景，待开发 |
| 5 | `Assets/Scenes/Day5_Rhythm.unity` | 0 | 空场景，待开发 |
| 6 | `Assets/Scenes/Result.unity` | 0 | 空场景，待开发 |

场景是否按“天”拆分不是硬性要求。独立环境和主要玩法适合独立 Scene；同一天内的卡牌、监测、对话等子流程可以在同一 Scene 内切换面板或状态，不需要为每个步骤单独建 Scene。

所有正式可用场景必须有序列化 GameObject 层级。不要重新使用一个常驻脚本在运行时生成整套 Demo。

## 5. 已完成内容

### 5.1 MainMenu 欢迎界面

根对象：

- `Main Camera`
- `Directional Light`
- `WelcomeStageRoot`
- `WelcomeScreenRoot`
- `EventSystem`
- `GameManager`

已实现：

- 可见欢迎界面和开始按钮。
- 显示历史最高健康积分。
- 开始按钮调用 `GameManager.StartNewGame()`。
- 从 `MainMenu` 正常进入 `Day1_Hospital`。
- `GameManager` 使用 `DontDestroyOnLoad`。

相关脚本：

- `Assets/Scripts/Core/GameManager.cs`
- `Assets/Scripts/UI/YutangWelcomeScreen.cs`

当前 `GameManager` 只负责：

- 单例生命周期。
- 从 MainMenu 进入 Day 1。
- 返回 MainMenu。
- 读取 `YutangDiary_BestHealthScore`。

它尚未实现本局健康值、健康积分、失败次数、当前天数和完整流程状态。

### 5.2 Day 1 医院探索

根对象：

- `HospitalEnvironment`
- `ClinicRoom`
- `Player`
- `Main Camera`
- `Directional Light`
- `Day1UI`
- `EventSystem`
- `Day1HospitalController`

已实现：

- 灰盒医院大厅、候诊区、接待台和诊室。
- 玩家使用 `CharacterController`。
- `WASD` 或方向键移动。
- 移动方向相对于当前相机。
- 玩家朝移动方向平滑转向。
- 接近医生后出现 `E` 交互提示。
- 医生三段对话。
- 对话时锁定玩家移动并释放鼠标。
- 对话完成后尝试加载 `Day2_Home`。
- `Esc` 可返回 MainMenu（非对话状态）。

相关脚本：

- `Assets/Scripts/Day1/Day1PlayerController.cs`
- `Assets/Scripts/Day1/Day1CameraFollow.cs`
- `Assets/Scripts/Day1/Day1HospitalController.cs`
- `Assets/Editor/Day1HospitalSceneBuilder.cs`

### 5.3 Day 1 第三人称相机

当前相机行为：

- 无需按住右键，移动鼠标直接环绕角色。
- 光标在游戏开始时锁定并隐藏。
- 对话开始时释放并显示光标。
- 滚轮缩放。
- 水平和垂直灵敏度独立。
- 相机使用 SphereCast 处理墙体碰撞并自动拉近。
- 相机跟随角色，但不会继承角色旋转造成画面倾斜。

当前默认参数：

| 参数 | 值 |
| --- | ---: |
| `Target Offset` | `(0, 0.65, 0)` |
| `Distance` | `6.2` |
| `Min Distance` | `2.2` |
| `Max Distance` | `10` |
| `Pitch` | `22` |
| `Min Pitch` | `5` |
| `Max Pitch` | `75` |
| `Horizontal Look Sensitivity` | `180` |
| `Vertical Look Sensitivity` | `140` |
| `Zoom Sensitivity` | `1.2` |
| `Position Smooth` | `10` |
| `Collision Radius` | `0.3` |
| `Collision Padding` | `0.15` |
| Camera FOV | `60` |

已验证相机侧后方遇墙时会从配置距离 `6.2` 自动缩短到约 `2.45`，说明碰撞处理生效。

### 5.4 Day 1 灯光一致性

已修复从 Welcome 进入 Day 1 时背光面接近纯黑的问题。

原因：

- Day 1 原本依赖 `Skybox` 环境光。
- 场景没有独立烘焙 Lighting Settings。
- 切场景时环境探针更新时机不稳定。

当前设置：

- `RenderSettings.ambientMode = AmbientMode.Flat`
- 环境光：`RGBA(0.36, 0.39, 0.42, 1)`
- Directional Light 强度：`1.05`
- Directional Light 颜色：`RGBA(1, 0.96, 0.90, 1)`

设置同时存在于：

- `Day1HospitalController.Awake()`，保证运行时立即生效。
- `Day1HospitalSceneBuilder`，保证重建场景不回退。
- 当前 `Day1_Hospital` 场景序列化数据。

已按真实路径验证：

```text
MainMenu -> GameManager.StartNewGame() -> Day1_Hospital
```

切换后环境光立即为固定值，画面亮度与直接启动 Day 1 一致。

## 6. 尚未完成内容

### 6.1 全局系统

尚未实现：

- 当前天数。
- 当前关卡/子阶段状态。
- 本局健康值。
- 本局健康积分。
- 失败和重试次数。
- 是否完成整局。
- 分数变化事件和通用 HUD。
- 最终最高分写入逻辑。
- 完整场景流转控制。

### 6.2 Day 2

`Day2_Home` 当前为空场景。

需要实现：

1. 基础家庭/桌面灰盒环境或直接以 UI 玩法为主。
2. 血糖监测流程科普展示。
3. 游戏化滑动窗口挑战：
   - 指示值自动下降。
   - 空格使指示值上升。
   - 在目标窗口内维持指定时间。
   - Day 2 目标窗口较宽，提示明确。
   - 失败后可重试。
4. 早餐卡牌：
   - 若干食物卡。
   - 选择 3 张。
   - 卡牌包含名称、类型、健康影响和喜好度。
   - 结算健康积分。
5. 完成后进入 Day 3。

### 6.3 Day 3

`Day3_Home` 当前为空场景。

需要实现：

- 午餐/晚餐卡牌，选择 4 张。
- 类型平衡限制。
- 指定距离的三轨或横版跑酷。
- 障碍扣健康值，有益道具恢复健康值或增加积分。
- 到终点成功，健康值归零失败。

### 6.4 Day 4

`Day4_Home` 当前为空场景。

需要实现：

- 选择 5 张的一日饮食规划。
- 复用 Day 2 监测系统，改为更窄或移动的目标窗口。
- 可选：复用 Day 3 做短版跑酷。

时间不足时可砍 Day 4 短版跑酷，不能砍卡牌和监测进阶。

### 6.5 Day 5

`Day5_Rhythm` 当前为空场景。

需要实现：

- 四条轨道。
- 建议键位 `D/F/J/K` 或 `A/S/K/L`，也可继续遵循原任务书使用 WASD。
- 音符下落。
- 判定线和命中窗口。
- 命中、漏按、连击、命中率和健康积分。
- 30 到 60 秒灰盒谱面。

### 6.6 Result

`Result` 当前为空场景。

需要显示：

- 本局健康积分。
- 历史最高健康积分。
- 是否刷新最高分。
- 失败/重试次数。
- 通关状态。
- 返回 MainMenu 按钮。

## 7. 推荐后续开发顺序

### P0：先补全流程基础

1. 扩展 `GameManager` 或新增 `GameSessionState`。
2. 实现健康值、健康积分、失败次数和当前 Day。
3. 实现统一的 `CompleteStage`、`RetryStage`、`LoadNextScene`。
4. 实现最高分写入和重置新游戏。
5. 给空场景加入 Camera、Directional Light、EventSystem 和明确根对象。

不要先做正式模型或动画。当前最大风险是主流程和共享系统尚未建立。

### P1：完成 Day 2

优先做血糖监测滑动窗口，因为它是不能砍的核心玩法，并且 Day 4 会复用。

建议模块：

```text
Scripts/
  Core/
    GameManager.cs
    GameSessionState.cs
    SceneFlowController.cs
  Systems/
    GlucoseMonitorMinigame.cs
    FoodCardSystem.cs
  UI/
    HealthScoreHUD.cs
```

Day 2 完成后必须从 MainMenu 走完整路径验证：

```text
MainMenu -> Day1 -> Day2 监测 -> Day2 卡牌 -> Day3
```

### P2：建立可复用卡牌系统

不要分别为 Day 2、Day 3、Day 4 写三套卡牌逻辑。

建议数据结构：

- `FoodCardDefinition`
- 食物名称。
- 类型。
- 健康影响。
- 喜好度。
- 可选说明文本。

系统参数化：

- 可选数量。
- 必需类型。
- 健康与喜好权重。
- 完成后奖励积分。

### P3：Day 3 跑酷

优先保证：

- 输入稳定。
- 障碍和道具可辨识。
- 健康值归零时可重试。
- 终点可完成。

暂时不追求复杂物理、程序生成或正式动画。

### P4：Day 4 复用组合

组合已有卡牌、监测和跑酷模块，不新增大系统。

### P5：Day 5 音游与 Result

完成四轨输入、命中统计和积分，再串到 Result。

## 8. 角色模型和动画策略

当前玩家和医生使用胶囊体占位。第三人称近距离镜头会放大占位模型的粗糙感，但不影响继续开发核心交互。

推荐路线：

1. 先完成相机、移动、碰撞、交互和全流程。
2. 再导入现成人形模型。
3. 使用 Humanoid Rig。
4. 用 Animator Blend Tree 接入 Idle 和 Walk。
5. 通过移动速度驱动动画参数。

不建议当前阶段自行建模或手工制作完整动作。可以使用许可明确的现成人形模型和动画资源。

替换模型时保留：

- `Player` 根对象。
- `CharacterController`。
- `Day1PlayerController`。
- 子对象中的可视模型和 `Animator`。

这样不会推翻现有移动和相机代码。

## 9. 技术约束与开发约定

### Unity 修改方式

- 用户要求优先且默认只通过 Unity MCP 修改 Unity 工程。
- 不要用外部 shell、文件复制或工作区镜像直接覆盖 `D:/unity_/GlucoseDiary`。
- 修改脚本后必须刷新/编译并读取 Console。
- 新组件只能在编译成功后挂载。
- 完成测试后退出 Play Mode，并把编辑器停在相关场景。

### 场景结构

- 场景中必须存在可见、可检查的序列化 GameObject。
- 不恢复 `__YT_APP` 或旧 `YutangDemoApp` 式整场景运行时生成方案。
- 新场景至少包含 Camera 和主光源。
- 可复用对象后续可逐步转为 Prefab。

### 脚本来源

当前 Unity 工程脚本清单：

```text
Assets/Editor/Day1HospitalSceneBuilder.cs
Assets/Scripts/Core/GameManager.cs
Assets/Scripts/Day1/Day1CameraFollow.cs
Assets/Scripts/Day1/Day1HospitalController.cs
Assets/Scripts/Day1/Day1PlayerController.cs
Assets/Scripts/UI/YutangWelcomeScreen.cs
```

不要依据 `work/day1` 的副本继续修改，除非先从 Unity MCP 读取最新脚本并明确同步。

## 10. 当前验证状态

已验证：

- 8080 Unity MCP 可连接。
- Unity 实例可锁定。
- Build Settings 中七个场景路径和顺序正确。
- `MainMenu` 和 `Day1_Hospital` 有序列化层级。
- `Day1_Hospital` 场景验证结果为 0 个缺失脚本和 0 个损坏 Prefab。
- Welcome 可进入 Day 1。
- 玩家移动和相机相对移动逻辑存在。
- 相机直接鼠标环绕、滚轮缩放、独立灵敏度、碰撞拉近有效。
- 医生交互和对话流程有效。
- 对话时光标释放。
- Welcome 到 Day 1 的灯光一致性已验证。

当前 Console 有一条 MCP 插件自身的旧 WebSocket 连接失败日志：

```text
MCP-FOR-UNITY: [WebSocket] Connection failed...
```

这是 MCP 插件传输层日志，不是项目脚本编译错误。当前 HTTP MCP 连接正常。后续验证时仍应检查是否出现新的项目脚本错误。

## 11. 已知风险

1. Day 1 对话结束会加载空的 `Day2_Home`，因此当前流程到这里中断。
2. `GameManager` 还没有本局状态，无法支撑积分、失败和结算。
3. 没有自动化测试。
4. 当前 UI 使用旧版 `UnityEngine.UI.Text`，可以继续使用以控制范围，不必立即迁移 TMP。
5. Day 1 场景较小，第三人称相机在墙边会明显自动拉近，这是预期行为。
6. 相机尚无肩位偏移、遮挡物淡出和输入设置菜单。
7. 场景构建器会重建 Day 1，手工修改 Day 1 前应理解其覆盖范围。
8. 旧交付说明容易误导后续 Agent，必须以本文和 Unity 实时状态为准。

## 12. 每轮 Agent 的完成标准

每次完成一个模块后：

1. 保存场景和资产。
2. 强制刷新脚本并等待编译完成。
3. Console 项目错误为 0。
4. 从上一个真实入口进入该模块，不只直接打开目标 Scene。
5. 验证成功、失败、重试和继续流程。
6. 退出 Play Mode。
7. 更新本文的完成状态、参数、已知问题和下一步。

## 13. 最终验收清单

- [x] 可从主菜单开始游戏。
- [x] 可进入 Day 1。
- [x] Day 1 可移动、寻找医生并完成对话。
- [ ] Day 1 完成后进入可玩的 Day 2。
- [ ] Day 2 血糖监测可玩且可重试。
- [ ] Day 2 早餐卡牌可玩。
- [ ] Day 3 饮食卡牌可玩。
- [ ] Day 3 跑酷可完成且可失败/重试。
- [ ] Day 4 一日卡牌可玩。
- [ ] Day 4 监测进阶可玩。
- [ ] Day 4 短版跑酷（可选）。
- [ ] Day 5 四轨音游可玩。
- [ ] Result 显示本局积分和历史最高分。
- [ ] 本地最高分可正确写入并在重启后保留。
- [ ] 完整流程无空场景和阻断。
- [ ] 医学相关文案不存在误导表达。

## 14. 下一位 Agent 的第一项任务

不要继续打磨 Day 1 画面。

下一步应先完成：

1. 扩展全局本局状态与积分系统。
2. 给 `Day2_Home` 建立序列化场景层级。
3. 实现 Day 2 血糖监测滑动窗口。
4. 实现 Day 2 早餐卡牌。
5. 从 MainMenu 经 Day 1 对话完整走到 Day 2 并验证。

完成后更新本文，而不是重新写一份相互冲突的交付说明。
