# CombatSolver 测试入口

按改动选择最小验证层，方法见 [无人测试](HEADLESS_TESTING.md) 与 [社区验收](community/testing-guide.md)。以下命令提供当前复跑入口，不表示本轮已执行。

强制结束动作使用既有 `forced-turn-terminal-v0111` / `potion-forced-turn-terminal-v0111`，同时检查卡牌／药水子节点、没有下一玩家回合的终局、直接末击丢弃结束请求及Fork边记录。出牌续体使用 `MIRRORED-HOOK-FILTER` / `CARD-EXECUTION-CONTINUATION`；普通、捕获、挂起与恢复按每个CardPlay核对完成回调次数。分段死亡使用 `mercury-reattach-boundary-v0111`，包括逐次多击死亡、最后一段行动保留及HP0等待者的重接资格。

隔离Native的 `--native-boundary-contracts` 复用AfterPlayCaptureMask断言，核对适配器回调与live不变；`run_adapter_gate.py --case hooks-double-proof-auto-capture --case hooks-poincare-end-replay-capture` 实际启用执行捕获，比较完整原生／预测状态，并修改子分支、父分支与live后分别验证根冻结和兄弟隔离。原蜈蚣10回合录制的40个状态边界、停笔原输入同预算新搜索、分支身份原输入新搜索分开记账；旧计划的错误结束标记保持原样，不自动改写后声称精确重放通过。

第三方选择的出牌前分类回归使用同一 `run_adapter_gate.py` 的 `b-Syllogism-choice-last`、`b-RenewFoundations-choice-last`、`b-LiteratureSearch-choice-last`、`b-BreathingRoom-choice-last`、`b-Permutation-choice-last`、`b-FromThisToThat-choice-last`、`b-AssociativeLaw-choice-last` 和条件无页面代表。现有 CardsB 固定计划夹具在出牌前检查潜在效果元数据，并核对依赖历史／生成选项的执行规格仍明确拒绝，之后按原完整计划进行 native/predicted 差分。原失败战斗 input 的独立新搜索保持原预算和严格 continuation；其结果不替换原错误，也不算新自然路线。

卡牌条件无选择与隐式全选的最小原生回归由 Designer 的 `run_adapter_gate.py` 执行：`b-syllogism-unqualified`、`b-syllogism-one-previous-skill`、`b-syllogism-qualified-no-attacks`、`b-syllogism-qualified-one-attack`、`matrix-b-Syllogism-u0`、`native-implicit-hologram-single` 和 `native-implicit-prepared-short`。夹具交给 Native 的是完整固定计划，不过滤隐式条目；比较完整有序状态、九条 RNG 与 continuation，后两个原版代表覆盖单候选与候选不足所需数量。旧错误录制计划的消费拒绝以及原自然战斗输入的新 1000 ms 生产搜索另记为负例与真实战斗回归，不能计作新的自然路线。

2026-10-05 本次结果：Designer `artifacts/simulation/native-choice-20261005-fixed-gate/verification.json` 的上述七例及 Seeker/Sly 两条嵌套代表共 9 例通过。`native-choice-20261005-battle-search/verification.json` 保持原失败战斗 input 字节、1000 ms 预算与所有比较字段，五回合胜利、19 次严格对账全等；新搜索没有选择三段论，三段论具体分支由上述配对夹具证明。`native-choice-20261005-recorded-negative/consumption-audit.json` 确认原四回合录制动作未改写，第四回合三段论多出的空选择被 `TurnStartChoiceCursor.AssertConsumed` 拒绝，拒绝前 15 次比较全等。失败的零/单攻击夹具分别保留于 `native-choice-20261005-before-zero` / `native-choice-20261005-before-one`，旧自然路线错误保留于 `p0-20261005-final-a0`。这只是最小修复回归，完整原 seed 路线仍由后继新冻结批次验收。

历史记录见 [归档索引](archive/testing/README.md)，0.48.1 的验证、失败与未验证项见 [历史卷 12](archive/testing/volume-12.md)。

0.49.0 的行为验证沿用本页 PR #203、#204 合并验收与 [战斗状态修复验证](archive/testing/volume-13.md)。版本及发布文档调整采用 L0 检查和发布构建；原有未验证项保留。

