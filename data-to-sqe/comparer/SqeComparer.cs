using System;
using System.Collections.Generic;
using DiffPlex;
using DiffPlex.Model;
using sqe_api;


namespace comparer
{


    public static class SqeComparer
    {
		public static Dictionary<string, string> NormalizedSigns = new Dictionary<string, string>() {
				{"" , "W"}
				, {"־", "X"}
				, {"°", "X"}
				,

		};
       /// <summary>
       /// Compares all poassible sequences which can be extracted from the sqe und source line and
       /// returns the list of ChangeIds for the best match.
       /// </summary>
       /// <param name="sqeLine"></param>
       /// <param name="sourceLine"></param>
       /// <returns>List of ChangeIds</returns>
        public static List<ChangeIds> Compare(Line sqeLine, Line sourceLine)
        {
            var differ = new Differ();
            var lastPenalty = 999999999;
            List<ChangeIds> bestResult = null;

            // Compare als SQE-Sequences with all Source-sequences
            foreach (var sqeSequence in sqeLine.getSequences())
            {
                foreach (var sourceSequence in sourceLine.getSequences())
                {
                    // First step: create the diff results.
                    var diffResult = differ.CreateCharacterDiffs(
                        sqeSequence.charString,
                        sourceSequence.charString,
                        true);

                    // Second Step create a list of ChangeIds from the diffResult
                    var changeIds = _createChangeIdsListFromDiffResult(
                        sourceSequence,
                        sqeSequence,
                        diffResult);

                    // Run first optimization on it
                  //  _firstOptimization(ref changeIds);

                    // Run second optimization
                  //  _secondOptimization(sourceLine, sqeLine, ref changeIds);

                    // Calculate the penalty of this comparison
                    // and keep it if it ist the best so far.
                    var currPenalty = _calculatePenalty(sourceLine, sqeLine,changeIds);
                    if (currPenalty < lastPenalty)
                    {
                        lastPenalty = currPenalty;
                        bestResult = changeIds;
                    }
                }
            }

            _printList(
                sourceLine,sqeLine,bestResult);

            return bestResult;
        }


       /// <summary>
       /// Calculates the penalty of a solution. Each difference in sign costs 1 point
       /// </summary>
       /// <param name="sourceLine"></param>
       /// <param name="sqeLine"></param>
       /// <param name="changeIds"></param>
       /// <returns>the amount of penalty points</returns>
       private static int _calculatePenalty(
            Line sourceLine,
            Line sqeLine,
            List<ChangeIds> changeIds)
        {
            var penalty = 0;
            foreach (var changeId in changeIds)
            {
                if (changeId.SqeId == null) penalty+=10;
				else if (changeId.SourceId == null)
					penalty += 5;
				else
                {
                    var sourceSign = sourceLine.GetSignInterpretationById(changeId.SourceId.Value).Character;
                    var sqeSign = sqeLine.GetSignInterpretationById(changeId.SqeId.Value).Character;

					if (!sourceSign.Equals(sqeSign))
						penalty += 1;
				}
            }

            return penalty;
        }


       /// <summary>
       /// Runs a first optimization on the given list of ChangeIds which combines deletes and inserts
       /// which follow directly to each other
       /// by mapping the overlapping parts.
       /// </summary>
       /// <param name="changeIds"></param>
        private static void _firstOptimization(ref List<ChangeIds> changeIds)
        {
            var deletes = new List<int>();
            var inserts = new List<int>();
            for (var i = 0; i < changeIds.Count; i++)
            {
                if (changeIds[i].SqeId == null)
                {
                    if (deletes.Count > 0)
                    {
                        changeIds[deletes[0]].SourceId = changeIds[i].SourceId;
                        changeIds[i].SourceId = null;
                        deletes.Remove(0);
                    }
                    else
						inserts.Add(i);
				}
                else if (changeIds[i].SourceId == null)
                {
                    if (inserts.Count > 0)
                    {
                        changeIds[inserts[0]].SqeId = changeIds[i].SqeId;
                        changeIds[i].SqeId = null;
                        inserts.Remove(0);
                    }
                    else
						deletes.Add(i);
				}
                else
                {
                    inserts.Clear();
                    deletes.Clear();
                }
            }

            changeIds.RemoveAll(i => i.SourceId == null && i.SqeId == null);
        }

