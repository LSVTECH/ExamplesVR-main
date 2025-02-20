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
    public TMP_InputField roomCodeInput; // Campo de texto para ingresar el código de sala

    public Button createRoomButton; // Botón para crear una sala
    public Button JoinRoomButton; // Botón para unirse a una sala

    public Button TeamAButton; // Botón para unirse al Equipo A
    public Button TeamBButton; // Botón para unirse al Equipo B

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
        stateTextValue.text = "Esperando acción...";
    }

    // Método para manejar el botón "Crear Sala"
    private void OnCreateRoomButtonClicked()
    {
        Debug.Log("Botón 'Crear Sala' presionado.");
        string roomCodeInputText = roomCodeInput.text.Trim();
        string roomCode = RoomManager.Instance.CreateRoom(string.IsNullOrEmpty(roomCodeInputText) ? null : roomCodeInputText);
        Debug.Log($"Sala creada con código: {roomCode}");
        stateTextValue.text = $"Sala creada con código: {roomCode}";
        errorCodeText.text = ""; // Limpiar errores anteriores

        roomCodeInput.text = roomCode;
    }

    // Método para manejar el botón "Unirse a Sala"
    private void OnJoinRoomButtonClicked()
    {
        string roomCode = roomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(roomCode))
        {
            errorCodeText.text = "Error: Ingresa un código de sala válido.";
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
            errorCodeText.text = "Error: No se pudo unir a la sala. Verifica el código.";
        }
    }

    // Método para manejar los botones de selección de equipo
    private void OnJoinTeamButtonClicked(Team team)
    {
        string roomCode = roomCodeInput.text.Trim();
        if (string.IsNullOrEmpty(roomCode))
        {
            errorCodeText.text = "Error: Ingresa un código de sala válido.";
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
            errorCodeText.text = "Error: No se pudo unir al equipo. Verifica el código o el equipo.";
        }
    }

    // Método para obtener el ID del jugador en la red
    private int GetPlayerId()
    {
        if (NetworkClient.connection != null && NetworkClient.connection.identity != null)
        {
            return (int) NetworkClient.connection.identity.netId;
        }
        return -1; // Valor temporal si no hay conexión
    }
}