## 0.49.1 内存提交回归（2026-10-04）

Calculist 分叉的当前准备入口为相邻 `../CalculistDesigner/simulation/scripts/run_simulation.py prepare`：冻结当前 fork HEAD + dirty source、构建 `CombatSolver.Calculist.dll`，随后通过 Designer 持有的隔离 Native 宿主与 Calculist 适配源码执行严格镜像和表达式夹具。产品输入来自相邻 Calculist；新批次默认写入 Designer 的 `artifacts/simulation/`。`CopyModOnBuild=false` / `SkipMemoryCleanerOnBuild=true`；该入口不部署或启动玩家客户端。当前语义验证结果以各批次的 manifest、fixture 输出和源码哈希为准，不能沿用上游历史通过记录。

下文 2026-10-04 各批次的 `artifacts/` 路径均指迁移前相邻 `Calculist/artifacts/` 的历史本地证据，保留原位置与冻结路径；它们被 Git 忽略，不随源码提交。新检出可从 Designer 的当前入口、兼容的已安装游戏/Mod 和新的输出目录重建，不要求旧批次存在；复核某次历史结论则需要保留该次冻结证据。仓库拆分不会把这些历史结果记为本次重跑通过。

新增状态域的本轮证据位于相邻 Calculist 的 `artifacts/simulation/content-validation-20261004/`：第 10 批数量缓存 8 项合约及两场原 Flutter 致命回归通过；第 11 批永久牌组 2 条 steal/escape/death-reward/claim 完整链及两场原 Flutter 条件回归通过。覆盖具体实例/有序牌组、父子/兄弟 Fork、指纹与 continuation、监听成员过滤、dirty 正负对照。第 11 批最终标准矩阵 356/356 严格有效，178 升级配对、8359 完整原生差分通过；4 项布局单列，夹具不计自然样本。各批次保留其原源码/DLL 哈希及失败原件，完整交付范围以 Calculist 总报告为准。

本轮 L0 已通过 `REFACTOR_BOUNDARIES_OK search_files=247`、`TOOLS_OK files=290 projects=37`；文档检查仅有既存社区 Q008/Q009/Q010 草稿的 3 条缺失 ZIP 链接，保持原状。构建过程的 `ValidateCalculistForkSources` 要求 Entry、BeamSolver 与两份扩展确实参与 Compile，并记录实际编译项、解析引用及导入项目哈希。第一次输出目录包含源码导致 SDK 排除源码的空壳构建已修复；第二次构建日志显示 `CALCULIST_FORK_COMPILE_ITEMS 804` 并成功编译 Native，随后旧 DLL 键导致适配 preflight 失败，现已同步维护入口；这两批失败不计为语义通过。原版检出在构建前由 assembly 名称门禁拒绝，不创建产物。

最终迁移烟测由 Calculist 隔离宿主在 `artifacts/simulation/content-validation-20261004/batch-03` 执行：完整 fork Release 编译，804 个 Compile 项、978 个实际编译输入哈希，Native 编译成功。`migration-adapter-03/verification.json` 的 `vanilla-strike` 与 `putpen-base` 两项 Passed，逐动作核对 continuation、资源、有序牌堆、Power/遗物/药水、牌组、表达式、阵容与九条 RNG，并通过父/兄弟 Fork 隔离；仅证明所选单效果夹具，不声称自然完整牌池或爬塔通过。源码树 SHA-256 为 `2c8fc08a577ec4e60a3b89c8c797db68010c86afe6ce36c56ee27340ff1c3391`，DLL 为 `939a61c9e4f2e8b2de9d0c6744e8e6f578dbf38f8e274c0f7d35c446f9cdb16e`；HEAD 仍是固定基线，迁移源码在 dirty/source 哈希中记录。

CoverageCatalog、OfflineSearchHarness、CheckpointTool 三个受影响工具 Release 编译成功，均 0 warning / 0 error；前两者引用 batch-02 的完整 fork DLL 验证更名后的 API/程序集消费，不替代上述 batch-03 语义烟测。

