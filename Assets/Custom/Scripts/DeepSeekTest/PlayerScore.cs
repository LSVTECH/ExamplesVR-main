using Mirror;
using UnityEngine;

public class PlayerScore : NetworkBehaviour
{
    [SyncVar(hook = nameof(UpdateScoreUI))]
    public int score = 0;

    // Referencia al UI de puntaje (asignar en el inspector)
    public GameObject scoreUI;

    // Actualizar UI cuando el SyncVar cambia
    private void UpdateScoreUI(int oldValue, int newValue)
    {
        //scoreUI.GetComponent<Text>().text = $"Score: {newValue}";
    }

    // Método para incrementar el puntaje (llamado desde el servidor)
    [Server]
    public void AddScore()
    {
        score++;
    }
}