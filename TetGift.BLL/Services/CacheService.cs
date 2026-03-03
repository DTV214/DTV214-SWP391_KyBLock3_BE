using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using TetGift.BLL.Interfaces;

namespace TetGift.BLL.Services
{
    public class CacheService(IDistributedCache cache) : ICacheService
    {
        private readonly IDistributedCache _cache = cache;
        private readonly JsonSerializerOptions _jsonOptions = new JsonSerializerOptions
        {
            // Giúp khớp "Data" với "data"
            PropertyNameCaseInsensitive = true,

            // Tránh lỗi vòng lặp nếu có
            ReferenceHandler = ReferenceHandler.IgnoreCycles
        };

        // Lấy dữ liệu và Deserialize
        public async Task<T?> GetAsync<T>(string key)
        {
            var cachedData = await _cache.GetStringAsync(key);
            if (string.IsNullOrEmpty(cachedData)) return default;

            return JsonSerializer.Deserialize<T>(cachedData, _jsonOptions);
        }

        // Serialize và Lưu dữ liệu
        public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null)
        {
            var options = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiration ?? TimeSpan.FromMinutes(10)
            };
            var jsonData = JsonSerializer.Serialize(value, _jsonOptions);
            await _cache.SetStringAsync(key, jsonData, options);
        }

        // Tạo Key duy nhất bằng MD5 Hash (Tránh key quá dài và ký tự đặc biệt)
        public string GenerateCacheKey(object queryParameters, string prefix)
        {
            var jsonQuery = JsonSerializer.Serialize(queryParameters);
            var hashBytes = MD5.HashData(Encoding.UTF8.GetBytes(jsonQuery));
            var hashString = Convert.ToHexString(hashBytes);

            return $"{prefix}:{hashString}";
        }

        // Xóa cache theo prefix
        public async Task RemoveByPrefixAsync(string prefix)
        {
            // No implement 
        }

        public async Task<string> GetVersionAsync(string module)
        {
            var version = await _cache.GetStringAsync($"{module}:Version");
            return version ?? "1";
        }

        public async Task IncreaseVersionAsync(string module)
        {
            var current = await GetVersionAsync(module);
            int next = int.Parse(current) + 1;
            await _cache.SetStringAsync($"{module}:Version", next.ToString());
        }
    }
}
