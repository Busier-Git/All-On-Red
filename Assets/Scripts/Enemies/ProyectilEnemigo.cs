using UnityEngine;

public class ProyectilEnemigo : MonoBehaviour
{
    public float dano = 1f;
    public float tiempoVida = 3f;

    void Start()
    {
        Destroy(gameObject, tiempoVida);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Player player = collision.GetComponent<Player>();
            if (player != null)
                player.RecibirDano((int)dano);

            Destroy(gameObject);
        }
    }
}