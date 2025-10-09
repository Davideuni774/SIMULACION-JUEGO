using UnityEngine;
using UnityEngine.SceneManagement;

public class Botonjugare : MonoBehaviour
{
    public void Jugar()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("Nivel1");
        Debug.Log("Entrando a nivel 1 ...");

    }
    public void Ajustes()
    {
        Debug.Log("Ajustes");
    }

}
