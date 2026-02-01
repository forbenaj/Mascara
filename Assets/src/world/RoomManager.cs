using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Keeps all rooms loaded; toggles visibility/activation when switching.
public class RoomManager : MonoBehaviour
{
    [Tooltip("Room to start in. If empty, first child Room is used.")]
    public string startRoomId;

    [Tooltip("Offset to place the player when spawning in the first room.")]
    public Vector2 startSpawnOffset = Vector2.zero;

    [Tooltip("Player tag for teleport/spawn.")]
    public string playerTag = "Player";

    [Tooltip("Rooms registered in the scene (auto-filled on Awake if empty).")]
    public List<Room> rooms = new List<Room>();

    private Room _current;
    public Room CurrentRoom => _current;

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

        var player = FindPlayer();
        SetCurrentRoom(target, startSpawnOffset, player);
    }

    public void SetCurrentRoom(Room room, Vector2 spawnOffset = default, Transform player = null)
    {
        if (room == null) return;
        _current = room;

        foreach (var r in rooms)
        {
            bool active = r == room;
            if (r.gameObject.activeSelf != active)
                r.gameObject.SetActive(active);
        }

        if (player == null)
            player = FindPlayer();

        if (player != null)
        {
            Vector3 pos = room.transform.position + (Vector3)spawnOffset;

            // If no offset provided, use a SpawnPoint inside the room if present.
            if (spawnOffset == Vector2.zero)
            {
                var spawn = room.GetComponentInChildren<SpawnPoint>();
                if (spawn != null)
                    pos = spawn.transform.position;
            }

            player.position = pos;
        }

        // Snap camera to new room before OnEnter side-effects.
        CameraFollowRoom[] cams;
#if UNITY_2022_2_OR_NEWER
        cams = Object.FindObjectsByType<CameraFollowRoom>(FindObjectsSortMode.None);
#else
        cams = Object.FindObjectsOfType<CameraFollowRoom>();
#endif
        foreach (var cam in cams)
            cam.SnapToRoom(room, player);

        _current.OnEnter();

        // If room has a BossRoomController, trigger intro SFX.
        var bossCtrl = room.GetComponent<BossRoomController>();
        if (bossCtrl != null)
            bossCtrl.PlayAppear();
    }

    public Room GetRoomById(string id) => rooms.FirstOrDefault(r => r.roomId == id);

    private Transform FindPlayer()
    {
        var go = GameObject.FindGameObjectWithTag(playerTag);
        return go != null ? go.transform : null;
    }
}