`BattlewornDummy` 超时修复在 Calculist `content-validation-20261004/batch-04` 的 `dummy-target-04/report.json` 与 `matrix-summary.json` 验证：V2/V3 各一项 A0、未升级固定牌组，合计 2 个 `event_defeat`；每项 4 次严格比较全部相等，GatePassed/AllComparisonsStrict 均 true。终局 live/predicted 都没有 E0 与 TIME_LIMIT_POWER，Victory=false，原生遭遇 `RanOutOfTime=true`。仅证明两个代表，不声称 A10/升级全矩阵或多敌逃跑；batch-03 的失败原样保留。

宿主回合末状态牌移动修复在同一 batch-04 的 `status-turnend-04/matrix-summary.json` 验证：AeonglassBoss、DenseVegetationEventEncounter、MechaKnightElite × A0/A10 × 基础/升级牌组，12/12 真实终局（11 dead、1 win），error/unsupported/truncated 均 0，全部严格比较通过且每项预算核对 true。覆盖枯萎、感染和灼伤三个效果族；比较仍保持完整 continuation 与有序牌堆，未改为排序或集合相等。固定 seed 17、1000ms、单并发、原始 6 张牌组与资源。batch-04 fork 源码树 SHA-256 为 `f198db5ee76255804696f0daad63ea8b5ffe64ed5ba224b32f1fe46139ad0ce9`，DLL 为 `a4380da87ecf95e4cc40299b67e160e3c617a5a5e73d9dfc8f071940dde4203b`；宿主源码与输入哈希由该批次单独冻结。这是固定遭遇回归，不能当作自然爬塔胜率或未执行形态的证据。

致命行动尾部的实验体代表在 `batch-05` / `subject-target-05` 验证：标准 TestSubjectBoss、A0/升级牌组、seed 17、1000ms，1 个真实 dead 终局，GatePassed/AllComparisonsStrict=true。第 6 回合原生 MULTI_CLAW_MOVE 从玩家 HP2、格挡10打至 HP0；终局完整 continuation 相等，MS0 双方 `1,2/0`，对应多段次数在致命攻击后递增。该代表已走到目标尾部，无须重复高生命失败场景；不声称第三形态或其他四个已审计尾部在此场得到覆盖。单一精确准入、原有 Apply 唯一权威的结构门禁通过；其他尾部由其独立夹具与矩阵分别证明。

Owl 的致命 VERDICT 使用原 batch-03 录制牌序在 `owl-lethal-replay-06/verification.json` 验证：第 8 回合攻击前实际存在 Soar，攻击后玩家 HP0、Owl 存活 65/247；45 次严格比较、零差异、GatePassed=true，终局双方既无 Soar 也无对死亡玩家新施加的 Vulnerable。`owl-lethal-replay-05` 的 Vulnerable 失败与普通 `owl-target-05` 在 PECK 死亡的有限通过分别保留；普通死亡不能替代 VERDICT 尾部证明。

`knowledge-choice-06` 的 KnowledgeDemonBoss A10/升级牌组第 10 回合在玩家阶段二死亡，完整差分因 Y/AttackStarts/WindowSide 失败；该失败保留为晚期阶段终局边界基线。维护源码在完整 phase-two + death-compensation 后补安全终局检查；`knowledge-phase2-replay-07/verification.json` 使用原 10 回合 RecordedPlans 逐动作原生重演与修复后的生产预测，全部计划 Mode=recorded-combat-replay、Nodes=0，46 次严格比较零差异，GatePassed=true。第 10 回合实际 HP5、Disintegration8 在结束阶段二死亡，不进入 Enemy；双方终局 turn=10、Y=`0/0/2/2`、AttackStarts=2、WindowSide=1，Outcome=dead、Victory=false。这是同一阶段死亡的精确回放，不由重新搜索或普通敌方攻击死亡替代；provenance 单独记录新批次与原输入/输出、DLL 哈希。

SoulNexus 的隔离宿主表现查询修复在 `soulnexus-kill-replay-07/verification.json` 验证：按原 matrix-06 牌序重演，第 7 回合 Verify 从 54/234 击杀目标，玩家 HP7、原生阵容清空，42 次完整严格比较零差异，win/Victory=true、进程退出0。精准 null-room 查找替换保留原 AfterDeath 的 Died 取消订阅与玩法死亡链；没有另行声称事件处理器数量已被探针核对。旧表现查询 NRE 保留，不将其误记为求解器或产品数值错误。

