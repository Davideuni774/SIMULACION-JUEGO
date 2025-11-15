using UnityEngine;

public class Caamara : MonoBehaviour
{
    public Transform jugador;
    public float velocidadcamara = 0.0025f;
    public Vector3 desplazamiento;

    private void LateUpdate()
    {
        if (jugador == null) return;
        Vector3 posicionDeseada = jugador.position + desplazamiento;
        Vector3 posicionSuavizada = Vector3.Lerp(transform.position, posicionDeseada, velocidadcamara);
        transform.position = posicionSuavizada;
    }

}
