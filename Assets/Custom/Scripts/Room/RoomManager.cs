using System.Collections.Generic;
using UnityEngine;
using Mirror;
using System.Linq;

public class RoomManager : NetworkBehaviour
{
    public static RoomManager Instance;

    [SyncVar]
    public List<Room> rooms = new List<Room>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string CreateRoom(string roomCode)
    {
        roomCode ??= GenerateRoomCode();
        rooms.Add(new Room { roomCode = roomCode });
        Debug.Log($"Sala creada con código: {roomCode}");
        return roomCode;
    }

    public Room GetRoomByCode(string roomCode)
    {
        return rooms.Find(room => room.roomCode == roomCode);
    }

    public bool JoinRoom(string roomCode, Team team, string playerName, int playerId)
    {
        var room = GetRoomByCode(roomCode);
        if (room == null)
        {
            Debug.LogWarning($"La sala con código {roomCode} no existe.");
            return false;
        }

        if (room.IsFull())
        {
            Debug.LogWarning($"La sala {roomCode} está llena.");
            return false;
        }

        return room.JoinTeam(team, playerName, playerId);
    }

    private string GenerateRoomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new System.Random();
        return new string(System.Linq.Enumerable.Repeat(chars, 6)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}