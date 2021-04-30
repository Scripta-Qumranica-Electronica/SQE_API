using System;
using System.Collections.Generic;
using System.Linq;

namespace SQE.DatabaseAccess.Helpers
{
	internal class Align
	{
		/// <summary>
		///  Compares two text strings and returns a list of correspondences
		///  between the two strings by index in each respective string.
		///  <para />
		///  The method here is adapted from https://www.geeksforgeeks.org/sequence-alignment-problem/
		/// </summary>
		/// <param name="string1">First string for comparison</param>
		/// <param name="string2">Second string for comparison</param>
		/// <param name="mismatchPenalty">Weight penalty for character match</param>
		/// <param name="gapPenalty">Weight penalty for gaps</param>
		/// <returns>
		///  A List of tuples, the str1Idx int in the tuple is the index of a character
		///  in string1 and the str2Idx int is the index of the corresponding character in
		///  string2.
		/// </returns>
		public static List<(int str1Idx, int str2Idx)> AlignTexts(
				string   string1
				, string string2
				, int    mismatchPenalty
				, int    gapPenalty)
		{
			// Initial count variables
			int i
				, j;

			// Capture length of each string
			var str1Len = string1.Length;
			var str2Len = string2.Length;

			// Matrix for best substructure
			var dp = new int[str2Len + str1Len + 1, str2Len + str1Len + 1];

			for (var q = 0; q < str2Len + str1Len + 1; q++)
			for (var w = 0; w < str2Len + str1Len + 1; w++)
				dp[q, w] = 0;

			// Init the table
			for (i = 0; i <= str2Len + str1Len; i++)
			{
				dp[i, 0] = i * gapPenalty;
				dp[0, i] = i * gapPenalty;
			}

			// Determine the minimum penalty
			for (i = 1; i <= str1Len; i++)
			{
				for (j = 1; j <= str2Len; j++)
				{
					if (string1[i - 1] == string2[j - 1])
						dp[i, j] = dp[i - 1, j - 1];
					else
					{
						dp[i, j] = Math.Min(
								Math.Min(
										dp[i - 1, j - 1] + mismatchPenalty
										, dp[i - 1, j] + gapPenalty)
								, dp[i, j - 1] + gapPenalty);
					}
				}
			}

			// Maximum match length
			var l = str2Len + str1Len;
			i = str1Len;
			j = str2Len;
			var str1Pos = l;
			var str2Pos = l;

			// Build the array of match pairs
			var matches = new List<(int str1Idx, int str2Idx)>();

			// Init the empty array
			for (var idx = 0; idx < l + 1; idx++)
				matches.Add((0, 0));

			// Collect matches
			while (!(i == 0 || j == 0))
			{
				matches[str1Pos] = (i - 1, matches[str1Pos].str2Idx);
				matches[str2Pos] = (matches[str2Pos].str1Idx, j - 1);
				str1Pos--;
				str2Pos--;

				if (string1[i - 1] == string2[j - 1]
					|| dp[i - 1, j - 1] + mismatchPenalty == dp[i, j])
				{
					i--;
					j--;
				}
				else if (dp[i - 1, j] + gapPenalty == dp[i, j])
					i--;
				else if (dp[i, j - 1] + gapPenalty == dp[i, j])
					j--;
			}

			// Return matches with duplicates filtered out
			return matches.Distinct().ToList();
		}
	}
}
