using Meta.State;

namespace Core.Save
{
    public sealed class SaveSystem
    {
        private const string Key = "PLAYER_SAVE";
        private readonly ISaveStorage _storage;

        public PlayerSave Current { get; private set; }

        public SaveSystem(ISaveStorage storage)
        {
            _storage = storage;
        }

        public void LoadOrCreate()
        {
            if (!_storage.HasKey(Key))
            {
                Current = new PlayerSave();
                Save();
                return;
            }

            var json = _storage.Load(Key);
            if (string.IsNullOrEmpty(json))
            {
                Current = new PlayerSave();
                Save();
                return;
            }

            Current = JsonUtil.FromJson<PlayerSave>(json);
            Current = SaveMigration.MigrateIfNeeded(Current);
        }

        public void Save()
        {
            var json = JsonUtil.ToJson(Current);
            _storage.Save(Key, json);
        }

        public void HardReset()
        {
            Current = new PlayerSave();
            Save();
        }
    }
}
