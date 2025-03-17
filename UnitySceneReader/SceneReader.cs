using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using System.Text;
using UnitySceneReader.SceneElements;
using YamlDotNet.RepresentationModel;

namespace UnitySceneReader
{
    public class SceneReader
    {
        public static List<SceneElement> Flatten(List<SceneElement> elements, MetaInfo meta)
        {
            return Flatten(0, elements, meta, null);
        }

        public static List<SceneElement> Flatten(int depth, List<SceneElement> elements, MetaInfo meta, PrefabInstanceElement parentPrefab)
        {
            var result = new List<SceneElement>();

            foreach (var el in elements)
            {
                el.Depth = depth;
                if (el is PrefabInstanceElement pel)
                {
                    var prefabElements = ReadPrefab(pel.prefab, meta);
                    foreach (var prefabElement in prefabElements)
                    {
                        prefabElement.Depth = depth + 1;
                        if (prefabElement is not PrefabInstanceElement && !prefabElement.stripped)
                        {
                            prefabElement.gameObject.transform.Parent = pel.TransformParent;
                        }
                    }

                    pel.ParentPrefab = parentPrefab;
                    var subElements = Flatten(depth + 1, prefabElements, meta, pel);
                    result.AddRange(subElements);
                }
                else
                {
                    result.Add(el);

                    var prefAnchor = el.Anchor;
                    var modPrefab = parentPrefab;

                    while (modPrefab != null)
                    {
                        if (modPrefab.Modifications.TryGetValue(prefAnchor, out var mod))
                        {
                            el.AppliedModifications ??= new();
                            var applied = el.AppliedModifications.FirstOrDefault(p => p.parentPrefab == modPrefab);
                            if (applied == null)
                            {
                                applied = new AppliedModification()
                                {
                                    parentPrefab = modPrefab
                                };
                                el.AppliedModifications.Add(applied);
                            }


                            applied.Modifications.AddRange(mod);
                        }

                        prefAnchor ^= modPrefab.Anchor;
                        modPrefab = modPrefab.ParentPrefab;
                    }
                }
            }

            return result;
        }

        public static SceneAsset ReadScene(string path, MetaInfo meta)
        {
            var result = new SceneAsset();
            result.elements = ReadSceneElements(path, meta);
            return result;
        }

        private static List<SceneElement> ReadSceneElements(string path, MetaInfo meta)
        {
            var lines = File.ReadAllLines(path);

            var sb = new StringBuilder();
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].EndsWith(" stripped"))
                {
                    lines[i] = lines[i].Substring(0, lines[i].Length - " stripped".Length);
                }

