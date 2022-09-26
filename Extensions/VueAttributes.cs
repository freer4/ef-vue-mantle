namespace EfVueMantle;

[AttributeUsage(AttributeTargets.Property)]
public class VuePropertyTypeAttribute : Attribute
{
    public string VueProperty { get; set; }
    public VuePropertyTypeAttribute(string property)
    {
        VueProperty = property;
    }
}

[AttributeUsage(AttributeTargets.Class)]
public class VueModelAttribute : Attribute
{
    public string VueModel { get; set; }
    public VueModelAttribute(string model)
    {
        VueModel = model;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class VueModelForeignKeyAttribute : Attribute
{
    public string VueModelForeignKey { get; set; }
    public VueModelForeignKeyAttribute(string foreignKey)
    {
        VueModelForeignKey = foreignKey;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class BitArrayLengthAttribute : Attribute
{
    public int BitArrayLength { get; set; }
    public BitArrayLengthAttribute(int length)
    {
        BitArrayLength = length;
    }
}

[AttributeUsage(AttributeTargets.Property)]
public class VueExcludeFromDataAttribute : Attribute
{
    public bool VueExcludeFromData { get; set; }
    public VueExcludeFromDataAttribute()
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