`CombatSolver.GcPolicyChecks` 直接编译生产 Runtime 内存代码。`default-commit` 在未改生产代码时 Failed：显式回退后分配上限仍有效；修复后 2 项 Passed，覆盖普通检查点保留限额、不可分割提交完成回收后续行、后续请求重新建立限额与取消保持原准入。

```bash
dotnet run --project tools/testing/checks/CombatSolver.GcPolicyChecks/CombatSolver.GcPolicyChecks.csproj -c Release -- default-commit
```

相邻真实 CLR 回归：`default-entry` 2 项、`recovery-lifecycle` 3 项 Passed，包含实际 NoGC 退出与恢复、显式退出持续生效及诊断失败清理。每项设有 15 或 20 秒截止时间。未重放 18 份原包整场，也没有可见 Steam 或低内存宿主性能结论；[排查记录](issues/0.49.0-memory-commit-regression-20261004.md)保留固定报告身份与触发窗口。

## PR #203 的机制合同

金币、最大生命回复与遗物间接回复的贡献者证据见 [专题记录](performance/gold-max-hp-healing-20261003.md)。独立场景为 `GOLD-HEALING-MECHANISMS`、`MAX-HP-HEALING-CALLBACKS`、`FEED-MAX-HP-CAP`、`RELIC-MAX-HP-HEALING-BOUNDS` 和 `AXEBOT-JOSS-DEFERRED-FORK`，均从平台原生无人入口运行，使用 IRONCLAD、FUZZY_WURM_CRAWLER_WEAK、120 秒上限及实例清理。原作者结果与本轮合并验证分别记账。

金币和回复合同同时传 `-EvidenceDirectory .local/validation/pr203/<场景>`（Bash：`--evidence-directory`），初始生命为 50/80。金纸合同还传 `-RelicsPath coverage/fixtures/regressions/axebot/axebot-joss-deferred-relics.json`（Bash：`--relics-path`）。搜索阶段顺序合同为 `KNOWN-HEALING-MEMBERS` 和 `KNOWN-HEALING-OPENING`，使用 SILENT；受影响的固定前缀合同使用 IRONCLAD，强制结束回合牌合同使用 REGENT。

## 当前矩阵命令

矩阵启动器只读取本节的平台原生命令；历史记录不作为自动执行清单。以下两项分别检查倾泻手空边界与相邻效果作用域，不运行整场搜索。需要当前游戏及 RitsuLib 路径，矩阵启动器统一管理实例并在结束时清理。

```powershell
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId CASCADE-EMPTY-HAND-NATIVE -EnemyCurrentHp 1000 -HeadlessFastModeForTest Instant -DeploymentFastModeForTest Instant -DeploymentInterActionDelaySecondsForTest 0 -TimeoutSeconds 120
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId EFFECT-SCOPE-ADJACENT-CONTRACT -EnemyCurrentHp 1000 -HeadlessFastModeForTest Instant -DeploymentFastModeForTest Instant -DeploymentInterActionDelaySecondsForTest 0 -TimeoutSeconds 120
```

```bash
./tools/testing/run-unattended-test.sh --scenario-id CASCADE-EMPTY-HAND-NATIVE --enemy-current-hp 1000 --headless-fast-mode-for-test Instant --deployment-fast-mode-for-test Instant --deployment-inter-action-delay-seconds-for-test 0 --timeout-seconds 120
./tools/testing/run-unattended-test.sh --scenario-id EFFECT-SCOPE-ADJACENT-CONTRACT --enemy-current-hp 1000 --headless-fast-mode-for-test Instant --deployment-fast-mode-for-test Instant --deployment-inter-action-delay-seconds-for-test 0 --timeout-seconds 120
```


## 0.48.0 硬错误机制合同

这些场景各自停止在共享首因或必要跨回合边界。完整runId、失败基线和未验证项见[历史卷13](archive/testing/volume-13.md)，逐包分类见[问题记录](issues/0.48.0-hardbugs-20261003.md)。

