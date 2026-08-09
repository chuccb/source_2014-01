# KncWX2Server C# 14 Migration Plan

## Overview
This document outlines the comprehensive migration strategy for converting the KncWX2Server project from C++ to C# 14.

## Current Architecture
- **Language Composition**: C++ (39.5%), C (31.6%), Lua (24.7%), C# (2.1%)
- **Main Components**:
  - LoginServer
  - GameServer
  - CenterServer
  - GameClient
  - Common Libraries (FSM, RTTI, SmartPointer)
  - Database (SQL Server via ODBC)
  - Lua Integration

## Migration Phases

### Phase 1: Foundation & Core Libraries (Weeks 1-2)
- [ ] Create .NET 8 project structure
- [ ] Implement C# 14 base classes and interfaces
- [ ] Replace SmartPointer with System.WeakReference<T>
- [ ] Implement RTTI equivalent using Reflection
- [ ] Create Singleton pattern wrapper
- [ ] Database abstraction layer with Entity Framework Core

### Phase 2: Common Utilities (Weeks 3-4)
- [ ] Port ExpTable system
- [ ] Implement FSM (Finite State Machine) framework
- [ ] Create Session management system
- [ ] Port network socket handling
- [ ] Implement logging framework

### Phase 3: Server Infrastructure (Weeks 5-6)
- [ ] Network communication layer (async/await)
- [ ] Session/Actor management
- [ ] Message handling pipeline
- [ ] Configuration system

### Phase 4: Server Components (Weeks 7-10)
- [ ] LoginServer migration
- [ ] GameServer core functionality
- [ ] CenterServer services
- [ ] Game logic systems

### Phase 5: Lua Integration (Weeks 11-12)
- [ ] NLua/MoonSharp integration
- [ ] Script binding system
- [ ] Configuration script processing

### Phase 6: Testing & Optimization (Weeks 13-14)
- [ ] Unit tests
- [ ] Integration tests
- [ ] Performance optimization
- [ ] Deployment testing

## C# 14 Key Features to Leverage

### 1. **Primary Constructors**
```csharp
public class Session(string sessionId, INetworkManager network) { }
```

### 2. **Record Types for Data Transfer**
```csharp
public record ExpData(int NeedExp, int TotalExp);
```

### 3. **Required Properties**
```csharp
public class Player
{
    public required string Name { get; set; }
    public required int Level { get; set; }
}
```

### 4. **Collection Expressions**
```csharp
var players = [player1, player2, player3];
```

### 5. **Pattern Matching Enhancements**
```csharp
var message = message switch
{
    LoginRequest req => HandleLogin(req),
    GameRequest req => HandleGame(req),
    _ => HandleUnknown()
};
```

### 6. **Params Collections**
```csharp
public void SendMessage(string format, params ReadOnlySpan<object> args) { }
```

## C++ to C# Mapping

| C++ Concept | C# 14 Equivalent |
|-------------|----------------|
| `shared_ptr<T>` | `System.WeakReference<T>` or reference semantics |
| `RTTI` macros | `reflection` + `Type` class |
| `virtual` + override | `virtual` + `override` |
| `enum class` | `enum` |
| `map<K,V>` | `Dictionary<K,V>` |
| `vector<T>` | `List<T>` or `Collection<T>` |
| Callbacks | `delegates` / `Action<T>` / `Func<T>` |
| `new`/`delete` | GC managed (IDisposable for unmanaged) |
| Macros | static methods / generics |
| Win32 API | `pinvoke` / `P/Invoke` |

## Dependencies & Packages

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="8.0" />
  <PackageReference Include="NLua" Version="1.6.7" />
  <PackageReference Include="Serilog" Version="3.0" />
  <PackageReference Include="Serilog.Sinks.File" Version="5.0" />
  <PackageReference Include="System.Configuration.ConfigurationManager" Version="8.0" />
</ItemGroup>
```

## Performance Considerations

1. **GC Pressure**: Use object pooling for frequently allocated objects
2. **Async I/O**: Fully async networking layer
3. **Memory**: Leverage `Span<T>` and `stackalloc` for hot paths
4. **Profiling**: Use BenchmarkDotNet for performance testing

## Risks & Mitigation

| Risk | Mitigation |
|------|----------|
| GC pauses in gameplay | Implement object pooling, tune GC settings |
| Performance regression | Profile early, benchmark against C++ |
| Lua script compatibility | Create comprehensive script binding layer |
| Database migration | Gradual migration with parallel systems |
| Network protocol changes | Create protocol adapters for compatibility |

## Success Criteria

- ✅ All components successfully compile
- ✅ Unit test coverage > 80%
- ✅ Performance within 10% of original C++ version
- ✅ Backward compatible with existing game clients
- ✅ Successfully handles concurrent player connections
- ✅ Database operations fully functional

## Timeline

**Total Duration**: ~14 weeks
**Start Date**: [To be determined]
**Target Release**: [To be determined]

---

*This plan is a living document and will be updated as the migration progresses.*
