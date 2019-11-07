using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions_EditionId_Artefacts : EditionRequestObject<EmptyInput, ArtefactListDTO>
        {
            /// <summary>
            ///     Request a list of artefacts by their editionId
            /// </summary>
            /// <param name="editionId">Id of the edition to search for artefacts</param>
            /// <param name="optional">List of optional parameters: "masks", "images"</param>
            public V1_Editions_EditionId_Artefacts(uint editionId, List<string> optional = null) : base(
                editionId,
                optional
            )
            {
            }
        }

        public class V1_Editions_EditionId_Artefacts_ArtefactId : ArtefactRequestObject<EmptyInput, ArtefactDTO>
        {
            public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId, uint artefactId) : base(
                editionId,
                artefactId,
                null
            )
            {
            }
        }

        public class V1_Editions_EditionId_Artefacts_ArtefactId_Rois
            : ArtefactRequestObject<EmptyInput, InterpretationRoiDTOList>
        {
            public V1_Editions_EditionId_Artefacts_ArtefactId_Rois(uint editionId, uint artefactId) : base(
                editionId,
                artefactId,
                null
            )
            {
            }
        }

        public class V1_Editions_EditionId_Artefacts_ArtefactId_TextFragments
            : ArtefactRequestObject<EmptyInput, TextFragmentDataListDTO>
        {
            public V1_Editions_EditionId_Artefacts_ArtefactId_TextFragments(uint editionId, uint artefactId) : base(
                editionId,
                artefactId,
                null
            )
            {
            }
        }

        public class V1_Editions_EditionId_Artefacts_ArtefactId_SuggestedTextFragments
            : ArtefactRequestObject<EmptyInput, TextFragmentDataListDTO>
        {
            public V1_Editions_EditionId_Artefacts_ArtefactId_SuggestedTextFragments(uint editionId, uint artefactId) :
                base(
                    editionId,
                    artefactId,
                    null
                )
            {
            }
        }
    }

    public static partial class Post
    {
        public class V1_Editions_EditionId_Artefacts : EditionRequestObject<CreateArtefactDTO, ArtefactDTO>
        {
            public V1_Editions_EditionId_Artefacts(uint editionId, CreateArtefactDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("createArtefact");
            }
        }
    }

    public static partial class Put
    {
        public class V1_Editions_EditionId_Artefacts_ArtefactId : ArtefactRequestObject<UpdateArtefactDTO, ArtefactDTO>
        {
            public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId, uint artefactId) : base(
                editionId,
                artefactId,
                null
            )
            {
                listenerMethod.Add("updateArtefact");
            }
        }
    }

    public static partial class Delete
    {
        public class V1_Editions_EditionId_Artefacts_ArtefactId : ArtefactRequestObject<EmptyInput, EmptyOutput>
        {
            public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId, uint artefactId) : base(
                editionId,
                artefactId,
                null
            )
            {
                listenerMethod.Add("deleteArtefact");
            }
        }
    }
}