```powershell
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId NATIVE-CHOOSE-OPEN-GATE -CharacterId SILENT -EncounterId CHOMPERS_NORMAL -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId CALCULATED-GAMBLE-ORDERED-DISCARD -CharacterId SILENT -EncounterId CHOMPERS_NORMAL -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId FROZEN-LIGHTNING-CHANNELS -CharacterId DEFECT -EncounterId MECHA_KNIGHT_ELITE -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId REPORT-ROUND-IMBALANCED -CharacterId NECROBINDER -EncounterId BOWLBUGS_NORMAL -InitialPlayerHp 200 -InitialPlayerMaxHp 200 -ClearPlayerHand -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId SECOND-WIND-REPORT-ROOT -CharacterId IRONCLAD -EncounterId DECIMILLIPEDE_ELITE -InitialRoundNumber 3 -InitialPlayerTurnNumber 3 -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId GROUP-DEBUFF-REACTIVE-DRAW -CharacterId NECROBINDER -EncounterId CHOMPERS_NORMAL -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId CONSTRUCT-REAPER-ARTIFACT -CharacterId NECROBINDER -EncounterId CONSTRUCT_MENAGERIE_NORMAL -InitialRoundNumber 3 -InitialPlayerTurnNumber 3 -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId FIXED-PREFIX-POTION-POLICY -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId FIXED-PREFIX-TURN-OUTCOMES -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId MIRRORED-HOOK-FILTER -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId FROZEN-ROOT-LISTENERS -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId CALCULATED-HISTORY-FREEZE -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId VOID-FORM-TURN-CHOICES -CharacterId REGENT -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId FLATTEN-MUSIC-BOX-ENTRY -CharacterId NECROBINDER -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId SEEKER-ORDERED-OPTIONS -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId RITUAL-TEMPORARY-STRENGTH -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId NOXIOUS-RAMPART-ORDER -CharacterId IRONCLAD -EncounterId TURRET_OPERATOR_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId EVIL-EYE-EXHAUST-HISTORY -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId BLOCK-SPEC-CARD-PLAY-IDENTITY -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId ROUTE-ADOPTION-LIFETIME -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId PAELS-LEGION-FINISHED-REFERENCE -CharacterId SILENT -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId END-TURN-RISK-LOSS-ACCOUNTING -CharacterId DEFECT -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId MAKE-IT-SO-FULL-HAND -CharacterId REGENT -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId CARD-COST-IDENTITY-CONTRACT -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId DAMPEN-REACTIVE-ROCKET-PUNCH -CharacterId DEFECT -EncounterId KNIGHTS_ELITE -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId DAMPEN-REACTIVE-MELANCHOLY -CharacterId NECROBINDER -EncounterId KNIGHTS_ELITE -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
pwsh -NoProfile -File tools\testing\run-unattended-test.ps1 -ScenarioId RADIANT-PEARL-ENTRY -CharacterId IRONCLAD -EncounterId FUZZY_WURM_CRAWLER_WEAK -EnemyCurrentHp 1000 -TimeoutSeconds 120 -CleanupInstanceOnExit
```

