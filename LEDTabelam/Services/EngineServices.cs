using System;

namespace LEDTabelam.Services;

/// <summary>
/// Render ve çizim ile ilgili servisleri gruplayan Facade implementasyonu
/// </summary>
public class EngineServices : IEngineServices, IDisposable
{
    private bool _disposed;

    public IFontLoader FontLoader { get; }
    public ILedRenderer LedRenderer { get; }
    public IAnimationService AnimationService { get; }
    public IExportService ExportService { get; }
    public IMultiLineTextRenderer MultiLineTextRenderer { get; }
    public IPreviewRenderer PreviewRenderer { get; }

    public EngineServices(
        IFontLoader fontLoader,
        ILedRenderer ledRenderer,
        IAnimationService animationService,
        IExportService exportService,
        IMultiLineTextRenderer multiLineTextRenderer,
        IPreviewRenderer previewRenderer)
    {
        FontLoader = fontLoader ?? throw new ArgumentNullException(nameof(fontLoader));
        LedRenderer = ledRenderer ?? throw new ArgumentNullException(nameof(ledRenderer));
        AnimationService = animationService ?? throw new ArgumentNullException(nameof(animationService));
        ExportService = exportService ?? throw new ArgumentNullException(nameof(exportService));
        MultiLineTextRenderer = multiLineTextRenderer ?? throw new ArgumentNullException(nameof(multiLineTextRenderer));
        PreviewRenderer = previewRenderer ?? throw new ArgumentNullException(nameof(previewRenderer));
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        // IDisposable olan servisleri temizle
        (LedRenderer as IDisposable)?.Dispose();
        (AnimationService as IDisposable)?.Dispose();

        GC.SuppressFinalize(this);
    }
}
