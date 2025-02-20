using UnityEngine;
using Mirror;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;

public class VRNetworkPlayerScript : NetworkBehaviour
{
    // Referencias de transformación VR
    public Transform rHandTransform;
    public Transform lHandTransform;
    public Transform headTransform;

    // Modelos de avatar
    public GameObject headModel;
    public GameObject rHandModel;
    public GameObject lHandModel;

    // Sistema VR y salud
    public VRPlayerRig vrPlayerRig;
    //public VRNetworkHealth vrNetworkHealth;

    [SyncVar(hook = nameof(OnPlayerScoreChanged))]
    public int playerScore = 0;  // SyncVar para el puntaje (int)

    public TMP_Text playerScoreText;
    public TMP_Text playerGlobalScoreText;

    // Lista de jugadores y nombre
    public readonly static List<VRNetworkPlayerScript> playersList = new List<VRNetworkPlayerScript>();
    [SyncVar(hook = nameof(OnNameChangedHook))]
    public string playerName = "";
    public TMP_Text textPlayerName;

    // Sistema de armas
    [SyncVar(hook = nameof(OnRightObjectChangedHook))]
    public NetworkIdentity rightHandObject;
    [SyncVar(hook = nameof(OnLeftObjectChangedHook))]
    public NetworkIdentity leftHandObject;
    public VRWeapon vrWeaponRight;
    public VRWeapon vrWeaponLeft;

    private void Start()
    {
        ChangeLocalText(0, 0);
    }

    #region Configuración Inicial
    public override void OnStartLocalPlayer()
    {
        ConfigurarVRRig();
        OcultarModelosLocales();
        ConfigurarNombreJugador();
        ActualizarUIInicial();
    }

    private void ConfigurarVRRig()
    {
        vrPlayerRig = FindObjectOfType<VRPlayerRig>();
        if (vrPlayerRig != null) vrPlayerRig.localVRNetworkPlayerScript = this;
    }

    private void OcultarModelosLocales()
    {
        headModel.SetActive(false);
        rHandModel.SetActive(false);
        lHandModel.SetActive(false);
    }

    private void ConfigurarNombreJugador()
    {
        string nombre = string.IsNullOrEmpty(VRStaticVariables.playerName)
            ? "Player: " + netId
            : VRStaticVariables.playerName;

        CmdSetupPlayer(nombre);
    }

    private void ActualizarUIInicial()
    {
        OnPlayerScoreChanged(playerScore, playerScore); // Actualiza con el valor inicial
        OnNameChangedHook("", playerName);
    }
    #endregion  

    #region Sistema de Red
    public override void OnStartServer() => playersList.Add(this);
    public override void OnStopServer() => playersList.Remove(this);

    [Command]
    public void CmdSetupPlayer(string _name) => playerName = _name;
    #endregion

    public void AddScore(int points)
    {
        Debug.Log($"IS SERVER: {isServer}");
        if (!isServer) return; // Solo el servidor puede modificar el puntaje

        Debug.Log($"[AddScore] Puntos antes: {playerScore}");
        playerScore = Mathf.Max(0, playerScore + points);
        Debug.Log($"[AddScore] Puntos después: {playerScore} (Jugador: {playerName})");
    }

    private void OnPlayerScoreChanged(int oldScore, int newScore)
    {
        Debug.Log($"[OnPlayerScoreChanged] {oldScore} -> {newScore} (Jugador: {playerName})");

        Debug.LogWarning(isLocalPlayer);
        if (playerScoreText != null)
        {
            ChangeLocalText(oldScore, newScore);
        }
        else
        {
            Debug.LogError("[UI] playerScoreText es null");
        }
    }

    private void ChangeLocalText(int oldScore, int newScore)
    {
        if (isLocalPlayer)
        {
            playerScoreText.text = $"Score: {newScore}";
            playerGlobalScoreText.text = "";
            Debug.LogWarning($"[UI] Texto actualizado: {newScore}");
        }
        else
        {
            playerScoreText.text = "";
            playerGlobalScoreText.text = $"Score: {newScore}";
            Debug.LogWarning($"[UI] Texto actualizado vacio");
        }
    }

