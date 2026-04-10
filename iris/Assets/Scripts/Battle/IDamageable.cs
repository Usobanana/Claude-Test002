/// <summary>
/// ダメージを受けられるオブジェクトのインターフェース
/// プレイヤー・エネミー両方に実装する
/// </summary>
public interface IDamageable
{
    bool IsAlive { get; }
    void TakeDamage(float damage, ElementType element = ElementType.Fire);
}
