using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace EfVueMantle.BaseClasses;

internal class SoftDelete: SoftDelete<long>
{

}

internal class SoftDelete<TKey>
    where TKey: IEquatable<TKey>
{
    public bool Deleted { get; set; }
    public TKey? DeletedByUserId { get; set; }
    public DateTime DeletedDateTime { get; set; }

}