    #region Sistema de Armas
    public void Fire(int _hand)
    {
        if (PuedeDispararManoDerecha(_hand)) ProcesarDisparoDerecho();
        if (PuedeDispararManoIzquierda(_hand)) ProcesarDisparoIzquierdo();
    }

    private bool PuedeDispararManoDerecha(int hand) => (hand == 0 || hand == 1) && rightHandObject && vrWeaponRight;
    private bool PuedeDispararManoIzquierda(int hand) => (hand == 0 || hand == 2) && leftHandObject && vrWeaponLeft;

    private void ProcesarDisparoDerecho()
    {
        if (Time.time > vrWeaponRight.weaponFireCooldownTime && vrWeaponRight.weaponAmmo > 0)
        {
            ActualizarCooldownDerecho();
            CmdFire(1);
            ReproducirSonidoDisparo();
        }
    }

    private void ProcesarDisparoIzquierdo()
    {
        if (Time.time > vrWeaponLeft.weaponFireCooldownTime && vrWeaponLeft.weaponAmmo > 0)
        {
            ActualizarCooldownIzquierdo();
            CmdFire(2);
            ReproducirSonidoDisparo();
        }
    }

    private void ActualizarCooldownDerecho() =>
        vrWeaponRight.weaponFireCooldownTime = Time.time + vrWeaponRight.weaponFireCooldown;

    private void ActualizarCooldownIzquierdo() =>
        vrWeaponLeft.weaponFireCooldownTime = Time.time + vrWeaponLeft.weaponFireCooldown;

    private void ReproducirSonidoDisparo()
    {
        if (vrPlayerRig != null && vrPlayerRig.audioCue != null)
            vrPlayerRig.audioCue.Play();
    }

    [Command]
    private void CmdFire(int _hand) => RpcOnFire(_hand);

    [ClientRpc]
    private void RpcOnFire(int _hand) => OnFire(_hand);

    private void OnFire(int _hand)
    {
        if (_hand == 1) ProcesarProyectilDerecho();
        if (_hand == 2) ProcesarProyectilIzquierdo();
    }

    private void ProcesarProyectilDerecho()
    {
        vrWeaponRight.weaponAmmo--;
        CrearProyectil(vrWeaponRight);
    }

    private void ProcesarProyectilIzquierdo()
    {
        vrWeaponLeft.weaponAmmo--;
        CrearProyectil(vrWeaponLeft);
    }

    private void CrearProyectil(VRWeapon arma)
    {
        GameObject proyectil = Instantiate(
            arma.weaponProjectile,
            arma.weaponFireLine.position,
            arma.weaponFireLine.rotation
        );

        Rigidbody rb = proyectil.GetComponent<Rigidbody>();
        rb.AddForce(arma.weaponFireLine.forward * arma.weaponProjectileSpeed);

        Destroy(proyectil, arma.weaponProjectileLife);
        arma.SetTextAmmo();
    }
    #endregion

    #region Hooks y Actualizaciones
    void OnNameChangedHook(string _old, string _new)
    {
        if (textPlayerName != null)
            textPlayerName.text = _new;
    }

    void OnRightObjectChangedHook(NetworkIdentity _old, NetworkIdentity _new)
    {
        vrWeaponRight = _new?.GetComponent<VRWeapon>();
        if (vrWeaponRight != null)
        {
            vrWeaponRight.vrNetworkPlayerScript = this;
            vrWeaponRight.SetTextAmmo();
        }
    }

    void OnLeftObjectChangedHook(NetworkIdentity _old, NetworkIdentity _new)
    {
        vrWeaponLeft = _new?.GetComponent<VRWeapon>();
        if (vrWeaponLeft != null)
        {
            vrWeaponLeft.vrNetworkPlayerScript = this;
            vrWeaponLeft.SetTextAmmo();
        }
    }
    #endregion
}