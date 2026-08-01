using System.Text.Json;
using System.Text.Json.Nodes;

namespace BiliTool.Vn.Infrastructure.Services;

internal static class ClinicalAuditPayloadRedactor
{
    private static readonly HashSet<string> SensitiveProperties = new(StringComparer.OrdinalIgnoreCase)
    {
        "name", "hoten", "hotenbenhnhan", "email", "phone", "sodienthoai",
        "address", "diachi", "patientid", "patientidentifier", "mabenhnhan", "googleid", "ip", "diachiip",
        "encounteridentifier", "orderidentifier", "specimenidentifier", "observationidentifier"
    };

    public static string Redact(object value, JsonSerializerOptions options)
    {
        var node = JsonNode.Parse(JsonSerializer.Serialize(value, options));
        RedactNode(node);
        return node?.ToJsonString(options) ?? "null";
    }

    private static void RedactNode(JsonNode? node)
    {
        if (node is JsonObject jsonObject)
        {
            foreach (var property in jsonObject.ToList())
            {
                if (SensitiveProperties.Contains(property.Key))
                {
                    jsonObject[property.Key] = "[REDACTED]";
                    continue;
                }

                RedactNode(property.Value);
            }
        }
        else if (node is JsonArray jsonArray)
        {
            foreach (var item in jsonArray) RedactNode(item);
        }
    }
}
