# 《七页轮回》 SevenPages

对抗式卡牌游戏 · 2 人对战 + AI 陪练 · Unity 2022.3（中国版）

> 你的命，就是这本书的七页。每受一次伤，墨就渗开一页；
> 七页撕尽，你在轮回中重活一次——但业障会留下，记忆也会留下。
> 第三次轮回，你就回不来了。

---

## 目录结构

```
SevenPages/
├── Assets/
│   ├── Scripts/
│   │   ├── Core/          ← 纯 C# 规则引擎（无 UnityEngine 依赖）
│   │   └── Game/          ← Unity 表现层（代码构建 uGUI）
│   ├── Editor/            ← 工程初始化工具（同步卡牌、生成场景）
│   ├── Scenes/            ← 对战场景（由工具生成）
│   └── Resources/         ← cards.json 运行期副本
├── cards/cards.json       ← 卡池 + 卡组配方的唯一来源
├── tools/SevenPages.Sim/  ← 无头模拟器 / 自测器（.NET 8）
├── docs/GDD.md            ← 设计文档
└── ProjectSettings/ Packages/
```

## 核心设计（一句话版）

**书页即血量。** 每人 7 页 × 每页 3 墨 = 一条命；七页撕尽触发轮回（业障入牌库、宿慧 +1）；
第三次轮回即败北。每回合基础灵 3、抽 2 张、手牌上限 10。

## 怎么跑

### 1. 规则自测 + 模拟（不需要 Unity）

```bash
export PATH=/d/dotnet:$PATH
cd tools/SevenPages.Sim
dotnet run -- test      # 规则自测
dotnet run -- demo      # 打一局并打印完整日志
dotnet run -- sim "" 500  # 跑 500 局 AI 对战，看平衡数据
```

### 2. Unity 客户端

用 Unity 2022.3.62f2c1 打开本目录，菜单 **七页轮回 → ③ 一键初始化**，
然后打开 `Assets/Scenes/Battle.unity` 点 Play。

## 加一张新卡

只改 `cards/cards.json`：

```json
{
  "id": "new_card",
  "name": "新卡",
  "type": "Spell",
  "target": "Enemy",
  "cost": 2,
  "text": "造成 3 点墨伤。",
  "art": "placeholder_new_card",
  "effects": [ { "kind": "Damage", "amount": 3 } ]
}
```

再在 `starterDeck` 里加入 `"new_card"` 即可。**引擎代码不用动。**

## 当前状态（Milestone 0）

- [x] 纯 C# 规则引擎（页/墨/轮回、回合、抽牌、疲劳、效果结算）
- [x] 10 张基础牌，数据驱动
- [x] 贪心 AI
- [x] 无头模拟器（500 局平衡报告）
- [x] 规则自测 26 项
- [x] Unity 表现层（代码构建 UI）
- [ ] 占位美术（千问生成）
- [ ] 后手补偿平衡
