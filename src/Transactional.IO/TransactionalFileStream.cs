namespace Transactional.IO;

///
public sealed class TransactionalFileStream : FileStream
{
    private readonly string _tempFilePath;
    private readonly string _originalFilePath;
    private readonly FileStream _tempFileStream;
    private bool _isCommitted;
    private bool _disposedValue;
    private string? _backupFilePath;

    ///
    public TransactionalFileStream(string filePath, FileMode mode)
        : base(CreateTempCopy(filePath, out var tempFilePath), mode)
    {
        _originalFilePath = filePath;
        _tempFilePath = tempFilePath;
        _tempFileStream = new FileStream(_tempFilePath, FileMode.Open);
    }

    private static string CreateTempCopy(string filePath, out string tempFilePath)
    {
        tempFilePath = filePath + DateTime.Now.ToFileTime() + ".tmp";
        File.Copy(filePath, tempFilePath);
        return tempFilePath;
    }

    /// <summary>
    /// Marks the stream to be commited on dispose.
    /// Notice that the stream is not commited right away, because the related
    /// <see cref="StreamWriter"/> might still have some content to flush, which
    /// happens on dispose.
    /// </summary>
    public void Commit()
    {
        if (_isCommitted)
        {
            throw new InvalidOperationException(
                $"Cannot commit the {nameof(TransactionalFileStream)} for " +
                $"'{_originalFilePath}' because it was already committed.");
        }
        _isCommitted = true;
    }

    private void CompleteTransaction()
    {
        if (_isCommitted)
        {
            _backupFilePath = _originalFilePath + DateTime.Now.ToFileTime() + ".original.tmp";
            File.Move(_originalFilePath, _backupFilePath);
            File.Move(_tempFilePath, _originalFilePath);
            File.Delete(_backupFilePath);
        }
        else
        {
            File.Delete(_tempFilePath);
            if (_backupFilePath is not null)
            {
                if (!File.Exists(_originalFilePath))
                {
                    File.Move(_backupFilePath, _originalFilePath);
                }
                File.Delete(_backupFilePath);
            }
        }
    }

    ///
    protected override void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                CompleteTransaction();
                _tempFileStream.Dispose();
            }
            _disposedValue = true;
        }
        base.Dispose(disposing);
    }
}

