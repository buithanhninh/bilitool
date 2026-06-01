using System.Globalization;
using System.Text.Json;

namespace BiliTool.Vn.Web.Services;

public class TranslationService
{
    private readonly Dictionary<string, Dictionary<string, string>> _translations = new();

    public TranslationService(IWebHostEnvironment env)
    {
        var filePath = Path.Combine(env.ContentRootPath, "Resources", "i18n.json");
        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            _translations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json) 
                            ?? new Dictionary<string, Dictionary<string, string>>();
        }
    }

    /// <summary>
    /// Trả về bản dịch dựa trên ngôn ngữ hiện tại.
    /// Mặc định trả về "vi" nếu không tìm thấy "en" hoặc "fr".
    /// </summary>
    public string this[string key]
    {
        get
        {
            var culture = CultureInfo.CurrentCulture.TwoLetterISOLanguageName.ToLower();
            
            // Fallback nếu culture lạ (không phải en/fr/vi) -> dùng "vi"
            if (!_translations.ContainsKey(culture))
            {
                culture = "vi";
            }

            if (_translations.TryGetValue(culture, out var langDict) && langDict.TryGetValue(key, out var val))
            {
                return val;
            }

            // Nếu không có trong ngôn ngữ được chọn, thử lấy từ tiếng việt
            if (culture != "vi" && _translations.TryGetValue("vi", out var viDict) && viDict.TryGetValue(key, out var viVal))
            {
                return viVal; // Fallback to Vietnamese
            }

            // Nếu không có bất cứ gì, trả về chính key
            return key;
        }
    }

    /// <summary>
    /// Thay đổi ngôn ngữ hiện hành (chỉ apply cho thread hiện tại)
    /// Thông thường Blazor tự update qua Middleware dựa trên Accept-Language header.
    /// </summary>
    public void SetCulture(string cultureCode)
    {
        var culture = new CultureInfo(cultureCode);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}
