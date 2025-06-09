using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class EndGamePanelController : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text resultText;

    [SerializeField] private Button botonMenu;
    [SerializeField] private Button botonSalir;
    [SerializeField] private Button botonReiniciar;

    private void Start()
    {
       
        if (botonMenu != null)
        {
            botonMenu.onClick.AddListener(IrAlMenu);
        }
        else
        {
            Debug.LogWarning("No se ha asignado el bot�n en el inspector.");
        }

        if (botonSalir != null)
        {
            botonSalir.onClick.AddListener(CerrarJuego);
        }
        else
        {
            Debug.LogWarning("No se ha asignado el bot�n de salir en el inspector.");
        }
        if (botonReiniciar != null)
        {
            botonReiniciar.onClick.AddListener(VolverAJugarServerRpc);

        }
        else
        {
            Debug.LogWarning("No se ha asignado el bot�n en el inspector.");
        }

    }
    public void ShowEndGamePanel(string resultMessage)
    {
        panel.SetActive(true);
        resultText.text = resultMessage;
    }
    public void IrAlMenu()
    {
        // Cerrar conexi�n y cargar escena localmente solo para quien pulse el bot�n
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene("MenuScene");
    }
    public void CerrarJuego()
    {
        Debug.Log("Cerrando juego...");
        Application.Quit();

        // En el editor de Unity, esto no funciona, as� que para testear puedes hacer:
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    [ServerRpc(RequireOwnership = false)]
    public void VolverAJugarServerRpc()
    {
        NetworkManager.Singleton.SceneManager.LoadScene("GameScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
        CoinManager.instance.ResetCoinServerRpc();
    }

}
