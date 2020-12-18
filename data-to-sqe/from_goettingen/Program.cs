using System;
using System.Collections.Generic;
using System.Linq;
using comparer;
using from_goettingen.Source;
using sqe_api;
using SQE.DatabaseAccess.Models;

namespace from_goettingen
{
	internal static class Program
	{
		private static readonly List<SourceFileInfo> sourceFiles = new List<SourceFileInfo>()
		{

				new SourceFileInfo()
				{
						FileName = "0_0_10K6_1r_I.xlsx"
						, ColName = "col. 1"
						, ImageFileName = "MS-TS-00010-K-00006-000-00017.jp2"
						, SqeImageId = 127492
						, Scale = (decimal) 1215 / 588
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_1v_II.xlsx"
						, ColName = "col. 2"
						, ImageFileName = "MS-TS-00010-K-00006-000-00018.jp2"
						, SqeImageId = 127493
						, OffsetX = 3370
						, ImagePart = imagePart.Right
						, Scale = (decimal) (1215 / 433 * Math.Sqrt(2) * 0.993)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_2r_III.xlsx"
						, ColName = "col. 3"
						, ImageFileName = "MS-TS-00010-K-00006-000-00018.jp2"
						, SqeImageId = 127493
						, ImagePart = imagePart.Left

						//		, Scale = (decimal) ((1215 / 433) * Math.Sqrt(2) * (3372/3442))
						, Scale = (decimal) (1215 / 433 * Math.Sqrt(2) * 0.993)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_2v_IV.xlsx"
						, ColName = "col. 4"
						, ImageFileName = "MS-TS-00010-K-00006-000-00019.jp2"
						, SqeImageId = 127494
						, ImagePart = imagePart.Right
						, OffsetX = 3390
						, Scale = (decimal) (1215 / 431 * Math.Sqrt(2) * 0.997)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_3r_V.xlsx"
						, ColName = "col. 5"
						, ImageFileName = "MS-TS-00010-K-00006-000-00019.jp2"
						, SqeImageId = 127494
						, ImagePart = imagePart.Left
						, Scale = (decimal) (1215 / 431 * Math.Sqrt(2) * 0.997)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_3v_VI.xlsx"
						, ColName = "col. 6"
						, ImageFileName = "MS-TS-00010-K-00006-000-00020.jp2"
						, SqeImageId = 127495
						, ImagePart = imagePart.Right
						, OffsetX = 3516
						, Scale = (decimal) (1215 / 431 * Math.Sqrt(2) * 0.997)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_4r_VII.xlsx"
						, ColName = "col. 7"
						, ImageFileName = "MS-TS-00010-K-00006-000-00020.jp2"
						, SqeImageId = 127495
						, ImagePart = imagePart.Left
						, Scale = (decimal) (1215 / 431 * Math.Sqrt(2) * 0.997)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_4v_VIII.xlsx"
						, ColName = "col. 8"
						, ImageFileName = "MS-TS-00010-K-00006-000-00021.jp2"
						, SqeImageId = 127496
						, Scale = (decimal) 1215 / 612
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_5r_IX.xlsx"
						, ColName = "col. 9"
						, ImageFileName = "MS-TS-00010-K-00006-000-00022.jp2"
						, SqeImageId = 127497
						, Scale = (decimal) 1215 / 583
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_5v_X.xlsx"
						, ColName = "col. 10"
						, ImageFileName = "MS-TS-00010-K-00006-000-00023.jp2"
						, SqeImageId = 127498
						, ImagePart = imagePart.Right
						, OffsetX = 3396
						, Scale = (decimal) (1215 / 434 * Math.Sqrt(2) * 0.9945)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_6r_XI.xlsx"
						, ColName = "col. 11"
						, ImageFileName = "MS-TS-00010-K-00006-000-00023.jp2"
						, SqeImageId = 127498
						, ImagePart = imagePart.Left
						, Scale = (decimal) (1215 / 434 * Math.Sqrt(2) * 0.9945)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_6v_XII.xlsx"
						, ColName = "col. 12"
						, ImageFileName = "MS-TS-00010-K-00006-000-00024.jp2"
						, SqeImageId = 127499
						, Scale = (decimal) 1215 / 590
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_7r_XIII.xlsx"
						, ColName = "col. 13"
						, ImageFileName = "MS-TS-00010-K-00006-000-00025.jp2"
						, SqeImageId = 127500
						, Scale = (decimal) 1215 / 590
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_7v_XIV.xlsx"
						, ColName = "col. 14"
						, ImageFileName = "MS-TS-00010-K-00006-000-00026.jp2"
						, SqeImageId = 127501
						, ImagePart = imagePart.Right
						, OffsetX = 3332
						, Scale = (decimal) (1215 / 436 * Math.Sqrt(2) * 0.985)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_8r_XV_neu.xlsx"
						, ColName = "col. 15"
						, ImageFileName = "MS-TS-00010-K-00006-000-00026.jp2"
						, SqeImageId = 127501
						, ImagePart = imagePart.Left
						, Scale = (decimal) (1215 / 436 * Math.Sqrt(2) * 0.985)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_10K6_8v_XVI.xlsx"
						, ColName = "col. 16"
						, ImageFileName = "MS-TS-00010-K-00006-000-00027.jp2"
						, SqeImageId = 127502
						, Scale = (decimal) 1215 / 590
						,
				}
/*
				, new SourceFileInfo()
				{
						FileName = "0_0_16.311_1r_XIX.xlsx"
						, ColName = "col. 19"
						, ImageFileName = "MS-TS-00016-00311-000-00001.jp2"
						, SqeImageId = 127503
						, OffsetX = 943
						, OffsetY = 232
						, Rotate = 2.05
						, Scale = (decimal) (1215 / 598 * 1.0155)
						,
				}
				, new SourceFileInfo()
				{
						FileName = "0_0_16.311_1v_XX.xlsx"
						, ColName = "col. 20"
						, ImageFileName = "MS-TS-00016-00311-000-00002.jp2"
						, SqeImageId = 127504
						, OffsetX = 490
						, OffsetY = 520
						, Rotate = 0.85
						, MidpointX = 0
						, Midpointy = 8631
						, Scale = (decimal) (1215 / 602 * 1.01)
						,
				} */
				,
		};

