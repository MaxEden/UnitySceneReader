using UnitySceneReader;
using UnitySceneReader.SceneElements;

namespace Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var assets = Path.GetFullPath(@"../../../../TestProject/Assets");

            Console.WriteLine("Reading meta " + assets + "...");

            var meta = SceneReader.ReadMeta(assets);

            var scene = assets+"/Scenes/SampleScene.unity";

            Console.WriteLine("Reading scene " + scene + "...");

            var sceneAsset = SceneReader.ReadScene(scene, meta);
            var elements = SceneReader.Flatten(sceneAsset.elements, meta);

            var gameObjects = elements.OfType<GameObjectElement>().ToArray();

            Console.WriteLine("-----------------------------");
            foreach (var gameObject in gameObjects)
            {
                var pad = new string(' ', 4 * gameObject.Depth);
                Console.WriteLine(pad + gameObject.name);

                foreach (var component in gameObject.Components)
                {
                    Console.Write(pad + "    " + component.TypeName);
                    if (component is MonoBehaviourElement monoBehaviour)
                    {
                        Console.Write(" "+monoBehaviour.script.fullTypeName);
                    }
                    Console.WriteLine();

                    if (component.AppliedModifications != null && component.AppliedModifications.Count > 0)
                    {
                        Console.WriteLine(pad + "        overrides:");
                        foreach (var appliedModification in component.AppliedModifications)
                        {
                            Console.WriteLine(pad + "        depth:" + appliedModification.parentPrefab.Depth);

                            foreach (var modification in appliedModification.Modifications)
                            {
                                Console.WriteLine(pad + "        " + modification.propertyPath + $" {modification.value??""}");
                            }
                        }
                    }
                }
            }
            Console.WriteLine("-----------------------------");
        }
    }
}