                sb.AppendLine(lines[i]);
            }

            var sceneText = sb.ToString();
            var input = new StringReader(sceneText);

            var yaml = new YamlStream();
            yaml.Load(input);

            var els = new List<SceneElement>();

            string UnityTagPrefix = "tag:unity3d.com,2011:";

            foreach (var doc in yaml.Documents)
            {
                var root = doc.RootNode;
                if (!int.TryParse(root.Tag.Value.Replace(UnityTagPrefix, ""), out var typeId))
                {

                }

                var el = new SceneElement();
                if (typeId == 4) el = new TransformElement();
                if (typeId == 1) el = new GameObjectElement();
                if (typeId == 114) el = new MonoBehaviourElement();
                if (typeId == 1001) el = new PrefabInstanceElement();
                if (typeId == 224) el = new RectTransformElement();

                el.typeId = typeId;
                el.Root = root;

                var anchor = long.Parse(root.Anchor.Value);

                el.Anchor = anchor;

                var pair = ((YamlMappingNode)root).Children.Single();

                el.TypeName = pair.Key;

                el.props = (YamlMappingNode)pair.Value;

                if (el is not GameObjectElement)
                {
                    el.gameObjectId = Reader.TryReadFileId(el.props, "m_GameObject");
                }

                if (el is GameObjectElement gel)
                {
                    gel.name = Reader.ReadString(el.props, "m_Name");
                    gel.isActive = Reader.Read<int>(el.props, "m_IsActive");
                }

                if (el is MonoBehaviourElement mel)
                {
                    if (el.props.Children.TryGetValue(new YamlScalarNode("m_Script"), out var value))
                    {
                        mel.scriptGuid = Reader.ReadGuid(value);
                    }

                    mel.Enabled = Reader.Read<int>(el.props, "m_Enabled");
                }

                if (el is PrefabInstanceElement pel)
                {
                    pel.SourcePrefabId = Reader.ReadGuid(el.props[new YamlScalarNode("m_SourcePrefab")]);
                    

                    var modifications = el.props[new YamlScalarNode("m_Modification")];
                    pel.TransformParentId = Reader.ReadFileId(modifications[new YamlScalarNode("m_TransformParent")]);

                    var modification = modifications[new YamlScalarNode("m_Modifications")];

                    foreach (var item in ((YamlSequenceNode)modification).Children)
                    {
                        var mod = new Modification();

                        var target = item[new YamlScalarNode("target")];
                        mod.prefabAnchor = Reader.ReadFileId(target);
                        mod.propertyPath = Reader.ReadString(item, "propertyPath");
                        mod.value = item[new YamlScalarNode("value")];
                        var objectReference = item[new YamlScalarNode("objectReference")];
                        mod.objectReference_FileId = Reader.ReadFileId(objectReference);
                        if (mod.objectReference_FileId != 0)
                        {
                            mod.objectReference_guid = Reader.ReadGuid(objectReference);
                        }

                        if (!pel.Modifications.TryGetValue(mod.prefabAnchor, out var list))
                        {
                            list = new List<Modification>();
                            pel.Modifications[mod.prefabAnchor] = list;
                        }

                        list.Add(mod);
                    }
                }

                els.Add(el);
            }

            var idToEl = els.ToDictionary(p => p.Anchor, p => p);

            foreach (var el in els)
            {
                if (el is TransformElement tel)
                {
                    if (tel.props.Children.TryGetValue(new YamlScalarNode("m_Children"), out var children))
                    {
                        tel.Children ??= new List<TransformElement>();

                        foreach (var item in ((YamlSequenceNode)children).Children)
                        {
                            var id = Reader.ReadFileId(item);
                            var cel = (TransformElement)idToEl[id];
                            tel.Children.Add(cel);
                            cel.Parent = tel;
                        }

                        var prop = (YamlMappingNode)el.props.Children[new YamlScalarNode("m_LocalRotation")];
                        tel.localRotation = Reader.ReadQuaternion(prop);

                        var prop2 = (YamlMappingNode)el.props.Children[new YamlScalarNode("m_LocalPosition")];
                        tel.localPosition = Reader.ReadVector3(prop2);

                        var prop3 = (YamlMappingNode)el.props.Children[new YamlScalarNode("m_LocalScale")];
                        tel.localScale = Reader.ReadVector3(prop3);
                    }
                    else
                    {
                        //stripped
                        tel.stripped = true;
                    }


                }

                if (el is GameObjectElement gel)
                {
                    gel.gameObject = gel;

                    if (gel.props.Children.TryGetValue(new YamlScalarNode("m_Component"), out var components))
                    {
                        gel.Components ??= new List<SceneElement>();

                        foreach (var item in ((YamlSequenceNode)components).Children)
                        {
                            var id = Reader.TryReadFileId(item, "component");
                            var cel = idToEl[id];
                            gel.Components.Add(cel);
                            cel.gameObject = gel;

                            if (cel is TransformElement trel)
                            {
                                gel.transform = trel;
                            }
                        }
                    }
                    else
                    {
                        //stripped
                        gel.stripped = true;
                    }
                }

                if (el is MonoBehaviourElement mel)
                {
                    if (meta.idToMono.TryGetValue(mel.scriptGuid, out var script))
                    {
                        mel.script = script;
                    }
                }

                if (el is PrefabInstanceElement pel)
                {
                    if (meta.idToPrefab.TryGetValue(pel.SourcePrefabId, out var prefab))
                    {
                        pel.prefab = prefab;
                    }

                    if (idToEl.TryGetValue(pel.TransformParentId, out var parentEl))
                    {
                        pel.TransformParent = (TransformElement)parentEl;
                    }
                }
            }

            return els;
        }

        public static List<MonoScript> ReadScriptsMeta(string assets)
        {
            var scripts = Directory.GetFiles(assets, "*.cs.meta", SearchOption.AllDirectories);
            var result = new List<MonoScript>();

            foreach (var script in scripts)
            {
                var mono = new MonoScript();
                mono.path = script;

                var text = File.ReadAllText(script);
                var input = new StringReader(text);
                var yaml = new YamlStream();
                yaml.Load(input);
                mono.guid = Reader.ReadGuid(yaml.Documents[0].RootNode);


                ReadFullInfo(mono);

                result.Add(mono);
            }

            return result;
        }

        private static void ReadFullInfo(MonoScript mono)
        {
            var path = mono.path.Substring(0, mono.path.Length - ".meta".Length);

            var code = File.ReadAllText(path);

            // Parse the code into a syntax tree
            var syntaxTree = CSharpSyntaxTree.ParseText(code);

            // Get the root of the syntax tree
            var root = syntaxTree.GetRoot();

            // Find all class declarations
            var classDeclaration = root.DescendantNodes()
                .OfType<ClassDeclarationSyntax>().FirstOrDefault();

            if (classDeclaration != null)
            {
                mono.fullTypeName = GetFullClassName(classDeclaration);
            }
        }

        private static string GetFullClassName(ClassDeclarationSyntax classDeclaration)
        {
            // Get the namespace of the class
            var namespaceDeclaration = classDeclaration.Ancestors()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();

            // Build the full name
            var fullName = namespaceDeclaration != null
                ? $"{namespaceDeclaration.Name}.{classDeclaration.Identifier.Text}"
                : classDeclaration.Identifier.Text;

            // If the class is within a partial class, it might be nested
            var containingClass = classDeclaration.Ancestors()
                .OfType<ClassDeclarationSyntax>()
                .FirstOrDefault();

            while (containingClass != null)
            {
                fullName = $"{containingClass.Identifier.Text}.{fullName}";
                containingClass = containingClass.Ancestors()
                    .OfType<ClassDeclarationSyntax>()
                    .FirstOrDefault();
            }

            return fullName;
        }

        public static List<PrefabAsset> ReadPrefabsMeta(string assets)
        {
            var prefabs = Directory.GetFiles(assets, "*.prefab.meta", SearchOption.AllDirectories);
            var result = new List<PrefabAsset>();

            foreach (var path in prefabs)
            {
                var prefab = new PrefabAsset();
                prefab.path = path;

                var text = File.ReadAllText(path);
                var input = new StringReader(text);
                var yaml = new YamlStream();
                yaml.Load(input);
                prefab.guid = Reader.ReadGuid(yaml.Documents[0].RootNode);
                result.Add(prefab);
            }

            return result;
        }

        private static List<SceneElement> ReadPrefab(PrefabAsset asset, MetaInfo meta)
        {
            var path = asset.path.Substring(0, asset.path.Length - ".meta".Length);
            var elements = ReadSceneElements(path, meta);
            return elements;
        }

        public static MetaInfo ReadMeta(string assets)
        {
            var scripts = SceneReader.ReadScriptsMeta(assets);
            var prefabs = SceneReader.ReadPrefabsMeta(assets);

            var meta = new MetaInfo();
            meta.idToMono = scripts.ToDictionary(p => p.guid, p => p);
            meta.idToPrefab = prefabs.ToDictionary(p => p.guid, p => p);

            return meta;
        }
    }
}
