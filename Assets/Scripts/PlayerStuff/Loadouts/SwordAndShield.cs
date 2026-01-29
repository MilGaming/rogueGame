using System.Collections;
using UnityEngine;

public class SwordAndShield : LoadoutBase
{
    GameObject _shield;
    SpriteRenderer _shieldRenderer;
    Collider2D _shieldCollider;

    GameObject _sword;
    SwordHitbox _swordhitbox;

    GameObject _heavySword;
    SwordHitbox _heavySwordhitbox;

    float _shieldUpTime = 2f;

    float _stunDuration = 2f;

    float _heavyStunDuration = 5f;

    float _heavyStunDamage = 20f;

    public SwordAndShield(Player player) : base(player)
    {
        _shield = GameObject.FindGameObjectWithTag("Shield");

        if (_shield != null)
        {
            _shieldRenderer = _shield.GetComponent<SpriteRenderer>();
            _shieldCollider = _shield.GetComponent<Collider2D>();
        }

        _sword = GameObject.FindGameObjectWithTag("Sword");
        if (_sword != null)
        {
            _swordhitbox = _sword.GetComponent<SwordHitbox>();
        }

        _heavySword = GameObject.FindGameObjectWithTag("HeavySword");
        if (_heavySword != null)
        {
            _heavySwordhitbox = _heavySword.GetComponent<SwordHitbox>();
        }
    }

    public override IEnumerator LightAttack(Vector2 mousePos)
    {
        yield return MeleeAttack(mousePos, _sword, _swordhitbox, 1.3f, _lightDamage);
    }

    public override IEnumerator HeavyAttack(Vector2 mousePos)
    {
        yield return MeleeAttack(mousePos, _heavySword, _heavySwordhitbox, 4f, _heavyDamage);
    }

    public override IEnumerator LightDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
        _player.SetStunning(true, _stunDuration);
        yield return base.LightDash(direction, transform, mousePos);
        _player.SetStunning(false, _stunDuration);
    }

    public override IEnumerator HeavyDash(Vector2 direction, Transform transform, Vector2 mousePos)
    {
        _player.SetStunning(true, _heavyStunDuration);
        _player.SetDealingDamage(true, _heavyStunDamage);
        _player.SetInvinsible(true);
        yield return base.HeavyDash(direction, transform, mousePos);
        _player.SetStunning(false, _heavyStunDuration);
        _player.SetDealingDamage(false, _heavyStunDamage);
        _player.SetInvinsible(false);
    }

    public override IEnumerator Defense(Vector2 mousePos)
    {
        if (_shield == null) yield break;

        if (_shieldRenderer) _shieldRenderer.enabled = true;
        if (_shieldCollider) _shieldCollider.enabled = true;

        Transform player = _shield.transform.parent;
        Vector2 playerPos = player.position;

        Vector2 toMouse = mousePos - playerPos;
        Vector2 dir = toMouse.sqrMagnitude > 0.000001f ? toMouse.normalized : Vector2.right;

        float shieldDistance = 1.5f;

        // world-space placement
        Vector3 pos = playerPos + dir * shieldDistance;
        pos.z = _shield.transform.position.z;
        _shield.transform.position = pos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        _shield.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        yield return new WaitForSeconds(_shieldUpTime);
        if (_shieldRenderer) _shieldRenderer.enabled = false;
        if (_shieldCollider) _shieldCollider.enabled = false;
    }

    private IEnumerator MeleeAttack(Vector2 mousePos, GameObject mySword, SwordHitbox mySwordHitbox, float distance, float damage)
    {
        if (mySwordHitbox == null) yield break;

        Transform player = mySwordHitbox.transform.parent;
        Vector2 playerPos = player.position;

        Vector2 toMouse = mousePos - playerPos;
        Vector2 dir = toMouse.sqrMagnitude > 0.000001f ? toMouse.normalized : Vector2.right;

        // world-space placement
        Vector3 pos = playerPos + dir * distance;
        pos.z = mySword.transform.position.z;
        mySword.transform.position = pos;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        mySword.transform.rotation = Quaternion.Euler(0f, 0f, angle + 90f);

        // short active window
        mySwordHitbox.Activate(damage, 0.1f);

        yield return new WaitForSeconds(_attackSpeed);
    }
}


