using Microsoft.Extensions.Logging;
using STranslate.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace STranslate.Plugin.Translate.YoudaoWeb;

public class Main : TranslatePluginBase
{
    private const string YoudaoTranslateUrl = "https://fanyi.youdao.com";
    private const string YoudaoDictUrl = "https://dict.youdao.com";
    private const string Client = "fanyideskweb";
    private const string Product = "webfanyi";
    private const string AppVersion = "1.0.0";
    private const string Vendor = "web";
    private const string PointParam = "client,mysticTime,product";
    private const string KeyFrom = "fanyi.web";
    private const string DefaultKey = "asdjnjfenknafdfsdfsd";
    private const string UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/124.0.0.0 Safari/537.36";
    private const string Cookie = "OUTFOX_SEARCH_USER_ID=1796239350@10.110.96.157;";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private Control? _settingUi;
    private IPluginContext Context { get; set; } = null!;

    public override void Init(IPluginContext context) => Context = context;

    public override Control GetSettingUI()
    {
        _settingUi ??= new UserControl
        {
            Padding = new Thickness(12),
            Content = new TextBlock
            {
                Text = "本插件无需配置有道官方 API Key 或 Secret。\n\n" +
                       "说明：本插件调用的是有道网页端翻译接口，而非有道智云官方付费 OpenAPI。\n\n" +
                       "免责声明：网页接口属于非公开接入方式，可能随时变更、失效、限流或返回异常结果。使用本插件所产生的可用性、兼容性或服务条款相关风险，请自行评估并承担。\n\n" +
                       "侵权与权利声明：本插件为非官方第三方实现，与有道及其关联方不存在授权、认可或合作关系；“有道”相关名称、商标及服务权利归其权利人所有。使用者应自行确认其使用方式符合相关法律法规、平台规则及有道服务条款；如权利人明确禁止、要求停止，或你无法确认使用合法性，请立即停止使用。",
                TextWrapping = TextWrapping.Wrap
            }
        };
        return _settingUi;
    }

