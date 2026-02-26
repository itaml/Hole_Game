using Meta.State;

namespace Core.Save
{
    public static class SaveMigration
    {
        public static PlayerSave MigrateIfNeeded(PlayerSave save)
        {
            if (save == null) return new PlayerSave();

            // Example:
            // if (save.version == 1) { ...; save.version = 2; }

            return save;
        }
    }
}
