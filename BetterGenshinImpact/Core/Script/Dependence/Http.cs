using BetterGenshinImpact.Core.Script.Utils;
using System;
using System.IO;
using System.Threading.Tasks;
using OpenCvSharp;
using System.Linq;
using System.Text.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.Extensions.Logging;

namespace BetterGenshinImpact.Core.Script.Dependence;

public partial class Http
{
    private readonly ILogger _logger;
    private readonly Action<string> checkPermission;
    private readonly Func<string, string, string?, Dictionary<string, string>, Task<HttpReponse>>? send;

    public Http(Action<string> checkPermission,
        Func<string, string, string?, Dictionary<string, string>, Task<HttpReponse>>? send = null,
        ILogger? logger = null)
    {
        this.checkPermission = checkPermission ?? throw new ArgumentNullException(nameof(checkPermission));
        this.send = send;
        _logger = logger ?? Microsoft.Extensions.Logging.Abstractions.NullLogger.Instance;
    }

    public class HttpReponse
    {
        public int status_code { get; set; }
        public Dictionary<string, string> headers { get; set; } = new();
        public string body { get; set; } = "";
    }


    /// <summary>
    /// 执行HTTP请求
    /// </summary>
    /// <param name="method">HTTP方法</param>
    /// <param name="url">请求URL</param>
    /// <param name="body">请求体</param>
    /// <param name="headersJson">请求头，JSON格式</param>
    /// <returns></returns>
    public async Task<HttpReponse> Request(string method, string url, string? body = null, string? headersJson = null)
    {
        _logger.LogDebug($"[HTTP] 发送HTTP请求: {method} {url} Body: {(body != null ? body : "null")} Headers: {(headersJson != null ? headersJson : "null")}");
        checkPermission(url);

        var dictHeaders = new Dictionary<string, string>();
        if (!string.IsNullOrWhiteSpace(headersJson))
        {
            try
            {
                var headers = JsonSerializer.Deserialize<Dictionary<string, string>>(headersJson);
                if (headers != null)
                {
                    dictHeaders = headers;
                }
            }
            catch (JsonException)
            {
                throw new ArgumentException("Headers JSON格式错误");
            }
        }

        // header全部小写
        dictHeaders = dictHeaders.ToDictionary(kvp => kvp.Key.ToLowerInvariant(), kvp => kvp.Value);

        // The embedding host may enforce its network policy and cancellation.
        if (send != null) return await send(method, url, body, dictHeaders);

        // 提前取出来Content-Type，防止被覆盖
        string contentType = "application/json";
        if (dictHeaders.TryGetValue("content-type", out var ct))
        {
            contentType = ct;
            dictHeaders.Remove("content-type");
        }

        // 使用HttpClient发送请求
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Clear();
        foreach (var header in dictHeaders)
        {
            httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
        }

        var content = body == null ? null : new StringContent(body, Encoding.UTF8, contentType);
        var response = await httpClient.SendAsync(new HttpRequestMessage(new HttpMethod(method), url) { Content = content });

        var responseCode = (int)response.StatusCode;
        var responseHeaders = response.Headers.ToDictionary(h => h.Key, h => h.Value.First()); // 只取第一个值
        var responseBody = await response.Content.ReadAsStringAsync();
        return new HttpReponse
        {
            status_code = responseCode,
            headers = responseHeaders,
            body = responseBody,
        };
    }
}
