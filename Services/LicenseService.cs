using App2.Data;
using App2.Models;

namespace App2.Services
{
    public static class LicenseService
    {
        public enum LicenseStatus
        {
            Valid,
            InvalidDevice,
            InvalidLicenseKey,
            DatabaseError,
            FirstTimeActivation,
            NeedsActivation
        }

        public static (LicenseStatus Status, string Message) ValidateLicense()
        {
            try
            {
                var hardwareId = HardwareIdService.GetHardwareId();
                var machineName = Environment.MachineName;

                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());

                // Check if this hardware ID is already registered with a license
                var existingLicense = db.Licenses.FirstOrDefault(l => l.HardwareId == hardwareId);

                if (existingLicense != null)
                {
                    // Check if license is expired
                    if (existingLicense.ExpiryDate.HasValue && existingLicense.ExpiryDate.Value < DateTime.Now.Date)
                    {
                        // Deactivate expired license
                        existingLicense.IsActive = false;
                        db.SaveChanges();
                        return (LicenseStatus.InvalidLicenseKey, "انتهت صلاحية الترخيص، يرجى إدخال مفتاح جديد");
                    }

                    // Check if license is inactive
                    if (!existingLicense.IsActive)
                    {
                        return (LicenseStatus.NeedsActivation, "يرجى إدخال مفتاح الترخيص");
                    }

                    // Update last activated date
                    existingLicense.LastActivatedDate = DateTime.Now;
                    db.SaveChanges();

                    return (LicenseStatus.Valid, "الترخيص صالح");
                }

                // Check if there are any licenses in the database
                var anyLicense = db.Licenses.Any();

                if (!anyLicense)
                {
                    // No license key required for first installation
                    // User will need to activate with a license key
                    return (LicenseStatus.NeedsActivation, "يرجى إدخال مفتاح الترخيص");
                }

                // This is a different device trying to use the same database
                return (LicenseStatus.InvalidDevice,
                    $"هذا النظام مرخص لجهاز آخر فقط. معرف الجهاز الحالي: {hardwareId.Substring(0, 8)}...");
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

                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());

                // Check if license key is already used by another device
                var existingLicense = db.Licenses.FirstOrDefault(l => l.LicenseKey == licenseKey);

                if (existingLicense != null)
                {
                    if (existingLicense.HardwareId == hardwareId)
                    {
                        // Check if license is expired
                        if (existingLicense.ExpiryDate.HasValue && existingLicense.ExpiryDate.Value < DateTime.Now.Date)
                        {
                            return (false, "انتهت صلاحية الترخيص، يرجى إدخال مفتاح جديد");
                        }

                        // Same device, just reactivate
                        existingLicense.IsActive = true;
                        existingLicense.LastActivatedDate = DateTime.Now;
                        db.SaveChanges();
                        return (true, "تم تفعيل الترخيص بنجاح");
                    }
                    else
                    {
                        return (false, "مفتاح الترخيص مستخدم بالفعل على جهاز آخر");
                    }
                }

                // Validate license key format (simple validation)
                if (!IsValidLicenseKeyFormat(licenseKey))
                {
                    return (false, "مفتاح الترخيص غير صالح");
                }

                // Check if the key exists in IssuedKeys and get its expiry date
                var issuedKey = db.IssuedKeys.FirstOrDefault(k => k.LicenseKey == licenseKey);

                // Key must exist in IssuedKeys table
                if (issuedKey == null)
                {
                    return (false, "مفتاح الترخيص غير صالح. يرجى استخدام مفتاح صادر من مطورالنظام");
                }

                // Check if key is already used
                if (issuedKey.IsUsed)
                {
                    return (false, "مفتاح الترخيص مستخدم بالفعل");
                }

                // Check if key is expired
                if (issuedKey.ExpiryDate < DateTime.Now.Date)
                {
                    return (false, "انتهت صلاحية مفتاح الترخيص، يرجى الحصول على مفتاح جديد");
                }

                // Create new license
                var newLicense = new License
                {
                    LicenseKey = licenseKey,
                    HardwareId = hardwareId,
                    MachineName = machineName,
                    FirstActivatedDate = DateTime.Now,
                    LastActivatedDate = DateTime.Now,
                    ExpiryDate = issuedKey.ExpiryDate,
                    IsActive = true,
                    Notes = "تفعيل جديد"
                };

                issuedKey.IsUsed = true;

                db.Licenses.Add(newLicense);
                db.SaveChanges();

                return (true, "تم تفعيل الترخيص بنجاح");
            }
            catch (Exception ex)
            {
                return (false, $"خطأ في تفعيل الترخيص: {ex.Message}");
            }
        }

        public static string GenerateLicenseKey(DateTime? expiryDate = null)
        {
            // Generate a unique license key
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

        private static bool IsValidLicenseKeyFormat(string licenseKey)
        {
            // Simple validation: XXXX-XXXX-XXXX-XXXX format
            if (string.IsNullOrWhiteSpace(licenseKey))
                return false;

            var parts = licenseKey.Split('-');
            if (parts.Length != 4)
                return false;

            foreach (var part in parts)
            {
                if (part.Length != 4)
                    return false;
            }

            return true;
        }

        public static List<License> GetAllLicenses()
        {
            try
            {
                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());
                return db.Licenses.ToList();
            }
            catch
            {
                return new List<License>();
            }
        }

        public static bool DeactivateLicense(int licenseId)
        {
            try
            {
                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());
                var license = db.Licenses.Find(licenseId);
                if (license != null)
                {
                    license.IsActive = false;
                    db.SaveChanges();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public static bool ActivateLicense(int licenseId)
        {
            try
            {
                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());
                var license = db.Licenses.Find(licenseId);
                if (license != null)
                {
                    license.IsActive = true;
                    license.LastActivatedDate = DateTime.Now;
                    db.SaveChanges();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}
