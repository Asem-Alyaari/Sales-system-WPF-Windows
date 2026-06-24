using System.Windows;
using App2.Services;

namespace App2.Views
{
    public partial class LicenseGeneratorView : Window
    {
        // This should be kept secret and only available in the developer's build/tool
        private const string PrivateKeyXml = @"<RSAKeyValue><Modulus>vi66o07LD7dDXcRlUUWCXQGSPFfwANm7Lm7R90vVJpAJ+blyNlooTaC18XDh4wChotcoEKL7q01J6pWlghbb653KmU6wboFHLWupNOgql3/U/Cb4xPAPPd/r8poLUXdiuBKuOqoDJ9yz1pnMZ7O1AwcToUuFQOJH/WBFl64dftmYuEh5K0gsA7A8pPoG4dXcjdfu3ONi9D8pbWiziclz1sxVT19YeNisgwEnzwXSqKMJoua/34ihEyFzkUon6U8wrgDB/H81zJskfFdLgqf4DivM5LNYZD88zk/MdfIOgPFhSVOMJANiXKA3XwogVD1P1I1wqDQ2vYPoeNGMJM7vGQ==</Modulus><Exponent>AQAB</Exponent><P>1k3BfgonJESPTWPEdS2YsG5k6wp0XUsEdBWqIcPc4ahi3o2wZKz9iG3BFYX8ZKX6XRjyEmBziyTZFtpNImXvmF+PHMmJhvT37M1FPaEROWJHU+ZiVQU7ad3RN+2ldLu/jipXWhmgPtHwlcN/+hApdGLuUkRhs4FRGTI7EWF6wZ8=</P><Q>4y+Fih9PL3TGKZ1lbBBxJacXy+mJ/NiOMRZ4Kj8bR05h60mcK9voO2qIpn40Bh4sJ5M3GcpyK9sNHAh8ZboNcGCM7UexW9FZHcuHX8aV1XYN3ZdOXhlhJvkaoLkdDaf29C103dshUC/jkE+O1ztzV+dxbHLjcojQWN4q20GLREc=</Q><DP>mXt83eE1oVL88ydF98pdNdcKrg+BwaNNoDo37BDT7EXl8ZC2yZPfzMsWY2zfk9IP2odYL/MmLXyJgkV8wusQyyd9Xte0iJR/z/g/4+CsblXF0gAJYuzpXWwBQLYSLuWcTpxijWQXEYbYcNpgmN7kYbfNCdxxwNcYFxyTk2ImQe8=</DP><DQ>f6CFw6d9I6rVXDGI9aFy/vUUwEAtfbbmgpsd3JXhLDjTd4u9yUHb/+0EYYwKi9lNctoYHUwGwa5oefQmdjuEKzqCURZyg6NjDgL1xQ9ZwzZz6aWDqAdX9b4BgIMd2Dsg1+HlgnEFEPgmPj8DftRuItbpeEQ+lGxZp2L/7nau1yc=</DQ><InverseQ>hrgnhZQGrCJx/Px97aF67XvOhXKPBn5qqx9EZZNgU23oGTCmyKlkHKpBzrHaw5WQhzKVTdSuRHv2gbhigvdLh0bo8ASaeJZE5KJU9fH1DKRtTkJegO+FnLMT2YdAiHfLb3rg5Q63pG/aBK06KSFTpHtH5zouCxQ6S8uURJsuUoo=</InverseQ><D>AEChoMve+yNcJIi5NbVo6eSxpADd4bJoTDR7dN+V5do1GS7E1wuhmBDJjQuAUUPT8xpkBDmjKeQNYBFYyeZr3lE0TqXLGCp1xLqq29YfEFpsFvWaSPCkpIlij8zijPYinGnx70QAgxII7xT/L3gOmnVs8j7ea2qhe9Wf4gaqJVv2AOcMllsH9XlPX9tL4X7I12oPlfCP5W8FMez9ADHQ7qEz7zHfMBNbebaKnxdPbv83UfiIdKnus53VhSXIkITaZrdyAw7xKUX56NJrTr/ej2K8jBN10rTXk5l/XlwKzJMibqGmW/WzaSByOW6Y+UL1Sb6ryM0YcZSE4bia2ppArQ==</D></RSAKeyValue>";

        public LicenseGeneratorView()
        {
            InitializeComponent();
            ExpiryDatePicker.SelectedDate = DateTime.Now.AddYears(1);
            LoadCurrentHardwareId();
        }

        private void LoadCurrentHardwareId()
        {
            HardwareIdTextBox.Text = HardwareIdService.GetHardwareId();
        }

        private void CopyHwidBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(HardwareIdTextBox.Text))
            {
                Clipboard.SetText(HardwareIdTextBox.Text);
                MessageBox.Show("تم نسخ معرف جهازك الحالي إلى الحافظة!", "نجاح", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void GenerateButton_Click(object sender, RoutedEventArgs e)
        {
            string hwid = HardwareIdTextBox.Text.Trim();
            DateTime? expiry = ExpiryDatePicker.SelectedDate;

            if (string.IsNullOrEmpty(hwid))
            {
                MessageBox.Show("يرجى إدخال معرف الجهاز");
                return;
            }

            if (!expiry.HasValue)
            {
                MessageBox.Show("يرجى اختيار تاريخ الانتهاء");
                return;
            }

            string key = LicenseService.GenerateSignedLicense(hwid, expiry.Value, PrivateKeyXml);
            GeneratedKeyTextBox.Text = key;
        }

        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(GeneratedKeyTextBox.Text))
            {
                Clipboard.SetText(GeneratedKeyTextBox.Text);
                MessageBox.Show("تم نسخ المفتاح إلى الحافظة!");
            }
        }
    }
}
