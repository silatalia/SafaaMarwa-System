using System.Globalization;
using SafaaAndMarwaSystem.Models;
using SafaaAndMarwaSystem.Services;

namespace SafaaAndMarwaSystem;

public partial class MainPage : ContentPage
{
    private readonly DatabaseService _database = new();
    private bool _initialized;

    public MainPage()
    {
        InitializeComponent();
        RecordDatePicker.Date = DateTime.Today;
        Loaded += async (_, _) =>
        {
            if (_initialized) return;
            _initialized = true;
            try
            {
                await _database.InitializeAsync();
                await RefreshRecordsAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("خطأ في قاعدة البيانات", ex.Message, "موافق");
            }
        };
    }

    private static double ReadNumber(Entry entry)
    {
        var raw = (entry.Text ?? "").Trim().Replace('٫', '.').Replace(',', '.');
        return double.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var value) ? value : 0;
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(CarNumberEntry.Text) || string.IsNullOrWhiteSpace(ClientEntry.Text))
        {
            await DisplayAlert("بيانات ناقصة", "أدخل رقم السيارة واسم العميل على الأقل.", "موافق");
            return;
        }

        var record = new TruckRecord
        {
            CarNumber = CarNumberEntry.Text.Trim(),
            CarLetters = CarLettersEntry.Text?.Trim() ?? "",
            TrailerNumber = TrailerNumberEntry.Text?.Trim() ?? "",
            TrailerLetters = TrailerLettersEntry.Text?.Trim() ?? "",
            ClientName = ClientEntry.Text.Trim(),
            MeasurerName = MeasurerEntry.Text?.Trim() ?? "",
            CarLength = ReadNumber(CarLengthEntry),
            CarWidth = ReadNumber(CarWidthEntry),
            CarHeight = ReadNumber(CarHeightEntry),
            TrailerLength = ReadNumber(TrailerLengthEntry),
            TrailerWidth = ReadNumber(TrailerWidthEntry),
            TrailerHeight = ReadNumber(TrailerHeightEntry),
            CustomDeduction = ReadNumber(DeductionEntry),
            ExtraPosition = string.IsNullOrWhiteSpace(ExtraPositionEntry.Text) ? "لا يوجد" : ExtraPositionEntry.Text.Trim(),
            ExtraNotes = ExtraNotesEditor.Text?.Trim() ?? "",
            RecordDate = RecordDatePicker.Date
        };

        var dimensions = new[] { record.CarLength, record.CarWidth, record.CarHeight,
            record.TrailerLength, record.TrailerWidth, record.TrailerHeight, record.CustomDeduction };
        if (dimensions.Any(x => x < 0))
        {
            await DisplayAlert("قيمة غير صحيحة", "لا يمكن إدخال قياسات أو خصومات سالبة.", "موافق");
            return;
        }

        if (record.CustomDeduction > 0 && record.CarLength * record.CarWidth * record.CarHeight
            + record.TrailerLength * record.TrailerWidth * record.TrailerHeight < record.CustomDeduction)
        {
            await DisplayAlert("مراجعة الخصم", "قيمة الخصم أكبر من إجمالي الحجم المحسوب. راجع البيانات قبل الحفظ.", "موافق");
            return;
        }

        try
        {
            await _database.SaveRecordAsync(record);
            ClearInputs();
            await RefreshRecordsAsync();
            await DisplayAlert("تم الحفظ", $"تم حفظ النقلة. الحجم النهائي وفق المعادلة الحالية: {record.FinalVolume:0.###} م³", "موافق");
        }
        catch (Exception ex)
        {
            await DisplayAlert("تعذر الحفظ", ex.Message, "موافق");
        }
    }

    private async Task RefreshRecordsAsync()
    {
        var records = await _database.GetRecordsAsync();
        RecordsCountLabel.Text = $"{records.Count} نقلة مسجلة";
        RecordsList.Children.Clear();

        foreach (var record in records.Take(50))
        {
            var car = $"{record.CarNumber} {record.CarLetters}".Trim();
            var trailer = $"{record.TrailerNumber} {record.TrailerLetters}".Trim();
            var details = new VerticalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    new Label { Text = $"سيارة {car} — {record.ClientName}", FontAttributes = FontAttributes.Bold, TextColor = Color.FromArgb("#15324B") },
                    new Label { Text = $"التاريخ: {record.RecordDate:dd/MM/yyyy} | الصافي: {record.FinalVolume:0.###} م³", TextColor = Color.FromArgb("#637487"), FontSize = 12 },
                    new Label { Text = $"المقطورة: {trailer} | الخصم: {record.CustomDeduction:0.###} م³", TextColor = Color.FromArgb("#637487"), FontSize = 12 },
                    new Label { Text = $"القائم بالتكعيب: {record.MeasurerName} | الوضع الإضافي: {record.ExtraPosition}", TextColor = Color.FromArgb("#637487"), FontSize = 12 }
                }
            };
            if (!string.IsNullOrWhiteSpace(record.ExtraNotes))
                details.Children.Add(new Label { Text = $"ملاحظات: {record.ExtraNotes}", TextColor = Color.FromArgb("#637487"), FontSize = 12 });

            var deleteButton = new Button
            {
                Text = "حذف منطقي",
                BackgroundColor = Color.FromArgb("#FCE8E8"),
                TextColor = Color.FromArgb("#A83232"),
                FontSize = 12,
                Padding = new Thickness(10, 3)
            };
            deleteButton.Clicked += async (_, _) =>
            {
                var ok = await DisplayAlert("تأكيد الحذف", "سيتم إخفاء النقلة من القائمة مع الاحتفاظ بها في قاعدة البيانات. هل تريد المتابعة؟", "حذف", "إلغاء");
                if (!ok) return;
                await _database.SoftDeleteAsync(record.Id, Environment.MachineName);
                await RefreshRecordsAsync();
            };
            details.Children.Add(deleteButton);
            RecordsList.Children.Add(new Border
            {
                BackgroundColor = Colors.White,
                Stroke = Color.FromArgb("#E1E8EF"),
                Padding = 12,
                StrokeShape = new RoundRectangle { CornerRadius = 12 },
                Content = details
            });
        }
    }

    private async void OnRefreshClicked(object sender, EventArgs e)
    {
        try { await RefreshRecordsAsync(); }
        catch (Exception ex) { await DisplayAlert("تعذر التحديث", ex.Message, "موافق"); }
    }

    private async void OnBackupClicked(object sender, EventArgs e)
    {
        try
        {
            var path = await _database.CreateBackupAsync();
            await DisplayAlert("النسخ الاحتياطي", $"تم إنشاء نسخة احتياطية في:\n{path}", "موافق");
        }
        catch (Exception ex)
        {
            await DisplayAlert("تعذر إنشاء النسخة", ex.Message, "موافق");
        }
    }

    private void ClearInputs()
    {
        foreach (var entry in new[] { CarNumberEntry, CarLettersEntry, TrailerNumberEntry, TrailerLettersEntry,
                     ClientEntry, MeasurerEntry, CarLengthEntry, CarWidthEntry, CarHeightEntry,
                     TrailerLengthEntry, TrailerWidthEntry, TrailerHeightEntry, ExtraPositionEntry, DeductionEntry })
            entry.Text = "";
        ExtraNotesEditor.Text = "";
        RecordDatePicker.Date = DateTime.Today;
    }
}
