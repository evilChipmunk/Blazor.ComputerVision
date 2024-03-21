using System.ComponentModel;

namespace Vision.Server.Services
{
    public enum DataSetType
    {
        [Description("None")]
        None,
        [Description("President Hand")]
        PresidentHand,
        [Description("Pedestrians")]
        Pedestrians,
        [Description("Web cam stream")]
        Stream,
        [Description("Mandrill")]
        Mandrill
    }
}