```bash
./tools/testing/run-unattended-test.sh --scenario-id NATIVE-CHOOSE-OPEN-GATE --character-id SILENT --encounter-id CHOMPERS_NORMAL --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id CALCULATED-GAMBLE-ORDERED-DISCARD --character-id SILENT --encounter-id CHOMPERS_NORMAL --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id FROZEN-LIGHTNING-CHANNELS --character-id DEFECT --encounter-id MECHA_KNIGHT_ELITE --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id REPORT-ROUND-IMBALANCED --character-id NECROBINDER --encounter-id BOWLBUGS_NORMAL --initial-player-hp 200 --initial-player-max-hp 200 --clear-player-hand --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id SECOND-WIND-REPORT-ROOT --character-id IRONCLAD --encounter-id DECIMILLIPEDE_ELITE --initial-round-number 3 --initial-player-turn-number 3 --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id GROUP-DEBUFF-REACTIVE-DRAW --character-id NECROBINDER --encounter-id CHOMPERS_NORMAL --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id CONSTRUCT-REAPER-ARTIFACT --character-id NECROBINDER --encounter-id CONSTRUCT_MENAGERIE_NORMAL --initial-round-number 3 --initial-player-turn-number 3 --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id FIXED-PREFIX-POTION-POLICY --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id FIXED-PREFIX-TURN-OUTCOMES --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id MIRRORED-HOOK-FILTER --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id FROZEN-ROOT-LISTENERS --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id CALCULATED-HISTORY-FREEZE --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id VOID-FORM-TURN-CHOICES --character-id REGENT --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id FLATTEN-MUSIC-BOX-ENTRY --character-id NECROBINDER --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id SEEKER-ORDERED-OPTIONS --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id RITUAL-TEMPORARY-STRENGTH --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id NOXIOUS-RAMPART-ORDER --character-id IRONCLAD --encounter-id TURRET_OPERATOR_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id EVIL-EYE-EXHAUST-HISTORY --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id BLOCK-SPEC-CARD-PLAY-IDENTITY --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id ROUTE-ADOPTION-LIFETIME --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id PAELS-LEGION-FINISHED-REFERENCE --character-id SILENT --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id END-TURN-RISK-LOSS-ACCOUNTING --character-id DEFECT --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id MAKE-IT-SO-FULL-HAND --character-id REGENT --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id CARD-COST-IDENTITY-CONTRACT --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id DAMPEN-REACTIVE-ROCKET-PUNCH --character-id DEFECT --encounter-id KNIGHTS_ELITE --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id DAMPEN-REACTIVE-MELANCHOLY --character-id NECROBINDER --encounter-id KNIGHTS_ELITE --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
./tools/testing/run-unattended-test.sh --scenario-id RADIANT-PEARL-ENTRY --character-id IRONCLAD --encounter-id FUZZY_WURM_CRAWLER_WEAK --enemy-current-hp 1000 --timeout-seconds 120 --cleanup-instance-on-exit
```

## 社区批次 B013 回归夹具（2026-10-03，Refs #172）

以下为贡献者在 PR #204 记录的结果，与本轮验证分别记账。五个主题的独立机制回归在 `src/Testing/Regressions/Community/B013*Checks.cs`，命令统一为
`tools/testing/run-unattended-test.ps1 -ScenarioId <ID> -TimeoutSeconds 120 -CleanupInstanceOnExit`（角色/遭遇按夹具要求，详见 PR #204）：

- `B013-BLOCK-DECIMAL-BOUNDARY`（T010，格挡乘法 Decimal 边界）：修改前 `dec22b8f` Failed（栈同报告）→ 修改后 `e2757311`/`a23dcf21` Passed；代表包 DOP1 哨兵路线与展开/转移一致。
- `B013-FIXED-PREFIX-TERMINAL-BOUNDARY`（T008，固定前缀越过锁定终局）：`971ccdd1` Failed → `0b3f96a6` Passed；代表包哨兵 22809/129762、终局一致。
- `B013-FIXED-PREFIX-TURN-END-CARD`（T007，结束回合卡固定前缀）：`eb64242d` Failed → `899ef881` Passed。
- `B013-RADIANT-PEARL-HAND-DRAW`（T006，抽牌前遗物生成对账）：`cd227ff6` Failed → `15c80923` Passed；代表包 SearchOnly 失配 → `TURN_SETUP_STATE_MATCH`。
- `B013-DEFAULT-GC-LIMIT`（T009，默认 GC 请求分配边界）：`6ac0f466` Failed（`limit=long.MaxValue`）→ `7c6f3b83` Passed。

变基到 0.48.1（`2ead87d9`）后五夹具复跑 Passed：`0f19174f` / `57e2086d` / `9d7173af` / `3892e307` / `fdfc8734`。代表包哨兵在新基线：T010 DOP1 与旧基线逐字段一致（展开 7426、转移 77129、4 回合胜、finalHp 71），耗时 3458.9ms；T009/T006 代表包 7 回合胜、finalHp 88、boundary None（展开 26194、转移 190351，路线随上游搜索改动微调）。

完整修改前后、哨兵与未验证项见 PR #204；批次状态与剩余项（T007 第二样本未复现）同样记录在该 PR。

## PR #203、#204 合并验收（2026-10-04）

