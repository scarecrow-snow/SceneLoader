using System;
using System.Collections.Generic;
using System.Linq;

using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

using UnityEngine;

using UnityEngine.SceneManagement;

namespace Systems.SceneManagement
{
    public class SceneGroupManager
    {
        public event Action<string> OnSceneLoaded = delegate { };
        public event Action<string> OnSceneUnLoaded = delegate { };
        public event Action OnSceneGroupLoaded = delegate { };

        SceneGroup ActiveSceneGroup;

        public async UniTask LoadScenes(SceneGroup group, IProgress<float> progress, bool reloadDupScenes = false)
        {
            ActiveSceneGroup = group;
            var loadedScenes = new List<string>();

            await UnloadScenes();

            int sceneCount = SceneManager.sceneCount;

            for (var i = 0; i < sceneCount; i++)
            {
                loadedScenes.Add(SceneManager.GetSceneAt(i).name);
            }

            var totalScenesToLoad = ActiveSceneGroup.Scenes.Count;

            var operationGroup = new AsyncOperationGroup(totalScenesToLoad);


            for(var i = 0; i < totalScenesToLoad; i++)
            {
                var sceneData = group.Scenes[i];
                if(!reloadDupScenes && loadedScenes.Contains(sceneData.Name)) continue;

                var operation = SceneManager.LoadSceneAsync(sceneData.Reference.Path, LoadSceneMode.Additive);

                await UniTask.Delay(TimeSpan.FromSeconds(2.5f));   //TODO debug remove me

                operationGroup.Operations.Add(operation);

                OnSceneLoaded.Invoke(sceneData.Name);
            }

            // Wait until all AsyncOperations in the group are done
            while(!operationGroup.IsDone)
            {
                progress?.Report(operationGroup.Progress);
                await UniTask.Delay(100);
            }

            Scene activeScene = SceneManager.GetSceneByName(ActiveSceneGroup.FindSceneNameByType(SceneType.ActiveScene));
            
            if(activeScene.IsValid())
            {
                SceneManager.SetActiveScene(activeScene);
            }

            OnSceneGroupLoaded.Invoke();
        }

        public async UniTask UnloadScenes()
        {
            var scenes = new List<string>();
            var activeScene = SceneManager.GetActiveScene().name;

            int sceneCount = SceneManager.sceneCount;

            for(var i = sceneCount - 1; i > 0; i--)
            {
                var sceneAt = SceneManager.GetSceneAt(i);
                if(!sceneAt.isLoaded) continue;

                var sceneName = sceneAt.name;
                if(sceneName.Equals(activeScene) || sceneName == "Boostrapper") continue;

                scenes.Add(sceneName);
            }

            // Crate an AsyncOperationGroup
            var operationGroup = new AsyncOperationGroup(scenes.Count);

            foreach(var scene in scenes)
            {
                var operation = SceneManager.UnloadSceneAsync(scene);
                if(operation == null) continue;

                operationGroup.Operations.Add(operation);

                OnSceneUnLoaded.Invoke(scene);
            }

            // Wait until all AsyncOperations in the group are done
            while(!operationGroup.IsDone)
            {
                await UniTask.Delay(100);
            }

            await Resources.UnloadUnusedAssets().ToUniTask();
        }
    }

    public readonly struct AsyncOperationGroup
    {
        public readonly List<AsyncOperation> Operations;

        public float Progress => Operations.Count == 0 ? 0 : Operations.Average(o => o.progress);
        public bool IsDone => Operations.All(o => o.isDone);

        public AsyncOperationGroup(int initialCapacity)
        {
            Operations = new List<AsyncOperation>(initialCapacity);
        }
    }

}
