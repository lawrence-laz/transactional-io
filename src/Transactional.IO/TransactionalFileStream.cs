namespace Transactional.IO;

///
public sealed class TransactionalFileStream : FileStream
{
    private readonly string _tempFilePath;
    private readonly string _originalFilePath;
    private readonly FileStream _tempFileStream;
    private bool _isCommitted;
    private bool _disposedValue;

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
        tempFilePath = $"{filePath}.{Guid.NewGuid()}.tmp";
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
            var backupFilePath = $"{_originalFilePath}.{Guid.NewGuid()}.original.tmp";
            try
            {
                File.Move(_originalFilePath, backupFilePath);
                File.Move(_tempFilePath, _originalFilePath);
                File.Delete(backupFilePath);
            }
            catch (Exception exception)
            {
                if (!File.Exists(_originalFilePath) && File.Exists(backupFilePath))
                {
                    File.Move(backupFilePath, _originalFilePath);
                    File.Delete(backupFilePath);
                }

                throw new FileStreamCompleteTransactionException(
                    $"Could not complete the trasaction for the" +
                    $"{nameof(TransactionalFileStream)} for " +
                    $"'{_originalFilePath}'. See inner exception for details.",
                    exception);
            }
        }
        else
        {
            File.Delete(_tempFilePath);
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

