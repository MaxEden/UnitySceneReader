namespace UnitySceneReader.SceneElements
{
    public class PrefabInstanceElement : SceneElement
    {
        public Guid SourcePrefabId;
        public PrefabAsset prefab;
        public Dictionary<long, List<Modification>> Modifications = new();
        public PrefabInstanceElement ParentPrefab;
    }
}