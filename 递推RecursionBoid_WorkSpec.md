# 递推 Boid 群体行为系统 — 详细工作规范 (WorkSpec)

## 一、概述

本文档定义了基于"递推"架构的鸟群群体行为模拟系统的详细工作规范。系统采用分层设计，将核心逻辑与Unity表现层分离，通过适配器接口进行桥接。

### 1.1 设计原则
- **接口驱动**：所有核心组件通过接口定义，便于测试和替换
- **分层架构**：核心逻辑层与Unity表现层分离
- **单一职责**：每个类/接口负责单一功能
- **可扩展性**：支持从2D扩展到3D

### 1.2 性能目标
- 支持鸟群数量：≤100只
- 更新频率：10Hz (0.1秒/tick)
- 层级树深度差：ceil(sqrt(N))

---

## 二、核心接口定义

### 2.1 基础鸟接口 `IBoid`

```csharp
public interface IBoid
{
    // 基础属性
    int Id { get; }
    Vector2 Position { get; }
    Vector2 Velocity { get; }
    float Speed { get; }
    
    // 状态
    bool IsActive { get; }
    
    // 方法
    void Initialize(int id, Vector2 startPosition, Vector2 initialVelocity);
    void UpdateVelocity(Vector2 newVelocity);
    void UpdatePosition(float deltaTime);
    void SetActive(bool active);
}
```

### 2.2 老大鸟接口 `ILeaderBoid`

```csharp
public interface ILeaderBoid : IBoid
{
    // 环境感知
    void AddAttractor(Vector2 position, float strength);
    void RemoveAttractor(Vector2 position);
    void AddRepulsor(Vector2 position, float strength);
    void RemoveRepulsor(Vector2 position);
    
    // 边界感知
    void SetBoundary(Rect boundary);
    
    // 向量计算
    Vector2 CalculateNewVelocity();
    
    // 事件
    event System.Action<Vector2> OnVelocityUpdated;
}
```

### 2.3 小鸟接口 `IFollowerBoid`

```csharp
public interface IFollowerBoid : IBoid
{
    // 层级关系
    int HierarchyDepth { get; }
    IBoid Parent { get; }
    IReadOnlyList<IBoid> Children { get; }
    
    // 递推向量
    void ReceiveParentVector(Vector2 parentVector);
    Vector2 CalculateNewVelocity(Vector2 separationForce);
    
    // 分离规则
    Vector2 CalculateSeparationForce(IReadOnlyList<IBoid> nearbyBoids);
}
```

### 2.4 层级树接口 `IHierarchyTree`

```csharp
public interface IHierarchyTree
{
    // 属性
    IBoid Root { get; }
    int TotalCount { get; }
    int MaxDepth { get; }
    int MaxChildrenPerNode { get; }
    
    // 节点管理
    bool AddNode(IBoid node, IBoid parent);
    bool RemoveNode(IBoid node);
    bool MoveNode(IBoid node, IBoid newParent);
    
    // 查询
    IBoid GetParent(IBoid node);
    IReadOnlyList<IBoid> GetChildren(IBoid node);
    int GetDepth(IBoid node);
    IReadOnlyList<IBoid> GetNodesAtDepth(int depth);
    
    // 验证
    bool ValidateTree();
    bool HasCycle();
    bool IsBalanced();
}
```

### 2.5 环境感知接口 `IEnvironmentSensor`

```csharp
public interface IEnvironmentSensor
{
    // 感知范围
    float PerceptionRadius { get; }
    
    // 环境元素
    void AddAttractor(Vector2 position, float strength);
    void RemoveAttractor(Vector2 position);
    void AddRepulsor(Vector2 position, float strength);
    void RemoveRepulsor(Vector2 position);
    
    // 边界
    void SetBoundary(Rect boundary);
    
    // 计算影响
    Vector2 CalculateInfluence(Vector2 position);
}
```

### 2.6 向量传播接口 `IVectorPropagator`

```csharp
public interface IVectorPropagator
{
    // 传播参数
    float TickInterval { get; }
    
    // 传播方法
    void Propagate(IHierarchyTree tree);
    void SetTickInterval(float interval);
    
    // 向量融合
    Vector2 BlendVectors(Vector2 parentVector, Vector2 currentVector);
}
```

---

## 三、数据结构定义

### 3.1 鸟数据结构 `BoidData`

