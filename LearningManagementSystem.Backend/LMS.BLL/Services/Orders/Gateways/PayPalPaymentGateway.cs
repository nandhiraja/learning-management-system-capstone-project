using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using LMS.BLL.Interfaces;
using LMS.Core.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace LMS.BLL.Services.Orders.Gateways
{
    public class PayPalPaymentGateway : IPaymentGateway
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<PayPalPaymentGateway> _logger;

        public PayPalPaymentGateway(IConfiguration configuration, ILogger<PayPalPaymentGateway> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public PaymentMethod SupportedMethod => PaymentMethod.PayPal;

        public async Task<(string? TransactionId, string? PaymentUrl)> CreateOrderAsync(int orderId, decimal amount, string currency)
        {
            var clientId = _configuration["PayPal:ClientId"];
            var clientSecret = _configuration["PayPal:ClientSecret"];
            var baseUrl = _configuration["PayPal:BaseUrl"] ?? "https://api-m.sandbox.paypal.com";

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("PayPal configuration is missing. ClientId and ClientSecret are required.");
            }

            using var client = new HttpClient();
            var accessToken = await GetPayPalAccessTokenAsync(client, clientId, clientSecret, baseUrl);
            if (string.IsNullOrEmpty(accessToken))
            {
                throw new InvalidOperationException("Failed to obtain PayPal access token.");
            }

            var returnUrl = _configuration["PayPal:ReturnUrl"] ?? "https://localhost:7165/api/payments/verify";
            var cancelUrl = _configuration["PayPal:CancelUrl"] ?? "https://localhost:7165/api/payments/cancel";
            var (paypalOrderId, approveUrl) = await CreatePayPalOrderAsync(client, amount, currency, accessToken, baseUrl, returnUrl, cancelUrl);

            if (string.IsNullOrEmpty(paypalOrderId) || string.IsNullOrEmpty(approveUrl))
            {
                throw new InvalidOperationException("Failed to initiate PayPal payment order session.");
            }

            return (paypalOrderId, approveUrl);
        }

        public async Task<bool> VerifyPaymentAsync(string transactionId, string status, object rawPayload)
        {
            var clientId = _configuration["PayPal:ClientId"];
            var clientSecret = _configuration["PayPal:ClientSecret"];
            var baseUrl = _configuration["PayPal:BaseUrl"] ?? "https://api-m.sandbox.paypal.com";

            if (string.IsNullOrEmpty(clientId) || string.IsNullOrEmpty(clientSecret))
            {
                throw new InvalidOperationException("PayPal configuration is missing. ClientId and ClientSecret are required.");
            }

            using var client = new HttpClient();
            var accessToken = await GetPayPalAccessTokenAsync(client, clientId, clientSecret, baseUrl);
            if (string.IsNullOrEmpty(accessToken))
            {
                _logger.LogWarning("Failed to obtain PayPal access token for payment verification.");
                return false;
            }

            return await CapturePayPalOrderAsync(client, transactionId, accessToken, baseUrl);
        }

        #region PayPal API Helpers

        private async Task<string?> GetPayPalAccessTokenAsync(HttpClient client, string clientId, string clientSecret, string baseUrl)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v1/oauth2/token");
                var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
                request.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var collection = new List<KeyValuePair<string, string>>
                {
                    new KeyValuePair<string, string>("grant_type", "client_credentials")
                };
                request.Content = new FormUrlEncodedContent(collection);

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"PayPal Token API returned non-success: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("access_token", out var tokenProp))
                {
                    return tokenProp.GetString();
                }
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to retrieve PayPal access token.");
                return null;
            }
        }

        private async Task<(string? OrderId, string? ApproveUrl)> CreatePayPalOrderAsync(HttpClient client, decimal amount, string currency, string accessToken, string baseUrl, string returnUrl, string cancelUrl)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                string formattedAmount = currency == "INR" 
                                     ? ((int)Math.Round(amount)).ToString() // Converts 99.98 to "100" (Whole number for INR sandbox)
                                     : amount.ToString("F2");
                var payload = new
                {
                    intent = "CAPTURE",
                    purchase_units = new[]
                    {
                        new
                        {
                            amount = new
                            {
                                currency_code = currency,
                                value = formattedAmount
                            }
                        }
                    },
                    application_context = new
                    {
                        return_url = returnUrl,
                        cancel_url = cancelUrl
                    }
                };

                request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning($"PayPal Create Order API returned non-success: {response.StatusCode}, details: {responseBody}");
                    return (null, null);
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                string? orderId = root.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
                string? approveUrl = null;

                if (root.TryGetProperty("links", out var linksProp))
                {
                    foreach (var link in linksProp.EnumerateArray())
                    {
                        if (link.TryGetProperty("rel", out var relProp) && relProp.GetString() == "approve")
                        {
                            approveUrl = link.TryGetProperty("href", out var hrefProp) ? hrefProp.GetString() : null;
                            break;
                        }
                    }
                }

                return (orderId, approveUrl);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to create PayPal order.");
                return (null, null);
            }
        }

        private async Task<bool> CapturePayPalOrderAsync(HttpClient client, string paypalOrderId, string accessToken, string baseUrl)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/v2/checkout/orders/{paypalOrderId}/capture");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                request.Content = new StringContent(string.Empty, Encoding.UTF8, "application/json");

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning($"PayPal Capture Order API returned non-success: {response.StatusCode}");
                    return false;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("status", out var statusProp))
                {
                    var status = statusProp.GetString();
                    return status == "COMPLETED";
                }
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Failed to capture PayPal order: {paypalOrderId}");
                return false;
            }
        }

        #endregion
    }
}
