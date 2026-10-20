
using System;
using System.Data;
using System.IO;
using System.Text;
using ExcelDataReader;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.ShaderGraph;
using UnityEngine;

namespace Core.Excel {
    public class PathData
    {
    }

    public static class ExcelHandle
    {
        private static string _excelLoadDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "UnityExcel",
            "excelDirectory.txt"
        );

        private static string _configDir = $"{Application.dataPath}/Res/Scripts/Generate/Configs";
        private static string _configLoadDir = $"{Application.dataPath}/Res/Scripts/Generate/ConfigLoader";
        private static string _bytesSaveDir = $"{Application.streamingAssetsPath}/Configs";

        [MenuItem("Tools/Excel工具/获取Excel目录")]
        private static void SetExcelDir()
        {
            string initDir;
            initDir = GetExcelDir();
            var selectDir = EditorUtility.OpenFolderPanel("获取Excel路径目录", initDir, "");
            try
            {
                var dir = Path.GetDirectoryName(_excelLoadDir);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
                File.WriteAllText(_excelLoadDir, selectDir, Encoding.UTF8);
                EditorUtility.DisplayDialog("成功", $"成功注册读取Excel目录，路径为{selectDir}", "确认");
            }
            catch (Exception e)
            {
                Debug.LogError($"error: 没有正确保存Excel目录{e.Message}");
                EditorUtility.DisplayDialog("错误", "没有成功注册对应Excel目录", "确认");
            }
            
        }

