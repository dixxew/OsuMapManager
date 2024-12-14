using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json; // Для сериализации/десериализации JSON

public class HttpClientService
{
    private readonly HttpClient _httpClient;

    public HttpClientService()
    {
        _httpClient = new HttpClient();
    }

    /// <summary>
    /// Отправка GET-запроса.
    /// </summary>
    public async Task<T?> GetAsync<T>(string url)
    {
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode(); // Генерирует исключение, если код ответа не успешный (2xx)
        var content = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<T>(content);
    }

    /// <summary>
    /// Отправка POST-запроса с телом в формате JSON.
    /// </summary>
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TResponse>(responseContent);
    }

    /// <summary>
    /// Отправка PUT-запроса с телом в формате JSON.
    /// </summary>
    public async Task<TResponse?> PutAsync<TRequest, TResponse>(string url, TRequest data)
    {
        var json = JsonConvert.SerializeObject(data);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var response = await _httpClient.PutAsync(url, content);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TResponse>(responseContent);
    }

    /// <summary>
    /// Отправка DELETE-запроса.
    /// </summary>
    public async Task DeleteAsync(string url)
    {
        var response = await _httpClient.DeleteAsync(url);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Отправка запроса произвольного типа (например, PATCH).
    /// </summary>
    public async Task<TResponse?> SendCustomRequestAsync<TRequest, TResponse>(HttpMethod method, string url, TRequest? data = null)
    {
        var request = new HttpRequestMessage(method, url);
        if (data != null)
        {
            var json = JsonConvert.SerializeObject(data);
            request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();
        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<TResponse>(responseContent);
    }
}
