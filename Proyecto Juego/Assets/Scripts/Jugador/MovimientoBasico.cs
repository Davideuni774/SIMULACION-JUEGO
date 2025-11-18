using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Collider2D))]
public class MovimientoBasico : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] float velocidad = 5f;
    [SerializeField] float aceleracion = 20f;
    [SerializeField] float desaceleracion = 30f;

    [Header("Salto")]
    [SerializeField] float fuerzaSalto = 7.5f;
    [SerializeField] float tiempoCoyote = 0.1f;
    [SerializeField] float bufferSalto = 0.1f;
    [SerializeField] bool permitirSaltoAire = false;

    [Header("Física")]
    [SerializeField] float peso = 1f;
    [SerializeField] float gravedadBase = 3f;

    [Header("Capas")]
    [SerializeField] LayerMask sueloLayers;
    [SerializeField] LayerMask paredLayers;
    [SerializeField] LayerMask letalesLayers;

    [Header("Chequeos")]
    [SerializeField] Transform puntoSuelo;
    [SerializeField] float radioSuelo = 0.2f;

    [Header("Animator")]
    [SerializeField] string paramVelocidad = "Speed";
    [SerializeField] string paramEnSuelo = "IsGrounded";
    [SerializeField] string paramVelocidadY = "YVelocity";
    [SerializeField] string paramSaltarTrigger = "Jump";

    Rigidbody2D rb;
    [SerializeField] Animator anim;
    Collider2D col;

    [Header("Visual")]
    [SerializeField] bool girarSprite = false;
    SpriteRenderer srMain;

    float inputX;
    bool mirandoDerecha = true;
    bool enSuelo;
    float ultimoTiempoEnSuelo;
    float ultimoTiempoSalto;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        srMain = GetComponentInChildren<SpriteRenderer>();

        if (col == null)
        {
            col = gameObject.AddComponent<BoxCollider2D>();
            col.isTrigger = false;
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && col is BoxCollider2D bc)
            {
                var b = sr.bounds;
                bc.size = new Vector2(b.size.x, b.size.y);
            }
        }

        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }
        rb.mass = Mathf.Max(0.01f, peso);
        rb.gravityScale = gravedadBase * Mathf.Max(0.1f, peso);

        if (sueloLayers.value == 0)
        {
            int m = LayerMask.GetMask("Ground");
            if (m == 0) m = LayerMask.GetMask("Default");
            sueloLayers = m;
        }
    }

    void Start() { }

    void Update()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        if (Input.GetButtonDown("Jump"))
            ultimoTiempoSalto = Time.time;

        ActualizarEstadoSuelo();
        ActualizarAnimator();
        if (girarSprite) GestionarGiro();
        IntentarSaltar();
    }

    void FixedUpdate()
    {
        Mover();
    }

    void Mover()
    {
        float objetivo = inputX * velocidad;
        float velX = rb.linearVelocity.x;
        float tasa = Mathf.Abs(Mathf.Abs(objetivo) > 0.01f ? aceleracion : desaceleracion);
        float nuevoX = Mathf.MoveTowards(velX, objetivo, tasa * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(nuevoX, rb.linearVelocity.y);
    }

    void IntentarSaltar()
    {
        bool puedeCoyote = Time.time - ultimoTiempoEnSuelo <= tiempoCoyote;
        bool tieneBuffer = Time.time - ultimoTiempoSalto <= bufferSalto;

        if ((puedeCoyote && tieneBuffer) || (permitirSaltoAire && Input.GetButtonDown("Jump")))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * fuerzaSalto, ForceMode2D.Impulse);
            if (!string.IsNullOrEmpty(paramSaltarTrigger))
                anim.SetTrigger(paramSaltarTrigger);
            ultimoTiempoSalto = -999f;
        }
    }

    void ActualizarEstadoSuelo()
    {
        bool estabaEnSuelo = enSuelo;
        enSuelo = EstaTocandoSuelo();
        if (enSuelo)
            ultimoTiempoEnSuelo = Time.time;
    }

    void ActualizarAnimator()
    {
        if (anim == null) return;

        float velX = rb != null ? rb.linearVelocity.x : 0f;
        float velY = rb != null ? rb.linearVelocity.y : 0f;

        if (!string.IsNullOrEmpty(paramVelocidad))
            anim.SetFloat(paramVelocidad, Mathf.Abs(velX));
        if (!string.IsNullOrEmpty(paramVelocidadY))
            anim.SetFloat(paramVelocidadY, velY);
        if (!string.IsNullOrEmpty(paramEnSuelo))
            anim.SetBool(paramEnSuelo, enSuelo);
    }

    bool EstaTocandoSuelo()
    {
        Vector3 pos = puntoSuelo ? puntoSuelo.position : transform.position;
        bool overlap = Physics2D.OverlapCircle(pos, radioSuelo, sueloLayers);
        bool touching = col != null && sueloLayers.value != 0 && col.IsTouchingLayers(sueloLayers);
        return overlap || touching;
    }

    void GestionarGiro()
    {
        if (inputX > 0.01f && !mirandoDerecha)
            Girar();
        else if (inputX < -0.01f && mirandoDerecha)
            Girar();
    }

    void Girar()
    {
        mirandoDerecha = !mirandoDerecha;
        if (srMain != null)
        {
            srMain.flipX = !mirandoDerecha;
        }
        else
        {
            Vector3 s = transform.localScale;
            s.x *= -1f;
            transform.localScale = s;
        }
    }



    void OnCollisionEnter2D(Collision2D other)
    {
        if (EstaEnLayerMask(other.gameObject.layer, letalesLayers))
        {
            SendMessage("OnJugadorMuere", SendMessageOptions.DontRequireReceiver);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (EstaEnLayerMask(other.gameObject.layer, letalesLayers))
        {
            SendMessage("OnJugadorMuere", SendMessageOptions.DontRequireReceiver);
        }
    }

    bool EstaEnLayerMask(int layer, LayerMask mask)
    {
        return (mask.value & (1 << layer)) != 0;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 pos = puntoSuelo ? puntoSuelo.position : transform.position;
        Gizmos.DrawWireSphere(pos, radioSuelo);
    }

    void OnValidate()
    {
        rb = GetComponent<Rigidbody2D>();
        if (anim == null) anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();
        srMain = GetComponentInChildren<SpriteRenderer>();
        if (rb != null)
        {
            rb.mass = Mathf.Max(0.01f, peso);
            rb.gravityScale = gravedadBase * Mathf.Max(0.1f, peso);
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        if (puntoSuelo == null)
        {
            var child = transform.Find("PuntoSuelo");
            if (child == null)
            {
                var go = new GameObject("PuntoSuelo");
                go.transform.SetParent(transform);
                go.transform.localPosition = new Vector3(0f, -0.5f, 0f);
                puntoSuelo = go.transform;
            }
            else
            {
                puntoSuelo = child;
            }
        }

        if (sueloLayers.value == 0)
            Debug.LogWarning("MovimientoBasico: 'sueloLayers' está vacío. Asigna la capa de suelo (por ejemplo 'Default' si tu Tilemap usa esa capa).");
    }
}
