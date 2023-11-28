using System.Diagnostics;

namespace pskrmqtt2db.Services;

/// <summary>
/// Hmm actually maybe not needed
/// </summary>
internal class FileWrapper 
{
    private readonly FileStream _fileStream;
    private readonly StreamWriter _streamWriter;
    private readonly Stopwatch _lastWriteStopwatch = new ();
    public bool IsClosed { get; private set; }

    public FileWrapper(string fullPath)
    {
        _fileStream = File.Open(fullPath, FileMode.Append, FileAccess.Write);
        _streamWriter = new StreamWriter(_fileStream);
        _ = Task.Run(async () => await WatchCloseTimerLoop());
    }

    private async Task WatchCloseTimerLoop()
    {
        while (_lastWriteStopwatch.ElapsedMilliseconds < 60000)
        {
            await Task.Delay(1000);
        }

        IsClosed = true;

        await _streamWriter.FlushAsync();
        _streamWriter.Close();
    }

    public async Task<bool> Append(string line)
    {
        if (IsClosed)
        {
            return false;
        }

        await _streamWriter.WriteLineAsync(line);
        _lastWriteStopwatch.Restart();
        return true;
    }
}