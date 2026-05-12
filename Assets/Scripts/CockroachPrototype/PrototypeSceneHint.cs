using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace IfYouWereCockroach.Prototype
{
    [ExecuteAlways]
    public sealed class PrototypeSceneHint : MonoBehaviour
    {
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Handles.Label(transform.position + Vector3.up * 1.4f, "按 Play 自动生成蟑螂求生原型");
        }
#endif
    }
}
