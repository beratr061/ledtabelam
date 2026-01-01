using FluentValidation;
using LEDTabelam.Models;

namespace LEDTabelam.Validators;

/// <summary>
/// Profile model için FluentValidation kuralları
/// </summary>
public class ProfileValidator : AbstractValidator<Profile>
{
    public ProfileValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Profil adı boş olamaz")
            .MaximumLength(100).WithMessage("Profil adı en fazla 100 karakter olabilir")
            .Matches(@"^[a-zA-Z0-9ğüşıöçĞÜŞİÖÇ\s\-_]+$")
            .WithMessage("Profil adı sadece harf, rakam, boşluk, tire ve alt çizgi içerebilir");

        RuleFor(x => x.FontName)
            .NotEmpty().WithMessage("Font adı boş olamaz");

        RuleFor(x => x.Settings)
            .NotNull().WithMessage("Görüntüleme ayarları gereklidir")
            .SetValidator(new DisplaySettingsValidator());

        RuleFor(x => x.Programs)
            .NotNull().WithMessage("Program listesi null olamaz")
            .Must(p => p.Count > 0).WithMessage("En az bir program olmalıdır");

        RuleForEach(x => x.Programs)
            .SetValidator(new TabelaProgramValidator());
    }
}

/// <summary>
/// DisplaySettings için FluentValidation kuralları
/// </summary>
public class DisplaySettingsValidator : AbstractValidator<DisplaySettings>
{
    public DisplaySettingsValidator()
    {
        RuleFor(x => x.PanelWidth)
            .InclusiveBetween(16, 1024).WithMessage("Panel genişliği 16-1024 piksel arasında olmalıdır");

        RuleFor(x => x.PanelHeight)
            .InclusiveBetween(8, 256).WithMessage("Panel yüksekliği 8-256 piksel arasında olmalıdır");

        RuleFor(x => x.PixelSize)
            .InclusiveBetween(1, 32).WithMessage("Piksel boyutu 1-32 arasında olmalıdır");

        RuleFor(x => x.Brightness)
            .InclusiveBetween(0, 100).WithMessage("Parlaklık 0-100 arasında olmalıdır");

        RuleFor(x => x.BackgroundDarkness)
            .InclusiveBetween(0, 100).WithMessage("Arka plan karartma 0-100 arasında olmalıdır");

        RuleFor(x => x.CustomPitchRatio)
            .InclusiveBetween(0.3, 0.95).WithMessage("Özel pitch oranı 0.3-0.95 arasında olmalıdır")
            .When(x => x.Pitch == PixelPitch.Custom);
    }
}

/// <summary>
/// TabelaProgram için FluentValidation kuralları
/// </summary>
public class TabelaProgramValidator : AbstractValidator<TabelaProgram>
{
    public TabelaProgramValidator()
    {
        RuleFor(x => x.Id)
            .GreaterThan(0).WithMessage("Program ID'si pozitif olmalıdır");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Program adı boş olamaz")
            .MaximumLength(50).WithMessage("Program adı en fazla 50 karakter olabilir");

        RuleFor(x => x.DurationSeconds)
            .InclusiveBetween(1, 60).WithMessage("Program süresi 1-60 saniye arasında olmalıdır");

        RuleFor(x => x.TransitionDurationMs)
            .InclusiveBetween(200, 1000).WithMessage("Geçiş süresi 200-1000 ms arasında olmalıdır");

        RuleFor(x => x.Items)
            .NotNull().WithMessage("Öğe listesi null olamaz");

        RuleForEach(x => x.Items)
            .SetValidator(new TabelaItemValidator());
    }
}

/// <summary>
/// TabelaItem için FluentValidation kuralları
/// </summary>
public class TabelaItemValidator : AbstractValidator<TabelaItem>
{
    public TabelaItemValidator()
    {
        RuleFor(x => x.X)
            .GreaterThanOrEqualTo(0).WithMessage("X koordinatı negatif olamaz");

        RuleFor(x => x.Y)
            .GreaterThanOrEqualTo(0).WithMessage("Y koordinatı negatif olamaz");

        RuleFor(x => x.Width)
            .GreaterThan(0).WithMessage("Genişlik pozitif olmalıdır");

        RuleFor(x => x.Height)
            .GreaterThan(0).WithMessage("Yükseklik pozitif olmalıdır");

        RuleFor(x => x.ScrollSpeed)
            .InclusiveBetween(0, 100).WithMessage("Kaydırma hızı 0-100 arasında olmalıdır");

        RuleFor(x => x.Content)
            .MaximumLength(1000).WithMessage("İçerik en fazla 1000 karakter olabilir")
            .When(x => x.ItemType == TabelaItemType.Text);

        RuleFor(x => x.LetterSpacing)
            .InclusiveBetween(0, 20).WithMessage("Harf aralığı 0-20 piksel arasında olmalıdır");

        RuleFor(x => x.SymbolSize)
            .InclusiveBetween(16, 32).WithMessage("Sembol boyutu 16-32 piksel arasında olmalıdır")
            .When(x => x.ItemType == TabelaItemType.Symbol);
    }
}
