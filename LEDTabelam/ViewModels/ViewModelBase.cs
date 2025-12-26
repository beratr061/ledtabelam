using System;
using System.Reactive.Disposables;
using ReactiveUI;

namespace LEDTabelam.ViewModels;

/// <summary>
/// Tüm ViewModel'ler için temel sınıf
/// ReactiveObject'ten türetilmiş, IDisposable desteği ile
/// Requirements: 10.4
/// </summary>
public abstract class ViewModelBase : ReactiveObject, IDisposable
{
    private bool _disposed = false;

    /// <summary>
    /// Reactive subscription'ları yönetmek için CompositeDisposable
    /// </summary>
    protected CompositeDisposable Disposables { get; } = new();

    /// <summary>
    /// ViewModel'in dispose edilip edilmediğini kontrol eder
    /// </summary>
    public bool IsDisposed => _disposed;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Disposables.Dispose();
            }
            _disposed = true;
        }
    }

    ~ViewModelBase()
    {
        Dispose(false);
    }
}
