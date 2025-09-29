using System;
using System.Collections.Generic;
using Enemy;

public enum PhaseType { Cooldown, Wave, Boss }

[Serializable]
public class PhaseData
{
    public PhaseType Type;
    public int TotalKillsRequired; // Only relevant for cooldown
    public int MaxConcurrentEnemies;
    public float SpawnIntervalMin;
    public float SpawnIntervalMax;
    public List<EnemyType> AllowedEnemyTypes;
    public List<EnemyType> PositiveEnemies; // e.g., heart/gem droppers
    public bool HasBoss;
    public EnemyType BossEnemy; // Optional, only if HasBoss == true
}