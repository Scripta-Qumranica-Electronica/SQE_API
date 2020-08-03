using qwb_to_sqe.Repositories;

namespace qwb_to_sqe
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var sqe = new SqeDatabase();
            var qwb = new QWBDatabase();

            foreach (var qwbScroll in qwb.GetScrollIds()) qwb.readInScrollText(qwbScroll);


            /*

            var qwbScrolls = qwb.GetScrolls().Where(s => s != null);
            foreach (var qwbScroll in qwbScrolls)
            {
  
                
                
                
                
                
               
                  var nextIdQuery = @"update qwb_word set next_qwb_word_id=@nextId where qwb_word_id=@wordId";
                 
                uint wordId = 0;
                string fragmentName = "";
                Console.WriteLine(qwbScroll.Name);
                foreach (var qwbScrollFragment in qwbScroll.fragments)
                {
                    
                    foreach (var qwbLine in qwbScrollFragment.Lines)
                    {
                        foreach (var qwbWord in qwbLine.words)
                        {
                            Console.Write($"{wordId}");
                            if (wordId != 0 && fragmentName==qwbScrollFragment.Name)
                            {
                                sqe.connection.Execute(nextIdQuery,
                                    new {wordId, nextId = qwbWord.QWBWordId});
                                Console.Write($" -> {qwbWord.QWBWordId}");
                            } else  fragmentName = qwbScrollFragment.Name;
                            wordId = qwbWord.QWBWordId;
                            Console.WriteLine();
                        }
                    }
                    
                }
                
            }
            */
        }
    }
}