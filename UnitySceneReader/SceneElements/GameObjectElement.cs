namespace UnitySceneReader.SceneElements
{
    public class GameObjectElement : SceneElement
    {
        public string name;
        public int isActive;
        public List<SceneElement> Components;
        public TransformElement transform;
    }
}