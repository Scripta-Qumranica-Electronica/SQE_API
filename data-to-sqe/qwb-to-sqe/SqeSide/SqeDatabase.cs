using qwb_to_sqe.Common;

namespace qwb_to_sqe.Repositories
{
    public class SqeDatabase : SshDatabase
    {
        public SqeDatabase() : base("SQE")
        {
        }
    }
}