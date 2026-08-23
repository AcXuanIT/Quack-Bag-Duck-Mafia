using UnityEngine;

/// <summary>
/// Vịt địch — spawn ở điểm nào đó, tự động di chuyển theo đường thẳng (trục X là chính,
/// trục Y tự điều chỉnh về phía mục tiêu, luôn giới hạn [0,2]) hướng về GameObject "MyTeam"
/// (tag "MyTeam"). Nếu trên đường đi phát hiện 1 UnitDuck (tag "Player") lọt vào tầm đánh,
/// LẬP TỨC đổi mục tiêu ưu tiên sang UnitDuck đó thay vì tiếp tục nhắm tới MyTeam.
/// Khi mục tiêu (Player hoặc MyTeam) lọt vào tầm đánh (weaponRangeCollider) thì dừng di
/// chuyển và tấn công theo chu kỳ weaponData.TimeDelay.
/// GameObject cần được gắn tag "Enemy" để UnitDuck nhận diện.
/// </summary>
public class EnemyDuck : Duck
{
    [Header("=== EnemyDuck - Movement ===")]
    [Tooltip("Tốc độ di chuyển (đơn vị/giây) khi chưa có mục tiêu trong tầm đánh")]
    [SerializeField] private float moveSpeed = 1f;

    // Mục tiêu ưu tiên: 1 UnitDuck (tag "Player") vừa phát hiện được, LUÔN được ưu tiên
    // hơn MyTeam cho tới khi UnitDuck đó chết/biến mất khỏi tầm đánh.
    private Transform _priorityPlayerTarget;
    private Transform _myTeamTransform;

    public override void Init(BaseDuckData duck, WeaponEntry weapon, int duckTierIn, int weaponTierIn)
    {
        base.Init(duck, weapon, duckTierIn, weaponTierIn);

        _priorityPlayerTarget = null;

        if (_myTeamTransform == null)
        {
            var myTeam = FindObjectOfType<MyTeam>();
            _myTeamTransform = myTeam != null ? myTeam.transform : null;
        }
    }

    private void Update()
    {
        if (IsDead) return;

        if (currentTarget != null)
        {
            UpdateAttack();
            return;
        }

        // Chưa có mục tiêu trong tầm đánh -> tiếp tục di chuyển về mục tiêu ưu tiên
        // (UnitDuck vừa phát hiện, nếu có) hoặc mặc định là MyTeam.
        Transform moveTarget = _priorityPlayerTarget != null ? _priorityPlayerTarget : _myTeamTransform;
        if (moveTarget != null)
            AutoMoveTowards(moveTarget.position, moveSpeed);
    }

    protected override void HandleTriggerEnter(Collider2D other)
    {
        if (IsDead) return;

        if (other.CompareTag("Player"))
        {
            // UnitDuck xuất hiện trong tầm đánh -> đổi hướng, ưu tiên tấn công UnitDuck này.
            _priorityPlayerTarget = other.transform;
            currentTarget          = other.transform;
        }
        else if (other.CompareTag("MyTeam") && currentTarget == null)
        {
            currentTarget = other.transform;
        }
    }

    protected override void HandleTriggerExit(Collider2D other)
    {
        if (currentTarget == null || other.transform != currentTarget) return;

        currentTarget = null;
        isAttacking   = false;

        if (other.CompareTag("Player") && other.transform == _priorityPlayerTarget)
            _priorityPlayerTarget = null;
    }
}
