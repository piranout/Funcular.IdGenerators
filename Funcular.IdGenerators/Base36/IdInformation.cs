using System;

namespace Funcular.IdGenerators.Base36
{
    public class IdInformation
    {
        public static IdInformation Default = new IdInformation();
        public int Base { get; set; }
        public string TimestampComponent { get; set; }
        public string HashComponent { get; set; }
        public string RandomComponent { get; set; }
        public DateTime? CreationTimestampUtc { get; set; }
    }
}