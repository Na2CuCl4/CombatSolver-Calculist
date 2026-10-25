# CombatSolver 开发笔记

这里只记录当前未发布的行为变化。发布定稿后将该批次完整移入历史卷；已有章节中的错误直接修正，不追加互相矛盾的“后续说明”。

0.50.1 本地定稿的智能药水修复及 PR #213、#224、#226 见[历史卷22](archive/development/volume-22.md)，玩家说明见[0.50.1 更新日志](releases/0.50.1-RELEASE_NOTES.md)。外部发布状态以对应标签及发布记录为准。

0.50.0 的 PR #207、PR #211、智能药水与组合补搜定稿及性能研究见[历史卷20](archive/development/volume-20.md)。其他记录见[归档索引](archive/development/README.md)。

## 回合结束、出牌续体与分段死亡

计划动作的 `EndsPlayerTurn` 依据实际开始回合结束流程的事实，不再仅依据下一玩家回合是否出现；卡牌和药水共用该事实，直接末击后被丢弃的结束请求不算已执行。边记录按值Fork，终局节点保留实际回合号，不改变玩法状态键。

出牌续体完成普通与晚期监听后派发一次自定义 `AfterCardResolved`；挂起时保留尾部，不提前结算。该回调负责适配器完成事实及待写入效果，不能用删除未完成事实绕过来源身份断言。

`ReattachPower.AfterDeath` 的行动切换在每次死亡边界立即执行：有其他存活段时切到 `DEAD_MOVE`，最后死亡段保持原行动；延后通道只处理死亡阶段及Power清理。重接资格按原生HP死亡条件判断，等待复活且HP为0的段不是存活同伴。

验证入口和本轮证明范围见测试入口的对应条目；不据这些技术修正作搜索强度或玩法平衡结论。

历史记录见 [归档索引](archive/development/README.md)。0.49.0 批次定稿与 PR #203、#204 合并记录见 [历史卷 12](archive/development/volume-12.md)，战斗状态修复记录见 [历史卷 11](archive/development/volume-11.md)。

玩家更新日志见 [0.49.0 更新日志](releases/0.49.0-RELEASE_NOTES.md)；日志站逐包处理及保留首因见 [排查记录](issues/0.48.0-hardbugs-20261003.md)。

0.49.1 紧急修复定稿见 [历史卷 13](archive/development/volume-13.md)，玩家说明见 [0.49.1 更新日志](releases/0.49.1-RELEASE_NOTES.md)。

## 下一版本（开发中）

2026-10-07：修复开局计划发现预查第三方卡牌时过早构造执行期选择规格。登记提供可选潜在效果元数据，缺省保持未知；root、手牌与抽牌堆分类读取元数据，Calculist 的选择牌明确声明 ModDefined。真实出牌规格和缺历史／生成选项的严格拒绝保持。独立回归复用 Designer 的选择差分夹具和原失败战斗输入；验证范围见 `TEST_MATRIX.md`，不改玩法数值或自然局原始结果。

2026-10-05：修复卡牌隐式全选产生虚构页面计划的问题。`ResolveActionCardChoice` 对已声明的确定全选立即调用原有 `CardChoiceSupport.Apply`；条件卡牌登记允许返回 nullable spec，Calculist 三段论未达到两次先前技能时不再产生空选择。显式页面和计划游标的额外选择拒绝保持。失败的自然战斗与零/单候选原生夹具保留在 Designer 独立批次，修复验证入口见 `TEST_MATRIX.md`；不修改产品玩法数值、RNG 或比较字段。

Calculist 仿真分叉从上游 0.49.1 固定基线迁入已有隔离宿主适配，当前上游同步基线包含 0.50.1 及后续未发布改动：自定义根/分支状态、逐牌语义键、严格镜像门禁、Power 生命周期与当前侧 continuation 历史。程序集改为 `CombatSolver.Calculist`，保留命名空间，默认禁用部署。分叉持有通用求解器契约，隔离宿主、Calculist 适配与测试编排由相邻 `../CalculistDesigner/simulation/` 维护，产品由 `../Calculist/` 维护。维护源码是唯一权威，准备阶段冻结源码并直接编译；不再临时改写上游检出。版本号仍描述上游基线，不代表这些 dirty 修改已经提交或发布。

本轮验证入口及局限见[测试入口](TEST_MATRIX.md)。不修改产品数值，不部署，不启动游戏客户端，不发布。

永久牌组新增有序分支状态：Swipe 原生移除具体 `DeckVersion`，此前预测仅移出战斗，观察器也错误沿用冻结牌表。现在根实例/监听来源、Fork 移除集合、指纹、continuation 与宿主观察投影保持一致；逃跑、死亡奖励及领取顺序分别保留原生行为。另修复数量缓存的读写混淆：`PowerAmountPredictionState.HasChanges` 和拥有者/来源成员门禁避免死亡清理后的只读 Flutter 数量重新写回。两者为状态语义修复，不调整卡牌、怪物或搜索评分。

完整矩阵发现状态牌弃牌顺序差异时，首因在 Calculist 隔离宿主的 `CampaignTurnFlow.EndPlayerTurn`：宿主只调用 `OnTurnEndInHandWrapper`，未复现原版 `CombatManager.DoTurnEndCards` 的 Play → 效果 → Discard/Exhaust 移动，导致状态牌在普通手牌 flush 时才按原手牌顺序弃掉。已补齐原版公开命令顺序，适用于枯萎、感染、灼伤及所有手牌回合末效果；求解器的该语义原本正确，不对怪物生成牌做排序补偿，严格有序牌堆比较保持。

`BattlewornDummy` 的计时 Power 超时分支沿原版 Escape 处理：事件败北标记后调用现有 `CreatureEscaped`，移除该敌人的活动/已知阵容与自身 Power。原版 timeout 不触发伤害、Kill 或 AfterDeath；初始错误是预测仅标记 EventDefeat 却保留敌人与能力，终局 continuation 仍多出 E0。该修复与宿主状态牌顺序修复的实际结果按新冻结批次另记，不沿用迁移烟测。

`TestSubject.MULTI_CLAW_MOVE` 在实际攻击杀死玩家后仍递增下一次多段次数；batch-03 标准 A0/升级牌组终局为预测 `1,2/0`、真实 `1,3/0`，batch-04 高生命阶段探针同样为 `1,28/0` 对 `1,29/0`。首因是 `MonsterMoveSemantics` 在 playerDied 时跳过整个原生方法尾部。现在由单一精确谓词准入已核对的 Fabricator/FABRICATING_STRIKE_MOVE、TestSubject/MULTI_CLAW_MOVE、OwlMagistrate/VERDICT 与 WaterfallGiant、GasBomb 的 EXPLODE_MOVE，继续使用既有 `Apply`。计数、移除 Soar、召唤和自杀均保留各自唯一权威；不是把未知致命行动全部继续执行。新批次的实际验证另记。

KnowledgeDemon A10/升级牌组在 batch-06 第 10 回合，玩家 HP5 被结束阶段二晚期的 DisintegrationPower8 击杀。`AdvanceRound` 先前只在阶段一之后提交终局，阶段二之后仍进入 Enemy，造成 continuation 的 Y 从真实 `0/0/2/2` 误清为 `0/0/0/0`、AttackStarts 从 2 误清为 0，并把自定义 WindowSide 从 1 改为 2。现在在完整阶段二与死亡补偿之后执行 `CheckWinCondition`，保持已开始 Hook 的结算顺序，并在侧切换前停止；不修改 History 值或比较规则来补偿。该失败与原录制牌序保留，修复验证使用新冻结批次重放同一阶段死亡。
