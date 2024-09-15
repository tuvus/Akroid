public interface IParticleHolder : IEffectHolder {
    /// <summary>
    /// Sets the playback speed of all particle systems attached to this battleObject
    /// </summary>
    /// <param name="speed">the speed at which the particle systems should be played at</param>
    public void SetParticleSpeed(float speed);

    /// <summary>
    /// Sets if the particles are shown or not on all particle systems attached
    /// </summary>
    /// <param name="shown">wether the particles should be shown</param>
    public void ShowParticles(bool shown);
}
