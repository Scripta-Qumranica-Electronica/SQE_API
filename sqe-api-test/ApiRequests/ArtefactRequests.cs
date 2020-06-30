using System.Collections.Generic;
using SQE.API.DTO;

namespace SQE.ApiTest.ApiRequests
{
    public static partial class Get
    {
        public class V1_Editions_EditionId_Artefacts : EditionRequestObject<EmptyInput, ArtefactListDTO, EmptyOutput>
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

        public class V1_Editions_EditionId_Artefacts_ArtefactId : ArtefactRequestObject<EmptyInput, ArtefactDTO, EmptyOutput>
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
            : ArtefactRequestObject<EmptyInput, InterpretationRoiDTOList, EmptyOutput>
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
            : ArtefactRequestObject<EmptyInput, TextFragmentDataListDTO, EmptyOutput>
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
            : ArtefactRequestObject<EmptyInput, TextFragmentDataListDTO, EmptyOutput>
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

        public class
            V1_Editions_EditionId_ArtefactGroups : EditionRequestObject<EmptyInput, ArtefactGroupListDTO, EmptyOutput>
        {
            public V1_Editions_EditionId_ArtefactGroups(uint editionId) : base(editionId, null) { }
        }
    }

    public static partial class Post
    {
        public class V1_Editions_EditionId_Artefacts : EditionRequestObject<CreateArtefactDTO, ArtefactDTO, ArtefactDTO>
        {
            public V1_Editions_EditionId_Artefacts(uint editionId, CreateArtefactDTO payload) : base(
                editionId,
                null,
                payload
            )
            {
                listenerMethod.Add("CreatedArtefact");
            }
        }

        public class
            V1_Editions_EditionId_ArtefactGroups : EditionRequestObject<CreateArtefactGroupDTO, ArtefactGroupDTO, ArtefactGroupDTO>
        {
            public V1_Editions_EditionId_ArtefactGroups(uint editionId, CreateArtefactGroupDTO payload) : base(
                editionId, null, payload)
            {
                listenerMethod.Add("CreatedArtefactGroup");
            }
        }
    }

    public static partial class Put
    {
        public class V1_Editions_EditionId_Artefacts_ArtefactId : ArtefactRequestObject<UpdateArtefactDTO, ArtefactDTO, ArtefactDTO>
        {
            public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId, uint artefactId, UpdateArtefactDTO payload) : base(
                editionId,
                artefactId,
                payload
            )
            {
                listenerMethod.Add("UpdatedArtefact");
            }
        }

        public class V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId : ArtefactGroupRequestObject<UpdateArtefactGroupDTO, ArtefactGroupDTO, ArtefactGroupDTO>
        {
            public V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(uint editionId, uint artefactGroupId, UpdateArtefactGroupDTO payload) : base(
                editionId,
                artefactGroupId,
                payload
            )
            {
                listenerMethod.Add("UpdatedArtefactGroup");
            }
        }
    }

    public static partial class Delete
    {
        public class V1_Editions_EditionId_Artefacts_ArtefactId : ArtefactRequestObject<EmptyInput, EmptyOutput, EmptyOutput>
        {
            public V1_Editions_EditionId_Artefacts_ArtefactId(uint editionId, uint artefactId) : base(
                editionId,
                artefactId,
                null
            )
            {
                listenerMethod.Add("DeletedArtefact");
            }
        }

        public class V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId : ArtefactGroupRequestObject<EmptyInput, DeleteDTO, DeleteDTO>
        {
            public V1_Editions_EditionId_ArtefactGroups_ArtefactGroupId(uint editionId, uint artefactGroupId) : base(
                editionId,
                artefactGroupId,
                null
            )
            {
                listenerMethod.Add("DeletedArtefactGroup");
            }
        }
    }
}