    public override string? GetSourceLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.Auto => "auto",
        LangEnum.ChineseSimplified => "zh-CHS",
        LangEnum.ChineseTraditional => "zh-CHT",
        LangEnum.English => "en",
        LangEnum.Japanese => "ja",
        LangEnum.Korean => "ko",
        LangEnum.French => "fr",
        LangEnum.Spanish => "es",
        LangEnum.PortuguesePortugal => "pt",
        LangEnum.PortugueseBrazil => "pt",
        LangEnum.Italian => "it",
        LangEnum.German => "de",
        LangEnum.Russian => "ru",
        LangEnum.Arabic => "ar",
        LangEnum.Thai => "th",
        LangEnum.Dutch => "nl",
        LangEnum.Indonesian => "id",
        LangEnum.Vietnamese => "vi",
        _ => null
    };

    public override string? GetTargetLanguage(LangEnum langEnum) => langEnum switch
    {
        LangEnum.ChineseSimplified => "zh-CHS",
        LangEnum.ChineseTraditional => "zh-CHT",
        LangEnum.English => "en",
        LangEnum.Japanese => "ja",
        LangEnum.Korean => "ko",
        LangEnum.French => "fr",
        LangEnum.Spanish => "es",
        LangEnum.PortuguesePortugal => "pt",
        LangEnum.PortugueseBrazil => "pt",
        LangEnum.Italian => "it",
        LangEnum.German => "de",
        LangEnum.Russian => "ru",
        LangEnum.Arabic => "ar",
        LangEnum.Thai => "th",
        LangEnum.Dutch => "nl",
        LangEnum.Indonesian => "id",
        LangEnum.Vietnamese => "vi",
        _ => null
    };

    public override void Dispose()
    {
    }

    public override async Task TranslateAsync(TranslateRequest request, TranslateResult result, CancellationToken cancellationToken = default)
    {
        try
        {
            if (GetSourceLanguage(request.SourceLang) is not string source)
            {
                result.Fail(Context.GetTranslation("UnsupportedSourceLang"));
                return;
            }

            if (GetTargetLanguage(request.TargetLang) is not string target)
            {
                result.Fail(Context.GetTranslation("UnsupportedTargetLang"));
                return;
            }

            var keyData = await GetKeyDataAsync(cancellationToken);
            var encryptedResponse = await RequestTranslationAsync(request.Text, source, target, keyData.SecretKey, cancellationToken);
            var json = DecryptTranslation(encryptedResponse, keyData.AesKey, keyData.AesIv);
            var translatedText = ParseTranslatedText(json);

            result.Success(translatedText);
        }
        catch (Exception ex)
        {
            Context.Logger.LogError(ex, "Youdao Web translate failed.");
            result.Fail(ex.Message);
        }
    }

    private async Task<KeyData> GetKeyDataAsync(CancellationToken cancellationToken)
    {
        var mysticTime = GetUnixTimeMilliseconds();
        var sign = Md5Hex($"client={Client}&mysticTime={mysticTime}&product={Product}&key={DefaultKey}");

        var options = new Options
        {
            Headers = CreateHeaders(),
            QueryParams = new Dictionary<string, string>
            {
                { "client", Client },
                { "product", Product },
                { "appVersion", AppVersion },
                { "vendor", Vendor },
                { "pointParam", PointParam },
                { "keyfrom", KeyFrom },
                { "keyid", "webfanyi-key-getter" },
                { "mysticTime", mysticTime },
                { "sign", sign }
            }
        };

        var response = await Context.HttpService.GetAsync($"{YoudaoDictUrl}/webtranslate/key", options, cancellationToken);
        var parsed = JsonSerializer.Deserialize<KeyResponse>(response, JsonOptions);

        if (parsed?.Code != 0 || parsed.Data is null)
        {
            throw new InvalidOperationException($"Failed to get Youdao Web key. Raw: {response}");
        }

        if (string.IsNullOrWhiteSpace(parsed.Data.SecretKey) ||
            string.IsNullOrWhiteSpace(parsed.Data.AesKey) ||
            string.IsNullOrWhiteSpace(parsed.Data.AesIv))
        {
            throw new InvalidOperationException($"Youdao Web key response is incomplete. Raw: {response}");
        }

        return parsed.Data;
    }

    private async Task<string> RequestTranslationAsync(string text, string source, string target, string secretKey, CancellationToken cancellationToken)
    {
        var mysticTime = GetUnixTimeMilliseconds();
        var sign = Md5Hex($"client={Client}&mysticTime={mysticTime}&product={Product}&key={secretKey}");

        var formData = new Dictionary<string, string>
        {
            { "i", text },
            { "from", source },
            { "to", target },
            { "dictResult", "false" },
            { "client", Client },
            { "product", Product },
            { "appVersion", AppVersion },
            { "vendor", Vendor },
            { "pointParam", PointParam },
            { "keyfrom", KeyFrom },
            { "keyid", "webfanyi" },
            { "mysticTime", mysticTime },
            { "sign", sign }
        };

        return await Context.HttpService.PostFormAsync($"{YoudaoDictUrl}/webtranslate", formData, CreateRequestOptions(), cancellationToken);
    }

    private static string DecryptTranslation(string encryptedText, string aesKey, string aesIv)
    {
        var encryptedBytes = Convert.FromBase64String(NormalizeBase64(encryptedText));

        using var aes = Aes.Create();
        aes.Key = MD5.HashData(Encoding.UTF8.GetBytes(aesKey));
        aes.IV = MD5.HashData(Encoding.UTF8.GetBytes(aesIv));
        aes.Mode = CipherMode.CBC;
        aes.Padding = PaddingMode.PKCS7;

        using var decryptor = aes.CreateDecryptor();
        var decryptedBytes = decryptor.TransformFinalBlock(encryptedBytes, 0, encryptedBytes.Length);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static string ParseTranslatedText(string json)
    {
        var parsed = JsonSerializer.Deserialize<TranslateResponse>(json, JsonOptions);
        var text = parsed?.TranslateResult?
            .SelectMany(group => group)
            .Select(item => item.Tgt)
            .Where(tgt => !string.IsNullOrEmpty(tgt))
            .ToList();

        if (text is not { Count: > 0 })
        {
            throw new InvalidOperationException($"No translation result. Raw: {json}");
        }

        return string.Join(Environment.NewLine, text);
    }

    private static Options CreateRequestOptions() => new()
    {
        Headers = CreateHeaders()
    };

    private static Dictionary<string, string> CreateHeaders() => new()
    {
        { "User-Agent", UserAgent },
        { "Referer", YoudaoTranslateUrl },
        { "Cookie", Cookie }
    };

    private static string GetUnixTimeMilliseconds() =>
        DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();

    private static string Md5Hex(string value) =>
        Convert.ToHexString(MD5.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();

    private static string NormalizeBase64(string value)
    {
        var normalized = value.Trim().Replace('-', '+').Replace('_', '/');
        var padding = normalized.Length % 4;
        return padding == 0 ? normalized : normalized.PadRight(normalized.Length + 4 - padding, '=');
    }

    private sealed class KeyResponse
    {
        [JsonPropertyName("data")]
        public KeyData? Data { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }

        [JsonPropertyName("msg")]
        public string? Message { get; set; }
    }

    private sealed class KeyData
    {
        [JsonPropertyName("secretKey")]
        public string SecretKey { get; set; } = string.Empty;

        [JsonPropertyName("aesKey")]
        public string AesKey { get; set; } = string.Empty;

        [JsonPropertyName("aesIv")]
        public string AesIv { get; set; } = string.Empty;
    }

    private sealed class TranslateResponse
    {
        [JsonPropertyName("translateResult")]
        public List<List<TranslateItem>>? TranslateResult { get; set; }

        [JsonPropertyName("type")]
        public string? Type { get; set; }

        [JsonPropertyName("code")]
        public int Code { get; set; }
    }

    private sealed class TranslateItem
    {
        [JsonPropertyName("src")]
        public string? Src { get; set; }

        [JsonPropertyName("tgt")]
        public string? Tgt { get; set; }
    }
}
