using Mirror;
using UnityEngine;

public class StickController : NetworkBehaviour
{
    public Transform stickTip;
    [SyncVar(hook = nameof(OnAttachedObjectChanged))]
    private NetworkIdentity _currentAttachedObject;

    [Header("Movement Settings")]
    [SerializeField] private float throwThreshold = 4.0f;
    [SerializeField] private float smoothTime = 0.2f;

    private Vector3 previousPosition;
    public Vector3 smoothedVelocity;
    private NetworkIdentity stickNetworkIdentity;

    private VRNetworkInteractable currentlyAttachedInteractable =>
        _currentAttachedObject?.GetComponent<VRNetworkInteractable>();

    // Añadir estas variables
    private float velocityOverThresholdTime = 0f;
    private Vector3 velocitySmoothRef;

    [Header("Nuclear Attachment Config")]
    [SerializeField] private float positionTolerance = 0.001f; // Tolerancia de posición

    void Start()
    {
        stickNetworkIdentity = GetComponent<NetworkIdentity>();
        previousPosition = stickTip.position;

        if (!isServer) return;
        Debug.Log("StickController: Inicializado en servidor");
    }

    [ServerCallback]
    private void Update()
    {
        if (_currentAttachedObject != null)
        {
            // Forzar sincronización de posición en el servidor
            VRNetworkInteractable interactable = _currentAttachedObject.GetComponent<VRNetworkInteractable>();
            if (interactable != null)
            {
                interactable.transform.position = stickTip.position;
                interactable.transform.rotation = stickTip.rotation;
            }
        }
    }

    [ServerCallback]
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Trash") && _currentAttachedObject == null)
        {
            Debug.Log("Servidor: Detección de basura válida");

            VRNetworkInteractable interactable = other.GetComponent<VRNetworkInteractable>();
            if (interactable != null && !interactable.IsAttachedToStick)
            {
                Debug.Log("Servidor: Iniciando adhesión");
                _currentAttachedObject = interactable.netIdentity;
                interactable.ServerAttachToStick(stickNetworkIdentity);
            }
        }
    }

    [Server]
    private void DetachCurrentObject()
    {
        if (_currentAttachedObject == null) return;

        VRNetworkInteractable interactable = _currentAttachedObject.GetComponent<VRNetworkInteractable>();

        // Verificar si el interactable aún existe
        if (interactable != null)
        {
            interactable.ServerDetachFromStick();
        }

        _currentAttachedObject = null;
    }

    private void OnAttachedObjectChanged(NetworkIdentity oldVal, NetworkIdentity newVal)
    {
        Debug.Log($"Cliente: Estado adhesión actualizado - {(newVal != null ? newVal.name : "Ninguno")}");
    }

    // Eliminar OnTriggerExit (manejado por ServerDetachFromStick)
}