using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using SQE.SqeApi.Server.DTOs;

namespace SQE.SqeApi.Server.Hubs
{
    public partial class MainHub : Hub
    {
        /// <summary>
        /// Creates a new text fragment in the given edition of a scroll
        /// </summary>
        /// <param name="createFragment">A JSON object with the details of the new text fragment to be created</param>
        /// <param name="editionId">Id of the edition</param>
        [Authorize]
        public async Task<TextFragmentDataDTO> PostV1EditionsEditionIdTextFragments(
            CreateTextFragmentDTO createFragment, uint editionId)
        {
            return await _textService.CreateTextFragmentAsync(
                _userService.GetCurrentUserObject(editionId),
                createFragment,
                clientId: Context.ConnectionId);
        }

        /// <summary>
        /// Retrieves the ids of all fragments in the given edition of a scroll
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <returns>An array of the text fregment ids in correct sequence</returns>
        [AllowAnonymous]
        public async Task<TextFragmentDataListDTO> GetV1EditionsEditionIdTextFragments(uint editionId)
        {
            return await _textService.GetFragmentDataAsync(_userService.GetCurrentUserObject(editionId));
        }

        /// <summary>
        /// Retrieves the ids of all lines in the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>An array of the line ids in the proper sequence</returns>
        [AllowAnonymous]
        public async Task<LineDataListDTO> GetV1EditionsEditionIdTextFragmentsTextFragmentIdLines(uint editionId,
            uint textFragmentId)
        {
            return await _textService.GetLineIdsAsync(
                _userService.GetCurrentUserObject(editionId),
                textFragmentId);
        }

        /// <summary>
        /// Retrieves all signs and their data from the given textFragmentName
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="textFragmentId">Id of the text fragment</param>
        /// <returns>A manuscript edition object including the fragments and their lines in a hierarchical order and in correct sequence</returns>
        [AllowAnonymous]
        public async Task<TextEditionDTO> GetV1EditionsEditionIdTextFragmentsTextFragmentId(uint editionId,
            uint textFragmentId)
        {
            return await _textService.GetFragmentByIdAsync(
                _userService.GetCurrentUserObject(editionId),
                textFragmentId);
        }

        /// <summary>
        /// Retrieves all signs and their data from the given line
        /// </summary>
        /// <param name="editionId">Id of the edition</param>
        /// <param name="lineId">Id of the line</param>
        /// <returns>A manuscript edition object including the fragments and their lines in a hierarchical order and in correct sequence</returns>
        [AllowAnonymous]
        public async Task<LineTextDTO> GetV1EditionsEditionIdLinesLineId(uint editionId, uint lineId)
        {
            return await _textService.GetLineByIdAsync(
                _userService.GetCurrentUserObject(editionId),
                lineId);
        }
    }
}