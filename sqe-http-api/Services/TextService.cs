using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MailKit;
using SQE.SqeHttpApi.DataAccess;
using SQE.SqeHttpApi.DataAccess.Helpers;
using SQE.SqeHttpApi.DataAccess.Models;
using SQE.SqeHttpApi.Server.DTOs;

namespace SQE.SqeHttpApi.Server.Services
{
    public interface ITextService
    {
        Task<LineTextDTO> GetLineByIdAsync(UserInfo user, uint lineId);
        Task<TextEditionDTO> GetFragmentByIdAsync(UserInfo user, uint fragmentId);
        Task<LineDataListDTO> GetLineIdsAsync(UserInfo user, uint fragmentId);
        Task<TextFragmentDataListDTO> GetFragmentDataAsync(UserInfo user);
    }
    public class TextService : ITextService
        {
            private readonly ITextRepository _textRepo;
            private readonly IUserRepository _userRepo;
        
            public TextService(ITextRepository textRepo, IUserRepository userRepo)
            {
                _textRepo = textRepo;
                _userRepo = userRepo;
            }
            
            public async Task<LineTextDTO> GetLineByIdAsync(UserInfo user, uint lineId)
            {
                var editionEditors = _userRepo.GetEditionEditorsAsync(user.editionId ?? 0);
                var editionLine = await _textRepo.GetLineByIdAsync(user, lineId);
                if (editionLine.manuscriptId == 0) 
                    throw new StandardErrors.DataNotFound("line", lineId, "line_id");
                return _textEditionLineToDTO(editionLine, await editionEditors);
            }

            public async Task<TextEditionDTO> GetFragmentByIdAsync(UserInfo user, uint fragmentId)
            {
                var editionEditors = _userRepo.GetEditionEditorsAsync(user.editionId ?? 0);
                var edition = await _textRepo.GetTextFragmentByIdAsync(user, fragmentId);
                if (edition.manuscriptId == 0) // TODO: describe missing data better here.
                    throw new StandardErrors.DataNotFound("text textFragmentName", fragmentId, "text_fragment_id");
                return _textEditionToDTO(edition, await editionEditors);
            }

            public async Task<LineDataListDTO> GetLineIdsAsync(UserInfo user, uint fragmentId)
            {
                return new LineDataListDTO((await _textRepo.GetLineIdsAsync(user, fragmentId))
                    .Select(x => new LineDataDTO(x.lineId, x.lineName)).ToList());
            }

            public async Task<TextFragmentDataListDTO> GetFragmentDataAsync(UserInfo user)
            {
                return new TextFragmentDataListDTO(
                    (await _textRepo.GetFragmentDataAsync(user))
                        .Select(x => new TextFragmentDataDTO(x.TextFragmentId, x.TextFragmentName)).ToList()
                    );
            }

