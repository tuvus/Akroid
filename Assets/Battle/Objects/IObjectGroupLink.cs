public interface IObjectGroupLink {
    /// <summary>
    /// Removes the object form the group without calling RemoveGroup on the BattleObject
    /// </summary>
    /// <param name="battleObject"></param>
    public void RemoveBattleObject(BattleObject battleObject);

    public void UpdateObjectGroup(bool changeSizeIndicatorPosition = false);
}
