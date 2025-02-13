using Mirror;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class NetworkTrash : NetworkBehaviour
{
    private XRGrabInteractable grabInteractable;

    private void Start()
    {
        grabInteractable = GetComponent<XRGrabInteractable>();
        grabInteractable.selectEntered.AddListener(OnGrab);
    }

    private void OnGrab(SelectEnterEventArgs args)
    {
        if (isServer)
        {
            // Obtener el NetworkIdentity del jugador desde el prefab raíz
            XRBaseInteractor interactor = args.interactorObject as XRBaseInteractor;
            Debug.Log($"Interactor root: {interactor.transform.root.name}");
            if (interactor != null)
            {
                // El jugador es el objeto raíz (VRPlayerPrefab)
                NetworkIdentity playerIdentity = interactor.transform.root.GetComponent<NetworkIdentity>();
                if (playerIdentity != null)
                {
                    netIdentity.AssignClientAuthority(playerIdentity.connectionToClient);
                    Debug.Log($"Autoridad asignada a: {playerIdentity.name}");
                }
            }
        }
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Bin"))
        {
            VRNetworkInteractable interactable = GetComponent<VRNetworkInteractable>();
            Debug.Log($"INTERACTABLE: {interactable}");
            Debug.Log($"HOLDER: {interactable.currentHolder}");
            if (interactable != null && interactable.currentHolder != null)
            {
                VRNetworkPlayerScript player = interactable.currentHolder.GetComponent<VRNetworkPlayerScript>();
                Debug.Log($"PLAYER NAME: {player.playerName}");
                if (player != null)
                {
                    Debug.Log($"PLAYER SCORE: {player.playerScore}");
                    player.AddScore(1);
                }
            }
            NetworkServer.Destroy(gameObject);
        }
    }
}