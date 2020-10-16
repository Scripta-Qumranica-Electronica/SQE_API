using System.Transactions;

namespace SQE.DatabaseAccess.Helpers
{
	public static class AsyncFlowTransaction
	{
		public static TransactionScope GetScope()
			=> new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
	}
}