		private static void Main()
		{
			const bool reset = false;
			const string oldScrollName = "CD";
			const string newScrollName = "CD A_2";

			var additionalLines = new List<Line>();


			var sqeApi = new SqeApi(2);

			var edition = sqeApi.GetEdition(oldScrollName, newScrollName, reset);

			Console.WriteLine(
					$"Edition {edition.ScrollName} with id {edition.EditionId} loaded.\n");

			//	sourceFiles.Reverse();

			foreach (var sourceFile in sourceFiles)
			{
				if (!sourceFile.ColName.Equals("col. 15"))
					continue;

				Line sqeLine = null;
				Console.WriteLine(sourceFile.Scale);
				Console.WriteLine(sourceFile.OffsetX);

				Console.WriteLine($"\n\n\n{sourceFile.FileName} => {sourceFile.ColName}");

				sourceFile.ArtefactId = sqeApi.GetArtefactId(
						sourceFile.SqeImageId
						, sourceFile.ColName);

				if (sourceFile.ArtefactId == 0)
				{
					Console.WriteLine($"Creating Artefact for ${sourceFile.ColName}");
					sourceFile.ArtefactId = sqeApi.CreateArtefact(sourceFile);
				}

				Console.WriteLine(sourceFile.ArtefactId);

				var source = new ExcelData("ExcelFiles/", sourceFile);

				List<uint> previousSignInterpretationIds;

				foreach (var sourceLine in source.getLines())
				{
					var lineName = sourceLine.LineName;

				//				if (!lineName.Equals("17"))
				//									continue;

					// We add from the last line the last Id as AnchorAfter to the last
					// line to be added
					if (additionalLines.Count > 0
						&& additionalLines.Last().AnchorAfter == 0
						&& sqeLine != null)
						additionalLines.Last().AnchorAfter = sqeLine.StartAnchorId;

					var sqeSourceLine = sourceLine.asSqeLine();

					var newSqeLine = edition.GetLine(sourceFile.ColName, lineName);

					if (newSqeLine == null)
					{
						Console.WriteLine(
								$"\nLine {sourceLine.LineName} not found - will be added\n");

						if (sqeLine != null)
						{
							var fragmentId = edition.GetFragmentId(sourceFile.ColName);

							sqeApi.InsertLine(
									fragmentId
									, sqeSourceLine.LineData
									, sqeLine.GetLineId());
						}
						else
						{
							var fragmentId = edition.GetFragmentId(sourceFile.ColName);
							sqeApi.PrependLine(fragmentId, sqeSourceLine.LineData);
						}

						continue;
					}

					sqeLine = newSqeLine;

					Console.Write($"Line: {lineName}: ");

					var changeIdsList = SqeComparer.Compare(sqeLine, sqeSourceLine);

					//	previousSignInterpretationIds = sqeLine.GetFirstAnchors();
					List<uint> nextSignInterpretationIds;
					var lastSignData = sqeLine.FirstAnchor;
					var sqeSignData = sqeLine.FirstAnchor;

					// Start to write the data by running through the hints given in changeIdsList
					foreach (var changeIds in changeIdsList)
					{
						// We have a sqeId
						if (changeIds.SqeId != null)
						{
							// Get the sqe sign
							sqeSignData = sqeLine.GetSignDataBySignInterpretationId(
									changeIds.SqeId.Value);

							//We have also a source id - thus we have to adjust the data
							// of the sqe sign
							if (changeIds.SourceId != null)
							{
								var sourceSignData =
										sqeSourceLine.GetSignDataBySignInterpretationId(
												changeIds.SourceId.Value);

								sourceSignData.SignId = sqeSignData.SignId;

								var sqeSignInterpretations =sqeSignData.SignInterpretations.ToList();

								var remainingSourceSignInterpretations = new List<SignInterpretationData>();

								foreach (var sourceSignInterpretation in sourceSignData
																		 .SignInterpretations
																		 .ToList())
								{
									// Try to find an existing sign interpretation with the same character.
									var sqeSignInterpretation = sqeSignInterpretations.Find(
											s => s.Character.Equals(
													sourceSignInterpretation.Character));

									// If such a sign interpretation had been found
									if (sqeSignInterpretation != null)
									{
										// transfer the SignInterpretationId to the new sign
										sourceSignInterpretation.SignInterpretationId =
												sqeSignInterpretation.SignInterpretationId;
										// Update the Signinterpretation with the new sign data
										sqeApi.UpdateSignInterpretationsData(sourceSignInterpretation);

										// Take the old sign interpretion out of the list
										sqeSignInterpretations.Remove(sqeSignInterpretation);
									}
									else
									{
										// store the non processed new sign interpretation
										remainingSourceSignInterpretations.Add(
												sourceSignInterpretation);
									}
								}


								if (remainingSourceSignInterpretations.Count > 0)
								{
									var newSignInterpetations = sqeApi.AddSignInterpretations(
											sourceSignData.SignId.GetValueOrDefault()
											, remainingSourceSignInterpretations
											, _getSignInterpretationIds(lastSignData)
											, _getNextSignInterpretationIds(sqeSignData)
											);
									sqeSignData.SignInterpretations.AddRange(newSignInterpetations);
								}

								sqeApi.DeleteSignInterpretations(sqeSignInterpretations);
								foreach (var sqeSI in sqeSignInterpretations)
									sqeSignData.SignInterpretations.Remove(sqeSI);

								lastSignData = sqeSignData;

							}
							else // We don't have a source id thus delete the sign.
							{
								sqeApi.DeleteSign(sqeSignData.SignId.GetValueOrDefault());

								foreach (var signInterpretation in lastSignData.SignInterpretations)
								{
									signInterpretation.NextSignInterpretations.Clear();

									foreach (var sqeSi in sqeSignData.SignInterpretations)
									{
										signInterpretation.NextSignInterpretations.AddRange(
												sqeSi.NextSignInterpretations);
									}
								}

								Console.WriteLine(
										$"Delete sign with id {sqeSignData.SignId.GetValueOrDefault()}");
							}
						}

						else // We don't have a sqe id, thus we have to add the sign from the source
						{
							var sourceSignData =
									sqeSourceLine.GetSignDataBySignInterpretationId(
											changeIds.SourceId.GetValueOrDefault());

							var newSignData = sqeApi.CreateSigns(
									sqeLine.GetLineId()
									, new List<SignData>() { sourceSignData }
									, _getSignInterpretationIds(lastSignData)
									, _getNextSignInterpretationIds(lastSignData)
									, sourceFile.ArtefactId);

							foreach (var si in newSignData.SignInterpretations)
							{
								si.NextSignInterpretations.Clear();

								foreach (var sii in lastSignData.SignInterpretations)
								{
									si.NextSignInterpretations.AddRange(
											sii.NextSignInterpretations);
								}
							}

							lastSignData = newSignData;

							Console.WriteLine("Added");
						}
					}
				}
			}
		}

		private static List<uint> _getSignInterpretationIds(SignData signData)
		{
			return signData.SignInterpretations
						   .Select(s => s.SignInterpretationId.GetValueOrDefault())
						   .ToList();
		}

		private static List<uint> _getNextSignInterpretationIds(SignData signData)
		{
			var result = new List<uint>();

			foreach (var si in signData.SignInterpretations)
				result.AddRange(si.NextSignInterpretations.Select(s => s.NextSignInterpretationId));

			return result;
		}
	}
}
