//TODO do we need to cover geometries specially here? doubtful, as long as property type name is "Point".
//TODO perhaps we should add more verbose geometry types though
//using NetTopologySuite.Geometries;
using System.Reflection;
using System.Text;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using System.Net.NetworkInformation;

//Makes enum and class models for consumption by Vue
namespace EfVueMantle;

public class ModelExport
{
    private static readonly List<Type> Enooms = new();
    private static readonly List<Type> Numerics = new List<Type>
    {
        typeof(int),
        typeof(long),
        typeof(decimal),
        typeof(short),
        typeof(double),
        typeof(float),
        typeof(uint),
        typeof(ulong),
        typeof(ushort),
    };

    //TODO put somewhere useful
    public static string ToDashCase(string value)
    {
        return Regex.Replace(value, @"([a-z])([A-Z])", "$1-$2").ToLower();
    }

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
            var names = Enum.GetNames(enumerType);
            var values = Enum.GetValues(enumerType);
            var flagsAttribute = Attribute.GetCustomAttribute(enumerType, typeof(FlagsAttribute)) as FlagsAttribute;
            var method = typeof(EnumExtensionMethods).GetMethod("GetDescription");


            byte[] lineOne = new UTF8Encoding(true).GetBytes("import Enum from 'ef-vue-crust/data-types/enum';\r\n\r\n");
            fs.Write(lineOne, 0, lineOne.Length);
            byte[] lineTwo = new UTF8Encoding(true).GetBytes($"const {enumerName} = new Enum({{\r\n");
            fs.Write(lineTwo, 0, lineTwo.Length);

