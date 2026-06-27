using App2.Data;
using App2.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace App2.ViewModels
{
    public class InventorySummaryItem : ObservableObject
    {
        private int _productId;
        private string _colorNumber = string.Empty;
        private string _originalColorNumber = string.Empty;
        private string _color = string.Empty;
        private string _originalColor = string.Empty;
        private int _totalQuantity;
        private bool _isLoaded;

        public int ProductId
        {
            get => _productId;
            set => SetProperty(ref _productId, value);
        }

        public string ColorNumber
        {
            get => _colorNumber;
            set
            {
                // Don't do anything if value is same as current
                if (_colorNumber == value)
                    return;

                // If not loaded yet, just set the value without confirmation
                if (!_isLoaded)
                {
                    SetProperty(ref _colorNumber, value);
                    return;
                }

                // Check if ColorNumber is already taken by another product
                if (!string.IsNullOrWhiteSpace(value))
                {
                    var factory = new AppDbContextFactory();
                    using var db = factory.CreateDbContext(Array.Empty<string>());
                    bool exists = db.Products.Any(p => p.ColorNumber == value && p.Id != ProductId);
                    if (exists)
                    {
                        System.Windows.MessageBox.Show($"رقم الخيط \"{value}\" موجود بالفعل!", "خطأ", 
                            System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        // Don't update ColorNumber
                        return;
                    }
                }

                // Show confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"هل أنت متأكد من تعديل رقم الصنف من \"{_colorNumber}\" إلى \"{value}\"؟", 
                    "تأكيد التعديل", 
                    System.Windows.MessageBoxButton.YesNo, 
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    if (SetProperty(ref _colorNumber, value))
                    {
                        SaveProductChanges();
                    }
                }
                else
                {
                    // Revert the value in UI
                    OnPropertyChanged(nameof(ColorNumber));
                }
            }
        }

        public string Color
        {
            get => _color;
            set
            {
                // Don't do anything if value is same as current
                if (_color == value)
                    return;

                // If not loaded yet, just set the value without confirmation
                if (!_isLoaded)
                {
                    SetProperty(ref _color, value);
                    return;
                }

                // Show confirmation dialog
                var result = System.Windows.MessageBox.Show(
                    $"هل أنت متأكد من تعديل اسم الصنف من \"{_color}\" إلى \"{value}\"؟", 
                    "تأكيد التعديل", 
                    System.Windows.MessageBoxButton.YesNo, 
                    System.Windows.MessageBoxImage.Question);

                if (result == System.Windows.MessageBoxResult.Yes)
                {
                    if (SetProperty(ref _color, value))
                    {
                        SaveProductChanges();
                    }
                }
                else
                {
                    // Revert the value in UI
                    OnPropertyChanged(nameof(Color));
                }
            }
        }

        public int TotalQuantity
        {
            get => _totalQuantity;
            set => SetProperty(ref _totalQuantity, value);
        }

        // إجمالي الكراتين للعرض
        public int TotalCartons => TotalQuantity / Inventory.KabbaPerCarton;
        
        // الكبة المتبقية للعرض
        public int RemainingKabba => TotalQuantity % Inventory.KabbaPerCarton;

        // نص العرض المنسق للإجمالي
        public string DisplayTotalQuantity => TotalCartons > 0 
            ? $"{TotalCartons} كرتون" + (RemainingKabba > 0 ? $" و {RemainingKabba} كبة" : "")
            : $"{TotalQuantity} كبة";

        // Method to set original color number (call this when creating an InventorySummaryItem
        public void SetOriginalColorNumber(string colorNumber)
        {
            _originalColorNumber = colorNumber;
        }
        
        // Method to set original color
        public void SetOriginalColor(string color)
        {
            _originalColor = color;
        }
        
        // Method to mark item as loaded (to skip confirmation during initialization)
        public void SetAsLoaded()
        {
            _isLoaded = true;
        }

        private async void SaveProductChanges()
        {
            try
            {
                var factory = new AppDbContextFactory();
                using var db = factory.CreateDbContext(Array.Empty<string>());
                var product = await db.Products.FindAsync(ProductId);
                if (product != null)
                {
                    product.ColorNumber = ColorNumber;
                    product.Color = Color;
                    await db.SaveChangesAsync();
                    _originalColorNumber = ColorNumber;
                    _originalColor = Color;
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"خطأ في حفظ التغييرات:\n{ex.Message}", "خطأ", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }
    }

    public class InventoryBatchItem
    {
        public int Id { get; set; }
        public string BatchName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public string Unit { get; set; } = "كبة";
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime DateAdded { get; set; }

        // إجمالي الكراتين لهذه الدفعة
        public int Cartons => Quantity / Inventory.KabbaPerCarton;
        
        // الكبة المتبقية لهذه الدفعة
        public int RemainingKabba => Quantity % Inventory.KabbaPerCarton;

        // نص العرض المنسق للدفعة
        public string DisplayQuantity => Cartons > 0 
            ? $"{Cartons} كرتون" + (RemainingKabba > 0 ? $" و {RemainingKabba} كبة" : "")
            : $"{Quantity} كبة";
    }
}