            private static TextEditionDTO _textEditionToDTO(TextEdition ed, List<EditorInfo> editors)
            {
                var editorList = new Dictionary<uint, EditorDTO>();
                foreach (var editor in editors)
                {
                    editorList.Add(editor.UserId, new EditorDTO()
                    {
                        forename = editor.Forename,
                        surname = editor.Surname,
                        organization = editor.Organization
                    });
                }
                
                // Check if this edition has a proper collaborators field, if not dynamically add
                // all edition editors to that field.
                if (string.IsNullOrEmpty(ed.collaborators))
                    ed.addLicence(editors);
                
                return new TextEditionDTO()
                {
                    editors = editorList,
                    licence = ed.licence,
                    manuscriptId = ed.manuscriptId,
                    editionName = ed.editionName,
                    editorId = ed.manuscriptAuthor,

                    textFragments = ed.fragments.Select(
                        x => new TextFragmentDTO()
                        {
                            textFragmentId = x.textFragmentId,
                            textFragmentName = x.textFragmentName,
                            editorId = x.textFragmentAuthor,

                            lines = x.lines.Select(
                                y => new LineDTO()
                                {
                                    lineId = y.lineId,
                                    lineName = y.line,
                                    editorId = y.lineAuthor,

                                    signs = y.signs.Select(
                                        z => new SignDTO()
                                        {
                                            signInterpretations = z.signInterpretations.Select(
                                                a => new SignInterpretationDTO()
                                                {
                                                    signInterpretationId = a.signInterpretationId,
                                                    signInterpretation = a.signInterpretation,
                                                    
                                                    attributes = a.attributes.Select(
                                                        b => new InterpretationAttributeDTO()
                                                        {
                                                            interpretationAttributeId = b.interpretationAttributeId,
                                                            attributeValueId = b.attributeValueId,
                                                            editorId = b.signInterpretationAttributeAuthor,
                                                            value = b.value
                                                        }).ToList(),
                                    
                                                    rois = a.signInterpretationRois.Select(
                                                        b => new InterpretationRoiDTO()
                                                        {
                                                            interpretationRoiId = b.SignInterpretationRoiId,
                                                            editorId = b.SignInterpretationRoiAuthor,
                                                            artefactId = b.ArtefactId,
                                                            shape = b.Shape,
                                                            position = b.Position,
                                                            exceptional = b.Exceptional,
                                                            valuesSet = b.ValuesSet
                                                        }).ToList()
                                                    
                                                }).ToList(),
                                            
                                            nextSignInterpretations = z.nextSignInterpretations.Select(
                                                b => new NextSignInterpretationDTO()
                                                {
                                                    nextSignInterpretationId = b.nextSignInterpretationId,
                                                    editorId = b.signSequenceAuthor
                                                }).ToList()
                                            
                                        }).ToList()

                                }).ToList()

                        }).ToList()
                };
            }
            
            private static LineTextDTO _textEditionLineToDTO(TextEdition ed, List<EditorInfo> editors)
            {
                
                var editorList = new Dictionary<uint, EditorDTO>();
                foreach (var editor in editors)
                {
                    editorList.Add(editor.UserId, new EditorDTO()
                    {
                        forename = editor.Forename,
                        surname = editor.Surname,
                        organization = editor.Organization
                    });
                }
                
                // Check if this edition has a proper collaborators field, if not dynamically add
                // all edition editors to that field.
                if (string.IsNullOrEmpty(ed.collaborators))
                    ed.addLicence(editors);
                
                return new LineTextDTO()
                {
                    editors = editorList,
                    licence = ed.licence,
                    lineId = ed.fragments.First().lines.First().lineId,
                    lineName = ed.fragments.First().lines.First().line,
                    editorId = ed.fragments.First().lines.First().lineAuthor,
                    signs = ed.fragments.First().lines.First().signs.Select(
                        z => new SignDTO()
                        {
                            signInterpretations = z.signInterpretations.Select(
                                a => new SignInterpretationDTO()
                                {
                                    signInterpretationId = a.signInterpretationId,
                                    signInterpretation = a.signInterpretation,
                                    
                                    attributes = a.attributes.Select(
                                        b => new InterpretationAttributeDTO()
                                        {
                                            interpretationAttributeId = b.interpretationAttributeId,
                                            attributeValueId = b.attributeValueId,
                                            editorId = b.signInterpretationAttributeAuthor,
                                            value = b.value
                                        }).ToList(),
                                    
                                    rois = a.signInterpretationRois.Select(
                                        b => new InterpretationRoiDTO()
                                        {
                                            interpretationRoiId = b.SignInterpretationRoiId,
                                            editorId = b.SignInterpretationRoiAuthor,
                                            artefactId = b.ArtefactId,
                                            shape = b.Shape,
                                            position = b.Position,
                                            exceptional = b.Exceptional,
                                            valuesSet = b.ValuesSet
                                        }).ToList()
                                    
                                }).ToList(),
                            
                            nextSignInterpretations = z.nextSignInterpretations.Select(
                                b => new NextSignInterpretationDTO()
                                {
                                    nextSignInterpretationId = b.nextSignInterpretationId,
                                    editorId = b.signSequenceAuthor
                                }).ToList(),
                            
                        }).ToList()
                };
            }
        }
    }
