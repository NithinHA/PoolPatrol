public class Constants
{
    public class GameConstants
    {

#region Tags

        public const string TAG_Player = "Player";
        public const string TAG_Enemy = "Enemy";
        public const string TAG_Bullet = "Bullet";

#endregion

#region Bullet collision parameters

        public const string BULLET_COLLISION_Collider = "BulletCollisionCollider";
        public const string BULLET_COLLISION_Direction = "BulletCollisionDirection";

#endregion

    }

    public class EnvironmentConstants
    {
        public const float X_MIN = -11.5f, X_MAX = 11.5f;
        public const float Y_MIN = -5.5f, Y_MAX = 5.5f;
        public const float SpawnWidth = 9.5f, SpawnHeight = 5.5f;
    }
    
    public static class Audio
    {
        public const string BGM = "BGM";

        public const string CLICK = "Click";
        // public const string PICK = "Pick";
        // public const string DROP = "Drop";
        //     
        // public const string WIN = "Win";
        // public const string LOSE = "Lose";
        // public const string DRAW = "Draw";
    }

    public static class SceneNames
    {
        public const string BOOTSTRAP = "Bootstrap";
        public const string GAME = "Game";
    }
}
