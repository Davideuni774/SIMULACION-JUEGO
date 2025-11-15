using UnityEngine;

// Sistema masa-resorte con integración RK4 que interactúa con el jugador.
// Adjunta este script a un GameObject con un Collider2D (isTrigger recomendado).
[RequireComponent(typeof(Collider2D))]
public class Resorte : MonoBehaviour
{
	// Parámetros físicos
	public float m = 1.5f;          // masa
	public float c = 1.5f;          // amortiguamiento
	public float k = 6.0f;          // constante del resorte
	public float x0 = 2.0f;         // posición inicial (desplazamiento desde equilibrio)
	public float v0 = 0.0f;         // velocidad inicial
	public float h = 0.02f;         // paso de integración (segundos)
	public Vector3 eje = Vector3.up;// eje del movimiento (vertical por defecto)

	// Interacción con jugador
	public string playerTag = "Player"; // Tag del jugador
	public float compresionJugador = 1.0f; // Cuánto comprime el resorte al tocarlo
	public float impulsoAlJugador = 8f;    // Impulso vertical aplicado al jugador al liberar
	public float fuerzaExtraDecay = 4f;    // Velocidad con que desaparece la fuerza externa

	// Estado interno
	private float x; // desplazamiento actual
	private float v; // velocidad actual
	private float t; // tiempo simulado
	private Vector3 puntoEquilibrio; // posición base
	private float fuerzaExterna; // fuerza acumulada por compresión del jugador

	private Collider2D col;

	void Start()
	{
		x = x0;
		v = v0;
		t = 0f;
		puntoEquilibrio = transform.position;
		col = GetComponent<Collider2D>();
		col.isTrigger = true; // recomendación para interacción limpia
	}

	void FixedUpdate()
	{
		// RK4 para el sistema masa-resorte amortiguado con fuerza externa F(t)
		Vector2 k1 = Derivadas(t, x, v);
		Vector2 k2 = Derivadas(t + h / 2f, x + h * k1.x / 2f, v + h * k1.y / 2f);
		Vector2 k3 = Derivadas(t + h / 2f, x + h * k2.x / 2f, v + h * k2.y / 2f);
		Vector2 k4 = Derivadas(t + h, x + h * k3.x, v + h * k3.y);

		x += (h / 6f) * (k1.x + 2f * k2.x + 2f * k3.x + k4.x);
		v += (h / 6f) * (k1.y + 2f * k2.y + 2f * k3.y + k4.y);
		t += h;

		// Decaimiento suave de fuerza externa añadida por el jugador
		if (fuerzaExterna > 0f)
		{
			fuerzaExterna = Mathf.Max(0f, fuerzaExterna - fuerzaExtraDecay * h);
		}

		// Actualizar posición en el eje
		transform.position = puntoEquilibrio + eje.normalized * x;
	}

	// Derivadas del sistema: dx/dt = v, dv/dt = (F(t) - c v - k x) / m
	private Vector2 Derivadas(float tiempo, float xLocal, float vLocal)
	{
		float F = FuerzaExterna(tiempo);
		float dxdt = vLocal;
		float dvdt = (F - c * vLocal - k * xLocal) / m;
		return new Vector2(dxdt, dvdt);
	}

	// Fuerza externa: proviene de compresión del jugador (acumulada)
	private float FuerzaExterna(float tiempo)
	{
		return fuerzaExterna; // puedes extender con funciones periódicas si lo deseas
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag(playerTag))
		{
			// Comprime el resorte (reduce x) y añade fuerza externa para empuje
			x -= compresionJugador;
			fuerzaExterna += k * compresionJugador; // proporcional a la compresión

			// Aplicar impulso al jugador (si tiene Rigidbody2D)
			var rbPlayer = other.attachedRigidbody;
			if (rbPlayer != null)
			{
				// Limpia velocidad vertical negativa antes del impulso
				var vP = rbPlayer.linearVelocity;
				if (vP.y < 0f) vP.y = 0f;
				rbPlayer.linearVelocity = vP;
				rbPlayer.AddForce(Vector2.up * impulsoAlJugador, ForceMode2D.Impulse);
			}
		}
	}

	void OnDrawGizmos()
	{
		Gizmos.color = Color.yellow;
		Vector3 eq = Application.isPlaying ? puntoEquilibrio : transform.position;
		Gizmos.DrawWireSphere(eq, 0.05f);
		Gizmos.color = Color.cyan;
		Gizmos.DrawLine(eq, eq + eje.normalized * x);
	}
}
