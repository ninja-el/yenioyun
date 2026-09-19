namespace MatchPack.Data
{
    /// <summary>
    /// Desteklenen diller. Kayıt PlayerPrefs'e enum adıyla yazıldığı için araya yeni bir dil
    /// eklenmesi eski seçimleri bozmaz; yine de yeni diller listenin sonuna eklenir.
    /// Yeni bir dil için LocalizationEntry'ye alanı ve Get switch'ine satırı da eklenmelidir.
    /// </summary>
    public enum LanguageCode
    {
        English = 0,
        Turkish = 1,
        Spanish = 2,
        Portuguese = 3
    }
}
