namespace DoorCEModel.Infrastructure.DataAccess;

public interface ITransactionalAccess
{
    public void BeginTransaction();
    public void CommitTransaction();
    public void RollbackTransaction();
}