```csharp
public struct BoidData
{
    public int Id;
    public Vector2 Position;
    public Vector2 Velocity;
    public float Speed;
    public bool IsActive;
    
    public BoidData(int id, Vector2 position, Vector2 velocity, float speed)
    {
        Id = id;
        Position = position;
        Velocity = velocity;
        Speed = speed;
        IsActive = true;
    }
}
```

### 3.2 层级节点结构 `HierarchyNode`

```csharp
public struct HierarchyNode
{
    public IBoid Boid;
    public int Depth;
    public IBoid Parent;
    public List<IBoid> Children;
    
    public HierarchyNode(IBoid boid, int depth, IBoid parent)
    {
        Boid = boid;
        Depth = depth;
        Parent = parent;
        Children = new List<IBoid>();
    }
}
```

### 3.3 环境元素结构 `EnvironmentElement`

```csharp
public struct EnvironmentElement
{
    public Vector2 Position;
    public float Strength;
    public ElementType Type;
    
    public enum ElementType
    {
        Attractor,
        Repulsor
    }
    
    public EnvironmentElement(Vector2 position, float strength, ElementType type)
    {
        Position = position;
        Strength = strength;
        Type = type;
    }
}
```

---

## 四、管理类接口

### 4.1 系统管理器接口 `IBoidSystemManager`

```csharp
public interface IBoidSystemManager
{
    // 系统状态
    bool IsRunning { get; }
    float TickInterval { get; }
    
    // 组件引用
    ILeaderBoid Leader { get; }
    IHierarchyTree HierarchyTree { get; }
    IEnvironmentSensor EnvironmentSensor { get; }
    IVectorPropagator VectorPropagator { get; }
    
    // 生命周期
    void Initialize(int initialBoidCount, float speed);
    void StartSimulation();
    void StopSimulation();
    void ResetSimulation();
    
    // 鸟群管理
    void AddBoid(IBoid boid);
    void RemoveBoid(IBoid boid);
    int GetBoidCount();
    
    // 更新
    void Tick();
}
```

### 4.2 层级树构建器接口 `IHierarchyBuilder`

```csharp
public interface IHierarchyBuilder
{
    // 构建参数
    int MaxChildrenPerNode { get; }
    int MaxDepthDifference { get; }
    
    // 构建方法
    IHierarchyTree BuildTree(ILeaderBoid leader, IReadOnlyList<IFollowerBoid> followers);
    bool AddToFollower(IFollowerBoid newBoid, IHierarchyTree tree);
    bool RemoveFromTree(IBoid boid, IHierarchyTree tree);
    
    // 验证
    bool ValidateConstraints(IHierarchyTree tree);
}
```

### 4.3 环境管理器接口 `IEnvironmentManager`

```csharp
public interface IEnvironmentManager
{
    // 环境元素管理
    void AddAttractor(Vector2 position, float strength);
    void RemoveAttractor(Vector2 position);
    void AddRepulsor(Vector2 position, float strength);
    void RemoveRepulsor(Vector2 position);
    
    // 边界管理
    void SetBoundary(Rect boundary);
    Rect GetBoundary();
    
    // 查询
    IReadOnlyList<EnvironmentElement> GetAttractors();
    IReadOnlyList<EnvironmentElement> GetRepulsors();
}
```

---

## 五、Unity 适配层接口

### 5.1 MonoBehaviour 适配器接口

```csharp
public interface IBoidView
{
    // 视觉表现
    void UpdateVisual(Vector2 position, float rotation);
    void SetActive(bool active);
    void SetColor(Color color);
    
    // 事件回调
    System.Action OnClicked { get; set; }
}

public interface ILeaderBoidView : IBoidView
{
    // 老大鸟特有视觉
    void ShowInfluenceRadius(float radius);
    void ShowAttractorConnections(IReadOnlyList<Vector2> attractors);
}

public interface IFollowerBoidView : IBoidView
{
    // 小鸟特有视觉
    void ShowHierarchyLine(Vector2 parentPosition);
    void SetDepthIndicator(int depth);
}
```

### 5.2 ScriptableObject 配置接口

```csharp
public interface IBoidSystemConfig
{
    // 基础参数
    float TickInterval { get; }
    float BoidSpeed { get; }
    int InitialBoidCount { get; }
    
    // 层级参数
    int MaxChildrenPerNode { get; }
    int MaxDepthDifference { get; }
    
    // 分离参数
    float SeparationDistance { get; }
    float SeparationStrength { get; }
    
    // 边界参数
    Rect Boundary { get; }
}
```

