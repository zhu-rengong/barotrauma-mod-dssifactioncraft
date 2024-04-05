# DSSI(Dynamic Submarine Script Injector) Faction Craft 派系工艺 (Abbreviation:DFC) v0.1

- [DSSI(Dynamic Submarine Script Injector) Faction Craft 派系工艺 (Abbreviation:DFC) v0.1](#dssidynamic-submarine-script-injector-faction-craft-派系工艺-abbreviationdfc-v01)
    - [简介](#简介)
    - [重生系统](#重生系统)
    - [初始化](#初始化)
      - [内部编辑器工具实现(可选,推荐)](#内部编辑器工具实现可选推荐)
    - [工具](#工具)
      - [☑ 初始化器(dfc\_initializer) C#组件(DfcInitializer)](#-初始化器dfc_initializer-c组件dfcinitializer)
      - [☑ 新增出生区(dfc\_newspawnpointset) C#组件(DfcNewSpawnPointSet)](#-新增出生区dfc_newspawnpointset-c组件dfcnewspawnpointset)
      - [☑ 新增派系(dfc\_newfaction) C#组件(DfcNewFaction)](#-新增派系dfc_newfaction-c组件dfcnewfaction)
      - [☑ 新增职业(dfc\_newjob) C#组件(DfcNewJob)](#-新增职业dfc_newjob-c组件dfcnewjob)
      - [☑ 新增装备(dfc\_newgear) C#组件(DfcNewGear)](#-新增装备dfc_newgear-c组件dfcnewgear)
      - [☑ 允许派系重生(dfc\_allowrespawn) C#组件(DfcAllowRespawn)](#-允许派系重生dfc_allowrespawn-c组件dfcallowrespawn)
      - [☑ 添加或移除职业(dfc\_addorremovejob) C#组件(DfcAddOrRemoveJob)](#-添加或移除职业dfc_addorremovejob-c组件dfcaddorremovejob)
      - [☑ 添加或移除装备(dfc\_addorremovegear) C#组件(DfcAddOrRemoveGear)](#-添加或移除装备dfc_addorremovegear-c组件dfcaddorremovegear)
      - [☑ Lua组件(dfc\_luacomponent) C#组件(DfcLuaComponent)](#-lua组件dfc_luacomponent-c组件dfcluacomponent)
      - [☑ Lua组件2代(dfc\_lua2component) C#组件(DfcLua2Component)](#-lua组件2代dfc_lua2component-c组件dfclua2component)
        - [Chunk块所在作用域包含的变量列表](#chunk块所在作用域包含的变量列表)
        - [示例一](#示例一)
        - [示例二](#示例二)
        - [示例三](#示例三)
        - [示例四](#示例四)
      - [☑ 传送点(dfc\_teleporter) C#组件(DfcTeleporter)](#-传送点dfc_teleporter-c组件dfcteleporter)
      - [☑ 数据获取器(dfc\_datagetter) C#组件(DfcDataGetter)](#-数据获取器dfc_datagetter-c组件dfcdatagetter)
      - [☑ 数据设置器(dfc\_datasetter) C#组件(DfcDataSetter)](#-数据设置器dfc_datasetter-c组件dfcdatasetter)
      - [☑ 包含区域(dfc\_regionincluded)](#-包含区域dfc_regionincluded)
      - [☑ 不包含区域(dfc\_regionexcluded)](#-不包含区域dfc_regionexcluded)
      - [☑ 角色检测器(dfc\_characterchecker) C#组件(DfcCharacterChecker)](#-角色检测器dfc_characterchecker-c组件dfccharacterchecker)
      - [☑ 角色检查器(单个)(dfc\_charactersinglechecker) C#组件(DfcCharacterSingleChecker)](#-角色检查器单个dfc_charactersinglechecker-c组件dfccharactersinglechecker)
      - [☑ 动作† 结束游戏(dfc\_actionendgame) C#组件(DfcActionEndGame)](#-动作-结束游戏dfc_actionendgame-c组件dfcactionendgame)
      - [☑ 动作† 发送聊天消息(dfc\_actionsendchatmessage) C#组件(DfcActionSendChatMessage)](#-动作-发送聊天消息dfc_actionsendchatmessage-c组件dfcactionsendchatmessage)
      - [☑ 事件☇ 进出区域(dfc\_evententerleaveregion) C#组件(DfcEventEnterLeaveRegion)](#-事件-进出区域dfc_evententerleaveregion-c组件dfcevententerleaveregion)
      - [☑ 事件☇ 死亡(dfc\_eventcharacterdeath) C#组件(DfcEventCharacterDeath)](#-事件-死亡dfc_eventcharacterdeath-c组件dfceventcharacterdeath)
      - [☑ 角色清除器(dfc\_charactercleaner) C#组件(DfcCharacterCleaner)](#-角色清除器dfc_charactercleaner-c组件dfccharactercleaner)
      - [☑ 物品清除器(dfc\_itemcleaner) C#组件(DfcItemCleaner)](#-物品清除器dfc_itemcleaner-c组件dfcitemcleaner)
      - [☑ 物品生成器(dfc\_itembuilder) C#组件(DfcItemBuilder)](#-物品生成器dfc_itembuilder-c组件dfcitembuilder)
      - [☑ 物品批处理器(dfc\_itembatch) C#组件(DfcItemBatch)](#-物品批处理器dfc_itembatch-c组件dfcitembatch)
      - [☑ 角色响应器(dfc\_characterresponder) C#组件(DfcCharacterResponder)](#-角色响应器dfc_characterresponder-c组件dfccharacterresponder)
      - [☑ 可穿戴物染色脚本(dfc\_scriptwearabledyeing) C#组件(DfcScriptWearableDyeing)](#-可穿戴物染色脚本dfc_scriptwearabledyeing-c组件dfcscriptwearabledyeing)
      - [☑ WIFI初始化脚本(dfc\_scriptwifiinitializer) C#组件(DfcScriptWifiInitializer)](#-wifi初始化脚本dfc_scriptwifiinitializer-c组件dfcscriptwifiinitializer)
      - [☑ 潜艇位置锁定脚本(dfc\_scriptsubmarinelocker) C#组件(DfcScriptSubmarineLocker)](#-潜艇位置锁定脚本dfc_scriptsubmarinelocker-c组件dfcscriptsubmarinelocker)
    - [模组依赖](#模组依赖)

[初始化器]: #-初始化器dfc_initializer-c组件dfcinitializer
[包含区域]: #-包含区域dfc_regionincluded
[不包含区域]: #-不包含区域dfc_regionexcluded
[物品清除器]: #-物品清除器dfc_itemcleaner-c组件dfcitemcleaner
[物品生成器]: #-物品生成器dfc_itembuilder-c组件dfcitembuilder

---
### 简介
派系工艺模组在渊深本体多人联机的沙盒模式中，以派系、职业、装备三元素作为基础，为玩家提供了丰富多样的自定义工具，以满足不同的地图需求。

1. **派系**：玩家可以选择加入不同的派系，每个派系都有其独特的背景故事、角色设定和玩法风格。例如，你可以加入守序的木卫二联盟，对社会的资源、制造与军力实施铁腕统治。或者加入木星分离主义分子，破坏木卫二联盟的正常运作，削弱其权威。
2. **职业**：每个派系提供了多种职业供玩家选择，每种职业都有独特的技能和能力。
3. **装备**：每个职业拥有一套独一无二、固有的或公共的装备体系，包括武器、防具、工具、药物和其他物资等，玩家可以根据场合上的情况选择不同装备进行应对。

在派系工艺中，玩家可以自由地选择派系、职业和装备。然而，这些选项都存在三个共同的限制条件。只有在满足以下所有条件的情况下，玩家才能进行相应的选择。

1. **剩余次数** ＞ 0
2. **当前数量** ＜ 数量上限
3. **比例(率)** ＜ 比重

这三个条件的**主谓项**（剩余次数、数量上限、当前数量、比例、比重）是根据它们的直接或间接的父级元素单独计算，派系限制条件的**主谓项**是根据所有的派系进行计算，而职业和装备的限制条件的**主谓项**是根据其所属的派系进行计算，<u>这么做的目的是为了让同一个职业、装备可以分别从属于不同的派系、职业，并且相互之间不产生冲突</u>。

---
### 重生系统
如果使用dfc自定义了游戏模式，那么重生系统会被改写。只有在开启了 `允许复活` 选项后才生效，在重生倒计时结束后，玩家无法改变派系，但可以在之后的任意时间内再次重新选择职业和装备，这意味着你可以等待大部队一起重生。

---
### 初始化

#### 内部编辑器工具实现(可选,推荐)
如果没有通过外部Lua脚本实例化 `dfc` 并对其初始化，那么会尝试解包地图.sub文件并查找是否存在 **[初始化器][初始化器]**，如果存在，则使用第一个被找到的初始化器执行初始化操作。在 `PVPMap` 模组中有完整的实例，请查看 `派系工艺 COC模组测试 ZRG改`。

---
### 工具
派系工艺为地图开发者们提供了一套丰富的自定义套件，它们可以用来方便地实现非常多原版完全无法做到的功能，<u>在潜艇编辑器内测试允许您实时修改这些工具的属性以进行调试，不过在多人联机下无法手动调整可编辑属性</u>，接下来会逐一介绍每种工具以及它们各自的用途与示例，你可以根据自身的需要查找对应的工具。

#### ☑ 初始化器(dfc_initializer) C#组件(DfcInitializer)
如果没有使用外部Lua脚本初始化地图，那么将利用该初始化器执行初始化操作。该组件提供了部分属性来改写游戏模式的基本设置：
1. **Allow Mid Round Join**: 允许玩家中途加入游戏
2. **Allow Respawn**: 允许复活

#### ☑ 新增出生区(dfc_newspawnpointset) C#组件(DfcNewSpawnPointSet)

#### ☑ 新增派系(dfc_newfaction) C#组件(DfcNewFaction)

#### ☑ 新增职业(dfc_newjob) C#组件(DfcNewJob)

#### ☑ 新增装备(dfc_newgear) C#组件(DfcNewGear)

#### ☑ 允许派系重生(dfc_allowrespawn) C#组件(DfcAllowRespawn)
`signal_in` 接受到 `"0"` 时禁止指定派系重生，否则视为允许。

#### ☑ 添加或移除职业(dfc_addorremovejob) C#组件(DfcAddOrRemoveJob)
`signal_in` 接受到 `"0"` 时移除指定派系中的指定职业，否则视为添加。
使用 `,` 分隔符可以指定多个派系和职业。

#### ☑ 添加或移除装备(dfc_addorremovegear) C#组件(DfcAddOrRemoveGear)
`signal_in` 接受到 `"0"` 时移除指定职业中的指定装备，否则视为添加。
使用 `,` 分隔符可以指定多个职业和装备。

#### ☑ Lua组件(dfc_luacomponent) C#组件(DfcLuaComponent)
Ctrl+CV自[RGzXTrauma](https://steamcommunity.com/sharedfiles/filedetails/?id=2937571024)模组，并且优化和解决了编译错误。

#### ☑ Lua组件2代(dfc_lua2component) C#组件(DfcLua2Component)
方案源自工坊模组MicroLua: https://steamcommunity.com/sharedfiles/filedetails/?id=3018125421
拥有32个并排IO，通过闭包上下文访问upvalues，减轻了性能负担，并撤除了环境约束。
##### Chunk块所在作用域包含的变量列表
- **out**: 包装了lua组件的userdata，在代码中执行setindex操作，用于控制信号输出
- **luaItem**: lua组件本身，类型为Barotrauma.Item
- **clear**: 清空table的函数，原型fun(t: table)
- **sync**: 仅服务器可用，客户端忽略，用于向客户端发送并同步输出信号，该函数接受一个table作为参数，表域内，键为需要执行同步输出的引脚序号，值为信号值
- **loaded**: 函数，在地图加载完成后执行
- **upd**: 更新函数，会传递更新周期作为参数
- **inp**: 由用户在chunk中定义，用于访问输入的信号值，类型为table\<integer, string|number\>或fun(i: integer, v:string|number)
- **sender**: 使用out输出信号时设置的发送者
- **senders**: 用于访问相应引脚接收到的信号的发送者

##### 示例一
```lua
-- 定义inp（可为`nil`），勿使用local声明，因为符号`inp`的变量已被预先声明为局部变量，其类型为`table<integer, string|number>`；
-- 在表域内，键为引脚序号（输入），值为对应引脚序号所接受到的信号值。
inp = {}

-- 定义senders（可为`nil`），同上，勿使用local声明；
senders = {}

local 逝去时间 = 0.0

-- 定义upd（可为`nil`），同上，勿使用local声明；
-- 在该组件更新时调用，参数`deltaTime`为更新周期
function upd(deltaTime)
    逝去时间 = 逝去时间 + deltaTime

    -- 如果signal_in1引脚接受到信号
    if inp[1] ~= nil then
        -- 设置信号发送者为signal_in1引脚接受到的信号的发送者
        sender = senders[1]
        -- 从signal_out1引脚输出逝去时间，会立即输出该信号，而不是像1代的lua组件一样等到更新结束之后再输出
        out[1] = 逝去时间
        -- 服务器发起同步事件，在客户端接受到事件数据后，会从signal_out1输出逝去时间
        sync { [1] = 逝去时间 }
        -- inp并不会被自动清除，需要手动清除inp[1]，sender和senders也一样
        inp[1] = nil
        sender = nil
        senders[1] = nil
    end

    -- 如果signal_in2引脚接受到信号
    if inp[2] then
        -- 函数清空inp表和senders表
        clear(inp)
        clear(senders)
        -- 时间复位
        逝去时间 = 0.0
    end
end
```
##### 示例二
```lua
local 逝去时间 = 0.0

-- inp也可以被定义成原型为`fun(i, v)`的函数；
-- 在接受到信号时回调，参数`i`为引脚序号（输入），`v`为对应引脚序号所接受到的信号值。
function inp(i, v)
    if i == 1 then
        out[1] = 逝去时间
    end
    if i == 2 then
        逝去时间 = 0.0
    end
end

function upd(deltaTime)
    逝去时间 = 逝去时间 + deltaTime
end
```
##### 示例三
```lua
-- 你也可以不用定义inp和upd，直接运行一段代码
print(("物品总数：%i"):format(#Item.ItemList))
print(("Lua组件位置：%s"):format(tostring(luaItem.WorldPosition)))
print("初始化完成！")
```
##### 示例四
```lua
-- 定义loaded函数（可为`nil`），在地图加载完成后执行
function loaded()
    local 随机密码 = math.random()
    out[1] = 随机密码
    sync { [1] = 随机密码 }
end
```

#### ☑ 传送点(dfc_teleporter) C#组件(DfcTeleporter)
与[ZDoor](https://steamcommunity.com/sharedfiles/filedetails/?id=2902757031)模组以及PVP服使用的传送门类似，交互时可以传送角色，须要接线才能使其运转，有所不同的是它没有实际的贴图，但可以调整其交互范围以及传送开关，通常可以摆放其他的装饰物作为该传送点的外观。
当由某个角色产生的信号发送到了传送点的**信号输入**时，该角色将被传送至该传送点的位置。
该组件拥有以下属性用于控制传送逻辑：
1. **Disable Teleport Dragged Character**: 禁止传送被拖拽的角色
2. **TeleportAble**: 当值为假时，角色无法被传送至该点，可由信号设置
3. **Cooldown**: 传送后需要等待的全局冷却时间，而非为每个角色单独计算冷却

#### ☑ 数据获取器(dfc_datagetter) C#组件(DfcDataGetter)
它接受一个信号，并根据当前组件的索引 `DataIndex` 获取一个全局数据，如果获取的值为空，则会自动为其分配一个默认值 `DefaultData`，接线面板的各引脚说明：
1. **get_data**: 获取数据
2. **set_index**: 设置索引
3. **signal_out**: 输出数据值

#### ☑ 数据设置器(dfc_datasetter) C#组件(DfcDataSetter)
它接受一个信号，并将其信号值存储至当前组件的索引 `DataIndex` 中，接线面板的各引脚说明：
1. **set_data**: 设置数据值
2. **set_index**: 设置索引

#### ☑ 包含区域(dfc_regionincluded)

#### ☑ 不包含区域(dfc_regionexcluded)

#### ☑ 角色检测器(dfc_characterchecker) C#组件(DfcCharacterChecker)
通过链接(link) **[包含区域][包含区域]** 或 **[不包含区域][不包含区域]** 组成区域，在接收到信号后，会输出该区域内匹配条件的角色总数。
该组件拥有以下属性用于修改角色的匹配条件：
1. **Minimum Velocity**: 最小速度
2. **Character Target Type**: 角色类型
3. **Character Species Names**: 角色种名
4. **Character Group**: 角色组别
5. **Character Tags**: 角色标记

#### ☑ 角色检查器(单个)(dfc_charactersinglechecker) C#组件(DfcCharacterSingleChecker)
接受一个由角色发出的信号，如果该角色匹配相应条件，将以该角色作为源将该**信号值**、**角色GUID**、**角色名称**、**角色种名**、**角色组别**、**角色标记**分别发送，通过以下属性修改角色的匹配条件：
1. **Character Team Type**: 角色类型
2. **Character Species Names**: 角色种名
3. **Character Group**: 角色组别
4. **Character Tags**: 角色标记

#### ☑ 动作† 结束游戏(dfc_actionendgame) C#组件(DfcActionEndGame)
在接收到信号以后结束本次巡回。

#### ☑ 动作† 发送聊天消息(dfc_actionsendchatmessage) C#组件(DfcActionSendChatMessage)
在接收到信号后向玩家发送消息，通过设置属性来过滤出可收到消息的玩家：
1. **Sender Receiver Relation**: 发送者(信号来源)必须存在且与接收者的必须关系，`无`则忽略
   - 无
   - 不同队伍
   - 相同队伍
2. **Sender Target Type**: 发送者类型
3. **Sender Species Names**: 发送者种名
4. **Sender Group**: 发送者组别
5. **Sender Tags**: 发送者标记
6. **Receiver Target Type**: 接受者类型
7. **Receiver Species Names**: 接受者种名
8. **Receiver Group**: 接受者组别
9. **Receiver Tags**: 接受者标记
10. **Spectator Receivable**: 发送给观众席
11. **Spectator Only Receivable**: 发送给仅观看的玩家

#### ☑ 事件☇ 进出区域(dfc_evententerleaveregion) C#组件(DfcEventEnterLeaveRegion)
通过链接(link) **[包含区域][包含区域]** 或 **[不包含区域][不包含区域]** 组成区域，当有角色进入或离开该区域后，会以该角色作为源并发送五个信号，分别为**角色GUID(Globally Unique Identifier: 全局唯一标识符, 用于唯一标识某个实体的256位字符串)**、**角色名称**、**角色种名**、**角色标记**、**“0”或“1”(“0”表示离开，“1”表示进入)**。
该组件拥有以下属性用于控制事件触发的条件：
1. **Minimum Velocity**: 最小速度
2. **Character Target Type**: 角色类型
3. **Character Species Names**: 角色种名
4. **Character Group**: 角色组别
5. **Character Tags**: 角色标记
6. **Match After Leave**: 在角色离开时须匹配2-5属性，否则无法触发事件

#### ☑ 事件☇ 死亡(dfc_eventcharacterdeath) C#组件(DfcEventCharacterDeath)
当有角色死亡时，会以该角色作为源并发送十个信号，分别为**死者GUID**、**死者名称**、**死者种名**、**死者组别**、**死者标记**、**凶手GUID**、**凶手名称**、**凶手种名**、**凶手组别**、**凶手标记**，1-5的信号源为死者，6-10的信号源为凶手。
该组件拥有以下属性用于控制事件触发的条件：
1. **Death Target Type**: 死者类型
2. **Death Species Names**: 死者种名
3. **Death Group**: 死者组别
4. **Death Tags**: 死者标记
5. **Killer Target Type**: 凶手类型
6. **Killer Species Names**: 凶手种名
7. **Killer Group**: 凶手组别
8. **Killer Tags**: 凶手标记

#### ☑ 角色清除器(dfc_charactercleaner) C#组件(DfcCharacterCleaner)
在引脚**signal_in**接收到信号后，会对其搜索到的所有待清除角色记录一次，如果目标的记录次数达到了给定次数，则将其清除，适当地合理使用可以缓解游戏不必要的卡顿，可以链接(link) **[包含区域][包含区域]** 和 **[不包含区域][不包含区域]** 以用于约束角色的搜索范围。。
- **属性**
  1. **Tolerance Threshold 清除阈值**
  角色记录次数达到该阈值时将被清除
  2. **Character Target Type**: 角色类型
  3. **Character Species Names**: 角色种名
  4. **Character Group**: 角色组别
  5. **Character Tags**: 角色标记

#### ☑ 物品清除器(dfc_itemcleaner) C#组件(DfcItemCleaner)
- **用途**
在引脚**signal_in**接收到信号后，会对其搜索到的所有待清除物品记录一次，如果物品的记录次数达到了给定次数，则将其清除，适当地合理使用可以缓解游戏不必要的卡顿。
- **属性**
  - **Tolerance Threshold 清除阈值**
  物品记录次数达到该阈值时将被清除
  - **Clean Rule 清除规则**
  构建一个清除规则的表达式需要定义至少一个标签组，以逗号`,`作为分隔符可以添加多个标签组，在标签组内也可以使用竖线 `|` 定义一个组内标签，当物品的**标签Tags**或**标识符identifier**匹配了其中一个标签组的所有组内标签时，则会被记录一次。
  清除规则分为**弱清除**与**强清除**，并继而分为**包含**与**不包含**，弱清除是指只有当物品不在容器内才会被记录，而强清除无视该条件。包含是指待清除物品须要匹配对应规则，而不包含要求不匹配。
    - **Weak Clean Pattern Includes 弱清除包含**
    - **Weak Clean Pattern Excludes 弱清除不包含**
    - **Strong Clean Pattern Includes 强清除包含**
    - **Strong Clean Pattern Excludes 强清除不包含**
  - **Only Indoor 仅清除其所在潜艇上的物品**
  - **Ignore Static Body 忽略静态物品(导航终端,引擎,接线盒……)**
  - **Ignore Attached 忽略安装在了墙壁上的物品**
  - **Ignore Initial 忽略在巡回开始时加载的物品**
- **链接**
  可以链接(link) **[包含区域][包含区域]** 和 **[不包含区域][不包含区域]** 以用于约束物品的搜索范围。
- **示例**
 下方的代码为该组件在保存之后于XML文本中的表示
  ```xml
  <!-- 弱清除带有 medical 或 weapon 标签的物品 -->
  <DfcItemCleaner
    WeakCleanIncludes="medical,weapon"
  />
  <!-- 弱清除带有 medical 或 weapon 标签的物品，但不包含吗啡或步枪 -->
  <DfcItemCleaner
    WeakCleanIncludes="medical,weapon"
    WeakCleanExcludes="antidama1,rifle"
  />
  <!-- 弱清除带有 smallitem＋weapon 标签的物品 -->
  <DfcItemCleaner
    WeakCleanIncludes="smallitem|weapon"
  />
  <!-- 弱清除带有 smallitem＋weapon 标签的物品，但不包含机关手枪 -->
  <DfcItemCleaner
    WeakCleanIncludes="smallitem|weapon"
    WeakCleanExcludes="machinepistol"
  />
  <!-- 弱清除带有 smallitem＋weapon 或 mediumitem＋weapon 标签的物品 -->
  <!-- 但不包含机关手枪或突击步枪 -->
  <DfcItemCleaner
    WeakCleanIncludes="smallitem|weapon,mediumitem|weapon"
    WeakCleanExcludes="machinepistol,assaultrifle"
  />
  ```

#### ☑ 物品生成器(dfc_itembuilder) C#组件(DfcItemBuilder)
属性**Item Builds 物品构建集**存储了一个lua脚本的`{}`，是**Item Builder Block 物品生成块**的数组，用于描述物品生成信息，该组件在接受到信号之后，会查找该信号的发送者，如若找到发送信号的角色，则将物品生成在角色身上，否则生成在该物品生成器所在的位置，通过链接(link) **[包含区域][包含区域]** 可以改变物品生成的位置。

该组件拥有以下属性用于控制生成逻辑：
1. **Item Builds**: 存储一个Lua脚本`{}`类型的值，用以描述物品生成信息
2. **Can Spawn**: 是否可以生成
3. **Amount**: 限定生成的次数
4. **Force Spawn At Positions**: 强制生成在即将被确定的某个位置，而不是角色身上
5. **Required Sender**: 要求发送者存在
6. **Sender Target Type**: 发送者类型
7. **Sender Species Names**: 发送者种名
8. **Sender Group**: 发送者组别
9. **Sender Tags**: 发送者标记
10. **Group**: 该值相同的物品生成器会被分到同一组，如果角色触发过**Group**组内的某个物品生成器，则无法生成，如果该属性值为空，则无限制。
11. **Identifier**: 标识符，暂时没有任何用途

**Item Builds**的定义示例如下：
```lua
return {
    "这只是一个用于调试的名称",
    {
        -- 生成一个自注射耳机(identifier: autoinjectorheadset)
        identifier = "autoinjectorheadset",
        -- 执行装备操作(equip: true)
        equip = true,
        -- 继承角色的耳机wifi通道(inheritchannel: true)
        inheritchannel = true,
        -- 在自注射耳机内生成c4炸弹
        -- 但物品标签须修改为"chem,medical"
        inventory = {
            { identifier = "c4block", tags = "chem,medical" },
        }
    },
    {
        -- 生成战地医疗服(identifier: doctorsuniform1)
        identifier = "doctorsuniform1",
        -- 执行装备操作(equip: true)
        equip = true,
        -- 在战地医疗服内生成3种药物
        inventory = {
            -- 生成1组绷带(8个)
            { identifier = "antibleeding1",  stacks = 1 },
            -- 生成2袋生理盐水
            { identifier = "antibloodloss2", amount = 2 },
            -- 在剩余的容器空间中填满吗啡
            { identifier = "antidama1",      fillinventory = true }
        }
    },
    {
        -- 生成一个声纳信标
        identifier = "sonarbeacon",
        -- 放置在17号槽位，对应人类物品栏内的最后一个槽位
        slotindex = 17,
        -- 设置声纳信标属性
        properties = {
            -- 设置声纳信标为不可交互的
            noninteractable = true,
            -- 设置CustomInterface组件的ElementStates属性，将可编辑的第一个条目(开启信标)设置为真
            [{ "custominterface", "elementstates" }] = "true,",
            -- 设置CustomInterface组件的Signals属性，将可编辑的第二个条目(声纳信号)设置为“我在这里~”
            [{ "custominterface", "signals" }] = ";我在这里~",
        },
        -- 因为CustomInterface的属性无法像其他组件的属性一样直接同步给客户端
        -- 所以须要创建一个CustomInterface的ServerEvent事件来实现同步
        -- 提示：并非所有组件的属性都可以通过创建ServerEvent事件来同步，具体问题需通过分析游戏源代码来解决
        serverevents = "custominterface",
        inventory = {
            {
                -- 生成一颗电池
                identifier = "batterycell",
                -- 电池改为不可破坏(无限能源)
                properties = { indestructible = true }
            }
        }
    },
    {
        identifier = "boardingaxe",
        -- 物品生成后的回调函数
        onspawned = function(item)
            -- 生成登船斧(identifier: boardingaxe)之后在控制台输出其名称
            print(item.Name)
        end
    },
    {
        identifier = "door",
        -- 生成一扇手动门, 并放置和固定在潜艇上(install: true)
        install = true
    },
    -- 随机调用3次(amount: 3)物品池并(pool)生成手榴弹
    {
        amount = 3,
        -- 25%几率(10/40)生成燃素、震撼、破片手榴弹其中一个
        -- 12.5%几率(5/40)生成EMP、酸液手榴弹其中一个
        pool = {
            -- 比重(weight)为10，表示在该物品池中的权重，影响其被抽中的几率
            { 10, { identifier = "fraggrenade" } },
            { 10, { identifier = "stungrenade" } },
            { 10, { identifier = "incendiumgrenade" } },
            { 5,  { identifier = "empgrenade" } },
            { 5,  { identifier = "chemgrenade" } },
        }
    },
    -- 抽卡, 50%概率: 一个存放了雷管(大师级c4炸弹或脏弹)的工具箱
    {
        pool = {
            {
                50,
                {
                    {
                        identifier = "toolbox",
                        inventory = {
                            { identifier = "screwdriver" },
                            { identifier = "wrench" },
                            { identifier = "bluewire" },
                            {
                                identifier = "detonator",
                                inventory = {
                                    pool = {
                                        { 40, { identifier = "c4block", quality = 3 } },
                                        { 20, { identifier = "dirtybomb" } },
                                    }
                                }
                            },
                            { identifier = "button" },
                        }
                    }
                }
            },
            -- 50%概率什么也没有
            {
                50,
                {}
            }
        }
    },
}
```

#### ☑ 物品批处理器(dfc_itembatch) C#组件(DfcItemBatch)
属性**Item Batch Unit 批处理单元**存储了一个lua脚本的`{}`，是**Item Batch Block 批处理块**的数组，用于描述物品批处理过程，该组件在接受到信号之后，会对其链接(link)的所有物品进行批处理，除此之外还会查找该信号的发送者，如若找到发送信号的角色，则也会对角色身上的物品进行批处理。

该组件拥有以下属性用于控制批处理逻辑：
1. **Item Batch Unit**: 存储一个Lua脚本`{}`类型的值，用以描述物品批处理过程
2. **Can Batch**: 是否可以进行批处理
3. **Only Batch Linked To**: 仅对链接的物品进行批处理
4. **Amount**: 限定批处理的次数
5. **Required Sender**: 要求发送者存在
6. **Sender Target Type**: 发送者类型
7. **Sender Species Names**: 发送者种名
8. **Sender Group**: 发送者组别
9. **Sender Tags**: 发送者标记
10. **Group**: 该值相同的物品批处理器会被分到同一组，如果角色触发过**Group**组内的某个物品批处理器，则无法生成，如果该属性值为空，则无限制。
11. **Identifier**: 标识符，暂时没有任何用途

**Item Batch Unit**的定义示例如下：
```lua
return {
    "这只是一个用于调试的名称",
    -- 丢弃掉耳机容器内的非注射型药物
    {
        -- 添加匹配条件: 要求物品须要在耳机槽位上，支持用数组表示
        -- 例如 slottype = { InvSlotType.Headset, InvSlotType.Any }，表示匹配耳机槽位和非装备型槽位(Any)
        slottype = InvSlotType.Headset,
        -- 匹配成功后，对匹配的物品其容器内的子物品进行批处理
        inventory = {
            -- 添加匹配条件: 没有syringe标签
            excludedtags = "syringe",
            -- 匹配成功后，丢弃掉匹配的物品
            drop = true
        }
    },
    {
        -- 添加匹配条件: 是战地医疗服(doctorsuniform1)，支持用数组表示
        -- 例如 identifiers = { "doctorsuniform1", "doctorsuniform2" }, 表示战地医疗服或医生制服
        identifiers = "doctorsuniform1",
        -- 添加匹配条件: 要求物品是被装备或穿戴的
        equipped = true,
        inventory = {
            -- 丢弃掉战地医疗服里的绷带
            { identifiers = "antibleeding1", drop = true },
            -- 移除掉生理盐水
            { identifiers = "antibloodloss2", remove = true },
            -- 匹配具有medical标签的物品，并将其图标颜色改为"0,0,0"(黑色)
            { tags = "medical", properties = { inventoryiconcolor = "0,0,0" } }
        }
    },
    {
        -- 添加匹配条件: 是声纳信标(sonarbeacon)
        identifiers = "sonarbeacon",
        -- 添加匹配条件: 在17号槽位上，对应人类物品栏内的最后一个槽位，支持用数组表示
        -- 例如 slotindex = { 16, 17 }, 表示人类物品栏内的倒数两个槽位
        slotindex = 17,
        properties = {
            -- 改为可交互的
            noninteractable = false,
        },
        inventory = {
            -- 将容器内的物品(电池)改为可破坏的
            properties = { indestructible = false }
        }
    },
    -- 把电棍的大小放大5倍
    {
        identifiers = "stungrenade",
        properties = { scale = 5 }
    },
    -- 丢弃掉工具箱->雷管内的物品
    {
        identifiers = "toolbox",
        inventory = {
            identifiers = "detonator",
            inventory = {
                drop = true
            }
        }
    },
    -- 移除品质为2的左轮手枪, quality对物品的品质进行匹配，也支持数组
    {
        identifier = "revolver",
        quality = 2,
        inventory = { remove = true }
    },
    -- 移除所有物品
    -- { remove = true },
    -- 移除物品栏内非装备槽位的所有物品
    -- { slottype = InvSlotType.Any, remove = true },
}
```

#### ☑ 角色响应器(dfc_characterresponder) C#组件(DfcCharacterResponder)
属性**Chunk 应答回调**存储了一个lua脚本的编程块(chunk/block)，该组件在接受到信号之后，如若查找到发送该信号的角色，则会对该角色执行脚本函数，使用示例如下：
```lua
return function(character)
    -- 给予角色狂战士天赋
    character.GiveTalent("berserker")
    -- 武器技能提高40
    character.Info.IncreaseSkillLevel("weapons", 40)
    -- 医疗技能设置为100
    character.Info.SetSkillLevel("medical", 100)

    -- 在角色的主肢体上施加1点画皮共生(husksymbiosis)
    character.CharacterHealth.ApplyAffliction(
        character.AnimController.MainLimb,
        AfflictionPrefab.Prefabs[Identifier("husksymbiosis")].Instantiate(1))
end
```
支持通过链接(link) **[包含区域][包含区域]** 或 **[不包含区域][不包含区域]** 组成区域，并将脚本函数应用于该区域内所有通过匹配的角色。
通过以下属性修改角色的响应条件：
1. **Can Response**: 是否可以响应
2. **Apply To Sender**: 是否应用于发送者
3. **Character Team Type**: 角色类型
4. **Character Species Names**: 角色种名
5. **Character Group**: 发送者组别
6. **Character Tags**: 发送者标记
7. **Group**: 该值相同的角色响应器会被分到同一组，如果角色被**Group**组内的某个角色响应器应用过脚本函数，则无法再次响应，如果该属性值为空，则无限制。
8. **Identifier**: 标识符，暂时没有任何用途


#### ☑ 可穿戴物染色脚本(dfc_scriptwearabledyeing) C#组件(DfcScriptWearableDyeing)
当角色穿戴物品时，可以改变该物品的颜色。
该组件拥有以下属性用于修改染色条件：
1. **Sprite Color**: 贴图颜色
2. **Inventory Icon Color**: 图标颜色
3. **Wearer Target Type**: 穿戴者类型
4. **Wearer Species Names**: 穿戴者种名
5. **Wearer Group**: 穿戴者组别
6. **Wearer Tags**: 穿戴者标记
7. **Matcher Pattern  Includes** 要求物品拥有指定标识符或标签，其表达式规则与 [物品清除器][物品清除器] 的相同
8. **Matcher Pattern Excludes** 要求物品没有指定标识符或标签
9. **InvSlotType** 允许的槽位类型

#### ☑ WIFI初始化脚本(dfc_scriptwifiinitializer) C#组件(DfcScriptWifiInitializer)
当任何带有wifi组件的物品被创建后，可以改变其wifi频道和所属队伍，通过链接(link) **[包含区域][包含区域]** 或 **[不包含区域][不包含区域]** 用于匹配物品生成的位置，也支持通过与 [物品清除器][物品清除器] 相同的表达式规则限制物品的标识符和标签。
该脚本组件部分属性说明：
1. **Wifi Channel Range** 一个数值范围，用于将wifi组件的通道设置为其范围内的一个随机数
2. **Always Calculate Random Channel** 是否始终计算随机通道？值为 `真` 时wifi组件的通道始终由 `Wifi Channel Range` 随机确定，否则只会被设置为第一次计算出来的随机数。
3. **TeamID** 修改wifi的所属队伍

#### ☑ 潜艇位置锁定脚本(dfc_scriptsubmarinelocker) C#组件(DfcScriptSubmarineLocker)

---
### 模组依赖
- **Lua For Barotrauma**
- **Cs For Barotrauma**
- **Moses**
- **Lua Utility Belt**
- **DSSI(Dynamic Submarine Script Injector)**
