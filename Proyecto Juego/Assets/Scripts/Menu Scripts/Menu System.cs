using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuSystem : MonoBehaviour
{
    public void Jugar()
    {
        SceneManager.LoadScene("Niveles");
        Debug.Log("Entrando a niveles ...");
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
    public void jugarnivel1()
    {
        SceneManager.LoadScene("Nivel1");
        Debug.Log("Entrando al nivel 1 ...");
    }
    public void jugarnivel2()
    {
        SceneManager.LoadScene("Nivel2");
        Debug.Log("Entrando al nivel 2 ...");
    }

}
