using UnityEngine;

namespace PTL.Framework.Services
{
    public class HighscoreService: IHighscore
    {
        private const string HIGHSCORE_PERSISTENT_KEY = "Highscore";

        private int _highscore
        {
            get
            {
                if (!PlayerPrefs.HasKey(HIGHSCORE_PERSISTENT_KEY))
                    PlayerPrefs.SetInt(HIGHSCORE_PERSISTENT_KEY, 0);
                return PlayerPrefs.GetInt(HIGHSCORE_PERSISTENT_KEY);
            }
            set => PlayerPrefs.SetInt(HIGHSCORE_PERSISTENT_KEY, value);
        }
        
#region Default callbacks

        public void Start()
        { }

        public void OnDestroy()
        { }

#endregion

        public int GetHighscore() => _highscore;

        public void SetHighscore(int value)
        {
            _highscore = value;
        }
    }
}