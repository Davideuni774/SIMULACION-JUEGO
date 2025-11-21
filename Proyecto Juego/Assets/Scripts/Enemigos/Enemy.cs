using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    public enum EjeMovimiento { Horizontal, Vertical }
    public EjeMovimiento eje = EjeMovimiento.Horizontal;

    public float velocidad = 2f;
    public float distancia = 3f;  // cuánto se aleja del punto inicial

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
    }

    public void Morir()
    {
        // aquí puedes poner animación/muerte antes de Destroy
        Destroy(gameObject);
    }
}
