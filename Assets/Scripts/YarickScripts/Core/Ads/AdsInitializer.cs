using UnityEngine;

public class AdsInitializer : MonoBehaviour
{
    [SerializeField] private ATTRequestor attRequestor;
    [SerializeField] private LevelPlayManager levelPlayManager;

    public async void Initialize()
    {
        Debug.Log("AdsInitializer: Starting initialization...");

        if (levelPlayManager == null)
        {
            Debug.LogError("LevelPlayManager not assigned!");
            return;
        }

        // Если ATT уже завершён (Android или уже решено на iOS)
        if (attRequestor != null && attRequestor.IsDone)
        {
            levelPlayManager.SetUserConsent(attRequestor.IsAuthorized);
            await levelPlayManager.InitializeAsync();
            return;
        }

        // Если есть ATTRequestor — ждём
        if (attRequestor != null)
        {
            attRequestor.OnFinished += OnATTFinished;
            attRequestor.Request();
            return;
        }

        // Если ATTRequestor нет — просто init
        await levelPlayManager.InitializeAsync();
    }

    private async void OnATTFinished(bool authorized)
    {
        Debug.Log($"ATT Finished. authorized={authorized}");

        if (attRequestor != null)
            attRequestor.OnFinished -= OnATTFinished;

        if (levelPlayManager != null)
        {
            levelPlayManager.SetUserConsent(authorized);
            await levelPlayManager.InitializeAsync();
        }
    }

    private void OnDestroy()
    {
        if (attRequestor != null)
            attRequestor.OnFinished -= OnATTFinished;
    }
}