using App2.Commands;
using App2.Data;
using App2.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace App2.ViewModels
{
    public class InventoryViewModel : ObservableObject
    {
        private string _searchText = string.Empty;
        private bool _isLoading;
        private int _totalProducts;
        private int _totalCartons;
        private int _totalRemainingKabba;
        public string SearchText
        {
            get => _searchText;
            set
            {
                SetProperty(ref _searchText, value);
                FilterInventory();
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public int TotalProducts
        {
            get => _totalProducts;
            set => SetProperty(ref _totalProducts, value);
        }

        public int TotalCartons
        {
            get => _totalCartons;
            set => SetProperty(ref _totalCartons, value);
        }

        public int TotalRemainingKabba
        {
            get => _totalRemainingKabba;
            set => SetProperty(ref _totalRemainingKabba, value);
        }



        private ObservableCollection<InventorySummaryItem> _inventoryItems = new();
        public ObservableCollection<InventorySummaryItem> InventoryItems
        {
            get => _inventoryItems;
            set => SetProperty(ref _inventoryItems, value);
        }
        
        private readonly List<InventorySummaryItem> _allItems = new();

        public ICommand RefreshCommand { get; }

        public InventoryViewModel()
        {
            // المُنشئ خفيف جداً — لا تحميل بيانات هنا
            RefreshCommand = new RelayCommand(ExecuteRefresh);
        }

        /// <summary>
        /// يُستدعى من حدث Loaded في الواجهة بعد ظهور النافذة
        /// </summary>
        public async Task LoadDataAsync()
        {
            if (IsLoading) return;
            IsLoading = true;

            try
            {
                // تنفيذ كل عمل قاعدة البيانات على خيط خلفي
                var summaryList = await Task.Run(() =>
                {
                    var factory = new AppDbContextFactory();
                    using var db = factory.CreateDbContext(Array.Empty<string>());

                    var inventories = db.Inventories
                        .Include(i => i.Product)
                        .AsNoTracking()
                        .ToList();

                    var result = new List<InventorySummaryItem>();

                    var grouped = inventories
                        .Where(i => i.Product != null)
                        .GroupBy(i => i.ProductId);

                    foreach (var group in grouped)
                    {
                        var product = group.First().Product;
                        if (product == null) continue;

                        // نحصل على آخر سجل مخزون (بناءً على تاريخ الإضافة)
                        var latestRecord = group.OrderByDescending(i => i.DateAdded).FirstOrDefault();
                        
                        var summaryItem = new InventorySummaryItem
                        {
                            ProductId = product.Id,
                            ColorNumber = product.ColorNumber,
                            Color = product.Color ?? string.Empty,
                            TotalQuantity = latestRecord?.Quantity ?? 0
                        };
                        summaryItem.SetOriginalColorNumber(product.ColorNumber);
                        summaryItem.SetOriginalColor(product.Color ?? string.Empty);
                        summaryItem.SetAsLoaded();

                        result.Add(summaryItem);
                    }

                    return result;
                });

                // تحديث الواجهة على UI thread (الـ await يرجع تلقائياً للـ UI thread)
                _allItems.Clear();
                _allItems.AddRange(summaryList);
                FilterInventory();
                CalculateStatistics();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل المخزون:\n{ex.Message}", "خطأ",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void FilterInventory()
        {
            var filtered = string.IsNullOrWhiteSpace(SearchText)
                ? _allItems
                : _allItems.Where(i =>
                    i.ColorNumber.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    i.Color.Contains(SearchText, StringComparison.OrdinalIgnoreCase));

            InventoryItems = new ObservableCollection<InventorySummaryItem>(filtered);
        }

        private void CalculateStatistics()
        {
            TotalProducts = _allItems.Count(i => i.TotalQuantity > 0);
            int totalKabba = _allItems.Sum(i => i.TotalQuantity);
            TotalCartons = totalKabba / Inventory.KabbaPerCarton;
            TotalRemainingKabba = totalKabba % Inventory.KabbaPerCarton;
        }

        private void ExecuteRefresh(object? parameter)
        {
            _allItems.Clear();
            InventoryItems = new ObservableCollection<InventorySummaryItem>();
            TotalProducts = 0;
            TotalCartons = 0;
            _ = LoadDataAsync();
        }
    }
}
