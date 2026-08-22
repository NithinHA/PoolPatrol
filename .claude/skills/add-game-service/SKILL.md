---
name: add-game-service
description: Add a new persistent game service to PoolPatrol (via ServiceLocator), or work with the game state machine, highscore, or scene loading services. Use when the task needs cross-scene singleton logic, a new manager-style service, game-state transitions, or scene changes. Explains ServiceLocator, IService, GameManager state machine, and Bootstrap registration.
---

# Adding / using a game service

Persistent, non-scene logic is delivered as **services**: plain C# classes (not MonoBehaviours)
implementing `IService`, registered once in `Bootstrap` and resolved through the static
`ServiceLocator`. Lives in `Assets/Scripts/Framework/Services` (namespaces `PTL.Framework` /
`PTL.Framework.Services`).

## The contract

`IService` (`Assets/Scripts/Framework/Services/IService.cs`):
```csharp
public interface IService { void Start(); void OnDestroy(); }
```
`ServiceLocator.RegisterService` calls `Start()` on register; `UnregisterAllServices` (called from
`Bootstrap.OnDestroy`) calls `OnDestroy()`. Existing services: `GameManager : IGameService`,
`HighscoreService : IHighscore`, `SceneService : ISceneService`.

## Steps to add a new service

1. Create an interface extending `IService`, e.g.
   `Assets/Scripts/Framework/Services/<Name>Service/I<Name>.cs`:
   ```csharp
   namespace PTL.Framework.Services { public interface IInventory : IService { /* API */ } }
   ```
2. Implement it as a plain class in the same folder:
   ```csharp
   public class InventoryService : IInventory {
       public void Start() { /* init */ }
       public void OnDestroy() { /* cleanup */ }
   }
   ```
3. Register it in `Bootstrap.InitializeAllServices` (`Assets/Scripts/Framework/Bootstrap.cs`) by adding
   `{ typeof(IInventory), new InventoryService() }` to the map.
4. (Optional) Add a convenience getter to `ServiceLocator`, matching the `GetGameManager()` /
   `GetHighscoreService()` pattern (they null-check via `HasService` first).

## Resolving a service

```csharp
var scenes = ServiceLocator.GetService<ISceneService>();
scenes.LoadScene(Constants.SceneNames.GAME, onComplete);
// or the null-safe convenience getters:
ServiceLocator.GetGameManager()?.SwitchState(GameState.MainMenu);
```
`GetService<T>` logs an error and returns null if the type isn't registered.

## Game state machine

`GameManager` (`IGameService`) holds a `GameState` and broadcasts a **static**
`Action<GameState prev, GameState cur>` on `SwitchState`. Subscribe/unsubscribe via
`AddListener` / `RemoveListener`. Use this for global transitions (e.g. it plays BGM when leaving
`Bootstrap`). To add a state, extend the `GameState` enum and handle it in listeners.

## Services vs. Singletons — which to use

- **Service** (`IService` + `ServiceLocator`): persistent, cross-scene, no scene GameObject needed
  (state, highscores, scene loading, save data).
- **`Singleton<T>`** (`Assets/Scripts/Framework/Singleton.cs`): a MonoBehaviour that must live in the
  scene and use Unity callbacks / inspector references (e.g. `AudioManager`, `CameraShaker`,
  `ObjectPoolManager`-style managers). Access via `Type.Instance` (null-conditional — it may not exist yet).

Conventions: `_camelCase` privates, `PascalCase` public. See **poolpatrol-architecture**.
