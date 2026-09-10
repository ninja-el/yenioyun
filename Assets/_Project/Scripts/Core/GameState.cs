namespace MatchPack.Core
{
    /// <summary>Oyunun içinde bulunduğu durum. Geçişleri yalnızca GameManager yapar.</summary>
    public enum GameState
    {
        Menu,
        Loading,
        Playing,
        Win,
        Lose
    }
}
