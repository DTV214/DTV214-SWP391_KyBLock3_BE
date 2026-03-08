using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TetGift.BLL.Dtos;
using TetGift.DAL.Interfaces;

namespace TetGift.BLL.Services
{
    public class GeminiChatService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<GeminiChatService> _logger;

        private static readonly object _toolDeclarations = new
        {
            function_declarations = new object[]
            {
                new
                {
                    name = "search_products",
                    description = "Tìm kiếm sản phẩm theo từ khóa tên hoặc danh mục. Dùng khi khách hỏi về sản phẩm, tìm quà, hỏi giá.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            keyword = new { type = "string", description = "Từ khóa tìm kiếm trong tên sản phẩm" },
                            category = new { type = "string", description = "Tên danh mục (ví dụ: Bánh ngọt, Kẹo mứt)" },
                            max_price = new { type = "number", description = "Giá tối đa (VNĐ)" }
                        }
                    }
                },
                new
                {
                    name = "get_product_detail",
                    description = "Lấy thông tin chi tiết của một sản phẩm theo ID.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            product_id = new { type = "integer", description = "ID của sản phẩm" }
                        },
                        required = new[] { "product_id" }
                    }
                },
                new
                {
                    name = "compare_products",
                    description = "So sánh nhiều sản phẩm với nhau theo danh sách ID.",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            product_ids = new
                            {
                                type = "array",
                                description = "Danh sách ID sản phẩm cần so sánh",
                                items = new { type = "integer" }
                            }
                        },
                        required = new[] { "product_ids" }
                    }
                }
            }
        };
    
        private const string SystemPrompt =
            "Bạn là trợ lý tư vấn bán hàng cho TetGift - cửa hàng quà Tết cao cấp. " +
            "Trả lời bằng tiếng Việt, thân thiện và chuyên nghiệp. " +
            "Chỉ tư vấn dựa trên sản phẩm thực tế trong database. " +
            "Dùng tools để tra cứu sản phẩm khi khách hỏi. " +
            "Nếu không tìm thấy sản phẩm phù hợp, hãy nói thật thay vì bịa đặt.";

        public GeminiChatService(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IProductRepository productRepository,
            ILogger<GeminiChatService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<ChatResponse> ChatAsync(string userMessage)
        {
            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];
                var model = _configuration["Gemini:Model"];

                if (string.IsNullOrWhiteSpace(apiKey))
                    return new ChatResponse { Reply = "Xin lỗi, hệ thống chatbot chưa được cấu hình đúng. Vui lòng liên hệ quản trị viên." };

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";
                var httpClient = _httpClientFactory.CreateClient();

                // Build mutable contents list for agentic loop
                var contents = new List<object>
                {
                    new { role = "user", parts = new[] { new { text = userMessage } } }
                };

                // Agentic loop: tối đa 5 vòng để tránh vòng lặp vô tận
                for (int i = 0; i < 5; i++)
                {
                    var requestBody = new
                    {
                        system_instruction = new { parts = new[] { new { text = SystemPrompt } } },
                        contents,
                        tools = new[] { _toolDeclarations }
                    };

                    var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                    var httpContent = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await httpClient.PostAsync(url, httpContent);
                    var responseString = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation("Gemini response status: {StatusCode}", response.StatusCode);

                    if (!response.IsSuccessStatusCode)
                    {
                        _logger.LogError("Gemini API error. Status: {StatusCode}, Body: {Body}", response.StatusCode, responseString);

                        if ((int)response.StatusCode == 429)
                            return new ChatResponse { Reply = "Xin lỗi, hệ thống AI đang bận. Vui lòng thử lại sau ít phút." };

                        return new ChatResponse { Reply = "Xin lỗi, hệ thống AI đang gặp sự cố. Vui lòng thử lại sau." };
                    }

                    var geminiResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

                    if (!geminiResponse.TryGetProperty("candidates", out var candidates) || candidates.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("No candidates in Gemini response.");
                        return new ChatResponse { Reply = "Xin lỗi, tôi không thể xử lý yêu cầu này lúc này." };
                    }

                    var candidate = candidates[0];
                    var candidateContent = candidate.GetProperty("content");
                    var parts = candidateContent.GetProperty("parts");
                    var finishReason = candidate.TryGetProperty("finishReason", out var fr) ? fr.GetString() : null;

                    // STOP → trả về text
                    if (finishReason == "STOP")
                    {
                        var text = parts[0].GetProperty("text").GetString();
                        return new ChatResponse { Reply = text ?? "Xin lỗi, tôi không có câu trả lời." };
                    }

                    // Function call → thực thi rồi loop tiếp
                    if (parts[0].TryGetProperty("functionCall", out var functionCall))
                    {
                        var funcName = functionCall.GetProperty("name").GetString()!;
                        var funcArgs = functionCall.GetProperty("args");

                        _logger.LogInformation("Gemini function call: {FunctionName}", funcName);

                        var funcResult = await ExecuteFunctionAsync(funcName, funcArgs);

                        // Thêm model turn
                        contents.Add(new
                        {
                            role = "model",
                            parts = new[] { new { functionCall } }
                        });

                        // Thêm function response turn
                        contents.Add(new
                        {
                            role = "function",
                            parts = new[]
                            {
                                new
                                {
                                    functionResponse = new
                                    {
                                        name = funcName,
                                        response = funcResult
                                    }
                                }
                            }
                        });

                        continue;
                    }

                    // Fallback nếu không có text lẫn function call
                    _logger.LogWarning("Unexpected Gemini response structure: {Response}", responseString);
                    return new ChatResponse { Reply = "Xin lỗi, tôi không thể xử lý yêu cầu này." };
                }

                return new ChatResponse { Reply = "Xin lỗi, hệ thống xử lý quá lâu. Vui lòng thử câu hỏi ngắn gọn hơn." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unhandled error in ChatAsync");
                return new ChatResponse { Reply = "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau." };
            }
        }

        private async Task<object> ExecuteFunctionAsync(string functionName, JsonElement args)
        {
            try
            {
                switch (functionName)
                {
                    case "search_products":
                    {
                        var keyword = args.TryGetProperty("keyword", out var kw) ? kw.GetString() : null;
                        var category = args.TryGetProperty("category", out var cat) ? cat.GetString() : null;
                        decimal? maxPrice = args.TryGetProperty("max_price", out var mp) && mp.ValueKind == JsonValueKind.Number
                            ? mp.GetDecimal() : null;

                        var products = await _productRepository.SearchAsync(keyword, category, maxPrice);
                        var dtos = products.Select(p => new ProductChatDto
                        {
                            Id = p.Productid,
                            Name = p.Productname ?? "",
                            Price = p.Price,
                            Category = p.Category?.Categoryname,
                            Description = p.Description
                        });
                        return new { products = dtos };
                    }

                    case "get_product_detail":
                    {
                        var id = args.GetProperty("product_id").GetInt32();
                        var product = await _productRepository.GetProductByIdAsync(id);
                        if (product == null)
                            return new { error = "Không tìm thấy sản phẩm." };

                        return new
                        {
                            product = new ProductChatDto
                            {
                                Id = product.Productid,
                                Name = product.Productname ?? "",
                                Price = product.Price,
                                Category = product.Category?.Categoryname,
                                Description = product.Description
                            }
                        };
                    }

                    case "compare_products":
                    {
                        var ids = args.GetProperty("product_ids")
                            .EnumerateArray()
                            .Select(x => x.GetInt32())
                            .ToList();

                        var products = await _productRepository.GetByIdsAsync(ids);
                        var dtos = products.Select(p => new ProductChatDto
                        {
                            Id = p.Productid,
                            Name = p.Productname ?? "",
                            Price = p.Price,
                            Category = p.Category?.Categoryname,
                            Description = p.Description
                        });
                        return new { products = dtos };
                    }

                    default:
                        return new { error = $"Function '{functionName}' không tồn tại." };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionName}", functionName);
                return new { error = $"Lỗi khi thực thi: {ex.Message}" };
            }
        }
    }
}