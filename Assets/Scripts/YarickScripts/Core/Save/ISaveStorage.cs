namespace Core.Save
{
    public interface ISaveStorage
    {
        bool HasKey(string key);
        string Load(string key);
        void Save(string key, string json);
    }
}
