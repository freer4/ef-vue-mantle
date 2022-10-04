# EfVueMantle
A package connecting Entity Framework Core data to EfVueCrust

What this project aims to do, ideally, is allow data models from .net applications to be understood and interacted with by Vue3 applications. 

## At this stage, nothing is sacred, any updates might be breaking. 

> If you're finding this already somehow, please know this is a very incomplete restart of a thing I've rebuilt a couple of times, this will hopefully be a clean, clear, trimmed down version of that system. If this message is here, consider it an Alpha version

## Stack

### Entity Framework Core
Define your models as you do today, using EfVueMantle.ModelBase as your base class. [(see caveats)](#caveats)

### EfVueMantle 

[Get it on GitHub](https://github.com/freer4/ef-vue-mantle) or [Nuget.org](https://www.nuget.org/packages/EfVueMantle)

Mantle provides bases for model, controller, and service for each data type. This scaffolds the [basic functionality](#functionality) allowing Crust to explore data via convention. It also crafts Javascript class files for Crust, allowing your Vue3 application to understand your entire data structure.

### ef-vue-crust

[Get it on GitHub](https://github.com/freer4/ef-vue-crust) or `npm install ef-vue-crust`

Provides interfaces for Vue3 to interact with your defined data models via convention. 

Creates a virtual "Database" that holds onto records, remembers query results and sort orders, and generally lets you worry about presentation instead of how to transfer data back and forth.

### Vue3
Traverse properties in your Vue3 components with dot notation object accessors, and let ef-vue-crust worry about asyncronous data loading.  

(Core, Mantle, Crust, get it? Great. Naming things is hard.)


## Functionality {#functionality}
These generic methods are understood and automatically sought by ef-vue-crust.

### Get list of all ids for a model
Implemented

### Get list of ids fitting a pattern for a model
Implemented (case-insensitive exact match, case-insensitive string-include match)

### Get list of ids ordered by property
Implemented (descending and ascending)

### Get all records for a model
Implemented

### Get a record by id for a model
Implemented

### Get records from a list of ids for a model
Implemented

### Create new data record for model
Implemented

### Update data record for model
Not implemented

### Delete data record for model
Not implemented

## Getting started!

You'll need to call the ExportDataObjects method, with a path to wherever you want the exported JS to live. I suggest a directory in your VueProject/src folder named something snazzy and original like "data". It will create sub-directories for models and enums as necessary.

This will automatically pick up any models using EfVueMantle.ModelBase as a base class, and any enums used in their properties.

I just drop this in my `Program.cs`:

```
if (_hostingEnvironment.IsDevelopment())
{
    ModelExport.ExportDataObjects("../../your-vue-project/src/data");
}
```

If you need to add any custom data types, create the hypen-cased.js file for the class in a directory named "data-types". 
Then decorate your property like so: 
```
[EfVuePropertyType("SomeType")]
```
This will add an include for your custom data-type definition `SomeType`, looking for `/path/to/your/data/data-type/some-type.js`

---

## Models

The `ModelBase` class contains only three properties - `Id`, `Created`, and `Updated`. It's mostly necessary so that the other base classes can rely on the Id property, and so the exporter knows which models to roll out. 

```
public class AccountModel : ModelBase{
    //Properties to your heart's desire.
}
```

You will need to manually define FKs at the moment... hopefully that won't remain a thing. 

```
public int UserId { get; set; }
[ForeignKey("UserId")]
public UserModel User { get; set; }
```

If you have a many-to- relationship, you'll need a property for the list of the keys in your model. Be sure to mark it as `[NotMapped]`.

```
[NotMapped]
public List<int> CommentsIds { get; set; }
public List<CommentModel> Comments { get; set; } = new();
```

And you'll need to define the relationship using `WithProjection` in your dbcontext: 

```
builder.Entity<DiscussionModel>()
    .HasMany(x => x.Comments);
builder.Entity<DiscussionModel>()
    .WithProjection(
        x => x.CommentsIds,
        x => x.Comments.Where(y => y.ParentCommentId == null).Select(y => y.Id).ToList()
    );
```

There are many custom decorators, and some native ones will be recognized as well. *TODO* many of which are vestigial, and need to be cleaned up

*TODO* also need to document them all

If the `[JsonIgnore]` decorator is used, the model export will ignore that property.

### Caveats
1. The .net-> js translator currently requires that you manually define the ForeignKeys for 1-1 relationships in your models. Not a huge thing but a bit annoying. This can hopefully be overcome later

2. Although Crust supports guid PKs/ids (untested) this package defaults ids to ints. It's not a priority for me at this time, but the intent is for GUID ids to be supported
---

## Controllers

Do your controllers as usual, just use the EfVueMantle.ControllerBase class as your base. 
```
public class CommentController : ControllerBase<CommentModel, CommentService>
{
    public CommentController(CommentService commentService) : base(commentService)
    {
    }
}
```

This adds the default endpoints needed for Crust to get the data it wants, and you don't have to do anything else unless you need custom functionality. 

All base controller methods are virtual, so override if you need more complex functionality.


## Services

Just like Models and Controllers, use ServiceBase to add a service with all the basic functionality needed already set up.

```
public class CommentService : ServiceBase<CommentModel>
{
    public CommentService(YourDbContext context) : base(context.Comments, context)
    {
    }
}

```

Once again, that's all you need. Override the base methods as needed and expand the service as you like. 

## Not implemented features

### Open socket data pushing
At some point we'll have open socket data alerts sent to your local Crust databases, allowing live updates on a per-entity basis.