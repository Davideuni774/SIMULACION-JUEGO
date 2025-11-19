using UnityEngine;
using UnityEngine.Serialization;

public class Resorte : MonoBehaviour
{
	[FormerlySerializedAs("stiffness")]
	[SerializeField] private float rigidez = 50f;
	[FormerlySerializedAs("damping")]
	[SerializeField] private float amortiguamiento = 6f;
	[FormerlySerializedAs("mass")]
	[SerializeField] private float masa = 1f;

	[FormerlySerializedAs("useInitialYAsRest")]
	[SerializeField] private bool usarYInicialComoReposo = true;
	[FormerlySerializedAs("restY")]
	[SerializeField] private float reposoY = -3.3f;

	[FormerlySerializedAs("playerTag")]
	[SerializeField] private string etiquetaJugador = "Player";
	[FormerlySerializedAs("applyReactionToPlayer")]
	[SerializeField] private bool aplicarReaccionAlJugador = true;

	[FormerlySerializedAs("clampDisplacement")]
	[SerializeField] private bool limitarDesplazamiento = false;
	[FormerlySerializedAs("maxCompression")]
	[SerializeField] private float compresionMaxima = 2f;
	[FormerlySerializedAs("maxExtension")]
	[SerializeField] private float extensionMaxima = 1f;

	private float desplazamiento;
	private float velocidad;
	private Rigidbody2D rbJugador;

	private void Start()
	{
		if (usarYInicialComoReposo)
		{
			reposoY = transform.position.y;
		}
		desplazamiento = transform.position.y - reposoY;
		velocidad = 0f;
	}

	private void FixedUpdate()
	{
		float dt = Time.fixedDeltaTime;
		float g = Mathf.Abs(Physics2D.gravity.y);

		float pesoJugador = 0f;
		if (rbJugador != null)
		{
			pesoJugador = rbJugador.mass * g * rbJugador.gravityScale;
		}

		IntegrarRK4(dt, g, pesoJugador);

		if (limitarDesplazamiento)
		{
			float minX = -Mathf.Abs(compresionMaxima);
			float maxX = Mathf.Abs(extensionMaxima);
			desplazamiento = Mathf.Clamp(desplazamiento, minX, maxX);
			if (velocidad > 0f && desplazamiento >= maxX) velocidad = 0f;
			if (velocidad < 0f && desplazamiento <= minX) velocidad = 0f;
		}

		var pos = transform.position;
		transform.position = new Vector3(pos.x, reposoY + desplazamiento, pos.z);

		if (aplicarReaccionAlJugador && rbJugador != null)
		{
			float fuerzaResorteArriba = (-rigidez * desplazamiento - amortiguamiento * velocidad);
			if (fuerzaResorteArriba > 0f)
			{
				rbJugador.AddForce(Vector2.up * fuerzaResorteArriba, ForceMode2D.Force);
			}
		}
	}

	private void IntegrarRK4(float dt, float g, float pesoJugador)
	{
		if (masa <= 0f) return;

		float ax1 = Aceleracion(desplazamiento, velocidad, g, pesoJugador);
		float k1x = velocidad;
		float k1v = ax1;

		float x2 = desplazamiento + 0.5f * dt * k1x;
		float v2 = velocidad + 0.5f * dt * k1v;
		float k2x = v2;
		float k2v = Aceleracion(x2, v2, g, pesoJugador);

		float x3 = desplazamiento + 0.5f * dt * k2x;
		float v3 = velocidad + 0.5f * dt * k2v;
		float k3x = v3;
		float k3v = Aceleracion(x3, v3, g, pesoJugador);

		float x4 = desplazamiento + dt * k3x;
		float v4 = velocidad + dt * k3v;
		float k4x = v4;
		float k4v = Aceleracion(x4, v4, g, pesoJugador);

		desplazamiento += (dt / 6f) * (k1x + 2f * k2x + 2f * k3x + k4x);
		velocidad += (dt / 6f) * (k1v + 2f * k2v + 2f * k3v + k4v);
	}

	private float Aceleracion(float xVal, float vVal, float g, float pesoJugador)
	{
		float fuerzaResorte = -rigidez * xVal;
		float fuerzaAmortiguamiento = -amortiguamiento * vVal;
		float fuerzaGravedad = -masa * g;
		float fuerzaExterna = -pesoJugador;
		return (fuerzaResorte + fuerzaAmortiguamiento + fuerzaGravedad + fuerzaExterna) / masa;
	}

	private void OnTriggerEnter2D(Collider2D other)
	{
		if (other.CompareTag(etiquetaJugador))
		{
			other.TryGetComponent<Rigidbody2D>(out rbJugador);
		}
	}

	private void OnTriggerExit2D(Collider2D other)
	{
		if (other.CompareTag(etiquetaJugador))
		{
			if (other.attachedRigidbody == rbJugador) rbJugador = null;
		}
	}

	private void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.collider.CompareTag(etiquetaJugador))
		{
			collision.collider.TryGetComponent<Rigidbody2D>(out rbJugador);
		}
	}

	private void OnCollisionExit2D(Collision2D collision)
	{
		if (collision.collider.CompareTag(etiquetaJugador))
		{
			if (collision.rigidbody == rbJugador) rbJugador = null;
		}
	}
}

