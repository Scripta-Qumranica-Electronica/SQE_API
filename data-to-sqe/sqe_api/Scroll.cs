using System;
using System.Collections.Generic;
using SQE.DatabaseAccess.Models;

namespace sqe_api
{
	public class Scroll
	{
		private readonly List<TextFragmentData> _fragments = new List<TextFragmentData>();
		private readonly SqeApi                 _sqeApi;
		private readonly UserInfo               _userInfo;
		public readonly  uint                   EditionId;
		public readonly  string                 ScrollName;

		//	public static Create

		public Scroll(Edition edition, SqeApi api, UserInfo userInfo)
		{
			ScrollName = edition.Name;
			EditionId = edition.EditionId;
			_sqeApi = api;
			_userInfo = userInfo;
			_fragments = api.TextRep.GetFragmentDataAsync(userInfo).Result;
		}

		private TextFragmentData _getFragmentData(string fragmentName)
		{
			var fragment = _fragments.Find(f => f.TextFragmentName == fragmentName);

			if (fragment == null)
				throw new Exception($"Fragment {fragmentName} not found.");

			return fragment;
		}

		/// <summary>
		///  Retrieves the Data of a given line as an instance of LineDataExtended,
		///  Throws an exception if the fragment or the line is not found.
		/// </summary>
		/// <param name="fragmentName"></param>
		/// <param name="lineName"></param>
		/// <returns>LineDataExtended</returns>
		/// <exception cref="Exception"></exception>
		public Line GetLine(string fragmentName, string lineName)
		{
			var fragment = _getFragmentData(fragmentName);

			if ((fragment.Lines == null)
				|| (fragment.Lines.Count == 0))
			{
				Console.Write($"Loading fragment {fragmentName}.");

				var fragmentLines = _sqeApi.TextRep.GetTextFragmentByIdAsync(
												   _userInfo
												   , fragment.TextFragmentId.GetValueOrDefault())
										   .Result.fragments[0]
										   .Lines;

				if (fragmentLines != null)
					fragment.Lines = fragmentLines;
				else
					throw new Exception($"Fragment {fragmentName} not found.");

				Console.Write($"Fragment {fragmentName} loaded.");
			}

			var line = fragment.Lines.Find(l => l.LineName == lineName);

			return line == null
					? null
					: new Line(this, line);
		}

		public uint GetFragmentId(string fragName)
			=> _getFragmentData(fragName).TextFragmentId.GetValueOrDefault();
	}
}
