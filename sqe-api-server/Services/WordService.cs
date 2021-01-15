using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.API.Server.Serialization;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
	public interface IWordService
	{
		Task<QwbWordVariantListDTO> GetQwbWordVariantForSignInterpretationId(
				UserInfo user
				, uint   editionId
				, uint   signInterpretationId);

		Task<QwbWordVariantListDTO>   GetQwbWordVariantForQwbWordId(uint qwbWordId);
		Task<QwbParallelListDTO>      GetQwbParallel(uint qwbStartWordId, uint qwbEndWordId);
		Task<QwbBibliographyEntryDTO> GetQwbBibliography(uint bibliographyId);
	}

	public class WordService : IWordService
	{
		private static readonly string _qwbHttpAPIAddress = @"https://vmext21-116.gwdg.de/qwbSQE";

		private readonly HttpClient                       _httpClient;
		private readonly IHubContext<MainHub, ISQEClient> _hubContext;

		private readonly string _qwbBibliographyAddress =
				$@"{_qwbHttpAPIAddress}/bibliography.xml\?bibId=";

		private readonly string _qwbWordParallelsAddress = $@"{
					_qwbHttpAPIAddress
				}/parallels.xml\?startWordId=$StartWordId&endWordId=$EndWordId";

		private readonly string _qwbWordVariantsAddress =
				$@"{_qwbHttpAPIAddress}/variants.xml\?wortId=";

		private readonly ISignInterpretationRepository _signInterpretationRepository;

		public WordService(
				IHubContext<MainHub, ISQEClient> hubContext
				, ISignInterpretationRepository  signInterpretationRepository
				, HttpClient                     httpClient)
		{
			_hubContext = hubContext;
			_signInterpretationRepository = signInterpretationRepository;
			_httpClient = httpClient;
		}

		public async Task<QwbWordVariantListDTO> GetQwbWordVariantForSignInterpretationId(
				UserInfo user
				, uint   editionId
				, uint   signInterpretationId)
		{
			var qwbWordId =
					await _signInterpretationRepository.GetQwbWordIfForSignInterpretationId(
							user
							, editionId
							, signInterpretationId);

			if (qwbWordId == 0)
			{
				throw new StandardExceptions.DataNotFoundException(
						"QWB word id"
						, signInterpretationId.ToString()
						, "sign interpretation to qwb word id");
			}

			return await GetQwbWordVariantForQwbWordId(qwbWordId);
		}

		public async Task<QwbWordVariantListDTO> GetQwbWordVariantForQwbWordId(uint qwbWordId)
		{
			try
			{
				var wordVariantString =
						await _httpClient.GetStringAsync($"{_qwbWordVariantsAddress}{qwbWordId}");

				var variants = JsonSerializer.Deserialize<QwbWordVariants>(wordVariantString);

				return variants.ToDTO();
			}
			catch
			{
				// TODO: maybe we do better error checking to see why the HTTP request failed.
				throw new StandardExceptions.DataNotFoundException(
						"QWB word variants"
						, qwbWordId.ToString()
						, "QWB API");
			}
		}

		public async Task<QwbParallelListDTO> GetQwbParallel(uint qwbStartWordId, uint qwbEndWordId)
		{
			try
			{
				var wordVariantString = await _httpClient.GetStringAsync(
						_qwbWordParallelsAddress.Replace("$StartWordId", qwbStartWordId.ToString())
												.Replace("$EndWordId", qwbEndWordId.ToString()));

				var variants = JsonSerializer.Deserialize<List<QwbParallel>>(wordVariantString);

				return variants.ToDTO();
			}
			catch
			{
				// TODO: maybe we do better error checking to see why the HTTP request failed.
				throw new StandardExceptions.DataNotFoundException(
						"QWB parallels"
						, $"{qwbStartWordId} and {qwbEndWordId}"
						, "QWB API");
			}
		}

		public async Task<QwbBibliographyEntryDTO> GetQwbBibliography(uint bibliographyId)
		{
			try
			{
				var wordVariantString =
						await _httpClient.GetStringAsync(
								$"{_qwbBibliographyAddress}{bibliographyId}");

				var entry = JsonSerializer.Deserialize<QwbBibliographyEntry>(wordVariantString);

				return entry.ToDTO();
			}
			catch
			{
				// TODO: maybe we do better error checking to see why the HTTP request failed.
				throw new StandardExceptions.DataNotFoundException(
						"QWB bibliography"
						, $"{bibliographyId}"
						, "QWB API");
			}
		}
	}

	// The following are the classes needed to deserialize JSON from QWB API endpoints
	public class QwbWordVariants
	{
		public QwbBibliographyList        noVariants { get; set; }
		public List<QwbWordVariantObject> variants   { get; set; }
	}

	public class QwbBibliographyList
	{
		public List<QwbBiblio> biblio { get; set; }
	}

	public class QwbWordVariantObject : QwbBibliographyList
	{
		public string type    { get; set; }
		public string word    { get; set; }
		public string lemma   { get; set; }
		public string grammar { get; set; }
		public string meaning { get; set; }
	}

	public class QwbBiblio
	{
		public uint   id         { get; set; }
		public string shortTitle { get; set; }
		public string pageRef    { get; set; }
		public string commentary { get; set; }
	}

	public class QwbParallelWord
	{
		public bool   isVariant       { get; set; }
		public bool   isReconstructed { get; set; }
		public int    wordId          { get; set; }
		public int    relatedWordId   { get; set; }
		public string word            { get; set; }
	}

	public class QwbParallel
	{
		public string                textref { get; set; }
		public List<QwbParallelWord> words   { get; set; }
	}

	public class QwbBibliographyEntry
	{
		public uint   id    { get; set; }
		public string title { get; set; }
	}
}
