using System.Collections.Generic;
using System.Linq;
using SQE.API.DTO;
using SQE.API.Server.Services;

namespace SQE.API.Server.Serialization;

public static partial class ExtensionsDTO
{
	public static QwbParallelListDTO ToDTO(this IEnumerable<QwbParallel> qpl)
		=> new() { parallels = qpl.Select(x => x.ToDTO()).ToArray() };

	public static QwbParallelDTO ToDTO(this QwbParallel qp) => new()
	{
			qwbTextReference = qp.textref
			, parallelWords = qp.words.Select(x => x.ToDTO()).ToArray()
			,
	};

	public static QwbParallelWordDTO ToDTO(this QwbParallelWord qpw) => new()
	{
			isReconstructed = qpw.isReconstructed
			, word = qpw.word
			, isVariant = qpw.isVariant
			, qwbWordId = qpw.wordId
			, relatedQwbWordId = qpw.relatedWordId
			,
	};

	public static QwbWordVariantListDTO ToDTO(this QwbWordVariants qwv)
		=> new() { variants = qwv.variants.Select(x => x.ToDTO()).ToArray() };

	public static QwbWordVariantDTO ToDTO(this QwbWordVariantObject qwvo) => new()
	{
			variantReading = qwvo.word
			, bibliography = qwvo.biblio.Select(x => x.ToDTO()).ToArray()
			,
	};

	public static QwbBibliographyDTO ToDTO(this QwbBiblio qb) => new()
	{
			bibliographyId = qb.id
			, comment = qb.commentary
			, pageReference = qb.pageRef
			, shortTitle = qb.shortTitle
			,
	};

	public static QwbBibliographyEntryDTO ToDTO(this QwbBibliographyEntry qbe)
		=> new() { entry = qbe.title };
}
