using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public class JugadorFisicoAvanzado : MonoBehaviour
{
    [Header("----- COMPONENTES -----")]
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    public Transform groundCheck;

    [Header("----- ESTADO ACTUAL -----")]
    public bool modoBola = false;
    private bool enSuelo;
    private bool enSueloPrevio;
    private float vyPrevio;

    [Header("----- MODO PERSONAJE -----")]
    public float masaHumano = 1.5f;
    public float gravedadHumano = 1.1f;
    public float velocidadCaminar = 6f;
    public float saltoHumano = 8f;

    [Header("----- MODO BOLA -----")]
    public float masaBola = 3.5f;
    public float gravedadBola = 1.3f;
    public float fuerzaRodar = 400f;
    public float limiteVelocidadBola = 10f;
    public float saltoBola = 10f;
    public float fuerzaImpactoRompible = 8f;

    [Header("----- DETECCIÓN DE SUELO -----")]
    public float groundRadius = 0.1f;
    public LayerMask groundMask;

    [Header("----- ESCALA Y COLOR -----")]
    public Vector3 escalaHumano = new Vector3(1f, 2f, 1f); // mide 2 bloques
    public Vector3 escalaBola = new Vector3(1f, 1f, 1f);   // mide 1 bloque
    public Color colorHumano = Color.white;
    public Color colorBola = Color.cyan;

    [Header("----- COLISIONES -----")]
    public PhysicsMaterial2D materialNormal;
    public PhysicsMaterial2D materialBola;

    private float inputX;
    private bool jumpPressed;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        rb.freezeRotation = true;
        CambiarModo(false);
    }

    void Update()
    {
        vyPrevio = rb.linearVelocity.y;
        LeerInput();

        if (Input.GetKeyDown(KeyCode.Q))
        {
            modoBola = !modoBola;
            CambiarModo(modoBola);
        }
    }

    void LeerInput()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        jumpPressed = Input.GetButtonDown("Jump") ||
                      Input.GetKeyDown(KeyCode.Space) ||
                      Input.GetKeyDown(KeyCode.W) ||
                      Input.GetKeyDown(KeyCode.UpArrow);
    }

    void FixedUpdate()
    {
        enSueloPrevio = enSuelo;
        if (groundCheck != null)
            enSuelo = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
        else
            enSuelo = false;

        if (modoBola)
            FisicaBola();
        else
            FisicaHumano();
    }

    // =====================
    // 🔹 MODO PERSONAJE
    // =====================
    void FisicaHumano()
    {
        Vector2 v = rb.linearVelocity;
        v.x = inputX * velocidadCaminar;
        rb.linearVelocity = new Vector2(v.x, rb.linearVelocity.y);

        if (jumpPressed && enSuelo)
        {
            rb.AddForce(Vector2.up * saltoHumano, ForceMode2D.Impulse);
        }
    }

    // =====================
    // 🔹 MODO BOLA
    // =====================
    void FisicaBola()
    {
        float move = Input.GetAxis("Horizontal");
        rb.AddForce(Vector2.right * move * fuerzaRodar * Time.fixedDeltaTime);

        // limitar velocidad horizontal
        rb.linearVelocity = new Vector2(
            Mathf.Clamp(rb.linearVelocity.x, -limiteVelocidadBola, limiteVelocidadBola),
            rb.linearVelocity.y
        );

        // salto especial (rebote mayor)
        if (jumpPressed && enSuelo)
        {
            rb.AddForce(Vector2.up * saltoBola, ForceMode2D.Impulse);
        }
    }

    // =====================
    // 🔹 CAMBIO DE MODO
    // =====================
    void CambiarModo(bool bola)
    {
        if (bola)
        {
            // ⚪ Se convierte en bola
            sr.color = colorBola;
            transform.localScale = escalaBola;
            rb.mass = masaBola;
            rb.gravityScale = gravedadBola;
            rb.sharedMaterial = materialBola;
        }
        else
        {
            // 🧍‍♂️ Vuelve a personaje
            sr.color = colorHumano;
            transform.localScale = escalaHumano;
            rb.mass = masaHumano;
            rb.gravityScale = gravedadHumano;
            rb.sharedMaterial = materialNormal;
        }
    }

    // =====================
    // 🔹 COLISIONES
    // =====================
    private void OnCollisionEnter2D(Collision2D col)
    {
        // Si está en modo bola y choca con fuerza, romper objeto rompible
        if (modoBola && col.relativeVelocity.magnitude > fuerzaImpactoRompible)
        {
            if (col.gameObject.CompareTag("Rompible"))
            {
                Destroy(col.gameObject);
                Debug.Log("💥 Objeto rompible destruido!");
            }
        }

        // Empuje de cajas solo si está en modo bola
        if (modoBola && col.rigidbody != null)
        {
            Vector2 dir = col.contacts[0].normal * -1f;
            col.rigidbody.AddForce(dir * 5f, ForceMode2D.Impulse);
        }
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
