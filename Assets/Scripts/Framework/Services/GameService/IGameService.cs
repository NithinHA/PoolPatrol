using System;

namespace PTL.Framework.Services
{
    public interface IGameService : IService
    {
        GameState GameState { get; }
        
        void SwitchState(GameState state);
        
        void AddListener(Action<GameState, GameState> listener);
        void RemoveListener(Action<GameState, GameState> listener);
    }

    public enum GameState
    {
        Bootstrap, MainMenu, InGame
    }
}