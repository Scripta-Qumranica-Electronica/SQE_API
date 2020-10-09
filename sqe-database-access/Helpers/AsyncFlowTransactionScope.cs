using System.Transactions;

namespace SQE.DatabaseAccess.Helpers
{
    public static class AsyncFlowTransaction
    {
        public static TransactionScope GetScope()
        {
            return new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
        }
    }
}