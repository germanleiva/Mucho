using UnityEngine;

[DisallowMultipleComponent]
public class StableObjectId : MonoBehaviour
{
    [SerializeField] private string id;

    public string Id => id;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = System.Guid.NewGuid().ToString();
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}