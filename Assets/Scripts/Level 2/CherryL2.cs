using System;
using UnityEngine;

public class CherryL2 : MonoBehaviour
{
    [SerializeField] private int scoreValue = 20;
    public Action<GameObject> OnCollected;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Level2Manager.I?.AddScore(scoreValue);
            Level2Manager.I?.GetLife();
            AudioManager.I?.PlaySfx(SfxEvent.PowerPickup, gameObject);
            Destroy(gameObject);
        }
    }
}