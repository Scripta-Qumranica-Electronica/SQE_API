namespace SQE.API.DTO
{
    public class SetPolygonDTO
    {
        public string mask { get; set; }
        public TransformationDTO transformation { get; set; }
    }

    public class PolygonDTO : SetPolygonDTO
    {
        public uint maskEditorId { get; set; }
        public uint positionEditorId { get; set; }
    }

    public class WktPolygonDTO
    {
        public string wktPolygon { get; set; }
    }
}