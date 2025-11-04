using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] float _health = 100;
    [SerializeField] UI _ui;
    float _score = 0;
    public event Action<GameObject /*killer*/> OnDied;

    public void TakeDamage(float damage, GameObject attacker)
    {
        _health -= damage;
        if (_health <= 0)
        {
            OnDied?.Invoke(attacker);
            Destroy(gameObject);
        }
        _ui.updateHealth(_health);
    }

    public void IncreaseScore(float scoreIncrease)
    {
        _score += scoreIncrease;
        _ui.updateScore(_score);
    }
}