            for (int i = 0; i < (values?.Length ?? 0); i++)
            {

                var value = Enum.Parse(enumerType, names[i]);
                var str = method?.Invoke(typeof(EnumExtensionMethods), parameters: new object[] { values?.GetValue(i) ?? new object() });
                if (value == null) continue;

                byte[] valueLine;
                if (flagsAttribute == null)
                {
                    valueLine = new UTF8Encoding(true).GetBytes($"    {(int)value} : '{str}',\r\n");
                }
                else
                {
                    valueLine = new UTF8Encoding(true).GetBytes($"    {(ushort)value} : '{str}',\r\n");
                }
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


        var vueModelAttribute = Attribute.GetCustomAttribute(modelType, typeof(EfVueModelAttribute)) as EfVueModelAttribute;
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
            var ignoreAttribute = modelProperty.GetCustomAttribute(typeof(JsonIgnoreAttribute)) as JsonIgnoreAttribute;
            var vueHidden = modelProperty.GetCustomAttribute(typeof(EfVueHiddenAttribute)) as EfVueHiddenAttribute;
            if (vueHidden != null || ignoreAttribute != null)
            {
                continue;
            }

            var modelPropertyType = modelProperty.PropertyType;
            var enumerable = false;
            var nullable = false;
            var configurationObject = new Dictionary<string, dynamic>() { };

            var underlyingType = Nullable.GetUnderlyingType(modelPropertyType);
            //First check if anything is nullable
            if (!modelPropertyType.IsValueType
                || underlyingType != null
                || modelProperty.Name == "Id"
                )
            {
                if (underlyingType != null)
                {
                    modelPropertyType = underlyingType;
                }
                nullable = true;
                configurationObject.Add("nullable", true);
            }

            //Second check if anything is enumerable
            if (
                modelPropertyType.IsArray
                || modelPropertyType.IsGenericType && modelPropertyType.GetGenericTypeDefinition() == typeof(List<>)
                )
            {
                enumerable = true;
                //Get the inner property type
                modelPropertyType = modelPropertyType.GenericTypeArguments?[0];
                if (modelPropertyType == null) {
                    Console.WriteLine("List or array with no type");
                    continue;
                }
            }

            //Third, get the name of the property
            var propertyName = modelProperty.Name;

            //Get the property type
            var propertyTypeName = string.Empty;
            var vuePropertyTypeAttribute = modelProperty.GetCustomAttribute(typeof(EfVuePropertyTypeAttribute)) as EfVuePropertyTypeAttribute;
            if (vuePropertyTypeAttribute != null)
            {
                propertyTypeName = vuePropertyTypeAttribute.VueProperty;
                if (propertyTypeName == "BitArray")
                {
                    imports.Add($"import {propertyTypeName} from 'ef-vue-crust/data-types/bit-array';\r\n");
                }
                else if (propertyTypeName == "Flag")
                {
                    imports.Add($"import {propertyTypeName} from 'ef-vue-crust/data-types/flag';\r\n");
                }
                else if (modelName != propertyTypeName)
                {
                    imports.Add($"import {propertyTypeName} from '../data-types/{ToDashCase(propertyTypeName)}';\r\n");
                }

            } else if (modelPropertyType.IsEnum)
            {
                propertyTypeName = modelPropertyType.Name;
                Enooms.Add(modelPropertyType);
                imports.Add($"import {propertyTypeName} from '../enums/{propertyTypeName}.js';\r\n");

            } else if (modelPropertyType == typeof(Guid))
            {
                propertyTypeName = "Guid";
                imports.Add($"import Guid from 'ef-vue-crust/data-types/guid';\r\n");
            } else if (Numerics.Contains(modelPropertyType))
            {
                propertyTypeName = "Number";
            } else if (modelPropertyType == typeof(bool))
            {
                propertyTypeName = "Boolean";
            } else if (modelPropertyType == typeof(string))
            {
                propertyTypeName = "String";
            } else if (modelPropertyType == typeof(DateTime) || modelPropertyType == typeof(DateTime?))
            {
                propertyTypeName = "Date";
            } else if (modelPropertyType == typeof(byte) && enumerable)
            {
                imports.Add($"import ByteArray from 'ef-vue-crust/data-types/byte-array';\r\n");
                propertyTypeName = "ByteArray";
                enumerable = false;
            } else if (modelPropertyType.Name == "Point")
            {
                propertyTypeName = modelPropertyType.Name;
                imports.Add($"import Point from 'ef-vue-crust/data-types/point';\r\n");
            } else if (modelPropertyType.IsClass)
            {
                propertyTypeName = modelPropertyType.Name;
                if (modelPropertyType != modelType)
                {
                    imports.Add($"import {propertyTypeName} from './{propertyTypeName}.js';\r\n");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(nameof(modelPropertyType));
            }
            propertyTypeName = enumerable ? $"[{propertyTypeName}]" : propertyTypeName;


            /// Configurations ///

            //Manual FKs
            var foreignKeyAttribute = modelProperty.GetCustomAttribute(typeof(ForeignKeyAttribute)) as ForeignKeyAttribute;
            var vueModelForeignKeyAttribute = modelProperty.GetCustomAttribute(typeof(EfVueModelForeignKeyAttribute)) as EfVueModelForeignKeyAttribute;
            if (vueModelForeignKeyAttribute != null) 
            {
                configurationObject.Add("foreignKey", JsonNamingPolicy.CamelCase.ConvertName(vueModelForeignKeyAttribute.VueModelForeignKey));
            } 
            else if (foreignKeyAttribute != null)
            {
                configurationObject.Add("foreignKey", JsonNamingPolicy.CamelCase.ConvertName(foreignKeyAttribute.Name));
            } 
            else if (modelPropertyType.IsSubclassOf(typeof(ModelBase)))
            {
                configurationObject.Add("foreignKey", $"{JsonNamingPolicy.CamelCase.ConvertName(propertyName)}Id{(enumerable ? "s":"")}");
            }

            var bitArrayLength = modelProperty.GetCustomAttribute(typeof(EfBitArrayLengthAttribute)) as EfBitArrayLengthAttribute;
            if (bitArrayLength != null)
            {
                configurationObject["length"] = bitArrayLength.BitArrayLength;
            }


            var minLengthPropertyAttribute = modelProperty.GetCustomAttribute(typeof(MinLengthAttribute)) as MinLengthAttribute;
            var maxLengthPropertyAttribute = modelProperty.GetCustomAttribute(typeof(MaxLengthAttribute)) as MaxLengthAttribute;

            if (
                minLengthPropertyAttribute != null 
                && maxLengthPropertyAttribute != null 
                && minLengthPropertyAttribute.Length == maxLengthPropertyAttribute.Length)
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

            var efVueEnum = modelProperty.GetCustomAttribute(typeof(EfVueEnumAttribute)) as EfVueEnumAttribute;

            if (efVueEnum?.VueEnum != null)
            {
                Enooms.Add(efVueEnum.VueEnum);
                imports.Add($"import {efVueEnum.VueEnum.Name} from '../enums/{efVueEnum.VueEnum.Name}.js';\r\n");
                configurationObject["enum"] = efVueEnum.VueEnum;
            }



                           

            if (configurationObject.Count > 0)
            {
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.None
                };
                jsonSerializerSettings.Converters.Add(new TypeConverter());
                properties.Add($"            '{JsonNamingPolicy.CamelCase.ConvertName(modelProperty?.Name ?? string.Empty)}': {{type: {propertyTypeName}, nullable: {nullable.ToString().ToLower()}, config: {JsonConvert.SerializeObject(configurationObject, jsonSerializerSettings)}}},\r\n");
            }
            else
            {
                properties.Add($"            '{JsonNamingPolicy.CamelCase.ConvertName(modelProperty?.Name ?? string.Empty)}': {{type: {propertyTypeName}, nullable: {nullable.ToString().ToLower()}}},\r\n");
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


public class TypeConverter : JsonConverter<Type>
{
    public override void WriteJson(JsonWriter writer, Type type, JsonSerializer serializer)
    {
        writer.WriteRawValue(type.ToString().Split('.').Last());
    }

    public override Type ReadJson(JsonReader reader, Type objectType, Type existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        throw new NotImplementedException();
    }
}