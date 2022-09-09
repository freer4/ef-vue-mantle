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
