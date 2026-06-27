using App2.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using System.Windows.Media;
using System.Linq;
using System.Windows.Data;

namespace App2.Views
{
    public partial class PurchaseInvoiceView : UserControl
    {
        private PurchaseInvoiceViewModel? _viewModel;
        private DispatcherTimer? _searchTimer;
        private TextBox? _activeTextBox;
        private ComboBox? _activeComboBox;
        private bool _isSelectionChanging = false;

        public PurchaseInvoiceView()
        {
            InitializeComponent();
            _viewModel = new PurchaseInvoiceViewModel();
            DataContext = _viewModel;
            
            this.DataContextChanged += (s, e) => {
                if (e.NewValue is PurchaseInvoiceViewModel vm)
                {
                    _viewModel = vm;
                }
            };

            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _searchTimer.Tick += SearchTimer_Tick;
        }

        private void SearchTimer_Tick(object? sender, EventArgs e)
        {
            _searchTimer?.Stop();

            if (_activeTextBox != null && _activeComboBox != null && _viewModel != null)
            {
                string text = _activeComboBox.Text;
                _viewModel.SearchProducts(text);

                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (!string.IsNullOrWhiteSpace(text) && _viewModel.SuggestedProducts.Any())
                    {
                        int caretIndex = _activeTextBox.SelectionStart;
                        _activeComboBox.IsDropDownOpen = true;
                        _activeTextBox.SelectionStart = caretIndex;
                        _activeTextBox.SelectionLength = 0;
                    }
                    else
                    {
                        _activeComboBox.IsDropDownOpen = false;
                    }
                }), DispatcherPriority.Background);
            }
        }

        private void ComboBox_DropDownClosed(object? sender, System.EventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.SelectedItem is App2.Models.Product product)
            {
                // عند اختيار منتج، يمكن ملء حقل اللون تلقائياً
                if (comboBox.DataContext is App2.Models.PurchaseInvoiceItem item)
                {
                    item.Color = product.Color ?? string.Empty;
                    item.IsNew = false;
                }
            }
        }

        private void ComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.Focus();

                // Find the TextBox inside the ComboBox's template and subscribe to its events
                var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;
                if (textBox != null)
                {
                    textBox.TextChanged += ComboBox_TextChanged;
                    textBox.PreviewTextInput += TextBox_PreviewTextInput;
                    DataObject.AddPastingHandler(textBox, TextBox_Pasting);

                    textBox.Focus();
                    textBox.CaretIndex = textBox.Text.Length;
                }
            }
        }

        private void ComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSelectionChanging)
            {
                return;
            }

            if (sender is TextBox textBox && _viewModel != null)
            {
                var comboBox = textBox.TemplatedParent as ComboBox;
                if (comboBox != null)
                {
                    _activeTextBox = textBox;
                    _activeComboBox = comboBox;

                    _searchTimer?.Stop();
                    _searchTimer?.Start();
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _isSelectionChanging = true;

            if (sender is ComboBox comboBox && comboBox.SelectedItem is App2.Models.Product product)
            {
                if (comboBox.DataContext is App2.Models.PurchaseInvoiceItem item)
                {
                    item.Color = product.Color ?? string.Empty;
                    item.IsNew = false;
                }
            }

            Dispatcher.BeginInvoke(new Action(() => _isSelectionChanging = false), DispatcherPriority.Background);
        }

        private void ComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                comboBox.IsDropDownOpen = false;
            }
        }

        private void ComboBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is ComboBox comboBox && _viewModel != null)
            {
                // السماح بالحذف والكتابة بشكل طبيعي
                if (e.Key == Key.Back || e.Key == Key.Delete)
                {
                    comboBox.IsDropDownOpen = false;
                }
                else if (e.Key == Key.Space)
                {
                    e.Handled = true; // منع إدخال المسافة
                }
            }
        }

        private void DataGridCell_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridCell cell && !cell.IsEditing && !cell.IsReadOnly)
            {
                if (!cell.IsFocused)
                {
                    cell.Focus();
                }

                var dataGrid = FindVisualParent<DataGrid>(cell);
                if (dataGrid != null)
                {
                    dataGrid.BeginEdit(e);
                }
            }
        }

        private void DataGrid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || sender is not DataGrid dataGrid)
                return;

            e.Handled = true;

            var currentCell = dataGrid.CurrentCell;
            if (!currentCell.IsValid) return;

            int currentColumnIndex = dataGrid.Columns.IndexOf(currentCell.Column);
            int currentRowIndex = dataGrid.Items.IndexOf(currentCell.Item);

            // آخر عمود قابل للتحرير (نتجاهل عمود "إجراء" الأخير)
            int lastEditableColumnIndex = dataGrid.Columns.Count - 2;

            // حفظ التعديل الحالي أولاً
            dataGrid.CommitEdit(DataGridEditingUnit.Cell, true);

            Dispatcher.BeginInvoke(new Action(() =>
            {
                if (currentColumnIndex < lastEditableColumnIndex)
                {
                    // الانتقال إلى العمود التالي في نفس الصف
                    var nextColumn = dataGrid.Columns[currentColumnIndex + 1];
                    dataGrid.CurrentCell = new DataGridCellInfo(currentCell.Item, nextColumn);
                    dataGrid.BeginEdit();
                }
                else
                {
                    // وصلنا لآخر عمود → إضافة صف جديد تلقائياً
                    if (_viewModel != null)
                    {
                        _viewModel.AddItemCommand.Execute(null);

                        // الانتظار حتى يظهر الصف الجديد في الجدول
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            // الصف الجديد هو آخر صف قبل NewItemPlaceholder
                            int newRowIndex = _viewModel.InvoiceItems.Count - 1;
                            if (newRowIndex >= 0)
                            {
                                var newItem = dataGrid.Items[newRowIndex];
                                dataGrid.CurrentCell = new DataGridCellInfo(newItem, dataGrid.Columns[0]);
                                dataGrid.ScrollIntoView(newItem);
                                dataGrid.BeginEdit();
                            }
                        }), DispatcherPriority.Background);
                    }
                }
            }), DispatcherPriority.Background);
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            if (parentObject is T parent)
                return parent;
            return FindVisualParent<T>(parentObject);
        }

        private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // السماح بالأرقام فقط
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void TextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!text.All(char.IsDigit))
                {
                    e.CancelCommand(); // إلغاء اللصق إذا احتوى على غير أرقام
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void DataGrid_PreparingCellForEdit(object sender, DataGridPreparingCellForEditEventArgs e)
        {
            if (e.EditingElement is TextBox textBox)
            {
                string header = e.Column.Header?.ToString() ?? string.Empty;
                if (header == "الكمية")
                {
                    textBox.PreviewTextInput += QuantityTextBox_PreviewTextInput;
                    DataObject.AddPastingHandler(textBox, QuantityTextBox_Pasting);
                    textBox.PreviewKeyDown += TextBox_PreviewKeyDown_BlockSpace;
                }

            }
        }

        private void QuantityTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !e.Text.All(char.IsDigit);
        }

        private void QuantityTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(DataFormats.Text))
            {
                string text = (string)e.DataObject.GetData(DataFormats.Text);
                if (!text.All(char.IsDigit))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void TextBox_PreviewKeyDown_BlockSpace(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Space)
            {
                e.Handled = true;
            }
        }
    }
}
