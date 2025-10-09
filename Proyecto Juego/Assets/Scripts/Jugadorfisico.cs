using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class JugadorFisico : MonoBehaviour
{
    public Animator animator;
    [Header("Física básica")]
    public float masa = 1.2f;
    [Tooltip("1 = gravedad normal; >1 = caída más rápida")]
    public float gravedadRelativa = 1.2f;
    [Range(0f, 1f)] public float coefRestitucion = 0.6f;

    [Header("Movimiento")]
    public float velocidadMax = 6f;
    public float aceleracionSuelo = 60f;
    public float aceleracionAire = 20f;
    public float dragSuelo = 6f;
    public float dragAire = 1f;

    [Header("Salto y Rebote")]
    public float impulsoSalto = 8f;
    [Tooltip("Fracción de la velocidad vertical que se conserva al rebotar")]
    public float reboteSuave = 0.3f;
    public float umbralRebote = 3f;

    [Header("Ground Check")]
    public Transform groundCheck;
    public float groundRadius = 0.08f;
    public LayerMask groundMask;

    [Header("Tiempos de gracia")]
    public float jumpBufferTime = 0.15f;  // margen para no perder saltos
    public float coyoteTime = 0.1f;       // permite saltar poco después de caer

    // Estado
    private bool enSuelo;
    private bool enSueloPrevio;
    private float vyPrevio;

    // Input
    private float inputX;
    private bool jumpPressed;
    private bool jumpHeld;

    // Temporizadores
    private float jumpBufferCounter = 0f;
    private float coyoteCounter = 0f;

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.mass = masa;
        rb.gravityScale = gravedadRelativa;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var mat = new PhysicsMaterial2D("ReboteNatural")
        {
            bounciness = coefRestitucion,
            friction = 0.8f
        };
        rb.sharedMaterial = mat;
    }

    void Update()
    {
        vyPrevio = rb.linearVelocity.y;
        LeerInput();
    }

    void LeerInput()
    {
        // --- Movimiento horizontal (A/D o flechas) ---
        float axisX = 0f;
        try { axisX = Input.GetAxisRaw("Horizontal"); } catch { axisX = 0f; }

        if (Mathf.Approximately(axisX, 0f))
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) axisX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) axisX += 1f;
        }
        inputX = Mathf.Clamp(axisX, -1f, 1f);

        // --- Saltar (detectar pulsación y mantener) ---
        jumpPressed = Input.GetButtonDown("Jump") ||
                      Input.GetKeyDown(KeyCode.Space) ||
                      Input.GetKeyDown(KeyCode.W) ||
                      Input.GetKeyDown(KeyCode.UpArrow);

        jumpHeld = Input.GetButton("Jump") ||
                   Input.GetKey(KeyCode.Space) ||
                   Input.GetKey(KeyCode.W) ||
                   Input.GetKey(KeyCode.UpArrow);

        // Guardar el salto en el buffer unos milisegundos
        if (jumpPressed)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        // --- Detectar suelo ---
        enSueloPrevio = enSuelo;
        if (groundCheck != null)
            enSuelo = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
        else
            enSuelo = false;

        // --- Contador de coyote time (gracia al caer) ---
        if (enSuelo)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.fixedDeltaTime;

        // --- Movimiento horizontal ---
        Vector2 v = rb.linearVelocity;
        float targetVX = inputX * velocidadMax;
        float accel = enSuelo ? aceleracionSuelo : aceleracionAire;
        v.x = Mathf.MoveTowards(v.x, targetVX, accel * Time.fixedDeltaTime);
        rb.linearDamping = enSuelo ? dragSuelo : dragAire;

        // --- SALTO con buffer y coyote time ---
        if (jumpBufferCounter > 0 && coyoteCounter > 0)
        {
            v.y = 0f;
            rb.AddForce(Vector2.up * impulsoSalto, ForceMode2D.Impulse);
            enSuelo = false;
            jumpBufferCounter = 0f; // limpiar buffer tras salto
        }

        // --- SALTO VARIABLE (altura depende del tiempo que mantienes la tecla) ---
        if (!enSuelo && !jumpHeld && rb.linearVelocity.y > 0)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.5f);
        }

        rb.linearVelocity = new Vector2(v.x, rb.linearVelocity.y);

        // --- Voltear sprite según dirección y actualizar animator ---
        float velocidadX = Input.GetAxis("Horizontal") * Time.deltaTime;
        animator.SetFloat("movement", velocidadX * velocidadMax);
        
        if (inputX < 0)
        {
            transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (inputX > 0)
        {
            transform.localScale = new Vector3(1, 1, 1);
        }

        // --- Rebote pequeño al aterrizar ---
        if (!enSueloPrevio && enSuelo)
        {
            float vyImpacto = Mathf.Abs(vyPrevio);
            if (vyImpacto > umbralRebote)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, vyImpacto * reboteSuave);
            else
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
        }
    }

    private void OnCollisionEnter2D(Collision2D col)
    {
        if (col.gameObject.CompareTag("Wall"))
        {
            Vector2 n = col.contacts[0].normal;
            rb.linearVelocity = Vector2.Reflect(rb.linearVelocity, n) * coefRestitucion;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
#endif
}
