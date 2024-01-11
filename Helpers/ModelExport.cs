//TODO do we need to cover geometries specially here? doubtful, as long as property type name is "Point".
//TODO perhaps we should add more verbose geometry types though
//using NetTopologySuite.Geometries;
using System.Reflection;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.ComponentModel.DataAnnotations.Schema;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;

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

        var models = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
          .Where(t => t.IsSubclassOf(typeof(ModelBase))
            || t.IsSubclassOf(typeof(ModelBase<Guid>))
            || t.IsSubclassOf(typeof(IModelBase))
            || t.IsSubclassOf(typeof(IModelBase<Guid>)));


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
        if (string.IsNullOrEmpty(source) && (
            modelType.IsSubclassOf(typeof(ModelBase))
            || modelType.IsSubclassOf(typeof(ModelBase<Guid>))
            || modelType.IsSubclassOf(typeof(IModelBase))
            || modelType.IsSubclassOf(typeof(IModelBase<Guid>))
            ))
        {
            source = modelName;
        }
        if (!string.IsNullOrEmpty(source))
        {
            source = Regex.Replace(source, "Model$", "");
            //source = source.Replace("Model", "");
        }

        string? endpoint = null;
        var efVueEndpointAttribute = Attribute.GetCustomAttribute(modelType, typeof(EfVueEndpointAttribute)) as EfVueEndpointAttribute;
        if (efVueEndpointAttribute != null)
        {
            endpoint = efVueEndpointAttribute.Endpoint;
        }

        List<string> imports = new();
        List<string> properties = new();
        Dictionary<string, string> foreignKeys = new();
        NullabilityInfoContext nullabilityContext = new NullabilityInfoContext();

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
            var configurationObject = new Dictionary<string, dynamic>() { };

            var underlyingType = Nullable.GetUnderlyingType(modelPropertyType);
            var nullabilityInfo = nullabilityContext.Create(modelProperty);
            //First check if anything is nullable
            if (underlyingType != null
                || nullabilityInfo.WriteState is NullabilityState.Nullable
                )
            {

                if (underlyingType != null)
                {
                    modelPropertyType = underlyingType;
                }
                configurationObject.Add("nullable", true);
            } else
            {
                configurationObject.Add("nullable", false);
            }

            //Second check if anything is enumerable
            if (
                (modelPropertyType.IsEnum
                    || modelPropertyType.IsArray
                    || modelPropertyType.IsGenericType && modelPropertyType.GetGenericTypeDefinition() == typeof(List<>))
                && modelPropertyType.BaseType != typeof(Enum)
                )
            {
                enumerable = true;

                if (modelPropertyType.GenericTypeArguments != null
                    && modelPropertyType.GenericTypeArguments.Length > 0 )
                {
                    //Get the inner property type
                    modelPropertyType = modelPropertyType.GenericTypeArguments?[0];
                    if (modelPropertyType == null)
                    {
                        Console.WriteLine("List or array with no type");
                        continue;
                    }
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
                    imports.Add($"import {propertyTypeName} from 'ef-vue-crust/data-types/bit-array';");
                }
                else if (propertyTypeName == "Flag")
                {
                    imports.Add($"import {propertyTypeName} from 'ef-vue-crust/data-types/flag';");
                }
                else if (modelName != propertyTypeName)
                {
                    if (models.Select(x => x.Name).ToList().Contains(propertyTypeName))
                    {
                        imports.Add($"import {propertyTypeName} from './{propertyTypeName}';");
                    } else
                    {
                        imports.Add($"import {propertyTypeName} from '../data-types/{ToDashCase(propertyTypeName)}';");
                    }
                }

            } else if (modelPropertyType.BaseType == typeof(Enum))
            {
                propertyTypeName = modelPropertyType.Name;
                Enooms.Add(modelPropertyType);
                imports.Add($"import {propertyTypeName} from '../enums/{propertyTypeName}.js';");

            } else if (modelPropertyType == typeof(Guid))
            {
                propertyTypeName = "Guid";
                imports.Add($"import Guid from 'ef-vue-crust/data-types/guid';");
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
                imports.Add($"import ByteArray from 'ef-vue-crust/data-types/byte-array';");
                propertyTypeName = "ByteArray";
                enumerable = false;
            } else if (modelPropertyType.Name == "Point")
            {
                propertyTypeName = modelPropertyType.Name;
                imports.Add($"import Point from 'ef-vue-crust/data-types/point';");
            } else if (modelPropertyType.IsClass)
            {
                propertyTypeName = modelPropertyType.Name;
                if (modelPropertyType != modelType)
                {
                    imports.Add($"import {propertyTypeName} from '../models/{propertyTypeName}.js';");
                }
            }
            else
            {
                throw new ArgumentOutOfRangeException(propertyTypeName);
            }
            propertyTypeName = enumerable ? $"[{propertyTypeName}]" : propertyTypeName;


            /// Configurations ///

            //Manual FKs
            var foreignKeyAttribute = modelProperty.GetCustomAttribute(typeof(ForeignKeyAttribute)) as ForeignKeyAttribute;
            var vueModelForeignKeyAttribute = modelProperty.GetCustomAttribute(typeof(EfVueModelForeignKeyAttribute)) as EfVueModelForeignKeyAttribute;
            if (vueModelForeignKeyAttribute != null) 
            {
                var fkName = JsonNamingPolicy.CamelCase.ConvertName(vueModelForeignKeyAttribute.VueModelForeignKey);
                configurationObject.Add("foreignKey", fkName);
                foreignKeys.Add(fkName, JsonNamingPolicy.CamelCase.ConvertName(propertyName));
            }
            else if (foreignKeyAttribute != null)
            {
                var fkName = JsonNamingPolicy.CamelCase.ConvertName(foreignKeyAttribute.Name);
                configurationObject.Add("foreignKey", fkName);
                foreignKeys.Add(fkName, JsonNamingPolicy.CamelCase.ConvertName(propertyName));
            }
            else if (modelPropertyType.IsSubclassOf(typeof(ModelBase)) || modelPropertyType.IsSubclassOf(typeof(ModelBase<Guid>)))
            {
                var fkName = $"{JsonNamingPolicy.CamelCase.ConvertName(propertyName)}Id{(enumerable ? "s" : "")}";
                configurationObject.Add("foreignKey", fkName);
                foreignKeys.Add(fkName, JsonNamingPolicy.CamelCase.ConvertName(propertyName));
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
                imports.Add($"import {efVueEnum.VueEnum.Name} from '../enums/{efVueEnum.VueEnum.Name}.js';");
                configurationObject["enum"] = efVueEnum.VueEnum;
            }



                           

            if (configurationObject.Count > 0)
            {
                var jsonSerializerSettings = new JsonSerializerSettings()
                {
                    TypeNameHandling = TypeNameHandling.None
                };
                jsonSerializerSettings.Converters.Add(new TypeConverter());
                properties.Add($"'{JsonNamingPolicy.CamelCase.ConvertName(modelProperty?.Name ?? string.Empty)}': {{type: {propertyTypeName}, config: {JsonConvert.SerializeObject(configurationObject, jsonSerializerSettings)}}},");
            }
            else
            {
                properties.Add($"'{JsonNamingPolicy.CamelCase.ConvertName(modelProperty?.Name ?? string.Empty)}': {{type: {propertyTypeName}}},");
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
            var lines = new List<string>();
            lines.Add("//This file was generated by EfVueMantle, do not modify directly as your changes will be lost.");
            lines.Add("import Model from 'ef-vue-crust/data-types/model';");

            foreach (string import in imports)
            {
                lines.Add(import);
            }

            lines.Add("");

            //OPEN CLASS
            lines.Add($"class {modelName} extends Model {{");
            
            lines.Add("    constructor(record, config = {}){");
            lines.Add("        super(record, config);");
            lines.Add("    }");

            lines.Add("");

            //STATICS BLOCK
            lines.Add($"    static name = '{modelName}';");
            if (!string.IsNullOrEmpty(source)) lines.Add($"    static source = '{source}';");
            if (!string.IsNullOrEmpty(endpoint)) lines.Add($"    static endpoint = '{endpoint}';");

            lines.Add($"    static dto = {modelType.IsSubclassOf(typeof(DataTransferObjectBase)).ToString().ToLower()};");
            lines.Add("");

            //PROPERTIES BLOCK
            lines.Add("    static get properties() {");
            lines.Add("        const value = {");
            foreach (string property in properties)
            {
                lines.Add($"            {property}");
            }
            lines.Add("        };");
            lines.Add("        Object.defineProperty(this, 'properties', {value})");
            lines.Add("        return value;");
            lines.Add("    }");

            //FOREIGN KEYS
            if (foreignKeys.Count > 0)
            {
                lines.Add("");
                lines.Add("    static foreignKeys = {");
                foreach(var foreignKey in foreignKeys)
                {
                    lines.Add($"        '{foreignKey.Key}': '{foreignKey.Value}',");
                }
                lines.Add("    };");
            }


            //CLOSE CLASS
            lines.Add("}");

            lines.Add("");


            lines.Add("");
            lines.Add($"window[Symbol.for({modelName}.name)] = {modelName};");

            lines.Add("");

            lines.Add($"export default {modelName};");

            WriteLines(fs, lines);
        }
        return filePath;
    }

    public static void WriteLines(FileStream fs, List<string> lines)
    {
        foreach (var line in lines)
        {
            var fullLine = $"{line}\r\n";
            fs.Write(
                new UTF8Encoding(true).GetBytes(fullLine),
                0,
                fullLine.Length
            );
        }
    }

    public static List<string> ExportDataObjects(string directory = "VueExports")
    {
        
        var models = AppDomain.CurrentDomain.GetAssemblies().SelectMany(s => s.GetTypes())
          .Where(t => t.IsSubclassOf(typeof(ModelBase)) 
          || t.IsSubclassOf(typeof(ModelBase<Guid>)) 
          || t.IsSubclassOf(typeof(IModelBase)) 
          || t.IsSubclassOf(typeof(IModelBase<Guid>)));
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