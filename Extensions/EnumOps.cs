using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Newtonsoft.Json;
using System.Reflection;

namespace EfVueMantle;

public static class EnumExtensionMethods
{
    public static string GetDescription(this Enum GenericEnum)
    {
        Type genericEnumType = GenericEnum.GetType();
        MemberInfo[] memberInfo = genericEnumType.GetMember(GenericEnum.ToString());
        if ((memberInfo != null && memberInfo.Length > 0))
        {
            var _Attribs = memberInfo[0].GetCustomAttributes(typeof(System.ComponentModel.DescriptionAttribute), false);
            if ((_Attribs != null && _Attribs.Count() > 0))
            {
                return ((System.ComponentModel.DescriptionAttribute)_Attribs.ElementAt(0)).Description;
            }
        }
        return GenericEnum.ToString();
    }
}

//From https://github.com/gkedzierski/entity-collection-serializer-example
public class EnumCollectionJsonValueConverter<T> : ValueConverter<ICollection<T>, string> where T : Enum
{
    public EnumCollectionJsonValueConverter() : base(
      v => JsonConvert
        .SerializeObject(v.Select(e => e.ToString()).ToList()),
      v => JsonConvert
        .DeserializeObject<ICollection<string>>(v)
        .Select(e => (T)Enum.Parse(typeof(T), e)).ToList())
    {
    }
}
public class CollectionValueComparer<T> : ValueComparer<ICollection<T>>
{
    public CollectionValueComparer() : base((c1, c2) => c1.SequenceEqual(c2),
      c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())), c => (ICollection<T>)c.ToHashSet())
    {
    }
}
