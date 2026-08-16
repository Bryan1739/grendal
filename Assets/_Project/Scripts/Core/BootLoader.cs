using UnityEngine;
using UnityEngine.SceneManagement;

namespace TapLegends.Core
{
    /// <summary>
    /// Escena de arranque. De momento solo carga Game; a partir de la Etapa 4 aqui
    /// se cargara el guardado antes de entrar a la partida.
    /// </summary>
    public class BootLoader : MonoBehaviour
    {
        private void Start()
        {
            SceneManager.LoadScene("Game");
        }
    }
}
