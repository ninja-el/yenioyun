using UnityEngine;

public class trail : MonoBehaviour
{
    [SerializeField] private Material _material;
    [SerializeField] private float _speed;

    private void Awake()
    {
        ResetOffset();
    }

    private void OnDestroy()
    {
        ResetOffset();
    }

    void Update()
    {
        _material.mainTextureOffset -= new Vector2(Time.deltaTime * _speed, 0);
    }

    // Offset paylasilan materyal asset'ine yaziliyor, yani oyun kapandiginda kayitli kaliyor.
    // Hem acilista hem cikista sifirlanirsa bant her zaman ayni yerden baslar ve .mat dosyasi
    // oturumdan oturuma degismez.
    private void ResetOffset()
    {
        if (_material == null) { return; }

        _material.mainTextureOffset = new Vector2(0, -0.27f);
    }
}
