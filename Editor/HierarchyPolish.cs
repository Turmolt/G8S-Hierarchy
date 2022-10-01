using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace G8S
{
    [InitializeOnLoad]
    public static class HierarchyPolish
    {
        static HierarchyPolish() 
        {
            Initialize();
        }

        struct InstanceData
        {
            public bool hasChildren;
            
            public int nestingLevel;
        }
        
        private static bool _initialized = false;
        private static int _firstInstanceID;
        private static Dictionary<int, InstanceData> _sceneIDs = new Dictionary<int, InstanceData>();
        private static Color _overlayColor = new Color(1f, 1f, 1f, 0.02f);
        
        private static void Initialize()
        {
            if (_initialized)
            {
                EditorApplication.hierarchyWindowItemOnGUI -= Draw;
                EditorApplication.hierarchyChanged -= RetrieveDataFromScene;
            }

            _initialized = true;
            
            EditorApplication.hierarchyWindowItemOnGUI += Draw;
            EditorApplication.hierarchyChanged += RetrieveDataFromScene;
        }

        private static void RetrieveDataFromScene()
        {
            if (Application.isPlaying)
                return;
            _sceneIDs.Clear();
            
            #if UNITY_2021_1_OR_NEWER
            var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            #else
            var prefabStage = UnityEditor.Experimental.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            #endif
            
            if (prefabStage != null)
            {
                _firstInstanceID = prefabStage.prefabContentsRoot.GetInstanceID();
                AnalyzeObjectWithChildren(prefabStage.prefabContentsRoot);
                return;
            }

            GameObject[] roots;
            Scene tempScene;
            _firstInstanceID = -1;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                tempScene = SceneManager.GetSceneAt(i);
                if (!tempScene.isLoaded) continue;
                
                roots = tempScene.GetRootGameObjects();

                for (var j = 0; j<roots.Length;j++)
                {
                    AnalyzeObjectWithChildren(roots[j], nestingGroup: j);
                }

                if (_firstInstanceID == -1 && roots.Length > 0) _firstInstanceID = roots[0].GetInstanceID();
            }
        }

        static void AnalyzeObjectWithChildren(GameObject go,  int nestingGroup = 0, int depth = 0, bool isLastChild = false)
        {
            var childCount = go.transform.childCount;

            var data = new InstanceData
            {
                hasChildren = childCount > 0,
                nestingLevel =  depth + 1,
            };

            _sceneIDs.Add(go.GetInstanceID(), data);
            depth += 1;
            
            for (var i = 0; i < childCount; i++)
            {
                AnalyzeObjectWithChildren(go.transform.GetChild(i).gameObject,  nestingGroup, depth, i == childCount - 1);
            }
        }

        private static bool drawOverlay;
        
        private static void Draw(int id, Rect rect)
        {
            if(!_sceneIDs.TryGetValue(id, out var currentItem)) return;
            
            #region Overlay
            
            if (id == _firstInstanceID) drawOverlay = true;
            
            if (drawOverlay)
            {
                EditorGUI.DrawRect(rect, _overlayColor);
            }
            
            drawOverlay = !drawOverlay;
            
            #endregion
            
            #region Tree

            if (rect.x < 60) return;

            if (currentItem.nestingLevel == 0 && !currentItem.hasChildren)
            {
                HierarchyRenderer.DrawVerticalLine(rect, 0);
            }
            else
            {
                for (int i = 0; i < currentItem.nestingLevel; i++)
                {
                    HierarchyRenderer.DrawVerticalLine(rect, i);
                }

                HierarchyRenderer.DrawHorizontalLine(rect, currentItem.nestingLevel, currentItem.hasChildren);
            }
            
            #endregion
        }
    }

    public static class HierarchyRenderer
    {
        private const float barWidth = 2f;

        private static readonly Color barColor = new Color(0.5f,0.5f,0.5f, 1f);

        public static void DrawVerticalLine(Rect originalRect, int nestLevel)
        {
            DrawHalfVerticalLine(originalRect, true, nestLevel);
            DrawHalfVerticalLine(originalRect, false, nestLevel);
        }

        public static void DrawHalfVerticalLine(Rect originalRect, bool startOnTop, int nestLevel)
        {
            EditorGUI.DrawRect(
                new Rect( GetStartX(originalRect, nestLevel), 
                    startOnTop? originalRect.y : (originalRect.y + originalRect.height/2f),
                    barWidth, originalRect.height/2f), barColor);
        }
        
        public static void DrawHorizontalLine(Rect originalRect, int nestLevel, bool hasChildren)
        {
            EditorGUI.DrawRect(new Rect(GetStartX(originalRect, nestLevel - 1), 
                originalRect.y + originalRect.height/2f - .5f,
                originalRect.height + (hasChildren ? -5 : 2),
                barWidth), barColor);
        }

        private static float GetStartX(Rect originalRect, int nestLevel)
        {
            return 37 + (originalRect.height - 2) * nestLevel;
        }
    }
}