using System.Collections.Generic;
using UnityEngine;

public enum Team
{
    None,
    TeamA,
    TeamB
}

[System.Serializable]
public class Room
{
    public string roomCode;
    public List<PlayerInfo> players = new List<PlayerInfo>();
    public int maxPlayers = 4;

    public bool IsFull()
    {
        return players.Count >= maxPlayers;
    }

    public bool JoinTeam(Team team, string playerName, int playerId)
    {
        int teamCount = players.FindAll(p => p.team == team).Count;
        if (teamCount >= 2)
        {
            Debug.LogWarning($"El equipo {team} en la sala {roomCode} ya está lleno.");
            return false;
        }

        players.Add(new PlayerInfo { playerName = playerName, team = team, playerId = playerId });
        Debug.Log($"{playerName} se ha unido al equipo {team} en la sala {roomCode}");
        return true;
    }

    public void LeaveRoom(int playerId)
    {
        var player = players.Find(p => p.playerId == playerId);
        if (player != null)
        {
            players.Remove(player);
            Debug.Log($"Jugador {player.playerName} ha salido de la sala {roomCode}");
        }
    }
}