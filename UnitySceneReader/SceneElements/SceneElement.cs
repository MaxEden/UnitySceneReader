using YamlDotNet.RepresentationModel;

namespace UnitySceneReader.SceneElements
{
    public class SceneElement
    {
        public bool stripped;
        public long Anchor;
        public YamlNode TypeName;
        public YamlMappingNode props;
        public long gameObjectId;
        public GameObjectElement gameObject;
        public YamlNode Root;
        public int typeId;
        public int Depth;
        public List<AppliedModification> AppliedModifications;

        public int GetTransformDepth()
        {
            int result = 0;
            var par = gameObject.transform.Parent;
            while (par!=null)
            {
                result++;
                par = par.Parent;
            }

            return result;
        }
    }

    public class AppliedModification
    {
        public PrefabInstanceElement parentPrefab;
        public List<Modification> Modifications = new();

        public override string ToString()
        {
            return string.Join(", ", Modifications.Select(p => p.propertyPath));
        }
    }
}