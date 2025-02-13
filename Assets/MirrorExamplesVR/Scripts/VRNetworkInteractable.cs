using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.XR.Interaction.Toolkit;

public class VRNetworkInteractable : NetworkBehaviour
{
    private Rigidbody rb;
    public VRWeapon vrWeapon;

    [SyncVar(hook = nameof(OnHeldStateChanged))]
    private bool isBeingHeld = false;

    [SyncVar]
    private NetworkIdentity _currentHolder;
    public NetworkIdentity currentHolder => _currentHolder;

    [SyncVar]
    private bool isAttachedToStick = false;
    public bool IsAttachedToStick => isAttachedToStick;

    [Header("Stick Attachment Settings")]
    [SerializeField] private float detachForceMultiplier = 0.5f;
    [SerializeField] private float positionFollowSpeed = 15f;
    [SerializeField] private float rotationFollowSpeed = 10f;
    [SerializeField] private float jointSpringForce = 5000f;

    [Header("Physics Settings")]
    [SerializeField] private float stickAttachmentSpring = 150000f; // Incrementar rigidez
    [SerializeField] private float stickAttachmentDamper = 1500f;  // Mayor amortiguación

    private ConfigurableJoint attachmentJoint;
    private Transform currentStickTip;
    private Vector3 stickVelocityAtDetach;

