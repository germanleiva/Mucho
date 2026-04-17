using System.Collections.Generic;
using UnityEngine;

public interface ISceneReferenceResolver
{
    GameObject ResolveGameObject(string id);
    Asset ResolveAsset(string id);

    string GetId(GameObject go);
    string GetId(Asset asset);
}

public class SceneReferenceResolver : ISceneReferenceResolver
{
    private readonly Dictionary<string, GameObject> gameObjectsById = new();
    private readonly Dictionary<string, Asset> assetsById = new();

    public SceneReferenceResolver()
    {
        var allIds = Object.FindObjectsOfType<StableObjectId>(true);
        foreach (var stable in allIds)
        {
            if (string.IsNullOrWhiteSpace(stable.Id))
                continue;

            gameObjectsById[stable.Id] = stable.gameObject;

            Asset asset = stable.GetComponent<Asset>();
            if (asset != null)
            {
                assetsById[stable.Id] = asset;
            }
        }
    }

    public GameObject ResolveGameObject(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return gameObjectsById.TryGetValue(id, out var go) ? go : null;
    }

    public Asset ResolveAsset(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return assetsById.TryGetValue(id, out var asset) ? asset : null;
    }

    public string GetId(GameObject go)
    {
        if (go == null)
            return null;

        var stable = go.GetComponent<StableObjectId>();
        return stable != null ? stable.Id : null;
    }

    public string GetId(Asset asset)
    {
        if (asset == null)
            return null;

        var stable = asset.GetComponent<StableObjectId>();
        return stable != null ? stable.Id : null;
    }
}