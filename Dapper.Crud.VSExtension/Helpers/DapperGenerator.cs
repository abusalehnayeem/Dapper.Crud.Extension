﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Dapper.Crud.VSExtension.Helpers
{
    public static class DapperGenerator
    {
        public static string SelectJoin(List<string> models, List<IList<PropertyInfo>> properties, bool generateMethod, bool generateClass)
        {
            var space = "";

            if (generateMethod)
                space = "    ";
            if (generateClass)
                space += "    ";

            var sb = new StringBuilder();

            var arrProp = new object[properties.Count];
            for (int i = 0; i < properties.Count; i++)
            {
                arrProp[i] = properties[i];
            }

            var prop = GenerateProperties((IList<PropertyInfo>)arrProp[0], false);

            sb.AppendLine(space + "// Select");
            sb.AppendLine(space + $"List<{models[0]}> ret;");
            sb.AppendLine(space + "using (var db = new SqlConnection(connstring))");
            sb.AppendLine(space + "{");
            sb.AppendLine(space + $"    const string sql = @\"SELECT {prop} FROM {models[0]} D0");

            for (int i = 1; i < arrProp.Length; i++)
            {
                sb.AppendLine(space + $"    INNER JOIN {models[i]} D{i} ON D0.AddressId = D{i}.Id\";");
            }

            sb.AppendLine("");
            sb.AppendLine(space + $"    ret = db.Query<{models[0]}>(sql, commandType: CommandType.Text).ToList();");
            sb.AppendLine(space + "}");
            sb.AppendLine(space + "return ret;");

            return sb.ToString();
        }

        public static string Select(string model, IList<PropertyInfo> properties, bool generateMethod, bool generateClass)
        {
            model = FixClassName(model);
            var space = "";

            if (generateMethod)
                space = "    ";
            if (generateClass)
                space += "    ";

            var sb = new StringBuilder();
            var prop = GenerateProperties(properties, false);

            sb.AppendLine(space + "// Select");
            sb.AppendLine(space + $"List<{model}> ret;");
            sb.AppendLine(space + "using (var db = new SqlConnection(connstring))");
            sb.AppendLine(space + "{");
            sb.AppendLine(space + $"    const string sql = @\"SELECT {prop} FROM {model}\";");
            sb.AppendLine("");
            sb.AppendLine(space + $"    ret = db.Query<{model}>(sql, commandType: CommandType.Text).ToList();");
            sb.AppendLine(space + "}");
            sb.AppendLine(space + "return ret;");

            return sb.ToString();
        }

        private static string FixClassName(string model)
        {
            if (model.Contains("\\"))
            {
                var str = model.Split('\\');
                model = str.Last(); // last item of the array
            }
            return model;
        }

        public static string Insert(string model, IList<PropertyInfo> properties, bool generateMethod, bool generateClass, bool autoIncrement)
        {
            model = FixClassName(model);
            var space = "";

            if (generateMethod)
                space = "    ";
            if (generateClass)
                space += "    ";

            if (autoIncrement)
                if (properties[0].Name.ToLower().Contains("id"))
                    properties.RemoveAt(0);

            var sb = new StringBuilder();

            var prop = GenerateProperties(properties, false);
            var propAt = GenerateProperties(properties, true);

            sb.AppendLine(space + "// Insert");
            sb.AppendLine(space + "using (var db = new SqlConnection(connstring))");
            sb.AppendLine(space + "{");
            sb.AppendLine(space + $"    const string sql = @\"INSERT INTO {model} ({prop}) VALUES ({propAt})\";");
            sb.AppendLine("");
            sb.AppendLine(space + $"    db.Execute(sql, new {{ {GenerateParameters(properties, model)} }}, commandType: CommandType.Text);");
            sb.AppendLine(space + "}");

            return sb.ToString();
        }

        public static string Update(string model, IList<PropertyInfo> properties, bool generateMethod, bool generateClass, bool autoIncrement)
        {
            model = FixClassName(model);
            var space = "";

            if (generateMethod)
                space = "    ";
            if (generateClass)
                space += "    ";

            // Creating a Id to use on Where before removed (in case of autoincrement)
            var propId = properties[0];

            if (autoIncrement)
                if (properties[0].Name.ToLower().Contains("id"))
                    properties.RemoveAt(0);

            var sb = new StringBuilder();

            sb.AppendLine(space + "// Update");
            sb.AppendLine(space + "using (var db = new SqlConnection(connstring))");
            sb.AppendLine(space + "{");
            sb.AppendLine(space + $"    const string sql = @\"UPDATE {model} SET {GenerateUpdateValues(properties)} WHERE {propId.Name} = @{propId.Name}\";");
            sb.AppendLine("");

            if (autoIncrement)
                sb.AppendLine(space + $"    db.Execute(sql, new {{ {propId.Name} = {model.ToLower()}.{propId.Name}, {GenerateParameters(properties, model)} }}, commandType: CommandType.Text);");
            else
                sb.AppendLine(space + $"    db.Execute(sql, new {{ {GenerateParameters(properties, model)} }}, commandType: CommandType.Text);");

            sb.AppendLine(space + "}");

            return sb.ToString();
        }

        public static string Delete(string model, IList<PropertyInfo> properties, bool generateMethod, bool generateClass)
        {
            model = FixClassName(model);
            var space = "";

            if (generateMethod)
                space = "    ";
            if (generateClass)
                space += "    ";

            var sb = new StringBuilder();

            sb.AppendLine(space + "// Delete");
            sb.AppendLine(space + "using (var db = new SqlConnection(connstring))");
            sb.AppendLine(space + "{");
            sb.AppendLine(space + $"    const string sql = @\"DELETE FROM {model} WHERE {properties[0].Name} = @{properties[0].Name}\";");
            sb.AppendLine("");
            sb.AppendLine(space + $"    db.Execute(sql, new {{ {model.ToLower()}.{properties[0].Name} }}, commandType: CommandType.Text);");
            sb.AppendLine(space + "}");

            return sb.ToString();
        }

        private static string GenerateProperties(IList<PropertyInfo> properties, bool includeAt)
        {
            var last = properties.Last();
            string str = string.Empty;
            foreach (var prop in properties)
            {
                if (includeAt)
                {
                    if (prop.Equals(last))
                    {
                        str += "@" + prop.Name;
                    }
                    else
                    {
                        str += "@" + prop.Name + ", ";
                    }
                }
                else
                {
                    if (prop.Equals(last))
                    {
                        str += prop.Name;
                    }
                    else
                    {
                        str += prop.Name + ", ";
                    }
                }
            }
            return str;
        }

        private static string GenerateUpdateValues(IList<PropertyInfo> properties)
        {
            var last = properties.Last();

            string str = string.Empty;
            foreach (var prop in properties)
            {
                if (prop.Equals(last))
                    str += $"{prop.Name} = @{prop.Name}";
                else
                    str += $"{prop.Name} = @{prop.Name}, ";
            }
            return str;
        }

        private static string GenerateParameters(IList<PropertyInfo> properties, string model)
        {
            var last = properties.Last();

            string str = string.Empty;
            foreach (var prop in properties)
            {
                if (prop.Equals(last))
                    str += $"{prop.Name} = {model.ToLower()}.{prop.Name}";
                else
                    str += $"{prop.Name} = {model.ToLower()}.{prop.Name}, ";
            }
            return str;
        }
    }
}