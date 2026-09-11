using UnityEngine;

public class trail : MonoBehaviour
{
    [SerializeField] private Material _material;
    [SerializeField] private float _speed;
    void Update()
    {
        _material.mainTextureOffset -= new Vector2(Time.deltaTime * _speed, 0);
    }
}
