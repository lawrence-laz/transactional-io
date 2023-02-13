namespace Transactional.IO;

/// <summary>
/// Provides a <see cref="Stream"/> for a file, supporting read and write operations
/// in a transactional manner. 
/// The changes are only saved on dispose if <see cref="Commit"/> was called beforehand. 
/// </summary>
public sealed class TransactionalFileStream : FileStream
{
    private readonly string _tempFilePath;
    private readonly bool _didOriginalFileExistOnCreate;
    private readonly FileMode _mode;
    private readonly string _originalFilePath;
    private readonly FileStream _tempFileStream;
    private bool _isCommitted;
    private bool _disposedValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionalFileStream"/>
    /// class with the specified path and creation mode.
    /// </summary>
    public TransactionalFileStream(string filePath, FileMode mode)
        : base(CreateTempCopy(filePath, out var tempFilePath, mode), mode)
    {
        _didOriginalFileExistOnCreate = File.Exists(filePath);
        _mode = mode;
        _originalFilePath = filePath;
        _tempFilePath = tempFilePath;
        _tempFileStream = new FileStream(_tempFilePath, FileMode.Open);
    }

    private static string CreateTempCopy(
        string filePath,
        out string tempFilePath,
        FileMode mode)
    {
        tempFilePath = $"{filePath}.{Guid.NewGuid()}.tmp";
        if (ShouldCreateIfDoesntExist(mode) && !File.Exists(filePath))
        {
            using var _ = File.Create(tempFilePath);
        }
        else
        {
            File.Copy(filePath, tempFilePath);
        }

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
                if (ShouldCreateIfDoesntExist(_mode) && !_didOriginalFileExistOnCreate)
                {
                    // Since original file did not exist, there is nothing to move.
                }
                else
                {
                    File.Move(_originalFilePath, backupFilePath);
                }

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

    /// <summary>
    /// Disposes the <see cref="TransactionalFileStream"/> and completes the 
    /// underlying transaction either by saving changes, or rolling them back.
    /// </summary>
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


    private static bool ShouldCreateIfDoesntExist(FileMode mode)
    {
        return mode == FileMode.Append
            || mode == FileMode.Create
            || mode == FileMode.OpenOrCreate;
    }
}

