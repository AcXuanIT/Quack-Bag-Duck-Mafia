using UnityEngine;

/// <summary>
/// Wrapper ScriptableObject để lưu 1 MyDuckData (class thuần) thành asset độc lập
/// trong Project (vì MyDuckData không tự là ScriptableObject).
/// </summary>
[CreateAssetMenu(fileName = "MyDuckDataAsset", menuName = "Game/Duck/My Duck Data Asset")]
public class MyDuckDataAsset : ScriptableObject
{
    public MyDuckData Data;
}
