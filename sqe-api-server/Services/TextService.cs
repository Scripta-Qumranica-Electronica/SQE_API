using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using SQE.API.DTO;
using SQE.API.Server.RealtimeHubs;
using SQE.DatabaseAccess;
using SQE.DatabaseAccess.Helpers;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Services
{
    public interface ITextService
    {
        Task<LineTextDTO> GetLineByIdAsync(EditionUserInfo editionUser, uint lineId);
        Task<TextEditionDTO> GetFragmentByIdAsync(EditionUserInfo editionUser, uint fragmentId);
        Task<ArtefactDataListDTO> GetArtefactsAsync(EditionUserInfo editionUser, uint fragmentId);
        Task<LineDataListDTO> GetLineIdsAsync(EditionUserInfo editionUser, uint fragmentId);
        Task<TextFragmentDataListDTO> GetFragmentDataAsync(EditionUserInfo editionUser);

        Task<TextFragmentDataDTO> CreateTextFragmentAsync(EditionUserInfo editionUser,
            CreateTextFragmentDTO createFragment,
            string clientId = null);
        Task<TextFragmentDataDTO> UpdateTextFragmentAsync(EditionUserInfo editionUser,
            uint textFragmentId,
            UpdateTextFragmentDTO updatedFragment,
            string clientId = null);
    }

    public class TextService : ITextService
    {
        private readonly IHubContext<MainHub> _hubContext;
        private readonly ITextRepository _textRepo;
        private readonly IUserRepository _userRepo;

        public TextService(ITextRepository textRepo, IUserRepository userRepo, IHubContext<MainHub> hubContext)
        {
            _textRepo = textRepo;
            _userRepo = userRepo;
            _hubContext = hubContext;
        }

        public async Task<LineTextDTO> GetLineByIdAsync(EditionUserInfo editionUser, uint lineId)
        {
            var editionEditors = _userRepo.GetEditionEditorsAsync(editionUser.EditionId);
            var editionLine = await _textRepo.GetLineByIdAsync(editionUser, lineId);
            if (editionLine.manuscriptId == 0)
                throw new StandardExceptions.DataNotFoundException("line", lineId, "line_id");
            return _textEditionLineToDTO(editionLine, await editionEditors);
        }

        public async Task<TextEditionDTO> GetFragmentByIdAsync(EditionUserInfo editionUser, uint fragmentId)
        {
            var editionEditors = _userRepo.GetEditionEditorsAsync(editionUser.EditionId);
            var edition = await _textRepo.GetTextFragmentByIdAsync(editionUser, fragmentId);
            if (edition.manuscriptId == 0) // TODO: describe missing data better here.
                throw new StandardExceptions.DataNotFoundException(
                    "text textFragmentName",
                    fragmentId,
                    "text_fragment_id"
                );
            return _textEditionToDTO(edition, await editionEditors);
        }

        public async Task<ArtefactDataListDTO> GetArtefactsAsync(EditionUserInfo editionUser, uint fragmentId)
        {
            return new ArtefactDataListDTO(
                (await _textRepo.GetArtefactsAsync(editionUser, fragmentId))
                .Select(x => new ArtefactDataDTO { id = x.ArtefactId, name = x.Name })
                .ToList()
            );
        }

        public async Task<LineDataListDTO> GetLineIdsAsync(EditionUserInfo editionUser, uint fragmentId)
        {
            return new LineDataListDTO(
                (await _textRepo.GetLineIdsAsync(editionUser, fragmentId))
                .Select(x => new LineDataDTO(x.LineId.GetValueOrDefault(), x.LineName))
                .ToList()
            );
        }

        public async Task<TextFragmentDataListDTO> GetFragmentDataAsync(EditionUserInfo editionUser)
        {
            return new TextFragmentDataListDTO(
                (await _textRepo.GetFragmentDataAsync(editionUser))
                .Select(x => new TextFragmentDataDTO(
                    x.TextFragmentId.GetValueOrDefault(), 
                    x.TextFragmentName, 
                    x.EditionEditorId.GetValueOrDefault()))
                .ToList()
            );
        }

        /// <summary>
        /// Create a new text fragment in an edition.
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="createFragment">Values for the new text fragment</param>
        /// <param name="clientId">SignalR client Id</param>
        /// <returns>Details of the newly created text fragment.
        /// TODO: decide if we will return info about the previous/next text fragment id's
        /// </returns>
        public async Task<TextFragmentDataDTO> CreateTextFragmentAsync(EditionUserInfo editionUser,
            CreateTextFragmentDTO createFragment,
            string clientId = null)
        {
            var fragmentData = new TextFragmentData(){TextFragmentName = createFragment.name};
            var newFragment = await _textRepo.CreateTextFragmentAsync(
                editionUser,
                fragmentData,
                createFragment.previousTextFragmentId,
                createFragment.nextTextFragmentId
            );

            var newTextFragmentData = new TextFragmentDataDTO(
                newFragment.TextFragmentId.GetValueOrDefault(),
                newFragment.TextFragmentName,
                newFragment.EditionEditorId.GetValueOrDefault()
            );
            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync("createTextFragment", newTextFragmentData);
            return newTextFragmentData;
        }

        /// <summary>
        /// Update the name and/or position of a text fragment
        /// </summary>
        /// <param name="editionUser">Edition user object</param>
        /// <param name="textFragmentId">Text fragment to be updated</param>
        /// <param name="updatedFragment">Details of the new values for the text fragment</param>
        /// <param name="clientId">SignalR client Id</param>
        /// <returns>Details of the updated text fragment.
        /// TODO: decide if we will return info about the previous/next text fragment id's
        /// </returns>
        public async Task<TextFragmentDataDTO> UpdateTextFragmentAsync(EditionUserInfo editionUser,
            uint textFragmentId,
            UpdateTextFragmentDTO updatedFragment,
            string clientId = null)
        {
            var newFragment = await _textRepo.UpdateTextFragmentAsync(
                editionUser,
                textFragmentId,
                updatedFragment.name,
                updatedFragment.previousTextFragmentId,
                updatedFragment.nextTextFragmentId
            );

            var newTextFragmentData = new TextFragmentDataDTO(
                newFragment.TextFragmentId.GetValueOrDefault(),
                newFragment.TextFragmentName,
                newFragment.EditionEditorId.GetValueOrDefault()
            );
            // Broadcast the change to all subscribers of the editionId. Exclude the client (not the user), which
            // made the request, that client directly received the response.
            await _hubContext.Clients.GroupExcept(editionUser.EditionId.ToString(), clientId)
                .SendAsync("createTextFragment", newTextFragmentData);
            return newTextFragmentData;
        }

        /// <summary>
        /// Serialize a TextEdition and the list of its editors to a TextEditionDTO.
        /// </summary>
        /// <param name="ed">Text edition to be serialized</param>
        /// <param name="editors">List of edition editors</param>
        /// <returns>A TextEditionDTO</returns>
        private static TextEditionDTO _textEditionToDTO(TextEdition ed, List<EditorInfo> editors)
        {
            var editorList = 
                editors.ToDictionary(editor => editor.UserId, 
                    editor => new EditorDTO
                    {
                        forename = editor.Forename, 
                        surname = editor.Surname, 
                        organization = editor.Organization
                    });

            // Check if this edition has a proper collaborators field, if not dynamically add
            // all edition editors to that field.
            if (string.IsNullOrEmpty(ed.collaborators))
                ed.AddLicence(editors);

            return new TextEditionDTO
            {
                editors = editorList,
                licence = ed.licence,
                manuscriptId = ed.manuscriptId,
                editionName = ed.editionName,
                editorId = ed.manuscriptAuthor,

                textFragments = ed.fragments.Select(
                        x => new TextFragmentDTO
                        {
                            textFragmentId = x.TextFragmentId.GetValueOrDefault(),
                            textFragmentName = x.TextFragmentName,
                            editorId = x.EditionEditorId.GetValueOrDefault(),

                            lines = x.Lines.Select(
                                    y => new LineDTO
                                    {
                                        lineId = y.LineId.GetValueOrDefault(),
                                        lineName = y.LineName,
                                        editorId = y.LineAuthor.GetValueOrDefault(),

                                        signs = y.Signs.Select(
                                                z => new SignDTO
                                                {
                                                    signInterpretations = z.SignInterpretations.Select(
                                                            a => new SignInterpretationDTO
                                                            {
                                                                signInterpretationId = a.SignInterpretationId.GetValueOrDefault(),
                                                                character = a.Character,

                                                                attributes = a.Attributes.Select(
                                                                        b => new InterpretationAttributeDTO
                                                                        {
                                                                            interpretationAttributeId =
                                                                                b.SignInterpretationAttributeId.GetValueOrDefault(),
                                                                            sequence = b.Sequence.GetValueOrDefault(),
                                                                            attributeValueId = b.AttributeValueId.GetValueOrDefault(),
                                                                            attributeValueString = b.AttributeString,
                                                                            editorId = b
                                                                                .SignInterpretationAttributeAuthor.GetValueOrDefault(),
                                                                            value = b.NumericValue.GetValueOrDefault()
                                                                        }
                                                                    )
                                                                    .ToList(),

                                                                rois = a.SignInterpretationRois.Select(
                                                                        b => new InterpretationRoiDTO
                                                                        {
                                                                            interpretationRoiId =
                                                                                b.SignInterpretationRoiId.GetValueOrDefault(),
                                                                            signInterpretationId =
                                                                                b.SignInterpretationId,
                                                                            editorId = b.SignInterpretationRoiAuthor.GetValueOrDefault(),
                                                                            artefactId = b.ArtefactId.GetValueOrDefault(),
                                                                            shape = b.Shape,
                                                                            translate = new TranslateDTO
                                                                            {
                                                                                x = b.TranslateX.GetValueOrDefault(),
                                                                                y = b.TranslateY.GetValueOrDefault()
                                                                            },
                                                                            exceptional = b.Exceptional.GetValueOrDefault(),
                                                                            valuesSet = b.ValuesSet.GetValueOrDefault()
                                                                        }
                                                                    )
                                                                    .ToList(),

                                                                nextSignInterpretations = a.NextSignInterpretations
                                                                    .Select(
                                                                        b => new NextSignInterpretationDTO
                                                                        {
                                                                            nextSignInterpretationId =
                                                                                b.NextSignInterpretationId,
                                                                            editorId = b.SignSequenceAuthor
                                                                        }
                                                                    )
                                                                    .ToList()
                                                            }
                                                        )
                                                        .ToList()
                                                }
                                            )
                                            .ToList()
                                    }
                                )
                                .ToList()
                        }
                    )
                    .ToList()
            };
        }

        private static LineTextDTO _textEditionLineToDTO(TextEdition ed, List<EditorInfo> editors)
        {
            var editorList = 
                editors.ToDictionary(
                    editor => editor.UserId, 
                    editor => new EditorDTO
                    {
                        forename = editor.Forename, 
                        surname = editor.Surname, 
                        organization = editor.Organization
                    });

            // Check if this edition has a proper collaborators field, if not dynamically add
            // all edition editors to that field.
            if (string.IsNullOrEmpty(ed.collaborators))
                ed.AddLicence(editors);

            return new LineTextDTO
            {
                editors = editorList,
                licence = ed.licence,
                lineId = ed.fragments.First().Lines.First().LineId.GetValueOrDefault(),
                lineName = ed.fragments.First().Lines.First().LineName,
                editorId = ed.fragments.First().Lines.First().LineAuthor.GetValueOrDefault(),
                signs = ed.fragments.First()
                    .Lines.First()
                    .Signs.Select(
                        z => new SignDTO
                        {
                            signInterpretations = z.SignInterpretations.Select(
                                    a => new SignInterpretationDTO
                                    {
                                        signInterpretationId = a.SignInterpretationId.GetValueOrDefault(),
                                        character = a.Character,

                                        attributes = a.Attributes.Select(
                                                b => new InterpretationAttributeDTO
                                                {
                                                    interpretationAttributeId = b.SignInterpretationAttributeId.GetValueOrDefault(),
                                                    sequence = b.Sequence.GetValueOrDefault(),
                                                    attributeValueId = b.AttributeValueId.GetValueOrDefault(),
                                                    attributeValueString = b.AttributeString,
                                                    editorId = b.SignInterpretationAttributeAuthor.GetValueOrDefault(),
                                                    value = b.NumericValue.GetValueOrDefault()
                                                }
                                            )
                                            .ToList(),

                                        rois = a.SignInterpretationRois.Select(
                                                b => new InterpretationRoiDTO
                                                {
                                                    interpretationRoiId = b.SignInterpretationRoiId.GetValueOrDefault(),
                                                    signInterpretationId = b.SignInterpretationId.GetValueOrDefault(),
                                                    editorId = b.SignInterpretationRoiAuthor.GetValueOrDefault(),
                                                    artefactId = b.ArtefactId.GetValueOrDefault(),
                                                    shape = b.Shape,
                                                    translate = new TranslateDTO
                                                    {
                                                        x = b.TranslateX.GetValueOrDefault(),
                                                        y = b.TranslateY.GetValueOrDefault()
                                                    },
                                                    exceptional = b.Exceptional.GetValueOrDefault(),
                                                    valuesSet = b.ValuesSet.GetValueOrDefault()
                                                }
                                            )
                                            .ToList(),

                                        nextSignInterpretations = a.NextSignInterpretations.Select(
                                                b => new NextSignInterpretationDTO
                                                {
                                                    nextSignInterpretationId = b.NextSignInterpretationId,
                                                    editorId = b.SignSequenceAuthor
                                                }
                                            )
                                            .ToList()
                                    }
                                )
                                .ToList()
                        }
                    )
                    .ToList()
            };
        }
    }
}