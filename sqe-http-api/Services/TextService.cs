using System.Linq;
using System.Threading.Tasks;
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
            private readonly ITextRepository _repo;
        
        
            public TextService(ITextRepository repo)
            {
                _repo = repo;
            }
            
             public async Task<LineTextDTO> GetLineByIdAsync(UserInfo user, uint lineId)
            {
                var editionLine = await _repo.GetLineByIdAsync(user, lineId);
                if (editionLine.manuscriptId == 0) 
                    throw new StandardErrors.DataNotFound("line", lineId, "line_id");
                return _textEditionLineToDTO(editionLine);
            }

            public async Task<TextEditionDTO> GetFragmentByIdAsync(UserInfo user, uint fragmentId)
            {
                var edition = await _repo.GetTextFragmentByIdAsync(user, fragmentId);
                if (edition.manuscriptId == 0) // TODO: describe missing data better here.
                    throw new StandardErrors.DataNotFound("text fragment", fragmentId, "text_fragment_id");
                return _textEditionToDTO(edition);
            }

            public async Task<LineDataListDTO> GetLineIdsAsync(UserInfo user, uint fragmentId)
            {
                return new LineDataListDTO((await _repo.GetLineIdsAsync(user, fragmentId))
                    .Select(x => new LineDataDTO(x.lineId, x.lineName)).ToList());
            }

            public async Task<TextFragmentDataListDTO> GetFragmentDataAsync(UserInfo user)
            {
                return new TextFragmentDataListDTO(
                    (await _repo.GetFragmentDataAsync(user))
                        .Select(x => new TextFragmentDataDTO(x.ColId, x.ColName)).ToList()
                    );
            }

            private static TextEditionDTO _textEditionToDTO(TextEdition ed)
            {
                return new TextEditionDTO()
                {
                    licence = ed.licence,
                    manuscriptId = ed.manuscriptId,
                    editionName = ed.editionName,
                    editorId = ed.manuscriptAuthor,

                    textFragments = ed.fragments.Select(
                        x => new TextFragmentDTO()
                        {
                            textFragmentId = x.textFragmentId,
                            textFragmentName = x.fragment,
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
                                            signId = z.signId,
                                            nextSignIds = z.nextSignIds.Split(',').Select(int.Parse).ToList(),
                                            editorId = z.signSequenceAuthor,
                                            
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
                                                            artefactId = b.ArtefactId,
                                                            shape = b.Shape,
                                                            position = b.Position,
                                                            exceptional = b.Exceptional,
                                                            valuesSet = b.ValuesSet
                                                        }).ToList()
                                                    
                                                }).ToList()
                                            
                                        }).ToList()

                                }).ToList()

                        }).ToList()
                };
            }
            
            private static LineTextDTO _textEditionLineToDTO(TextEdition ed)
            {
                return new LineTextDTO()
                {
                    licence = ed.licence,
                    lineId = ed.fragments.First().lines.First().lineId,
                    lineName = ed.fragments.First().lines.First().line,
                    editorId = ed.fragments.First().lines.First().lineAuthor,
                    signs = ed.fragments.First().lines.First().signs.Select(
                        z => new SignDTO()
                        {
                            signId = z.signId,
                            nextSignIds = z.nextSignIds.Split(',').Select(int.Parse).ToList(),
                            editorId = z.signSequenceAuthor,
                            
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
                                            artefactId = b.ArtefactId,
                                            shape = b.Shape,
                                            position = b.Position,
                                            exceptional = b.Exceptional,
                                            valuesSet = b.ValuesSet
                                        }).ToList()
                                    
                                }).ToList()
                            
                        }).ToList()
                };
            }
        }
    }
