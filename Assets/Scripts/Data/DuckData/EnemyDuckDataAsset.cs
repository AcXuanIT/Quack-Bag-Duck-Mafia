using UnityEngine;

/// <summary>
/// Wrapper ScriptableObject để lưu 1 EnemyDuckData (class thuần) thành asset độc lập
/// trong Project (vì EnemyDuckData không tự là ScriptableObject).
/// </summary>
[CreateAssetMenu(fileName = "EnemyDuckDataAsset", menuName = "Game/Duck/Enemy Duck Data Asset")]
public class EnemyDuckDataAsset : ScriptableObject
{
    public EnemyDuckData Data;
}
