using System.ComponentModel;

namespace Vision.Server.Services
{
    public enum ParticleModelType
    {
        [Description("Appearance")]
        Appearance,
        [Description("Particle")]
        Particle,
        [Description("Velocity")]
        Velocity,
        [Description("Motion")]
        MDParticleFilter
    }
}