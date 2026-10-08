using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DoorCEModel.Infrastructure.DataAccess;

public abstract class MDbTransactionalAccess(DbContext context) : ITransactionalAccess
{
    private IDbContextTransaction? _transaction;
    public void BeginTransaction()
    {
        if (null != _transaction)
            throw new InvalidOperationException("Transaction already started");
        _transaction = context.Database.BeginTransaction();
    }

    public void CommitTransaction()
    {
        if (null == _transaction)
            throw new InvalidOperationException("Transaction not yet started");
        _transaction.Commit();
        _transaction.Dispose();
        _transaction = null;
    }

    public void RollbackTransaction()
    {
        if (null == _transaction)
            throw new InvalidOperationException("Transaction not yet started");
        context.ChangeTracker.Clear();
        _transaction.Rollback();
        _transaction.Dispose();
        _transaction = null;
    }
}