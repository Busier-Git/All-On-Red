using UnityEngine;

/// <summary>
/// Suelta monedas con cierta probabilidad. Lo llaman los enemigos al morir
/// (y los obstaculos destructibles si quieres). Asigna Coin.prefab a prefabMoneda.
/// </summary>
public class Botin : MonoBehaviour
{
    public GameObject prefabMoneda;
    [Range(0f, 1f)] public float probabilidad = 0.4f;
    public int minMonedas = 1;
    public int maxMonedas = 2;

    public void Soltar()
    {
        if (prefabMoneda == null) return;
        if (Random.value > probabilidad) return;

        int cantidad = Random.Range(minMonedas, maxMonedas + 1);
        for (int i = 0; i < cantidad; i++)
        {
            Vector3 offset = (Vector3)(Random.insideUnitCircle * 0.6f);
            Instantiate(prefabMoneda, transform.position + offset, Quaternion.identity);
        }
    }
}
