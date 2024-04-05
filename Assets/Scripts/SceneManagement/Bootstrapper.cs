using UnityEngine;

using UnityEngine.SceneManagement;
using Cysharp.Threading.Tasks;

public class Bootstrapper : PersistentSingleton<Bootstrapper>
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static async UniTaskVoid Init() {
        Debug.Log("Bootstrapper...");
        await SceneManager.LoadSceneAsync("Bootstrapper", LoadSceneMode.Single).ToUniTask();
    }
}
