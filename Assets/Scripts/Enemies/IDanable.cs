// Interfaz para todo lo que pueda recibir daño (enemigos, jefe, obstaculos).
// Asi el proyectil del jugador no necesita conocer cada tipo: solo pide IDanable.
public interface IDanable
{
    void RecibirDano(float cantidad);
}
