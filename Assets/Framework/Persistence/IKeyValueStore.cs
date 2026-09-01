namespace MobileGamesFramework.Persistence
{
    public interface IKeyValueStore
    {
        string GetString(string key, string defaultValue);
        void SetString(string key, string value);
    }
}
