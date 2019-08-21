using System.Collections.Generic;

namespace SQE.SqeHttpApi.DataAccess.Models
{
	public class ImagedObject
	{
		public string Id { get; set; }

		public string Institution { get; set; }
		public string Catalog1 { get; set; }
		public string Catalog2 { get; set; }
	}

	public class ImageStack
	{
		public uint Id { get; set; }
		public List<Image> Images { set; get; }
		public int MasterIndex { set; get; }
	}
}