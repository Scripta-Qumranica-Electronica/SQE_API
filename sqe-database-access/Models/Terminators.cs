namespace SQE.DatabaseAccess.Models
{
    public class Terminators
    {
        private uint[] _data;
        public uint StartId => _data[0];
        public uint EndId => _data[1];
        public bool IsValid => _data.Length == 2;

        public Terminators(uint[] data)
        {
            _data = data;
        }


    }
}