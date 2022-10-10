using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EfVueMantle;

public interface ISoftDelete : ISoftDelete<long> { }
public interface ISoftDelete<TKey>
    where TKey: IEquatable<TKey>
{
    public bool? Deleted { get; set; }
    //TODO can't use TKey here because of nullability issues? 
    public long? DeletedByUserId { get; set; }
    public DateTime? DeletedDateTime { get; set; }
}