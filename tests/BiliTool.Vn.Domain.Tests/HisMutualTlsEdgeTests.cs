using System.Net;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BiliTool.Vn.Domain.Tests;

public sealed class HisMutualTlsEdgeTests
{
    [Fact]
    public async Task KestrelHttps_ExposesClientCertificateAndRejectsMissingCertificate()
    {
        using var serverCertificate = CreateCertificate("localhost", false);
        using var clientCertificate = CreateCertificate("his-client", true);
        var expectedFingerprint = Convert.ToHexString(SHA256.HashData(clientCertificate.RawData));
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.ConfigureKestrel(options => options.Listen(IPAddress.Loopback, 0, listen =>
            listen.UseHttps(https =>
            {
                https.ServerCertificate = serverCertificate;
                https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
                https.ClientCertificateValidation = (_, _, _) => true;
            })));
        var app = builder.Build();
        app.MapGet("/mtls", async context =>
        {
            var certificate = await context.Connection.GetClientCertificateAsync();
            if (certificate == null)
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }
            await context.Response.WriteAsync(Convert.ToHexString(SHA256.HashData(certificate.RawData)));
        });
        await app.StartAsync();

        try
        {
            var address = app.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            using var anonymousClient = CreateClient();
            using var mtlsClient = CreateClient(clientCertificate);

            var anonymousResponse = await anonymousClient.GetAsync($"{address}/mtls");
            var mtlsResponse = await mtlsClient.GetAsync($"{address}/mtls");

            Assert.Equal(HttpStatusCode.Unauthorized, anonymousResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, mtlsResponse.StatusCode);
            Assert.Equal(expectedFingerprint, await mtlsResponse.Content.ReadAsStringAsync());
        }
        finally
        {
            await app.StopAsync();
        }
    }

    private static HttpClient CreateClient(X509Certificate2? certificate = null)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        if (certificate != null) handler.ClientCertificates.Add(certificate);
        return new HttpClient(handler);
    }

    private static X509Certificate2 CreateCertificate(string commonName, bool clientAuthentication)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={commonName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509BasicConstraintsExtension(false, false, 0, false));
        request.CertificateExtensions.Add(new X509KeyUsageExtension(X509KeyUsageFlags.DigitalSignature, false));
        request.CertificateExtensions.Add(new X509SubjectKeyIdentifierExtension(request.PublicKey, false));
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection
            {
                new(clientAuthentication ? "1.3.6.1.5.5.7.3.2" : "1.3.6.1.5.5.7.3.1")
            },
            false));
        if (!clientAuthentication)
        {
            var san = new SubjectAlternativeNameBuilder();
            san.AddDnsName("localhost");
            san.AddIpAddress(IPAddress.Loopback);
            request.CertificateExtensions.Add(san.Build());
        }
        return request.CreateSelfSigned(DateTimeOffset.UtcNow.AddMinutes(-1), DateTimeOffset.UtcNow.AddHours(1));
    }
}
