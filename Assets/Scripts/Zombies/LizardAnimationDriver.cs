using UnityEngine;

/// <summary>
/// Legacy visual adapter for Lizard Monster.
/// LizardMonster now uses standard parameters (MoveSpeed, Attack) in Lizard_Controller like Spider.
/// </summary>
[System.Obsolete("LizardMonster now uses standard parameters (MoveSpeed, Attack) in Lizard_Controller like Spider.")]
[RequireComponent(typeof(EnemyNavAgent), typeof(ZombieHealth), typeof(ZombieAttack))]
public sealed class LizardAnimationDriver : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string idleState = "Idle";
    [SerializeField] private string walkState = "Walk";
    [SerializeField] private string attackState = "attack1";
    [SerializeField] private string hitState = "hit";
    [SerializeField] private string deathState = "die";
    [SerializeField, Min(0.05f)] private float attackVisualInterval = 1.05f;
    [SerializeField, Min(0.01f)] private float transitionDuration = 0.08f;

    private EnemyNavAgent navAgent;
    private ZombieHealth health;
    private int currentState;
    private int previousHealth;
    private float nextAttackVisual;
    private Vector3 previousPosition;

    private void Awake()
    {
        // Auto-disable if present so it doesn't conflict with controller transitions
        enabled = false;
        navAgent = GetComponent<EnemyNavAgent>();
        health = GetComponent<ZombieHealth>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        previousPosition = transform.position;
        previousHealth = health != null ? health.currentHealth : 0;
    }

    private void OnEnable()
    {
        if (health != null) previousHealth = health.currentHealth;
        currentState = 0;
        nextAttackVisual = 0f;
    }

    private void Update()
    {
        if (animator == null || health == null || navAgent == null) return;

        if (health.currentHealth <= 0)
        {
            Play(deathState, true);
            return;
        }

        if (health.currentHealth < previousHealth)
        {
            Play(hitState, true);
            previousHealth = health.currentHealth;
            return;
        }
        previousHealth = health.currentHealth;

        bool attacking = navAgent.BlockingPlant != null || navAgent.IsAtHouse;
        if (attacking)
        {
            if (Time.time >= nextAttackVisual)
            {
                Play(attackState, true);
                nextAttackVisual = Time.time + attackVisualInterval;
            }
            previousPosition = transform.position;
            return;
        }

        float moved = Vector3.Distance(transform.position, previousPosition);
        Play(moved > 0.002f ? walkState : idleState, false);
        previousPosition = transform.position;
    }

    private void Play(string stateName, bool restart)
    {
        int state = Animator.StringToHash(stateName);
        if (restart || currentState != state)
        {
            animator.CrossFade(state, transitionDuration, 0, restart ? 0f : float.NegativeInfinity);
            currentState = state;
        }
    }
}