    [Header("Hyper-Stable Attachment Settings")]
    [SerializeField] private float positionSnapForce = 1000000f; // Fuerza extrema para mantener posición
    [SerializeField] private float maxAttachmentDistance = 0.01f; // Distancia máxima permitida

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Forzar inicialización segura
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // Inicializar collider
        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;
    }

    // Hook para cuando cambia el estado de isBeingHeld
    private void OnHeldStateChanged(bool oldValue, bool newValue)
    {
        Debug.Log($"Estado de agarre cambió: {newValue}");
    }

    public void EventPickup()
    {
        if (!isBeingHeld)
        {
            ResetInteractableVelocity();
            CmdPickup(VRStaticVariables.handValue);
        }
        else
        {
            GetComponent<XRGrabInteractable>().interactionManager.CancelInteractableSelection(
                GetComponent<XRGrabInteractable>()
            );
        }
    }

    public void EventDrop()
    {
        if (isOwned)
        {
            CmdDrop(VRStaticVariables.handValue);
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdPickup(int _hand, NetworkConnectionToClient sender = null)
    {
        if (isBeingHeld || IsAttachedToStick)
        {
            return;
        }

        ResetInteractableVelocity();

        // Quitar autoridad previa si existe
        if (hasAuthority && netIdentity.connectionToClient != sender)
        {
            netIdentity.RemoveClientAuthority();
        }

        // Asignar nueva autoridad
        netIdentity.AssignClientAuthority(sender);
        isBeingHeld = true;
        _currentHolder = sender.identity;

        if (vrWeapon)
        {
            vrWeapon.vrNetworkPlayerScript = sender.identity.GetComponent<VRNetworkPlayerScript>();
            if (_hand == 2)
            {
                vrWeapon.vrNetworkPlayerScript.leftHandObject = this.netIdentity;
            }
            else
            {
                vrWeapon.vrNetworkPlayerScript.rightHandObject = this.netIdentity;
            }
        }

        // Confirmar a todos los clientes
        RpcConfirmPickup(sender.identity);
    }

    [Command(requiresAuthority = false)]
    public void CmdDrop(int _hand, NetworkConnectionToClient sender = null)
    {
        if (_currentHolder != sender.identity)
            return;

        if (vrWeapon && vrWeapon.vrNetworkPlayerScript)
        {
            if (_hand == 2)
            {
                vrWeapon.vrNetworkPlayerScript.leftHandObject = null;
            }
            else
            {
                vrWeapon.vrNetworkPlayerScript.rightHandObject = null;
            }
        }

        // Limpiar el estado
        isBeingHeld = false;
        //_currentHolder = null;

        // Quitar la autoridad del cliente
        if (netIdentity.connectionToClient != null)
        {
            netIdentity.RemoveClientAuthority();
        }

        // Confirmar a todos los clientes
        RpcConfirmDrop();
    }

    [ClientRpc]
    private void RpcConfirmPickup(NetworkIdentity newHolder)
    {
        isBeingHeld = true;
        _currentHolder = newHolder;
        Debug.Log("Objeto agarrado por: " + newHolder);
    }

    [ClientRpc]
    private void RpcConfirmDrop()
    {
        isBeingHeld = false;
        //_currentHolder = null;
        Debug.Log("Objeto soltado");
    }

    private void ResetInteractableVelocity()
    {
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    public VRNetworkPlayerScript vrNetworkPlayerScript;

    private void Update()
    {
        if (vrNetworkPlayerScript == null)
            return;

        // Simular entrada manual para pruebas
        if (Input.GetKeyDown(KeyCode.P))
        {
            EventPickup();
        }
        else if (Input.GetKeyDown(KeyCode.D))
        {
            EventDrop();
        }

        // Detectar botón A/X para soltar objetos adheridos al palo
        if (Input.GetKeyDown(KeyCode.JoystickButton0)) // Botón A/X
        {
            if (IsAttachedToStick)
            {
                Debug.Log("Soltando objeto con botón A/X");
                CmdDetachFromStick(NetworkClient.connection as NetworkConnectionToClient);
            }
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdAttachToStick(NetworkIdentity stickHolder, NetworkConnectionToClient sender = null)
    {
        Debug.Log("CmdAttachToStick llamado");

        if (isBeingHeld || IsAttachedToStick)
        {
            Debug.LogWarning("CmdAttachToStick: La basura ya está siendo sostenida o adherida al palo.");
            return;
        }

        NetworkTransform networkTransform = GetComponent<NetworkTransform>();
        networkTransform.enabled = false;

        Debug.Log("CmdAttachToStick: Asignando autoridad al palo");
        netIdentity.AssignClientAuthority(sender);

        Debug.Log("CmdAttachToStick: Marcando como adherida al palo");
        isAttachedToStick = true;
        _currentHolder = stickHolder;

        // Desactivar el collider antes de confirmar la adherencia
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Debug.Log("CmdAttachToStick: Desactivando collider");
            collider.enabled = false;
        }
        else
        {
            Debug.LogError("CmdAttachToStick: Collider no encontrado");
        }

        Debug.Log("CmdAttachToStick: Confirmando adherencia a todos los clientes");
        RpcConfirmAttachToStick(stickHolder);

        // Desactivar la física
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log("CmdAttachToStick: Desactivando física");
            rb.isKinematic = true;
        }
        else
        {
            Debug.LogError("CmdAttachToStick: Rigidbody no encontrado");
        }

        // Desactivar la interacción manual
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            Debug.Log("CmdAttachToStick: Desactivando interacción manual");
            grabInteractable.enabled = false;
        }
        else
        {
            Debug.LogError("CmdAttachToStick: XRGrabInteractable no encontrado");
        }

        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdDetachFromStick(NetworkConnectionToClient sender = null)
    {
        Debug.Log("CmdDetachFromStick llamado");

        if (!IsAttachedToStick || _currentHolder != sender.identity)
        {
            Debug.LogWarning("CmdDetachFromStick: La basura no está adherida al palo o el cliente no tiene autoridad.");
            return;
        }

        Debug.Log("CmdDetachFromStick: Limpiando estado de adherencia");
        isAttachedToStick = false;
        _currentHolder = null;

        // Quitar la autoridad del cliente
        if (netIdentity.connectionToClient != null)
        {
            Debug.Log("CmdDetachFromStick: Quitando autoridad del cliente");
            netIdentity.RemoveClientAuthority();
        }

        Debug.Log("CmdDetachFromStick: Confirmando desprendimiento a todos los clientes");
        RpcConfirmDetachFromStick();

        // Reactivar la física
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            Debug.Log("CmdDetachFromStick: Reactivando física");
            rb.isKinematic = false;
        }
        else
        {
            Debug.LogError("CmdDetachFromStick: Rigidbody no encontrado");
        }

        // Reactivar el collider
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            Debug.Log("CmdDetachFromStick: Reactivando collider");
            collider.enabled = true;
        }
        else
        {
            Debug.LogError("CmdDetachFromStick: Collider no encontrado");
        }

        // Reactivar la interacción manual
        XRGrabInteractable grabInteractable = GetComponent<XRGrabInteractable>();
        if (grabInteractable != null)
        {
            Debug.Log("CmdDetachFromStick: Reactivando interacción manual");
            grabInteractable.enabled = true;
        }
        else
        {
            Debug.LogError("CmdDetachFromStick: XRGrabInteractable no encontrado");
        }
    }

    private void FixedUpdate()
    {
        if (isAttachedToStick && currentStickTip != null)
        {
            // Forzar posición directamente si hay desviación
            if (Vector3.Distance(transform.position, currentStickTip.position) > maxAttachmentDistance)
            {
                rb.MovePosition(currentStickTip.position);
                rb.MoveRotation(currentStickTip.rotation);
            }

            // Resetear fuerzas residuales
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    [ClientRpc]
    private void RpcConfirmAttachToStick(NetworkIdentity stickHolder)
    {
        isAttachedToStick = true;
        _currentHolder = stickHolder;

        // Desactivar gravedad durante la adhesión
        if (rb != null) rb.useGravity = false;

        // Configurar posición inicial perfectamente alineada
        transform.position = stickHolder.transform.Find("StickTip").position;
        transform.rotation = stickHolder.transform.Find("StickTip").rotation;
    }

    private void CreateAttachmentJoint(Transform stickTip)
    {
        if (stickTip == null) return;

        // Destruir joint existente si hay uno
        if (attachmentJoint != null) Destroy(attachmentJoint);

        // Crear nuevo joint con parámetros reforzados
        attachmentJoint = gameObject.AddComponent<ConfigurableJoint>();
        attachmentJoint.connectedBody = stickTip.GetComponent<Rigidbody>();

        // Configurar movimiento lineal y angular
        JointDrive superStiffDrive = new JointDrive
        {
            positionSpring = stickAttachmentSpring,
            positionDamper = stickAttachmentDamper,
            maximumForce = Mathf.Infinity
        };

        attachmentJoint.xDrive = superStiffDrive;
        attachmentJoint.yDrive = superStiffDrive;
        attachmentJoint.zDrive = superStiffDrive;

        // Bloquear todas las rotaciones
        attachmentJoint.angularXMotion = ConfigurableJointMotion.Locked;
        attachmentJoint.angularYMotion = ConfigurableJointMotion.Locked;
        attachmentJoint.angularZMotion = ConfigurableJointMotion.Locked;

        // Forzar actualización inmediata
        attachmentJoint.projectionMode = JointProjectionMode.PositionAndRotation;
        attachmentJoint.projectionDistance = 0.01f;
        attachmentJoint.projectionAngle = 1f;
    }

    private IEnumerator FollowStick()
    {
        while (isAttachedToStick && currentStickTip != null)
        {
            // Actualización directa para prevenir drift
            if (attachmentJoint == null)
            {
                transform.position = currentStickTip.position;
                transform.rotation = currentStickTip.rotation;
            }
            yield return new WaitForFixedUpdate(); // Sincronizar con física
        }
    }

    [ClientRpc]
    private void RpcConfirmDetachFromStick()
    {
        isAttachedToStick = false;
        _currentHolder = null;

        // Reactivar collider en todos los clientes
        Collider collider = GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = true;
        }
        else
        {
            Debug.LogError("RpcConfirmDetachFromStick: Collider no encontrado");
        }

        NetworkTransform networkTransform = GetComponent<NetworkTransform>();
        networkTransform.enabled = true;

        Debug.Log("Objeto desprendido del palo");
    }

    [Server] // Solo el servidor puede ejecutar este método
    public void ServerAttachToStick(NetworkIdentity stickHolder)
    {
        if (isBeingHeld || IsAttachedToStick) return;

        Debug.Log("Servidor: Adjuntando al palo...");

        // Desactivar collider INMEDIATAMENTE en el servidor
        GetComponent<Collider>().enabled = false;

        isAttachedToStick = true;
        _currentHolder = stickHolder;

        // Sincronizar cambios a clientes
        RpcAttachToStick(stickHolder);
    }

    // Modificar ServerDetachFromStick
    [Server]
    public void ServerDetachFromStick()
    {
        // Verificar si el objeto aún existe
        if (this == null || _currentHolder == null) return;

        // Guardar velocidad ANTES de resetear variables
        StickController stickController = _currentHolder.GetComponent<StickController>();
        if (stickController != null)
        {
            stickVelocityAtDetach = stickController.smoothedVelocity;
        }

        StartCoroutine(SmoothDetach());
        RpcDetachFromStick();
    }

    private IEnumerator SmoothDetach()
    {
        // Verificar componentes críticos
        if (rb == null || currentStickTip == null) yield break;

        // 1. Reactivar componentes
        GetComponent<Collider>().enabled = true;
        rb.isKinematic = false;

        // 2. Transición suave
        float elapsed = 0f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;

        while (elapsed < 0.3f)
        {
            if (currentStickTip == null) break;

            transform.position = Vector3.Lerp(
                startPos,
                currentStickTip.position,
                elapsed / 0.3f
            );

            transform.rotation = Quaternion.Slerp(
                startRot,
                currentStickTip.rotation,
                elapsed / 0.3f
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        // 3. Aplicar fuerzas solo si el rigidbody existe
        if (rb != null && !rb.isKinematic)
        {
            rb.AddForce(stickVelocityAtDetach * detachForceMultiplier, ForceMode.VelocityChange);
            rb.AddTorque(rb.angularVelocity * 0.5f, ForceMode.VelocityChange);
        }

        // 4. Resetear estado
        isAttachedToStick = false;
        _currentHolder = null;
    }

    [ClientRpc]
    private void RpcAttachToStick(NetworkIdentity stickHolder)
    {
        Debug.Log("Cliente: Recibiendo adhesión al palo");
        isAttachedToStick = true;
        _currentHolder = stickHolder;
        GetComponent<Collider>().enabled = false; // Desactivar en todos los clientes
        StartCoroutine(FollowStick());
    }

    [ClientRpc]
    private void RpcDetachFromStick()
    {
        // Verificar existencia antes de acceder
        if (this == null) return;

        isAttachedToStick = false;
        _currentHolder = null;

        Collider col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        XRGrabInteractable grab = GetComponent<XRGrabInteractable>();
        if (grab != null) grab.enabled = true;
    }
}