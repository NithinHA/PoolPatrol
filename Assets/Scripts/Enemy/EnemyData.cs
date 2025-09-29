using System;
using UnityEngine;

namespace Enemy
{
    public enum EnemyType
    {
        Idle,
        RandomMove,
        ChaseStepWise,
        ChaseContinuous,
        Shooting,
        StarFish,
        GemDropper,
        HeartDropper,
    }

    [Obsolete]
    public class EnemyData
    {
        public long ID;
        public EnemyType Type;
        public GameObject Prefab;
    }
}