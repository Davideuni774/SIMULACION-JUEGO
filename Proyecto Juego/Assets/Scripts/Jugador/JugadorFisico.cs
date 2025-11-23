using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

// Movimiento físico 
// Colisión con suelo, paredes, cajas y enemigos usando Physics2D.

public class JugadorFisico : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animator;
    public Transform groundCheck;

    [Header("Física básica")]
    public float masa = 1.2f;
    public float gravedad = 9.81f;
    [Range(0f, 1f)] public float coefRestitucion = 0.6f;

    [Header("Movimiento horizontal")]
    public float velocidadMax = 10f;
    public float aceleracionSuelo = 60f;
    public float aceleracionAire = 20f; 
    public float friccionSuelo = 8f;
    public float friccionAire = 2f;

    [Header("Enemigos / Golpe")]
    public LayerMask enemyMask;          // capa de enemigos 
    public float knockbackHorizontal = 6f;
    public float knockbackVertical = 5f;
    public float tiempoInvulnerable = 0.5f;

    [Header("Salto / Rebote")]
    public float alturaMaxSalto = 2.5f;
    public float alturaMaxRebote = 1.8f;

    [Header("Caída con resistencia")]
    [Tooltip("Coeficiente de resistencia del aire (c). Solo se aplica al bajar.")]
    public float coefResistenciaAire = 5f;

    [Header("Ground check (plataformas, suelo, cajas)")]
    public float groundRadius = 0.15f;
    public LayerMask groundMask;    // aquí metes Ground, Cajas, LP

    [Header("Colisión (paredes / cajas)")]
    public float radioColision = 0.3f;   // radio del "cuerpo" del jugador
    public float skin = 0.03f;           // margen para no pegarse dentro del collider
    public float fuerzaEmpujeCajas = 20f;

    [Header("Tiempos de gracia")]
    public float jumpBufferTime = 0.2f;
    public float coyoteTime = 0.15f;

    [Header("Límites del mundo")]
    public bool limitarX = true;
    public float xMin = -20f, xMax = 20f;
    public bool limitarY = true;
    public float yMin = -3f;
    public string loseSceneName = "Lose";  

    [Header("Debug")]
    public bool debugGround = true;

    // Estado interno
    private Vector2 velocidad;
    private float velSalto;
    private float velReboteMax;
    private float offsetGroundY;

    private bool enSuelo;
    private bool enSueloPrevio;
    private float coyoteCounter;
    private float jumpBufferCounter;
    private float vyAntesDeIntegrar;

    private float inputX;

    private float invulnCounter;

    // Animator params cache
    private Dictionary<string, AnimatorControllerParameterType> animParams;

    void Start()
    {
        if (groundCheck != null)
            offsetGroundY = transform.position.y - groundCheck.position.y;

        float g = Mathf.Abs(gravedad);
        velSalto = Mathf.Sqrt(2f * g * Mathf.Max(alturaMaxSalto, 0.01f));
        velReboteMax = Mathf.Sqrt(2f * g * Mathf.Max(alturaMaxRebote, 0.01f));

        if (animator != null)
        {
            animParams = new Dictionary<string, AnimatorControllerParameterType>();
            foreach (var p in animator.parameters)
            {
                if (!animParams.ContainsKey(p.name))
                    animParams.Add(p.name, p.type);
            }
        }
    }

    void Update()
    {
        LeerInput();
    }

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // contador de invulnerabilidad
        if (invulnCounter > 0f)
            invulnCounter -= dt;

        //1) DETECCIÓN DE SUELO
        Collider2D colSuelo = null;
        bool sueloAhora = false;

        if (groundCheck != null)
        {
            colSuelo = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
            sueloAhora = colSuelo != null;

            if (debugGround)
            {
                Color c = sueloAhora ? Color.green : Color.red;
                Debug.DrawLine(
                    groundCheck.position,
                    groundCheck.position + Vector3.down * groundRadius,
                    c
                );
            }
        }

        if (sueloAhora)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= dt;

        // 2) MOVIMIENTO HORIZONTAL 
        // SOLO se controla en el suelo. En el aire no
        if (sueloAhora)
        {
            float objetivoVX = inputX * velocidadMax;
            float accel = aceleracionSuelo;
            velocidad.x = Mathf.MoveTowards(velocidad.x, objetivoVX, accel * dt);

            if (Mathf.Approximately(inputX, 0f))
            {
                float fric = friccionSuelo;
                velocidad.x = Mathf.MoveTowards(velocidad.x, 0f, fric * dt);
            }
        }
        else
        {
            // En el aire, se va frenando por fricción
            float fric = friccionAire;
            velocidad.x = Mathf.MoveTowards(velocidad.x, 0f, fric * dt);
        }

        // 3) SALTO
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            velocidad.y = velSalto;
            sueloAhora = false;
            coyoteCounter = 0f;
            jumpBufferCounter = 0f;
        }

        // 4) ACELERACIÓN VERTICAL
        float aY = 0f;

        if (sueloAhora && velocidad.y <= 0f)
        {
            // Apoyado en el suelo
            aY = 0f;
            velocidad.y = 0f;
        }
        else
        {
            // En el aire:
            // - Si va subiendo (velocidad.y > 0) -> solo gravedad (salto con caída libre)
            // - Si va bajando (velocidad.y <= 0) -> gravedad + resistencia del aire
            float c = Mathf.Max(coefResistenciaAire, 0f);
            float m = Mathf.Max(masa, 0.01f);

            if (velocidad.y > 0f)
            {
                // Fase de subida: caída libre
                aY = -gravedad;
            }
            else
            {
                // Fase de bajada: resistencia aire (termino - (c/m)*v )
                // dv/dt = -g - (c/m)*v
                aY = -gravedad - (c / m) * velocidad.y;
            }
        }

        vyAntesDeIntegrar = velocidad.y;
        velocidad.y += aY * dt;

        // 5) INTEGRACIÓN CON COLISIÓN EN X/Y 
        Vector2 pos = transform.position;

        // X: paredes / cajas / enemigos 
        float movX = velocidad.x * dt;
        if (Mathf.Abs(movX) > 0.0001f)
        {
            Vector2 dirX = new Vector2(Mathf.Sign(movX), 0f);
            float distX = Mathf.Abs(movX) + skin;

            // Origen un poco elevado para evitar enganchar la esquina del suelo
            Vector2 origenX = pos + Vector2.up * (radioColision * 0.5f);

            RaycastHit2D hitX = Physics2D.CircleCast(origenX, radioColision, dirX, distX, groundMask | enemyMask);
            if (hitX.collider != null)
            {
                if (hitX.collider.CompareTag("Enemigo"))
                {
                    // choque lateral con enemigo
                    RecibirDanio(hitX.normal);
                }
                else
                {
                    float nuevoX = hitX.point.x - dirX.x * (radioColision + skin);
                    pos.x = nuevoX;
                    velocidad.x = 0f;

                    // Empuje de cajas
                    Rigidbody2D rbCaja = hitX.rigidbody;
                    if (rbCaja != null && hitX.collider.CompareTag("Caja"))
                    {
                        rbCaja.AddForce(new Vector2(dirX.x * fuerzaEmpujeCajas, 0f),
                                        ForceMode2D.Force);
                    }
                }
            }
            else
            {
                pos.x += movX;
            }
        }

        // Y: caída / salto (colisión vertical + enemigos) 
        float movY = velocidad.y * dt;

        if (Mathf.Abs(movY) > 0.0001f)
        {
            Vector2 dirY = new Vector2(0f, Mathf.Sign(movY));
            float distY = Mathf.Abs(movY) + skin;

            RaycastHit2D hitY = Physics2D.CircleCast(pos, radioColision, dirY, distY, groundMask | enemyMask);

            if (hitY.collider != null)
            {
                // Es enemigo???
                if (hitY.collider.CompareTag("Enemigo"))
                {
                    Enemy enemy = hitY.collider.GetComponent<Enemy>();

                    bool cayendo = vyAntesDeIntegrar < 0f;
                    bool desdeArriba = dirY.y < 0f && hitY.normal.y > 0.5f;

                    if (cayendo && desdeArriba)
                    {
                        // Stomp: lo pisa desde arriba
                        float nuevoY = hitY.point.y - dirY.y * (radioColision + skin);
                        pos.y = nuevoY;

                        // Rebote hacia arriba (trayectoria curva por gravedad)
                        velocidad.y = velSalto * 0.6f;

                        if (enemy != null)
                            enemy.Morir();
                    }
                    else
                    {
                        // No fue stomp
                        RecibirDanio(hitY.normal);
                    }
                }
                else
                {
                    //Comportamiento normal con suelo/techo
                    Vector2 n = hitY.normal;

                    bool esSueloValido = dirY.y < 0f && n.y > 0.5f;
                    bool esTechoValido = dirY.y > 0f && n.y < -0.5f;
                    bool resolverY = esSueloValido || esTechoValido;

                    if (resolverY)
                    {
                        float nuevoY = hitY.point.y - dirY.y * (radioColision + skin);
                        pos.y = nuevoY;
                        velocidad.y = 0f;
                    }
                    else
                    {
                        // Normal casi horizontal (pared/esquina) -> dejamos pasar en Y
                        pos.y += movY;
                    }
                }
            }
            else
            {
                pos.y += movY;
            }
        }
        else
        {
            pos.y += movY;
        }

        //LÍMITE Y + ESCENA LOSE 
        if (limitarY && pos.y < yMin)
        {
            SceneManager.LoadScene(loseSceneName);
            return;
        }

        // Límite X global opcional
        if (limitarX)
        {
            if (pos.x < xMin)
            {
                pos.x = xMin;
                velocidad.x = 0f;
            }
            else if (pos.x > xMax)
            {
                pos.x = xMax;
                velocidad.x = 0f;
            }
        }

        transform.position = pos;

        ChequearSolapamientoEnemigos(pos);

        //6) CHEQUEO DE ATERRIZAJE + REBOTE 
        enSueloPrevio = enSuelo;

        if (groundCheck != null)
        {
            colSuelo = Physics2D.OverlapCircle(groundCheck.position, groundRadius, groundMask);
            enSuelo = colSuelo != null;
        }
        else
        {
            enSuelo = false;
        }

        if (!enSueloPrevio && enSuelo && vyAntesDeIntegrar < 0f)
        {
            float vyImpacto = -vyAntesDeIntegrar;
            float vyRebote = Mathf.Min(vyImpacto * coefRestitucion, velReboteMax);

            if (vyRebote < 0.5f)
            {
                velocidad.y = 0f;
            }
            else
            {
                velocidad.y = vyRebote;
                enSuelo = false;
            }
        }

        // 7) ANIMACIÓN
        if (animator != null)
        {
            float speedXAbs = Mathf.Abs(velocidad.x);
            float movement = Mathf.Clamp01(velocidadMax > 0f ? speedXAbs / velocidadMax : 0f);

            TrySetBool("Grounded", enSuelo);
            TrySetFloat("Speed", speedXAbs);
            TrySetFloat("VelY", velocidad.y);
            TrySetFloat("movement", movement);
        }

        // Flip del sprite (solo según input, aunque en el aire no se mueva)
        if (inputX < -0.01f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (inputX > 0.01f)
            transform.localScale = new Vector3(1f, 1f, 1f);

        ResolverPenetraciones();
    }
    void ChequearSolapamientoEnemigos(Vector2 pos)
    {
        if (invulnCounter > 0f) return;

        float radio = radioColision * 1.2f;

        Collider2D[] hits = Physics2D.OverlapCircleAll(pos, radio);
        foreach (var h in hits)
        {
            if (!h.CompareTag("Enemigo")) continue;

            Vector2 normal = (Vector2)transform.position - (Vector2)h.transform.position;
            if (normal.sqrMagnitude < 0.0001f)
                normal = Vector2.right;

            normal.Normalize();
            RecibirDanio(normal);
            break; 
        }
    }

    // Saca al jugador de cualquier collider en el que haya quedado metido
    void ResolverPenetraciones()
    {
        Vector2 pos = transform.position;

        Collider2D[] overlaps = Physics2D.OverlapCircleAll(
            pos,
            radioColision,
            groundMask
        );

        foreach (var col in overlaps)
        {
            Vector2 p = col.ClosestPoint(pos);
            Vector2 delta = pos - p;
            float dist = delta.magnitude;

            if (dist < 0.0001f)
            {
                delta = Vector2.up;
                dist = 0.0001f;
            }

            float penetracion = radioColision - dist;

            if (penetracion > 0f)
            {
                pos += delta.normalized * penetracion;
            }
        }

        transform.position = pos;
    }

    void LeerInput()
    {
        // Horizontal
        float axisX = 0f;
        try { axisX = Input.GetAxisRaw("Horizontal"); } catch { axisX = 0f; }

        if (Mathf.Approximately(axisX, 0f))
        {
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) axisX -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) axisX += 1f;
        }
        inputX = Mathf.Clamp(axisX, -1f, 1f);

        // Salto
        bool jumpDown =
            Input.GetButtonDown("Jump") ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.UpArrow);

        if (jumpDown)
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;
    }

    // Golpe 
    void RecibirDanio(Vector2 normalGolpe)
    {
        if (invulnCounter > 0f) return;

        // Rebote tipo "curva": velocidad hacia atrás y hacia arriba
        Vector2 knockDir = normalGolpe;
        if (knockDir.y <= 0f) knockDir.y = 0.5f; // impulso hacia arriba
        knockDir.Normalize();

        velocidad.x = knockDir.x * knockbackHorizontal;
        velocidad.y = Mathf.Abs(knockbackVertical);

        invulnCounter = tiempoInvulnerable;
    }
    void TrySetFloat(string name, float value)
    {
        if (animator == null) return;
        if (animParams != null && animParams.TryGetValue(name, out var t) && t == AnimatorControllerParameterType.Float)
        {
            animator.SetFloat(name, value);
        }
    }

    void TrySetBool(string name, bool value)
    {
        if (animator == null) return;
        if (animParams != null && animParams.TryGetValue(name, out var t) && t == AnimatorControllerParameterType.Bool)
        {
            animator.SetBool(name, value);
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

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radioColision);
    }
#endif
}
