using UnityEngine;

namespace Core.Save
{
    public static class JsonUtil
    {
        /// <summary>
        /// JsonUtility can't serialize polymorphism and dictionaries; keep PlayerSave plain and serializable.
        /// </summary>
        public static string ToJson<T>(T obj) => JsonUtility.ToJson(obj);

        public static T FromJson<T>(string json) => JsonUtility.FromJson<T>(json);
    }
}
