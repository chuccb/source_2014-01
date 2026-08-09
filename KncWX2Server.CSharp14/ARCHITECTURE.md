# KncWX2Server C# 14 Migration - Architecture Overview

## Project Structure

```
KncWX2Server.CSharp14/
├── Core/
│   ├── Collections/         # Utility classes (SmartPointer)
│   ├── Reflection/          # RTTI replacement
│   ├── Singleton/           # Singleton pattern
│   ├── FSM/                 # Finite State Machine
│   ├── Network/             # Network communication
│   ├── Data/                # Game data (ExpTable, GameSession)
│   ├── Actor/               # Actor system
│   ├── Messaging/           # Message handling
│   ├── Authentication/      # Auth service
│   ├── Database/            # Database abstraction
│   ├── Logging/             # Logging utilities
│   └── Server/              # Server bootstrap
├── Servers/                 # Server implementations
│   ├── LoginServer/
│   ├── GameServer/
│   └── CenterServer/
└── Tests/                   # Unit tests

```

## Key Improvements from C++ to C# 14

### 1. **Memory Management**
- ✅ Automatic garbage collection instead of manual new/delete
- ✅ `using` statements for resource cleanup
- ✅ `IAsyncDisposable` for async cleanup operations

### 2. **Async/Await**
- ✅ Native async networking (no callbacks)
- ✅ `Task`-based concurrency model
- ✅ Cancellation tokens for graceful shutdown

### 3. **Type Safety**
- ✅ Nullable reference types (`#nullable enable`)
- ✅ Strong typing without macros
- ✅ Reflection for RTTI replacement

### 4. **C# 14 Features**
- ✅ Primary constructors
- ✅ Record types for immutable data
- ✅ Pattern matching
- ✅ Collection expressions
- ✅ Required properties
- ✅ Caller info attributes for logging

### 5. **Concurrency**
- ✅ `ConcurrentDictionary` for thread-safe collections
- ✅ `ReaderWriterLockSlim` for optimized locking
- ✅ Task parallel library for scalable async operations

### 6. **Error Handling**
- ✅ Exception-based error handling (no error codes)
- ✅ Structured logging with Serilog
- ✅ Caller context in logs

## C++ to C# Migration Mapping

| C++ | C# 14 |
|-----|-------|
| `KActor` | `Actor` base class |
| `KSession` | `ISession` interface |
| `KGSFSM` | `FiniteStateMachine<,>` generic |
| `KExpTable` | `ExpTable` singleton |
| `shared_ptr<T>` | `SmartPointer<T>` wrapper or direct reference |
| `map<K,V>` | `ConcurrentDictionary<K,V>` or `Dictionary<K,V>` |
| `NiDeclareRTTI` | `ReflectionHelper` + `TypeMetadata` |
| Callbacks | `Action<T>` / `Func<T>` / Events |
| Win32 Sockets | `System.Net.Sockets.TcpListener` / `Socket` |
| ODBC | `Entity Framework Core` |
| Lua C API | `NLua` or `MoonSharp` |

## Threading Model

- **Network Thread**: Accepts connections, receives messages
- **Update Thread**: Updates actor states (game loop)
- **Worker Threads**: Task parallel library for message processing
- **All collections are thread-safe** using `ConcurrentDictionary` and locks

## Data Flow

```
1. Client connects
   ↓
2. NetworkServer accepts connection → creates TcpSession
   ↓
3. GameSession created and registered
   ↓
4. Actor created for player
   ↓
5. Network messages arrive → MessageDispatcher processes
   ↓
6. Message handlers update game state
   ↓
7. ActorManager update loop processes frame updates
   ↓
8. State changes propagated to client
   ↓
9. Client disconnects → cleanup resources
```

## Next Steps

### Phase 3: Server Components
- [ ] Implement `LoginServer`
- [ ] Implement `GameServer` with game logic
- [ ] Implement `CenterServer` for inter-server communication

### Phase 4: Game Systems
- [ ] Combat system
- [ ] Item system
- [ ] Quest system
- [ ] Dungeon system

### Phase 5: Lua Integration
- [ ] Script bindings
- [ ] Configuration loading
- [ ] Game script execution

## Testing Strategy

1. **Unit Tests**: Test individual components in isolation
2. **Integration Tests**: Test component interactions
3. **Load Tests**: Verify performance with many concurrent players
4. **Protocol Tests**: Ensure compatibility with game clients

## Performance Targets

- Handle 1000+ concurrent connections
- Process 10,000+ messages per second
- Maintain 60 FPS game loop with <16ms frame time
- GC pause < 5ms (tuned GC settings)
- Memory usage comparable to C++ version

---

## Building and Running

```bash
# Build
dotnet build KncWX2Server.CSharp14.sln

# Run tests
dotnet test

# Publish
dotnet publish -c Release
```

## Configuration

Server configuration via `ServerConfiguration` record:

```csharp
var config = new ServerConfiguration
{
    BindAddress = "0.0.0.0",
    Port = 9300,
    DatabaseConnectionString = "Server=localhost;Database=KncWX2;",
    MaxConnections = 1000,
    UpdateInterval = 0.016f
};
```

## Logging

All logging uses Serilog with:
- Console output for development
- File output with daily rolling intervals
- Caller information (method name, file, line number)
- Structured logging with properties

---

*Last Updated: 2026-08-09*
