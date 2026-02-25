public interface IRemoteConfigService
{
    bool GetBool(string key, bool defaultValue);
    int GetInt(string key, int defaultValue);
    void FetchAndActivate(System.Action onDone = null); // заглушка
}