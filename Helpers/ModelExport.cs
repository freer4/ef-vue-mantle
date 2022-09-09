//TODO do we need to cover geometries specially here? doubtful, as long as property type name is "Point".
//TODO perhaps we should add more verbose geometry types though
//using NetTopologySuite.Geometries;
using System.Reflection;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;

//Makes enum and class models for consumption by Vue
namespace EfVueMantle;

public class ModelExport
{

    public static string ConstructEnum(Type enumerType, string directory)
    {
        var enumerName = enumerType.Name;
        var filePath = $"{directory}/{enumerName}.js";
        using (FileStream fs = File.Create(filePath))
        {
            byte[] lineOne = new UTF8Encoding(true).GetBytes("import Enum from '../Enum.js';\r\n\r\n");
            fs.Write(lineOne, 0, lineOne.Length);
            byte[] lineTwo = new UTF8Encoding(true).GetBytes($"const {enumerName} = new Enum({{\r\n");
            fs.Write(lineTwo, 0, lineTwo.Length);
            var names = Enum.GetNames(enumerType);
            var values = Enum.GetValues(enumerType);


            for (int i = 0; i < values.Length; i++)
            {
                var method = typeof(EnumExtensionMethods).GetMethod("GetDescription");
                var str = method.Invoke(typeof(EnumExtensionMethods), new object[] { values.GetValue(i) });

                byte[] valueLine = new UTF8Encoding(true).GetBytes($"    {(int)values.GetValue(i)} : '{str}',\r\n");
                fs.Write(valueLine, 0, valueLine.Length);
            }

            byte[] lineThree = new UTF8Encoding(true).GetBytes("});\r\n\r\n");
            fs.Write(lineThree, 0, lineThree.Length);
            byte[] lineFour = new UTF8Encoding(true).GetBytes($"export default {enumerName};");
            fs.Write(lineFour, 0, lineFour.Length);

        }
        return filePath;
    }

    public static string ExportEnum(Type enoom, string directory)
    {
        return ConstructEnum(enoom, directory);
    }
    public static List<string> ExportEnums(List<Type> enooms, string directory)
    {
        var createdFiles = new List<string>();

        foreach (var enoom in enooms)
        {
            createdFiles.Add(ExportEnum(enoom, directory));
        }
        return createdFiles;
    }


