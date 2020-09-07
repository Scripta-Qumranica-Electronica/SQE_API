using System.Collections.Generic;
using System.Linq;
using NetTopologySuite.Geometries.Utilities;
using NetTopologySuite.IO;
using NetTopologySuite.Operation.Union;
using SQE.API.DTO;
using SQE.DatabaseAccess.Models;

namespace SQE.API.Server.Serialization
{
    public static partial class ExtensionsDTO
    {
        /// <summary>
        ///     Outputs a ScriptTextFragmentDTO
        /// </summary>
        /// <param name="stf"></param>
        /// <returns></returns>
        public static ScriptTextFragmentDTO ToDTO(this ScriptTextFragment stf)
        {
            return new ScriptTextFragmentDTO
            {
                textFragmentId = stf.TextFragmentId,
                textFragmentName = stf.TextFragmentName,
                lines = stf.Lines != null ?
                    stf.Lines.Select(x => x.ToDTO()).ToList() :
                    new List<ScriptLineDTO>()
            };
        }

        /// <summary>
        ///     Outputs a ScriptLineDTO
        /// </summary>
        /// <param name="sl"></param>
        /// <returns></returns>
        public static ScriptLineDTO ToDTO(this ScriptLine sl)
        {
            return new ScriptLineDTO
            {
                lineId = sl.LineId,
                lineName = sl.LineName,
                artefacts = sl.Artefacts.Select(x => x.ToDTO()).ToList()
            };
        }

        /// <summary>
        ///     Outputs a ScriptArtefactCharactersDTO
        /// </summary>
        /// <param name="sac"></param>
        /// <returns></returns>
        public static ScriptArtefactCharactersDTO ToDTO(this ScriptArtefactCharacters sac)
        {
            // TODO: Check on the thread safety of WKTWriter and WKBReader (maybe we can make this more efficient).
            var wkw = new WKTWriter();
            var wbr = new WKBReader();
            return new ScriptArtefactCharactersDTO
            {
                artefactId = sac.ArtefactId,
                artefactName = sac.ArtefactName,
                placement = sac.PlacementDTO(),
                characters = sac.Characters.Select(x => new SignInterpretationDTO
                {
                    signInterpretationId = x.SignInterpretationId,
                    character = x.SignInterpretationCharacter.ToString(),
                    attributes = x.Attributes.Select(y => y.ToDTO()).ToArray(),
                    nextSignInterpretations = x.NextCharacters.Select(z => z.ToDTO()).ToArray(),
                    rois = x.Rois.Take(1).Select(a => new InterpretationRoiDTO
                    {
                        artefactId = sac.ArtefactId,
                        interpretationRoiId = a.SignInterpretationRoiId,
                        signInterpretationId = x.SignInterpretationId,
                        stanceRotation = a.RoiRotate,
                        translate = null,
                        // Gather all the ROI shapes into a single (multi)polygon
                        shape = wkw.Write(
                            new CascadedPolygonUnion(x.Rois
                                .Select(h =>
                                {
                                    var poly = wbr.Read(h.RoiShape);
                                    // Each individual ROI should have its translate applied
                                    var tr = new AffineTransformation();
                                    tr.Translate(h.RoiTranslateX, h.RoiTranslateY);
                                    return tr.Transform(poly);
                                })
                                // TODO: make sure all values are valid in the database, then probably remove this check
                                .Where(i => i.IsValid && !i.IsEmpty)
                                .ToList()).Union()) // Union applies the CascadedPolygonUnion
                    }).ToArray() // The rois attribute must be a list
                }).ToList() // The characters attribute is a list
            };
        }

        /// <summary>
        ///     Outputs a TransformationDTO
        /// </summary>
        /// <param name="sac"></param>
        /// <returns></returns>
        public static PlacementDTO PlacementDTO(this ScriptArtefactCharacters sac)
        {
            return new PlacementDTO
            {
                // Use default values if a null was passed
                rotate = sac.ArtefactRotate ?? 0,
                scale = sac.ArtefactScale ?? 1,
                zIndex = sac.ArtefactZIndex ?? 0,

                // The x and y translate may be null, but sent the DTO in any event
                translate = sac.ArtefactTranslateX.HasValue && sac.ArtefactTranslateY.HasValue
                    ? new TranslateDTO
                    {
                        x = sac.ArtefactTranslateX.Value,
                        y = sac.ArtefactTranslateY.Value
                    }
                    : null
            };
        }

        /// <summary>
        ///     Outputs an InterpretationAttributeDTO
        /// </summary>
        /// <param name="ca"></param>
        /// <returns></returns>
        public static InterpretationAttributeDTO ToDTO(this CharacterAttribute ca)
        {
            return new InterpretationAttributeDTO
            {
                attributeValueId = ca.SignInterpretationAttributeId,
                // String the two attributes together for custom CSS directives
                attributeValueString = $"{ca.AttributeName}_{ca.AttributeValue}"
            };
        }

        /// <summary>
        ///     Outputs a NextSignInterpretationDTO
        /// </summary>
        /// <param name="csp"></param>
        /// <returns></returns>
        public static NextSignInterpretationDTO ToDTO(this CharacterStreamPosition csp)
        {
            return new NextSignInterpretationDTO
            {
                nextSignInterpretationId = csp.NextSignInterpretationId
            };
        }
    }
}