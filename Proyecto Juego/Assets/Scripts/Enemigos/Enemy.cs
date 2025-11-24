using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    public enum EjeMovimiento { Horizontal, Vertical }
    public EjeMovimiento eje = EjeMovimiento.Horizontal;

    public float velocidad = 2f;
    public float distancia = 3f;  // cu�nto se aleja del punto inicial

    // Asignar desde el inspector. Debe tener un parámetro float llamado "enemov"
    public Animator animator;

    private Vector3 startPos;
    private int dir = 1;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        Vector3 pos = transform.position;

        if (eje == EjeMovimiento.Horizontal)
        {
            pos.x += velocidad * dir * Time.deltaTime;
            if (Mathf.Abs(pos.x - startPos.x) >= distancia)
                dir *= -1;
        }
        else
        {
            pos.y += velocidad * dir * Time.deltaTime;
            if (Mathf.Abs(pos.y - startPos.y) >= distancia)
                dir *= -1;
        }

        transform.position = pos;

        // Voltea el sprite según la dirección del movimiento
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * dir;
        transform.localScale = scale;

        // Actualiza el parámetro de movimiento para animaciones (dirección * velocidad)
        if (animator != null)
        {
            animator.SetFloat("enemov", velocidad * dir);
        }
    }

    public void Morir()
    {
        // aqu� puedes poner animaci�n/muerte antes de Destroy
        Destroy(gameObject);
    }
}
