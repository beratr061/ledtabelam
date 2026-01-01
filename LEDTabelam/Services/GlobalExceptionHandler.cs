using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Avalonia.Threading;

namespace LEDTabelam.Services;

/// <summary>
/// Global exception handler - tüm yakalanmamış hataları yönetir
/// </summary>
public class GlobalExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Global exception handler'ları kaydeder
    /// </summary>
    public void Register()
    {
        // Unhandled exceptions
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

        // Task exceptions
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
    }

    /// <summary>
    /// Global exception handler'ları kaldırır
    /// </summary>
    public void Unregister()
    {
        AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
        TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
    }

    private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        var exception = e.ExceptionObject as Exception;
        _logger.LogCritical(exception, "Yakalanmamış hata oluştu. IsTerminating: {IsTerminating}", e.IsTerminating);

        if (e.IsTerminating)
        {
            // Uygulama kapanmadan önce log'ları flush et
            Serilog.Log.CloseAndFlush();
        }
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _logger.LogError(e.Exception, "Gözlemlenmemiş Task hatası");
        e.SetObserved(); // Exception'ı işlenmiş olarak işaretle
    }

    /// <summary>
    /// UI thread'de güvenli hata gösterimi
    /// </summary>
    public void ShowErrorOnUI(string title, string message)
    {
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                // Basit hata mesajı göster (MessageBox yerine log)
                _logger.LogError("UI Hatası - {Title}: {Message}", title, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hata mesajı gösterilirken hata oluştu");
            }
        });
    }
}

/// <summary>
/// Exception handler olmadan kullanım için static helper
/// </summary>
public static class ExceptionHelper
{
    /// <summary>
    /// Action'ı try-catch ile sarar ve hataları loglar
    /// </summary>
    public static void SafeExecute(Action action, string context = "")
    {
        try
        {
            action();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Hata oluştu: {Context}", context);
        }
    }

    /// <summary>
    /// Async action'ı try-catch ile sarar ve hataları loglar
    /// </summary>
    public static async Task SafeExecuteAsync(Func<Task> action, string context = "")
    {
        try
        {
            await action();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Async hata oluştu: {Context}", context);
        }
    }

    /// <summary>
    /// Func'ı try-catch ile sarar, hata durumunda default değer döner
    /// </summary>
    public static T SafeExecute<T>(Func<T> func, T defaultValue, string context = "")
    {
        try
        {
            return func();
        }
        catch (Exception ex)
        {
            Serilog.Log.Error(ex, "Hata oluştu: {Context}", context);
            return defaultValue;
        }
    }
}