        [MenuItem("Tools/Excel工具/处理所有的Excel文件")]
        private static void ReadAllExcels()
        {
            var excelsDir = GetExcelDir();
            foreach (var path in new string[] { _configDir, _configLoadDir, _bytesSaveDir })
            {
                if(Directory.Exists(path)) continue;
                Directory.CreateDirectory(path);
            }

            var excelFiles = Directory.GetFiles(excelsDir, "*.xlsx");
            foreach (var excelFile in excelFiles)
            {
                using var stream = File.Open(excelFile, FileMode.Open, FileAccess.Read);
                using var reader = ExcelReaderFactory.CreateReader(stream);
                var dataSet = reader.AsDataSet();
                foreach (DataTable table in dataSet.Tables)
                {
                    if(table == null) continue;
                    if(string.IsNullOrEmpty(table.TableName) || !table.TableName.EndsWith("Config")) continue;
                    string tableName = table.TableName;

                    string className = tableName;

                    string configCode = GetExcelConfig(table, className);
                    File.WriteAllText(Path.Combine(_configDir, $"{className}.cs"), configCode, Encoding.UTF8);

                    string loaderCode = GetExcelConfigLoader(table, className);
                    File.WriteAllText(Path.Combine(_configLoadDir, $"{className}Loader.cs"), loaderCode, Encoding.UTF8);

                    string serializableCode = GetExcelConfigSerializable(table, className);
                    File.WriteAllText(Path.Combine(_configLoadDir, $"{className}Serializable.cs"), serializableCode, Encoding.UTF8);
                    
                    WriteBytesFile(table);
                }
            }
        }
        /// <summary>
        /// 自动生成数据类脚本
        /// </summary>
        /// <param name="table"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        private static string GetExcelConfig(DataTable table, string className)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("using System;");
            stringBuilder.AppendLine("using System.Collections.Generic;");
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("namespace Core.Excel {");
            stringBuilder.AppendLine("    // 自动生成信息，请勿手动修改");
            stringBuilder.AppendLine($"    // 日期: {DateTime.Now}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("    [Serializable]");
            stringBuilder.AppendLine($"    public class {className}");
            stringBuilder.AppendLine("    {");
            var descriptiveNames = table.Rows[0];//第一行固定的描述名
            var variableNames = table.Rows[1];//第二行固定的变量类型名
            for (int col = 0; col < table.Columns.Count; col++)
            {
                string curVariable = variableNames[col].ToString();
                string curDesName = descriptiveNames[col].ToString().Trim();
                if(string.IsNullOrEmpty(curDesName)) continue;
                string typeName = ToPascalCase(curDesName);
                string type = CalculateCSharpType(curVariable, table.Rows[2][col].ToString());
                string name = !string.IsNullOrEmpty(typeName) ? typeName : $"{table.TableName}_{col}";
                stringBuilder.AppendLine($"        public {type} {name} {{ get; set; }}");
            }
            stringBuilder.AppendLine("");
            var key = GetKeyExpression(table);
            stringBuilder.AppendLine($"        public int GetKey() => {key};");
            stringBuilder.AppendLine("    }");
            stringBuilder.AppendLine("");
            stringBuilder.AppendLine("}");
            return stringBuilder.ToString();
        }
        /// <summary>
        /// 自动生成数据类加载器脚本
        /// </summary>
        /// <param name="table"></param>
        /// <param name="className"></param>
        private static string GetExcelConfigLoader(DataTable table, string className)
        {
            var stringBuilder = new StringBuilder();
            string fileType = className.Replace("Config", "");
            stringBuilder.AppendLine("using System.Collections.Generic;");
            stringBuilder.AppendLine("using System.IO;");
            stringBuilder.AppendLine("using System.Linq;");
            stringBuilder.AppendLine("using UnityEngine;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("namespace Core.Excel");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("    // 自动生成信息，请勿手动修改");
            stringBuilder.AppendLine($"    // 日期: {DateTime.Now}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"    public static class {className}Loader");
            stringBuilder.AppendLine("    {");
            stringBuilder.AppendLine($"        private static readonly string bytesDir = Application.streamingAssetsPath + \"/Configs/{fileType}.bytes\";");
            stringBuilder.AppendLine($"        private static Dictionary<int, {className}> cache;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"        public static Dictionary<int, {className}> LoadAll()");
            stringBuilder.AppendLine("        {");
            stringBuilder.AppendLine("            if (cache != null) return cache;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"            cache = new Dictionary<int, {className}>();");
            stringBuilder.AppendLine("            var bytes = File.ReadAllBytes(bytesDir);");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("            using (var stream = new MemoryStream(bytes))");
            stringBuilder.AppendLine("            using (var reader = new BinaryReader(stream))");
            stringBuilder.AppendLine("            {");
            stringBuilder.AppendLine("                var count = reader.ReadInt32();");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("                for (int cur = 0; cur < count; cur++)");
            stringBuilder.AppendLine("                {");
            stringBuilder.AppendLine($"                    var serialize = new {className}Serializable();");
            stringBuilder.AppendLine("                    var config = serialize.Reader(reader);");
            stringBuilder.AppendLine("                    if (config == null) continue;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("                    var key = config.GetKey();");
            stringBuilder.AppendLine("                    cache[key] = config;");
            stringBuilder.AppendLine("                }");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("                return cache;");
            stringBuilder.AppendLine("            }");
            stringBuilder.AppendLine("        }");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"        public static List<{className}> GetAll()");
            stringBuilder.AppendLine("        {");
            stringBuilder.AppendLine("            if (cache == null) LoadAll();");
            stringBuilder.AppendLine("            return cache.Values.ToList();");
            stringBuilder.AppendLine("        }");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"        public static {className} Get(int key)");
            stringBuilder.AppendLine("        {");
            stringBuilder.AppendLine("            if (cache == null) LoadAll();");
            stringBuilder.AppendLine("            bool canFind = cache.TryGetValue(key, out var value);");
            stringBuilder.AppendLine("            return canFind ? value : null;");
            stringBuilder.AppendLine("        }");
            stringBuilder.AppendLine("    }");
            stringBuilder.AppendLine("}");

            return stringBuilder.ToString();
        }
        /// <summary>
        /// 自动生成数据类翻译器脚本
        /// </summary>
        /// <param name="table"></param>
        /// <param name="className"></param>
        /// <returns></returns>
        private static string GetExcelConfigSerializable(DataTable table, string className)
        {
            if (table == null || string.IsNullOrEmpty(className))
            {
                Debug.LogError("ConfigSerializable is error");
                return "";
            }
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("using System.Collections.Generic;");
            stringBuilder.AppendLine("using System.IO;");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine("namespace Core.Excel");
            stringBuilder.AppendLine("{");
            stringBuilder.AppendLine("    // 自动生成信息，请勿手动修改");
            stringBuilder.AppendLine($"    // 日期: {DateTime.Now}");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"    public class {className}Serializable");
            stringBuilder.AppendLine("    {");
            stringBuilder.AppendLine($"        public void Writer(BinaryWriter writer, {className} writerConfig)");
            stringBuilder.AppendLine("        {");

            for (int col = 0; col < table.Columns.Count; col++)
            {
                var curColName = table.Rows[0][col].ToString().Trim();
                var curColType = table.Rows[1][col].ToString();

                if (string.IsNullOrEmpty(curColName)) continue;
                var typeName = ToPascalCase(curColName);
                var flag = CalculateCSharpType(
                    curColType,
                    !string.IsNullOrEmpty(table.Rows[2][col].ToString())
                        ? table.Rows[2][col].ToString()
                        : ""
                );

                if (flag.StartsWith("List<"))
                {
                    stringBuilder.AppendLine($"            writer.Write(writerConfig.{typeName}.Count);");
                    stringBuilder.AppendLine($"            for (int i_{col} = 0; i_{col} < writerConfig.{typeName}.Count; i_{col}++)");
                    stringBuilder.AppendLine("            {");
                    stringBuilder.AppendLine($"                writer.Write(writerConfig.{typeName}[i_{col}]);");
                    stringBuilder.AppendLine("            }");
                }
                else
                    stringBuilder.AppendLine($"            writer.Write(writerConfig.{typeName});");
            }
            stringBuilder.AppendLine("        }");
            stringBuilder.AppendLine();
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"        public {className} Reader(BinaryReader reader)");
            stringBuilder.AppendLine("        {");
            stringBuilder.AppendLine($"            var obj = new {className}();");

            for (int col = 0; col < table.Columns.Count; col++)
            {
                var curColName = table.Rows[0][col].ToString().Trim();
                var curColType = table.Rows[1][col].ToString();

                if (string.IsNullOrEmpty(curColName)) continue;
                var typeName = ToPascalCase(curColName);
                var flag = CalculateCSharpType(
                    curColType,
                    !string.IsNullOrEmpty(table.Rows[2][col].ToString())
                        ? table.Rows[2][col].ToString()
                        : ""
                );
                if (flag == "int")
                    stringBuilder.AppendLine($"            obj.{typeName} = reader.ReadInt32();");
                else if (flag == "float")
                    stringBuilder.AppendLine($"            obj.{typeName} = reader.ReadSingle();");
                else if (flag == "bool")
                    stringBuilder.AppendLine($"            obj.{typeName} = reader.ReadBoolean();");
                else if (flag.StartsWith("List<"))
                {
                    var flagHasList = flag.Substring(5, flag.Length - 6);
                    stringBuilder.AppendLine($"            var len_{col} = reader.ReadInt32();");
                    stringBuilder.AppendLine($"            obj.{typeName} = new List<{flagHasList}>();");
                    stringBuilder.AppendLine($"            for (int i_{col} = 0; i_{col} < len_{col}; i_{col}++)");
                    stringBuilder.AppendLine("            {");
                    if (flagHasList == "int")
                        stringBuilder.AppendLine($"                obj.{typeName}.Add(reader.ReadInt32());");
                    else if (flagHasList == "float")
                        stringBuilder.AppendLine($"                obj.{typeName}.Add(reader.ReadSingle());");
                    else if (flagHasList == "bool")
                        stringBuilder.AppendLine($"                obj.{typeName}.Add(reader.ReadBoolean());");
                    stringBuilder.AppendLine("            }");
                }
                else
                    stringBuilder.AppendLine($"            obj.{typeName} = reader.ReadString();");
                
            }

            stringBuilder.AppendLine();
            stringBuilder.AppendLine("            return obj;");
            stringBuilder.AppendLine("        }");
            stringBuilder.AppendLine("    }");
            stringBuilder.AppendLine("}");

            return stringBuilder.ToString();
        }
        
        /// <summary>
        /// 自动解析变量类型
        /// </summary>
        /// <param name="curVariable">当前传入字符串</param>
        /// <param name="inferVariable">Excel当前列对应的描述文本</param>
        /// <returns></returns>
        private static string CalculateCSharpType(string curVariable, string inferVariable)
        {
            if (!string.IsNullOrEmpty(curVariable))
            {//从传入字符串中转换对应变量类型
                if (curVariable.Contains("[]"))
                {
                    var us = curVariable.Substring(0, curVariable.Length - 2);
                    if (us.Contains("int")) return "List<int>";
                    if (us.Contains("bool")) return "List<bool>";
                    if (us.Contains("float")) return "List<float>";
                }

                if (curVariable.Contains("int")) return "int";
                if (curVariable.Contains("bool")) return "bool";
                if (curVariable.Contains("float")) return "float";
                return "string";
            }

            if (!string.IsNullOrEmpty(inferVariable))
            {//从描述文本推断当前变量类型
                bool isList = false;
                if (inferVariable.Contains("[]")) isList = true;
                bool isInt = true, isBool = true, isFloat = true;
                if (!inferVariable.Contains("int")) isInt = false;
                if (!inferVariable.Contains("bool")) isBool = false;
                if (!inferVariable.Contains("float")) isFloat = false;

                if (isInt) return isList ? "List<int>" : "int";
                if (isBool) return isList ? "List<bool>" : "bool";
                if (isFloat) return isList ? "List<float>" : "float";
                
                return isList ? "List<string>" : "string";
            }

            return "string";
        }
        private static void WriteBytesFile(DataTable table)
        {
            if(table == null) return;

            string sheetName = table.TableName;
            var us = sheetName.Replace("Config", "");
            string bytesName = $"{us}.bytes";
            
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                int startReader = 3;
                int needReader = table.Rows.Count - startReader;
                writer.Write(needReader);

                for (int currentRow = startReader; currentRow < table.Rows.Count; currentRow++)
                {
                    for (int currentCol = 0; currentCol < table.Columns.Count; currentCol++)
                    {
                        string value = table.Rows[currentRow][currentCol].ToString();
                        string type = table.Rows[1][currentCol].ToString();
                        
                        ByteWriterValue(writer, value, type);
                    }
                }
            File.WriteAllBytes
            (
                Path.Combine(_bytesSaveDir, bytesName), 
                stream.ToArray()
                );
            };
        }
        private static string ToPascalCase(string input)
        {
            if (string.IsNullOrEmpty(input)) return input;
            return char.ToUpper(input[0]) + input.Substring(1);
        }
        private static void ByteWriterValue(BinaryWriter writer, string valueString, string typeString)
        {
            if(writer == null) return;
            if (string.IsNullOrEmpty(valueString) || string.IsNullOrEmpty(typeString))
            {
                Debug.LogError("error ByteWriterValue");
                return;
            }

            string type = typeString.Trim().ToLowerInvariant();
            if (type.EndsWith("[]"))
            {//数组类型
                var arrString = type.Substring(0, type.Length - 2).Trim();
                var values = string.IsNullOrEmpty(valueString) 
                    ? Array.Empty<string>()
                    : valueString.Trim().Split(new char[]{','}, StringSplitOptions.RemoveEmptyEntries);
                writer.Write(values.Length);
                foreach (var value in values)
                {
                    if (arrString.Contains("int"))
                    {
                        int.TryParse(value, out int v);
                        writer.Write(v);
                    }
                    else if (arrString.Contains("bool") || arrString.Contains("boolean"))
                    {
                        bool.TryParse(value, out bool v);
                        writer.Write(v);
                    }
                    else if (arrString.Contains("float") || arrString.Contains("single"))
                    {
                        float.TryParse(value, out float v);
                        writer.Write(v);
                    }
                    else
                    {
                        writer.Write(value);
                    }
                }
                return;    
            }

            if (type == "int" || type == "int32")
            {
                int.TryParse(valueString, out int v);
                writer.Write(v);
            }
            else if (type == "bool" || type == "boolean")
            {
                bool.TryParse(valueString, out bool v);
                writer.Write(v);
            }
            else if (type == "float" || type == "single")
            {
                float.TryParse(valueString, out float v);
                writer.Write(v);
            }
            else
            {
                writer.Write(valueString ?? string.Empty);
            }
        }

        private static string GetExcelDir()
        {
            try
            {
                string envi = Environment.GetEnvironmentVariable("EXCELDIRECTORY");
                if (!string.IsNullOrEmpty(envi) && Directory.Exists(envi))
                    return envi;
                var dir = File.ReadAllText(_excelLoadDir, Encoding.UTF8).Trim();
                if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir))
                    return dir;
            }
            catch (Exception e)
            {
                Debug.LogError($"error: 没有正确读取Excel文件目录值 {e.Message}");
            }

            var defau = Path.Combine(
                Application.dataPath,
                "Excels"
                );
            try
            {
                if (!string.IsNullOrEmpty(defau) && !Directory.Exists(defau)) Directory.CreateDirectory(defau);
                return defau;
            }
            catch (Exception e)
            {
                Debug.LogError($"error: 没有正确读取默认Excel文件目录值 {e.Message}");
            }

            return defau;
        }

        private static string GetKeyExpression(DataTable table)
        {
            return ToPascalCase(table.Rows[0][0].ToString());
        }
    }
}