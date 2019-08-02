namespace SQE.SqeApi.DataAccess.Queries
{
    internal interface IQueryResponse<T>
    {
        T CreateModel();
    }
}
