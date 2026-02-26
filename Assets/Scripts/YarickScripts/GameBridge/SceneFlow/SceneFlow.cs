using UnityEngine.SceneManagement;
using GameBridge.Contracts;

namespace GameBridge.SceneFlow
{
    public static class SceneFlow
    {
        public static RunConfig PendingRunConfig { get; private set; }
        public static LevelResult PendingLevelResult { get; private set; }

        public static void StartGame(RunConfig config)
        {
            PendingRunConfig = config;
            PendingLevelResult = null;
            SceneManager.LoadScene(SceneNames.Game);
        }

        public static void ReturnToMenu(LevelResult result)
        {
            PendingLevelResult = result;
            PendingRunConfig = null;
            SceneManager.LoadScene(SceneNames.Menu);
        }
    }
}
