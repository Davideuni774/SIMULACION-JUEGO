using UnityEngine;
using UnityEngine.Serialization;
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody2D))]
public class MovimientoBasico : MonoBehaviour
{
	[FormerlySerializedAs("moveSpeed")]
	[SerializeField] private float velocidadMovimiento = 5f;
	[FormerlySerializedAs("speedParameter")]
	[SerializeField] private string parametroVelocidad = "speed";
	[FormerlySerializedAs("movementParameter")]
	[SerializeField] private string parametroMovimiento = "movement";
	[FormerlySerializedAs("jumpForce")]
	[SerializeField] private float fuerzaSalto = 7f;
	[FormerlySerializedAs("groundCheck")]
	[SerializeField] private Transform verificadorSuelo;
	[FormerlySerializedAs("groundCheckRadius")]
	[SerializeField] private float radioVerificadorSuelo = 0.1f;
	[FormerlySerializedAs("groundLayer")]
	[SerializeField] private LayerMask capaSuelo;
	[FormerlySerializedAs("lockYOnGround")]
	[SerializeField] private bool bloquearYEnSuelo = true;
	[FormerlySerializedAs("jumpKey")]
	[SerializeField] private KeyCode teclaSalto = KeyCode.Space;
	[FormerlySerializedAs("playerMass")]
	[SerializeField] private float masaJugador = 1f;
	[FormerlySerializedAs("playerGravityScale")]
	[SerializeField] private float escalaGravedadJugador = 1f;

	[SerializeField] private SpriteRenderer renderizadorSprite;
	[SerializeField] private bool voltearSprite = true;

	private const float LockedY = -3.3f;

	[FormerlySerializedAs("animator")]
	[SerializeField] private Animator animador;
	private Rigidbody2D rb;
	private bool saltoEncolado;
	private int resortesEnContacto;
	private bool mirandoDerecha = true;
	private float escalaInicialX = 1f;

	private bool tieneFloatVelocidad;
	private bool tieneBoolMovimiento;
	private bool tieneFloatMovimiento;

	private void Awake()
	{
		if (animador == null)
		{
			animador = GetComponent<Animator>();
		}
		rb = GetComponent<Rigidbody2D>();

		if (renderizadorSprite == null)
		{
			renderizadorSprite = GetComponent<SpriteRenderer>();
			if (renderizadorSprite == null)
			{
				renderizadorSprite = GetComponentInChildren<SpriteRenderer>();
			}
		}
		escalaInicialX = transform.localScale.x;

		// Cachear qué parámetros existen y sus tipos para evitar warnings
		if (animador != null)
		{
			foreach (var p in animador.parameters)
			{
				if (!string.IsNullOrEmpty(parametroVelocidad) && p.name == parametroVelocidad && p.type == AnimatorControllerParameterType.Float)
				{
					tieneFloatVelocidad = true;
				}
				if (!string.IsNullOrEmpty(parametroMovimiento) && p.name == parametroMovimiento)
				{
					tieneBoolMovimiento = p.type == AnimatorControllerParameterType.Bool;
					tieneFloatMovimiento = p.type == AnimatorControllerParameterType.Float;
				}
			}
		}
	}

	private void Start()
	{
		var p = rb.position;
		p.y = LockedY;
		rb.position = p;
		rb.constraints = RigidbodyConstraints2D.FreezeRotation;

		// Asegurar masa y gravedad para interacción con el resorte
		rb.mass = Mathf.Max(0.01f, masaJugador);
		rb.gravityScale = escalaGravedadJugador;
	}

	private void Update()
	{
		if (Input.GetButtonDown("Jump") || Input.GetKeyDown(teclaSalto))
		{
			saltoEncolado = true;
		}
	}

	private void FixedUpdate()
	{
		float inputX = Input.GetAxisRaw("Horizontal");
		float vy = rb.linearVelocity.y;

		// Saltar solo si estamos en el suelo
		if (saltoEncolado && EstaEnSuelo())
		{
			vy = fuerzaSalto;
			saltoEncolado = false;
		}

		rb.linearVelocity = new Vector2(inputX * velocidadMovimiento, vy);

		// Voltear sprite según dirección horizontal
		if (voltearSprite && Mathf.Abs(inputX) > 0.01f)
		{
			bool haciaDerecha = inputX > 0f;
			if (haciaDerecha != mirandoDerecha)
			{
				mirandoDerecha = haciaDerecha;
				AplicarFlip();
			}
		}

		if (animador != null)
		{
			float velocidadAbs = Mathf.Abs(rb.linearVelocity.x);
			if (tieneFloatVelocidad)
			{
				animador.SetFloat(parametroVelocidad, velocidadAbs);
			}
			if (tieneBoolMovimiento)
			{
				animador.SetBool(parametroMovimiento, velocidadAbs > 0.01f);
			}
			else if (tieneFloatMovimiento)
			{
				animador.SetFloat(parametroMovimiento, velocidadAbs);
			}
		}
	}

	private void LateUpdate()
	{
		// Solo fijar Y al suelo si no usamos groundCheck (modo simple)
		if (bloquearYEnSuelo && verificadorSuelo == null && resortesEnContacto == 0)
		{
			var pos = transform.position;
			if (pos.y < LockedY)
			{
				transform.position = new Vector3(pos.x, LockedY, pos.z);
			}
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.collider.GetComponent<Resorte>() != null)
		{
			resortesEnContacto++;
		}
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.collider.GetComponent<Resorte>() != null)
		{
			resortesEnContacto = Mathf.Max(0, resortesEnContacto - 1);
		}
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.GetComponent<Resorte>() != null)
		{
			resortesEnContacto++;
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (other.GetComponent<Resorte>() != null)
		{
			resortesEnContacto = Mathf.Max(0, resortesEnContacto - 1);
		}
	}

	private bool EstaEnSuelo()
	{
		// Considerar contacto con resorte como suelo para permitir salto
		if (resortesEnContacto > 0) return true;
		if (verificadorSuelo != null)
		{
			return Physics2D.OverlapCircle(verificadorSuelo.position, radioVerificadorSuelo, capaSuelo) != null;
		}
		// Sin groundCheck: considerar suelo en la línea LockedY
		return transform.position.y <= LockedY + 0.01f;
	}

	private void AplicarFlip()
	{
		if (renderizadorSprite != null)
		{
			renderizadorSprite.flipX = !mirandoDerecha;
		}
		else
		{
			var s = transform.localScale;
			s.x = Mathf.Abs(escalaInicialX) * (mirandoDerecha ? 1f : -1f);
			transform.localScale = s;
		}
	}
}

