namespace SQE.SqeHttpApi.Server.DTOs
{
    public class PolygonDTO
    {
        public string mask { get; set; } 
        public uint maskEditorId { get; set; } 
        public string transformMatrix { get; set; } //CAN BE NULL
        public uint transformMatrixEditorId { get; set; } 
    }
}
