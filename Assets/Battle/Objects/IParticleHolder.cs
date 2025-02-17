public interface IParticleHolder : IEffectHolder {
    /// <summary>
    /// Sets the playback speed of all particle systems attached to this battleObject
    /// </summary>
    /// <param name="speed">the speed at which the particle systems should be played at</param>
    public void SetParticleSpeed(float speed);
}
