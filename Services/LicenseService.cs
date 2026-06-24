using App2.Data;
using App2.Models;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace App2.Services
{
    public static class LicenseService
    {
        // RSA Public Key for verification
        private const string PublicKeyXml = @"<RSAKeyValue><Modulus>vi66o07LD7dDXcRlUUWCXQGSPFfwANm7Lm7R90vVJpAJ+blyNlooTaC18XDh4wChotcoEKL7q01J6pWlghbb653KmU6wboFHLWupNOgql3/U/Cb4xPAPPd/r8poLUXdiuBKuOqoDJ9yz1pnMZ7O1AwcToUuFQOJH/WBFl64dftmYuEh5K0gsA7A8pPoG4dXcjdfu3ONi9D8pbWiziclz1sxVT19YeNisgwEnzwXSqKMJoua/34ihEyFzkUon6U8wrgDB/H81zJskfFdLgqf4DivM5LNYZD88zk/MdfIOgPFhSVOMJANiXKA3XwogVD1P1I1wqDQ2vYPoeNGMJM7vGQ==</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";
        
        // Note: PrivateKeyXml should only be available to the developer.
        // For this task, it's moved to LicenseGeneratorView.xaml.cs or kept separately.

        public enum LicenseStatus
        {
            Valid,
            InvalidDevice,
            InvalidLicenseKey,
            DatabaseError,
            FirstTimeActivation,
            NeedsActivation,
            Expired
        }

        public class LicenseData
        {
            public string HardwareId { get; set; } = string.Empty;
            public DateTime ExpiryDate { get; set; }
            public string RandomSeed { get; set; } = Guid.NewGuid().ToString();
        }

        public static (LicenseStatus Status, string Message) ValidateLicense()
        {
            try
            {
                var hardwareId = HardwareIdService.GetHardwareId();
                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());

                var existingLicense = db.Licenses.FirstOrDefault(l => l.HardwareId == hardwareId);

                if (existingLicense != null)
                {
                    // Verify the digital signature of the stored key
                    var (isValid, expiryDate, _) = VerifySignedLicense(existingLicense.LicenseKey, hardwareId);
                    
                    if (!isValid)
                    {
                        existingLicense.IsActive = false;
                        db.SaveChanges();
                        return (LicenseStatus.InvalidLicenseKey, "مفتاح الترخيص غير صالح أو تم التلاعب به");
                    }

                    if (expiryDate.HasValue && expiryDate.Value < DateTime.Now.Date)
                    {
                        existingLicense.IsActive = false;
                        db.SaveChanges();
                        return (LicenseStatus.Expired, "انتهت صلاحية الترخيص، يرجى إدخال مفتاح جديد");
                    }

                    if (!existingLicense.IsActive)
                    {
                        return (LicenseStatus.NeedsActivation, "يرجى إدخال مفتاح الترخيص");
                    }

                    existingLicense.LastActivatedDate = DateTime.Now;
                    db.SaveChanges();

                    return (LicenseStatus.Valid, "الترخيص صالح");
                }

                return (LicenseStatus.NeedsActivation, "يرجى إدخال مفتاح الترخيص");
            }
            catch (Exception ex)
            {
                return (LicenseStatus.DatabaseError, $"خطأ في التحقق من الترخيص: {ex.Message}");
            }
        }

        public static (bool Success, string Message) ActivateLicense(string licenseKey)
        {
            try
            {
                var hardwareId = HardwareIdService.GetHardwareId();
                var machineName = Environment.MachineName;

                // Verify the signed license key
                var (isValid, expiryDate, message) = VerifySignedLicense(licenseKey, hardwareId);
                
                if (!isValid)
                {
                    return (false, message);
                }

                if (expiryDate.HasValue && expiryDate.Value < DateTime.Now.Date)
                {
                    return (false, "انتهت صلاحية مفتاح الترخيص");
                }

                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());

                // Check if this key is already used by another device in our local DB
                var otherLicense = db.Licenses.FirstOrDefault(l => l.LicenseKey == licenseKey && l.HardwareId != hardwareId);
                if (otherLicense != null)
                {
                    return (false, "هذا المفتاح مستخدم بالفعل على جهاز آخر");
                }

                var existingLicense = db.Licenses.FirstOrDefault(l => l.HardwareId == hardwareId);

                if (existingLicense != null)
                {
                    existingLicense.LicenseKey = licenseKey;
                    existingLicense.IsActive = true;
                    existingLicense.LastActivatedDate = DateTime.Now;
                    existingLicense.ExpiryDate = expiryDate;
                    db.SaveChanges();
                }
                else
                {
                    var newLicense = new License
                    {
                        LicenseKey = licenseKey,
                        HardwareId = hardwareId,
                        MachineName = machineName,
                        FirstActivatedDate = DateTime.Now,
                        LastActivatedDate = DateTime.Now,
                        ExpiryDate = expiryDate,
                        IsActive = true,
                        Notes = "تفعيل رقمي جديد"
                    };
                    db.Licenses.Add(newLicense);
                    db.SaveChanges();
                }

                return (true, "تم تفعيل الترخيص بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في تفعيل الترخيص: {ex.Message}");
            }
        }

        public static string GenerateSignedLicense(string hardwareId, DateTime expiryDate, string privateKeyXml)
        {
            var data = new LicenseData
            {
                HardwareId = hardwareId,
                ExpiryDate = expiryDate
            };

            string jsonData = JsonSerializer.Serialize(data);
            byte[] dataBytes = Encoding.UTF8.GetBytes(jsonData);

            using var rsa = new RSACryptoServiceProvider(2048);
            rsa.FromXmlString(privateKeyXml);

            byte[] signature = rsa.SignData(dataBytes, CryptoConfig.MapNameToOID("SHA256"));

            // Key format: Base64(Data) . Base64(Signature)
            return Convert.ToBase64String(dataBytes) + "." + Convert.ToBase64String(signature);
        }

        public static (bool IsValid, DateTime? ExpiryDate, string Message) VerifySignedLicense(string licenseKey, string currentHardwareId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(licenseKey) || !licenseKey.Contains('.'))
                {
                    return (false, null, "تنسيق مفتاح الترخيص غير صالح");
                }

                string[] parts = licenseKey.Split('.');
                if (parts.Length != 2) return (false, null, "تنسيق مفتاح الترخيص غير صالح");

                byte[] dataBytes = Convert.FromBase64String(parts[0]);
                byte[] signatureBytes = Convert.FromBase64String(parts[1]);

                using var rsa = new RSACryptoServiceProvider(2048);
                rsa.FromXmlString(PublicKeyXml);

                bool isSignatureValid = rsa.VerifyData(dataBytes, CryptoConfig.MapNameToOID("SHA256"), signatureBytes);

                if (!isSignatureValid)
                {
                    return (false, null, "فشل التحقق من التوقيع الرقمي للمفتاح");
                }

                string jsonData = Encoding.UTF8.GetString(dataBytes);
                var data = JsonSerializer.Deserialize<LicenseData>(jsonData);

                if (data == null) return (false, null, "بيانات الترخيص تالفة");

                if (data.HardwareId != currentHardwareId)
                {
                    return (false, null, "هذا المفتاح مخصص لجهاز آخر");
                }

                return (true, data.ExpiryDate, "المفتاح صالح");
            }
            catch
            {
                return (false, null, "خطأ أثناء التحقق من المفتاح");
            }
        }

        // Keep existing helper methods but they might be less used now
        public static string GenerateLegacyKey(DateTime? expiryDate = null)
        {
            var guid = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            return $"{guid.Substring(0, 4)}-{guid.Substring(4, 4)}-{guid.Substring(8, 4)}-{guid.Substring(12, 4)}";
        }

        // Restore GenerateLicenseKey for compatibility
        public static string GenerateLicenseKey(DateTime? expiryDate = null)
        {
            // This is the legacy key generator
            var guid = Guid.NewGuid().ToString().Replace("-", "").ToUpper();
            var key = $"{guid.Substring(0, 4)}-{guid.Substring(4, 4)}-{guid.Substring(8, 4)}-{guid.Substring(12, 4)}";

            if (expiryDate.HasValue)
            {
                try
                {
                    var factory = new AppDbContextFactory();
                    using var db = factory.CreateDbContext(Array.Empty<string>());
                    
                    db.IssuedKeys.Add(new IssuedKey
                    {
                        LicenseKey = key,
                        ExpiryDate = expiryDate.Value,
                        IsUsed = false,
                        CreatedDate = DateTime.Now
                    });
                    db.SaveChanges();
                }
                catch
                {
                    // If DB fails, we still return the key, but it won't have an expiry enforced via IssuedKeys
                }
            }

            return key;
        }
    }
}
