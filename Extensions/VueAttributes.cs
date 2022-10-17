namespace EfVueMantle;

[AttributeUsage(AttributeTargets.Property)]
public class EfVuePropertyTypeAttribute : Attribute
{
    public string VueProperty { get; set; }
    public EfVuePropertyTypeAttribute(string property)
    {
        VueProperty = property;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class EfVueModelAttribute : Attribute
{
    public string VueModel { get; set; }
    public EfVueModelAttribute(string model)
    {
        VueModel = model;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class EfVueModelForeignKeyAttribute : Attribute
{
    public string VueModelForeignKey { get; set; }
    public EfVueModelForeignKeyAttribute(string foreignKey)
    {
        VueModelForeignKey = foreignKey;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class EfBitArrayLengthAttribute : Attribute
{
    public int BitArrayLength { get; set; }
    public EfBitArrayLengthAttribute(int length)
    {
        BitArrayLength = length;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class EfVueExcludeFromDataAttribute : Attribute
{
    public bool VueExcludeFromData { get; set; }
    public EfVueExcludeFromDataAttribute()
    {
        VueExcludeFromData = true;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class EfVueHiddenAttribute : Attribute
{
    //
}
[AttributeUsage(AttributeTargets.Class)]
public class EfVueSourceAttribute : Attribute
{
    public string VueSource { get; set; }
    public EfVueSourceAttribute(Type vueSource)
    {
        VueSource = vueSource.Name;
    }
    public EfVueSourceAttribute(string vueSource)
    {
        VueSource = vueSource;
    }
}
[AttributeUsage(AttributeTargets.Class)]
public class EfVueEndpointAttribute : Attribute
{
    public string Endpoint { get; set; }
    public EfVueEndpointAttribute(string endpoint)
    {
        Endpoint = endpoint;
    }
}


[AttributeUsage(AttributeTargets.Property)]
public class EfVueEnumAttribute : Attribute
{
    public Type VueEnum { get; set; }
    public EfVueEnumAttribute(Type vueEnum)
    {
        VueEnum = vueEnum;
    }
}