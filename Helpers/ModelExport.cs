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
    private static readonly List<Type> Enooms = new();

    public static string ConstructEnum(Type enumerType, string directory)
    {
        if (!Directory.Exists($"{directory}/"))
        {
            Directory.CreateDirectory($"{directory}/");
        }
        var enumerName = enumerType.Name;
        var filePath = $"{directory}/{enumerName}.js";
        using (FileStream fs = File.Create(filePath))
        {
            byte[] lineOne = new UTF8Encoding(true).GetBytes("import Enum from '@ef-vue-crust/data-types/enum';\r\n\r\n");
            fs.Write(lineOne, 0, lineOne.Length);
            byte[] lineTwo = new UTF8Encoding(true).GetBytes($"const {enumerName} = new Enum({{\r\n");
            fs.Write(lineTwo, 0, lineTwo.Length);
            var names = Enum.GetNames(enumerType);
            var values = Enum.GetValues(enumerType);

            for (int i = 0; i < (values?.Length ?? 0); i++)
            {
                var method = typeof(EnumExtensionMethods).GetMethod("GetDescription");
                var str = method?.Invoke(typeof(EnumExtensionMethods), parameters: new object[] { values?.GetValue(i) ?? new object() });

                byte[] valueLine = new UTF8Encoding(true).GetBytes($"    {(int)(values?.GetValue(i) ?? 0)} : '{str}',\r\n");
                fs.Write(valueLine, 0, valueLine.Length);
            }

            byte[] lineThree = new UTF8Encoding(true).GetBytes("});\r\n\r\n");
            fs.Write(lineThree, 0, lineThree.Length);
            byte[] lineFour = new UTF8Encoding(true).GetBytes($"export default {enumerName};");
            fs.Write(lineFour, 0, lineFour.Length);

        }
        return filePath;
    }

    private static List<string> ExportEnums(string directory)
    {
        var createdFiles = new List<string>();

        foreach (var enoom in Enooms)
        {
            createdFiles.Add(ConstructEnum(enoom, directory));
        }
        return createdFiles;
    }


    public static string ConstructModel(Type modelType, string directory)
    {
        string modelName;


        var vueModelAttribute = Attribute.GetCustomAttribute(modelType, typeof(VueModelAttribute)) as VueModelAttribute;
        var efVueSourceAttribute = Attribute.GetCustomAttribute(modelType, typeof(EfVueSourceAttribute)) as EfVueSourceAttribute;
        if (vueModelAttribute != null)
        {
            modelName = vueModelAttribute.VueModel;
        }
        else
        {
            modelName = modelType.Name;
        }
        var modelProperties = modelType.GetProperties();
        string? source = efVueSourceAttribute?.VueSource;
        if (string.IsNullOrEmpty(source) && modelType.IsSubclassOf(typeof(ModelBase)))
        {
            source = modelName;
        }
        if (!string.IsNullOrEmpty(source))
        {
            source = source.Replace("Model", "");
        }


        List<string> imports = new List<string>();
        List<string> properties = new List<string>();

        foreach (var modelProperty in modelProperties)
        {
            var foreignKeyAttribute = modelProperty.GetCustomAttribute(typeof(ForeignKeyAttribute)) as ForeignKeyAttribute;
            var vueModelForeignKeyAttribute = modelProperty.GetCustomAttribute(typeof(VueModelForeignKeyAttribute)) as VueModelForeignKeyAttribute;
            var ignoreAttribute = modelProperty.GetCustomAttribute(typeof(JsonIgnoreAttribute)) as JsonIgnoreAttribute;
            var vuePropertyAttribute = modelProperty.GetCustomAttribute(typeof(VuePropertyTypeAttribute)) as VuePropertyTypeAttribute;
            var minLengthPropertyAttribute = modelProperty.GetCustomAttribute(typeof(MinLengthAttribute)) as MinLengthAttribute;
            var maxLengthPropertyAttribute = modelProperty.GetCustomAttribute(typeof(MaxLengthAttribute)) as MaxLengthAttribute;
            var bitArrayLength = modelProperty.GetCustomAttribute(typeof(BitArrayLengthAttribute)) as BitArrayLengthAttribute;
            var vueHidden = modelProperty.GetCustomAttribute(typeof(EfVueHiddenAttribute)) as EfVueHiddenAttribute;

            if (vueHidden != null || ignoreAttribute != null)
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
                        imports.Add($"import {vuePropertyAttribute.VueProperty} from '@ef-vue-crust/data-types/bit-array';\r\n");
                    }
                    else
                    {
                        imports.Add($"import {vuePropertyAttribute.VueProperty} from '@ef-vue-crust/data-types/bit-array';\r\n");
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
                    nullable = false;
                }
                propertyType = "String";
            }
            else if (modelProperty.PropertyType == typeof(Guid))
            {
                imports.Add($"import Guid from '@ef-vue-crust/data-types/guid';\r\n");
                propertyType = "Guid";
            }
            else if (modelProperty.PropertyType == typeof(Guid?))
            {
                nullable = true;
                imports.Add($"import Guid from '@ef-vue-crust/data-types/guid';\r\n");
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
                Enooms.Add(modelProperty.PropertyType);
                imports.Add($"import {modelProperty.PropertyType.Name} from '../enums/{modelProperty.PropertyType.Name}.js';\r\n");
                propertyType = modelProperty.PropertyType.Name;
            }
            else if (modelProperty.PropertyType.GenericTypeArguments?.FirstOrDefault()?.IsEnum == true)
            {
                imports.Add($"import {modelProperty.PropertyType.GenericTypeArguments.FirstOrDefault()?.Name} from './{modelProperty?.PropertyType?.GenericTypeArguments?.FirstOrDefault()?.Name}.js';\r\n");
                propertyType = modelProperty?.PropertyType.GenericTypeArguments.FirstOrDefault()?.Name ?? "Null";
                nullable = true;
            }
            else if (modelProperty.PropertyType.Name == "Byte[]")
            {
                imports.Add($"import ByteArray from 'ef-vue-crust/data-types/byte-array';\r\n");
                propertyType = $"ByteArray";
            }
            else if (modelProperty.PropertyType.IsClass)
            {
                //not self-referencing
                if (modelName != modelProperty.PropertyType.Name)
                {
                    if (modelProperty.PropertyType.Name == "Point")
                    {
                        imports.Add($"import {modelProperty.PropertyType.Name} from 'ef-vue-crust/data-types/point';\r\n");
                        propertyType = modelProperty.PropertyType.Name;
                    }
                    else if (modelProperty.PropertyType.Name == "List`1")
                    {

                        if (modelProperty.PropertyType.GenericTypeArguments?[0].Name == "Int32")
                        {
                            nullable = true;
                            propertyType = "[Number]";
                        }
                        else if (modelProperty.PropertyType.GenericTypeArguments?[0].Name == "String")
                        {
                            nullable = true;
                            propertyType = "[String]";
                        }
                        else
                        {
                            if (modelProperty.PropertyType.GenericTypeArguments?[0].Name != modelName)
                            {
                                imports.Add($"import {modelProperty.PropertyType.GenericTypeArguments?[0].Name} from './{modelProperty.PropertyType.GenericTypeArguments?[0].Name}.js';\r\n");
                            }
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
                Enooms.Add(modelProperty.PropertyType);
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
                properties.Add($"            '{JsonNamingPolicy.CamelCase.ConvertName(modelProperty?.Name ?? string.Empty)}': {{type: {propertyType}, nullable: {nullable.ToString().ToLower()}, config: {System.Text.Json.JsonSerializer.Serialize(configurationObject)}}},\r\n");
            }
            else
            {
                properties.Add($"            '{JsonNamingPolicy.CamelCase.ConvertName(modelProperty?.Name ?? string.Empty)}': {{type: {propertyType}, nullable: {nullable.ToString().ToLower()}}},\r\n");
            }

        }

        if (!Directory.Exists($"{directory}/"))
        {
            Directory.CreateDirectory($"{directory}/");
        }

        var filePath = $"{directory}/{modelName}.js";
        imports = imports.Distinct().ToList();

        using (FileStream fs = File.Create(filePath))
        {
            byte[] lineOne = new UTF8Encoding(true).GetBytes("import Model from 'ef-vue-crust/data-types/model';\r\n\r\n");
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

            byte[] lineFour = new UTF8Encoding(true).GetBytes("        super(record, config);\r\n");
            fs.Write(lineFour, 0, lineFour.Length);


            byte[] lineSix = new UTF8Encoding(true).GetBytes($"    }}\r\n\r\n    static name = '{modelName}';\r\n");
            fs.Write(lineSix, 0, lineSix.Length);

            if (!string.IsNullOrEmpty(source))
            {
                byte[] lineSeven = new UTF8Encoding(true).GetBytes($"    static source = '{source}';\r\n");
                fs.Write(lineSeven, 0, lineSeven.Length);
            }
            byte[] lineSevenish = new UTF8Encoding(true).GetBytes($"    static dto = {modelType.IsSubclassOf(typeof(DataTransferObjectBase)).ToString().ToLower()};\r\n");
            fs.Write(lineSevenish, 0, lineSevenish.Length);

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

    public static List<string> ExportDataObjects(string directory = "VueExports")
    {
        
        var models = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
          .Where(t => t.IsSubclassOf(typeof(ModelBase)));
        var fileLocations = new List<string>();
        
        foreach (var model in models)
        {
            fileLocations.Add(ConstructModel(model, $"{directory}/models"));
        }

        var dtos = AppDomain.CurrentDomain.GetAssemblies().SelectMany(x => x.GetTypes())
            .Where(x => x.IsSubclassOf(typeof(DataTransferObjectBase)));

        foreach (var dto in dtos)
        {
            fileLocations.Add(ConstructModel(dto, $"{directory}/dtos"));
        }

        fileLocations.AddRange(ExportEnums($"{directory}/enums"));

        return fileLocations;
    }
}
