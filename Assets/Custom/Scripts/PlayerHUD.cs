using Mirror;
using TMPro; // Importa el namespace de TextMeshPro
using UnityEngine;

public class PlayerHUD : NetworkBehaviour
{
    public TMP_Text scoreText; // Cambia Text por TMP_Text (de TextMeshPro)

    private PlayerScoreManager scoreManager;

    private void Start()
    {
        if (isLocalPlayer)
        {
            scoreManager = GetComponent<PlayerScoreManager>();
        }
        else
        {
            enabled = false; // Solo actualiza el HUD del jugador local
        }
    }

    private void Update()
    {
        if (isLocalPlayer && scoreManager != null)
        {
            scoreText.text = "Score: " + scoreManager.score; // Actualiza el texto con el puntaje
        }
    }
}
