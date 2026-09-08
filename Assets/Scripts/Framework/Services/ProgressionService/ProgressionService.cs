using System.Collections.Generic;
using SpawningLogic;
using UnityEngine;

namespace PTL.Framework.Services
{
    public class ProgressionService : IProgression
    {
        private const string PROGRESS_KEY_PREFIX = "Arena_{0}_Level_{1}";

        private readonly List<ArenaDefinitionSO> _arenas;

        public ProgressionService(List<ArenaDefinitionSO> arenas)
        {
            _arenas = arenas;
        }

        public int ArenaCount => _arenas.Count;

        public void Start() { }
        public void OnDestroy() { }

        public bool IsLevelUnlocked(int arenaIndex, int levelIndex)
        {
            if (arenaIndex == 0 && levelIndex == 0) return true;
            if (levelIndex > 0)
                return IsLevelCompleted(arenaIndex, levelIndex - 1);
            return arenaIndex > 0 && IsArenaCompleted(arenaIndex - 1);
        }

        public void CompleteLevel(int arenaIndex, int levelIndex)
        {
            PlayerPrefs.SetInt(ProgressKey(arenaIndex, levelIndex), 1);
            PlayerPrefs.Save();
        }

        public int GetHighestUnlockedArena()
        {
            for (int i = _arenas.Count - 1; i >= 0; i--)
            {
                if (IsLevelUnlocked(i, 0)) return i;
            }
            return 0;
        }

        public int GetHighestUnlockedLevel(int arenaIndex)
        {
            var arena = _arenas[arenaIndex];
            for (int i = arena.LevelCount - 1; i >= 0; i--)
            {
                if (IsLevelUnlocked(arenaIndex, i)) return i;
            }
            return 0;
        }

        public ArenaSpawnConfigSO GetLevelConfig(int arenaIndex, int levelIndex)
        {
            return _arenas[arenaIndex].Levels[levelIndex];
        }

        public ArenaDefinitionSO GetArenaDefinition(int arenaIndex)
        {
            return _arenas[arenaIndex];
        }

        public bool IsLevelCompleted(int arenaIndex, int levelIndex)
        {
            return PlayerPrefs.GetInt(ProgressKey(arenaIndex, levelIndex), 0) == 1;
        }

        private bool IsArenaCompleted(int arenaIndex)
        {
            var arena = _arenas[arenaIndex];
            return IsLevelCompleted(arenaIndex, arena.LevelCount - 1);
        }

        private static string ProgressKey(int arenaIndex, int levelIndex)
        {
            return string.Format(PROGRESS_KEY_PREFIX, arenaIndex, levelIndex);
        }
    }
}
