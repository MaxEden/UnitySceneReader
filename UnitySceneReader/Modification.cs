using YamlDotNet.RepresentationModel;

namespace UnitySceneReader
{
    public class Modification
    {
        public long prefabAnchor;
        public string propertyPath;
        public YamlNode value;
        public long objectReference_FileId;
        public Guid objectReference_guid;
    }
}
