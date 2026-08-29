using UnityEngine;

/// <summary>
/// GameData - Lưu trữ các giá trị chung của game hiện tại (session data).
/// Gắn vào GameObject con của GameManager và gán tay vào GameManager.gameData.
/// </summary>
public class GameData : MonoBehaviour
{
    [Tooltip("MapBattleData của màn/level đang chiến đấu")]
    public MapBattsleData CurrentWave;
}