        private static void _secondOptimization(
            Line sourceLine,
            Line sqeLine,
            ref List<ChangeIds> changeIds)
        {
            var runs = true;
            while (runs)
            {
                runs = false;
                for (var i = 0; i < changeIds.Count - 1; i++)
                {
                    var data = changeIds[i];
                    var nextData = changeIds[i + 1];
                    if (data.SqeId == null && nextData.SqeId != null && data.SourceId != null)
                    {
                        var sourceSign = sourceLine.GetSignInterpretationById(data.SourceId.Value).Character;
                        var sqeSign = sqeLine.GetSignInterpretationById(nextData.SqeId.Value).Character;
                        if (sourceSign.Equals(sqeSign))
                        {
                            data.SqeId = nextData.SqeId;
                            nextData.SqeId = null;
                            runs = true;
                        }
                    }
                    else if (data.SourceId == null && nextData.SourceId != null && data.SqeId != null)
                    {
                        var sourceSign = sourceLine.GetSignInterpretationById(nextData.SourceId.Value).Character;
                        var sqeSign = sqeLine.GetSignInterpretationById(data.SqeId.Value).Character;
                        if (sourceSign.Equals(sqeSign))
                        {
                            data.SourceId = nextData.SourceId;
                            nextData.SourceId = null;
                            runs = true;
                        }
                    }
                }
            }
        }

        private static List<ChangeIds> _createChangeIdsListFromDiffResult(
            SignInterpretationSequence sourceSequence,
            SignInterpretationSequence sqeSequence,
            DiffResult diffResult)
        {
            var sourceListPosition = 0;
            var sqeListPosition = 0;
            var _changeIds = new List<ChangeIds>();

            foreach (var diffBlock in diffResult.DiffBlocks)
            {
                var deleteStartSource = diffBlock.DeleteStartA;
                var deleteCountSource = diffBlock.DeleteCountA;
                var insertStartSqe = diffBlock.InsertStartB;
                var insertCountSqe = diffBlock.InsertCountB;

                // Add as correlating the next ids until we reach a position marked in the diffBlock.
                //Or the end of one of the lists
                while (sqeListPosition < deleteStartSource
                       && sourceListPosition < insertStartSqe
                       && sourceListPosition < sourceSequence.NumberOfInterpretations()
                       && sqeListPosition < sqeSequence.NumberOfInterpretations())
                {
                    _changeIds.Add(new ChangeIds()
                    {
                        SourceId = sourceSequence.GetSignInterpretationIdAtPosition(sourceListPosition++),
                        SqeId = sqeSequence.GetSignInterpretationIdAtPosition(sqeListPosition++),
                    });
                }

                // Source contains a block where  the sign is different and/or signs from
                // source must be inserted
                while (deleteCountSource > 0 && insertCountSqe > 0)
                {
                    _changeIds.Add(new ChangeIds()
                    {
                        SourceId = sourceSequence.GetSignInterpretationIdAtPosition(sourceListPosition++),
                        SqeId = sqeSequence.GetSignInterpretationIdAtPosition(sqeListPosition++),
                    });

                    deleteCountSource--;
                    insertCountSqe--;
                }

                while (deleteCountSource > 0)
                {
                    _changeIds.Add(new ChangeIds()
                    {

                        SqeId = sqeSequence.GetSignInterpretationIdAtPosition(sqeListPosition++),
					});
                    deleteCountSource--;
                }

                while (insertCountSqe > 0)
                {
                    _changeIds.Add(new ChangeIds()
                    {
                        SourceId = sourceSequence.GetSignInterpretationIdAtPosition(sourceListPosition++),
                    });

                    insertCountSqe--;
                }
            }

            while (sourceListPosition < sourceSequence.NumberOfInterpretations()
                   && sqeListPosition < sqeSequence.NumberOfInterpretations())
            {
                _changeIds.Add(new ChangeIds()
                {
                    SourceId = sourceSequence.GetSignInterpretationIdAtPosition(sourceListPosition++),
                    SqeId = sqeSequence.GetSignInterpretationIdAtPosition(sqeListPosition++),
                });

            }

            while (sqeListPosition < sqeSequence.NumberOfInterpretations())
            {
                _changeIds.Add(new ChangeIds()
                {
                    SqeId = sqeSequence.GetSignInterpretationIdAtPosition(sqeListPosition++),
                });

            }

            while (sourceListPosition < sourceSequence.NumberOfInterpretations())
            {
                _changeIds.Add(new ChangeIds()
                {
                    SourceId = sourceSequence.GetSignInterpretationIdAtPosition(sourceListPosition++),
                });
            }

            return _changeIds;
        }


        private static void _printList(Line sourceLine, Line sqeLine, List<ChangeIds> changeIds)
        {
            foreach (var ci in changeIds)
            {
                var sourcSign = ci.SourceId != null
                    ? sourceLine.GetSignInterpretationById(ci.SourceId.Value).Character
                    : "null";

                if (sourcSign.Equals("")) sourcSign = " ";
                var sqeSign = ci.SqeId != null
                    ? sqeLine.GetSignInterpretationById(ci.SqeId.Value).Character
                    : "null";
                if (sqeSign.Equals("")) sqeSign = " ";


                Console.Write(sourcSign.Equals(sqeSign) ? sourcSign: $"[{sourcSign} => {sqeSign}]" );

            }
            Console.WriteLine();
        }
    }
}
