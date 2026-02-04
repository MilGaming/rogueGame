using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
public class LoadoutState : MonoBehaviour
{

    [SerializeField] private Player player;

    [SerializeField] private PlayerAnimDriver anim;

    public enum ActionAnim
    {
        None,
        LightAttack,
        HeavyAttack,
        Dash,
        Block
    }


    [Header("Movement")]
    public float maxSpeed = 8f;
    public float accel = 100f;
    public float decel = 100f;

    Vector2 vel;
    bool blockedMovement = false;
    bool blockedActions = false;



    private float dashPressTime;
    private bool dashHeld;
    [SerializeField] private float heavyDashHoldTime = 0.4f;

    private LoadoutBase loadout;
    private Vector2 mousePos;

    private float nextHeavyDashTime;
    private float nextDefTime;
    private float nextDashTime;

    private Coroutine dashLockRoutine;


    [Header("Input (assign from your Controls asset)")]
    public InputActionReference move;   // Gameplay/Move (Vector2)
    public InputActionReference point;  // Gameplay/Look (Vector2) -> <Pointer>/position
    public InputActionReference def;  // Gameplay/Block (Button)
    public InputActionReference dash;   // Gameplay/Dash (Button)
    public InputActionReference attack;   // Gameplay/Dash (Button)

    public InputActionReference loadout1;
    public InputActionReference loadout2;
    public InputActionReference loadout3;  


    Camera cam;

    void Awake()
    {
        cam = Camera.main;
    }
    void Start()
    {
        loadout = new LoadoutBase(player);
    }

    void OnEnable()
    {
        attack.action.performed += OnAttack;
        dash.action.started += OnDashStarted;
        dash.action.canceled += OnDashCanceled;
        def.action.performed += OnDefense;

        loadout1.action.performed += OnLoadout1;
        loadout2.action.performed += OnLoadout2;
        loadout3.action.performed += OnLoadout3;

        loadout1.action.Enable();
        loadout2.action.Enable();
        loadout3.action.Enable();

        move.action.Enable();
        point.action.Enable();
        def.action.Enable();
        attack.action.Enable();
        dash.action.Enable();

    }

    void OnDisable()
    {
        attack.action.performed -= OnAttack;
        dash.action.started -= OnDashStarted;
        dash.action.canceled -= OnDashCanceled;
        def.action.performed -= OnDefense;

        loadout1.action.performed -= OnLoadout1;
        loadout2.action.performed -= OnLoadout2;
        loadout3.action.performed -= OnLoadout3;

        loadout1.action.Disable();
        loadout2.action.Disable();
        loadout3.action.Disable();

        move.action.Disable();
        point.action.Disable();
        def.action.Disable();
        attack.action.Disable();
        dash.action.Disable();
    }

    void OnLoadout1(InputAction.CallbackContext ctx)
    {
        loadout = new TwoCrossbow(player);
    }

    void OnLoadout2(InputAction.CallbackContext ctx)
    {
        loadout = new SwordAndShield(player);
    }

    void OnLoadout3(InputAction.CallbackContext ctx)
    {
        loadout = new DualSwords(player);
    }

    void OnAttack(InputAction.CallbackContext ctx)
    {
        if (blockedActions) return;
        blockedActions = true;
        if (ctx.interaction is HoldInteraction)
            StartCoroutine(DoAttack(true));
        else if (ctx.interaction is TapInteraction)
            StartCoroutine(DoAttack(false));
    }

    IEnumerator DoAttack(bool heavy)
    {
        float duration;
        IEnumerator gameplay;
        ActionAnim animType;
        string tag;

        if (heavy)
        {
            duration = loadout.GetHeavyAttackDuration();
            gameplay = loadout.HeavyAttack(mousePos);
            animType = ActionAnim.HeavyAttack;
            tag = "Special";
        }
        else
        {
            duration = loadout.GetLightAttackDuration();
            gameplay = loadout.LightAttack(mousePos);
            animType = ActionAnim.LightAttack;
            tag = "Attack";
        }

        yield return RunAction(duration, gameplay, animType, tag);
    }

    void OnDashStarted(InputAction.CallbackContext ctx)
    {
        if (blockedActions) return;
        dashHeld = true;

        dashPressTime = Time.time;

        blockedActions = true;
        dashLockRoutine = StartCoroutine(LockMovementAfterDelay());
    }
    IEnumerator LockMovementAfterDelay()
    {
        yield return new WaitForSeconds(heavyDashHoldTime);

        if (Time.time < nextHeavyDashTime)
        {
            yield return new WaitForSeconds(nextHeavyDashTime-Time.time);
        }

        blockedMovement = true;
    }

