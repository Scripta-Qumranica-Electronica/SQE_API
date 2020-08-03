using System.Collections.Generic;
using Dapper;
using qwb_to_sqe.Common;

namespace qwb_to_sqe
{
    public class QWBDatabase : SshDatabase
    {
        private const string getScrollDataQuery = @"
                          SELECT Id, Buch as Name
                          FROM buch
                          WHERE (Art LIKE 'qb' 
                                 OR (Art LIKE 'q' AND Reihe < 1910 ))
                             AND Haupt=0
                         ORDER BY Reihe";

        private const string getScrollTextQuery = @"
                        SELECT kol as Name, zeile, wort.Id AS QWBWordId, wort AS QWBText
                        FROM wort
                            LEFt JOIN stellen on StelleId = stellen.Id
                        WHERE Buchnr = @scrollId
                            AND wort.pos=0
                            AND owner=0
                        ORDER BY Buchwortnr";

        public QWBDatabase() : base("QWBlocal")
        {
        }


        public IEnumerable<QWBScroll> GetScrollIds()
        {
            return connection.Query<QWBScroll>(getScrollDataQuery);
        }

        public IEnumerable<string> GetFragments(uint scrollId)
        {
            return connection.Query<string>(
                @"SELECT DISTINCT kol
                            FROM stellen
                             WHERE Buchnr = @scrollId
                             ORDER BY Buchwortnr",
                new { scrollId }
            );
        }


        public void readInScrollText(QWBScroll qwbScroll)
        {
            var currentLine = new QWBLine();
            var currentFrag = new QWBFragment();
            connection.Query<QWBFragment, string, QWBWord, QWBFragment>(
                getScrollTextQuery,
                (kol, zeile, word) =>
                {
                    if (kol.Name != currentFrag.Name)
                    {
                        qwbScroll.fragments.Add(kol);
                        currentFrag = kol;
                    }

                    if (zeile != currentLine.Name)
                    {
                        currentLine = new QWBLine { Name = zeile };
                        currentFrag.Lines.Add(currentLine);
                    }

                    currentLine.words.Add(word);
                    return null;
                },
                splitOn: "zeile, QWBWordId",
                param: new { scrollId = qwbScroll.Id }
            );
        }

        public IEnumerable<QWBScroll> GetScrolls()
        {
            var currentFrag = new QWBFragment();
            var currentScroll = new QWBScroll();
            var currentLine = new QWBLine();
            return connection.Query<QWBScroll, string, string, QWBWord, QWBScroll>(
                @"SELECT Buchnr AS Id, Buch AS Name, kol, zeile, wort.Id AS QWBWordId, wort AS QWBText
                        FROM wort
                            LEFt JOIN stellen on StelleId = stellen.Id
                            LEFT JOIN buch on buch.Id =Buchnr
                        WHERE (Art LIKE 'qb' OR (Art LIKE 'q' AND Reihe < 1910 ))
                            AND Haupt=0
                            AND wort.pos=0
                            AND owner=0
                        ORDER BY Reihe, Buchwortnr
",
                (scroll, kol, line, word) =>
                {
                    var newScroll = currentScroll.Id != scroll.Id;
                    if (newScroll) currentScroll = scroll;
                    if (kol != currentFrag.Name)
                    {
                        currentFrag = new QWBFragment { Name = kol };
                        currentScroll.fragments.Add(currentFrag);
                        currentLine = new QWBLine();
                    }

                    if (line != currentLine.Name)
                    {
                        currentLine = new QWBLine { Name = line };
                        currentFrag.Lines.Add(currentLine);
                    }

                    currentLine.words.Add(word);

                    return newScroll ? currentScroll : null;
                },
                splitOn: "kol, zeile, QWBWordId"
            );
        }
    }
}