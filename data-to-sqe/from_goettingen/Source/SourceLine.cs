using System;
using System.Collections.Generic;
using System.Linq;
using sqe_api;
using SQE.DatabaseAccess.Models;

namespace from_goettingen.Source
{
    public class SourceLine : IEquatable<SourceLine>, IComparable<SourceLine>
    {
        public string LineName { get; set; }
        private readonly List<SourceSign> Signs = new List<SourceSign>();

        public SourceLine(string lineName) => LineName = lineName.Trim();

		public void addSourceSign(SourceSign sourceSign)
        {
            Signs.Add(sourceSign);

        }

        public void addSourceSigns(List<SourceSign> sourceSigns)
        {
            Signs.AddRange(sourceSigns);
        }

        public List<string> sortedSigns()
        {
            Signs.Sort();
            return Signs.Select(x => x.Sign).ToList();
        }

        public bool Equals(SourceLine other) => LineName == other?.LineName;

		public int CompareTo(SourceLine other)
        {
            if (int.TryParse(LineName, out var lineInt))
            {
                if (int.TryParse(other.LineName, out var otherLineInt))
					return lineInt.CompareTo(otherLineInt);
			}

            Console.WriteLine($"|{LineName}| - |{other.LineName}|");
            return 1;
        }

        public string getTestString()
        {
            var testString = "";
            Signs.Sort();
            foreach (var sign in Signs)
            {
                if (sign.isSpace()) testString += " ";
                else if (sign.isVacat()) testString += "V";
                else testString += sign.Sign;
            }

            return testString;
        }


        public Line asSqeLine()
        {
            var lineData = new LineData();
            lineData.LineName = LineName;
            uint signInterpretationId = 1;
            foreach (var s in Signs)
            {
                lineData.Signs.Add(s.asSignData(ref signInterpretationId));
                var count = lineData.Signs.Count;
                if (count <= 1) continue;
                foreach (var currSi in lineData.Signs.Last().SignInterpretations)
                {
                    var lastSign = lineData.Signs[count - 2].SignInterpretations;
                    foreach (var lastSi in lastSign)
                    {
                        lastSi.NextSignInterpretations.Add(new NextSignInterpretation()
                        {
                            NextSignInterpretationId = currSi.SignInterpretationId.GetValueOrDefault(),
                        });
                    }
                }
            }

            var line = new Line(lineData);
            return line;
        }
    }
}