    void OnDashCanceled(InputAction.CallbackContext ctx)
    {
        if (!dashHeld) return;
        dashHeld = false;
        if (dashLockRoutine != null)
        {
            StopCoroutine(dashLockRoutine);
            dashLockRoutine = null;
        }
        bool heavy = (Time.time - dashPressTime) >= heavyDashHoldTime;
        StartCoroutine(DoDash(heavy));
    }


    IEnumerator DoDash(bool heavy)
    {
        try
        {
            // Cooldown checks
            if (heavy)
            {
                if (Time.time < nextHeavyDashTime)
                    yield break;

                nextHeavyDashTime = Time.time + loadout.getHeavyDashCD();
            }
            else
            {
                if (Time.time < nextDashTime)
                    yield break;

                nextDashTime = Time.time + loadout.getLightDashCD();
            }

            blockedMovement = true;

            float duration = heavy ? loadout.GetHeavyDashDuration()
                                   : loadout.GetLightDashDuration();

            IEnumerator gameplay = heavy ? loadout.HeavyDash(transform, mousePos) : loadout.LightDash(vel, transform, mousePos);

            yield return RunAction(duration, gameplay, ActionAnim.Dash, "Dash");
        }
        finally
        {
            blockedMovement = false;
            blockedActions = false;

            // also cancel the delayed movement lock if it was started
            if (dashLockRoutine != null)
            {
                StopCoroutine(dashLockRoutine);
                dashLockRoutine = null;
            }

            // make sure InAction isn't stuck on
            anim.SetInAction(false);
        }
    }



    void OnDefense(InputAction.CallbackContext ctx)
    {
        if (blockedActions || Time.time < nextDefTime) return;
        blockedActions = true;

        StartCoroutine(DoDefense());
    }

    IEnumerator DoDefense()
    {
        nextDefTime = Time.time + loadout.getDefenseCD();

        float duration = loadout.GetDefenseDuration();
        IEnumerator gameplay = loadout.Defense(mousePos);

        yield return RunAction(duration, gameplay, ActionAnim.Block, "Defense");
    }

    IEnumerator RunAction(float duration, IEnumerator gameplayRoutine, ActionAnim animToPlay, string stateTag)
    {
        blockedActions = true;
        anim.SetInAction(true);
        Animator animator = anim.GetAnimator();

        // Fire animation trigger
        switch (animToPlay)
        {
            case ActionAnim.LightAttack: anim.TriggerAttack(); break;
            case ActionAnim.HeavyAttack: anim.TriggerSpecial(); break;
            case ActionAnim.Dash: anim.TriggerDash(); break;
            case ActionAnim.Block: anim.TriggerDefense(); break;
        }

        // Wait until the animator
        while (animator.GetCurrentAnimatorStateInfo(0).IsTag(stateTag))
            yield return null;

        // read clip length
        float clipLen = anim.GetCurrentClipLengthSeconds(0);

        float prevSpeed = animator.speed;

        if (clipLen > 0.001f && duration > 0.001f)
            animator.speed = clipLen / duration;
        else
            animator.speed = 1f;

        //  parallel
        Coroutine gameplay = null;
        if (gameplayRoutine != null)
            gameplay = StartCoroutine(gameplayRoutine);

        // Lock for your gameplay duration
        yield return new WaitForSeconds(duration);

        // Ensure gameplay finished too
        if (gameplay != null) yield return gameplay;

        animator.speed = prevSpeed;
        anim.SetInAction(false);
        blockedActions = false;
    }





    private void Update()
    {
        // World-space mouse
        Vector2 mouseScreen = point.action.ReadValue<Vector2>();
        float zDist = Mathf.Abs(cam.transform.position.z - transform.position.z);
        Vector3 mouseWorld3 = cam.ScreenToWorldPoint(new Vector3(mouseScreen.x, mouseScreen.y, zDist));
        mouseWorld3.z = transform.position.z; // lock to 2D plane
        mousePos = mouseWorld3;

        // Move player
        Vector2 input = move.action.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input.Normalize();
        Vector2 targetVel = input * maxSpeed;
        float rate = (input.sqrMagnitude > 0.0001f) ? accel : decel;
        vel = Vector2.MoveTowards(vel, targetVel, rate * Time.deltaTime);

        // Facing direction (mouse -> player)
        Vector2 lookDir = mousePos - (Vector2)transform.position;
        if (lookDir.sqrMagnitude > 0.0001f) lookDir.Normalize();
        else lookDir = Vector2.down;

        Vector2 animVel = blockedMovement ? Vector2.zero : vel; //make shit zero so standing still works
        // update animation
        anim.UpdateLocomotion(lookDir, animVel);

        if (blockedMovement) return;
        transform.position += (Vector3)(vel * Time.deltaTime);



    }



}
