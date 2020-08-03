namespace SQE.DatabaseAccess.Models
{
    public interface ISearchData
    {
        string getSearchParameterString();
        string getJoinsString();
    }
}