using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Mirror;

public class GameManager : MonoBehaviour
{
    public GameObject panel; // Panel principal de la UI

    public TMP_Text errorCodeText; // Texto para mostrar errores
    public TMP_InputField roomCodeInput; // Campo de texto para ingresar el c�digo de sala

    public Button createRoomButton; // Bot�n para crear una sala
    public Button JoinRoomButton; // Bot�n para unirse a una sala

    public Button TeamAButton; // Bot�n para unirse al Equipo A
    public Button TeamBButton; // Bot�n para unirse al Equipo B

    public TMP_Text stateTextValue; // Texto para mostrar el estado actual

    private string playerName = "Player"; // Nombre del jugador (puedes personalizarlo)
    private int playerId; // ID del jugador en la red

    void Start()
    {
        // Asignar eventos a los botones usando listeners
        createRoomButton.onClick.AddListener(OnCreateRoomButtonClicked);
        JoinRoomButton.onClick.AddListener(OnJoinRoomButtonClicked);
        TeamAButton.onClick.AddListener(() => OnJoinTeamButtonClicked(Team.TeamA));
        TeamBButton.onClick.AddListener(() => OnJoinTeamButtonClicked(Team.TeamB));

        // Obtener el ID del jugador
        playerId = GetPlayerId();
        Debug.Log($"ID del jugador asignado: {playerId}");

        // Inicializar el panel y los mensajes
        panel.SetActive(true);
        errorCodeText.text = "";
        stateTextValue.text = "Esperando acci�n...";
    }

    // M�todo para manejar el bot�n "Crear Sala"
    private void OnCreateRoomButtonClicked()
    {
        Debug.Log("Bot�n 'Crear Sala' presionado.");
        string roomCodeInputText = roomCodeInput.text.Trim();
        string roomCode = RoomManager.Instance.CreateRoom(string.IsNullOrEmpty(roomCodeInputText) ? null : roomCodeInputText);
        Debug.Log($"Sala creada con c�digo: {roomCode}");
        stateTextValue.text = $"Sala creada con c�digo: {roomCode}";
        errorCodeText.text = ""; // Limpiar errores anteriores

        roomCodeInput.text = roomCode;
    }

    // M�todo para manejar el bot�n "Unirse a Sala"
    private void OnJoinRoomButtonClicked()
    {
        string roomCode = roomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(roomCode))
        {
            errorCodeText.text = "Error: Ingresa un c�digo de sala v�lido.";
            return;
        }

        bool success = RoomManager.Instance.JoinRoom(roomCode, Team.None, playerName, playerId);
        if (success)
        {
            stateTextValue.text = $"Te has unido a la sala {roomCode}.";
            errorCodeText.text = ""; // Limpiar errores anteriores
        }
        else
        {
            errorCodeText.text = "Error: No se pudo unir a la sala. Verifica el c�digo.";
        }
    }

    // M�todo para manejar los botones de selecci�n de equipo
    private void OnJoinTeamButtonClicked(Team team)
    {
        string roomCode = roomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(roomCode))
        {
            errorCodeText.text = "Error: Ingresa un c�digo de sala v�lido.";
            return;
        }

        bool success = RoomManager.Instance.JoinRoom(roomCode, team, playerName, playerId);
        if (success)
        {
            stateTextValue.text = $"Te has unido al {team} en la sala {roomCode}.";
            errorCodeText.text = ""; // Limpiar errores anteriores
        }
        else
        {
            errorCodeText.text = "Error: No se pudo unir al equipo. Verifica el c�digo o el equipo.";
        }
    }

    // M�todo para obtener el ID del jugador en la red
    private int GetPlayerId()
    {
        if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            return (int) NetworkClient.connection.identity.netId;
        }
        return -1; // Valor temporal si no hay conexi�n
    }
}