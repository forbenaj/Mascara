using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Keeps all rooms loaded; toggles visibility/activation when switching.
public class RoomManager : MonoBehaviour
{
    [Tooltip("Room to start in. If empty, first child Room is used.")]
    public string startRoomId;

    [Tooltip("Rooms registered in the scene (auto-filled on Awake if empty).")]
    public List<Room> rooms = new List<Room>();

    private Room _current;

    private void Awake()
    {
        if (rooms == null || rooms.Count == 0)
        {
            rooms = GetComponentsInChildren<Room>(true).ToList();
        }

        if (rooms.Count == 0)
        {
            Debug.LogWarning("RoomManager: no rooms found.");
            return;
        }

        var target = !string.IsNullOrEmpty(startRoomId)
            ? rooms.FirstOrDefault(r => r.roomId == startRoomId)
            : rooms[0];

        SetCurrentRoom(target);
    }

    public void SetCurrentRoom(Room room)
    {
        if (room == null) return;
        _current = room;

        foreach (var r in rooms)
        {
            bool active = r == room;
            if (r.gameObject.activeSelf != active)
                r.gameObject.SetActive(active);
        }

        _current.OnEnter();
    }

    public Room GetRoomById(string id) => rooms.FirstOrDefault(r => r.roomId == id);
}
