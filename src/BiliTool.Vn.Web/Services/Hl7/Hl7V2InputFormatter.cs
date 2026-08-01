using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;

namespace BiliTool.Vn.Web.Services.Hl7;

public sealed class Hl7V2InputFormatter : TextInputFormatter
{
    public Hl7V2InputFormatter()
    {
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("application/hl7-v2"));
        SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/plain"));
        SupportedEncodings.Add(System.Text.Encoding.UTF8);
    }

    public override async Task<InputFormatterResult> ReadRequestBodyAsync(
        InputFormatterContext context,
        System.Text.Encoding encoding)
    {
        using var reader = new StreamReader(context.HttpContext.Request.Body, encoding);
        return await InputFormatterResult.SuccessAsync(await reader.ReadToEndAsync());
    }
}
