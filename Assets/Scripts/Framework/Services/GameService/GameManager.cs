using System;
using UnityEngine;

namespace PTL.Framework.Services
{
    public class GameManager : IGameService
    {
        private GameState _gameState;
        public GameState GameState => _gameState;
        private static Action<GameState, GameState> _onGameStateChanged;
        
#region Default callbacks

        public void Start()
        {
            AddListener(OnGameStateChanged);
        }

        public void OnDestroy()
        {
            RemoveListener(OnGameStateChanged);
        }

#endregion

        public void SwitchState(GameState state)
        {
            GameState prev = _gameState;
            _gameState = state;
            _onGameStateChanged?.Invoke(prev, _gameState);
        }

        public void AddListener(Action<GameState, GameState> listener)
        {
            _onGameStateChanged += listener;
        }

        public void RemoveListener(Action<GameState, GameState> listener)
        {
            _onGameStateChanged -= listener;
        }

#region Event listeners

        private void OnGameStateChanged(GameState prevState, GameState curState)
        {
            if (prevState == GameState.Bootstrap)
                AudioManager.Instance?.PlaySound(Constants.Audio.BGM);
        }

#endregion
    }
}