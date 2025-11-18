using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSystem : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene("Nivel1");
        Debug.Log("Entrando a nivel 1 ...");
    }
    public void Ajustes()
    {
        Debug.Log("Ajustes");
    }
    public void Salir()
    {
        Debug.Log("Saliendo del juego ...");
        //Application.Quit();
    }
    public void Reintentar()
    {
        SceneManager.LoadScene("Nivel1");
        Debug.Log("Regresando al menu principal ...");
    }

}
