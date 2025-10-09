using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class JugadorFisicoBaseAvanzado : MonoBehaviour
{
    [Header("----- COMPONENTES -----")]
    private Rigidbody2D rb;
    private Collider2D col;
    private SpriteRenderer sr;

    [Header("----- FÍSICA BÁSICA -----")]
    public float masa = 1.2f;
    [Tooltip("1 = gravedad normal; >1 = caída más rápida")]
    public float gravedadRelativa = 1.0f;
    [Range(0f, 1f)] public float coefRestitucion = 0.85f; // Rebote global

    [Header("----- MOVIMIENTO -----")]
    public float velocidadMax = 6f;
    public float aceleracionSuelo = 60f;
    public float aceleracionAire = 20f;
    public float dragSuelo = 3.5f;
    public float dragAire = 0f; // 0 para no amortiguar el salto

    [Header("----- SALTO Y REBOTE -----")]
    public float impulsoSalto = 11f;
    public float reboteSuave = 0.3f;
    public float umbralRebote = 3f;
    public float impulsoReboteVerticalExtra = 2.5f; // fuerza de rebote manual extra

    [Header("----- DETECCIÓN DE SUELO -----")]
    public Transform groundCheck;
    public float groundRadius = 0.18f;
    public LayerMask groundMask;

    [Header("----- TIEMPOS DE GRACIA -----")]
    public float jumpBufferTime = 0.20f;
    public float coyoteTime = 0.15f;

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

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        sr = GetComponent<SpriteRenderer>();

        rb.mass = masa;
        rb.gravityScale = gravedadRelativa;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        // Material físico para rebotes
        var mat = new PhysicsMaterial2D("ReboteNatural")
        {
            bounciness = coefRestitucion,
            friction = 0.6f
        };
        rb.sharedMaterial = mat;
    }

    void Update()
    {
        vyPrevio = rb.linearVelocity.y;
        LeerInput();

        if (jumpPressed)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;
    }

    void FixedUpdate()
    {
        // --- Detección de suelo robusta ---
        enSueloPrevio = enSuelo;
        bool porOverlap = groundCheck && Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
        bool porContacto = col.IsTouchingLayers(groundMask);
        enSuelo = porOverlap || porContacto;

        // --- Coyote time ---
        if (enSuelo)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.fixedDeltaTime;

        MovimientoHorizontal();
        SaltoAvanzado();
    }

    void MovimientoHorizontal()
    {
        Vector2 v = rb.linearVelocity;
        float targetVX = inputX * velocidadMax;
        float accel = enSuelo ? aceleracionSuelo : aceleracionAire;

        v.x = Mathf.MoveTowards(v.x, targetVX, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(v.x, rb.linearVelocity.y);

        rb.linearDamping = enSuelo ? dragSuelo : dragAire;
    }

    void SaltoAvanzado()
    {
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * impulsoSalto, ForceMode2D.Impulse);

            enSuelo = false;
            jumpBufferCounter = 0f;
        }

        if (!enSuelo && !jumpHeld && rb.linearVelocity.y > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * 0.6f);
        }
    }

    // ===============================
    // 🔹 Rebote físico con detección angular
    // ===============================
    private void OnCollisionEnter2D(Collision2D colision)
    {
        if (colision.contacts.Length == 0) return;

        Vector2 normal = colision.contacts[0].normal.normalized;
        Vector2 v = rb.linearVelocity;
        float velocidadImpacto = Vector2.Dot(-v, normal); // componente perpendicular del impacto

        // Solo rebota si el impacto fue fuerte (evita vibraciones)
        if (velocidadImpacto > 2f)
        {
            // Rebote físico: v' = v - (1 + e)(v·n)n
            Vector2 vReflejada = v - (1 + coefRestitucion) * Vector2.Dot(v, normal) * normal;

            // Si el impacto fue desde arriba, añade impulso extra vertical
            float anguloImpacto = Vector2.Dot(normal, Vector2.up);
            if (anguloImpacto > 0.75f)
            {
                vReflejada.y = Mathf.Abs(vReflejada.y) + 1.5f; // rebote visible solo si viene desde arriba
            }

            rb.linearVelocity = vReflejada;
        }
        else
        {
            // Si el impacto fue débil, anula el rebote y deja que fricción y gravedad actúen
            rb.linearVelocity = new Vector2(v.x * 0.9f, v.y * 0.5f);
        }
    }


    void LeerInput()
    {
        float axisX = 0f;
        try { axisX = Input.GetAxisRaw("Horizontal"); } catch { axisX = 0f; }

        if (Mathf.Approximately(axisX, 0f))
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) axisX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) axisX += 1f;
        }
        inputX = Mathf.Clamp(axisX, -1f, 1f);

        jumpPressed = Input.GetButtonDown("Jump") ||
                      Input.GetKeyDown(KeyCode.Space) ||
                      Input.GetKeyDown(KeyCode.W) ||
                      Input.GetKeyDown(KeyCode.UpArrow);

        jumpHeld = Input.GetButton("Jump") ||
                   Input.GetKey(KeyCode.Space) ||
                   Input.GetKey(KeyCode.W) ||
                   Input.GetKey(KeyCode.UpArrow);
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = enSuelo ? Color.green : Color.cyan;
            Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
        }
    }
#endif
}
