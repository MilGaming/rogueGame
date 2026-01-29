using System;

using UnityEngine;
using UnityEngine.Events;
using System.Collections;

public class Player : MonoBehaviour
{
    [SerializeField] float _health = 100;
    UI _ui;
    float _score = 0;
    public event Action<GameObject /*killer*/> OnDied;

    private bool _isInvinsible = false;
    private bool _isStunning = false;
    private float _stunDuration = 1f;
    private bool _isDealingDmg = false;
    private float _damage = 5f;


    bool _isRespawning;

    private void Start()
    {
        _ui = FindAnyObjectByType<UI>();
    }
    public void TakeDamage(float damage, GameObject attacker)
    {
        if (_isInvinsible) return;
        _health -= damage;
        if (_health <= 0)
        {
            OnDied?.Invoke(attacker);
            //Destroy(gameObject);
            StartCoroutine(RespawnRoutine());
        }
        _ui.updateHealth(_health);
    }

    public void IncreaseScore(float scoreIncrease)
    {
        _score += scoreIncrease;
        _ui.updateScore(_score);
    }

    public void Heal(float health)
    {
        _health += health;
        _ui.updateHealth(_health);
    }

    public void TeleportTo(Vector3 worldPos)
    {
        transform.position = worldPos;
    }
    IEnumerator RespawnRoutine() {
        _isRespawning = true;
        var col = GetComponent<Collider2D>();
        if (col) col.enabled = false;
        yield return new WaitForSeconds(0.2f);
        _health = 100;
        _ui.updateHealth(_health);

        if (col) col.enabled = true;
        _isRespawning = false;
    }

    public void SetStunning(bool isStunning, float stunDuration)
    {
        _isStunning = isStunning;
        _stunDuration = stunDuration;
    }

    public void SetDealingDamage(bool isDealingdmg, float damageAmount)
    {
        _isDealingDmg = isDealingdmg;
        _damage = damageAmount;
    }

    public void SetInvinsible(bool isInvinsible)
    {
        _isInvinsible = isInvinsible;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            if (_isStunning)
            {
                enemy.ApplyStun(_stunDuration);
            }
            if (_isDealingDmg)
            {
                enemy.TakeDamage(_damage);
            }
        }
    }



}
