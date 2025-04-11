using System.Numerics;

namespace UnitySceneReader.SceneElements
{
    public class TransformElement : SceneElement
    {
        public List<TransformElement> Children;
        public TransformElement Parent;

        public Quaternion localRotation;
        public Vector3 localPosition;
        public Vector3 localScale;
        public Vector3 localEulerAnglesHint;
    }
}
