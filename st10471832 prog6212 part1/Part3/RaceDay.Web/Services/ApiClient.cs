using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public T? Data { get; init; }
    public string? Error { get; init; }
    public bool Unauthorized => StatusCode == 401;
}

public class ApiClient
{
    private const string ApiCookieKey = "ApiCookie";
    private const string ApiCookieName = ".RaceDay.Session";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly IHttpContextAccessor _accessor;

    public ApiClient(HttpClient http, IHttpContextAccessor accessor)
    {
        _http = http;
        _accessor = accessor;
    }

    private ISession Session => _accessor.HttpContext!.Session;

    public Task<ApiResult<T>> GetAsync<T>(string url) => SendAsync<T>(HttpMethod.Get, url);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body = null) => SendAsync<T>(HttpMethod.Post, url, body);

    public Task<ApiResult<T>> PutAsync<T>(string url, object? body) => SendAsync<T>(HttpMethod.Put, url, body);

    public Task<ApiResult<bool>> DeleteAsync(string url) => SendAsync<bool>(HttpMethod.Delete, url);

    public async Task<ApiResult<T>> SendAsync<T>(HttpMethod method, string url, object? body = null)
    {
        try
        {
            using var request = new HttpRequestMessage(method, url);

            var cookie = Session.GetString(ApiCookieKey);
            if (!string.IsNullOrEmpty(cookie))
                request.Headers.Add("Cookie", cookie);

            if (body != null)
                request.Content = JsonContent.Create(body);

            using var response = await _http.SendAsync(request);
            CaptureApiCookie(response);
            return await ReadAsync<T>(response);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = 0,
                Error = "The RaceDay service is not reachable right now. Please try again shortly."
            };
        }
    }

    public async Task<ApiResult<T>> UploadAsync<T>(string url, IFormFile file)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, url);

            var cookie = Session.GetString(ApiCookieKey);
            if (!string.IsNullOrEmpty(cookie))
                request.Headers.Add("Cookie", cookie);

            using var content = new MultipartFormDataContent();
            await using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            var contentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
            fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
            content.Add(fileContent, "file", file.FileName);
            request.Content = content;

            using var response = await _http.SendAsync(request);
            CaptureApiCookie(response);
            return await ReadAsync<T>(response);
        }
        catch (HttpRequestException)
        {
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = 0,
                Error = "The RaceDay service is not reachable right now. Please try again shortly."
            };
        }
    }
    private void CaptureApiCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var values))
            return;

        foreach (var value in values)
        {
            if (value.StartsWith(ApiCookieName + "=", StringComparison.Ordinal))
            {
                Session.SetString(ApiCookieKey, value.Split(';')[0]);
                return;
            }
        }
    }

    private static async Task<ApiResult<T>> ReadAsync<T>(HttpResponseMessage response)
    {
        var status = (int)response.StatusCode;

        if (response.IsSuccessStatusCode)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
                return new ApiResult<T> { Success = true, StatusCode = status };

            var data = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
            return new ApiResult<T> { Success = true, StatusCode = status, Data = data };
        }

        var text = await response.Content.ReadAsStringAsync();
        return new ApiResult<T> { Success = false, StatusCode = status, Error = ExtractError(text, status) };
    }

    private static string ExtractError(string body, int status)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            if (root.ValueKind == JsonValueKind.Object)
            {
                if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    return message.GetString()!;

                if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
                {
                    var messages = errors.EnumerateObject()
                        .Where(p => p.Value.ValueKind == JsonValueKind.Array)
                        .SelectMany(p => p.Value.EnumerateArray().Select(e => e.GetString()))
                        .Where(m => !string.IsNullOrWhiteSpace(m));

                    var joined = string.Join(" ", messages);
                    if (joined.Length > 0)
                        return joined;
                }
            }
        }
        catch (JsonException)
        {
        }

        return status switch
        {
            401 => "Please log in to continue.",
            403 => "You do not have permission to do that.",
            404 => "We could not find what you were looking for.",
            409 => "That could not be done because it conflicts with existing information.",
            _ => "Something went wrong. Please try again."
        };
    }
}