using System.Xml.Serialization;

namespace Deck_Tracker_Copy_For_Study
{
    [XmlRoot("Version")]
    public class SerializableVersion
    {
        public int Major;
        public int Minor;
        public int Build;
        public int Revision;
        public override string ToString()
        {
            return $"{Major}.{Minor}.{Build}.{Revision}";
        }
    }
}
