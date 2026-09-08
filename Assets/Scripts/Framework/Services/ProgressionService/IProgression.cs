using SpawningLogic;

namespace PTL.Framework.Services
{
    public interface IProgression : IService
    {
        bool IsLevelUnlocked(int arenaIndex, int levelIndex);
        bool IsLevelCompleted(int arenaIndex, int levelIndex);
        void CompleteLevel(int arenaIndex, int levelIndex);
        int GetHighestUnlockedArena();
        int GetHighestUnlockedLevel(int arenaIndex);
        ArenaSpawnConfigSO GetLevelConfig(int arenaIndex, int levelIndex);
        ArenaDefinitionSO GetArenaDefinition(int arenaIndex);
        int ArenaCount { get; }
    }
}
