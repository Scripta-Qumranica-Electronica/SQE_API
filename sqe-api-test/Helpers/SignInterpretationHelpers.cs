using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using SQE.API.DTO;
using SQE.ApiTest.ApiRequests;

namespace SQE.ApiTest.Helpers
{
	public static class SignInterpretationHelpers
	{
		/// <summary>
		///  Find a sign interpretation id in the edition
		/// </summary>
		/// <param name="editionId"></param>
		/// <returns></returns>
		public static async Task<SignInterpretationDTO> GetEditionSignInterpretation(
				uint         editionId
				, HttpClient client)
		{
			var textFragmentsRequest = new Get.V1_Editions_EditionId_TextFragments(editionId);

			await textFragmentsRequest.SendAsync(client, auth: true);
			var textFragments = textFragmentsRequest.HttpResponseObject;

			foreach (var textRequest in textFragments.textFragments.Select(
					tf => new Get.V1_Editions_EditionId_TextFragments_TextFragmentId(
							editionId
							, tf.id)))
			{
				await textRequest.SendAsync(client, auth: true);
				var text = textRequest.HttpResponseObject;

				foreach (var si in from ttf in text.textFragments
								   from tl in ttf.lines
								   from sign in tl.signs
								   from si in sign.signInterpretations
								   from att in si.attributes
								   where (att.attributeValueId == 1)
										 && !string.IsNullOrEmpty(si.character)
								   select si)
					return si;
			}

			throw new Exception(
					$"Edition {editionId} has no letters, this may be a problem the database.");
		}
	}
}
