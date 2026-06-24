using System;
using App2.Services;

namespace App2.Tests
{
    public class LicenseVerificationTest
    {
        // Rename Main to avoid conflicting entry point
        public static void RunTest(string privateKeyXml)
        {
            try 
            {
                string hwid = "TEST-DEVICE-ID-12345";
                DateTime expiry = DateTime.Now.AddDays(30);

                Console.WriteLine($"Generating license for HWID: {hwid}, Expiry: {expiry}");
                string key = LicenseService.GenerateSignedLicense(hwid, expiry, privateKeyXml);
                Console.WriteLine($"Generated Key: {key}");

                Console.WriteLine("\nVerifying with correct HWID...");
                var (isValid, expDate, message) = LicenseService.VerifySignedLicense(key, hwid);
                Console.WriteLine($"Result: IsValid={isValid}, Expiry={expDate}, Message={message}");

                Console.WriteLine("\nVerifying with WRONG HWID...");
                var (isValidWrong, expDateWrong, messageWrong) = LicenseService.VerifySignedLicense(key, "WRONG-ID");
                Console.WriteLine($"Result: IsValid={isValidWrong}, Message={messageWrong}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
