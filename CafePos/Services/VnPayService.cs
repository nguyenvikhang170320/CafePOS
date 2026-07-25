using CafePos.Models;
using CafePos.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;

namespace CafePos.Services
{
    // 1. Giao diện (Interface) định nghĩa khuôn mẫu
    public interface IVnPayService
    {
        string CreatePaymentUrl(VnPayPaymentRequest model, HttpContext context);
        VnPayPaymentResponse PaymentExecute(IQueryCollection collections);
    }

    // 2. Lớp (Class) triển khai logic chi tiết
    public class VnPayService : IVnPayService
    {
        private readonly IConfiguration _configuration;

        public VnPayService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string CreatePaymentUrl(VnPayPaymentRequest model, HttpContext context)
        {
            var tmnCode = _configuration["VnPayer:TmnCode"] ?? _configuration["VnPay:TmnCode"];
            var hashSecret = _configuration["VnPayer:HashSecret"] ?? _configuration["VnPay:HashSecret"];
            var baseUrl = _configuration["VnPayer:BaseUrl"] ?? _configuration["VnPay:BaseUrl"] ?? "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
            var returnUrl = _configuration["VnPayer:ReturnUrl"] ?? _configuration["VnPay:ReturnUrl"];

            var vnPayData = new SortedDictionary<string, string>(StringComparer.Ordinal)
            {
                ["vnp_Version"] = "2.1.0",
                ["vnp_Command"] = "pay",
                ["vnp_TmnCode"] = tmnCode!,
                ["vnp_Amount"] = ((long)(model.Amount * 100)).ToString(), // VNPAY nhân 100 bỏ thập phân
                ["vnp_CreateDate"] = DateTime.Now.ToString("yyyyMMddHHmmss"),
                ["vnp_CurrCode"] = "VND",
                ["vnp_IpAddr"] = GetIpAddress(context),
                ["vnp_Locale"] = string.IsNullOrEmpty(model.Language) ? "vn" : model.Language,
                ["vnp_OrderInfo"] = model.OrderInfo,
                ["vnp_OrderType"] = "other",
                ["vnp_ReturnUrl"] = returnUrl!,
                ["vnp_TxnRef"] = model.OrderId.ToString() // Lưu OrderId để Callback dễ parse
            };

            if (!string.IsNullOrWhiteSpace(model.BankCode))
            {
                vnPayData.Add("vnp_BankCode", model.BankCode);
            }

            var rawData = string.Join("&", vnPayData
                .Where(kvp => !string.IsNullOrEmpty(kvp.Value))
                .Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

            var secureHash = HmacSha512(hashSecret!, rawData);
            return $"{baseUrl}?{rawData}&vnp_SecureHash={secureHash}";
        }

        public VnPayPaymentResponse PaymentExecute(IQueryCollection collections)
        {
            var hashSecret = _configuration["VnPayer:HashSecret"] ?? _configuration["VnPay:HashSecret"];
            var responseData = collections.ToDictionary(x => x.Key, x => x.Value.ToString());

            if (!responseData.ContainsKey("vnp_SecureHash"))
            {
                return new VnPayPaymentResponse { Success = false };
            }

            var vnpSecureHash = responseData["vnp_SecureHash"];
            responseData.Remove("vnp_SecureHash");
            responseData.Remove("vnp_SecureHashType");

            var sortedData = new SortedDictionary<string, string>(responseData, StringComparer.Ordinal);
            var rawData = string.Join("&", sortedData
                .Where(x => !string.IsNullOrEmpty(x.Value) && x.Key.StartsWith("vnp_"))
                .Select(kvp => $"{WebUtility.UrlEncode(kvp.Key)}={WebUtility.UrlEncode(kvp.Value)}"));

            var checkHash = HmacSha512(hashSecret!, rawData);
            bool isValidSignature = checkHash.Equals(vnpSecureHash, StringComparison.InvariantCultureIgnoreCase);

            if (!isValidSignature)
            {
                return new VnPayPaymentResponse { Success = false };
            }

            decimal amount = 0;
            if (sortedData.TryGetValue("vnp_Amount", out var rawAmount))
            {
                decimal.TryParse(rawAmount, out amount);
                amount = amount / 100;
            }

            return new VnPayPaymentResponse
            {
                Success = true,
                OrderCode = sortedData.GetValueOrDefault("vnp_TxnRef") ?? "",
                TransactionNo = sortedData.GetValueOrDefault("vnp_TransactionNo"),
                ResponseCode = sortedData.GetValueOrDefault("vnp_ResponseCode"),
                TransactionStatus = sortedData.GetValueOrDefault("vnp_TransactionStatus"),
                OrderInfo = sortedData.GetValueOrDefault("vnp_OrderInfo"),
                Amount = amount
            };
        }

        private static string HmacSha512(string key, string inputData)
        {
            var keyBytes = Encoding.UTF8.GetBytes(key);
            var inputBytes = Encoding.UTF8.GetBytes(inputData);
            using var hmac = new HMACSHA512(keyBytes);
            var hashValue = hmac.ComputeHash(inputBytes);
            return BitConverter.ToString(hashValue).Replace("-", "").ToLower();
        }

        private static string GetIpAddress(HttpContext context)
        {
            var ipAddress = context.Connection.RemoteIpAddress?.ToString();
            return string.IsNullOrEmpty(ipAddress) ? "127.0.0.1" : ipAddress;
        }
    }
}