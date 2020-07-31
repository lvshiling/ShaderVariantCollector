using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine.Rendering;
using UnityEditor.Rendering;
using System.Reflection;
using System.Text.RegularExpressions;

namespace SVCTool
{

    public static class Utils
    {

        public static Type FindTypeInAssembly(String typeName, Assembly assembly = null)
        {
            Type type = null;
            if (assembly == null)
            {
                type = Type.GetType(typeName, false);
            }
            if (type == null && assembly != null)
            {
                var types = assembly.GetTypes();
                for (int j = 0; j < types.Length; ++j)
                {
                    var b = types[j];
                    if (b.FullName == typeName)
                    {
                        type = b;
                        break;
                    }
                }
            }
            return type;
        }

        public static Type FindType(String typeName, String assemblyName = null)
        {
            Type type = null;
            try
            {
                if (String.IsNullOrEmpty(assemblyName))
                {
                    type = Type.GetType(typeName, false);
                }
                if (type == null)
                {
                    var asm = AppDomain.CurrentDomain.GetAssemblies();
                    for (int i = 0; i < asm.Length; ++i)
                    {
                        var a = asm[i];
                        if (String.IsNullOrEmpty(assemblyName) || a.GetName().Name == assemblyName)
                        {
                            var types = a.GetTypes();
                            for (int j = 0; j < types.Length; ++j)
                            {
                                var b = types[j];
                                if (b.FullName == typeName)
                                {
                                    type = b;
                                    goto END;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
        END:
            if (type == null)
            {
                Debug.LogWarningFormat("FindType( \"{0}\", \"{1}\" ) failed!",
                    typeName, assemblyName ?? String.Empty);
            }
            return type;
        }

        public static object RflxGetValue(String typeName, String memberName, String assemblyName = null)
        {
            object value = null;
            var type = FindType(typeName, assemblyName);
            if (type != null)
            {
                var smembers = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0, count = smembers.Length; i < count && value == null; ++i)
                {
                    var m = smembers[i];
                    if ((m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property) &&
                        m.Name == memberName)
                    {
                        var pi = m as PropertyInfo;
                        if (pi != null)
                        {
                            value = pi.GetValue(null, null);
                        }
                        else
                        {
                            var fi = m as FieldInfo;
                            if (fi != null)
                            {
                                value = fi.GetValue(null);
                            }
                        }
                    }
                }
            }
            return value;
        }

        public static bool RflxSetValue(String typeName, String memberName, object value, String assemblyName = null)
        {
            var type = FindType(typeName, assemblyName);
            if (type != null)
            {
                var smembers = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < smembers.Length; ++i)
                {
                    var m = smembers[i];
                    if ((m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property) &&
                        m.Name == memberName)
                    {
                        var pi = m as PropertyInfo;
                        if (pi != null)
                        {
                            pi.SetValue(null, value, null);
                            return true;
                        }
                        else
                        {
                            var fi = m as FieldInfo;
                            if (fi != null)
                            {
                                if (fi.IsLiteral == false && fi.IsInitOnly == false)
                                {
                                    fi.SetValue(null, value);
                                    return true;
                                }
                                else
                                {
                                    return false;
                                }
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static object RflxGetValue(Type type, String memberName)
        {
            object value = null;
            if (type != null)
            {
                var smembers = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0, count = smembers.Length; i < count && value == null; ++i)
                {
                    var m = smembers[i];
                    if ((m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property) &&
                        m.Name == memberName)
                    {
                        var pi = m as PropertyInfo;
                        if (pi != null)
                        {
                            value = pi.GetValue(null, null);
                        }
                        else
                        {
                            var fi = m as FieldInfo;
                            if (fi != null)
                            {
                                value = fi.GetValue(null);
                            }
                        }
                    }
                }
            }
            return value;
        }

        public static bool RflxSetValue(Type type, String memberName, object value)
        {
            if (type != null)
            {
                var smembers = type.GetMembers(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < smembers.Length; ++i)
                {
                    var m = smembers[i];
                    if ((m.MemberType == MemberTypes.Field || m.MemberType == MemberTypes.Property) &&
                        m.Name == memberName)
                    {
                        var pi = m as PropertyInfo;
                        if (pi != null)
                        {
                            pi.SetValue(null, value, null);
                            return true;
                        }
                        else
                        {
                            var fi = m as FieldInfo;
                            if (fi != null && fi.IsLiteral == false && fi.IsInitOnly == false)
                            {
                                fi.SetValue(null, value);
                                return true;
                            }
                        }
                    }
                }
            }
            return false;
        }

        public static object RflxStaticCall(String typeName, String funcName, object[] parameters = null, String assemblyName = null)
        {
            var type = FindType(typeName, assemblyName);
            if (type != null)
            {
                var f = type.GetMethod(funcName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (f != null)
                {
                    var r = f.Invoke(null, parameters);
                    return r;
                }
            }
            return null;
        }

        public static object RflxStaticCall(Type type, String funcName, object[] parameters = null)
        {
            if (type != null)
            {
                var f = type.GetMethod(funcName, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
                if (f != null)
                {
                    var r = f.Invoke(null, parameters);
                    return r;
                }
            }
            return null;
        }

        public static String GetProjectUnityTempPath()
        {
            var rootPath = Environment.CurrentDirectory.Replace('\\', '/');
            rootPath += "/Temp";
            if (Directory.Exists(rootPath))
            {
                rootPath = Path.GetFullPath(rootPath);
                return rootPath.Replace('\\', '/');
            }
            else
            {
                return rootPath;
            }
        }

        public static bool IsUnityDefaultResource(String path)
        {
            return String.IsNullOrEmpty(path) == false &&
                (path == "Resources/unity_builtin_extra" ||
                path == "Library/unity default resources");
        }
    }

    public static class ListExtra
    {

        public static void Resize<T>(this List<T> list, int sz, T c)
        {
            int cur = list.Count;
            if (sz < cur)
            {
                list.RemoveRange(sz, cur - sz);
            }
            else if (sz > cur)
            {
                //this bit is purely an optimisation, to avoid multiple automatic capacity changes.
                if (sz > list.Capacity)
                {
                    list.Capacity = sz;
                }
                int count = sz - cur;
                for (int i = 0; i < count; ++i)
                {
                    list.Add(c);
                }
            }
        }

        public static void Resize<T>(this List<T> list, int sz)
        {
            Resize(list, sz, default(T));
        }

        public static int FindIndex<T, C>(this IList<T> list, C ctx, Func<T, C, bool> match)
        {
            for (int i = 0, count = list.Count; i < count; ++i)
            {
                if (match(list[i], ctx))
                {
                    return i;
                }
            }
            return -1;
        }
    }

    public static class ShaderInspectorPlatformsPopup
    {

        static Type s_Type = null;
        static Type internalType
        {
            get
            {
                if (s_Type == null)
                {
                    s_Type = Utils.FindType("UnityEditor.ShaderInspectorPlatformsPopup", "UnityEditor");
                }
                return s_Type;
            }
        }
        public static int currentMode
        {
            get
            {
                return (int)Utils.RflxGetValue(internalType, "currentMode");
            }
            set
            {
                Utils.RflxSetValue(internalType, "currentMode", value);
            }
        }
        public static int currentPlatformMask
        {
            get
            {
                return (int)Utils.RflxGetValue(internalType, "currentPlatformMask");
            }
            set
            {
                Utils.RflxSetValue(internalType, "currentPlatformMask", value);
            }
        }
        public static int currentVariantStripping
        {
            get
            {
                return (int)Utils.RflxGetValue(internalType, "currentVariantStripping");
            }
            set
            {
                Utils.RflxSetValue(internalType, "currentVariantStripping", value);
            }
        }
    }

    /// <summary>
    /// �������洢Combinations��Ϣ
    /// </summary>
    public class ShaderParsedCombinations
    {

        internal static readonly Regex REG_FILE_HEADER = new Regex(@"^// Total snippets: (\d+)");
        internal static readonly Regex REG_SNIPPET_ID = new Regex(@"^// Snippet #(\d+) platforms ([0-9a-fA-F]+):");
        internal static readonly Regex REG_SHADER_FEATURE_KEYWORDS = new Regex(@"^Keywords stripped away when not used: (.+)$");
        internal static readonly Regex REG_MULTI_COMPILE_KEYWORDS = new Regex(@"^Keywords always included into build: (.+)$");
        internal static readonly Regex REG_BUILTIN_KEYWORDS = new Regex(@"^Builtin keywords used: (.+)$");
        internal static readonly Regex REG_VARIANTS_NUM = new Regex(@"^(\d+) keyword variants used in scene:$");
        internal const String TAG_NO_KEYWORDS_DEFINED = "<no keywords defined>";

        public class Snippet
        {
            public int id;
            public int platformBits;
            public String[] shader_features;
            public String[] multi_compiles;
            public String[] builtins;
            public List<String[]> variants;
        }
        public Shader shader;
        public List<Snippet> snippets;

        public bool IsValidKeyword(String keyword)
        {
            if (snippets != null)
            {
                for (int i = 0; i < snippets.Count; ++i)
                {
                    var snippet = snippets[i];
                    if (snippet.shader_features != null)
                    {
                        if (Array.IndexOf(snippet.shader_features, keyword) >= 0)
                        {
                            return true;
                        }
                    }
                    if (snippet.multi_compiles != null)
                    {
                        if (Array.IndexOf(snippet.multi_compiles, keyword) >= 0)
                        {
                            return true;
                        }
                    }
                    if (snippet.builtins != null)
                    {
                        if (Array.IndexOf(snippet.builtins, keyword) >= 0)
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public String Dump()
        {
            String result;
            var sb = new StringBuilder();
            sb.Append(ShaderUtils.DumpShaderPasses(shader));

            if (snippets != null && snippets.Count > 0)
            {
                sb.AppendFormat("// Total snippets: {0}", snippets.Count).AppendLine();
                sb.AppendLine();

                for (int i = 0; i < snippets.Count; ++i)
                {
                    var snippet = snippets[i];
                    sb.AppendFormat("// Snippet #{0} platforms {1:x}:", snippet.id, snippet.platformBits).AppendLine();
                    sb.AppendLine();

                    if (snippet.shader_features != null && snippet.shader_features.Length > 0)
                    {
                        sb.AppendFormat("Keywords stripped away when not used:\n\n{0}", String.Join("\n", snippet.shader_features)).AppendLine();
                        sb.AppendLine();
                    }
                    if (snippet.multi_compiles != null && snippet.multi_compiles.Length > 0)
                    {
                        sb.AppendFormat("Keywords always included into build:\n\n{0}", String.Join("\n", snippet.multi_compiles)).AppendLine();
                        sb.AppendLine();
                    }
                    if (snippet.builtins != null && snippet.builtins.Length > 0)
                    {
                        sb.AppendFormat("Builtin keywords used:\n\n{0}", String.Join("\n", snippet.builtins)).AppendLine();
                        sb.AppendLine();
                    }
                    sb.AppendLine();

                    if (snippet.variants != null && snippet.variants.Count > 0)
                    {
                        sb.AppendFormat("{0} keyword variants used in scene:", snippet.variants.Count).AppendLine();
                        sb.AppendLine();
                        for (int j = 0; j < snippet.variants.Count; ++j)
                        {
                            sb.AppendFormat("[{0}]:", j).AppendLine();
                            sb.Append(String.Join("\n", snippet.variants[j].ToArray()));
                            sb.AppendLine();
                            sb.AppendLine();
                        }
                    }
                    else
                    {
                        sb.Append(TAG_NO_KEYWORDS_DEFINED).AppendLine();
                    }
                    sb.AppendLine();
                }
                result = sb.ToString();
            }
            else
            {
                result = "Empty ShaderParsedCombinations.";
            }
            Debug.Log(result);
            return result;
        }
    }

    /// <summary>
    /// ����༭����ShaderUtil��Ҫ������װ
    /// </summary>
    public static class ShaderUtils
    {

        public enum ShaderCompilerPlatformType
        {
            OpenGL,
            D3D9,
            Xbox360,
            PS3,
            D3D11,
            OpenGLES20,
            OpenGLES20Desktop,
            Flash,
            D3D11_9x,
            OpenGLES30,
            PSVita,
            PS4,
            XboxOne,
            PSM,
            Metal,
            OpenGLCore,
            N3DS,
            WiiU,
            Vulkan,
            Switch,
            Count
        }

        static MethodInfo _GetShaderVariantEntries = null;

        internal static MethodInfo GetShaderVariantEntriesMethod
        {
            get
            {
                if (_GetShaderVariantEntries == null)
                {
                    _GetShaderVariantEntries = typeof(ShaderUtil).GetMethod(
                        "GetShaderVariantEntries", BindingFlags.NonPublic | BindingFlags.Static);
                }
                return _GetShaderVariantEntries;
            }
        }

        public static List<Texture> GetTextures(Material mat)
        {
            var list = new List<Texture>();
            var count = GetPropertyCount(mat);
            for (var i = 0; i < count; i++)
            {
                if (GetPropertyType(mat, i) == ShaderUtil.ShaderPropertyType.TexEnv)
                {
                    list.Add((Texture)GetProperty(mat, i));
                }
            }
            return list;
        }

        public static int GetPropertyCount(Material mat)
        {
            return ShaderUtil.GetPropertyCount(mat.shader);
        }

        public static ShaderUtil.ShaderPropertyType GetPropertyType(Material mat, int index)
        {
            return ShaderUtil.GetPropertyType(mat.shader, index);
        }

        public static string GetPropertyName(Material mat, int index)
        {
            return ShaderUtil.GetPropertyName(mat.shader, index);
        }

        public static void SetProperty(Material material, int index, object value)
        {
            var name = GetPropertyName(material, index);
            var type = GetPropertyType(material, index);
            switch (type)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                    material.SetColor(name, (Color)value);
                    break;
                case ShaderUtil.ShaderPropertyType.Vector:
                    material.SetVector(name, (Vector4)value);
                    break;
                case ShaderUtil.ShaderPropertyType.Range:
                    material.SetFloat(name, (float)value);
                    break;
                case ShaderUtil.ShaderPropertyType.Float:
                    material.SetFloat(name, (float)value);
                    break;
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    material.SetTexture(name, (Texture)value);
                    break;
            }
        }

        public static object GetProperty(Material material, int index)
        {
            var name = GetPropertyName(material, index);
            var type = GetPropertyType(material, index);
            switch (type)
            {
                case ShaderUtil.ShaderPropertyType.Color:
                    return material.GetColor(name);
                case ShaderUtil.ShaderPropertyType.Vector:
                    return material.GetVector(name);
                case ShaderUtil.ShaderPropertyType.Float:
                case ShaderUtil.ShaderPropertyType.Range:
                    return material.GetFloat(name);
                case ShaderUtil.ShaderPropertyType.TexEnv:
                    return material.GetTexture(name);
            }
            return null;
        }

        public static bool DoesShaderHasAnyOneOfProperties(Shader shader, String name, params ShaderUtil.ShaderPropertyType[] types)
        {
            for (int i = 0, count = ShaderUtil.GetPropertyCount(shader); i < count; ++i)
            {
                if (ShaderUtil.GetPropertyName(shader, i) == name)
                {
                    var type = ShaderUtil.GetPropertyType(shader, i);
                    if (Array.IndexOf(types, type) != -1)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static int FindPropertyIndex(Shader shader, String name, ShaderUtil.ShaderPropertyType? type = null)
        {
            var count = ShaderUtil.GetPropertyCount(shader);
            for (int i = 0; i < count; ++i)
            {
                var _name = ShaderUtil.GetPropertyName(shader, i);
                var _type = ShaderUtil.GetPropertyType(shader, i);
                if (_name == name && (type == null || type.Value == _type))
                {
                    return i;
                }
            }
            return -1;
        }

        public static void OpenShaderCombinations(Shader shader, bool usedBySceneOnly)
        {
            if (!HasCodeSnippets(shader))
            {
                Debug.LogErrorFormat("{0} is precompiled shader.", shader.name);
                return;
            }
            Utils.RflxStaticCall(
                typeof(ShaderUtil),
                "OpenShaderCombinations",
                new object[] { shader, usedBySceneOnly });
        }

        public static bool HasCodeSnippets(Shader shader)
        {
            return HasShaderSnippets(shader) || HasSurfaceShaders(shader) || HasFixedFunctionShaders(shader);
        }

        public static void OpenCurrentCompiledShader(Shader shader)
        {
            if (!HasCodeSnippets(shader))
            {
                Debug.LogErrorFormat("{0} is precompiled shader.", shader.name);
                return;
            }
            OpenCompiledShader(shader, ShaderInspectorPlatformsPopup.currentMode, ShaderInspectorPlatformsPopup.currentPlatformMask, ShaderInspectorPlatformsPopup.currentVariantStripping == 0);
        }

        public static void OpenCompiledShader(Shader shader, int mode, int externPlatformsMask, bool includeAllVariants)
        {
            if (!HasCodeSnippets(shader))
            {
                Debug.LogErrorFormat("{0} is precompiled shader.", shader.name);
                return;
            }
            Utils.RflxStaticCall(
                typeof(ShaderUtil),
                "OpenCompiledShader",
                new object[] { shader, mode, externPlatformsMask, includeAllVariants });
        }

        public static bool HasShaderSnippets(Shader shader)
        {
            return (bool)Utils.RflxStaticCall(
                typeof(ShaderUtil),
                "HasShaderSnippets",
                new object[] { shader });
        }

        public static bool HasSurfaceShaders(Shader shader)
        {
            return (bool)Utils.RflxStaticCall(
                typeof(ShaderUtil),
                "HasSurfaceShaders",
                new object[] { shader });
        }

        public static bool HasFixedFunctionShaders(Shader shader)
        {
            return (bool)Utils.RflxStaticCall(
                typeof(ShaderUtil),
                "HasFixedFunctionShaders",
                new object[] { shader });
        }

        public static void ClearCurrentShaderVariantCollection()
        {
            Utils.RflxStaticCall(
                typeof(ShaderUtil),
                "ClearCurrentShaderVariantCollection", null);
        }

        public static void SaveCurrentShaderVariantCollection(String path)
        {
            Utils.RflxStaticCall(
                typeof(ShaderUtil),
                "SaveCurrentShaderVariantCollection", new object[] { path });
        }

        public static int GetCurrentShaderVariantCollectionShaderCount()
        {
            var shaderCount = Utils.RflxStaticCall(
                typeof(ShaderUtil),
                "GetCurrentShaderVariantCollectionShaderCount", null);
            return (int)shaderCount;
        }

        public static int GetCurrentShaderVariantCollectionVariantCount()
        {
            var shaderVariantCount = Utils.RflxStaticCall(
                typeof(ShaderUtil),
                "GetCurrentShaderVariantCollectionVariantCount", null);
            return (int)shaderVariantCount;
        }

        class ShaderParsedCombinationsItem
        {
            internal ShaderParsedCombinations data;
            internal long lastWriteTime;
        }

        static Dictionary<Shader, ShaderParsedCombinationsItem> s_ShaderParsedCombinationsCache = new Dictionary<Shader, ShaderParsedCombinationsItem>();

        public static ShaderParsedCombinations ParseShaderCombinations(Shader shader, bool usedBySceneOnly)
        {
            if (shader == null || !ShaderUtils.HasCodeSnippets(shader))
            {
                return null;
            }
            var assetPath = AssetDatabase.GetAssetPath(shader);
            var deps = AssetDatabase.GetDependencies(assetPath);
            long lastWriteTime = 0;
            for (int i = 0; i < deps.Length; ++i)
            {
                var fi = new FileInfo(deps[i]);
                if (fi.Exists)
                {
                    if (fi.LastWriteTime.ToFileTime() > lastWriteTime)
                    {
                        lastWriteTime = fi.LastWriteTime.ToFileTime();
                    }
                }
            }
            ShaderParsedCombinationsItem item;
            if (s_ShaderParsedCombinationsCache.TryGetValue(shader, out item))
            {
                if (item.lastWriteTime != lastWriteTime)
                {
                    s_ShaderParsedCombinationsCache.Remove(shader);
                }
                else
                {
                    return item.data;
                }
            }

            ShaderParsedCombinations result = null;
            try
            {
                var fixedShaderName = shader.name.Replace('/', '-');
                var combFilePath = String.Format("{0}/ParsedCombinations-{1}.shader", Utils.GetProjectUnityTempPath(), fixedShaderName);
                if (File.Exists(combFilePath))
                {
                    File.Delete(combFilePath);
                }
                Func<String, String[]> keywordsSpliter = src => {
                    var srcKeywords = src.Split(' ');
                    var dstKeywords = new List<String>();
                    for (int j = 0; j < srcKeywords.Length; ++j)
                    {
                        var x = srcKeywords[j].Trim();
                        if (!String.IsNullOrEmpty(x) && !dstKeywords.Contains(x))
                        {
                            dstKeywords.Add(x);
                        }
                    }
                    if (dstKeywords.Count > 0)
                    {
                        return dstKeywords.ToArray();
                    }
                    return null;
                };
                ShaderUtils.OpenShaderCombinations(shader, true);
                if (File.Exists(combFilePath))
                {
                    var lines = File.ReadAllLines(combFilePath);
                    ShaderParsedCombinations.Snippet curSnippet = null;
                    for (int i = 0; i < lines.Length; ++i)
                    {
                        var line = lines[i];
                        if (String.IsNullOrEmpty(line) || Char.IsWhiteSpace(line[0]))
                        {
                            continue;
                        }
                        Match match = ShaderParsedCombinations.REG_FILE_HEADER.Match(line);
                        if (match.Success)
                        {
                            if (match.Groups.Count > 1)
                            {
                                int num;
                                if (int.TryParse(match.Groups[1].Value, out num) && num > 0)
                                {
                                    result = new ShaderParsedCombinations();
                                    result.shader = shader;
                                    result.snippets = new List<ShaderParsedCombinations.Snippet>(num);
                                }
                            }
                        }
                        else if (result != null && (match = ShaderParsedCombinations.REG_SNIPPET_ID.Match(line)).Success)
                        {
                            if (match.Groups.Count > 2)
                            {
                                int id;
                                if (int.TryParse(match.Groups[1].Value, out id))
                                {
                                    int bits;
                                    if (int.TryParse(match.Groups[2].Value, System.Globalization.NumberStyles.HexNumber, null, out bits))
                                    {
                                        var snippet = new ShaderParsedCombinations.Snippet();
                                        curSnippet = snippet;
                                        snippet.id = id;
                                        snippet.platformBits = bits;
                                        result.snippets.Add(snippet);
                                    }
                                }
                            }
                        }
                        else if (curSnippet != null && (match = ShaderParsedCombinations.REG_SHADER_FEATURE_KEYWORDS.Match(line)).Success)
                        {
                            if (match.Groups.Count > 1)
                            {
                                var keywords = keywordsSpliter(match.Groups[1].Value);
                                if (keywords != null)
                                {
                                    curSnippet.shader_features = keywords;
                                }
                            }
                        }
                        else if (curSnippet != null && (match = ShaderParsedCombinations.REG_MULTI_COMPILE_KEYWORDS.Match(line)).Success)
                        {
                            if (match.Groups.Count > 1)
                            {
                                var keywords = keywordsSpliter(match.Groups[1].Value);
                                if (keywords != null)
                                {
                                    curSnippet.multi_compiles = keywords;
                                }
                            }
                        }
                        else if (curSnippet != null && (match = ShaderParsedCombinations.REG_BUILTIN_KEYWORDS.Match(line)).Success)
                        {
                            if (match.Groups.Count > 1)
                            {
                                var keywords = keywordsSpliter(match.Groups[1].Value);
                                if (keywords != null)
                                {
                                    curSnippet.builtins = keywords;
                                }
                            }
                        }
                        else if (curSnippet != null && (match = ShaderParsedCombinations.REG_VARIANTS_NUM.Match(line)).Success)
                        {
                            if (match.Groups.Count > 1)
                            {
                                int num;
                                if (int.TryParse(match.Groups[1].Value, out num) && num > 0)
                                {
                                    curSnippet.variants = new List<String[]>(num);
                                }
                            }
                        }
                        else if (curSnippet != null && line.StartsWith(ShaderParsedCombinations.TAG_NO_KEYWORDS_DEFINED))
                        {
                            if (curSnippet.variants != null)
                            {
                                curSnippet.variants = null;
                            }
                        }
                        else if (curSnippet != null && curSnippet.variants != null)
                        {
                            var keywords = keywordsSpliter(line);
                            if (keywords != null)
                            {
                                curSnippet.variants.Add(keywords);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            s_ShaderParsedCombinationsCache.Add(shader,
                new ShaderParsedCombinationsItem { data = result, lastWriteTime = lastWriteTime });
            return result;
        }

        public static String DumpShaderPasses(Shader shader)
        {
            String result;
            var sb = new StringBuilder();
            var shaderData = ShaderUtil.GetShaderData(shader);
            sb.AppendFormat("// Subshader Count: {0}", shaderData.SubshaderCount).AppendLine();
            sb.AppendLine();

            for (int i = 0; i < shaderData.SubshaderCount; ++i)
            {
                var subShader = shaderData.GetSubshader(i);
                sb.AppendFormat("// Subshader {0}", i);
                if (shaderData.ActiveSubshaderIndex == i)
                {
                    sb.Append("-(Active)");
                }
                sb.Append(":").AppendLine();

                for (int j = 0; j < subShader.PassCount; ++j)
                {
                    var name = subShader.GetPass(j).Name;
                    sb.AppendFormat("  -Pass: \"{0}\"", String.IsNullOrEmpty(name) ? "<noname>" : name).AppendLine();
                }
                sb.AppendLine();
            }
            result = sb.ToString();
            return result;
        }

        public static List<String> GetShaderKeywords(Shader shader)
        {
            int[] types = null;
            String[] keywords = null;
            object[] args = new object[] { shader, new ShaderVariantCollection(), types, keywords };
            ShaderUtils.GetShaderVariantEntriesMethod.Invoke(null, args);
            keywords = args[3] as String[];
            var result = new List<String>();
            foreach (var keyword in keywords)
            {
                foreach (var t in keyword.Split(' '))
                {
                    if (!result.Contains(t))
                    {
                        result.Add(t);
                    }
                }
            }
            return result;
        }
    }

    /// <summary>
    /// ���弯��ȡ����
    /// </summary>
    public static class ShaderVariantCollectionHelper
    {

        public struct ShaderVariant
        {
            public Shader shader;
            public PassType passType;
            public string[] keywords;
            public string[] sorted_keywords;
            String desc;
            public ShaderVariant(Shader shader, PassType passType, params string[] keywords)
            {
                this.shader = shader;
                this.passType = passType;
                this.keywords = keywords;
                if (keywords != null)
                {
                    this.sorted_keywords = new string[keywords.Length];
                    Array.Copy(keywords, this.sorted_keywords, keywords.Length);
                    Array.Sort(this.sorted_keywords);
                }
                else
                {
                    this.sorted_keywords = null;
                }
                desc = null;
            }
            public override string ToString()
            {
                desc = desc ?? String.Format("{0}, [{1}] : '{2}'", shader.name, passType, sorted_keywords != null ? String.Join(" ", sorted_keywords) : "--");
                return desc;
            }
        }

        public class RawData : Dictionary<Shader, List<ShaderVariant>>
        {
        }

        public static RawData GetShaderVariantEntries(Shader shader)
        {
            if (shader == null)
            {
                return null;
            }
            int[] types = null;
            String[] keywords = null;
            object[] args = new object[] { shader, new ShaderVariantCollection(), types, keywords };
            ShaderUtils.GetShaderVariantEntriesMethod.Invoke(null, args);
            types = args[2] as int[];
            keywords = args[3] as String[];
            var result = new RawData();
            for (int i = 0; i < keywords.Length; ++i)
            {
                var keyword = keywords[i];
                var sv = new ShaderVariant(shader, (PassType)types[i], keyword.Split(' '));
                List<ShaderVariant> variants;
                if (!result.TryGetValue(shader, out variants))
                {
                    variants = new List<ShaderVariant>();
                    result.Add(shader, variants);
                }
                variants.Add(sv);
            }
            return result;
        }

        public static RawData ExtractData(ShaderVariantCollection svc)
        {
            var shaderVariants = new RawData();
            using (var so = new SerializedObject(svc))
            {
                var array = so.FindProperty("m_Shaders.Array");
                if (array != null && array.isArray)
                {
                    var arraySize = array.arraySize;
                    for (int i = 0; i < arraySize; ++i)
                    {
                        var shaderRef = array.FindPropertyRelative(String.Format("data[{0}].first", i));
                        var shaderShaderVariants = array.FindPropertyRelative(String.Format("data[{0}].second.variants", i));
                        if (shaderRef != null && shaderRef.propertyType == SerializedPropertyType.ObjectReference &&
                            shaderShaderVariants != null && shaderShaderVariants.isArray)
                        {
                            var shader = shaderRef.objectReferenceValue as Shader;
                            if (shader == null)
                            {
                                continue;
                            }
                            var shaderAssetPath = AssetDatabase.GetAssetPath(shader);
                            List<ShaderVariant> variants = null;
                            if (!shaderVariants.TryGetValue(shader, out variants))
                            {
                                variants = new List<ShaderVariant>();
                                shaderVariants.Add(shader, variants);
                            }
                            var variantCount = shaderShaderVariants.arraySize;
                            for (int j = 0; j < variantCount; ++j)
                            {
                                var prop_keywords = shaderShaderVariants.FindPropertyRelative(String.Format("Array.data[{0}].keywords", j));
                                var prop_passType = shaderShaderVariants.FindPropertyRelative(String.Format("Array.data[{0}].passType", j));
                                if (prop_keywords != null && prop_passType != null && prop_keywords.propertyType == SerializedPropertyType.String)
                                {
                                    var srcKeywords = prop_keywords.stringValue;
                                    var keywords = srcKeywords.Split(' ');
                                    var pathType = (UnityEngine.Rendering.PassType)prop_passType.intValue;
                                    variants.Add(new ShaderVariant(shader, pathType, keywords));
                                }
                            }
                        }
                    }
                }
            }
            return shaderVariants;
        }
    }

    /// <summary>
    /// ����Unity������룬�����
    /// </summary>
    public static class ShaderVariantCollectionExporter
    {

        static bool ShaderVariant_Equal(ref ShaderVariantCollection.ShaderVariant left, ref ShaderVariantCollection.ShaderVariant right)
        {
            if (left.shader != right.shader)
            {
                return false;
            }
            if (left.passType != right.passType)
            {
                return false;
            }
            if (object.ReferenceEquals(left.keywords, right.keywords))
            {
                return true;
            }
            var lcount = left.keywords != null ? left.keywords.Length : 0;
            var rcount = right.keywords != null ? right.keywords.Length : 0;
            if (lcount != rcount)
            {
                return false;
            }
            if (lcount > 0)
            {
                var llist = left.keywords.OrderBy(s => s);
                var rlist = right.keywords.OrderBy(s => s);
                return llist.SequenceEqual(rlist);
            }
            return false;
        }

        static bool ShaderVariant_Equal(ref ShaderVariantCollectionHelper.ShaderVariant left, ref ShaderVariantCollectionHelper.ShaderVariant right)
        {
            if (left.shader != right.shader)
            {
                return false;
            }
            if (left.passType != right.passType)
            {
                return false;
            }
            if (object.ReferenceEquals(left.keywords, right.keywords))
            {
                return true;
            }
            var lcount = left.keywords != null ? left.keywords.Length : 0;
            var rcount = right.keywords != null ? right.keywords.Length : 0;
            if (lcount != rcount)
            {
                return false;
            }
            if (lcount > 0)
            {
                var llist = left.sorted_keywords;
                var rlist = right.sorted_keywords;
                return llist.SequenceEqual(rlist);
            }
            return false;
        }

        static bool CreateDirectory(String dirName)
        {
            try
            {
                // first remove file name and extension;
                var ext = Path.GetExtension(dirName);
                String fileNameAndExt = Path.GetFileName(dirName);
                if (!String.IsNullOrEmpty(fileNameAndExt) && !string.IsNullOrEmpty(ext))
                {
                    dirName = dirName.Substring(0, dirName.Length - fileNameAndExt.Length);
                }
                var sb = new StringBuilder();
                var dirs = dirName.Split('/', '\\');
                if (dirs.Length > 0)
                {
                    if (dirName[0] == '/')
                    {
                        // abs path tag on Linux OS
                        dirs[0] = "/" + dirs[0];
                    }
                }
                for (int i = 0; i < dirs.Length; ++i)
                {
                    if (dirs[i].Length == 0)
                    {
                        continue;
                    }
                    if (sb.Length != 0)
                    {
                        sb.Append('/');
                    }
                    sb.Append(dirs[i]);
                    var cur = sb.ToString();
                    if (String.IsNullOrEmpty(cur))
                    {
                        continue;
                    }
                    if (!Directory.Exists(cur))
                    {
                        var info = Directory.CreateDirectory(cur);
                        if (null == info)
                        {
                            return false;
                        }
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
            }
            return false;
        }

        static void SaveShaderVariants(bool keepTempShaderVariants = true)
        {
            var tempPath = "Assets/__temp_VariantCollection.shadervariants";
            try
            {
                if (File.Exists(tempPath))
                {
                    AssetDatabase.DeleteAsset(tempPath);
                }
                ShaderUtils.SaveCurrentShaderVariantCollection(tempPath);
                AssetDatabase.Refresh();
                var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(tempPath);
                if (svc != null)
                {
                    // ��ǰshader������Ϣ���棬���Դ���ִ�й����ж���Դ���е��κ��޸�
                    var shaderCombinations = new Dictionary<Shader, ShaderParsedCombinations>();
                    var shaderVariants = ShaderVariantCollectionHelper.ExtractData(svc);
                    var extraVariants = new ShaderVariantCollectionHelper.RawData();
                    var depShaders = new List<Shader>();
                    foreach (var sv in shaderVariants)
                    {
                        depShaders.Clear();
                        var shaderPath = AssetDatabase.GetAssetPath(sv.Key);
                        var deps = AssetDatabase.GetDependencies(shaderPath).ToList();
                        // ����������Լ��Լ��ı��������Ϣ
                        for (int i = 0; i < deps.Count; ++i)
                        {
                            Shader depShader = null;
                            if (deps[i] == shaderPath)
                            {
                                depShader = sv.Key;
                            }
                            else
                            {
                                if (deps[i].EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
                                {
                                    depShader = AssetDatabase.LoadAssetAtPath<Shader>(deps[i]);
                                    if (depShader != null)
                                    {
                                        depShaders.Add(depShader);
                                    }
                                    else
                                    {
                                        Debug.LogErrorFormat("Load shader '{0}' failed.", deps[i]);
                                    }
                                }
                            }
                            if (depShader != null)
                            {
                                if (!shaderCombinations.ContainsKey(depShader))
                                {
                                    var info = ShaderUtils.ParseShaderCombinations(depShader, true);
                                    if (info != null)
                                    {
                                        shaderCombinations.Add(depShader, info);
                                    }
                                }
                            }
                        }
                        var shaderVariantsPath = System.IO.Path.ChangeExtension(shaderPath, ".shadervariants");
                        shaderVariantsPath = shaderVariantsPath.Insert(0, "Assets/ShaderVariants/");
                        if (CreateDirectory(shaderVariantsPath))
                        {
                            var va = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(shaderVariantsPath);
                            if (va == null)
                            {
                                va = new ShaderVariantCollection();
                                AssetDatabase.CreateAsset(va, shaderVariantsPath);
                                va = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(shaderVariantsPath);
                            }
                            if (va == null)
                            {
                                continue;
                            }
                            va.Clear();
                            var svInfos = sv.Value;
                            svInfos.Sort((l, r) => l.ToString().CompareTo(r.ToString()));
                            for (int i = 0; i < svInfos.Count; ++i)
                            {
                                var _sv = svInfos[i];
                                try
                                {
                                    var realSV = new ShaderVariantCollection.ShaderVariant(_sv.shader, _sv.passType, _sv.keywords);
                                    va.Add(realSV);
                                }
                                catch (System.ArgumentException e)
                                {
                                    // ��������ʧ�ܣ�˵���˱��岻���ڸ�Shader�����PassType
                                    Debug.LogWarningFormat("{0}, PassType = {1}, Keywords = '{2}'", _sv.shader.name, _sv.passType, String.Join(" ", _sv.keywords));
                                    Debug.LogWarningFormat(e.ToString());
                                }
                                if (depShaders.Count > 0)
                                {
                                    // ���˱����Ƿ��������������
                                    for (int j = 0; j < depShaders.Count; ++j)
                                    {
                                        ShaderParsedCombinations combs;
                                        var depShader = depShaders[j];
                                        if (shaderCombinations.TryGetValue(depShader, out combs))
                                        {
                                            var _keywords = _sv.keywords.ToList();
                                            // ɾ�������ڴ�Shader�Ĺؼ���
                                            var _combs = combs;
                                            var removeCount = _keywords.RemoveAll(e => !_combs.IsValidKeyword(e));
                                            var _newKeywords = removeCount > 0 ? _keywords.ToArray() : _sv.keywords;
                                            try
                                            {
                                                // ���Ե�ǰ����ı����Ƿ�Ϸ���������Ϸ����˹��캯�����׳�����������쳣�����������
                                                new ShaderVariantCollection.ShaderVariant(depShader, _sv.passType, _newKeywords);
                                                // ���浽����������Shader��
                                                List<ShaderVariantCollectionHelper.ShaderVariant> _depShaderVariants;
                                                if (!extraVariants.TryGetValue(depShader, out _depShaderVariants))
                                                {
                                                    _depShaderVariants = new List<ShaderVariantCollectionHelper.ShaderVariant>();
                                                    extraVariants.Add(depShader, _depShaderVariants);
                                                }
                                                var exists = false;
                                                var newShaderVariant = new ShaderVariantCollectionHelper.ShaderVariant(
                                                        depShader, _sv.passType, _newKeywords);
                                                for (int n = 0; n < _depShaderVariants.Count; ++n)
                                                {
                                                    var v = _depShaderVariants[n];
                                                    if (ShaderVariant_Equal(ref newShaderVariant, ref v))
                                                    {
                                                        exists = true;
                                                        break;
                                                    }
                                                }
                                                if (!exists)
                                                {
                                                    _depShaderVariants.Add(newShaderVariant);
                                                }
                                            }
                                            catch (System.ArgumentException e)
                                            {
                                                Debug.LogWarningFormat("{0}, PassType = {1}, Keywords = '{2}'", depShader.name, _sv.passType, String.Join(" ", _newKeywords));
                                                Debug.LogWarning(e.ToString());
                                            }
                                        }
                                    }
                                }
                            }
                            EditorUtility.SetDirty(va);
                        }
                    }
                    foreach (var extra in extraVariants)
                    {
                        var shaderPath = AssetDatabase.GetAssetPath(extra.Key);
                        var shaderVariantsPath = System.IO.Path.ChangeExtension(shaderPath, ".shadervariants");
                        shaderVariantsPath = shaderVariantsPath.Insert(0, "Assets/ShaderVariants/");
                        if (CreateDirectory(shaderVariantsPath))
                        {
                            var va = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(shaderVariantsPath);
                            if (va == null)
                            {
                                va = new ShaderVariantCollection();
                                AssetDatabase.CreateAsset(va, shaderVariantsPath);
                                va = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(shaderVariantsPath);
                            }
                            if (va == null)
                            {
                                continue;
                            }
                            va.Clear();
                            var svInfos = extra.Value;
                            svInfos.Sort((l, r) => l.ToString().CompareTo(r.ToString()));
                            for (int i = 0; i < svInfos.Count; ++i)
                            {
                                var _sv = svInfos[i];
                                try
                                {
                                    var realSV = new ShaderVariantCollection.ShaderVariant(_sv.shader, _sv.passType, _sv.keywords);
                                    va.Add(realSV);
                                }
                                catch (Exception e)
                                {
                                    // ��������ʧ�ܣ�˵���˱��岻���ڸ�Shader�����PassType
                                    Debug.LogWarningFormat("{0}, PassType = {1}, Keywords = '{2}'", _sv.shader.name, _sv.passType, String.Join(" ", _sv.keywords));
                                }
                            }
                        }
                    }
                }
            }
            finally
            {
                if (File.Exists(tempPath))
                {
                    if (!keepTempShaderVariants)
                    {
                        AssetDatabase.DeleteAsset(tempPath);
                    }
                }
                AssetDatabase.Refresh();
                AssetDatabase.SaveAssets();
            }
        }

    }

    /// <summary>
    /// ���ñ����ų�����
    /// </summary>
    class BuiltinShaderPreprocessor : IPreprocessShaders
    {
        static ShaderKeyword[] s_uselessKeywords;
        public int callbackOrder
        {
            get { return 0; }
        }
        static BuiltinShaderPreprocessor()
        {
            s_uselessKeywords = new ShaderKeyword[] {
                new ShaderKeyword( "DIRLIGHTMAP_COMBINED" ),
                new ShaderKeyword( "LIGHTMAP_SHADOW_MIXING" ),
                new ShaderKeyword( "SHADOWS_SCREEN" ),
            };
        }
        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            for (int i = data.Count - 1; i >= 0; --i)
            {
                for (int j = 0; j < s_uselessKeywords.Length; ++j)
                {
                    if (data[i].shaderKeywordSet.IsEnabled(s_uselessKeywords[j]))
                    {
                        data.RemoveAt(i);
                        break;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Shader�����Ż�������
    /// </summary>
    class ShaderPreprocessor : IPreprocessShaders
    {

        class ShaderVariantInfo
        {
            internal ShaderVariantCollection variantCollection;
            internal ShaderVariantCollectionHelper.RawData rawVariantCollection;
            internal ShaderParsedCombinations combinations;
        }

        struct PassNameWithType
        {
            internal String name;
            internal PassType type;
        }

        struct CheckKeywordRecord
        {
            /// <summary>
            /// ����ܵ������IList<ShaderCompilerData>������
            /// </summary>
            internal int srcDataIndex;
            /// <summary>
            /// �û��ռ���һ��shader�����б����е�����һ������id������+1
            /// </summary>
            internal int rawVariantsId;
            /// <summary>
            /// �����Ƿ���ڲ�����������
            /// </summary>
            internal bool exists;
        }

        static Dictionary<String, ShaderVariantInfo> s_ShaderVariantCollections = new Dictionary<String, ShaderVariantInfo>();

        public int callbackOrder
        {
            get { return 1; }
        }

        /// <summary>
        /// ��ǰ�εı������뱸�ݣ������ִ��������ȫ�ָ����������κεı���ü�
        /// </summary>
        static List<ShaderCompilerData> backupShaderCompilerData = new List<ShaderCompilerData>();

        static Shader lastProcessShader = null;
        static ShaderVariantInfo lastProcessShaderVariantInfo = null;
        /// <summary>
        /// ���ڼ�¼һ��shader��Ӧ����Ŀ���ռ����ı��弯�Ƿ񶼾������룬���򱨴��¼ȱʧ
        /// </summary>
        static List<CheckKeywordRecord> checkedKeywords = new List<CheckKeywordRecord>();
        static List<PassNameWithType> compiledShaderPasses = new List<PassNameWithType>();

        public static void Reset()
        {
            Debug.Assert(false, "����shader���ڴ�AssetBundle�����д����ģ�����������Ҫ������������ӿڶԽӵ����Լ���Bundle����ϵͳ��ȥ");

            // ���������AssetBundle�������룬��Ҫ�������Hook
            /*
            try {
                if ( OnBeginBuildAssetBundles != null ) {
                    try {
                        OnBeginBuildAssetBundles( outpath, _abBuilds );
                    } catch ( Exception e ) {
                        Debug.LogException( e );
                    }
                }
                manifest = BuildPipeline.BuildAssetBundles( outpath, _abBuilds, buildOpts, m_buildTarget );
            } finally {
                if ( OnEndBuildAssetBundles != null ) {
                    OnEndBuildAssetBundles( outpath, _abBuilds, manifest );
                    try {
                    } catch ( Exception e ) {
                        Debug.LogException( e );
                    }
                }
            }
            */
        }

        static void OnBeginBuildAssetBundles(String outpath, AssetBundleBuild[] builds)
        {
        }

        static void OnEndBuildAssetBundles(String outpath, AssetBundleBuild[] builds, AssetBundleManifest manifest)
        {
            CheckLastShaderVariantCompleteness();
            s_ShaderVariantCollections.Clear();
            lastProcessShader = null;
            lastProcessShaderVariantInfo = null;
            checkedKeywords.Clear();
            backupShaderCompilerData.Clear();
            compiledShaderPasses.Clear();
        }

        /// <summary>
        /// ͬһ��Shader���ܻ��ξ�������ܵ�����Pass, SubShader��ԭ�򣩣����Ե����α�����ϴμ�¼��Shader��һ���ˣ�˵����һ��Shader�Ѿ��������ˣ�������һ�������Լ��
        /// </summary>
        static void CheckLastShaderVariantCompleteness()
        {
            if (lastProcessShader != null && lastProcessShaderVariantInfo != null)
            {
                var keywordsMatchedCount = 0;
                var totalCount = 0;
                List<ShaderVariantCollectionHelper.ShaderVariant> lastRawVariants;
                if (lastProcessShaderVariantInfo.rawVariantCollection.TryGetValue(lastProcessShader, out lastRawVariants))
                {
                    for (int i = 0; i < checkedKeywords.Count; ++i)
                    {
                        if (checkedKeywords[i].rawVariantsId <= 0)
                        {
                            // δ����
                            continue;
                        }
                        var variant = lastRawVariants[checkedKeywords[i].rawVariantsId - 1];
                        var _variant = variant;
                        if (compiledShaderPasses.FindIndex(e => e.type == _variant.passType) < 0)
                        {
                            // ��passType����û�о����κα��룬˵��shader�ı�������UsePass�����ģ��������
                            continue;
                        }
                        ++totalCount;
                        if (checkedKeywords[i].exists)
                        {
                            ++keywordsMatchedCount;
                        }
                        else
                        {
                            Debug.LogErrorFormat("Shader '{0}.[{1}]' variants missing: '{2}'", lastProcessShader.name, lastRawVariants[i].passType, String.Join(" ", lastRawVariants[i].sorted_keywords));
                        }
                    }
                    if (keywordsMatchedCount != totalCount)
                    {
                        Debug.LogErrorFormat("Shader '{0}' variants missing.", lastProcessShader.name);
                    }
                }
            }
        }

        public void OnProcessShader(Shader shader, ShaderSnippetData snippet, IList<ShaderCompilerData> data)
        {
            // ��������ȫ���޳���Ҫ������pass���� 
            if (snippet.passType == PassType.ForwardAdd ||
                snippet.passType == PassType.LightPrePassBase ||
                snippet.passType == PassType.LightPrePassFinal ||
                snippet.passType == PassType.Deferred ||
                snippet.passType == PassType.ScriptableRenderPipeline ||
                snippet.passType == PassType.ScriptableRenderPipelineDefaultUnlit ||
                snippet.passType == PassType.Meta ||
                snippet.passType == PassType.MotionVectors)
            {
                data.Clear();
                return;
            }
            var shaderPath = AssetDatabase.GetAssetPath(shader);
            if (Utils.IsUnityDefaultResource(shaderPath))
            {
                // �ų�ϵͳ�Դ�shader�����޳�
                return;
            }
            backupShaderCompilerData.Clear();
            backupShaderCompilerData.AddRange(data);
            ShaderVariantInfo svcInfo = null;
            if (!s_ShaderVariantCollections.TryGetValue(shaderPath, out svcInfo))
            {
                var shaderVariantsPath = System.IO.Path.ChangeExtension(shaderPath, ".shadervariants");
                shaderVariantsPath = shaderVariantsPath.Insert(0, "Assets/ShaderVariants/");
                var svc = AssetDatabase.LoadAssetAtPath<ShaderVariantCollection>(shaderVariantsPath);
                if (svc != null)
                {
                    var comb = ShaderUtils.ParseShaderCombinations(shader, true);
                    if (comb != null && comb.snippets != null && comb.snippets.Count > 0)
                    {
                        svcInfo = new ShaderVariantInfo();
                        svcInfo.variantCollection = svc;
                        svcInfo.rawVariantCollection = ShaderVariantCollectionHelper.ExtractData(svc);
                        svcInfo.combinations = comb;
                        s_ShaderVariantCollections.Add(shaderPath, svcInfo);
                    }
                }
            }
            if (svcInfo != null)
            {
                List<ShaderVariantCollectionHelper.ShaderVariant> rawVariants;
                if (!svcInfo.rawVariantCollection.TryGetValue(shader, out rawVariants))
                {
                    return;
                }
                HashSet<String> multi_compiles = null;
                var shaderData = ShaderUtil.GetShaderData(shader);
                for (int k = 0; k < shaderData.SubshaderCount; ++k)
                {
                    var subShader = shaderData.GetSubshader(k);
                    for (int i = 0; i < subShader.PassCount; ++i)
                    {
                        var pass = subShader.GetPass(i);
                        if (String.IsNullOrEmpty(pass.Name) || pass.Name == snippet.passName)
                        {
                            multi_compiles = multi_compiles ?? new HashSet<String>();
                            var snippets = svcInfo.combinations.snippets;
                            for (int j = 0; j < snippets.Count; ++j)
                            {
                                var snippetData = snippets[j];
                                if (snippetData.multi_compiles != null)
                                {
                                    Array.ForEach(snippetData.multi_compiles, keyword => multi_compiles.Add(keyword));
                                }
                            }
                        }
                    }
                }
                var removeCount = 0;
                StringBuilder infoSB = null;
                var fullKeywords = new List<String>();
                var fullKeywords2 = new List<String>();
                var keywordList = new HashSet<String>();
                var emptyStringArray = new String[] { };
                if (lastProcessShader != shader)
                {
                    CheckLastShaderVariantCompleteness();
                    checkedKeywords.Clear();
                    lastProcessShader = shader;
                    lastProcessShaderVariantInfo = svcInfo;
                    compiledShaderPasses.Clear();
                }
                if (compiledShaderPasses.FindIndex(snippet, (e, _snippet) => e.name == _snippet.passName && e.type == _snippet.passType) < 0)
                {
                    compiledShaderPasses.Add(new PassNameWithType { name = snippet.passName, type = snippet.passType });
                }
                checkedKeywords.Resize(rawVariants.Count);
                for (int i = 0; i < data.Count; ++i)
                {
                    fullKeywords.Clear();
                    fullKeywords2.Clear();
                    var _keywords = data[i].shaderKeywordSet.GetShaderKeywords();
                    for (int j = 0; j < _keywords.Length; ++j)
                    {
                        var name = _keywords[j].GetKeywordName();
                        if (multi_compiles != null && multi_compiles.Contains(name))
                        {
                            continue;
                        }
                        fullKeywords.Add(name);
                    }
                    fullKeywords.Sort();
                    for (int j = 0; j < rawVariants.Count; ++j)
                    {
                        if (rawVariants[j].passType != snippet.passType)
                        {
                            continue;
                        }
                        if (checkedKeywords[j].exists)
                        {
                            continue;
                        }
                        var usedKeywords = rawVariants[j].sorted_keywords ?? emptyStringArray;
                        for (int k = 0; k < usedKeywords.Length; ++k)
                        {
                            if (multi_compiles != null && multi_compiles.Contains(usedKeywords[k]))
                            {
                                continue;
                            }
                            if (!String.IsNullOrEmpty(usedKeywords[k]))
                            {
                                fullKeywords2.Add(usedKeywords[k]);
                            }
                        }
                        if (fullKeywords.SequenceEqual(fullKeywords2))
                        {
                            // �����shader��������������ռ��ı��弯��
                            checkedKeywords[j] = new CheckKeywordRecord { srcDataIndex = i, rawVariantsId = j + 1, exists = true };
                            break;
                        }
                    }
                }
                // ����Դ����գ��Ѿ���������backupShaderCompilerData
                data.Clear();
                backupShaderCompilerData.RemoveAll(
                    _data => {
                        fullKeywords.Clear();
                        var _keywords = _data.shaderKeywordSet.GetShaderKeywords();
                        // ֻ�޳��йؼ��ֵ����Σ����ٴ��븴�Ӷ�
                        // ʵ���ϣ��޹ؼ��ֵı���Ҳ���ܱ��������ã�����������޳���������������̫����븺��
                        if (_keywords.Length > 0)
                        {
                            keywordList.Clear();
                            for (int j = 0; j < _keywords.Length; ++j)
                            {
                                var name = _keywords[j].GetKeywordName();
                                fullKeywords.Add(name);
                                // ����е�ǰ�ؼ�������multi_compiles��ע���ų�
                                if (multi_compiles != null && multi_compiles.Contains(name))
                                {
                                    // �ų�multi_compiles����꣬��Щ�Ǳ���ʹ�õģ������޳�
                                    continue;
                                }
                                keywordList.Add(name);
                            }
                            if (keywordList.Count > 0)
                            {
                                try
                                {
                                    var matched = false;
                                    // �������д���Ŀ���Ѽ����ı���
                                    for (int n = 0; n < rawVariants.Count; ++n)
                                    {
                                        var variant = rawVariants[n];
                                        if (keywordList.Count > variant.keywords.Length)
                                        {
                                            // keywordList �Ѿ�ȥ����multi_compiles����variant.keywords���ܺ���
                                            // �������Ĳ��������ǰ��֪��������ƥ�����
                                            continue;
                                        }
                                        var matchCount = -1;
                                        var mismatchCount = 0;
                                        var skipCount = 0;
                                        if (variant.shader == shader && variant.passType == snippet.passType)
                                        {
                                            matchCount = 0;
                                            // ����ƥ��ı���ʱ����Ҫ�ų�multi_compiles�ؼ���
                                            for (var m = 0; m < variant.keywords.Length; ++m)
                                            {
                                                var keyword = variant.keywords[m];
                                                if (multi_compiles == null || !multi_compiles.Contains(keyword))
                                                {
                                                    // ����multi_compiles��keyword
                                                    if (keywordList.Contains(keyword))
                                                    {
                                                        ++matchCount;
                                                    }
                                                    else
                                                    {
                                                        ++mismatchCount;
                                                        break;
                                                    }
                                                }
                                                else
                                                {
                                                    ++skipCount;
                                                }
                                            }
                                        }
                                        if (matchCount >= 0 && mismatchCount == 0 && matchCount == keywordList.Count && matchCount + skipCount == variant.keywords.Length)
                                        {
                                            matched = true;
                                            break;
                                        }
                                    }
                                    if (!matched)
                                    {
                                        ++removeCount;
                                        // ��ɾ�����ȴ���������˻������û���
                                        data.Add(_data);
                                        infoSB = infoSB ?? new StringBuilder();
                                        var _fullKeywords = String.Join(" ", fullKeywords.ToArray());
                                        infoSB.Append(_fullKeywords).AppendLine();
                                        var info = String.Format("{0}\nRemove Shader ShaderVariant: {1}", shader.name, _fullKeywords);
                                        Debug.Log(info);
                                        return true;
                                    }
                                }
                                catch (Exception e)
                                {
                                    Debug.LogException(e);
                                }
                            }
                        }
                        return false;
                    }
                );
                data.Clear();
                backupShaderCompilerData.ForEach(e => data.Add(e));
                backupShaderCompilerData.Clear();
                if (removeCount > 0 && infoSB != null)
                {
                    var info = String.Format("{0}\nRemove ShaderVariant Count: {1}\n{2}", shader.name, removeCount, infoSB.ToString());
                    Debug.Log(info);
                }
            }
        }
    }
}
//EOF