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
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly IProductRepository _productRepository;
        private readonly ILogger<GeminiChatService> _logger;

        public GeminiChatService(
            HttpClient httpClient,
            IConfiguration configuration,
            IProductRepository productRepository,
            ILogger<GeminiChatService> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<ChatResponse> ChatAsync(List<object>? history, string userMessage)
        {
            try
            {
                var apiKey = _configuration["Gemini:ApiKey"];
                var model = _configuration["Gemini:Model"] ?? "gemini-2.0-flash";

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    return new ChatResponse
                    {
                        Reply = "Xin lỗi, hệ thống chatbot chưa được cấu hình đúng. Vui lòng liên hệ quản trị viên."
                    };
                }

                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent?key={apiKey}";

                // Build conversation history
                var contents = new List<object>();

                // Add current user message
                contents.Add(new
                {
                    role = "user",
                    parts = new[] { new { text = userMessage } }
                });

                // Prepare request payload with function declarations
                var requestBody = new
                {
                    system_instruction = new
                    {
                        parts = new[]
                        {
                            new
                            {
                                text = @"Bạn là trợ lý tư vấn bán hàng thông minh cho TetGift - cửa hàng quà Tết cao cấp.
Nhiệm vụ của bạn:
- Trả lời bằng tiếng Việt lưu loát, thân thiện và chuyên nghiệp
- Chỉ tư vấn dựa trên sản phẩm thực tế có trong database
- Sử dụng các tools để tra cứu thông tin sản phẩm khi cần thiết
- Nếu không tìm thấy sản phẩm phù hợp, hãy nói thật và gợi ý các lựa chọn khác
- KHÔNG bịa đặt thông tin sản phẩm không tồn tại
- Tư vấn nhiệt tình, giúp khách hàng tìm quà Tết ý nghĩa

Khi khách hỏi về sản phẩm:
1. Dùng search_products để tìm kiếm
2. Dùng get_product_detail để xem chi tiết 1 sản phẩm
3. Dùng compare_products để so sánh nhiều sản phẩm

Hãy tư vấn như một nhân viên bán hàng chuyên nghiệp!"
                            }
                        }
                    },
                    contents,
                    tools = new object[]
                    {
                        new
                        {
                            function_declarations = new object[]
                            {
                                new
                                {
                                    name = "search_products",
                                    description = "Tìm kiếm sản phẩm theo từ khóa, danh mục hoặc mức giá tối đa. Trả về danh sách tối đa 10 sản phẩm phù hợp.",
                                    parameters = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            keyword = new
                                            {
                                                type = "string",
                                                description = "Từ khóa tìm kiếm trong tên sản phẩm (ví dụ: 'bánh', 'kẹo', 'nước')"
                                            },
                                            category = new
                                            {
                                                type = "string",
                                                description = "Tên danh mục sản phẩm (ví dụ: 'Bánh ngọt', 'Kẹo mứt', 'Nước uống')"
                                            },
                                            max_price = new
                                            {
                                                type = "number",
                                                description = "Giá tối đa (VNĐ)"
                                            }
                                        }
                                    }
                                },
                                new
                                {
                                    name = "get_product_detail",
                                    description = "Lấy thông tin chi tiết của một sản phẩm cụ thể theo ID",
                                    parameters = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            product_id = new
                                            {
                                                type = "integer",
                                                description = "ID của sản phẩm cần xem chi tiết"
                                            }
                                        },
                                        required = new[] { "product_id" }
                                    }
                                },
                                new
                                {
                                    name = "compare_products",
                                    description = "So sánh nhiều sản phẩm với nhau dựa trên danh sách ID",
                                    parameters = new
                                    {
                                        type = "object",
                                        properties = new
                                        {
                                            product_ids = new
                                            {
                                                type = "array",
                                                description = "Danh sách ID các sản phẩm cần so sánh",
                                                items = new { type = "integer" }
                                            }
                                        },
                                        required = new[] { "product_ids" }
                                    }
                                }
                            }
                        }
                    }
                };

                // Agentic loop: handle function calling
                var maxIterations = 5;
                var iteration = 0;

                while (iteration < maxIterations)
                {
                    iteration++;

                    var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                    });

                    _logger.LogInformation("Sending request to Gemini (iteration {Iteration})", iteration);

                    var content = new StringContent(json, Encoding.UTF8, "application/json");
                    var response = await _httpClient.PostAsync(url, content);
                    var responseString = await response.Content.ReadAsStringAsync();

                    if (!response.IsSuccessStatusCode)
                    {
                        if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                        {
                            _logger.LogWarning("Rate limit exceeded. Response: {Response}", responseString);
                            
                            // Parse retry delay from response if available
                            var errorResponse = JsonSerializer.Deserialize<JsonElement>(responseString);
                            if (errorResponse.TryGetProperty("error", out var error) &&
                                error.TryGetProperty("details", out var details))
                            {
                                foreach (var detail in details.EnumerateArray())
                                {
                                    if (detail.TryGetProperty("@type", out var type) &&
                                        type.GetString() == "type.googleapis.com/google.rpc.RetryInfo" &&
                                        detail.TryGetProperty("retryDelay", out var retryDelay))
                                    {
                                        _logger.LogInformation("Suggested retry delay: {RetryDelay}", retryDelay.GetString());
                                    }
                                }
                            }
                            
                            return new ChatResponse
                            {
                                Reply = "Xin lỗi, hệ thống AI tạm thời quá tải. Vui lòng thử lại sau vài phút hoặc liên hệ quản trị viên."
                            };
                        }

                        _logger.LogError("Gemini API error. Status: {StatusCode}, Response: {Response}",
                            response.StatusCode, responseString);

                        return new ChatResponse
                        {
                            Reply = "Xin lỗi, đã có lỗi khi kết nối với trợ lý AI. Vui lòng thử lại sau."
                        };
                    }

                    var geminiResponse = JsonSerializer.Deserialize<JsonElement>(responseString);

                    if (!geminiResponse.TryGetProperty("candidates", out var candidates) ||
                        candidates.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("No candidates in response");
                        return new ChatResponse
                        {
                            Reply = "Xin lỗi, tôi không thể trả lời câu hỏi này lúc này."
                        };
                    }

                    var candidate = candidates[0];

                    if (!candidate.TryGetProperty("content", out var candidateContent))
                    {
                        _logger.LogWarning("No content in candidate");
                        return new ChatResponse
                        {
                            Reply = "Xin lỗi, tôi không thể xử lý yêu cầu này."
                        };
                    }

                    if (!candidateContent.TryGetProperty("parts", out var parts) ||
                        parts.GetArrayLength() == 0)
                    {
                        _logger.LogWarning("No parts in content");
                        return new ChatResponse
                        {
                            Reply = "Xin lỗi, tôi không thể xử lý yêu cầu này."
                        };
                    }

                    var finishReason = candidate.TryGetProperty("finishReason", out var fr)
                        ? fr.GetString()
                        : null;

                    // Check if it's a final answer
                    if (finishReason == "STOP")
                    {
                        var textPart = parts[0];
                        if (textPart.TryGetProperty("text", out var textElement))
                        {
                            var reply = textElement.GetString() ?? "Xin lỗi, tôi không có câu trả lời.";
                            _logger.LogInformation("Final response received");
                            return new ChatResponse
                            {
                                Reply = reply
                            };
                        }
                    }

                    // Check for function call
                    var firstPart = parts[0];
                    if (firstPart.TryGetProperty("functionCall", out var functionCall))
                    {
                        var functionName = functionCall.GetProperty("name").GetString();
                        var args = functionCall.GetProperty("args");

                        _logger.LogInformation("Function call detected: {FunctionName}", functionName);

                        // Execute the function
                        var functionResult = await ExecuteFunctionAsync(functionName!, args);

                        // Deserialize args to Dictionary for proper serialization
                        var argsDict = new Dictionary<string, object?>();
                        foreach (var prop in args.EnumerateObject())
                        {
                            argsDict[prop.Name] = prop.Value.ValueKind switch
                            {
                                JsonValueKind.String => prop.Value.GetString(),
                                JsonValueKind.Number => prop.Value.GetDecimal(),
                                JsonValueKind.True => true,
                                JsonValueKind.False => false,
                                JsonValueKind.Null => null,
                                JsonValueKind.Array => prop.Value.EnumerateArray()
                                    .Select(x => x.ValueKind == JsonValueKind.Number ? (object)x.GetInt32() : x.GetString())
                                    .ToList(),
                                _ => prop.Value.GetRawText()
                            };
                        }

                        // Add model's function call to contents
                        contents.Add(new
                        {
                            role = "model",
                            parts = new[]
                            {
                                new
                                {
                                    functionCall = new
                                    {
                                        name = functionName,
                                        args = argsDict
                                    }
                                }
                            }
                        });

                        // Add function response to contents
                        contents.Add(new
                        {
                            role = "function",
                            parts = new[]
                            {
                                new
                                {
                                    functionResponse = new
                                    {
                                        name = functionName,
                                        response = functionResult
                                    }
                                }
                            }
                        });

                        // Update request body for next iteration
                        requestBody = new
                        {
                            system_instruction = requestBody.system_instruction,
                            contents,
                            tools = requestBody.tools
                        };

                        continue; // Loop again
                    }

                    // Fallback if no text and no function call
                    _logger.LogWarning("No text or function call in response");
                    return new ChatResponse
                    {
                        Reply = "Xin lỗi, tôi không thể xử lý yêu cầu này."
                    };
                }

                return new ChatResponse
                {
                    Reply = "Xin lỗi, hệ thống đang xử lý quá lâu. Vui lòng thử lại câu hỏi ngắn gọn hơn."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ChatAsync: {Message}", ex.Message);
                return new ChatResponse
                {
                    Reply = "Xin lỗi, đã có lỗi xảy ra. Vui lòng thử lại sau."
                };
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
                            decimal? maxPrice = null;
                            if (args.TryGetProperty("max_price", out var mp) && mp.ValueKind == JsonValueKind.Number)
                            {
                                maxPrice = mp.GetDecimal();
                            }

                            _logger.LogInformation("Searching products: keyword={Keyword}, category={Category}, maxPrice={MaxPrice}",
                                keyword, category, maxPrice);

                            // Using GetAllActiveProductsAsync and filtering in-memory
                            var allProducts = await _productRepository.GetAllActiveProductsAsync();
                            var products = allProducts
                                .Where(p =>
                                    (string.IsNullOrEmpty(keyword) || (p.Productname?.Contains(keyword, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                                    (string.IsNullOrEmpty(category) || (p.Category?.Categoryname?.Equals(category, StringComparison.OrdinalIgnoreCase) ?? false)) &&
                                    (!maxPrice.HasValue || p.Price <= maxPrice.Value)
                                )
                                .Take(10)
                                .ToList();

                            var productDtos = products.Select(p => new ProductChatDto
                            {
                                Id = p.Productid,
                                Name = p.Productname ?? "Không có tên",
                                Price = p.Price,
                                Category = p.Category?.Categoryname,
                                Description = p.Description,
                                Stock = null
                            }).ToList();

                            _logger.LogInformation("Found {Count} products", productDtos.Count);
                            return new { products = productDtos };
                        }

                    case "get_product_detail":
                        {
                            var productId = args.GetProperty("product_id").GetInt32();
                            _logger.LogInformation("Getting product detail: productId={ProductId}", productId);

                            var product = await _productRepository.GetProductByIdAsync(productId);

                            if (product == null)
                            {
                                _logger.LogWarning("Product not found: {ProductId}", productId);
                                return new { error = "Không tìm thấy sản phẩm" };
                            }

                            var productDto = new ProductChatDto
                            {
                                Id = product.Productid,
                                Name = product.Productname ?? "Không có tên",
                                Price = product.Price,
                                Category = product.Category?.Categoryname,
                                Description = product.Description,
                                Stock = null
                            };

                            return new { product = productDto };
                        }

                    case "compare_products":
                        {
                            var productIds = args.GetProperty("product_ids")
                                .EnumerateArray()
                                .Select(x => x.GetInt32())
                                .ToList();

                            _logger.LogInformation("Comparing products: {ProductIds}", string.Join(", ", productIds));

                            var allProducts = await _productRepository.GetAllActiveProductsAsync();
                            var products = allProducts
                                .Where(p => productIds.Contains(p.Productid))
                                .ToList();

                            var productDtos = products.Select(p => new ProductChatDto
                            {
                                Id = p.Productid,
                                Name = p.Productname ?? "Không có tên",
                                Price = p.Price,
                                Category = p.Category?.Categoryname,
                                Description = p.Description,
                                Stock = null
                            }).ToList();

                            _logger.LogInformation("Comparing {Count} products", productDtos.Count);
                            return new { products = productDtos };
                        }

                    default:
                        _logger.LogWarning("Unknown function: {FunctionName}", functionName);
                        return new { error = "Function not found" };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error executing function {FunctionName}: {Message}",
                    functionName, ex.Message);
                return new { error = $"Lỗi khi thực thi function: {ex.Message}" };
            }
        }
    }
}