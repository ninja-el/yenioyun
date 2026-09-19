namespace MatchPack.Data
{
    /// <summary>
    /// Booster çeşitleri. Sıra doğrudan PlayerData.BoosterCounts dizisinin index'idir; araya yeni
    /// bir değer eklenirse eski kayıtlardaki adetler kayar, bu yüzden yeni booster listenin
    /// sonuna eklenir.
    /// </summary>
    public enum BoosterType
    {
        Freeze = 0,
        Shuffle = 1,
        AutoMatch = 2,
        JokerBox = 3
    }
}
