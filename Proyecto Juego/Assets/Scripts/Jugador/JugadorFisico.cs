using UnityEngine;
using UnityEngine.SceneManagement;  
using System.Collections.Generic;

// Movimiento físico 
// Colisión con suelo, paredes y cajas usando Physics2D.

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

    [Header("Salto / Rebote")]
    public float alturaMaxSalto = 2.5f;
    public float alturaMaxRebote = 1.8f;

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
    public string loseSceneName = "Lose";   // <-- nombre de la escena de derrota

    [Header("Debug")]
    public bool debugGround = true;

    // --- Estado interno ---
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

        // -------- 1) DETECCIÓN DE SUELO (ANTES DE MOVER) --------
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

        // -------- 2) MOVIMIENTO HORIZONTAL --------
        float objetivoVX = inputX * velocidadMax;
        float accel = sueloAhora ? aceleracionSuelo : aceleracionAire;
        velocidad.x = Mathf.MoveTowards(velocidad.x, objetivoVX, accel * dt);

        if (Mathf.Approximately(inputX, 0f))
        {
            float fric = sueloAhora ? friccionSuelo : friccionAire;
            velocidad.x = Mathf.MoveTowards(velocidad.x, 0f, fric * dt);
        }

        // -------- 3) SALTO --------
        if (jumpBufferCounter > 0f && coyoteCounter > 0f)
        {
            velocidad.y = velSalto;
            sueloAhora = false;
            coyoteCounter = 0f;
            jumpBufferCounter = 0f;
        }

        // -------- 4) ACELERACIÓN VERTICAL --------
        float aY = -gravedad;
        if (sueloAhora && velocidad.y <= 0f)
        {
            aY = 0f;
            velocidad.y = 0f;
        }

        vyAntesDeIntegrar = velocidad.y;
        velocidad.y += aY * dt;

        // -------- 5) INTEGRACIÓN CON COLISIÓN EN X/Y --------
        Vector2 pos = transform.position;

        // --- X: paredes / cajas ---
        float movX = velocidad.x * dt;
        if (Mathf.Abs(movX) > 0.0001f)
        {
            Vector2 dirX = new Vector2(Mathf.Sign(movX), 0f);
            float distX = Mathf.Abs(movX) + skin;

            // Origen un poco elevado para evitar enganchar la esquina del suelo
            Vector2 origenX = pos + Vector2.up * (radioColision * 0.5f);

            RaycastHit2D hitX = Physics2D.CircleCast(origenX, radioColision, dirX, distX, groundMask);
            if (hitX.collider != null)
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
            else
            {
                pos.x += movX;
            }
        }

        // --- Y: caída / salto (colisión vertical) ---
        float movY = velocidad.y * dt;

        if (Mathf.Abs(movY) > 0.0001f)
        {
            Vector2 dirY = new Vector2(0f, Mathf.Sign(movY));
            float distY = Mathf.Abs(movY) + skin;

            RaycastHit2D hitY = Physics2D.CircleCast(pos, radioColision, dirY, distY, groundMask);

            if (hitY.collider != null)
            {
                Vector2 n = hitY.normal;

                // Solo consideramos "suelo" o "techo" verdaderos:
                // - si caemos (dirY.y < 0) queremos normales con componente Y positiva (suelo)
                // - si subimos (dirY.y > 0) queremos normales con componente Y negativa (techo)
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
                    // Si la normal es casi horizontal (pared/esquina), dejamos que pase en Y
                    pos.y += movY;
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

        // -------- LÍMITE Y + ESCENA LOSE --------
        if (limitarY && pos.y < yMin)
        {
            // Cargar escena de derrota
            SceneManager.LoadScene(loseSceneName);
            return; // salimos de FixedUpdate
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

        // -------- 6) CHEQUEO DE ATERRIZAJE + REBOTE --------
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

        // -------- 7) ANIMACIÓN --------
        if (animator != null)
        {
            float speedXAbs = Mathf.Abs(velocidad.x);
            float movement = Mathf.Clamp01(velocidadMax > 0f ? speedXAbs / velocidadMax : 0f);

            TrySetBool("Grounded", enSuelo);
            TrySetFloat("Speed", speedXAbs);
            TrySetFloat("VelY", velocidad.y);
            // Compatibilidad con parámetro esperado por el Animator
            TrySetFloat("movement", movement);
        }

        if (inputX < -0.01f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
        else if (inputX > 0.01f)
            transform.localScale = new Vector3(1f, 1f, 1f);

        // -------- 8) CORREGIR PENETRACIONES (por si quedó dentro de algo) --------
        ResolverPenetraciones();
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

    // --- Utilidades Animator (evita warnings si el parámetro no existe) ---
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
