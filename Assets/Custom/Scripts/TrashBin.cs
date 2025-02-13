using UnityEngine;
using Mirror;

public class TrashBin : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // Verificar si el objeto es basura
        if (other.CompareTag("Trash"))
        {
            // Obtener el jugador que sostiene el objeto
            NetworkIdentity trashOwner = other.GetComponent<NetworkIdentity>();

            if (trashOwner != null)
            {
                VRNetworkPlayerScript playerScript = trashOwner.GetComponent<VRNetworkPlayerScript>();

                Debug.Log(playerScript);
                Debug.Log(isServer);
                if (playerScript != null && isServer)
                {
                    // Incrementar el puntaje del jugador en el servidor
                    //playerScript.CmdAddScore(1);/
                }
            }

            // Destruir la basura en el servidor
            NetworkServer.Destroy(other.gameObject);
        }
    }
}
