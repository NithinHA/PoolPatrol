using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;

namespace PTL.Framework
{
    public class SessionManager : Singleton<SessionManager>
    {
        private ISession _activeSession;

        private ISession ActiveSession
        {
            get => _activeSession;
            set
            {
                _activeSession = value;
                Debug.Log($"Active session: {_activeSession}");
            }
        }

        const string playerNamePropertyKey = "playerName";

        private async void Start()
        {
            try
            {
                await UnityServices.InitializeAsync(); // Initialize Unity Gaming Services SDKs.
                await AuthenticationService.Instance.SignInAnonymouslyAsync(); // Anonymously authenticate the player
                Debug.Log($"Sign in anonymously succeeded! PlayerID: {AuthenticationService.Instance.PlayerId}");

                // Start a new session as a host
                StartSessionAsHost();
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        }

        async Task<Dictionary<string, PlayerProperty>> GetPlayerProperties()
        {
            // Custom game-specific properties that apply to an individual player, ie: name, role, skill level, etc.
            var playerName = await AuthenticationService.Instance.GetPlayerNameAsync();
            var playerNameProperty = new PlayerProperty(playerName, VisibilityPropertyOptions.Member);
            return new Dictionary<string, PlayerProperty> { { playerNamePropertyKey, playerNameProperty } };
        }

        async Task<Dictionary<string, PlayerProperty>> GetPlayerProperties(string playerName)
        {
            return new Dictionary<string, PlayerProperty>
            {
                {
                    Constants.MultiplayerConstants.PlayerProperty_Name,
                    new PlayerProperty(playerName, VisibilityPropertyOptions.Member)
                },
                {
                    Constants.MultiplayerConstants.PlayerProperty_Character,
                    new PlayerProperty("0", VisibilityPropertyOptions.Member)
                },
                {
                    Constants.MultiplayerConstants.PlayerProperty_Floatie,
                    new PlayerProperty("0", VisibilityPropertyOptions.Member)
                },
                {
                    Constants.MultiplayerConstants.PlayerProperty_Weapon,
                    new PlayerProperty("0", VisibilityPropertyOptions.Member)
                },
                {
                    Constants.MultiplayerConstants.PlayerProperty_Ready,
                    new PlayerProperty("0", VisibilityPropertyOptions.Member)
                },
            };
        }

        async void StartSessionAsHost()
        {
            var playerProperties = await GetPlayerProperties();

            var options = new SessionOptions
            {
                MaxPlayers = 2,
                IsLocked = false,
                IsPrivate = false,
                PlayerProperties = playerProperties
            }.WithRelayNetwork(); // or WithDistributedAuthorityNetwork() to use Distributed Authority instead of Relay

            ActiveSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            Debug.Log($"Session {ActiveSession.Id} created! Join code: {ActiveSession.Code}");
        }

        async void JoinSessionById(string sessionId)
        {
            ActiveSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
            Debug.Log($"Session {ActiveSession.Id} joined!");
        }

        async void JoinSessionByCode(string sessionCode)
        {
            ActiveSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
            Debug.Log($"Session {ActiveSession.Id} joined!");
        }

        async void KickPlayer(string playerId)
        {
            if (!ActiveSession.IsHost) return;
            await ActiveSession.AsHost().RemovePlayerAsync(playerId);
        }

        async Task<IList<ISessionInfo>> QuerySessions()
        {
            var sessionQueryOptions = new QuerySessionsOptions();
            QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(sessionQueryOptions);
            return results.Sessions;
        }

        async void LeaveSession()
        {
            if (ActiveSession != null)
            {
                try
                {
                    await ActiveSession.LeaveAsync();
                }
                catch
                {
                    // Ignored as we are exiting the game
                }
                finally
                {
                    ActiveSession = null;
                }
            }
        }
    }
}