    public static string ConstructModel(Type modelType, string directory)
    {
        string modelName;


        var vueModelAttribute = (VueModelAttribute)Attribute.GetCustomAttribute(modelType, typeof(VueModelAttribute));
        if (vueModelAttribute != null)
        {
            modelName = vueModelAttribute.VueModel;
        }
        else
        {
            modelName = modelType.Name;
        }
        var modelProperties = modelType.GetProperties();

        List<string> imports = new List<string>();
        List<string> properties = new List<string>();

        foreach (var modelProperty in modelProperties)
        {
            var foreignKeyAttribute = (ForeignKeyAttribute)modelProperty.GetCustomAttribute(typeof(ForeignKeyAttribute));
            var vueModelForeignKeyAttribute = (VueModelForeignKeyAttribute)modelProperty.GetCustomAttribute(typeof(VueModelForeignKeyAttribute));
            var ignoreAttribute = (JsonIgnoreAttribute)modelProperty.GetCustomAttribute(typeof(JsonIgnoreAttribute));
            var vuePropertyAttribute = (VuePropertyTypeAttribute)modelProperty.GetCustomAttribute(typeof(VuePropertyTypeAttribute));
            var minLengthPropertyAttribute = (MinLengthAttribute)modelProperty.GetCustomAttribute(typeof(MinLengthAttribute));
            var maxLengthPropertyAttribute = (MaxLengthAttribute)modelProperty.GetCustomAttribute(typeof(MaxLengthAttribute));
            var bitArrayLength = (BitArrayLengthAttribute)modelProperty.GetCustomAttribute(typeof(BitArrayLengthAttribute));

            if (ignoreAttribute != null)
            {
                continue;
            }

            var configurationObject = new Dictionary<string, dynamic>() { };
            if (bitArrayLength != null)
            {
                configurationObject["length"] = bitArrayLength.BitArrayLength;
            }
            if (foreignKeyAttribute != null)
            {
                configurationObject["foreignKey"] = JsonNamingPolicy.CamelCase.ConvertName(foreignKeyAttribute.Name);
            }
            if (vueModelForeignKeyAttribute != null)
            {
                configurationObject["foreignKey"] = JsonNamingPolicy.CamelCase.ConvertName(vueModelForeignKeyAttribute.VueModelForeignKey);
            }
            if (minLengthPropertyAttribute != null && maxLengthPropertyAttribute != null && minLengthPropertyAttribute.Length == maxLengthPropertyAttribute.Length)
            {
                configurationObject["length"] = minLengthPropertyAttribute.Length;
            }
            else
            {
                if (minLengthPropertyAttribute != null)
                {
                    configurationObject["minLength"] = minLengthPropertyAttribute.Length;
                }
                if (maxLengthPropertyAttribute != null)
                {
                    configurationObject["maxLength"] = maxLengthPropertyAttribute.Length;
                }
            }
            //!!Can add regex here

            string propertyType = "null";
            bool nullable = false;
            if (modelProperty.Name == "Id"
                || modelProperty.PropertyType.IsGenericType
                && modelProperty.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                nullable = true;
            }
            //if (modelProperty.Name != "Id" && Nullable.GetUnderlyingType(modelProperty.PropertyType) == null)
            //{
            //    nullable = false;
            //}
            if (vuePropertyAttribute != null)
            {
                if (modelName != vuePropertyAttribute.VueProperty)
                {
                    if (vuePropertyAttribute.VueProperty == "BitArray")
                    {
                        imports.Add($"import {vuePropertyAttribute.VueProperty} from '../{vuePropertyAttribute.VueProperty}.js';\r\n");
                    }
                    else
                    {
                        imports.Add($"import {vuePropertyAttribute.VueProperty} from './{vuePropertyAttribute.VueProperty}.js';\r\n");
                    }
                }
                if (modelProperty.PropertyType.IsArray
                    || modelProperty.PropertyType == typeof(IEnumerable<int>)
                    || modelProperty.PropertyType == typeof(IEnumerable<int>))
                {
                    propertyType = $"[{vuePropertyAttribute.VueProperty}]";
                }
                else
                {
                    propertyType = vuePropertyAttribute.VueProperty;
                }
            }
            else if (modelProperty.PropertyType == typeof(string))
            {
                if (modelProperty.GetMethod?.CustomAttributes.Where(x => x.AttributeType.Name == "NullableContextAttribute").Count() == 0)
                {
                    nullable = true;
                }
                propertyType = "String";
            }
            else if (modelProperty.PropertyType == typeof(Guid))
            {
                imports.Add($"import Guid from '../Guid.js';\r\n");
                propertyType = "Guid";
            }
            else if (modelProperty.PropertyType == typeof(Guid?))
            {
                nullable = true;
                imports.Add($"import Guid from '../Guid.js';\r\n");
                propertyType = "Guid";
            }
            else if (
                modelProperty.PropertyType == typeof(int)
                || modelProperty.PropertyType == typeof(int?)
                || modelProperty.PropertyType == typeof(decimal)
                || modelProperty.PropertyType == typeof(decimal?)
                || modelProperty.PropertyType == typeof(double)
                || modelProperty.PropertyType == typeof(double?)
            )
            {
                propertyType = "Number";
            }
            else if (modelProperty.PropertyType == typeof(bool) || modelProperty.PropertyType == typeof(bool?))
            {
                propertyType = "Boolean";
            }
            else if (modelProperty.PropertyType == typeof(DateTime) || modelProperty.PropertyType == typeof(DateTime?))
            {
                propertyType = "Date";
            }
            else if (modelProperty.PropertyType.IsEnum)
            {
                imports.Add($"import {modelProperty.PropertyType.Name} from '../enums/{modelProperty.PropertyType.Name}.js';\r\n");
                propertyType = modelProperty.PropertyType.Name;
            }
            else if (modelProperty.PropertyType.GenericTypeArguments?.FirstOrDefault()?.IsEnum == true)
            {
                imports.Add($"import {modelProperty.PropertyType.GenericTypeArguments.FirstOrDefault()?.Name} from './{modelProperty.PropertyType.GenericTypeArguments.FirstOrDefault().Name}.js';\r\n");
                propertyType = modelProperty.PropertyType.GenericTypeArguments.FirstOrDefault()?.Name ?? "Null";
                nullable = true;
            }
            else if (modelProperty.PropertyType.Name == "Byte[]")
            {
                imports.Add($"import ByteArray from '../ByteArray.js';\r\n");
                propertyType = $"ByteArray";
            }
            else if (modelProperty.PropertyType.IsClass)
            {
                //not self-referencing
                if (modelName != modelProperty.PropertyType.Name)
                {
                    if (modelProperty.PropertyType.Name == "Point")
                    {
                        imports.Add($"import {modelProperty.PropertyType.Name} from '../{modelProperty.PropertyType.Name}.js';\r\n");
                        propertyType = modelProperty.PropertyType.Name;
                    }
                    else if (modelProperty.PropertyType.Name == "List`1")
                    {

                        if (modelProperty.PropertyType.GenericTypeArguments?[0].Name == "Int32")
                        {
                            nullable = true;
                            propertyType = "[Number]";
                        }
                        else
                        {
                            imports.Add($"import {modelProperty.PropertyType.GenericTypeArguments?[0].Name} from './{modelProperty.PropertyType.GenericTypeArguments?[0].Name}.js';\r\n");
                            propertyType = $"[{modelProperty.PropertyType.GenericTypeArguments?[0].Name}]";
                        }
                    }
                    else
                    {
                        imports.Add($"import {modelProperty.PropertyType.Name} from './{modelProperty.PropertyType.Name}.js';\r\n");
                        propertyType = modelProperty.PropertyType.Name;
                    } 
                }
                else
                {
                    propertyType = modelProperty.PropertyType.Name;

                }
            }
            else if (modelProperty is IEnumerable<string>)
            {
                propertyType = "[String]";
            }
            else if (modelProperty is IEnumerable<int>)
            {
                propertyType = "[Number]";
            }
            else if (modelProperty is IEnumerable<Enum>)
            {
                imports.Add($"import {modelProperty.PropertyType.Name} from '../enums/{modelProperty.PropertyType.Name}.js';\r\n");
                propertyType = $"[{modelProperty.PropertyType.Name}]";
            }
            else if (modelProperty is IEnumerable<object>)
            {
                imports.Add($"import {modelProperty.PropertyType.Name} from './{modelProperty.PropertyType.Name}.js';\r\n");
                propertyType = $"[{modelProperty.PropertyType.Name}]";
            }
            else if (modelProperty.PropertyType.Name == "IEnumerable`1")
            {
                if (modelProperty.CustomAttributes.FirstOrDefault()?.AttributeType?.Name == "NullableAttribute")
                {
                    nullable = true;
                }
                if (modelProperty.PropertyType.GenericTypeArguments?[0].Name == "Int32")
                {
                    nullable = true;
                    propertyType = "[Number]";
                }
                else
                {
                    imports.Add($"import {modelProperty.PropertyType.GenericTypeArguments?[0].Name} from './{modelProperty.PropertyType.GenericTypeArguments?[0].Name}.js';\r\n");
                    propertyType = $"[{modelProperty.PropertyType.GenericTypeArguments?[0].Name}]";
                }
            }
            else
            {
                propertyType = modelProperty.PropertyType.Name;
            }

            if (configurationObject.Count > 0)
            {
                properties.Add($"            '{JsonNamingPolicy.CamelCase.ConvertName(modelProperty.Name)}': {{type: {propertyType}, nullable: {nullable.ToString().ToLower()}, config: {System.Text.Json.JsonSerializer.Serialize(configurationObject)}}},\r\n");
            }
            else
            {
                properties.Add($"            '{JsonNamingPolicy.CamelCase.ConvertName(modelProperty.Name)}': {{type: {propertyType}, nullable: {nullable.ToString().ToLower()}}},\r\n");
            }

        }

        var filePath = $"{directory}/{modelName}.js";
        imports = imports.Distinct().ToList();

        using (FileStream fs = File.Create(filePath))
        {
            byte[] lineOne = new UTF8Encoding(true).GetBytes("import Model from '../Model.js';\r\n\r\n");
            fs.Write(lineOne, 0, lineOne.Length);
            foreach (string import in imports)
            {
                byte[] fsLine = new UTF8Encoding(true).GetBytes(import);
                fs.Write(fsLine, 0, fsLine.Length);
            }
            byte[] lineTwo = new UTF8Encoding(true).GetBytes($"\r\nclass {modelName} extends Model {{\r\n");
            fs.Write(lineTwo, 0, lineTwo.Length);

            byte[] lineThree = new UTF8Encoding(true).GetBytes("    constructor(record, config = {}){\r\n");
            fs.Write(lineThree, 0, lineThree.Length);

            byte[] lineFour = new UTF8Encoding(true).GetBytes("        super(record, config);\r\n\r\n");
            fs.Write(lineFour, 0, lineFour.Length);


            byte[] lineSix = new UTF8Encoding(true).GetBytes($"    }}\r\n    static name = '{modelName}';\r\n");
            fs.Write(lineSix, 0, lineSix.Length);

            if (!modelName.Contains("DTO"))
            {
                byte[] lineSeven = new UTF8Encoding(true).GetBytes($"    static source = '{modelName.Replace("Model", "")}';\r\n");
                fs.Write(lineSeven, 0, lineSeven.Length);
            }

            byte[] lineSevenOne = new UTF8Encoding(true).GetBytes($"\r\n    static getProperties = () => {{\r\n");
            fs.Write(lineSevenOne, 0, lineSevenOne.Length);

            byte[] lineSevenTwo = new UTF8Encoding(true).GetBytes($"\r\n        return {{\r\n");
            fs.Write(lineSevenTwo, 0, lineSevenTwo.Length);

            foreach (string property in properties)
            {
                byte[] fsLine = new UTF8Encoding(true).GetBytes(property);
                fs.Write(fsLine, 0, fsLine.Length);
            }

            byte[] lineSevenThree = new UTF8Encoding(true).GetBytes($"        }};\r\n");
            fs.Write(lineSevenThree, 0, lineSevenThree.Length);

            byte[] lineSevenFour = new UTF8Encoding(true).GetBytes($"    }};\r\n");
            fs.Write(lineSevenFour, 0, lineSevenFour.Length);

            byte[] lineSevenAndAHalf = new UTF8Encoding(true).GetBytes($"}}\r\nwindow[Symbol.for({modelName}.name)] = {modelName};\r\n");
            fs.Write(lineSevenAndAHalf, 0, lineSevenAndAHalf.Length);

            byte[] lineEight = new UTF8Encoding(true).GetBytes($"\r\nexport default {modelName};");
            fs.Write(lineEight, 0, lineEight.Length);

        }
        return filePath;
    }

    public static List<string> ExportModel(string directory)
    {
        
        var models = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
          .Where(t => t.IsSubclassOf(typeof(ModelBase)));
        var fileLocations = new List<string>();
        
        foreach (var model in models)
        {
            fileLocations.Add(ConstructModel(model, directory));
        }

        return fileLocations;
    }
}
