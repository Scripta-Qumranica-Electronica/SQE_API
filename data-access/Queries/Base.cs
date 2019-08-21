namespace SQE.SqeHttpApi.DataAccess.Queries
{
	internal interface IQueryResponse<T>
	{
		T CreateModel();
	}
}