### 5.3 Editor 工具接口

```csharp
public interface IBoidEditorTools
{
    // 场景设置
    void SetupScene(IBoidSystemConfig config);
    void ClearScene();
    
    // 调试工具
    void DrawHierarchyGizmos(IHierarchyTree tree);
    void DrawEnvironmentGizmos(IEnvironmentManager environment);
    void DrawVelocityVectors(IReadOnlyList<IBoid> boids);
    
    // 运行时控制
    void PauseSimulation();
    void ResumeSimulation();
    void StepSimulation();
}
```

---

## 六、关键算法接口

### 6.1 向量融合算法

```csharp
public interface IVectorBlending
{
    // 融合方法
    Vector2 Blend(Vector2 parentVector, Vector2 currentVector, float weight);
    
    // 权重计算
    float CalculateWeight(int depth, int totalDepth);
}
```

### 6.2 分离力计算

```csharp
public interface ISeparationCalculator
{
    // 分离力计算
    Vector2 CalculateSeparationForce(IBoid current, IReadOnlyList<IBoid> neighbors, float separationDistance);
    
    // 参数
    float SeparationDistance { get; }
    float SeparationStrength { get; }
}
```

### 6.3 边界处理

```csharp
public interface IBoundaryHandler
{
    // 边界检查
    bool IsInsideBoundary(Vector2 position);
    Vector2 ClampToBoundary(Vector2 position);
    
    // 边界力计算
    Vector2 CalculateBoundaryForce(Vector2 position, float influenceDistance);
}
```

---

## 七、事件系统接口

### 7.1 系统事件

```csharp
public interface IBoidSystemEvents
{
    // 系统事件
    event System.Action OnSimulationStarted;
    event System.Action OnSimulationStopped;
    event System.Action OnSimulationReset;
    
    // 鸟群事件
    event System.Action<IBoid> OnBoidAdded;
    event System.Action<IBoid> OnBoidRemoved;
    event System.Action<IBoid> OnBoidActivated;
    event System.Action<IBoid> OnBoidDeactivated;
    
    // 层级事件
    event System.Action<IBoid, IBoid> OnParentChanged;
    event System.Action<int> OnHierarchyRebalanced;
    
    // 环境事件
    event System.Action<Vector2, float> OnAttractorAdded;
    event System.Action<Vector2> OnAttractorRemoved;
    event System.Action<Vector2, float> OnRepulsorAdded;
    event System.Action<Vector2> OnRepulsorRemoved;
}
```

---

## 八、工厂接口

### 8.1 鸟工厂

```csharp
public interface IBoidFactory
{
    // 创建方法
    ILeaderBoid CreateLeader(Vector2 position, Vector2 initialVelocity);
    IFollowerBoid CreateFollower(Vector2 position, Vector2 initialVelocity);
    
    // 配置
    void Configure(IBoidSystemConfig config);
}
```

### 8.2 层级树工厂

```csharp
public interface IHierarchyTreeFactory
{
    IHierarchyTree CreateTree(ILeaderBoid leader);
    IHierarchyBuilder CreateBuilder();
}
```

---

## 九、使用示例

### 9.1 系统初始化

```csharp
// 1. 创建配置
IBoidSystemConfig config = new BoidSystemConfig();

// 2. 创建工厂
IBoidFactory boidFactory = new BoidFactory(config);
IHierarchyTreeFactory treeFactory = new HierarchyTreeFactory();

// 3. 创建核心组件
ILeaderBoid leader = boidFactory.CreateLeader(Vector2.zero, Vector2.right);
IHierarchyTree tree = treeFactory.CreateTree(leader);
IEnvironmentSensor environment = new EnvironmentSensor();
IVectorPropagator propagator = new VectorPropagator(config.TickInterval);

// 4. 创建系统管理器
IBoidSystemManager system = new BoidSystemManager(leader, tree, environment, propagator);

// 5. 添加小鸟
for (int i = 0; i < config.InitialBoidCount; i++)
{
    IFollowerBoid follower = boidFactory.CreateFollower(GetRandomPosition(), GetRandomDirection());
    system.AddBoid(follower);
}

// 6. 启动模拟
system.StartSimulation();
```

### 9.2 Unity 集成示例

