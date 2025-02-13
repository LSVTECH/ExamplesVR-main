using Mirror;
public class PlayerScoreManager : NetworkBehaviour
{
    [SyncVar] // Sincroniza el puntaje del jugador
    public int score = 0;

    public void AddScore(int points)
    {
        if (isServer)
        {
            score += points;
        }
    }
}
