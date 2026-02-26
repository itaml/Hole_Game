using UnityEngine;

namespace Core.Save
{
    public sealed class PlayerPrefsJsonStorage : ISaveStorage
    {
        public bool HasKey(string key) => PlayerPrefs.HasKey(key);

        public string Load(string key) => PlayerPrefs.GetString(key, "");

        public void Save(string key, string json)
        {
            PlayerPrefs.SetString(key, json);
            PlayerPrefs.Save();
        }
    }
}