```csharp
// Unity MonoBehaviour 适配器
public class BoidSystemController : MonoBehaviour
{
    [SerializeField] private BoidSystemConfigSO config;
    
    private IBoidSystemManager system;
    private Dictionary<IBoid, IBoidView> viewMap;
    
    private void Start()
    {
        // 初始化系统
        system = CreateSystem(config);
        
        // 创建视图
        foreach (var boid in GetAllBoids())
        {
            IBoidView view = CreateView(boid);
            viewMap[boid] = view;
        }
    }
    
    private void Update()
    {
        if (system.IsRunning)
        {
            system.Tick();
            
            // 更新视图
            foreach (var kvp in viewMap)
            {
                kvp.Value.UpdateVisual(kvp.Key.Position, GetRotation(kvp.Key.Velocity));
            }
        }
    }
}
```

---

## 十、约束条件

### 10.1 层级树约束
- 最大子节点数：3
- 最大深度差：ceil(sqrt(N))
- 无环约束：使用并查集检测
- 树结构必须保持连通

### 10.2 性能约束
- 最大鸟群数量：100只
- 更新频率：10Hz
- 向量传播延迟：深度 * TickInterval

### 10.3 物理约束
- 所有鸟速度大小相同
- 使用Slerp进行朝向平滑插值
- 分离力仅用于避免碰撞

---

## 十一、扩展性考虑

### 11.1 3D扩展
- 将Vector2替换为Vector3
- 将Rect替换为Bounds
- 添加Y轴控制

### 11.2 性能扩展
- 添加空间分区（如四叉树）
- 实现LOD（细节层次）系统
- 支持多线程更新

### 11.3 功能扩展
- 添加障碍物避障
- 实现动态速度调整
- 支持多种鸟群行为模式

---

## 十二、测试接口

### 12.1 单元测试接口

```csharp
public interface IBoidSystemTests
{
    // 层级树测试
    void TestTreeCreation();
    void TestTreeAddNode();
    void TestTreeRemoveNode();
    void TestTreeValidation();
    
    // 向量传播测试
    void TestVectorPropagation();
    void TestVectorBlending();
    
    // 环境感知测试
    void TestAttractorInfluence();
    void TestRepulsorInfluence();
    void TestBoundaryHandling();
}
```

### 12.2 集成测试接口

```csharp
public interface IBoidSystemIntegrationTests
{
    // 完整系统测试
    void TestFullSimulationCycle();
    void TestDynamicBoidAddition();
    void TestDynamicBoidRemoval();
    void TestHierarchyRebalancing();
}
```

---

## 十三、配置文件格式

### 13.1 ScriptableObject 配置

```csharp
[CreateAssetMenu(fileName = "BoidSystemConfig", menuName = "Boid/System Config")]
public class BoidSystemConfigSO : ScriptableObject, IBoidSystemConfig
{
    [Header("基础参数")]
    public float tickInterval = 0.1f;
    public float boidSpeed = 2.0f;
    public int initialBoidCount = 50;
    
    [Header("层级参数")]
    public int maxChildrenPerNode = 3;
    public int maxDepthDifference = 7; // ceil(sqrt(50))
    
    [Header("分离参数")]
    public float separationDistance = 2.0f;
    public float separationStrength = 1.0f;
    
    [Header("边界参数")]
    public Rect boundary = new Rect(-10, -10, 20, 20);
}
```

---

## 十四、错误处理接口

### 14.1 异常定义

```csharp
public class BoidSystemException : System.Exception
{
    public BoidSystemException(string message) : base(message) { }
}

public class HierarchyException : BoidSystemException
{
    public HierarchyException(string message) : base(message) { }
}

public class CycleDetectedException : HierarchyException
{
    public CycleDetectedException() : base("Cycle detected in hierarchy tree") { }
}

public class MaxChildrenExceededException : HierarchyException
{
    public MaxChildrenExceededException() : base("Maximum children per node exceeded") { }
}

public class MaxDepthDifferenceExceededException : HierarchyException
{
    public MaxDepthDifferenceExceededException() : base("Maximum depth difference exceeded") { }
}
```

### 14.2 验证接口

```csharp
public interface ISystemValidator
{
    // 验证方法
    ValidationResult ValidateConfiguration(IBoidSystemConfig config);
    ValidationResult ValidateHierarchy(IHierarchyTree tree);
    ValidationResult ValidateEnvironment(IEnvironmentManager environment);
    
    // 错误处理
    void HandleError(System.Exception error);
}

public struct ValidationResult
{
    public bool IsValid;
    public string ErrorMessage;
    public System.Exception Exception;
}
```

---

