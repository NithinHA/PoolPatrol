namespace PTL.Framework
{
    /// <summary>
    /// Carries the arena/level the player picked in the Menu scene across the scene load into
    /// the Game scene. Plain static state (not a service) since it only needs to survive one
    /// scene transition within the same play session, not be looked up polymorphically.
    /// </summary>
    public static class GameSession
    {
        public static int SelectedArenaIndex;
        public static int SelectedLevelIndex;
    }
}
