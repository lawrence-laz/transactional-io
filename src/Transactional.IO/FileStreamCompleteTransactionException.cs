namespace Transactional.IO;

/// <summary>
/// The exception that is thrown when a <see cref="TransactionalFileStream"/> 
/// is committed, but the transaction could not be completed successfully.
/// </summary>
public sealed class FileStreamCompleteTransactionException
    : Exception
{
    /// <summary>
    /// An empty constructor.
    /// </summary>
    public FileStreamCompleteTransactionException()
    {
    }

    /// <summary>
    /// A constructor accepting a message.
    /// </summary>
    public FileStreamCompleteTransactionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// A constructor accepting a message and the exception that caused the transaction to fail.
    /// </summary>
    public FileStreamCompleteTransactionException(string message, Exception inner)
        : base(message, inner)
    {
    }

}
