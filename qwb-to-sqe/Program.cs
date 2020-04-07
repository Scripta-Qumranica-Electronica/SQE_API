using System;

namespace qwb_to_sqe
{
    class Program
    {
        static void Main(string[] args)
        {
            var qwbDB = new QwbDatabase();
            // To prefer to much load on the server, we work book by book
            foreach (var bookId in qwbDB.GetBookIds())
            {
                
            }
            
            qwbDB.Close();
            
        }
    }
}
