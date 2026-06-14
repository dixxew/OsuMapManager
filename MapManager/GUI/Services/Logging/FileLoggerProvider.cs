using System;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace MapManager.GUI.Services.Logging;

/// <summary>
/// Минимальный файловый провайдер для стандартного Microsoft.Extensions.Logging.
/// В самом фреймворке файлового sink нет, поэтому пишем свой: один файл на запуск дня,
/// потокобезопасная дозапись, AutoFlush (альфа-софт — ничего не теряем при краше).
/// Подключается через <see cref="FileLoggerExtensions.AddFileLogger"/>.
/// Лог лежит в %LocalAppData%/MapManager/logs/mapmanager-yyyy-MM-dd.log.
/// </summary>
public sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly StreamWriter _writer;
    private readonly object _gate = new();
    private readonly LogLevel _minLevel;
    private bool _disposed;

    // Папка рядом с исполняемым файлом (AppContext.BaseDirectory), подпапка logs.
    public static string LogDirectory { get; } = Path.Combine(AppContext.BaseDirectory, "logs");

    public string LogFilePath { get; }

    public FileLoggerProvider(LogLevel minLevel = LogLevel.Trace)
    {
        _minLevel = minLevel;

        Directory.CreateDirectory(LogDirectory);
        LogFilePath = Path.Combine(LogDirectory, $"mapmanager-{DateTime.Now:yyyy-MM-dd}.log");

        // FileShare.ReadWrite — чтобы лог можно было открыть/хвостить во время работы приложения.
        var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
        _writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };

        Write("FileLogger", LogLevel.Information,
            $"==== Log session started {DateTime.Now:O} (pid {Environment.ProcessId}, min level {_minLevel}) ====",
            null);
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this, categoryName);

    internal bool IsEnabled(LogLevel level) => level != LogLevel.None && level >= _minLevel;

    internal void Write(string category, LogLevel level, string message, Exception? exception)
    {
        var sb = new StringBuilder(256);
        sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
        sb.Append(" [").Append(ShortLevel(level)).Append("] ");
        sb.Append(ShortCategory(category)).Append(" — ").Append(message);
        if (exception != null)
            sb.Append(Environment.NewLine).Append(exception);

        var line = sb.ToString();
        lock (_gate)
        {
            if (_disposed) return;
            _writer.WriteLine(line);
        }
    }

    // Оставляем только имя класса (MapManager.GUI.Services.ChatService -> ChatService).
    private static string ShortCategory(string category)
    {
        var idx = category.LastIndexOf('.');
        return idx >= 0 && idx < category.Length - 1 ? category[(idx + 1)..] : category;
    }

    private static string ShortLevel(LogLevel level) => level switch
    {
        LogLevel.Trace => "TRC",
        LogLevel.Debug => "DBG",
        LogLevel.Information => "INF",
        LogLevel.Warning => "WRN",
        LogLevel.Error => "ERR",
        LogLevel.Critical => "CRT",
        _ => "???",
    };

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed) return;
            _disposed = true;
            try { _writer.Flush(); _writer.Dispose(); } catch { /* shutdown — игнорируем */ }
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;
        private readonly string _category;

        public FileLogger(FileLoggerProvider provider, string category)
        {
            _provider = provider;
            _category = category;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => _provider.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            _provider.Write(_category, logLevel, formatter(state, exception), exception);
        }
    }
}

public static class FileLoggerExtensions
{
    /// <summary>Добавляет файловый sink к стандартному логгеру Microsoft.</summary>
    public static ILoggingBuilder AddFileLogger(this ILoggingBuilder builder, LogLevel minLevel = LogLevel.Trace)
    {
        builder.AddProvider(new FileLoggerProvider(minLevel));
        return builder;
    }
}