## 十五、性能监控接口

### 15.1 性能计数器

```csharp
public interface IPerformanceMonitor
{
    // 计数器
    int TickCount { get; }
    float AverageTickTime { get; }
    float MaxTickTime { get; }
    
    // 方法
    void StartTick();
    void EndTick();
    void Reset();
    
    // 报告
    PerformanceReport GetReport();
}

public struct PerformanceReport
{
    public int TotalTicks;
    public float AverageTickTime;
    public float MaxTickTime;
    public float MinTickTime;
    public int BoidCount;
    public int HierarchyDepth;
}
```

---

## 十六、日志接口

### 16.1 系统日志

```csharp
public interface IBoidSystemLogger
{
    // 日志级别
    void LogInfo(string message);
    void LogWarning(string message);
    void LogError(string message);
    void LogDebug(string message);
    
    // 特殊日志
    void LogHierarchyChange(IBoid node, IBoid oldParent, IBoid newParent);
    void LogVectorPropagation(IBoid source, IBoid target, Vector2 vector);
    void LogEnvironmentChange(EnvironmentElement element, bool added);
}
```

---

## 十七、序列化接口

### 17.1 状态保存/加载

```csharp
public interface ISystemSerializer
{
    // 保存
    string SerializeState(IBoidSystemManager system);
    byte[] SerializeStateBinary(IBoidSystemManager system);
    
    // 加载
    IBoidSystemManager DeserializeState(string json);
    IBoidSystemManager DeserializeStateBinary(byte[] data);
    
    // 验证
    bool ValidateSerializedData(string json);
}
```

---

## 十八、网络同步接口（未来扩展）

### 18.1 网络同步

```csharp
public interface INetworkSync
{
    // 同步方法
    void SyncHierarchy(IHierarchyTree tree);
    void SyncEnvironment(IEnvironmentManager environment);
    void SyncBoidStates(IReadOnlyList<IBoid> boids);
    
    // 压缩
    byte[] CompressState(IBoidSystemManager system);
    IBoidSystemManager DecompressState(byte[] data);
}
```

---

## 十九、附录

### 19.1 默认参数值

| 参数 | 默认值 | 说明 |
|------|--------|------|
| TickInterval | 0.1f | 更新间隔(秒) |
| BoidSpeed | 2.0f | 鸟的速度 |
| InitialBoidCount | 50 | 初始鸟群数量 |
| MaxChildrenPerNode | 3 | 每节点最大子节点数 |
| MaxDepthDifference | 7 | 最大深度差 (ceil(sqrt(50))) |
| SeparationDistance | 2.0f | 分离距离 |
| SeparationStrength | 1.0f | 分离力强度 |
| Boundary | (-10,-10,20,20) | 边界范围 |

### 19.2 命名约定

- 接口：以 `I` 开头，如 `IBoid`, `ILeaderBoid`
- 类：PascalCase，如 `BoidSystemManager`, `HierarchyBuilder`
- 方法：PascalCase，如 `AddBoid()`, `CalculateVelocity()`
- 属性：PascalCase，如 `Position`, `Velocity`
- 事件：以 `On` 开头，如 `OnBoidAdded`, `OnVelocityUpdated`
- 私有字段：`m_` 前缀，如 `m_Leader`, `m_HierarchyTree`

### 19.3 文件组织建议

```
Assets/
├── Scripts/
│   ├── Core/                    # 核心逻辑（纯C#）
│   │   ├── Interfaces/         # 接口定义
│   │   ├── DataStructures/     # 数据结构
│   │   ├── Algorithms/         # 算法实现
│   │   └── Managers/           # 管理类
│   ├── Unity/                  # Unity特定代码
│   │   ├── Adapters/           # 适配器
│   │   ├── Components/         # MonoBehaviour
│   │   ├── Configs/            # ScriptableObject
│   │   └── Editors/            # 编辑器工具
│   └── Tests/                  # 测试代码
│       ├── UnitTests/          # 单元测试
│       └── IntegrationTests/   # 集成测试
├── Configs/                    # 配置文件
├── Prefabs/                    # 预制体
└── Scenes/                     # 场景文件
```

---

## 二十、版本历史

| 版本 | 日期 | 说明 |
|------|------|------|
| 1.0 | 2026-06-24 | 初始版本，基于设计文档创建详细工作规范 |

---

*本WorkSpec文档定义了递推Boid群体行为系统的完整接口规范，供开发团队参考实现。*