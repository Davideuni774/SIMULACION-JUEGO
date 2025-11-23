using UnityEngine;

public class Resorte : MonoBehaviour
{
	[SerializeField] float mass = 1f;
	[SerializeField] float springConstant = 20f;
	[SerializeField] float damping = 1f;
	[SerializeField] float initialAmplitude = 1f;
	[SerializeField] float initialVelocity = 0f;
	[SerializeField] Vector3 axis = Vector3.up;
	[SerializeField] float positionThreshold = 0.01f;
	[SerializeField] float velocityThreshold = 0.01f;
	[SerializeField] float settleDuration = 1f;

	Vector3 basePosition;
	float x;
	float v;
	float settleTimer;

	void Awake()
	{
		basePosition = transform.localPosition;
		ResetSpring();
	}

	void FixedUpdate()
	{
		float dt = Time.fixedDeltaTime;
		IntegrateRK4(dt);
		transform.localPosition = basePosition + axis.normalized * x;
		if (Mathf.Abs(x) < positionThreshold && Mathf.Abs(v) < velocityThreshold)
		{
			settleTimer += dt;
			if (settleTimer >= settleDuration) ResetSpring();
		}
		else
		{
			settleTimer = 0f;
		}
	}

	void IntegrateRK4(float dt)
	{
		float a1 = Acceleration(x, v);
		float k1x = v * dt;
		float k1v = a1 * dt;

		float a2 = Acceleration(x + k1x * 0.5f, v + k1v * 0.5f);
		float k2x = (v + k1v * 0.5f) * dt;
		float k2v = a2 * dt;

		float a3 = Acceleration(x + k2x * 0.5f, v + k2v * 0.5f);
		float k3x = (v + k2v * 0.5f) * dt;
		float k3v = a3 * dt;

		float a4 = Acceleration(x + k3x, v + k3v);
		float k4x = (v + k3v) * dt;
		float k4v = a4 * dt;

		x += (k1x + 2f * k2x + 2f * k3x + k4x) / 6f;
		v += (k1v + 2f * k2v + 2f * k3v + k4v) / 6f;
	}

	float Acceleration(float px, float pv)
	{
		return -(springConstant / mass) * px - (damping / mass) * pv;
	}

	public void ResetSpring()
	{
		x = initialAmplitude;
		v = initialVelocity;
		settleTimer = 0f;
	}

	public float Displacement => x;
	public float Velocity => v;
	public bool IsSettled => settleTimer >= settleDuration;
}