以下为本轮直接运行结果；全部使用120秒上限和实例清理，已通过的请求未重复运行。无头验证停在对应机制边界。

| 场景 | runId | 结果与范围 |
| --- | --- | --- |
| `GOLD-HEALING-MECHANISMS` | `3bc7928290c343cca88248c0c28dd95e` | Passed；金币修正、三个回调、完整原生状态/RNG和父分支隔离 |
| `MAX-HP-HEALING-CALLBACKS` | `cd6549478924451882fc600e67724a53` | Passed；最大生命实际增量、封顶与回复回调的原生差分 |
| `FEED-MAX-HP-CAP` | `ddcf661e391c4013a0aa87861f8542f3` | Passed；两次致命出牌的实际增量与成长计数 |
| `RELIC-MAX-HP-HEALING-BOUNDS` | `bee94b31824d449e8f7e9fa55cbe94f5` | Passed；两项直接原生遗物回调与熔化来源排除 |
| `AXEBOT-JOSS-DEFERRED-FORK` | `0fd9902a8a594e36a0a9fbf533ceee65` | Passed；根计数、零状态指纹、父子/兄弟Fork与逐分支消费 |
| `KNOWN-HEALING-MEMBERS` | `fab7a8170a3942e9920e09fa466aa34e` | Passed；严格增量、DOP2、控制质量、成长门与live隔离 |
| `B013-FIXED-PREFIX-TERMINAL-BOUNDARY` | `5b097fd5f17745a0868a6b49d7af3b37` | Passed；终局前缀截断与可交付胜利 |
| `B013-FIXED-PREFIX-TURN-END-CARD` | `c778f285a52a4b9fbbe0e370688e6ec1` | Passed；强制结束回合卡的固定前缀推进 |
| `B013-RADIANT-PEARL-HAND-DRAW` | `ca904a20eafd43b690d8c824f60e2069` | Passed；抽牌前生成的原生数量、升级与归属对账 |
| `B013-DEFAULT-GC-LIMIT` | `3e440c65f9564b5ea6a65d094521b8d7` | Passed；限额、真实Gen2回收续搜、退出清理与CLR模式 |
| `KNOWN-HEALING-OPENING` | `3fecd35f80a34cb792a43ca3b5bc3b1c` | Passed；非认证根延后计划、单次执行与控制质量 |
| `FIXED-PREFIX-TURN-OUTCOMES` | `b18d23d1829b4438a557a5f75acf6425` | Passed；独立前缀完整状态oracle、多回合结果与终局截断 |
| `B013-BLOCK-DECIMAL-BOUNDARY` | `b556026e627c4153bb3d2bf23948a095` | Passed；原生虚弱倍率、完整状态/RNG；模拟96/95/200层与零格挡 |

真实CLR工具：`default-entry` 2项、`diagnostic-failure` 8项、`scopes` 8项 Passed。入口失败基线为日志抛错后信号仍启用；修复后同异常传播、信号清理及后续独占准入均通过。

失败记录：首次无头启动在私有进程身份检查处失败并清理，未进入游戏测试，原因未确定；金币首轮14项对账已通过，但缺EvidenceDirectory导致产物写入失败；金纸首轮缺遗物输入。补齐请求参数后仅重跑失败请求，成功记录在上表。固定前缀09f94286012d420d81242f480ebd1803仍执行旧的终局拒绝断言，合同更新为开局探测和完整搜索共同截断，并断言终局动作数及回合。新增格挡夹具初次编译因原生调用参数及私有setter失败，修正后Release零警告/错误。

L0：原生回复审计工具迁入tools/inspection后构建和真实DLL扫描通过。CoverageCatalog重新生成3035项目录，状态字段未分类为0；`--verify-state-writes`仍因既有InfusedCore.AfterSideTurnStart缺运行证据失败（1项）。PowerShell结构检查247文件通过，工具检查290文件/37项目通过，文档427文件/1635链接及覆盖目录检查通过。额外Bash结构检查因运行耗时停止，未完成；两平台脚本静态语法检查通过。

范围：本轮没有重跑作者长预算全根性能筛查，没有验证低内存玩家宿主、原生整场部署或可见Steam性能；原作者失败与未验证项保留在所属报告。
