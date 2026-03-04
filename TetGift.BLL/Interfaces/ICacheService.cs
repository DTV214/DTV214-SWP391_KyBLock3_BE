namespace TetGift.BLL.Interfaces
{
    public interface ICacheService
    {
        Task<T?> GetAsync<T>(string key);
        Task SetAsync<T>(string key, T value, TimeSpan? expiration = null);
        Task RemoveByPrefixAsync(string prefix);
        string GenerateCacheKey(object queryParameters, string prefix);
        Task<string> GetVersionAsync(string module);
        Task IncreaseVersionAsync(string module);
    }
}
